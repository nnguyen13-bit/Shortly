# 01 — Functional Requirements

> **Cross-reference:** Business rules referenced by ID (e.g., BR-LNK-001) are defined in [Document 00](00-ExecutiveSummary-KeyRequirements.md).

---

## Requirement Summary by Phase

| Phase | Requirements | Scope |
|-------|-------------|-------|
| Phase 1 (MVP) | FR-LNK-001 to FR-LNK-005, FR-RDR-001 to FR-RDR-004, FR-DOM-001 to FR-DOM-003, FR-API-001, FR-INF-001 to FR-INF-003 | Link creation, redirect, domain registry, core API |
| Phase 2 (Production Hardening) | FR-SEC-001 to FR-SEC-003, FR-OBS-001 to FR-OBS-003, FR-ADM-001, FR-INF-004 to FR-INF-007, FR-IAC-001 to FR-IAC-005 | Security, observability, admin, resilience, IaC |
| Phase 3 (Analytics & Enhancements) | FR-ANL-001 to FR-ANL-003, FR-LNK-006, FR-ADM-002 | Analytics, bulk creation, reporting |

---

## 1. Link Management

### FR-LNK-001: Create Short Link

- **Priority:** P0
- **Phase:** 1
- **Description:** The system shall accept a destination URL and domain prefix, generate a unique short code, and return the complete short link.

**Detailed Requirements:**
- FR-LNK-001.1: The system shall validate the destination URL per BR-LNK-004 and BR-LNK-005
- FR-LNK-001.2: The system shall validate the domain prefix exists and is active per BR-LNK-007
- FR-LNK-001.3: The system shall generate a short code using counter-based Base62 encoding per BR-LNK-001 and BR-LNK-002
- FR-LNK-001.4: The system shall persist the link to the MongoDB `links` collection with status `Active`
- FR-LNK-001.5: The system shall return the short link URL, short code, domain prefix, and link ID
- FR-LNK-001.6: The system shall optionally accept an `expiresAt` timestamp per BR-LNK-006
- FR-LNK-001.7: The system shall optionally accept metadata tags per BR-LNK-008
- FR-LNK-001.8: The system shall publish a `LinkCreated` domain event

**Acceptance Criteria:**
- Given a valid destination URL and active domain prefix, when a create link request is made, then the system returns a short link with a 5-character Base62 code
- Given an invalid URL, when a create link request is made, then the system returns HTTP 400 with a validation error

**Dependencies:** FR-DOM-001 (domain must be registered first)

---

### FR-LNK-002: Disable Link

- **Priority:** P0
- **Phase:** 1
- **Description:** The system shall allow disabling an active link, preventing future redirects.

**Detailed Requirements:**
- FR-LNK-002.1: The system shall set the link status to `Disabled`
- FR-LNK-002.2: The system shall publish a `LinkDisabled` domain event
- FR-LNK-002.3: Disabled links shall return HTTP 410 per BR-LFC-002

**Acceptance Criteria:**
- Given an active link, when a disable request is made, then the link status becomes `Disabled` and subsequent redirects return HTTP 410

**Dependencies:** FR-LNK-001

---

### FR-LNK-003: Expire Links Automatically

- **Priority:** P1
- **Phase:** 1
- **Description:** The system shall automatically expire links that pass their `expiresAt` timestamp.

**Detailed Requirements:**
- FR-LNK-003.1: MongoDB TTL index on `expiresAt` shall delete expired link documents per BR-LFC-004
- FR-LNK-003.2: Expired links queried before TTL cleanup shall return HTTP 410 per BR-LFC-001
- FR-LNK-003.3: The system shall publish a `LinkExpired` event when detecting an expired link

**Acceptance Criteria:**
- Given a link with `expiresAt` in the past, when a redirect is requested, then the system returns HTTP 410 Gone

**Dependencies:** FR-LNK-001

---

### FR-LNK-004: Get Link Details

