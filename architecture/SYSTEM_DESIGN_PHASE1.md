# Shortly — Phase 1 MVP System Design

> **Version**: 1.0  
> **Phase**: 1 — MVP  
> **Status**: Complete  
> **Last Updated**: 11 May 2026

---

## 1. Overview

**Shortly** is a URL shortening platform that enables internal services to create branded short links, manage custom domain prefixes, and redirect users to destination URLs. Phase 1 delivers the core link lifecycle: creation, resolution, disable, and automatic expiry, backed by MongoDB and with domain events published to Azure Service Bus.

### 1.1 Key Capabilities

- Create short links with custom domain prefixes (e.g., `/ho/6laZG`)
- Resolve short links to destination URLs via HTTP 302 redirect
- Register and manage custom domain prefixes
- Automatic link expiry via MongoDB TTL indexes
- Domain event publishing (Azure Service Bus or logging fallback)
- OpenAPI/Swagger documentation

### 1.2 Technology Stack

| Component | Technology | Version |
|---|---|---|
| Runtime | .NET | 10.0.4 |
| Language | C# | 14 |
| Database | MongoDB | 7.x |
| Message Broker | Azure Service Bus | — |
| Driver | MongoDB.Driver | 3.8.0 |
| Service Bus Client | Azure.Messaging.ServiceBus | 7.20.1 |
| API Documentation | Swashbuckle.AspNetCore | 10.1.7 |
| Containerisation | Docker (aspnet:10.0) | — |
| Orchestration | Docker Compose | — |

---

## 2. Architecture

### 2.1 Architectural Style

**Clean Architecture** with **Domain-Driven Design** (Pragmatic DDD). Four layers with strict dependency rules enforced by project references:

```
┌─────────────────────────────────────────────────────┐
│                   Shortly.Api                        │
│         Controllers, Contracts, Middleware            │
│              (Presentation Layer)                     │
├──────────────────────┬──────────────────────────────┤
│                      │                               │
│                      ▼                               │
│             Shortly.Application                      │
│        Services, Interfaces, Result<T>               │
│              (Application Layer)                      │
├──────────────────────┬──────────────────────────────┤
│                      │                               │
│          ┌───────────┴────────────┐                  │
│          ▼                        ▼                  │
│   Shortly.Domain          Shortly.Infrastructure     │
│   Entities, Value         Repositories, Code Gen,    │
│   Objects, Events         Event Publishers, Mappers  │
│   (Domain Layer)          (Infrastructure Layer)      │
└─────────────────────────────────────────────────────┘
```

**Dependency Rules:**
- `Shortly.Domain` → no project references (zero external dependencies)
- `Shortly.Application` → references `Shortly.Domain` only
- `Shortly.Infrastructure` → references `Shortly.Application` (implements its interfaces)
- `Shortly.Api` → references `Shortly.Application` + `Shortly.Infrastructure` (composition root)

### 2.2 Solution Structure

```
Shortly.slnx
├── src/
│   ├── Shortly.Domain/              # Domain layer (0 NuGet packages)
│   │   ├── Common/                  # Entity<TId>, ValueObject, DomainEvent, DomainException
│   │   ├── LinkManagement/          # Link aggregate, value objects, events
│   │   └── CustomDomains/           # CustomDomain aggregate, events
│   │
│   ├── Shortly.Application/         # Application layer
│   │   ├── Common/                  # Result<T>, ErrorType
│   │   ├── Interfaces/              # ILinkRepository, ICustomDomainRepository,
│   │   │                            # IShortCodeGenerator, IEventPublisher
│   │   ├── Links/                   # LinkService, RedirectService
│   │   └── CustomDomains/           # CustomDomainService, DeactivationResult
│   │
│   ├── Shortly.Infrastructure/      # Infrastructure layer
│   │   ├── Configuration/           # MongoDbSettings, ServiceBusSettings
│   │   ├── Persistence/
│   │   │   ├── Documents/           # LinkDocument, CustomDomainDocument (POCOs)
│   │   │   ├── Mappers/             # LinkMapper, CustomDomainMapper
│   │   │   ├── MongoDbContext.cs    # Database context + GuidSerializer registration
│   │   │   ├── MongoDbIndexInitialiser.cs  # IHostedService for index creation
│   │   │   ├── MongoLinkRepository.cs
│   │   │   └── MongoCustomDomainRepository.cs
│   │   ├── CodeGeneration/          # RangeBasedCodeGenerator, Base62
│   │   └── Events/                  # ServiceBusEventPublisher, LoggingEventPublisher
│   │
│   └── Shortly.Api/                 # Presentation layer
│       ├── Controllers/             # LinksController, DomainsController, RedirectController
│       ├── Contracts/               # Request/Response DTOs, ErrorResponse
│       ├── Middleware/              # ExceptionHandlingMiddleware
│       └── Program.cs              # Composition root
│
├── tests/
│   ├── Shortly.Domain.Tests/        # 118 tests — domain model, value objects, business rules
│   ├── Shortly.Application.Tests/   # 5 tests — service logic, result handling
│   ├── Shortly.Infrastructure.Tests/# 122 tests — MongoDB integration, code gen, mappers
│   └── Shortly.Api.Tests/           # 7 tests — controller, middleware
│
├── Dockerfile                       # Single-stage (host-publish + COPY)
├── docker-compose.yml               # API + MongoDB 7
└── architecture/                    # ADRs, bounded contexts, C4 diagrams
```

