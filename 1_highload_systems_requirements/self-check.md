# Self-check answers

Link: <https://git.epam.com/epmc-msft/net-mentoring-programs/-/blob/main/advanced+/01_understanding_high_load_asr/04-self-check.md>

---

## 1. Which scalability option is the primary choice for high-load systems?

**Answer:**  
Horizontal scalability is the primary choice for high-load systems.  
This is because, if horizontal scaling is properly configured (using load balancers, data locks, and automatic addition/removal of servers), the system can grow almost without limits to handle increasing user demand.

---

## 2. What are the main challenges of horizontal scalability?

**Answer:**

- Distributing traffic between multiple servers (usually using a load balancer)
- Data management: how to store data, access it, and guarantee consistency
- Storing session data: making servers stateless and using client-side storage for identity-related information
- System cost and complexity
- Deploying new releases: different versions may be incompatible, and cached data from older APIs can break newer versions
- Monitoring and observability

---

## 3. Name at least 3 hardware and software metrics to consider when designing a high-load system

**Answer:**

- Mean time to failure (MTTF)
- Mean time between failures (MTBF)
- Resource utilization (CPU, memory)
- Data usage and read/write rates
- Network latency
- Request rate
- Apdex score
- Throughput — number of transactions or requests an application can process within a given timeframe
- Garbage collection performance

---

## 4. Give examples of domains where high-load system design is required and not required

**Answer:**

**Required (high availability and traffic spikes):**

- e-commerce
- social networks
- messengers
- video streaming services
- payment systems
- search engines
- online games

**Not required (limited audience or scope):**

- Internal company tools (utilities, desktop apps, one-time scripts)
- Niche websites
- MVPs
- Software for specialized hardware (medical, military, government)