- **Priority:** P1
- **Phase:** 1
- **Description:** The system shall allow retrieving a link's details by its ID or by domain prefix + short code.

**Detailed Requirements:**
- FR-LNK-004.1: The system shall return link ID, short code, domain prefix, destination URL, status, creation date, expiry date, and metadata
- FR-LNK-004.2: The system shall return HTTP 404 for non-existent links

**Acceptance Criteria:**
- Given an existing link, when queried by its short code and prefix, then the system returns the full link details

**Dependencies:** FR-LNK-001

---

### FR-LNK-005: Validate Link Uniqueness

- **Priority:** P0
- **Phase:** 1
- **Description:** The system shall guarantee short code uniqueness within each domain prefix.

**Detailed Requirements:**
- FR-LNK-005.1: The MongoDB unique compound index `{ domainPrefix, shortCode }` shall enforce uniqueness at the database level per BR-LNK-001
- FR-LNK-005.2: Counter-based generation shall eliminate collisions by design
- FR-LNK-005.3: On the unlikely event of a duplicate key error, the system shall retry with the next counter value

**Acceptance Criteria:**
- Given 100,000 concurrent link creation requests for the same prefix, when all complete, then zero duplicate short codes exist

**Dependencies:** None

---

### FR-LNK-006: Bulk Create Links

- **Priority:** P2
- **Phase:** 3
- **Description:** The system shall accept a batch of link creation requests in a single API call.

**Detailed Requirements:**
- FR-LNK-006.1: The system shall accept up to 100 links per batch request
- FR-LNK-006.2: The system shall return results for each link (success or failure) in the response
- FR-LNK-006.3: Partial failures shall not roll back successful creates

**Acceptance Criteria:**
- Given a batch of 50 valid links, when submitted, then all 50 are created and returned with their short links

**Dependencies:** FR-LNK-001

---

## 2. Redirect

### FR-RDR-001: Resolve and Redirect

- **Priority:** P0
- **Phase:** 1
- **Description:** The system shall resolve a short link path (`/{prefix}/{code}`) to a destination URL and return an HTTP redirect.

**Detailed Requirements:**
- FR-RDR-001.1: The system shall query MongoDB `links` collection using the compound index `{ domainPrefix, shortCode }`
- FR-RDR-001.2: For active, non-expired links, the system shall return HTTP 301 per BR-RDR-001
- FR-RDR-001.3: The `Location` header shall contain the destination URL
- FR-RDR-001.4: The entire redirect operation shall complete in <100ms at p99 per BR-RDR-002

**Acceptance Criteria:**
- Given an active short link `ho/a3Bx9`, when a GET request is made, then the system returns HTTP 301 with the correct destination URL in the Location header

**Dependencies:** FR-LNK-001

---

### FR-RDR-002: Handle Missing Links

- **Priority:** P0
- **Phase:** 1
- **Description:** The system shall return HTTP 404 for short codes that do not exist.

**Detailed Requirements:**
- FR-RDR-002.1: Unknown prefix + code combinations shall return HTTP 404 per BR-RDR-003
- FR-RDR-002.2: The response body shall contain a brief error message (not stack traces)

**Acceptance Criteria:**
- Given a non-existent short code, when a redirect is requested, then the system returns HTTP 404

**Dependencies:** None

---

### FR-RDR-003: Handle Expired and Disabled Links

- **Priority:** P0
- **Phase:** 1
- **Description:** The system shall return HTTP 410 for expired or disabled links.

**Detailed Requirements:**
- FR-RDR-003.1: Links with status `Expired` or `Disabled` shall return HTTP 410 per BR-LFC-001 and BR-LFC-002
- FR-RDR-003.2: Links with `expiresAt` in the past (even if status not yet updated) shall return HTTP 410

**Acceptance Criteria:**
- Given a disabled link, when a redirect is requested, then the system returns HTTP 410 Gone

**Dependencies:** FR-LNK-002, FR-LNK-003

---

