# 00 — Executive Summary and Key Requirements

## Executive Summary

### Business Problem

Organisations send millions of messages daily — SMS, email, push notifications — containing URLs that link customers to orders, invoices, deliveries, and other business transactions. These URLs are often 100+ characters long, consuming valuable message space, increasing SMS costs (longer messages require multiple segments at higher cost), and creating poor user experiences with unwieldy links.

Current solutions like Bitly provide generic shortening but lack the ability to embed **business domain context** into the link itself. When a support agent or developer sees a short link in logs, they cannot immediately identify whether it relates to a handover order, an invoice, or a delivery notification. This forces costly lookups and slows incident response.

### Solution Overview

**Shortly** is a high-throughput URL shortening platform that produces minimal-length short links with embedded business domain context. Each link follows the format `{base-url}/{domain-prefix}/{short-code}` — for example, `https://sh.rt/ho/a3Bx9` where `ho` identifies the link as belonging to the "Handover Order" domain.

The platform is designed to handle 60,000 link creations per day with burst capacity of 1,000 requests per second, while redirecting users in under 100 milliseconds (p99). It is built as a set of microservices on .NET 8, Azure Container Apps, and MongoDB.

### Key Benefits

- **Cost Reduction:** Shorter links mean shorter SMS messages — fewer multi-segment messages, lower messaging costs
- **Operational Clarity:** Domain prefix in every link enables instant identification of business context without database lookups
- **Scale:** Counter-based code generation with range pre-allocation handles 1,000+ req/s with zero collisions
- **Simplicity:** Internal microservices integrate via a single REST API call to create links; redirect is a sub-5ms MongoDB indexed lookup

---

## Core Business Concepts

### Entity Model

| Concept | Type | Description |
|---------|------|-------------|
| **Link** | Aggregate Root | The core entity: combines a short code, domain prefix, and destination URL |
| **Short Code** | Value Object | A 5–8 character Base62-encoded identifier, unique within its domain prefix |
| **Domain Prefix** | Value Object | A 2–4 character lowercase code representing a business context (e.g., `ho`, `inv`) |
| **Destination URL** | Value Object | The original URL to redirect to (HTTP/HTTPS, max 2048 chars) |
| **Domain** | Aggregate Root | A registered business domain with its prefix, name, and active status |
| **Redirect Event** | Domain Event | Published when a short link is successfully resolved and redirected |

### URL Format

```
https://sh.rt/ho/a3Bx9
         │    │   │
         │    │   └─ Short Code (5 chars, Base62)
         │    └───── Domain Prefix ("ho" = Handover Order)
         └────────── Base Domain
```

**Total path length:** 8–13 characters (2–4 prefix + `/` + 5–8 code)

### Short Code Generation

Codes are generated using a **counter-based Base62 encoding** strategy:
- Each domain prefix has its own atomic counter in MongoDB
- Counter starts at 100,000,000 (ensures consistent 5-character codes from day one)
- Range pre-allocation: service instances claim batches of 1,000 codes at once, dispensing from memory
- Zero collision guarantee — no check-and-retry needed

---

## Business Rules Catalogue

### Link Creation Rules

| ID | Rule | Priority |
|----|------|----------|
| BR-LNK-001 | Short code shall be unique within its domain prefix | P0 |
| BR-LNK-002 | Short code shall be 5–8 Base62 characters (`[a-zA-Z0-9]`) | P0 |
| BR-LNK-003 | Domain prefix shall be 2–4 lowercase alphanumeric characters (`[a-z0-9]`) | P0 |
| BR-LNK-004 | Destination URL shall be a valid absolute HTTP or HTTPS URL, max 2048 characters | P0 |
| BR-LNK-005 | Destination URL shall not contain `javascript:` or `data:` URI schemes (security) | P0 |
| BR-LNK-006 | Link cannot be created with an expiry date in the past | P0 |
| BR-LNK-007 | Domain prefix must reference an active, registered domain | P0 |
| BR-LNK-008 | Link metadata tags are limited to 10 entries, key max 50 chars, value max 200 chars | P1 |

### Link Lifecycle Rules

| ID | Rule | Priority |
|----|------|----------|
| BR-LFC-001 | Expired links shall return HTTP 410 Gone | P0 |
| BR-LFC-002 | Disabled links shall return HTTP 410 Gone | P0 |
| BR-LFC-003 | Expired links cannot be reactivated | P0 |
| BR-LFC-004 | Links with a TTL shall be automatically cleaned up by MongoDB TTL index | P1 |