---

## 3. Domain Model

### 3.1 Bounded Contexts

Phase 1 contains two bounded contexts within a single deployable:

#### Link Management

The core context responsible for creating, resolving, and managing short links.

**Aggregate Root: `Link`**

| Property | Type | Description |
|---|---|---|
| `Id` | `LinkId` (Guid) | Unique identifier |
| `ShortCode` | `ShortCode` (ValueObject) | 5–8 Base62 characters |
| `DomainPrefix` | `DomainPrefix` (ValueObject) | 2–4 lowercase alphanumeric characters |
| `DestinationUrl` | `DestinationUrl` (ValueObject) | Valid HTTP/HTTPS URL, max 2048 chars |
| `Status` | `LinkStatus` (Enum) | `Active`, `Disabled`, `Expired` |
| `CreatedAt` | `DateTimeOffset` | UTC creation timestamp |
| `ExpiresAt` | `DateTimeOffset?` | Optional expiry (drives MongoDB TTL) |
| `CreatedBy` | `string` | Identifier of the creating service/user |
| `Metadata` | `LinkMetadata` (ValueObject) | Up to 10 key-value tags |
| `Version` | `int` | Optimistic concurrency version |

**Behaviours:**
- `Link.Create(...)` — Factory method. Validates inputs, sets status to `Active`, raises `LinkCreatedEvent`
- `Link.Disable()` — Idempotent. Transitions `Active` → `Disabled`, raises `LinkDisabledEvent`. Cannot disable an expired link.
- `Link.IsRedirectable()` — Returns `true` if `Active` and not past `ExpiresAt`
- `Link.IsExpired()` — Checks status and `ExpiresAt`

**Domain Events:**
- `LinkCreatedEvent` — Contains LinkId, DomainPrefix, ShortCode, DestinationUrl
- `LinkDisabledEvent` — Contains LinkId, DomainPrefix, ShortCode
- `LinkRedirectedEvent` — Contains LinkId, DomainPrefix, ShortCode

#### Custom Domains

Manages the registry of domain prefixes that links are created under.

**Aggregate Root: `CustomDomain`**

| Property | Type | Description |
|---|---|---|
| `Id` | `CustomDomainId` (Guid) | Unique identifier |
| `Prefix` | `DomainPrefix` (ValueObject) | Shared value object from Link Management |
| `Name` | `string` | Human-readable domain name |
| `Description` | `string?` | Optional description |
| `IsActive` | `bool` | Whether the prefix accepts new links |
| `CreatedAt` | `DateTimeOffset` | UTC creation timestamp |
| `Version` | `int` | Optimistic concurrency version |

**Behaviours:**
- `CustomDomain.Register(...)` — Factory method. Validates name, raises `CustomDomainRegisteredEvent`
- `CustomDomain.Deactivate()` — Idempotent. Sets `IsActive = false`, raises `CustomDomainDeactivatedEvent`

**Domain Events:**
- `CustomDomainRegisteredEvent` — Contains CustomDomainId, Prefix, Name
- `CustomDomainDeactivatedEvent` — Contains CustomDomainId, Prefix

### 3.2 Value Objects

| Value Object | Constraints | Validation |
|---|---|---|
| `ShortCode` | 5–8 chars, `[a-zA-Z0-9]` | Compiled regex `^[a-zA-Z0-9]{5,8}$` |
| `DomainPrefix` | 2–4 chars, `[a-z0-9]` | Compiled regex `^[a-z0-9]{2,4}$` |
| `DestinationUrl` | HTTP/HTTPS only, max 2048 chars | `Uri.TryCreate` + scheme check. Blocks `javascript:` and `data:` schemes |
| `LinkMetadata` | Max 10 tags, key ≤50 chars, value ≤200 chars | Validated on construction |

