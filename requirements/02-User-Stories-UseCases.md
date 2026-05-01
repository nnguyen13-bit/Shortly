# 02 — User Stories and Use Cases

> **Cross-reference:** Business rules are defined in [Document 00](00-ExecutiveSummary-KeyRequirements.md). Functional requirements are defined in [Document 01](01-Functional-Requirements.md).

---

## Personas

### P1: API Consumer Service

An internal microservice (e.g., Order Management, Invoice System, Delivery Tracker) that programmatically creates short links and embeds them in customer-facing messages.

- **Goals:** Create short links quickly, receive the short URL, embed in SMS/email
- **Skills:** Software developer maintaining the calling service; understands REST APIs
- **Frequency:** High — 60,000+ link creation calls per day across all consumer services
- **Pain Points:** Long URLs break SMS formatting, multi-segment SMS cost; needs sub-second API response times

### P2: Platform Administrator

An internal operations or DevOps engineer responsible for managing domains, monitoring system health, and investigating issues.

- **Goals:** Register new domain prefixes, deactivate old ones, monitor throughput and error rates, search links for investigation
- **Skills:** Technical — comfortable with APIs, dashboards, and logs
- **Frequency:** Low — domain changes are infrequent; monitoring is continuous
- **Pain Points:** Cannot identify business context from generic short URLs; needs operational visibility

### P3: Support Agent

An internal support team member who investigates customer-reported issues with short links (e.g., "my link doesn't work").

- **Goals:** Look up a link by its short URL, check status, identify destination, understand why a link might not be working
- **Skills:** Semi-technical — can use admin tools but not APIs directly
- **Frequency:** Medium — several investigations per day
- **Pain Points:** Cannot tell from the URL alone what domain/context a link belongs to; needs quick lookup

### P4: End User (Customer)

A customer who receives a short link in an SMS, email, or notification and clicks it.

- **Goals:** Reach the destination (order details, invoice, etc.) quickly and reliably
- **Skills:** Non-technical
- **Frequency:** Occasional — when they receive a message with a short link
- **Pain Points:** Broken links, slow redirects, confusing error pages

---

## Epics

| Epic | Persona | Phase | User Stories |
|------|---------|-------|-------------|
| E1: Link Creation | P1 (API Consumer) | 1 | US-001 to US-005 |
| E2: Redirect | P4 (End User) | 1 | US-006 to US-009 |
| E3: Domain Management | P2 (Platform Admin) | 1 | US-010 to US-013 |
| E4: Link Investigation | P3 (Support Agent) | 1–2 | US-014 to US-016 |
| E5: Platform Operations | P2 (Platform Admin) | 2 | US-017 to US-020 |
| E6: Analytics | P2 (Platform Admin) | 3 | US-021 to US-023 |

---

## Epic 1: Link Creation

### US-001: Create a Short Link

**As** an API Consumer Service (P1),
**I want to** create a short link for a destination URL with a specific domain prefix,
**So that** I can embed a minimal-length, context-rich URL in customer communications.

**Given** I have a valid destination URL `https://orders.example.com/handover/ORD-2026-1234567`
**And** the domain prefix `ho` is registered and active
**When** I call `POST /api/links` with `{ "domainPrefix": "ho", "destinationUrl": "..." }`
**Then** the system returns HTTP 201 with the short link `https://sh.rt/ho/{code}`
**And** the short code is 5 characters of Base62
**And** the link is stored with status `Active`

**Requirements:** FR-LNK-001
**Priority:** P0

---

### US-002: Create a Short Link with Expiry

**As** an API Consumer Service (P1),
**I want to** create a short link that automatically expires after a given date,
**So that** time-sensitive links (e.g., promotional offers) stop working after the intended window.

**Given** I have a valid destination URL and domain prefix
**And** I provide an `expiresAt` timestamp 7 days in the future
**When** I call `POST /api/links` with the expiry
**Then** the system returns a short link that will return HTTP 410 after the expiry date

**Given** I provide an `expiresAt` timestamp in the past
**When** I call `POST /api/links`
**Then** the system returns HTTP 400 with an error message per BR-LNK-006

