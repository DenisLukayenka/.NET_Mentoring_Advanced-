#### Data Storage and Transmission Format

1. **Comprehensive Serialization Formats:**
   - List the advantages and disadvantages of JSON, XML, CSV, Thrift, and Protocol Buffers.
   - For each format, identify scenarios in high-load systems where it would be the most appropriate choice.

   | Format               | Advantages                                                                                                                          | Disadvantages                                                                                                      | Best high-load scenario                                                                                                                                                                                                               |
   | -------------------- | ----------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
   | **JSON**             | Human-readable text; native browser support; schemaless/flexible; self-describing                                                   | Verbose (large payloads); no native binary/schema; parsing slower than binary; no strong typing; ambiguous numbers | Public/partner-facing REST and web/mobile APIs, webhooks, and event envelopes (e.g. CloudEvents); document stores and config where schema flexibility and human-readability outweigh raw density                                   |
   | **XML**              | Self-describing; rich schema/validation (XSD), namespaces, XSLT; mature enterprise tooling                                          | Most verbose; heavy parsing (DOM); poor density at scale; declining adoption                                       | Enterprise/regulated B2B and document workflows (SOAP, ISO 20022/SWIFT, HL7 CDA, SAML) needing XSD validation, namespaces, and XSLT transforms                                                                                                |
   | **CSV**              | Minimal overhead; trivially streamable row-by-row; universal spreadsheet/analytics ingest                                           | Flat/tabular only; no types/schema; delimiter/encoding traps; no metadata; unreadable at scale                     | Bulk export/import and ETL into data warehouses and analytics; spreadsheet interchange and very large tabular datasets streamed row-by-row (billions of rows)                                                                                                      |
   | **Thrift**           | Compact binary; schema via IDL + multi-language codegen; bundles full RPC framework + pluggable transports/protocols; strong typing | Extra build/codegen step; not human-readable; smaller ecosystem than Protobuf/gRPC; framework lock-in              | High-throughput polyglot microservice RPC where one IDL must serve many languages and a bundled transport/protocol stack is wanted (its origin use case at Facebook)                                                                               |
   | **Protocol Buffers** | Very compact, fast binary; IDL + codegen; strong typing; explicit schema evolution (field tags); first-class gRPC pairing           | Not human-readable; requires `.proto` + codegen; debugging harder; less ad-hoc flexibility                         | High-volume internal service-to-service RPC (gRPC), low-latency mobile APIs, and telemetry/event-stream or message-queue payloads where wire size, CPU, and schema evolution dominate |

   **Rule of thumb:** text formats (JSON/XML/CSV) at system edges for interoperability and human inspection; binary schema formats (Protobuf/Thrift) on hot machine-to-machine paths for density, speed, and typed contracts. Binary belongs _on the wire_; at rest, prefer the store's native model (queryable JSON, typed columns) over opaque binary blobs.

#### Network IO (HTTP/2)

