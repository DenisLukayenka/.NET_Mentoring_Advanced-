# Distributed Data Replication Module

## UC 2.1: Execute a Job At a Scheduled Time

**Description:** Starts job execution at a specified time.
**Input:** JobId, DateTimeUtc
**Output:** ExecutionStatus, Logs

### System Component Requirements

Affected models:

#### Jobs

- Id: Guid (36 bytes)
- JobDefinitionId: Guid (36 bytes)
- CreatedAt: DateTime (8 bytes)
- UpdatedAt: DateTime (8 bytes)
- ScheduledAt: DateTime (8 bytes)
- Status: string (32 bytes)
- ErrorMessage: string (255 bytes)

Max size of Job: 383 bytes.

#### JobOutputs

- Id: Guid (36 bytes)
- JobId: Guid (36 bytes)
- Date: DateTime (8 bytes)
- Level: string/enum (32 bytes)
- Message: string, optional (255 bytes)

Max size of JobOutput: 367 bytes.

---

**1. Formulate system component requirements:**

- **Expected data volume** (number of records, data size in GB)
  - Jobs:
    - Retention period to store data: 6 months
    - Number of Jobs for retention period (jobs \* days): 1 mln jobs \* 30 days per month \* 6 months = 180_000_000 jobs
    - Jobs data size: 383 bytes \* 180_000_000 = ~64 GB
    - Cosmos DB overhead + indexes: ~30% = ~20 GB
    - Total Jobs size: ~84 Gb

  - JobOutputs:
    - Average amount of JobOutput per Job execution: 5
    - Number of JobOutput: 180 mln jobs \* 5 = 900_000_000 job outputs.
    - JobOutputs data size: 367 bytes \* 900_000_000 = ~308 GB
    - Cassandra overhead: ~30% = ~92 GB
    - Total JobOutputs size: ~400 GB

- **Expected load** (read/write requests per second)

  Assuming that all jobs are distributed evenly throught the day:
  Number of jobs run per second: 1_000_000 records / 86_400 seconds per day = ~12.
  There are 5 statuses per each job, 5 JobOutput per each Job.
  - JobDefinitions:
    - Read all required JobDefinitions: 1 bulk read per second (~12 definitions).
    - Update JobDefinition's NextExecutionDate: 12 patches per second.
    - Total: ~13 requests per second
  - Jobs:
    - Create new Job: 12 creates per second.
    - Update each job with 5 statuses: 12 \* (5 - 1) = 48 updates per second.
    - Total: ~60 requests per second.
  - JobOutputs:
    - Create new JobOutput: 12 \* 5 = 60 creates per second.
    - Total: ~60 requests per second

  **Note on peak load:** The figures above assume even distribution. In practice, jobs concentrate at common cron boundaries (midnight, top-of-hour). At peak, 10,000+ jobs may start simultaneously, generating ~50,000 writes and up to ~500,000 RU/s in a short window. A queue layer must sit in front of the Jobs store to absorb these bursts and write to Cosmos DB at a controlled rate.

- **Consistency requirements** (strong or eventual consistency)
  - Jobs: Azure Cosmos DB (NoSQL API) Consistent Prefix consistency. Use duplicate prevention by a **unique compound index on `(JobDefinitionId, ScheduledAt)`**. One insert succeeds; any concurrent insert for the same pair fails with a `DuplicateKeyError`, which the application treats as "job already scheduled."
  - JobOutputs: **Eventual consistency** — log entries are append-only and minor read delays are acceptable

- **Partition key design and query limitations**

  **Jobs — partition key: `JobDefinitionId`**

  `JobDefinitionId` is chosen as the partition key to scope the unique key constraint on `ScheduledAt` to individual definitions. This prevents duplicate job scheduling without cross-partition coordination — two concurrent inserts for the same `(JobDefinitionId, ScheduledAt)` pair race within a single partition, and only one succeeds.

  _Per-partition limits:_

  Worst case: the job task that runs every second for the entire retention period:
  - RU/s: 1 create/sec + 4 status updates/sec = 5 writes/sec × ~10 RU/write = **~50 RU/s**
  - Storage: 86,400 jobs/day × 183 days × 383 bytes = **~6 GB**

  | Limit                       | Cosmos DB limit | Projected usage              |
  | --------------------------- | --------------- | ---------------------------- |
  | RU/s per physical partition | 10,000 RU/s     | ~50 RU/s                     |
  | Logical partition size      | 20 GB           | ~6 GB over 6-month retention |

  A single high-frequency definition is partition limits.

  _Query limitations:_ Cross-definition queries (e.g., all jobs sorted by `CreatedAt` regardless of definition) fan out across all logical partitions, with RU cost scaling proportionally to partition count. This is not a supported access pattern — all reads must be scoped to a known `JobDefinitionId`.