### FR-RDR-004: Publish Redirect Events

- **Priority:** P1
- **Phase:** 1
- **Description:** The system shall publish a `LinkRedirected` event after each successful redirect.

**Detailed Requirements:**
- FR-RDR-004.1: Events shall be published asynchronously (fire-and-forget) per BR-RDR-004
- FR-RDR-004.2: Event publishing failure shall NOT impact the redirect response
- FR-RDR-004.3: Events shall be published to Azure Service Bus

**Acceptance Criteria:**
- Given a successful redirect, when the response is sent, then a `LinkRedirected` event is published without adding latency to the redirect

**Dependencies:** FR-RDR-001

---

## 3. Domain Registry

### FR-DOM-001: Register Domain Prefix

- **Priority:** P0
- **Phase:** 1
- **Description:** The system shall allow registering a new business domain prefix.

**Detailed Requirements:**
- FR-DOM-001.1: The prefix shall be validated per BR-DOM-001 (unique) and BR-LNK-003 (2–4 lowercase alphanumeric)
- FR-DOM-001.2: The system shall store the domain in MongoDB `domains` collection
- FR-DOM-001.3: The system shall publish a `DomainRegistered` event
- FR-DOM-001.4: Registration shall require a human-readable name and optional description

**Acceptance Criteria:**
- Given a unique prefix `ho` with name "Handover Order", when registered, then the domain is active and available for link creation

**Dependencies:** None

---

### FR-DOM-002: List Domains

- **Priority:** P1
- **Phase:** 1
- **Description:** The system shall allow listing all registered domain prefixes.

**Detailed Requirements:**
- FR-DOM-002.1: The response shall include prefix, name, description, active status, and creation date
- FR-DOM-002.2: Results shall support filtering by active status

**Acceptance Criteria:**
- Given 5 registered domains (3 active, 2 inactive), when listing with filter `active=true`, then 3 domains are returned

**Dependencies:** FR-DOM-001

---

### FR-DOM-003: Deactivate Domain

- **Priority:** P1
- **Phase:** 1
- **Description:** The system shall allow deactivating a domain prefix, preventing new link creation.

**Detailed Requirements:**
- FR-DOM-003.1: The system shall set `isActive` to `false` per BR-DOM-003
- FR-DOM-003.2: The system shall publish a `DomainDeactivated` event
- FR-DOM-003.3: Existing links under the prefix shall continue to redirect (not affected)
- FR-DOM-003.4: A warning shall be returned if active links exist per BR-DOM-004

**Acceptance Criteria:**
- Given an active domain with existing links, when deactivated, then new link creation for that prefix fails but existing links still redirect

**Dependencies:** FR-DOM-001

---

## 4. API Design

### FR-API-001: RESTful API

- **Priority:** P0
- **Phase:** 1
- **Description:** The system shall expose a RESTful API for all link management and domain registry operations.

**Detailed Requirements:**
- FR-API-001.1: Endpoints:
  - `POST /api/links` — Create link (FR-LNK-001)
  - `GET /api/links/{prefix}/{code}` — Get link details (FR-LNK-004)
  - `DELETE /api/links/{prefix}/{code}` — Disable link (FR-LNK-002)
  - `POST /api/domains` — Register domain (FR-DOM-001)
  - `GET /api/domains` — List domains (FR-DOM-002)
  - `DELETE /api/domains/{prefix}` — Deactivate domain (FR-DOM-003)
  - `GET /{prefix}/{code}` — Redirect (FR-RDR-001)
- FR-API-001.2: All management endpoints shall return JSON
- FR-API-001.3: The redirect endpoint shall be on a separate route (no `/api/` prefix)
- FR-API-001.4: API shall return standard HTTP status codes and error response bodies
- FR-API-001.5: API shall include OpenAPI/Swagger documentation

**Acceptance Criteria:**
- Given the API is running, when `GET /swagger` is requested, then the OpenAPI specification is returned