### 3.3 Base Classes

| Class | Purpose |
|---|---|
| `Entity<TId>` | Identity, `CreatedAt`, `Version`, domain event collection (`RaiseDomainEvent`, `ClearDomainEvents`) |
| `ValueObject` | Structural equality via `GetEqualityComponents()`, operator overloads |
| `DomainEvent` | `EventId` (Guid), `OccurredAt` (DateTimeOffset) |
| `DomainException` | Base exception for domain rule violations |

---

## 4. Application Layer

### 4.1 Services

#### `LinkService`

Orchestrates link creation, retrieval, and disable operations.

| Method | Description |
|---|---|
| `CreateAsync(prefix, url, createdBy, expiresAt?, tags?)` | Validates domain exists and is active → generates short code → creates Link → persists → publishes events |
| `GetByIdAsync(id)` | Retrieves link by GUID |
| `GetByPrefixAndCodeAsync(prefix, code)` | Retrieves link by compound key |
| `DisableAsync(prefix, code)` | Finds link → calls `Disable()` → persists → publishes events |

#### `RedirectService`

Handles the redirect flow with fire-and-forget event publishing.

| Method | Description |
|---|---|
| `ResolveAsync(prefix, code)` | Looks up link → checks `IsRedirectable()` → publishes `LinkRedirectedEvent` fire-and-forget → returns destination URL |

Returns `RedirectResult` with three states: `Success(url)`, `NotFound()`, `Gone()`.

#### `CustomDomainService`

Manages domain prefix registration and deactivation.

| Method | Description |
|---|---|
| `RegisterAsync(prefix, name, description?)` | Checks uniqueness → creates CustomDomain → persists → publishes events |
| `ListAsync(activeOnly?)` | Returns all domains, optionally filtered |
| `DeactivateAsync(prefix)` | Finds domain → deactivates → counts active links → returns with warning if links exist |

### 4.2 Interfaces (Ports)

```csharp
ILinkRepository           // GetByIdAsync, GetByPrefixAndCodeAsync, AddAsync, UpdateAsync, CountByPrefixAsync
ICustomDomainRepository   // GetByPrefixAsync, GetAllAsync, AddAsync, UpdateAsync, ExistsAsync
IShortCodeGenerator       // GenerateAsync(prefix) → ShortCode
IEventPublisher           // PublishAsync(event), PublishFireAndForgetAsync(event)
```

### 4.3 Result Pattern

All service methods return `Result<T>` with typed error support:

```csharp
Result<T>.Success(value)
Result<T>.Failure(error, ErrorType.Validation)
Result<T>.NotFound(error)
Result<T>.Conflict(error)
```

`ErrorType` enum: `Validation`, `NotFound`, `Conflict` — mapped to HTTP status codes in controllers.

---

## 5. Infrastructure Layer

### 5.1 MongoDB Persistence

#### Collections

| Collection | Document | Purpose |
|---|---|---|
| `links` | `LinkDocument` | Stores short link data |
| `domains` | `CustomDomainDocument` | Stores domain prefix registrations |
| `counters` | `CounterDocument` | Atomic counter ranges for code generation |

#### Indexes

Created at startup by `MongoDbIndexInitialiser` (IHostedService):

| Collection | Index | Type | Purpose |
|---|---|---|---|
| `links` | `ix_links_prefix_code_unique` | Compound unique (`DomainPrefix` + `ShortCode`) | Guarantees link uniqueness |
| `links` | `ix_links_expiresAt_ttl` | TTL (`ExpiresAt`, `ExpireAfter: 0`) | Automatic document expiry |
| `links` | `ix_links_status` | Secondary (`Status`) | Status-based queries |
| `domains` | `ix_domains_prefix_unique` | Unique (`Prefix`) | Guarantees prefix uniqueness |

#### Document Mapping

Static mapper classes (`LinkMapper`, `CustomDomainMapper`) convert between domain entities and persistence documents. Domain entities have `internal` reconstitution constructors — no validation, no events — used exclusively by mappers.

```
Link ←→ LinkDocument      via LinkMapper.ToDocument() / ToDomain()
CustomDomain ←→ CustomDomainDocument  via CustomDomainMapper.ToDocument() / ToDomain()
```

#### MongoDbContext

