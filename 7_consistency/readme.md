# Consistency Requirements Analysis

---

## UC 1.1 — Create a New Job

A job is one `JobDefinition` (schedule/config) plus one `JobDetail` (payload), both in **Azure
DocumentDB** (MongoDB-compatible). At create time they are **written together atomically**; they **do
not share a _read_ consistency requirement**, and that split is what pays off later (see UC 2.1).

### Native consistency options (DocumentDB / MongoDB)

| Native setting                                                                                              | Guarantee                                                                                  | ~ model                       |
| ----------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------ | ----------------------------- |
| `readPreference: primary` + `readConcern: linearizable` (+ `w: majority`)                                   | Reflects all majority-acknowledged writes completed before the read; primary only, slower. | **Strong**                    |
| `readPreference: primary`, `readConcern: local` _(default)_                                                 | Reads the primary's latest in-memory state; fresh in practice, no majority wait.           | **Strong-ish** (primary-only) |
| Causally consistent session + `readConcern: majority` + `w: majority` (or simply `readPreference: primary`) | Read-your-writes + monotonic reads — holds even when reading a secondary. _Causal sessions are **not used in code** — see Chosen consistency._ | **Read-after-write** (causal) |
| `readPreference: secondary` / `secondaryPreferred`, `readConcern: local`                                    | Reads the geo-replica; may lag and may roll back.                                          | **Eventual**                  |