**Dependencies:** All FR-LNK and FR-DOM requirements

---

## 5. Security

### FR-SEC-001: API Authentication

- **Priority:** P0
- **Phase:** 2
- **Description:** The system shall authenticate all management API requests (link creation, domain management).

**Detailed Requirements:**
- FR-SEC-001.1: Management endpoints (`/api/*`) shall require an API key or OAuth2 bearer token
- FR-SEC-001.2: The redirect endpoint (`/{prefix}/{code}`) shall NOT require authentication (public)
- FR-SEC-001.3: Invalid or missing credentials shall return HTTP 401

**Acceptance Criteria:**
- Given a request to `POST /api/links` without an API key, when processed, then the system returns HTTP 401

**Dependencies:** FR-API-001

---

### FR-SEC-002: Input Validation

- **Priority:** P0
- **Phase:** 2
- **Description:** The system shall validate all inputs to prevent injection and abuse.

**Detailed Requirements:**
- FR-SEC-002.1: Destination URLs shall be validated against BR-LNK-004 and BR-LNK-005
- FR-SEC-002.2: All string inputs shall be sanitised (no script injection)
- FR-SEC-002.3: Request body size shall be limited to 64KB

**Acceptance Criteria:**
- Given a destination URL with `javascript:alert(1)`, when a create request is made, then the system returns HTTP 400

**Dependencies:** FR-LNK-001

---

### FR-SEC-003: Rate Limiting

- **Priority:** P1
- **Phase:** 2
- **Description:** The system shall enforce rate limits on link creation to prevent abuse.

**Detailed Requirements:**
- FR-SEC-003.1: Rate limits shall be configurable per API key
- FR-SEC-003.2: Exceeded rate limits shall return HTTP 429 with `Retry-After` header
- FR-SEC-003.3: Default rate limit: 100 requests/second per API key

**Acceptance Criteria:**
- Given a client exceeding 100 req/s, when the next request arrives, then the system returns HTTP 429

**Dependencies:** FR-SEC-001

---

## 6. Observability

### FR-OBS-001: Health Checks

- **Priority:** P0
- **Phase:** 2
- **Description:** The system shall expose health check endpoints for container orchestration.

**Detailed Requirements:**
- FR-OBS-001.1: `GET /health/live` — Liveness probe (is the process running?)
- FR-OBS-001.2: `GET /health/ready` — Readiness probe (can it accept traffic? checks MongoDB connection)

**Acceptance Criteria:**
- Given MongoDB is unavailable, when readiness is checked, then the system returns HTTP 503

**Dependencies:** None

---

### FR-OBS-002: Structured Logging

- **Priority:** P1
- **Phase:** 2
- **Description:** The system shall produce structured JSON logs for all operations.

**Detailed Requirements:**
- FR-OBS-002.1: Logs shall include correlation ID, timestamp, level, service name, and operation context
- FR-OBS-002.2: Link creation and redirect operations shall be logged at INFO level
- FR-OBS-002.3: Errors shall be logged at ERROR level with stack traces

**Acceptance Criteria:**
- Given a link creation request, when processed, then a structured JSON log entry is emitted with the link ID and short code

**Dependencies:** None

---

### FR-OBS-003: Metrics

- **Priority:** P1
- **Phase:** 2
- **Description:** The system shall expose application metrics for monitoring.

**Detailed Requirements:**
- FR-OBS-003.1: Expose Prometheus-compatible metrics endpoint
- FR-OBS-003.2: Metrics: requests/sec, redirect latency histogram, error rate, MongoDB query duration

**Acceptance Criteria:**
- Given the metrics endpoint is scraped, then redirect latency p50/p95/p99 values are available

**Dependencies:** None

---

## 7. Infrastructure

### FR-INF-001: MongoDB Connection Management

- **Priority:** P0
- **Phase:** 1
- **Description:** The system shall manage MongoDB connections with appropriate resilience.

