# 04 — Implementation Phases and Open Questions

> **Cross-reference:** Functional requirements (FR-*) are in [Document 01](01-Functional-Requirements.md). Acceptance criteria (TC-*) are in [Document 03](03-Acceptance-Criteria-TestScenarios.md).

---

## Phase Overview

| Phase | Name | Focus | Duration Estimate | Key Deliverables |
|-------|------|-------|-------------------|-----------------|
| 1 | MVP | Core link creation, redirect, domain registry | — | Working API, redirect service, MongoDB setup |
| 2 | Production Hardening | Security, observability, resilience | — | Auth, health checks, logging, rate limiting |
| 3 | Analytics & Enhancements | Usage analytics, bulk operations, reporting | — | Click tracking, bulk API, dashboard API |

---

## Phase 1: MVP

### Goal
Deliver a working URL shortening platform that can create links, redirect users, and manage domain prefixes. Internal services can integrate immediately.

### Requirements In Scope

| Requirement | Description | Priority |
|-------------|-------------|----------|
| FR-LNK-001 | Create short link | P0 |
| FR-LNK-002 | Disable link | P0 |
| FR-LNK-003 | Expire links automatically | P1 |
| FR-LNK-004 | Get link details | P1 |
| FR-LNK-005 | Link uniqueness guarantee | P0 |
| FR-RDR-001 | Resolve and redirect | P0 |
| FR-RDR-002 | Handle missing links (404) | P0 |
| FR-RDR-003 | Handle expired/disabled links (410) | P0 |
| FR-RDR-004 | Publish redirect events | P1 |
| FR-DOM-001 | Register domain prefix | P0 |
| FR-DOM-002 | List domains | P1 |
| FR-DOM-003 | Deactivate domain | P1 |
| FR-API-001 | RESTful API with OpenAPI docs | P0 |
| FR-INF-001 | MongoDB connection management | P0 |
| FR-INF-002 | MongoDB index management | P0 |
| FR-INF-003 | Azure Service Bus integration | P1 |

### Technical Tasks

#### 1.1 Solution Scaffolding
- Create .NET 10 solution with Clean Architecture layers
  - `Shortly.Domain` — Entities, value objects, domain events, interfaces
  - `Shortly.Application` — Use cases, commands, queries, handlers
  - `Shortly.Infrastructure` — MongoDB repositories, Service Bus publisher, code generator
  - `Shortly.Api` — Controllers, DTOs, middleware, OpenAPI
- Configure dependency injection
- Set up project references enforcing layer dependencies

#### 1.2 Domain Model
- Implement `Link` aggregate root with status transitions
- Implement value objects: `ShortCode`, `DomainPrefix`, `DestinationUrl`, `LinkMetadata`
- Implement `Domain` aggregate root
- Implement `LinkStatus` enum (`Active`, `Disabled`, `Expired`)
- Implement domain events: `LinkCreated`, `LinkDisabled`, `LinkExpired`
- Implement domain events: `DomainRegistered`, `DomainDeactivated`
- Unit tests for all domain logic and business rules

#### 1.3 Short Code Generation
- Implement `RangeBasedCodeGenerator` with counter range pre-allocation
- Implement Base62 encoding/decoding
- Implement MongoDB counter collection integration (`$inc` with batch size 1000)
- Unit tests for Base62 encoding
- Integration tests for counter atomicity under concurrency

#### 1.4 MongoDB Infrastructure
- Implement `ILinkRepository` and `MongoLinkRepository`
- Implement `IDomainRepository` and `MongoDomainRepository`
- Implement index creation on startup (compound unique, TTL, secondary)
- Configure connection pooling and read/write concerns
- Integration tests against a real MongoDB instance (Docker)