1. **HTTP/2 Improvements:**
   - Explain the key improvements HTTP/2 offers over HTTP/1.1. How do these improvements enhance the performance of high-load systems?
   - What are the potential challenges when migrating from HTTP/1.1 to HTTP/2 in a high-load environment?

   **Key improvements over HTTP/1.1**
   - **Multiplexed streams** over one TCP connection — concurrent request/response streams interleave, removing HTTP/1.1's application-layer head-of-line blocking and the 6-connections-per-origin browser workaround.
   - **Binary framing** — messages parse deterministically from length-prefixed frames instead of text: cheaper, less ambiguous.
   - **HPACK header compression** — a shared dynamic table avoids re-sending repetitive headers (cookies, auth, user-agent) per request; a large win for chatty, many-small-request workloads.
   - **Stream prioritization** — the client signals which resources matter most (CSS, above-the-fold).
   - **Flow control** — per-stream and connection windows stop a fast sender overwhelming a slow consumer.
   - **Server push** — could proactively send sub-resources; _largely deprecated and removed from major browsers (Chrome) — prefer `103 Early Hints`/preload._

   **Why it helps under load:** fewer sockets/handshakes per client (less memory, lower TLS setup, better reuse) and lower per-request overhead (binary + HPACK) raise requests/sec per core; multiplexing + prioritization cut latency for **SSR pages that pull many sub-resources**; internal **gRPC** (HTTP/2-only) gets high throughput with minimal connection churn.

   **Migration challenges**
   - **Wire-incompatible protocols — negotiation, not a switch** — HTTP/2's binary framing is not compatible with HTTP/1.1's text format, so you can't just "turn it on." Client and server negotiate the version _per connection_, and you must keep serving HTTP/1.1 for clients, bots, and intermediaries that don't speak HTTP/2 (dual-stack with graceful fallback).
   - **TCP head-of-line blocking remains** — one lost segment stalls _all_ multiplexed streams; fixed only by **HTTP/3/QUIC**.
   - **L4 load-balancer imbalance** — long-lived HTTP/2 connections pin to one backend, so connection-level (L4) balancing spreads _connections_, not _requests_, and a few fat connections can hot-spot a backend replica. Prefer L7/request-aware balancing, tune `MaxConcurrentStreams`, and recycle connections.
   - **Every hop must speak HTTP/2** — CDN/edge, origin LB, app server, mesh; any hop that only understands HTTP/1.1 silently downgrades the connection.
   - **Operational retuning** — old HTTP/1.1 tricks (6-connection limits, domain sharding, sprite sheets) become anti-patterns.
   - **Debuggability** — binary framing needs protocol-aware tooling (Wireshark HTTP/2, devtools, curl `--http2`).

2. **Efficient Network Communication:**
   - Discuss the benefits and trade-offs of using HTTP/2 multiplexing and server push features in high-load systems.
   - How does the binary framing layer in HTTP/2 contribute to network efficiency?

   **Multiplexing**
   - _Benefit — concurrency over one connection:_ many independent **streams** interleave over a single TCP connection, so thousands of concurrent gRPC calls or SSR sub-resource fetches don't each need a socket — fewer handshakes, file descriptors, and warm connections; ideal for high-fan-out service-to-service traffic and long-lived bidirectional **gRPC streaming**.
   - _Trade-off — TCP head-of-line blocking persists:_ multiplexing only removes blocking _above_ TCP; a single lost packet stalls every stream until retransmit. Fixed only by **HTTP/3 (QUIC)**.
   - _Trade-off — load balancing:_ one sticky long-lived connection pins all streams to one backend, defeating naive **L4 balancing**; needs L7/gRPC-aware balancing.
   - _Trade-off — prioritization:_ HTTP/2 stream prioritization is complex and inconsistently implemented, so "important streams first" is unreliable in practice.

   **Server push** — effectively dead: **deprecated and removed** from major browsers. It failed because the server can't know the client's cache state and routinely **re-pushes already-cached resources, wasting bandwidth**. Replacement: **HTTP 103 Early Hints + `Link: rel=preload`** — the server hints, the _client_ decides, respecting its own cache; the right lever for edge/CDN-fronted SSR.

   **Binary framing layer** — HTTP/2 encodes everything as length-prefixed **binary frames** (`HEADERS`, `DATA`, `SETTINGS`, `WINDOW_UPDATE`…), each tagged with a stream ID. This gives **deterministic, cheaper parsing** (no ambiguous text scanning, no request-smuggling whitespace), lets independently sized frames **interleave** on one connection (the mechanism that _enables_ multiplexing), carries **HPACK** in the `HEADERS` frame, and provides frame-level **flow control** and clean message boundaries for reliable streaming.

#### Storage IO (HDD, SSD, NVMe, RAID)