- **Availability requirements** (uptime expectations, acceptable downtime)
  - SLA: 99.995%

- **Geographic distribution** (single region or multi-region deployment)
  - Multi-region: primary region handles all writes; a secondary region hosts a replica for read offloading and failover.

---

**2. Select the most suitable database and justify your choice:**

##### Jobs — Azure Cosmos DB (NoSQL API)

- **Which database type is most appropriate: SQL or NoSQL?**

  NoSQL — **Azure Cosmos DB** (NoSQL API, RU-based), chosen over Azure DocumentDB and Azure SQL to handle hot spikes of concurrent job starts without manual scaling or warmup delays.

- **Database comparison**

  |                                   | Azure SQL                 | Azure DocumentDB               | Cassandra                                                           | **Azure Cosmos DB**                                                              |
  | --------------------------------- | ------------------------- | ------------------------------ | ------------------------------------------------------------------- | -------------------------------------------------------------------------------- |
  | **Burst scaling**                 | Reactive, 20–60 s lag     | Shard scale-out, not instant   | Horizontal, but no autoscale                                        | **Instant, automatic**                                                           |
  | **Write throughput**              | Single primary bottleneck | Horizontal with sharding       | High — but only with eventual consistency                           | Millions of ops/sec, auto-partitioned                                            |
  | **Unique constraint under burst** | Strong, native SQL        | DuplicateKeyError per-document | No native support                                                   | Unique keys scoped per partition — works with `JobDefinitionId` as partition key |
  | **Lock contention**               | High under burst          | Document-level, low            | None for regular writes                                             | No locks — optimistic concurrency via ETags; conflicts returned immediately      |
  | **Availability SLA**              | 99.99% (Premium)          | 99.995%                        | Depends on deployment                                               | **99.999%** (multi-region)                                                       |
  | **Consistency model**             | Strong (native)           | Tunable?                       | Tunable (ONE → QUORUM)                                              | 5 tunable levels including Strong                                                |
  | **Query complexity**              | Best (full SQL)           | Full MQL + aggregations        | partition-scoped; cross-partition queries require secondary indexes | Core operations only, limited aggregations                                       |
  | **Pricing**                       | Predictable               | Predictable vCore              | Infrastructure cost, self-managed or AKS                            | Variable RU — can spike under sustained high load                                |

- **What are the advantages and disadvantages of the selected database given these requirements?**

  **Advantages:**
  - **Instant, zero-warmup scaling**: when thousands of jobs are scheduled at the same time (e.g., midnight batch window or an hourly hot spot), Cosmos DB absorbs the spike transparently without pre-provisioning or reactive scale-up lag.
  - **Automatic partitioning**: writes are distributed across partitions automatically; no manual shard key strategy required as with DocumentDB.
  - **Unique key constraint per partition**: with `JobDefinitionId` as the partition key, a unique constraint on `ScheduledAt` enforces `(JobDefinitionId, ScheduledAt)` uniqueness natively — no cross-shard coordination overhead and no application-level duplicate handling fallback needed.
  - **Exceeds availability target**: 99.999% multi-region SLA surpasses the 99.995% requirement.
  - The optional `ErrorMessage` field fits naturally in the document model without schema migrations.

  **Disadvantages:**
  - **Variable RU cost**: pricing scales with throughput consumed; a sustained burst can spike costs unpredictably. Requires setting a max RU cap or using the serverless tier with cost modelling.
  - **Limited query capabilities**: optimized for point reads and simple queries. Complex aggregations (e.g., count failed jobs per definition per day) require Synapse Link or offloading to a separate analytics store.
  - **Not co-located with JobDefinitions**: if UC 1.1 uses DocumentDB, cross-collection lookups during job dispatch cross a service boundary instead of staying within one cluster.

  **Why Cassandra was rejected for Jobs:**
  Cassandra's high write throughput requires eventual consistency. Enforcing the `(JobDefinitionId, ScheduledAt)` uniqueness constraint in Cassandra requires Lightweight Transactions (LWT — `INSERT IF NOT EXISTS`). Under midnight burst conditions, LWT serializes writes within each partition and eliminates the throughput advantage entirely. Cosmos DB's native unique key constraint achieves the same guarantee at regular write cost.