- Singleton — creates `MongoClient` and `IMongoDatabase`
- Registers `GuidSerializer(GuidRepresentation.Standard)` at startup (thread-safe, one-time)
- Required because MongoDB.Driver 3.x defaults to `GuidRepresentation.Unspecified`, which throws on serialisation

### 5.2 Short Code Generation

**Algorithm: Range-based counter with Base62 encoding**

```
┌──────────────┐     ┌───────────────────┐     ┌─────────────────┐
│   Request    │────▶│  In-Memory Range  │────▶│  Base62.Encode   │
│              │     │  (ConcurrentDict) │     │  → "6laZG"       │
└──────────────┘     └────────┬──────────┘     └─────────────────┘
                              │ exhausted
                              ▼
                     ┌───────────────────┐
                     │   MongoDB $inc    │
                     │   (atomic, batch  │
                     │    size = 1000)   │
                     └───────────────────┘
```

**How it works:**
1. Each domain prefix gets an independent counter range stored in memory
2. When a code is requested, the generator increments the in-memory counter and Base62-encodes it
3. When the range is exhausted, a new range is claimed via MongoDB `FindOneAndUpdate` with `$inc` (batch size 1000)
4. Initial counter value: `100,000,000` (ensures 5+ character Base62 codes from the start)

**Concurrency Handling:**
- Fast path: `ConcurrentDictionary` + `lock` for in-memory range — no I/O
- Slow path: Per-prefix `SemaphoreSlim` with double-check pattern prevents thundering herd when 1000 concurrent requests exhaust a range simultaneously
- MongoDB `$inc` is atomic — safe across multiple application instances

**Base62 Alphabet:** `0-9A-Za-z` (62 characters)

### 5.3 Event Publishing

Two implementations of `IEventPublisher`:

| Implementation | When Used | Behaviour |
|---|---|---|
| `ServiceBusEventPublisher` | Service Bus connection string configured | Publishes to Azure Service Bus topic (`shortly-events`) as JSON |
| `LoggingEventPublisher` | No connection string (local dev) | Logs events at INFO level |

Selection is automatic at startup via DI registration in `DependencyInjection.cs`.

`RedirectService` uses `PublishFireAndForgetAsync` — redirect latency is not affected by event publishing failures.

---

## 6. API Layer

### 6.1 Endpoints

#### Link Management (`/api/Links`)

| Method | Route | Description | Success | Error Codes |
|---|---|---|---|---|
| `POST` | `/api/Links` | Create a short link | 201 + `LinkResponse` | 400, 409 |
| `GET` | `/api/Links/{prefix}/{code}` | Get link by prefix + code | 200 + `LinkResponse` | 404 |
| `GET` | `/api/Links/{id:guid}` | Get link by ID | 200 + `LinkResponse` | 404 |
| `DELETE` | `/api/Links/{prefix}/{code}` | Disable a link | 200 + `LinkResponse` | 400, 404 |

#### Domain Management (`/api/Domains`)

| Method | Route | Description | Success | Error Codes |
|---|---|---|---|---|
| `POST` | `/api/Domains` | Register a domain prefix | 201 + `CustomDomainResponse` | 400, 409 |
| `GET` | `/api/Domains` | List all domains | 200 + `CustomDomainResponse[]` | — |
| `DELETE` | `/api/Domains/{prefix}` | Deactivate a domain prefix | 200 + `CustomDomainResponse` | 404 |

#### Redirect (root routes)

| Method | Route | Description | Success | Error Codes |
|---|---|---|---|---|
| `GET` | `/{prefix}/{code}` | Resolve and redirect | 302 + `Location` header | 404, 410 |

Route constraint: `prefix` matches `^[[a-z0-9]]{2,4}$`, `code` matches `^[[a-zA-Z0-9]]{5,8}$` (brackets escaped for ASP.NET route templates).

The redirect controller is hidden from Swagger (`[ApiExplorerSettings(IgnoreApi = true)]`).

### 6.2 Request Validation

DTO validation via `System.ComponentModel.DataAnnotations`:

**`CreateLinkRequest`:**
- `DomainPrefix`: Required, 2–4 chars, `^[a-z0-9]+$`
- `DestinationUrl`: Required, valid URL, max 2048 chars
- `CreatedBy`: Required, 1–100 chars
- `ExpiresAt`: Optional
- `Tags`: Optional dictionary

**`RegisterCustomDomainRequest`:**
- `Prefix`: Required, 2–4 chars, `^[a-z0-9]+$`
- `Name`: Required, 1–200 chars
- `Description`: Optional, max 500 chars

