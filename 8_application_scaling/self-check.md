## Questions for the self-check

**1. Scaling Fundamentals**

- Q1.1: What is the difference between vertical and horizontal scaling? What are the trade-offs of each approach?

  **Vertical (scale up):** add more CPU/RAM/disk to a single node. Simple, no code changes, but limited by hardware, expensive at the top end, and a single point of failure.
  **Horizontal (scale out):** add more nodes behind a load balancer. Almost unlimited capacity and built-in redundancy, but requires stateless services, coordination (consensus, sharding), and adds distributed-systems complexity (network failures, consistency).

- Q1.2: What is load balancing, and why is it essential for horizontally scaled systems?

  A load balancer spreads incoming requests across multiple instances using algorithms (round-robin, least-connections, weighted, hash-based) at L4 (TCP/UDP) or L7 (HTTP, with content-based routing and TLS termination). It is essential because horizontal scaling only helps if traffic is spread evenly, unhealthy nodes are skipped (via health checks), sticky sessions support stateful clients, new instances auto-register on scale-out, and clients see a single endpoint no matter how many nodes are behind it.

- Q1.3: How does fault tolerance contribute to system reliability in distributed architectures?

  Fault tolerance keeps the system running when individual components fail. With redundancy, replication, retries with backoff, circuit breakers, bulkheads, timeouts, health probes, idempotent operations, and graceful degradation, the system avoids cascading failures and stays available even when nodes, networks, or dependencies misbehave.

**2. Batch and Stream Processing**

- Q2.1: What is batch processing, and what are typical use cases where batch processing is most appropriate?

  Batch processing runs over a finite dataset on a schedule or trigger, optimizing for throughput rather than latency. Typical use cases: nightly ETL, billing/invoicing, payroll, report generation, ML model training, large data migrations, and end-of-day reconciliations.

- Q2.2: What is stream processing, and how does it differ from batch processing in terms of data handling and latency?

  Stream processing handles an unbounded sequence of events continuously, producing results in near-real-time (milliseconds to seconds). Unlike batch, which sees the whole dataset at once and accepts delays of minutes to hours, streams work record-by-record or in time windows and accept some incompleteness (late/out-of-order events) in exchange for low latency.

- Q2.3: When would you choose a hybrid processing architecture that combines both batch and stream processing?

  When the same data must serve both real-time decisions (fraud detection, dashboards, alerts) and accurate historical analytics (reports, ML training). Lambda is the true hybrid pattern — separate batch and speed layers merged in a serving view. Kappa is a stream-only alternative that replays the immutable log through the same engine to avoid maintaining two pipelines; choose Kappa when stream tooling can meet accuracy needs.

- Q2.4: What are the main challenges when implementing stream processing in high-load systems?

  Out-of-order and late events, effectively-once processing (via transactional sinks + idempotent consumers — true exactly-once over a network is impossible), backpressure, watermarking and windowing, managing state at scale, partitioning/keying for parallelism (avoiding hot partitions), schema evolution, monitoring lag, and making sure data is durable and can be replayed after consumer failures.

**3. MapReduce and Distributed Processing**

- Q3.1: What is the MapReduce programming model, and how does it enable distributed data processing?

  MapReduce (Google, 2004) splits a large dataset into input splits processed in parallel across many nodes using two user-defined functions: `map` produces intermediate key/value pairs, and `reduce` aggregates values per key. The framework handles input partitioning, data locality (moving compute to the data), shuffling, sorting, optional combiners (local pre-reduction), fault recovery, and re-running failed tasks, hiding most of the distributed complexity from the developer.

- Q3.2: What are the Map and Reduce phases, and what happens in each phase?

  **Map:** each worker reads its input split (ideally co-located for data locality), applies the map function, and emits intermediate `(key, value)` pairs. An optional combiner can pre-aggregate locally to reduce shuffle traffic.
  **Shuffle/Sort:** the framework groups all values by key and routes them to reducers.
  **Reduce:** each reducer receives a key with all its values and aggregates them (sum, count, join, etc.), persisting final output to distributed storage.

- Q3.3: What are some real-world applications that benefit from MapReduce?

  Web index building, log analysis, word/event counting, building inverted indexes, large-scale joins, pre-computing recommendations, click-stream aggregation, genomics pipelines, and any embarrassingly parallel batch analytics on TB/PB-scale data.

**4. Messaging and Event-Driven Architecture**