**Detailed Requirements:**
- FR-INF-001.1: Connection string shall be configurable via environment variable or Azure Key Vault
- FR-INF-001.2: Connection pooling shall be enabled with configurable pool size
- FR-INF-001.3: Read and write concerns shall be configurable per operation

**Acceptance Criteria:**
- Given the MongoDB connection string is provided via environment variable, when the service starts, then it connects successfully

**Dependencies:** None

---

### FR-INF-002: MongoDB Index Management

- **Priority:** P0
- **Phase:** 1
- **Description:** The system shall ensure required indexes exist on startup.

**Detailed Requirements:**
- FR-INF-002.1: Unique compound index `{ domainPrefix: 1, shortCode: 1 }` on `links`
- FR-INF-002.2: Unique index `{ prefix: 1 }` on `domains`
- FR-INF-002.3: TTL index on `{ expiresAt: 1 }` on `links`
- FR-INF-002.4: Secondary indexes for admin queries per data model specification

**Acceptance Criteria:**
- Given the service starts fresh against an empty database, when startup completes, then all required indexes exist

**Dependencies:** None

---

### FR-INF-003: Azure Service Bus Integration

- **Priority:** P1
- **Phase:** 1
- **Description:** The system shall publish domain events to Azure Service Bus.

**Detailed Requirements:**
- FR-INF-003.1: Events shall be published to Service Bus topics
- FR-INF-003.2: Publishing shall be asynchronous and non-blocking
- FR-INF-003.3: Failed publishes shall be logged but not fail the parent operation

**Acceptance Criteria:**
- Given a link is created, when the `LinkCreated` event is published, then it appears on the Service Bus topic within 5 seconds

**Dependencies:** FR-LNK-001

---

### FR-INF-004: Resilience Patterns

- **Priority:** P1
- **Phase:** 2
- **Description:** The system shall implement resilience patterns for external dependencies.

**Detailed Requirements:**
- FR-INF-004.1: Circuit breaker on MongoDB connections (open after 5 consecutive failures)
- FR-INF-004.2: Retry with exponential backoff for transient failures
- FR-INF-004.3: Timeout of 5 seconds for MongoDB operations

**Acceptance Criteria:**
- Given MongoDB is temporarily unavailable, when 5 consecutive requests fail, then the circuit breaker opens and subsequent requests fail fast

**Dependencies:** FR-INF-001

---

### FR-INF-005: Redis Cache for Redirects

- **Priority:** P1
- **Phase:** 2
- **Description:** The system shall cache frequently accessed links in Redis to reduce MongoDB read load on the redirect hot path.

**Detailed Requirements:**
- FR-INF-005.1: Cache link lookups (prefix + short code → destination URL) in Redis with a configurable TTL (default: 5 minutes)
- FR-INF-005.2: Invalidate cache entries when a link is disabled or expires
- FR-INF-005.3: Fall back to MongoDB on cache miss (cache-aside pattern)
- FR-INF-005.4: Redis connection failure shall not block redirects — fall back to direct MongoDB reads
- FR-INF-005.5: Cache hit/miss ratio shall be exposed via metrics (FR-OBS-003)

**Acceptance Criteria:**
- Given a link has been redirected once, when the same link is redirected again within the TTL, then the response is served from cache without a MongoDB query
- Given Redis is unavailable, when a redirect is requested, then the system falls back to MongoDB and the redirect succeeds
- Given a link is disabled, when the cache entry exists, then the cache entry is invalidated immediately

**Dependencies:** FR-INF-001, FR-RDR-001

---

### FR-INF-006: Horizontal Scaling

- **Priority:** P1
- **Phase:** 2
- **Description:** The system shall support running multiple API instances behind a load balancer for horizontal scalability.