### 6.3 Error Handling

`ExceptionHandlingMiddleware` catches exceptions globally:

| Exception Type | HTTP Status | Behaviour |
|---|---|---|
| `DomainException` | 400 | Logged at WARN |
| `ArgumentException` | 400 | Logged at WARN |
| `OperationCanceledException` (client abort) | 499 | Logged at INFO |
| Unhandled `Exception` | 500 | Logged at ERROR, generic message returned |

All error responses use consistent `ErrorResponse` shape:
```json
{
  "error": "Domain prefix 'xyz' is not registered.",
  "statusCode": 400
}
```

### 6.4 API Response Examples

**Create Link:**
```json
POST /api/Links
{
  "domainPrefix": "e2e",
  "destinationUrl": "https://github.com/nnguyen13-bit/Shortly",
  "createdBy": "smoke-test"
}

→ 201 Created
{
  "id": "3fa85f64-...",
  "shortCode": "6laZG",
  "domainPrefix": "e2e",
  "destinationUrl": "https://github.com/nnguyen13-bit/Shortly",
  "status": "Active",
  "createdBy": "smoke-test",
  "createdAt": "2026-05-11T04:20:29Z",
  "expiresAt": null,
  "tags": null,
  "shortUrl": "http://localhost:5254/e2e/6laZG"
}
```

**Redirect:**
```
GET /e2e/6laZG

→ 302 Found
Location: https://github.com/nnguyen13-bit/Shortly
```

---

## 7. Dependency Injection

Registered in two extension methods called from `Program.cs`:

### `AddApplication()`
| Registration | Lifetime | Type |
|---|---|---|
| `LinkService` | Scoped | Application service |
| `RedirectService` | Scoped | Application service |
| `CustomDomainService` | Scoped | Application service |

### `AddInfrastructure(configuration)`
| Registration | Lifetime | Type |
|---|---|---|
| `MongoDbContext` | Singleton | Database context |
| `MongoLinkRepository` → `ILinkRepository` | Scoped | Repository |
| `MongoCustomDomainRepository` → `ICustomDomainRepository` | Scoped | Repository |
| `RangeBasedCodeGenerator` → `IShortCodeGenerator` | Singleton | Code generator (holds in-memory counter ranges) |
| `ServiceBusEventPublisher` or `LoggingEventPublisher` → `IEventPublisher` | Singleton | Event publisher (conditional) |
| `MongoDbIndexInitialiser` | Hosted Service | Index creation on startup |

---

## 8. Data Flow

### 8.1 Create Link

```
Client ──POST /api/Links──▶ LinksController
                                   │
                                   ▼
                             LinkService.CreateAsync()
                                   │
                    ┌──────────────┼──────────────────┐
                    ▼              ▼                   ▼
          ICustomDomainRepo   IShortCodeGen      ILinkRepository
          (check prefix        (generate code)    (persist link)
           exists + active)         │
                                    ▼
                           RangeBasedCodeGenerator
                           ┌─ in-memory range ──┐
                           │   or MongoDB $inc  │
                           └────────────────────┘
                                   │
                                   ▼
                            IEventPublisher
                          (LinkCreatedEvent)
                                   │
                                   ▼
                            201 Created + LinkResponse
```

### 8.2 Redirect

```
Client ──GET /e2e/6laZG──▶ RedirectController
                                   │
                                   ▼
                          RedirectService.ResolveAsync()
                                   │
                                   ▼
                            ILinkRepository
                          (GetByPrefixAndCode)
                                   │
                          ┌────────┴────────┐
                          │                 │
                     Found + Active    Not Found / Gone
                          │                 │
                          ▼                 ▼
                   Fire-and-forget     404 or 410
                   LinkRedirectedEvent
                          │
                          ▼
                   302 Redirect
                   Location: destination URL
```

---

## 9. Deployment

### 9.1 Docker

**Dockerfile** — Single-stage build (corporate proxy prevents in-container NuGet restore):

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
COPY publish/ .
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "Shortly.Api.dll"]
```

**Build process:**
```bash
dotnet publish src/Shortly.Api -c Release -o ./publish --no-self-contained
docker compose up --build -d
```

### 9.2 Docker Compose

```yaml
services:
  shortly-api:          # .NET 10 API, port 5254 → 8080
    depends_on:
      mongo: { condition: service_healthy }

  mongo:                # MongoDB 7, port 27017
    healthcheck:        # mongosh ping, 5s interval
    volumes:
      - mongo-data:/data/db