1. **Storage Performance Comparison:**
   - Compare and contrast the performance characteristics of HDD, SSD, NVMe, and different RAID configurations.
   - In a high-load system, what are the key factors to consider when choosing between NVMe, SSDs, and HDDs for data storage?

   **Storage media**

   | Aspect          | HDD (SATA/SAS)                           | SATA/SAS SSD                            | NVMe SSD                                       |
   | --------------- | ---------------------------------------- | --------------------------------------- | ---------------------------------------------- |
   | Interface       | SATA/SAS, spinning platters              | SATA/SAS AHCI (single queue, ~32 depth) | PCIe lanes, NVMe (64K queues × 64K depth)      |
   | Random IOPS     | ~100–200                                 | ~10k–100k                               | ~100k–1M+                                      |
   | Latency         | ~5–15 ms (seek + rotation)               | sub-ms (~0.1–0.5 ms)                    | ~10–100 µs                                     |
   | Seq. throughput | ~100–250 MB/s                            | ~500–550 MB/s (SATA-capped)             | ~2–7+ GB/s                                     |
   | Cost / GB       | Lowest                                   | Mid                                     | Highest                                        |
   | Best use        | Cold/archival, large sequential, backups | General hot data, mixed workloads       | Latency-critical, high-IOPS transactional/OLTP |

   HDD bottlenecks on mechanical seek (random I/O kills it); SATA SSD removes seek but is throttled by the AHCI single-queue bus; NVMe exploits PCIe parallelism and deep multi-queue I/O, dominating at high concurrency/random access — at a cost and endurance premium.

   **RAID levels**

   | Level   | Layout                 | Performance                                                       | Redundancy                        | Usable capacity |
   | ------- | ---------------------- | ----------------------------------------------------------------- | --------------------------------- | --------------- |
   | RAID 0  | Striping               | Max read+write, max capacity                                      | None (any disk loss = total loss) | 100%            |
   | RAID 1  | Mirroring              | Read scales, write = single disk                                  | Survives 1 disk                   | 50%             |
   | RAID 5  | Stripe + single parity | Good read; write penalty (parity read-modify-write), slow rebuild | Survives 1 disk                   | (N−1)/N         |
   | RAID 6  | Stripe + double parity | Higher write penalty than 5                                       | Survives 2 disks                  | (N−2)/N         |
   | RAID 10 | Mirror + stripe        | Best balance: high R/W + fast rebuild                             | Survives 1 per mirror             | 50%             |

   **Key selection factors:** IOPS/latency/throughput targets; read-vs-write and random-vs-sequential mix; cost/GB and capacity; and data temperature — hot transactional → NVMe/SSD, cold/archival/large-sequential (log archives, backups) → HDD.

   **Reliability (distinct failure profiles, not just speed):** **HDDs** wear mechanically and are shock/vibration-sensitive but fail _gradually_ (often flagged by SMART) and have no write-cycle limit; **SSD/NVMe** have no moving parts (shock-resistant) but flash **endurance (DWPD/TBW)** is consumed by write-heavy load, they can fail _abruptly_ (controller death), and lose data retention if left unpowered — poor for offline cold archive. Weigh **MTBF/AFR** and **UBER**, prefer enterprise drives with **power-loss protection (PLP)**, and remember single-drive reliability is ultimately backstopped by **RAID/replication**, not the drive itself.