#### 1.5 API Layer
- Implement `LinksController` (POST, GET, DELETE)
- Implement `DomainsController` (POST, GET, DELETE)
- Implement `RedirectController` (GET `/{prefix}/{code}`)
- Configure OpenAPI/Swagger
- Request validation middleware
- Error handling middleware (domain exceptions → appropriate HTTP codes)
- API integration tests

#### 1.6 Event Publishing
- Implement `IEventPublisher` and `ServiceBusEventPublisher`
- Fire-and-forget pattern for redirect events
- Logging on publish failure (non-blocking)
- Integration test with Azure Service Bus emulator or in-memory fallback

#### 1.7 Docker and Local Development
- Dockerfile for the API service
- `docker-compose.yml` with MongoDB and the API
- Local development configuration (appsettings.Development.json)

### Exit Criteria
- All P0 test cases from Document 03 pass (TC-LNK-001, TC-RDR-001, TC-RDR-002, TC-DOM-001, TC-INF-001, TC-INF-003)
- Short code uniqueness validated under 1000 concurrent requests (TC-LNK-013)
- Redirect latency <100ms p99 with 10,000 links (TC-RDR-005)
- OpenAPI documentation accessible at `/swagger`
- Docker Compose setup runs end-to-end locally

---

## Phase 2: Production Hardening

### Goal
Add security, observability, and resilience required for production deployment.

### Requirements In Scope

| Requirement | Description | Priority |
|-------------|-------------|----------|
| FR-SEC-001 | API authentication | P0 |
| FR-SEC-002 | Input validation hardening | P0 |
| FR-SEC-003 | Rate limiting | P1 |
| FR-OBS-001 | Health check endpoints | P0 |
| FR-OBS-002 | Structured logging | P1 |
| FR-OBS-003 | Prometheus metrics | P1 |
| FR-INF-004 | Resilience patterns | P1 |
| FR-INF-005 | Redis cache for redirects | P1 |
| FR-INF-006 | Horizontal scaling | P1 |
| FR-INF-007 | MongoDB replica set | P1 |
| FR-IAC-001 | Terraform project structure | P1 |
| FR-IAC-002 | Azure Container Apps deployment | P1 |
| FR-IAC-003 | Azure Service Bus provisioning | P1 |
| FR-IAC-004 | Azure Key Vault for secrets | P1 |
| FR-IAC-005 | Azure Container Registry | P1 |
| FR-ADM-001 | Link search | P1 |

### Technical Tasks

#### 2.1 Authentication and Authorisation
- Implement API key authentication middleware for `/api/*` endpoints
- Ensure redirect endpoint (`/{prefix}/{code}`) is excluded from auth
- Store API keys securely (Azure Key Vault)
- Return 401 for missing/invalid credentials

#### 2.2 Input Validation Hardening
- Add request body size limit (64KB)
- Add input sanitisation for all string fields
- Add validation attributes to DTOs
- Security-focused integration tests

#### 2.3 Rate Limiting
- Implement rate limiting middleware (per API key)
- Configurable limits per consumer
- Return 429 with `Retry-After` header
- Default: 100 req/s per key

#### 2.4 Health Checks
- Implement `/health/live` (liveness)
- Implement `/health/ready` (readiness — checks MongoDB connectivity)
- Azure Container Apps probe configuration

#### 2.5 Structured Logging
- Configure Serilog with JSON output
- Add correlation ID middleware
- Log link creation and redirect at INFO level
- Log errors at ERROR level with context
- Ensure no sensitive data in logs

#### 2.6 Metrics
- Add Prometheus metrics endpoint
- Instrument: request rate, redirect latency histogram, error rate, MongoDB query duration
- Configure Azure Monitor integration

#### 2.7 Resilience
- Add Polly circuit breaker on MongoDB operations
- Add retry with exponential backoff for transient failures
- Add 5-second timeout on MongoDB operations
- Integration test circuit breaker behaviour

#### 2.8 Link Search API
- Implement `GET /api/links` with query parameters
- Support filtering by `status`, `createdBy`, `domainPrefix`
- Cursor-based pagination
- Sort by `createdAt` descending