> **`linearizable` caveat.** In MongoDB, `readConcern: linearizable` only provides its guarantee for
> queries that **uniquely identify a single document** — it does not strengthen multi-document
> queries (such as the scheduler's due-jobs poll in UC 2.1). Additionally, Azure DocumentDB's
> [feature-compatibility documentation](https://learn.microsoft.com/en-us/azure/documentdb/compatibility-features)
> does not list `linearizable` as supported. The row above is kept to show the full MongoDB model,
> but the consistencies chosen below deliberately avoid it for both reasons.

> A DocumentDB geo-replica replicates
> **asynchronously**, so it always trails the primary by some replication lag. A plain replica read
> (`readConcern: local`) is therefore eventually consistent and may be stale; only the primary is
> guaranteed current. The same holds for the Cosmos DB / Cassandra tables in UC 2.1, where lag follows the chosen
> level.

### Chosen consistency

| Entity                        | Operation                                                           | Critical | Chosen consistency                                |
| ----------------------------- | ------------------------------------------------------------------- | -------- | ------------------------------------------------- |
| `JobDefinition` + `JobDetail` | Create both atomically (multi-document transaction)                 | **Yes**  | **Strong write** (`w: majority` on `primary`)     |
| `JobDefinition` + `JobDetail` | Read back the just-created job to confirm / return it to the caller | No       | **Read-after-write** (in code: plain `primary` read — causal sessions are not implemented) |

- **Create → atomic, durable write.** Both documents are written in a single multi-document
  transaction on the primary with `w: majority`, so a job is never half-created (a definition with no
  detail, or vice-versa). This atomic write is the whole point of UC 1.1.
- **Read-back → Read-after-write, implemented as a primary read.** If the create path returns the
  stored job to its caller, that read must reflect the write just made. The MongoDB-native way is a
  **causally consistent session**, but that means creating a session, threading its handle through
  every repository API, and keeping it alive across calls — implementation complexity we deliberately
  **do not take on in this codebase**. Instead the read-back simply targets the **primary** (the same
  keyed client that ran the create transaction): the primary always reflects its own committed writes,
  so read-your-writes holds with zero session machinery. The trade-off is that the guarantee is
  "reads of the primary are fresh" rather than a portable causal chain — which is all this use case
  needs.

---

## UC 2.1 — Execute a Job At a Scheduled Time

The scheduler **reads `JobDefinition`s each scheduling window to find due jobs** (**Azure DocumentDB**).
When one comes due it updates that definition's `NextExecutionDate` so the slot fires once, reads the
`JobDetail` payload to run it, then writes a `Job` record (**Azure Cosmos DB for NoSQL**) and appends
`JobOutput` log entries (**Azure Cosmos DB for Cassandra**).

### Native consistency options (Cosmos DB for NoSQL — `Jobs`)

Five account-level levels, strongest → weakest:

| Native level            | Guarantee                                                                        | ~ model                               |
| ----------------------- | -------------------------------------------------------------------------------- | ------------------------------------- |
| **Strong**              | Linearizable — every read sees the latest committed write across regions.        | **Strong**                            |
| **Bounded Staleness**   | Lag bounded by at most _K_ versions or _T_ time, whichever comes first.          | near-**Strong** within _K_/_T_        |
| **Session** _(default)_ | Per client session: read-your-writes + monotonic + ordered, via a session token. | **Read-after-write** (causal session) |
| **Consistent Prefix**   | Never returns writes out of order, but may be stale — _no_ read-your-writes.     | ordered **Eventual**                  |
| **Eventual**            | No ordering; replicas converge eventually.                                       | **Eventual**                          |

### Native consistency options (Cosmos DB for Cassandra — `JobOutputs`)

CQL tunable consistency, mapped onto the Cosmos account level:

| Native level                      | Guarantee                                               | ~ model               |
| --------------------------------- | ------------------------------------------------------- | --------------------- |
| `QUORUM` / `LOCAL_QUORUM`         | A majority of replicas must agree on the write/read.    | approaches **Strong** |
| `ONE` / `LOCAL_ONE` _(used here)_ | Ack/read from a single replica — fastest, may be stale. | **Eventual**          |

### Chosen consistency

| Entity          | Operation                                                                   | Critical | Chosen consistency                              |
| --------------- | --------------------------------------------------------------------------- | -------- | ----------------------------------------------- |
| `JobDefinition` | Read each scheduling window (~1s) to find jobs that are due (DocumentDB)    | **Yes**  | **Primary read** (`primary`, default `local`)             |
| `JobDetail`     | Read the payload at fire time to run the job (DocumentDB)                   | No       | **Eventual** (`secondary`, fall back to `primary` on miss) |
| `JobDefinition` | Claim the due slot: update `NextExecutionDate` by `_id` (DocumentDB)        | **Yes**  | **Atomic conditional `findOneAndUpdate`** (`w: majority`) |
| `Jobs`          | Insert the run row, unique on `(JobDefinitionId, ScheduledAt)`              | **Yes**  | **Write-time unique key** (consistency level N/A)         |
| `Jobs`          | Runner writes status transitions (`Pending→Running→Succeeded/Failed`)       | No       | **Session**                                     |
| `Jobs`          | Scheduler/runner reads its own writes (queries `Jobs` by id)                | No       | **Session**                                     |
| `Jobs`          | User views run history on the dashboard                                     | No       | **Eventual**                                    |
| `JobOutputs`    | Append and read execution log entries for diagnostics                       | No       | **Eventual** (`LOCAL_ONE`)                      |

- **`JobDefinition` read → Primary read.** The scheduler polls definitions every second on
  `NextExecutionDate` / `Active` / `CronExpression`. The poll is a **multi-document query**, so
  `linearizable` would not apply to it even where supported (see the caveat in UC 1.1) — and it is not
  documented as supported by Azure DocumentDB anyway. Reading the **primary** with the default `local`
  read concern is sufficient: the primary's state is current, and **correctness does not rest on this
  read at all** — it rests on the atomic claim below. The worst a marginally stale poll can do is delay
  a fire by one ~1s cycle, which the next poll self-heals. The replica is still avoided: replication
  lag there is unbounded, so a replica poll could delay or replay fires for the full lag window.
- **`JobDetail` read → Eventual, with primary fallback.** The payload is read only at fire time; it is
  not part of the per-second scheduling decision, so seconds of lag never matter for *updates* to it.
  Keeping the large payload on the replica offloads the primary at no correctness cost. One edge case:
  a job created and due **within the replication-lag window** may be claimed off the primary before its
  `JobDetail` has reached the replica — the replica read finds *nothing*. The runner therefore **falls
  back to the primary when the replica read misses**, closing the gap without giving up the offload in
  the common case.
- **Slot claim → atomic conditional update (the exactly-once mechanism).** When a definition comes due,
  the scheduler claims it with a single `findOneAndUpdate` by `_id` whose **filter also matches the
  expected due `NextExecutionDate`**, setting the next one (`w: majority`). When _N_ schedulers race,
  the filter matches for exactly one — the rest find no document and walk away. This conditional write
  is the **primary duplicate-prevention mechanism**; it is why the poll read above needs no strong
  guarantee.
- **Duplicate prevention backstop → write-time unique key.** Two runners must never both execute the
  same `(JobDefinitionId, ScheduledAt)` slot. The unique key on `ScheduledAt` scoped to the
  `JobDefinitionId` partition makes concurrent inserts race within one partition — exactly one wins,
  the rest get a conflict (`409`). This is enforced **at write time inside the partition** and is
  **independent of the account consistency level** — Cosmos DB consistency levels govern read
  visibility, and per-request overrides can only *weaken* the account level, never strengthen it, so
  labeling this insert "Strong" would be neither meaningful nor implementable on a Session account.
  This key is the **cross-database backstop** to the slot claim above: if a future change ever breaks
  the claim, the unique key still stops a duplicate run record.
- **`Jobs` → Session (account default).** No component reads `Jobs` by status to drive control flow — a
  runner takes a job by id and runs it to completion. **Session** is kept regardless because it is the
  Cosmos default and **preserves read-your-writes for any query the scheduler/runner issues** (e.g.
  reading a `Job` by id right after writing it). Dropping to **Eventual** would constrain read-your-writes
  and block those query patterns during implementation.
- **History → Eventual.** A run-history dashboard does not need sub-second freshness; with Session,
  a reader holding no session token reads as eventual, offloading the secondary region.
- **`JobOutputs` → Eventual.** Append-only, immutable, high-volume logs. Entries never mutate (no
  stale-overwrite hazard) and a line surfacing a second later is fine for diagnostics; write throughput
  and availability dominate (Cassandra `ONE` / `LOCAL_ONE`).

---