- **Deployment approach: self-hosted or cloud service?**

  Cloud service (Azure Cosmos DB managed offering).

##### JobOutputs — Azure Cosmos DB for Apache Cassandra

- **Which database type is most appropriate: SQL or NoSQL?**

  NoSQL — **Azure Cosmos DB for Apache Cassandra**, a wide-column store purpose-built for high-throughput, append-only, time-series log data.

- **What are the advantages and disadvantages of the selected database given these requirements?**

  **Advantages:**
  - Cassandra's wide-column model with `JobId` as partition key and `Date` as clustering key maps directly to the access pattern — queries for a job's log history are sequential and efficient.
  - Global distribution and replication are handled transparently by the Cosmos DB platform — same model as the NoSQL API, no manual keyspace replication configuration required.
  - TTL support allows automatic expiration of old log records without a separate cleanup process.
  - Wire protocol compatibility means standard Cassandra drivers and CQL work with minimal connection string changes.

  **Disadvantages:**
  - Eventual consistency means very recently written log entries may not be immediately visible across regions.
  - The data model must be designed around known access patterns upfront; ad-hoc queries such as "find all error-level outputs across all jobs" require secondary indexes or materialized views, adding operational complexity.

- **Deployment approach: self-hosted or cloud service?**

  Cloud service (Azure Cosmos DB for Apache Cassandra).

---

**3. Design the replication strategy for the selected database:**

##### Jobs — Azure Cosmos DB

- **Justify why replication is required for this system component.**

  Jobs are written and read at high frequency, with hot hours where a large number of jobs fire simultaneously. Azure Cosmos DB provides geo-replication out of the box with near-infinite horizontal scalability, making it a natural fit — reads can be offloaded to the secondary region during peak load, and the service absorbs burst traffic without any manual intervention. Without replication, a single-region outage would stop job execution entirely.

- **Describe the replication strategy:**

  Azure Cosmos DB applies two layers of replication automatically:
  - **Within a region (synchronous):** Every write is synchronously replicated to 4 replicas inside the primary region before being acknowledged. This provides zero data loss and high availability within the region at no extra configuration.
  - **Cross-region (asynchronous):** A secondary region is added to the account and data is continuously replicated there asynchronously. This region handles read offloading during peak hours and serves as a failover target if the primary region goes down.

  The Consistent Prefix consistency level applies globally, ensuring reads in all regions never observe out-of-order writes.

- **Define the replication configuration parameters:**
  - Within-region: 4 synchronous replicas (built-in, non-configurable)
  - Cross-region: 1 secondary read region, asynchronous replication mode
  - Consistency level: Consistent Prefix (configured at account level, applied globally)
  - Automatic failover: enabled; secondary region set as priority-1 failover target

##### JobOutputs — Azure Cosmos DB for Apache Cassandra

- **Justify why replication is required for this system component.**

  Log data is written at high throughput (~60 writes/sec, accumulating hundreds of millions of records over the retention period). A regional outage without replication would create a gap in execution logs, making it impossible to diagnose failures or audit job history. Cross-region replication ensures log data remains available and durable even during an outage.

- **Describe the replication strategy:**

  Azure Cosmos DB for Apache Cassandra uses the same internal replication model as all Cosmos DB APIs — `NetworkTopologyStrategy` and RF values in `CREATE KEYSPACE` are accepted for CQL compatibility but are ignored; replication is managed entirely by the platform:
  - **Within a region (synchronous):** Every write is synchronously replicated to 4 replicas before being acknowledged, providing zero data loss within the region.
  - **Cross-region (asynchronous):** A secondary region is added at the account level and data is continuously replicated there asynchronously.

- **Define the replication configuration parameters:**
  - Within-region: 4 synchronous replicas (built-in, non-configurable)
  - Cross-region: 1 secondary read region, asynchronous replication mode
  - Consistency level: Eventual (configured at account level; `LOCAL_ONE` CQL keyword is accepted and maps to Eventual)
  - Automatic failover: enabled; secondary region set as priority-1 failover target