### Redirect Rules

| ID | Rule | Priority |
|----|------|----------|
| BR-RDR-001 | Active links shall redirect with HTTP 301 (permanent) or 302 (temporary) | P0 |
| BR-RDR-002 | Redirect latency shall be <100ms at p99 | P0 |
| BR-RDR-003 | Unknown short codes shall return HTTP 404 Not Found | P0 |
| BR-RDR-004 | Redirect events shall be published asynchronously (fire-and-forget) to avoid blocking | P0 |

### Domain Registry Rules

| ID | Rule | Priority |
|----|------|----------|
| BR-DOM-001 | Domain prefix shall be globally unique | P0 |
| BR-DOM-002 | Domain prefix cannot be changed after creation (immutable) | P0 |
| BR-DOM-003 | Deactivated domains shall prevent creation of new links with that prefix | P1 |
| BR-DOM-004 | Deactivation of a domain with existing active links shall generate a warning (soft constraint) | P2 |

---

## Success Metrics

### Quantitative KPIs

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Link Creation Throughput** | ≥60,000/day sustained; ≥1,000/s burst | Application metrics (requests/sec) |
| **Redirect Latency (p99)** | <100ms | Application Performance Monitoring |
| **Redirect Latency (p50)** | <20ms | APM |
| **System Availability** | 99.9% uptime | Health check monitoring |
| **Short Code Length** | 5 characters (for first 916M codes per prefix) | Code generation validation |
| **API Error Rate** | <0.1% for valid requests | Error rate monitoring |
| **MongoDB Read Latency** | <5ms for redirect lookups | Database metrics |

### Qualitative Indicators

- Internal services can integrate in under 1 hour (simple REST API)
- Support team can identify link context from URL without database lookup
- No short code collisions in production (zero tolerance)

---

## Example Scenarios

### Scenario 1: Handover Order Link Creation

An internal order management service needs to send a customer an SMS with a link to their handover order details.

1. Order service calls: `POST /api/links` with body `{ "domainPrefix": "ho", "destinationUrl": "https://orders.example.com/handover/ORD-2026-1234567/details?token=abc123" }`
2. Shortly validates the prefix `ho` is active (Domain Registry lookup)
3. Shortly generates short code `a3Bx9` (from counter: 100,000,001 → Base62)
4. Shortly persists link to MongoDB `links` collection
5. Shortly returns: `{ "shortLink": "https://sh.rt/ho/a3Bx9", "shortCode": "a3Bx9", "domainPrefix": "ho" }`
6. Order service includes `https://sh.rt/ho/a3Bx9` in the SMS (14 chars total path)

### Scenario 2: Customer Clicks Short Link

1. Customer receives SMS containing `https://sh.rt/ho/a3Bx9`
2. Customer clicks the link
3. Request arrives at Shortly's Redirect service: `GET /ho/a3Bx9`
4. Redirect service queries MongoDB: `find({ domainPrefix: "ho", shortCode: "a3Bx9" })`
5. MongoDB returns the link document in <5ms
6. Redirect service verifies status is `Active` and not expired
7. Redirect service returns HTTP 301 to `https://orders.example.com/handover/ORD-2026-1234567/details?token=abc123`
8. Redirect service publishes `LinkRedirected` event asynchronously
9. Customer's browser follows the redirect to the order details page

**Total redirect time:** <20ms typical, <100ms p99

---

## Questions and Ambiguities

| # | Question | Impact | Recommendation |
|---|----------|--------|----------------|
| Q1 | Should redirect use HTTP 301 (permanent/cached) or 302 (temporary)? | Affects browser caching and analytics accuracy | 302 for links with expiry; 301 for permanent links |
| Q2 | What is the base domain for short links? (e.g., `sh.rt`, `go.company.com`) | Affects total URL length | Defer — configurable per deployment |
| Q3 | Should link creation require authentication? | Security of the API | Yes — API key or OAuth for internal services |
| Q4 | Maximum TTL for a link? | Data retention and storage cost | Configurable, default 1 year, max 5 years |
| Q5 | Should analytics be in-scope for MVP? | Scope and timeline | No — defer to Phase 3 |