```

**Environment variables:**
- `MongoDB__ConnectionString` — MongoDB connection string
- `MongoDB__DatabaseName` — Database name (`shortly`)
- `ServiceBus__ConnectionString` — Azure Service Bus (empty = logging fallback)
- `ServiceBus__TopicName` — Topic name (`shortly-events`)

### 9.3 Configuration

`appsettings.json` provides defaults:
```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "shortly"
  },
  "ServiceBus": {
    "ConnectionString": "",
    "TopicName": "shortly-events"
  }
}
```

Environment variables override settings via `__` separator (ASP.NET configuration convention).

---

## 10. Testing

### 10.1 Test Summary

| Project | Tests | Focus |
|---|---|---|
| `Shortly.Domain.Tests` | 118 | Value object validation, entity behaviour, status transitions, domain events, edge cases |
| `Shortly.Application.Tests` | 5 | Service orchestration, result pattern |
| `Shortly.Infrastructure.Tests` | 122 | MongoDB integration (Testcontainers), code generation (concurrency, uniqueness), mapper round-trips, index verification, redirect latency |
| `Shortly.Api.Tests` | 7 | Controller response mapping, middleware error handling |
| **Total** | **240** | |

### 10.2 Key Test Scenarios

**Concurrency & Uniqueness (Exit Criteria):**
- 1,000 concurrent requests to `RangeBasedCodeGenerator` — 0 duplicate codes (single instance)
- 1,000 concurrent requests across 3 generator instances — 0 duplicate codes

**Performance (Exit Criteria):**
- 1,000 sequential redirect lookups against 10,000 seeded links — P99 < 100ms ✓ (measured: 2.98ms)
- 1,000 concurrent redirect lookups (50 concurrency) against 10,000 seeded links — P99 < 100ms ✓ (measured: 68.88ms)

**Integration Tests:**
- All MongoDB tests use `Testcontainers.MongoDb` (real MongoDB 7 in Docker)
- Index creation verified: unique compound, TTL, secondary, prefix unique
- Mapper round-trip tests: 13 tests covering all document↔domain conversions

### 10.3 Testing Infrastructure

| Tool | Version | Purpose |
|---|---|---|
| xUnit | 2.9.3 | Test framework |
| Testcontainers.MongoDb | 4.11.0 | MongoDB containers for integration tests |
| Docker Desktop | — | Container runtime for Testcontainers |

---

## 11. Security Considerations (Phase 1)

Phase 1 has minimal security — production hardening is Phase 2.

**Current protections:**
- Input validation on all DTOs (data annotations + domain value object validation)
- `DestinationUrl` blocks `javascript:` and `data:` schemes
- URL scheme restricted to HTTP/HTTPS only
- Domain exception handling prevents stack trace leakage (500 returns generic message)
- No authentication — all endpoints are publicly accessible

**Deferred to Phase 2:**
- API key authentication (`FR-SEC-001`)
- Input sanitisation (`FR-SEC-002`)
- Rate limiting (`FR-SEC-003`)
- Request body size limits

---

## 12. Known Limitations

| Limitation | Impact | Resolution |
|---|---|---|
| No authentication | All API endpoints are publicly accessible | Phase 2, Task 2.1 |
| No rate limiting | API can be overwhelmed by excessive requests | Phase 2, Task 2.3 |
| No caching | Every redirect hits MongoDB | Phase 2, Task 2.9 (Redis) |
| No health checks | Cannot be monitored by orchestrators | Phase 2, Task 2.4 |
| No structured logging | Logs are not JSON-formatted | Phase 2, Task 2.5 (Serilog) |
| No resilience patterns | MongoDB failures propagate directly to clients | Phase 2, Task 2.7 (Polly) |
| Single MongoDB instance | No replication or failover | Phase 2, Task 2.11 |
| No link search/filter | Cannot query links by criteria | Phase 2, Task 2.8 |
| Docker build requires host publish | Corporate proxy blocks NuGet inside Docker | Environment-specific workaround |

---

## 13. Phase 2 Roadmap Preview

Phase 2 adds production hardening across 17 tasks:

| Priority | Tasks |
|---|---|
| **P0** | Authentication (2.1), Input validation hardening (2.2), Health checks (2.4) |
| **P1** | Rate limiting (2.3), Structured logging (2.5), Metrics (2.6), Resilience (2.7), Link search (2.8), Redis cache (2.9), Horizontal scaling (2.10), MongoDB replica set (2.11), Transactional outbox (2.12), Terraform IaC (2.13–2.17) |