**Detailed Requirements:**
- FR-INF-006.1: The API shall be stateless — no in-process session or memory-dependent state
- FR-INF-006.2: The short code generator shall support concurrent instances without producing duplicate codes (range pre-allocation per instance)
- FR-INF-006.3: Health check endpoints shall be compatible with Azure Container Apps or Kubernetes probes
- FR-INF-006.4: Docker Compose shall support scaling the API service (`docker compose up --scale api=N`)
- FR-INF-006.5: The system shall handle at least 1,000 concurrent redirect requests across multiple instances

**Acceptance Criteria:**
- Given 3 API instances are running, when 1,000 concurrent redirect requests are issued, then all requests are served with <100ms p99 latency
- Given 3 API instances are running, when short codes are generated concurrently, then no duplicate codes are produced

**Dependencies:** FR-INF-001, FR-OBS-001

---

### FR-INF-007: MongoDB Replica Set with Read Preference

- **Priority:** P1
- **Phase:** 2
- **Description:** The system shall use a MongoDB replica set with read preference configuration to distribute read load to secondary replicas.

**Detailed Requirements:**
- FR-INF-007.1: Configure MongoDB connection string for replica set topology
- FR-INF-007.2: Redirect reads (hot path) shall use `ReadPreference.SecondaryPreferred` to offload the primary
- FR-INF-007.3: Write operations (link creation, disable, domain registration) shall use `WriteConcern.WMajority` for durability
- FR-INF-007.4: Short code counter operations shall use `ReadPreference.Primary` to ensure consistency
- FR-INF-007.5: Docker Compose shall include a 3-node MongoDB replica set for local development

**Acceptance Criteria:**
- Given a replica set with 1 primary and 2 secondaries, when redirect requests are issued, then reads are distributed across secondaries
- Given the primary fails, when a secondary is elected, then write operations resume within 10 seconds

**Dependencies:** FR-INF-001, FR-INF-004

---

## 8. Analytics (Future)

### FR-ANL-001: Track Redirect Counts

- **Priority:** P2
- **Phase:** 3
- **Description:** The system shall track the total number of redirects per link.

**Dependencies:** FR-RDR-004

---

### FR-ANL-002: Query Link Statistics

- **Priority:** P2
- **Phase:** 3
- **Description:** The system shall expose an API to query click counts and redirect statistics per link.

**Dependencies:** FR-ANL-001

---

### FR-ANL-003: Dashboard API

- **Priority:** P3
- **Phase:** 3
- **Description:** The system shall provide aggregate statistics (total links, total redirects, top domains) via API.

**Dependencies:** FR-ANL-001, FR-ANL-002

---

## 9. Administration

### FR-ADM-001: Link Search

- **Priority:** P1
- **Phase:** 2
- **Description:** The system shall allow searching links by status, creator, or domain prefix.

**Detailed Requirements:**
- FR-ADM-001.1: Support filtering by `status`, `createdBy`, `domainPrefix`
- FR-ADM-001.2: Support pagination (cursor-based or offset)
- FR-ADM-001.3: Results sorted by `createdAt` descending

**Acceptance Criteria:**
- Given 1000 links across 3 prefixes, when filtering by prefix `ho`, then only links with prefix `ho` are returned

**Dependencies:** FR-LNK-001

---

## 10. Infrastructure as Code

### FR-IAC-001: Terraform Project Structure

- **Priority:** P1
- **Phase:** 2
- **Description:** The system shall define all Azure infrastructure as code using Terraform, enabling repeatable, auditable, and version-controlled deployments.

**Detailed Requirements:**
- FR-IAC-001.1: Terraform configuration shall use a modular structure with reusable modules per resource type
- FR-IAC-001.2: Remote state shall be stored in Azure Storage Account with state locking
- FR-IAC-001.3: Environment-specific configuration shall be managed via `.tfvars` files (dev, staging, prod)
- FR-IAC-001.4: All resources shall be tagged with `project`, `environment`, and `managed-by` tags
- FR-IAC-001.5: Terraform version and provider versions shall be pinned

