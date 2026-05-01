# Shortly — Bounded Context Canvas

## Context Overview

| Attribute | Value |
|-----------|-------|
| **Name** | Shortly URL Shortening Platform |
| **Purpose** | Transform long URLs into minimal-length short links with domain context, and redirect users with sub-100ms latency |
| **Domain Type** | Core |
| **Business Model** | Operational Efficiency (reduces SMS/message costs via shorter URLs) |
| **Evolution** | Custom-Built |
| **DDD Rigor** | PRAGMATIC |

---

## Bounded Contexts

### 1. Link Management (Core)

| Attribute | Value |
|-----------|-------|
| **Purpose** | Create, manage, and expire shortened links |
| **Classification** | Core Domain |
| **Owner** | Shortly Team |
| **Upstream** | Domain Registry |
| **Downstream** | Redirect, Analytics |

**Responsibilities:**
- Generate unique, minimal-length short codes
- Associate short codes with destination URLs
- Enforce domain prefix rules (e.g., `/ho/` for handover orders)
- Manage link lifecycle (active, expired, disabled)
- Validate destination URLs
- Ensure collision-free code generation at 60K links/day

**Out of Scope:**
- Redirect resolution (belongs to Redirect context)
- Click tracking/analytics (belongs to Analytics context)
- Domain prefix CRUD (belongs to Domain Registry context)

---

### 2. Redirect (Core)

| Attribute | Value |
|-----------|-------|
| **Purpose** | Resolve short codes to destination URLs with minimal latency |
| **Classification** | Core Domain |
| **Owner** | Shortly Team |
| **Upstream** | Link Management |
| **Downstream** | Analytics |

**Responsibilities:**
- Resolve `{domain-prefix}/{short-code}` to destination URL
- Return HTTP 301/302 redirects
- Handle expired/disabled links gracefully (404/410)
- Publish redirect events for analytics consumption
- Optimise for <100ms p99 latency

**Out of Scope:**
- Link creation or modification
- Click aggregation or reporting
- Domain prefix management

---

### 3. Domain Registry (Supporting)

| Attribute | Value |
|-----------|-------|
| **Purpose** | Manage business domain prefixes that provide context in short URLs |
| **Classification** | Supporting Domain |
| **Owner** | Shortly Team |
| **Upstream** | None (root context) |
| **Downstream** | Link Management |

**Responsibilities:**
- Register domain prefixes (e.g., `ho` = Handover Order, `inv` = Invoice)
- Validate prefix uniqueness and length constraints
- Provide prefix lookup for link creation
- Maintain prefix-to-business-domain mapping

**Out of Scope:**
- Link creation (uses prefix but doesn't own it)
- Redirect logic

---

### 4. Analytics (Generic — Future)

| Attribute | Value |
|-----------|-------|
| **Purpose** | Track and report on link usage metrics |
| **Classification** | Generic/Supporting (future phase) |
| **Owner** | Shortly Team |
| **Upstream** | Redirect |
| **Downstream** | None |

**Responsibilities:**
- Consume redirect events
- Aggregate click counts per link
- Track geographic/temporal patterns
- Provide reporting APIs

**Out of Scope:**
- Real-time redirect performance (belongs to Redirect)
- Link management operations

---

## Ubiquitous Language

| Term | Definition |
|------|-----------|
| **Short Code** | The minimal-length Base62-encoded unique identifier for a link (e.g., `a3Bx9k`) |
| **Domain Prefix** | A 2-4 character code representing a business domain/context (e.g., `ho` for handover order) |
| **Short Link** | The complete shortened URL: `{base-url}/{domain-prefix}/{short-code}` |
| **Destination URL** | The original long URL that the short link redirects to |
| **Link** | The aggregate combining short code, domain prefix, and destination URL |
| **Redirect** | The act of resolving a short link and sending the user to the destination |
| **Domain** | A business context category (not DNS domain) that groups related links |
| **Expiry** | The point at which a link becomes inactive and returns 410 Gone |
| **Collision** | When a generated short code already exists (must be prevented) |

---

## Strategic Classification

```
┌─────────────────────────────────────────────────────────┐
│                    SHORTLY PLATFORM                       │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  ┌──────────────────┐    ┌──────────────────┐           │
│  │ Link Management  │───▶│    Redirect      │           │
│  │     (CORE)       │    │     (CORE)       │           │
│  └────────┬─────────┘    └────────┬─────────┘           │
│           │                       │                      │
│           │                       ▼                      │
│  ┌────────┴─────────┐    ┌──────────────────┐           │
│  │ Domain Registry  │    │   Analytics      │           │
│  │  (SUPPORTING)    │    │   (GENERIC)      │           │
│  └──────────────────┘    │   [FUTURE]       │           │
│                          └──────────────────┘           │
└─────────────────────────────────────────────────────────┘
```

---

## Integration Patterns

| Relationship | Pattern | Mechanism |
|-------------|---------|-----------|
| Domain Registry → Link Management | Customer/Supplier | Synchronous lookup (prefix validation) |
| Link Management → Redirect | Published Language | MongoDB shared read model (eventually consistent) |
| Redirect → Analytics | Event-Driven | Domain events via Azure Service Bus |
| Link Management → Analytics | Event-Driven | Domain events via Azure Service Bus |

---

## Key Architectural Decisions

- **Shared Read Model**: Redirect context reads directly from MongoDB (same collection as Link Management writes). This is a deliberate pragmatic trade-off — avoids event-sourcing complexity for a simple key-value lookup.
- **Domain Prefix in URL Path**: Format is `/{prefix}/{code}` keeping total path minimal while preserving business context.
- **Base62 Encoding**: Characters `[a-zA-Z0-9]` provide URL-safe codes without encoding overhead.
- **MongoDB**: Chosen for low-latency reads with flexible document model — compound index on `{prefix, shortCode}` provides sub-5ms lookups.