2. **Scaling Storage Solutions:**
   - How can RAID configurations be used to scale storage performance and reliability in high-load systems?
   - Discuss the implications of storage IO bottlenecks in high-load systems and how they can be mitigated.

   **RAID for performance + reliability**
   - **RAID 0 (stripe):** IOPS and throughput scale ~linearly with device count; zero redundancy — pure performance play.
   - **RAID 1 (mirror):** read scaling + survives a disk loss, at 50% capacity cost.
   - **RAID 5/6 (parity):** capacity-efficient redundancy but a **write penalty** (~4 I/O per write for RAID 5, ~6 for RAID 6) and risky long rebuilds that stress survivors.
   - **RAID 10 (stripe of mirrors):** the standard for write-heavy databases — striping throughput + mirror redundancy, no parity penalty, fast rebuilds; best fit for burst-sensitive, write-heavy transactional stores.
   - **Caveats:** RAID is **availability, not backup** (still need replication + PITR); a **battery-backed write cache (BBU/NVRAM)** absorbs write bursts; the rebuild window is degraded and vulnerable. Cloud-managed storage abstracts these mechanics (managed disks + replicated storage), but the same striping-for-throughput / replication-for-durability principles still govern provisioning.

   **IO-bottleneck implications:** under heavy write bursts, saturation surfaces as **rising latency**, **queue-depth buildup**, **throttling** (e.g. database `429` / throughput-limit errors), **backpressure cascading upstream**, **dropped or delayed work**, and **write-loss risk** for append-heavy logs.

   **Mitigations**
   - **Buffer the burst:** a queue in front of the write-hot store absorbs the spike so consumers drain writes at a sustainable rate.
   - **Batch/bulk writes** to cut per-op overhead and cost.
   - **Autoscale throughput** to ride peaks without permanent over-provisioning.
   - **Partition/shard** by a high-cardinality key so no hot partition serializes the burst.
   - **Offload reads:** a secondary-region read replica + a distributed cache for hot point reads + CDN — keeps read IO off the write-hot primary.
   - **Async/write-behind:** decouple acknowledgment from durable persistence.
   - **Right model:** append-only, LSM/wide-column stores with **TTL** to expire cold logs; hot/cold tiering + compression to cut bytes written per operation.

#### Serialization and Compression

1. **Efficient Serialization:**
   - What are the performance considerations when choosing a serialization format (e.g., JSON, XML, Thrift, Protocol Buffers) for a high-load system?
   - Provide examples of how efficient serialization can reduce network latency and improve throughput in high-load systems.

   **Why it matters at scale:** at high message rates and write bursts, per-message byte size and (de)serialization CPU are multiplied across every hop (producer → broker → workers → datastore), so small per-message savings become large savings in bandwidth, CPU, per-request cost, and tail latency.

   **Performance considerations**
   - **Wire size:** binary (Protobuf/Thrift/Avro) encodes by tag number with varint/packed bytes; text (JSON/XML) repeats every field _name_ per record. Smaller bytes = less bandwidth, more messages per packet, lower latency.
   - **CPU & allocations:** text parsing tokenizes/UTF-8-scans and allocates many transient strings (GC pressure); binary parsing is mostly length-prefixed buffer reads — higher throughput, lower tail latency, smoother GC pauses.
   - **Schema vs schemaless:** schema formats omit field names from the wire, generate typed code, and support **evolution** via stable tag numbers; schemaless (JSON) is flexible but pays repeated keys and ad-hoc versioning.
   - **Streaming vs full-document:** length-delimited binary / gRPC streaming allow incremental reads; JSON/XML usually parse the whole document (higher latency + peak memory on large record batches).
   - **Readability/interop:** JSON is universal and debuggable in logs/CloudEvents; Protobuf needs a shared `.proto` and tooling (`protoc --decode`).

   **Concrete impact**
   - **Smaller payloads → lower latency/bandwidth:** Protobuf is commonly **~3–10× smaller** than equivalent JSON; encoding messages with Protobuf fits more per packet and per broker message.
   - **Cheaper parse → higher throughput, lower tail latency:** binary decode costs a fraction of JSON parse, so freed CPU during traffic spikes cuts queue buildup and p99 latency.
   - **No repeated field names:** a JSON message repeats its field names (`"id"`, `"status"`, `"timestamp"`…) on _every_ record across millions of messages; Protobuf sends only tag numbers, shrinking wire size and decode CPU. (At rest this is moot: wide-column/columnar stores hold typed columns and document stores need queryable JSON — trim storage by keeping records lean and compressing large opaque fields, not by re-encoding items as binary.)

   **Where each format fits:** JSON at the edge (public/SSR/REST, CloudEvents — readability, interop, no shared schema); schema-based binary (Protobuf) as the wire format on hot internal/machine-to-machine paths to trim bytes and CPU at peak, paired with **gRPC** for synchronous internal RPC; at rest, keep document stores as lean JSON and let columnar/wide-column stores + compression carry bulk density. **Rule of thumb:** human-readable JSON where humans/3rd parties matter; schema-based binary where machine-to-machine volume, CPU, and bytes dominate.