**Requirements:** FR-LNK-001 (sub FR-LNK-001.6), FR-LNK-003
**Priority:** P1

---

### US-003: Create a Short Link with Metadata

**As** an API Consumer Service (P1),
**I want to** attach metadata tags to a short link (e.g., `orderId`, `customerId`),
**So that** I can later search for or correlate links with business entities.

**Given** I have a valid destination URL and domain prefix
**And** I provide metadata `{ "orderId": "ORD-2026-1234567", "channel": "sms" }`
**When** I call `POST /api/links` with the metadata
**Then** the system stores the metadata with the link
**And** the metadata is returned when the link is queried

**Given** I provide more than 10 metadata tags
**When** I call `POST /api/links`
**Then** the system returns HTTP 400 per BR-LNK-008

**Requirements:** FR-LNK-001 (sub FR-LNK-001.7)
**Priority:** P1

---

### US-004: Disable a Link

**As** an API Consumer Service (P1),
**I want to** disable a short link that should no longer work,
**So that** customers cannot access content that has been revoked or is no longer valid.

**Given** a short link `ho/a3Bx9` is active
**When** I call `DELETE /api/links/ho/a3Bx9`
**Then** the link status becomes `Disabled`
**And** subsequent redirect requests return HTTP 410

**Given** a short link `ho/a3Bx9` is already disabled
**When** I call `DELETE /api/links/ho/a3Bx9`
**Then** the system returns HTTP 200 (idempotent)

**Requirements:** FR-LNK-002
**Priority:** P0

---

### US-005: Handle Duplicate Destination URLs

**As** an API Consumer Service (P1),
**I want to** create multiple short links for the same destination URL,
**So that** I can track different campaigns or channels separately.

**Given** a short link already exists for `https://orders.example.com/ORD-123`
**When** I call `POST /api/links` with the same destination URL but different metadata
**Then** the system creates a new, separate short link with a different code
**And** both links redirect to the same destination independently

**Requirements:** FR-LNK-001 (implicit — no deduplication by design)
**Priority:** P1

---

## Epic 2: Redirect

### US-006: Click a Short Link

**As** an End User (P4),
**I want to** click a short link in my SMS and be taken to the right page,
**So that** I can view my order details, invoice, or other business content.

**Given** I received SMS with `https://sh.rt/ho/a3Bx9`
**And** the link is active and not expired
**When** I click the link
**Then** my browser is redirected to the original destination URL
**And** the redirect completes in under 100ms (p99)

**Requirements:** FR-RDR-001
**Priority:** P0

---

### US-007: Click an Expired Link

**As** an End User (P4),
**I want to** see a clear error when I click an expired link,
**So that** I understand the link is no longer valid (rather than seeing a generic error).

**Given** I click a short link whose `expiresAt` has passed
**When** the redirect is attempted
**Then** the system returns HTTP 410 Gone
**And** a brief message indicates the link has expired

**Requirements:** FR-RDR-003
**Priority:** P0

---

### US-008: Click a Disabled Link

**As** an End User (P4),
**I want to** see a clear error when I click a disabled link,
**So that** I understand the link has been revoked.

**Given** I click a short link that has been disabled
**When** the redirect is attempted
**Then** the system returns HTTP 410 Gone

**Requirements:** FR-RDR-003
**Priority:** P0

---

### US-009: Click a Non-Existent Link

**As** an End User (P4),
**I want to** see a 404 error when I click a link that doesn't exist,
**So that** I know the link was never valid (e.g., typo in URL).

**Given** I navigate to `https://sh.rt/ho/ZZZZZ` which does not exist
**When** the redirect is attempted
**Then** the system returns HTTP 404 Not Found

**Requirements:** FR-RDR-002
**Priority:** P0

---

## Epic 3: Domain Management

### US-010: Register a Domain Prefix

**As** a Platform Administrator (P2),
**I want to** register a new domain prefix (e.g., `inv` for Invoices),
**So that** consumer services can create short links with business context identifiers.

