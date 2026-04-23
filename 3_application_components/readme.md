## 1. Create Infrastructure Diagram for Solution

**Main Cloud Provider:** Azure

**Frontend Cloud Provider:** Cloudflare

### Components

| Component               | Service                     | Role                                                                                                 |
| ----------------------- | --------------------------- | ---------------------------------------------------------------------------------------------------- |
| Frontend                | Cloudflare                  | CDN, proxy, and edge security layer in front of the backend                                          |
| Authentication          | Azure B2C / Entra ID        | Managed identity provider handling user authentication and JWT issuance                              |
| Backend                 | Azure Web Apps              | ASP.NET MVC SSR application serving server-rendered HTML pages                                       |
| Jobs Scheduler          | Azure Container Apps        | Containerized service responsible for scheduling and dispatching job messages                        |
| Job Orchestrator/Runner | Azure Durable Functions     | Stateful workflow engine that orchestrates and executes job activities across integrations           |
| Integrations            | Azure Functions             | Individual serverless functions handling third-party integration calls triggered by the Orchestrator |
| Notification Queue      | Azure Service Bus           | Message broker decoupling the Scheduler from the Notification Manager                                |
| Notification Manager    | Azure Functions             | Event-driven function that consumes notification messages and dispatches them to users               |
| Database                | Azure Cosmos DB for MongoDB | Distributed document store for job metadata, status records, and notification rules                  |
| Identity & Access       | Azure Managed Identities    | Credential-free service-to-service authentication across all Azure components                        |
| Container Registry      | Azure Container Registry    | Private registry for storing and versioning container images used by Container Apps and Functions    |

---

## 2. Describe Communication Patterns Between Components

### Frontend to Backend

`Requester -> Cloudflare (Proxy) -> Azure B2C/Entra ID -> Backend SSR application`

The frontend communicates with the Azure Web App over HTTPS, receiving server-rendered HTML pages.
Cloudflare sits in front, handling caching, DDoS protection and routing requests to the Web App origin.
Authentication is handled separately via Azure B2C or Entra ID, with the resulting JWT token attached to each request.

### Azure Service to Service

All communication between Azure services is managed using Azure Managed Identities. Thanks to that there is no need to specify credentials in connection strings or hardcode IPs in virtual network rules.

### Backend to Database

All Azure backend apps (Web Apps, Container Apps, Functions) connect to Azure Cosmos DB for MongoDB using base connection strings.

### Job Management Service to Jobs Scheduler

The Job Management Service communicates with the Scheduler via synchronous HTTP calls.

### Jobs Scheduler to Job Orchestrator

The Jobs Scheduler publishes a job message to the Job Queue. The Job Orchestrator (Azure Durable Functions) consumes messages from the queue and passes execution to Integrations.

### Jobs Scheduler to Notification

Upon receiving a message with Job Execution Status, the Job Scheduler publishes a notification message to the Notification Queue (Azure Service Bus). The Azure Functions Notification Manager consumes from the queue and sends notifications to users according to rules from DB.

---

## 3. Provide Reasoning for Your Selection

| Service                      | Reasoning                                                                                                                                                                                                                                   |
| ---------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Cloudflare**               | Chosen for its strong and cheap security mechanisms (DDoS, WAF, bot protection) that Azure lacks natively at comparable cost. Provides global edge routing and caching as a bonus.                                                          |
| **Azure B2C / Entra ID**     | Chosen for compliance and security — handles MFA, token management, and audit logging out of the box, avoiding the high risk and cost of building custom auth.                                                                              |
| **Azure Web Apps**           | A long-running HTTP host better suited for an SSR application with persistent connections, middleware pipelines, and consistent latency. No cold-start concerns, natural fit for an MVC layer before handing off to the async job pipeline. |
| **Azure Container Apps**     | Chosen for scalability and cost-efficiency — scales to zero when idle and scales out under load without managing infrastructure, well suited for a scheduler with variable execution frequency.                                             |
| **Azure Durable Functions**  | Chosen for reliability — built-in state persistence, automatic retries, and fault tolerance ensure long-running jobs survive failures without custom checkpointing logic.                                                                   |
| **Azure Service Bus**        | Chosen for reliability — guaranteed at-least-once delivery and dead-lettering ensure no notification is silently lost, even under downstream failures.                                                                                      |
| **Azure Functions**          | Chosen for cost-efficiency — serverless pay-per-execution model eliminates idle compute cost for infrequent, short-lived notification and integration calls.                                                                                |
| **Azure Cosmos DB**          | Chosen for high availability (multi-region replication, 99.999% SLA) and horizontal scalability without schema migrations.                                                                                                                  |
| **Azure Managed Identities** | Chosen for security — credentials never exist in config or code, eliminating the risk of secret leakage and the operational burden of secret rotation.                                                                                      |
| **Azure Container Registry** | Chosen for security and maintainability — private image storage with built-in vulnerability scanning and geo-replication, keeping images close to compute for faster deployments.                                                           |

---

## 4. Provide Notes About Scalability Options

**Frontend (Cloudflare):**
Cloudflare automatically scales at the edge globally — no capacity planning needed.

**Authentication (Azure B2C / Entra ID):**
Fully managed service that scales transparently to millions of authentications without configuration.

**Backend (Azure Web Apps):**
Supports auto-scaling rules based on CPU, memory, or HTTP queue length. Scale-out adds instances behind the load balancer automatically. Scale-up allows moving to a higher App Service plan tier for more compute.

**Jobs Scheduler (Azure Container Apps):**
Scales out replicas based on custom KEDA scalers (e.g. queue depth, cron, HTTP traffic). Scales to zero when idle, eliminating cost during inactivity.

**Job Orchestrator/Runner (Azure Durable Functions):**
Scales out automatically based on the number of messages in the Job Queue. Each Durable Function instance processes activities in parallel, enabling fan-out across many concurrent jobs.

**Notification (Azure Service Bus + Azure Functions):**
Service Bus queues buffer spikes in notification volume. Azure Functions scale out instances based on queue depth, processing messages in parallel.

**Integrations (Azure Functions):**
Each integration Function scales independently based on its own trigger load, isolating scalability concerns per integration.

**Database (Azure Cosmos DB):**
Throughput can be scaled manually or with autoscale (RU/s). Partitioning allows horizontal scaling across physical nodes. Multi-region writes can be enabled for global write scalability.