#### 2.9 Redis Cache for Redirects
- Add `StackExchange.Redis` NuGet package
- Implement `ICacheService` interface in Application layer
- Implement `RedisCacheService` in Infrastructure layer (cache-aside pattern)
- Cache redirect lookups (prefix + code → destination URL) with configurable TTL (default 5 min)
- Invalidate cache on link disable/expiry via `LinkService`
- Fall back to MongoDB on Redis failure (resilient — redirect must not break)
- Add cache hit/miss metrics to Prometheus endpoint
- Integration tests with Redis Testcontainer

#### 2.10 Horizontal Scaling
- Verify API is fully stateless (no in-process state beyond DI singletons)
- Validate `RangeBasedCodeGenerator` supports concurrent instances (each instance pre-allocates its own counter range via atomic `$inc`)
- Update `docker-compose.yml` to support `--scale api=N` with an Nginx or Traefik load balancer
- Add load balancer health check routing to `/health/ready`
- Load test: 1,000 concurrent requests across 3 instances — verify no duplicate codes and <100ms p99 redirect latency

#### 2.11 MongoDB Replica Set
- Update `docker-compose.yml` with 3-node MongoDB replica set (1 primary + 2 secondaries)
- Add replica set initialisation script (`rs.initiate()`)
- Configure `MongoClientSettings` with `ReadPreference.SecondaryPreferred` for redirect reads
- Configure `WriteConcern.WMajority` for write operations
- Ensure counter collection (`$inc`) uses `ReadPreference.Primary`
- Update `MongoDbSettings` to support replica set connection strings
- Integration test: verify reads go to secondaries, writes go to primary

#### 2.12 Transactional Outbox for Event Reliability
- Implement outbox collection in MongoDB to store domain events alongside entity writes in the same transaction
- Background `OutboxProcessor` hosted service to poll and publish events to Service Bus
- Mark outbox entries as dispatched on successful publish
- Idempotent event processing (deduplication by event ID)
- Replace direct `PublishEventsAsync` calls with outbox writes
- Integration test: verify events are published even if the process restarts after the DB write

#### 2.13 Terraform Project Structure
- Initialise `infra/` directory with Terraform configuration
- Configure `azurerm` provider with version pinning
- Configure remote state backend (Azure Storage Account)
- Create `environments/` with `dev.tfvars`, `staging.tfvars`, `prod.tfvars`
- Define resource group, naming conventions, and standard tags (`project`, `environment`, `managed-by`)
- Add `.gitignore` entries for `.terraform/`, `*.tfstate`, `*.tfstate.backup`

#### 2.14 Azure Key Vault Module
- Create `modules/key-vault/` Terraform module
- Provision Key Vault with RBAC-based access control
- Enable soft delete and purge protection (configurable per environment)
- Output Key Vault ID and URI for use by other modules

#### 2.15 Azure Container Registry Module
- Create `modules/container-registry/` Terraform module
- Provision ACR with Basic SKU (configurable)
- Output ACR login server URL and resource ID

#### 2.16 Azure Service Bus Module
- Create `modules/service-bus/` Terraform module
- Provision namespace (Standard SKU), `shortly-events` topic, `event-processor` subscription
- Create Send-only authorisation rule (least-privilege)
- Store connection string in Key Vault as a secret
- Configure 7-day message TTL and dead-lettering on expiration

#### 2.17 Azure Container Apps Module
- Create `modules/container-apps/` Terraform module
- Provision Container Apps Environment with Log Analytics workspace
- Deploy API container with configurable CPU/memory and replica counts
- Configure external ingress (HTTPS, port 8080)
- Configure liveness (`/health/live`) and readiness (`/health/ready`) probes
- Inject secrets from Key Vault via managed identity
- Grant managed identity `AcrPull` on Container Registry and `Key Vault Secrets User` on Key Vault
- Configure HTTP-based auto-scaling (default: 50 concurrent requests per instance)