**Given** the prefix `inv` is not yet registered
**When** I call `POST /api/domains` with `{ "prefix": "inv", "name": "Invoice", "description": "Invoice-related links" }`
**Then** the domain is created with status `Active`
**And** consumer services can now create links with prefix `inv`

**Given** the prefix `ho` is already registered
**When** I call `POST /api/domains` with prefix `ho`
**Then** the system returns HTTP 409 Conflict per BR-DOM-001

**Requirements:** FR-DOM-001
**Priority:** P0

---

### US-011: List Registered Domains

**As** a Platform Administrator (P2),
**I want to** see all registered domain prefixes and their statuses,
**So that** I can manage the domain registry and understand which business contexts are configured.

**Given** 5 domains are registered (3 active, 2 inactive)
**When** I call `GET /api/domains`
**Then** all 5 domains are returned with their prefix, name, status, and creation date

**Given** I want only active domains
**When** I call `GET /api/domains?active=true`
**Then** only the 3 active domains are returned

**Requirements:** FR-DOM-002
**Priority:** P1

---

### US-012: Deactivate a Domain

**As** a Platform Administrator (P2),
**I want to** deactivate a domain prefix that is no longer in use,
**So that** no new links can be created with that prefix while existing links continue to work.

**Given** the domain `old` is active with 500 existing links
**When** I call `DELETE /api/domains/old`
**Then** the domain status becomes inactive
**And** the response includes a warning that 500 active links exist per BR-DOM-004
**And** existing links under `old` continue to redirect
**And** new link creation with prefix `old` is rejected

**Requirements:** FR-DOM-003
**Priority:** P1

---

### US-013: Understand Domain Prefix Is Immutable

**As** a Platform Administrator (P2),
**I want to** understand that domain prefixes cannot be renamed after creation,
**So that** I choose prefixes carefully since they appear in all generated URLs.

**Given** a domain with prefix `ho` exists
**When** I attempt to change the prefix to `hand`
**Then** the system returns HTTP 400 per BR-DOM-002

**Requirements:** BR-DOM-002 (constraint)
**Priority:** P0

---

## Epic 4: Link Investigation

### US-014: Look Up a Link by Short URL

**As** a Support Agent (P3),
**I want to** look up a link by entering the short URL path (e.g., `ho/a3Bx9`),
**So that** I can investigate customer-reported issues with specific links.

**Given** a customer reports that `https://sh.rt/ho/a3Bx9` isn't working
**When** I call `GET /api/links/ho/a3Bx9`
**Then** the system returns the link details: status, destination URL, creation date, expiry, metadata

**Requirements:** FR-LNK-004
**Priority:** P1

---

### US-015: Search Links by Domain Prefix

**As** a Support Agent (P3),
**I want to** search for all links under a specific domain prefix,
**So that** I can investigate issues affecting a particular business domain.

**Given** 1000 links exist under prefix `ho`
**When** I call `GET /api/links?prefix=ho&page=1&pageSize=20`
**Then** the first 20 links are returned, sorted by creation date (newest first)

**Requirements:** FR-ADM-001
**Priority:** P1

---

### US-016: Identify Business Context from URL

**As** a Support Agent (P3),
**I want to** immediately know which business domain a link belongs to by looking at the URL,
**So that** I can route the investigation to the right team without a database lookup.

**Given** I see a short URL `https://sh.rt/ho/a3Bx9`
**When** I read the URL
**Then** I know it belongs to the "Handover Order" domain (prefix `ho`)
**And** I can route the issue to the Order Management team

**Requirements:** Architectural (domain prefix in URL by design)
**Priority:** P0

---

## Epic 5: Platform Operations

### US-017: Monitor System Health

**As** a Platform Administrator (P2),
**I want to** check the health of the Shortly platform,
**So that** I can confirm it is running and connected to its dependencies.

**Given** the system is running and MongoDB is reachable
**When** I call `GET /health/ready`
**Then** the system returns HTTP 200

**Given** MongoDB is unreachable
**When** I call `GET /health/ready`
**Then** the system returns HTTP 503