- Q4.1: What is the role of a message broker in a distributed system?

  A message broker decouples producers from consumers by buffering, routing, and delivering messages. It absorbs load spikes, enables asynchronous communication, provides durability with delivery guarantees (at-least-once, at-most-once, exactly-once), supports retries, dead-letter queues, FIFO ordering, deduplication, pub/sub and competing-consumer patterns, and lets components scale and fail independently.

- Q4.2: What is the difference between the Publisher-Subscriber pattern and the Producer-Consumer pattern?

  **Producer-Consumer (queue):** each message is delivered to exactly one consumer in a group; used for work distribution. Better suited for sync-like operations — the producer expects the work to be completed by exactly one worker and can correlate a reply (request-reply over a queue) when a response is needed.
  **Publisher-Subscriber (topic):** each message is fanned out to all interested subscribers; used for broadcasting events. Inherently async fire-and-forget — the publisher doesn't know or care who consumes the event, making it ideal for fully decoupled, asynchronous notifications. Per message, pub/sub fans out to many subscribers while producer/consumer delivers to exactly one consumer in a competing-consumers group; both topologies can have many producers and many consumers overall.

- Q4.3: What is an event stream, and how is it used in event-driven architectures?

  An event stream is an ordered, append-only, durable log of immutable events (e.g., a Kafka topic, Azure Event Hubs). Streams are partitioned for scale and per-key ordering, retain events per a configured policy (time/size/compacted), and support non-destructive reads — unlike queues, which are typically consume-once. Consumers read at their own pace from independent offsets, can replay history, and multiple services can react to the same events, making the stream the system's source of truth and main integration channel.

- Q4.4: What are the benefits of using event-driven architecture compared to synchronous request-response patterns?

  Loose coupling between services, independent scaling and deployment, natural resilience (the broker buffers downtime), better support for fan-out, audit trails via event logs, easier to add new consumers, and higher throughput because producers don't wait for consumers.

**5. Event Sourcing**

- Q5.1: What is event sourcing, and how does it differ from traditional state management?

  Event sourcing stores every state change as an immutable event in an append-only event store; current state is rebuilt by replaying events, with snapshots as a performance optimization. Traditional CRUD keeps only the latest state, overwriting history. Event sourcing keeps the full audit trail, supports temporal queries ("state as of timestamp X"), and lets you build new read models (projections) by replaying the log — events are typically named in past tense to capture business intent.

- Q5.2: What are the main benefits and challenges of implementing event sourcing?

  **Benefits:** complete audit log, point-in-time queries, easy debugging, replay-driven projections, natural fit with event-driven systems and CQRS.
  **Challenges:** event schema/versioning, eventual consistency, snapshots for performance, harder querying (append-only streams need separate read models), unbounded storage growth, GDPR/PII compliance (deletion typically via crypto-shredding or compensating events), optimistic concurrency on streams, and steeper learning curve.

- Q5.3: How do event sourcing and CQRS complement each other in distributed systems?

  Event sourcing is a persistence pattern (state stored as an append-only event log); CQRS separates command and query responsibilities into distinct models. Together, commands produce events appended to the event store on the write side, and projections consume those events to build optimized, denormalized read models — giving independent scaling of reads and writes, temporal queries, and replay-driven rebuilds, at the cost of eventual consistency.

**6. Advanced Patterns**

- Q6.1: Why is immutability important in event-driven systems, especially with respect to state management?

  Immutable events guarantee a reliable history: consumers can replay safely, projections can be rebuilt, audits are trustworthy, and concurrent consumers can't corrupt past data. Stable event IDs make idempotent processing and deduplication safe under at-least-once delivery; immutability preserves causal ordering, enables temporal/time-travel queries, simplifies caching and distribution across replicas, and makes the event log a clear source of truth — schema evolution is handled via explicit versioning/upcasting rather than in-place mutation.

**7. Caching and Performance**

- Q7.1: What are common distributed caching patterns (cache-aside, write-through, write-behind)?

  **Cache-aside (lazy loading):** app reads from cache; on miss, loads from DB and fills the cache. Simple, but stale data possible.
  **Write-through:** writes go to cache and DB at the same time. Consistent but slower writes.
  **Write-behind (write-back):** writes go to cache, then saved to DB asynchronously. Fast writes, but risk of data loss on failure.
  **Read-through:** the cache itself loads from DB on miss (app sees a single API).