### Exit Criteria
- All Phase 2 test cases pass (TC-SEC-001 to TC-SEC-004, TC-INF-001 to TC-INF-005)
- Structured logs contain correlation IDs and are parseable as JSON
- Metrics endpoint returns redirect latency histograms
- Rate limiting correctly throttles above configured threshold
- Circuit breaker opens after 5 consecutive MongoDB failures
- Redis cache hit rate >80% for redirect requests under sustained load
- 3 API instances handle 1,000 concurrent redirects with <100ms p99
- MongoDB replica set failover completes within 10 seconds
- Domain events are reliably published via transactional outbox (zero event loss)
- `terraform plan` on a clean subscription shows all resources to be created with no errors
- `terraform apply` provisions all resources (Container Apps, Service Bus, Key Vault, ACR) successfully
- Container App pulls image from ACR and reads secrets from Key Vault via managed identity
- `terraform plan` on an existing deployment shows no changes (idempotent)

---

## Phase 3: Analytics and Enhancements

### Goal
Add usage analytics, bulk operations, and reporting capabilities.

### Requirements In Scope

| Requirement | Description | Priority |
|-------------|-------------|----------|
| FR-ANL-001 | Track redirect counts | P2 |
| FR-ANL-002 | Query link statistics | P2 |
| FR-ANL-003 | Dashboard API | P3 |
| FR-LNK-006 | Bulk create links | P2 |
| FR-ADM-002 | Admin reporting | P3 |

### Technical Tasks

#### 3.1 Analytics Event Consumer
- Implement Service Bus consumer for `LinkRedirected` events
- Aggregate redirect counts per link (MongoDB or separate analytics store)
- Handle duplicate events idempotently

#### 3.2 Statistics API
- `GET /api/links/{prefix}/{code}/stats` — click count, last click timestamp
- `GET /api/analytics/summary` — total links, total redirects, top domains

#### 3.3 Bulk Link Creation
- `POST /api/links/batch` — accept up to 100 links
- Process in parallel, return individual results
- Partial failure handling (some succeed, some fail)

### Exit Criteria
- Redirect counts are accurately tracked within 30 seconds of the redirect
- Bulk creation of 100 links completes in <5 seconds
- Dashboard API returns aggregate statistics

---

## Risk Assessment

### Technical Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| MongoDB latency spikes under write load | Medium | High (redirect latency) | Separate read/write concerns; redirect reads from secondary replicas; monitor query performance |
| Counter hot document contention | Low | Medium (creation throughput) | Range pre-allocation (1000 per batch) distributes writes; monitor counter collection |
| Azure Service Bus outage blocking redirects | Low | High (if not handled) | Fire-and-forget pattern already designed; ensure non-blocking publish |
| Short code exhaustion per prefix | Very Low | Low | 916M 5-char codes per prefix; alert at 80% capacity; codes extend to 6+ chars automatically |
| MongoDB connection pool exhaustion | Medium | High | Configure pool size based on load testing; circuit breaker prevents cascade; monitor pool metrics |

### Operational Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Domain prefix collision (two teams choose same prefix) | Medium | Low | Registration API enforces uniqueness; prefix naming convention documentation |
| Accidental domain deactivation | Low | Medium | Warning when active links exist (BR-DOM-004); no auto-delete of links on deactivation |
| Missing monitoring in early phases | High (Phase 1) | Medium | Phase 2 adds observability; accept risk in Phase 1 with manual monitoring |

---

## Open Questions

### Requiring Decision Before Phase 1