**Requirements:** FR-OBS-001
**Priority:** P0

---

### US-018: Monitor Redirect Latency

**As** a Platform Administrator (P2),
**I want to** monitor redirect latency percentiles in real time,
**So that** I can detect performance degradation before it impacts end users.

**Given** the metrics endpoint is configured
**When** I scrape `GET /metrics`
**Then** I see redirect latency p50, p95, and p99 values

**Requirements:** FR-OBS-003
**Priority:** P1

---

### US-019: Investigate Errors via Logs

**As** a Platform Administrator (P2),
**I want to** search structured logs by correlation ID or operation type,
**So that** I can trace a specific request through the system.

**Given** a failed link creation with correlation ID `abc-123`
**When** I search logs for `correlationId: abc-123`
**Then** I see all log entries for that request, including the error details

**Requirements:** FR-OBS-002
**Priority:** P1

---

### US-020: Protect API from Abuse

**As** a Platform Administrator (P2),
**I want to** configure rate limits per API consumer,
**So that** a misbehaving service cannot overwhelm the platform.

**Given** a consumer is configured with a limit of 100 req/s
**When** the consumer sends 150 req/s
**Then** requests beyond 100/s receive HTTP 429 with a `Retry-After` header

**Requirements:** FR-SEC-003
**Priority:** P1

---

## Epic 6: Analytics (Phase 3)

### US-021: View Link Click Count

**As** a Platform Administrator (P2),
**I want to** see how many times a specific link has been clicked,
**So that** I can report on link usage to business stakeholders.

**Requirements:** FR-ANL-001, FR-ANL-002
**Priority:** P2

---

### US-022: View Top Domains by Traffic

**As** a Platform Administrator (P2),
**I want to** see which domain prefixes receive the most redirect traffic,
**So that** I can understand platform usage patterns.

**Requirements:** FR-ANL-003
**Priority:** P3

---

### US-023: Bulk Create Links

**As** an API Consumer Service (P1),
**I want to** create up to 100 short links in a single API call,
**So that** batch operations (e.g., campaign launches) are efficient.

**Given** I submit a batch of 50 valid link creation requests
**When** I call `POST /api/links/batch`
**Then** all 50 links are created and returned

**Given** 2 out of 50 requests have invalid URLs
**When** I call `POST /api/links/batch`
**Then** 48 links are created successfully
**And** the 2 failures are reported with error details

**Requirements:** FR-LNK-006
**Priority:** P2

---

## Use Case Diagram

```
┌─────────────────────────────────────────────────────────┐
│                      Shortly Platform                    │
│                                                         │
│  ┌──────────┐    ┌──────────────┐    ┌──────────────┐  │
│  │  Create   │    │   Redirect   │    │  Register    │  │
│  │  Link     │    │   Link       │    │  Domain      │  │
│  └─────┬────┘    └──────┬───────┘    └──────┬───────┘  │
│        │                │                    │          │
│  ┌─────┴────┐    ┌──────┴───────┐    ┌──────┴───────┐  │
│  │  Disable  │    │  Handle 404  │    │  Deactivate  │  │
│  │  Link     │    │  /410/301    │    │  Domain      │  │
│  └──────────┘    └──────────────┘    └──────────────┘  │
│                                                         │
│  ┌──────────┐    ┌──────────────┐    ┌──────────────┐  │
│  │  Get Link │    │  Search      │    │  Monitor     │  │
│  │  Details  │    │  Links       │    │  Health      │  │
│  └──────────┘    └──────────────┘    └──────────────┘  │
└─────────────────────────────────────────────────────────┘
     ▲                    ▲                    ▲
     │                    │                    │
┌────┴─────┐    ┌────────┴──────┐    ┌───────┴────────┐
│ P1: API  │    │ P3: Support   │    │ P2: Platform   │
│ Consumer │    │ Agent         │    │ Admin          │
└──────────┘    └───────────────┘    └────────────────┘
                                              
              ┌───────────────┐
              │ P4: End User  │──── Clicks Short Links
              └───────────────┘
```