2. **Compression Techniques:**
   - How does data compression impact network IO and overall system performance in high-load environments?
   - Compare different compression algorithms (e.g., gzip, snappy, zlib) in terms of their compression ratios and performance impacts on serialization and deserialization processes.

   **Impact — a CPU-vs-IO trade-off:** spend CPU to shrink payloads, buying back bandwidth, storage, and latency.
   - **Bytes on the wire** ↓ → lower bandwidth/latency for large payloads, more messages per connection, fewer broker throughput units / lower egress.
   - **Bytes at rest** ↓ → lower storage cost and lower per-request database cost (smaller items = cheaper writes/reads), shrinking large append-only datasets.
   - **Cost:** extra compress/decompress CPU plus added latency _on the hot path_.
   - **When it backfires:** **tiny messages** (per-message overhead exceeds savings — use a size threshold), **already-compressed/high-entropy data** (near-zero gain), and latency-critical small RPCs.
   - **Two layers:** transport-level (HTTP gzip/Brotli at the edge, gRPC per-message, broker payloads) and storage-level (database item compression, columnar/log-segment, cold-archive).

   **Algorithm comparison**

   | Algorithm                 | Compression ratio    | Compress speed         | Decompress speed | Typical use                                                     |
   | ------------------------- | -------------------- | ---------------------- | ---------------- | --------------------------------------------------------------- |
   | **Snappy**                | Low                  | Very fast              | Very fast        | Big-data/RPC hot paths (Cassandra, Kafka); throughput over size |
   | **LZ4**                   | Low (~Snappy)        | Very fast (fastest)    | Very fast        | Hot-path RPC, real-time log writes, in-memory caches            |
   | **gzip / zlib (DEFLATE)** | Medium-good          | Moderate               | Fast             | HTTP responses, general-purpose; ubiquitous baseline            |
   | **Zstd**                  | High, tunable (1–22) | Fast (level-dependent) | Very fast        | Modern default; fast at low levels, archive-grade at high       |
   | **Brotli**                | High (best on text)  | Slow at high levels    | Fast             | Static web/text/SSR HTTP at the CDN/edge               |

   > **gzip vs zlib:** same DEFLATE core; zlib is the library/raw-stream wrapper, gzip adds a file header/CRC — one family.

   Rough rankings — **ratio:** Brotli ~ Zstd(high) > gzip/zlib > Snappy ~ LZ4; **speed:** LZ4 ~ Snappy > Zstd > gzip > Brotli(high).

   **Guidance**
   - **Hot, latency-sensitive paths** (broker messages, live log/event writes under heavy bursts): **Snappy or LZ4** — cut bytes without meaningful latency or CPU stall.
   - **HTTP responses at the edge** (SSR pages, REST/JSON): let the **CDN/edge apply Brotli (static/text) or gzip** — high ratio, CPU amortized across cache hits.
   - **Cold archives** (large append-only logs/datasets): **Zstd-high or Brotli** when tiering to cold storage — maximize ratio since CPU is amortized over write-once/read-rarely data.
   - **Always** skip compression for tiny payloads (size threshold) and already-compressed/binary blobs.

   **Rule of thumb:** _fast-low-ratio on the hot path, high-ratio where CPU is amortized._
