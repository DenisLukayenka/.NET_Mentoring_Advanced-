# Distributed Data Replication Module

## Task 1. Replication Strategy Decisions

Technical Requirements Overview:

- Handle thousands of concurrent jobs and users

### UC 1.1: Create a New Job

#### System Component Requirements

Affected models:

##### JobDefinitions

- Id: Guid (36 bytes)
- Name: string (128 bytes)
- Description: string (256 bytes)
- CronExpression: string (256 bytes)
- Concurrency: boolean (1 byte)
- UserId: Guid (36 bytes)
- JobDetailId: Guid (36 bytes)
- CreatedDate: DateTime (8 bytes)
- UpdatedDate: DateTime (8 bytes)
- Active: boolean (1 byte)
- NextExecutionDate: DateTime (8 bytes)

Size of JobDefinition: 774 bytes.

##### JobDetails

- Id: Guid (36 bytes)
- Type: string (1–32 bytes)
- Payload: string (0–1023 bytes)
- CreatedDate: DateTime (8 bytes)
- UpdatedDate: DateTime (8 bytes)

> Note: Payload may contain different data in different formats for each job. Its max size is assumed to be 1023 bytes.

Max size of JobDetail: 1107 bytes.

---

**1. Formulate system component requirements:**

- **Expected data volume** (number of records, data size in GB)
  - 1 million records
  - Raw Data size: (774 + 1107 bytes) \* 1_000_000 = ~1.7 GB
  - MongoDB overhead + indexes: ~0.6 GB
  - Oplog for replication: ~1 GB
  - Total size: ~3.3 GB

- **Expected load** (read/write requests per second)
  Assuming 5% of the jobs are modified/created throughout the day:
  - Number of modified jobs: 1_000_000 \* 0.05 = 50_000 jobs.
  - Seconds per day = 86_400

  - Number of job requests per second: 50_000 / 86_400 = ~1

- **Consistency requirements** (strong or eventual consistency)
  - Strong consistency on the primary database. Azure DocumentDB uses synchronous replication between primary and standby shards within a region, guaranteeing zero data loss and always-fresh data on failover.
  - Eventual consistency with the cross-region replica. Cross-region replication is asynchronous and cannot be configured otherwise — write confirmation is returned before replication to the replica completes. Some data may not yet be replicated at the time of failover promotion.

- **Availability requirements** (uptime expectations, acceptable downtime)
  - Azure DocumentDB SLA: 99.995%

- **Geographic distribution** (single region or multi-region deployment)
  - Multi-region deployment with the primary instance in one region and a read replica in another. Failover to the replica must be initiated manually in case of a primary region outage.

---

**2. Select the most suitable database and justify your choice:**

- **Which database type is most appropriate: SQL or NoSQL?**

  NoSQL — specifically **Azure DocumentDB** (vCore-based, MongoDB-compatible) for its scalability and consistency guarantees.

- **What are the advantages and disadvantages of the selected database given these requirements?**

  **Advantages:**
  - The dynamic `Payload` field per job type is a natural fit for a document model — no schema migrations when new job types are introduced.
  - Full MongoDB compatibility means standard drivers, query operators, and transactions work without emulation quirks.
  - vCore/compute-based pricing is predictable and avoids the RU capacity-planning complexity of Cosmos DB.
  - The model involves only two entities (`JobDetails` and `JobDefinitions`), which are cleanly handled by the built-in multi-document transaction support.

  **Disadvantages:**
  - Multi-region replication capabilities are more limited compared to Azure Cosmos DB's instant global distribution.
  - Does not offer the sub-10ms latency guarantees of Cosmos DB's native engine for globally distributed reads.

- **Deployment approach: self-hosted or cloud service?**

  Cloud service (Azure DocumentDB managed offering), to support geo-distribution, failovers, replication, and managed backups without operational overhead.

---

**3. Design the replication strategy for the selected database:**

- **Justify why replication is required for this system component.**

  Job information is the core of the entire Scheduler. An issue with the primary server would not only prevent users from managing jobs, but would also stop the Scheduler and job execution entirely. Replication in another region significantly improves the resilience of the service.

- **Describe the replication strategy:**
  - _For cloud service databases:_ Identify the default replication strategy and how you will configure it.
  - _For self-hosted databases:_ Specify which replication strategy you will implement (leader-follower, multi-leader, or leaderless).

  In Azure DocumentDB, a secondary replica is configured in another region. It supports read operations and can be promoted to become the new primary during a failover.

- **Define the replication configuration parameters** (number of replicas, synchronous/asynchronous mode, quorum settings if applicable)
  - 1 replica, asynchronous mode.