**Acceptance Criteria:**
- Given a clean Azure subscription, when `terraform apply` is run with dev variables, then all required resources are provisioned
- Given an existing deployment, when `terraform plan` is run with no changes, then the plan shows no modifications

**Dependencies:** None

---

### FR-IAC-002: Azure Container Apps Deployment

- **Priority:** P1
- **Phase:** 2
- **Description:** The system shall deploy the Shortly API as an Azure Container App with auto-scaling and ingress configuration.

**Detailed Requirements:**
- FR-IAC-002.1: Provision Azure Container Apps Environment with a Log Analytics workspace
- FR-IAC-002.2: Deploy the API container with configurable CPU/memory limits and min/max replicas
- FR-IAC-002.3: Configure external ingress on port 8080 with HTTPS
- FR-IAC-002.4: Configure health probes using `/health/live` (liveness) and `/health/ready` (readiness)
- FR-IAC-002.5: Inject application settings (MongoDB connection string, Service Bus connection string) from Key Vault references
- FR-IAC-002.6: Configure auto-scaling rules based on HTTP concurrent requests (default: scale at 50 concurrent requests per instance)

**Acceptance Criteria:**
- Given the Terraform configuration is applied, when the container image is deployed, then the API is accessible via the Container Apps FQDN
- Given traffic exceeds the scaling threshold, when auto-scaling triggers, then additional replicas are created

**Dependencies:** FR-IAC-001, FR-IAC-004

---

### FR-IAC-003: Azure Service Bus Provisioning

- **Priority:** P1
- **Phase:** 2
- **Description:** The system shall provision Azure Service Bus namespace, topic, and subscription via Terraform.

**Detailed Requirements:**
- FR-IAC-003.1: Provision Service Bus namespace with Standard SKU (required for topics)
- FR-IAC-003.2: Create `shortly-events` topic with 7-day message TTL and ordering support
- FR-IAC-003.3: Create `event-processor` subscription with dead-lettering on expiration
- FR-IAC-003.4: Create a Send-only authorisation rule for the application (least-privilege)
- FR-IAC-003.5: Store the Send-only connection string in Azure Key Vault

**Acceptance Criteria:**
- Given the Terraform configuration is applied, then the Service Bus namespace, topic, and subscription exist
- Given the application starts, when it reads the connection string from Key Vault, then it can publish events to the topic

**Dependencies:** FR-IAC-001, FR-IAC-004

---

### FR-IAC-004: Azure Key Vault for Secrets Management

- **Priority:** P1
- **Phase:** 2
- **Description:** The system shall provision Azure Key Vault to store and manage all application secrets.

**Detailed Requirements:**
- FR-IAC-004.1: Provision Azure Key Vault with RBAC-based access control
- FR-IAC-004.2: Store MongoDB connection string, Service Bus connection string, and API keys as Key Vault secrets
- FR-IAC-004.3: Grant the Container App managed identity `Key Vault Secrets User` role
- FR-IAC-004.4: Enable soft delete and purge protection for production environments

**Acceptance Criteria:**
- Given the Terraform configuration is applied, then the Key Vault exists with all required secrets
- Given the Container App starts, when it accesses Key Vault via managed identity, then it retrieves secrets without credentials in config

**Dependencies:** FR-IAC-001

---

### FR-IAC-005: Azure Container Registry

- **Priority:** P1
- **Phase:** 2
- **Description:** The system shall provision Azure Container Registry to store the Shortly API container image.

**Detailed Requirements:**
- FR-IAC-005.1: Provision Azure Container Registry with Basic SKU (upgradeable to Standard for geo-replication)
- FR-IAC-005.2: Grant the Container App managed identity `AcrPull` role for image pulls
- FR-IAC-005.3: Enable admin account only if required for CI/CD bootstrap; prefer managed identity

**Acceptance Criteria:**
- Given the Terraform configuration is applied, then the Container Registry exists
- Given a container image is pushed, when the Container App is deployed, then it pulls the image successfully via managed identity

**Dependencies:** FR-IAC-001
