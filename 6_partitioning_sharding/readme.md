# Partitioning / Sharding Strategy

## 1. Data Growth and Access Patterns

### JobDefinitions + JobDetails — Azure DocumentDB (MongoDB-compatible)

| Growth        | Value                                                           |
| ------------- | --------------------------------------------------------------- |
| Total records | ~1M (configuration-like, user-driven)                           |
| Net growth    | Low — ~50,000/day modified or created (5% of 1M) → ~1 write/sec |
| Total size    | ~2.2 GB (incl. 30% overhead + indexes)                          |

| Access pattern             | Query shape                                              | Frequency        |
| -------------------------- | -------------------------------------------------------- | ---------------- |
| Job execution lookup       | `WHERE _id = ?` on `jobDefinitions`                      | ~12 reads/sec    |
| Fetch job payload          | `WHERE _id = ?` on `jobDetails`                          | ~12 reads/sec    |
| Update next execution time | `PATCH NextExecutionDate WHERE _id = ?`                  | ~12 writes/sec   |
| User browses job list      | `WHERE UserId = ?` on `jobDefinitions`                   | Low, interactive |
| Create job definition      | Insert `jobDefinitions` + `jobDetails` (one transaction) | ~1 write/sec     |

### Jobs — Azure Cosmos DB (NoSQL API)

| Growth           | Value                          |
| ---------------- | ------------------------------ |
| Daily / monthly  | ~1M/day (12/sec) → ~30M/month  |
| Retention volume | ~180M records (6-month window) |
| Total size       | ~84 GB (incl. 30% overhead)    |

| Access pattern              | Query shape                                        | Frequency       |
| --------------------------- | -------------------------------------------------- | --------------- |
| Create job instance         | Insert, unique on `(JobDefinitionId, ScheduledAt)` | ~12 inserts/sec |
| Update job status           | `PATCH Status WHERE id = ?, JobDefinitionId = ?`   | ~48 updates/sec |
| Job history / status filter | `WHERE JobDefinitionId = ? [AND Status = ?]`       | On-demand       |

Cross-definition queries are unsupported (fan-out across all partitions). **Peak:** jobs cluster at cron boundaries — 10,000+ may fire at once (~50,000 writes); a queue layer absorbs the burst.

### JobOutputs — Azure Cosmos DB for Apache Cassandra

| Growth           | Value                          |
| ---------------- | ------------------------------ |
| Daily / monthly  | ~5M/day (5/job) → ~150M/month  |
| Retention volume | ~900M records (6-month window) |
| Total size       | ~400 GB (incl. 30% overhead)   |

| Access pattern    | Query shape                                | Frequency      |
| ----------------- | ------------------------------------------ | -------------- |
| Write log entry   | `INSERT (JobId, Date, Level, Message)`     | ~60 writes/sec |
| Read job run logs | `WHERE JobId = ? ORDER BY Date ASC`        | On-demand      |
| Time-range read   | `WHERE JobId = ? AND Date BETWEEN ? AND ?` | On-demand      |
| Expiration        | TTL per record                             | Continuous     |

All reads scoped to one `JobId`; cross-job aggregation is unsupported.

---

## 2. Partitioning vs. Sharding Decision

### JobDefinitions + JobDetails — Azure DocumentDB

**Decision: No sharding at current load — single physical shard, scale vertically.**

DocumentDB splits a collection into **logical shards** (one per shard-key value) mapped onto **physical shards** (nodes). Per [guidance](https://learn.microsoft.com/en-us/azure/documentdb/partitioning), sharding isn't required unless a collection can exceed one physical shard's capacity (32 TB disk). At ~2.2 GB, ~13 writes/sec and ~24 reads/sec, a single node is far from that limit — the first scaling lever is **vertical** (cluster tier / storage SKU), and even 10× growth (~22 GB) stays well under it. While unsharded, `WHERE UserId = ?` is a single local indexed lookup.

**If it ever outgrows one physical shard:** DocumentDB shards only by **hashing** the shard key (no range or list option). In out case:

| Collection       | Shard key (hashed)          | Why                                                                                                                                                                                                                                                                                                  |
| ---------------- | --------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `JobDefinitions` | `_id`                       | Highest cardinality (random GUID) → even spread, and the dominant filter (`WHERE _id = ?`), so point lookups hit one shard. `UserId` rejected — enterprise users would hotspot a shard and its cardinality is uneven; a secondary index on `UserId` serves list views (scatter-gather once sharded). |
| `JobDetails`     | `_id` = `JobDefinition._id` | 1:1 relation → identical key co-locates each detail with its definition on the same shard, keeping the create transaction and dispatch read single-shard. Makes `JobDefinition.JobDetailId` redundant.                                                                                               |

**Shard count:** none today; set at cluster creation when needed and increased later (the service rebalances automatically), keeping each logical shard under the 4 TB best-practice size. No fixed count is committed up front.

### Jobs — Azure Cosmos DB (NoSQL API)

**Decision: Partitioning — managed service, no manual sharding.** Cosmos DB splits and moves physical partitions automatically; the only application choice is the logical partition key.

**Shard count: N/A.**

- **Strategy:** Hash-based (Cosmos hashes the partition key internally; not configurable).
- **Partition key: `JobDefinitionId`.** Every query is definition-scoped (history, status filter, duplicate check), so no cross-partition fan-out, and the unique key on `ScheduledAt` is enforced within one partition — concurrent duplicates race in one place, no coordination.
  Worst-case hot partition (1-sec job, 6 months) ≈ 6 GB / ~50 RU/s, within the 20 GB / 10,000 RU/s limits.

### JobOutputs — Azure Cosmos DB for Apache Cassandra

**Decision: Partitioning — managed service, no manual sharding.** Cosmos DB abstracts Cassandra's ring/token management; `CREATE KEYSPACE` replication settings are accepted but ignored. Self-hosting ~400 GB at RF 3 would need ~3–5 nodes.

**Shard count: N/A.**

- **Strategy:** Hash-based.
- **Partition key: `JobId`, clustering key `Date ASC`.** Every read is scoped to one `JobId`; co-locates a run's log entries in one partition for sequential, time-ordered reads with no secondary index. Partitions are tiny and bounded (~1.8 KB/run) → no hotspot risk; TTL expires records automatically.

---

## 3. Summary

| Entity           | Database              | Partition / Shard key                         | Strategy | Notes                                                    |
| ---------------- | --------------------- | --------------------------------------------- | -------- | -------------------------------------------------------- |
| `JobDefinitions` | DocumentDB            | `_id` (only if sharded)                       | Hash     | Unsharded now (2.2 GB ≪ 32 TB); `UserId` secondary index |
| `JobDetails`     | DocumentDB            | `_id` = `JobDefinition._id` (only if sharded) | Hash     | Shared 1:1 id co-locates with definition                 |
| `Jobs`           | Cosmos DB (NoSQL)     | `JobDefinitionId`                             | Hash     | Definition-scoped; unique `ScheduledAt` per partition    |
| `JobOutputs`     | Cosmos DB (Cassandra) | `JobId` (clustering `Date ASC`)               | Hash     | Append-only logs; bounded partitions; TTL                |