| # | Question | Options | Recommendation | Impact |
|---|----------|---------|----------------|--------|
| OQ-1 | Default redirect type: 301 or 302? | A) 301 (permanent — browsers cache) B) 302 (temporary — no caching) C) Configurable per link | C) Configurable, default 302 | Affects analytics accuracy if 301 |
| OQ-2 | Base domain for short links? | A) Custom short domain (e.g., `sh.rt`) B) Subdomain of company domain C) Configurable per environment | C) Configurable via settings | Deployment configuration |
| OQ-3 | Authentication mechanism? | A) API keys (simpler) B) OAuth2/JWT (standard) C) Both | A) API keys for MVP, add OAuth2 in Phase 2 | Implementation complexity |

### Requiring Decision Before Phase 2

| # | Question | Options | Recommendation | Impact |
|---|----------|---------|----------------|--------|
| OQ-4 | Where to store API keys? | A) Azure Key Vault B) MongoDB C) Azure AD | A) Azure Key Vault | Security architecture |
| OQ-5 | Rate limit strategy? | A) Fixed window B) Sliding window C) Token bucket | C) Token bucket (smoothest) | Rate limiting fairness |
| OQ-6 | Logging destination? | A) Azure Monitor/App Insights B) ELK Stack C) Both | A) Azure Monitor (native integration) | Operational tooling |

### Requiring Decision Before Phase 3

| # | Question | Options | Recommendation | Impact |
|---|----------|---------|----------------|--------|
| OQ-7 | Analytics data store? | A) Same MongoDB B) Separate time-series DB C) Azure Data Explorer | A) Same MongoDB for MVP analytics | Data architecture |
| OQ-8 | Analytics retention period? | A) 30 days B) 90 days C) 1 year | B) 90 days with configurable retention | Storage cost |

---

## Dependency Map

```
Phase 1 (MVP)
├── 1.1 Solution Scaffolding
│   └── 1.2 Domain Model
│       ├── 1.3 Short Code Generation
│       │   └── 1.4 MongoDB Infrastructure
│       │       └── 1.5 API Layer
│       │           └── 1.6 Event Publishing
│       └── 1.4 MongoDB Infrastructure (parallel with 1.3)
└── 1.7 Docker Setup (parallel with all)

Phase 2 (Production Hardening)
├── 2.1 Authentication (depends on Phase 1)
├── 2.2 Input Validation (depends on Phase 1)
├── 2.3 Rate Limiting (depends on 2.1)
├── 2.4 Health Checks (independent)
├── 2.5 Structured Logging (independent)
├── 2.6 Metrics (independent)
├── 2.7 Resilience (depends on Phase 1)
├── 2.8 Link Search (depends on Phase 1)
├── 2.9 Redis Cache (depends on Phase 1)
├── 2.10 Horizontal Scaling (depends on Phase 1)
├── 2.11 MongoDB Replica Set (depends on Phase 1)
├── 2.12 Transactional Outbox (depends on 1.6)
└── 2.13 Terraform Project Structure (independent)
    ├── 2.14 Key Vault Module (depends on 2.13)
    ├── 2.15 Container Registry Module (depends on 2.13)
    ├── 2.16 Service Bus Module (depends on 2.13, 2.14)
    └── 2.17 Container Apps Module (depends on 2.13, 2.14, 2.15, 2.4)

Phase 3 (Analytics)
├── 3.1 Analytics Consumer (depends on 1.6)
├── 3.2 Statistics API (depends on 3.1)
└── 3.3 Bulk Creation (depends on 1.5)
```

---

## Definition of Done (All Phases)

- [ ] All functional requirements for the phase are implemented
- [ ] All acceptance criteria (test cases) for the phase pass
- [ ] Code compiles with zero errors and zero warnings
- [ ] Unit test coverage ≥85% for domain and application layers
- [ ] Integration tests pass against real dependencies (MongoDB, Service Bus)
- [ ] OpenAPI documentation is accurate and up to date
- [ ] No TODO comments in production code
- [ ] Code reviewed against ADR compliance
- [ ] Docker Compose setup works end-to-end
- [ ] HANDOFF.md and CONTEXT.md updated
