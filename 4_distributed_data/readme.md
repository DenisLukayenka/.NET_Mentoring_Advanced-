# Distributed Data

## Collections

### JobDefinitions

Azure MongoDB, used by the Scheduler Service. Contains `CronExpression` for managing job frequency and execution time.

Isolation level is **Read Committed**. Supports concurrent reads from multiple Scheduler instances while preventing dirty reads.

### JobDetails

Azure MongoDB for storing job execution payloads. Different integrations require different payload schemas, so a document-oriented database is ideal. Each `JobDefinition` has one `JobDetail`. Kept in a separate collection because it is accessed by the Job Runner, not the Scheduler Service.

Isolation level is **Read Committed**. Written rarely (user-driven). Read Committed is sufficient to avoid reading partial or invalid payloads.

### Jobs

Azure MongoDB for storing the history of job runs with their execution status. Has a unique constraint on `(JobDefinitionId, ScheduledAt)` to prevent duplicate job instances for the same scheduled slot.

Isolation level is **Read Committed**. The unique constraint on `(JobDefinitionId, ScheduledAt)` handles duplicate prevention at the database level, so Serializable is not needed — a conflicting insert will simply fail the constraint.

### JobOutputs

Cassandra DB for storing output and log entries for each job run. Well suited for append-only, high-volume writes and time-series data (partitioned by `JobId`, clustered by `Date`).

Cassandra uses **tunable consistency** — write consistency of `ONE` is appropriate, prioritising write throughput over strict consistency, which is acceptable for log data.