- Q7.2: How does distributed caching help address scalability bottlenecks?

  It offloads repeated reads from the database, cuts latency, reduces network/compute cost, and provides a horizontally scalable layer (sharded or replicated) that absorbs traffic spikes — letting the database focus on writes and complex queries only. It also externalizes session and shared state so app instances stay stateless behind a load balancer, and supports cross-instance coordination through distributed locks, rate-limit counters, and idempotency keys.

- Q7.3: What factors should you consider when setting cache expiration (TTL) policies?

  How often the data changes, how much staleness is acceptable (consistency SLA), read/write ratio, cost of a cache miss vs. cost of a stale read, origin/downstream load (e.g., Cosmos DB RU spikes), invalidation strategy (event-driven vs. TTL), tiered TTLs across cache layers (CDN edge vs. app cache), regulatory/PII retention limits, and whether the data is user-specific or shared.

**8. Practical Application**

- Q8.1: For a job scheduling system, would you use batch processing, stream processing, or a hybrid approach for executing scheduled jobs? Justify your choice.

  Hybrid. Use **event-driven (message-driven) processing** for trigger evaluation and dispatch — the scheduler publishes due-job events to Service Bus and Durable Functions consume them in near-real-time, giving low latency and easy scaling via competing consumers. Use **batch processing** (timer-triggered Functions) for periodic heavy operations: long-running data jobs, retry cleanup, reconciliation, reporting, and aggregations over completed jobs. This combines responsiveness with efficient bulk work.

- Q8.2: What events would you publish for the use case "Create a New Job"? Design a sample event schema.

  Events: `JobCreated`, `JobScheduled`, `JobCreationFailed` (validation failures produce `JobCreationFailed` rather than a separate event).

  Schema follows the CloudEvents 1.0 envelope:

  ```json
  {
    "specversion": "1.0",
    "id": "b3e1f0a2-...-...",
    "type": "com.scheduler.job.created.v1",
    "source": "/scheduler-api/jobs",
    "subject": "job-9f1c...",
    "time": "2026-06-26T10:15:30.123Z",
    "datacontenttype": "application/json",
    "dataschema": "https://schemas.scheduler/job-created/v1.json",
    "correlationid": "req-7c9a...",
    "causationid": "cmd-3a2b...",
    "partitionkey": "tenant-42",
    "tenantid": "tenant-42",
    "data": {
      "aggregateId": "job-9f1c...",
      "aggregateType": "Job",
      "name": "nightly-report",
      "schedule": "0 2 * * *",
      "timezone": "UTC",
      "jobType": "Report",
      "parameters": { "reportId": "r-101" },
      "owner": "user-15",
      "enabled": true
    }
  }
  ```

- Q8.3: How would you handle failure scenarios in an event-driven architecture (e.g., message broker downtime, consumer failures)?

  Use durable, replicated brokers and acknowledged delivery; producers retry with exponential backoff and use the Transactional Outbox pattern for at-least-once publishing. Consumers acknowledge messages only after successful processing (Service Bus peek-lock + Complete; Kafka/Event Hubs commit offsets), send poison messages to a dead-letter queue (auto-DLQ after MaxDeliveryCount in Service Bus), and use idempotency keys (or broker-side duplicate detection) to handle redelivery safely. Add circuit breakers, bulkheads, sagas/compensating actions for multi-step failures, and consumer-lag monitoring; for broker downtime, buffer locally via the outbox, and design every handler to be retry-safe and idempotent.

- Q8.4: If you need to process 1 million jobs scheduled for execution at the same time, what processing model and architecture would you choose?

  Partitioned stream + horizontally scaled workers. Place job triggers on a partitioned event log (Azure Event Hubs partitioned by `jobId` hash, or Service Bus topics with sessions for per-key ordering) so order and load spread across partitions. A pool of stateless Azure Container Apps workers (KEDA auto-scaling on queue depth / consumer lag) consumes in parallel, scaled out by adding partitions/replicas. Pre-schedule messages (Service Bus scheduled enqueue) to avoid thundering-herd at the spike. Use Azure Cache for Redis for hot job metadata, idempotency keys to survive retries (or Service Bus duplicate detection), backpressure and rate-limiting toward downstream systems, Cosmos DB partition key aligned to `jobId`, and built-in dead-letter queues for failures. For long-running work, hand off to Durable Functions fan-out/fan-in orchestrations and publish completion events back to the stream.
