# Shortly — Context Map

## Bounded Context Relationships

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         SHORTLY PLATFORM                                     │
│                                                                              │
│  ┌────────────────────┐         ┌────────────────────┐                      │
│  │                    │  [CS]   │                    │                      │
│  │  Domain Registry   │────────▶│  Link Management   │                      │
│  │   (Supporting)     │         │      (Core)        │                      │
│  │                    │         │                    │                      │
│  └────────────────────┘         └─────────┬──────────┘                      │
│                                           │                                  │
│                                    [PL]   │  Published Language              │
│                                    (shared read model)                        │
│                                           │                                  │
│                                           ▼                                  │
│                                 ┌────────────────────┐                      │
│                                 │                    │                      │
│                                 │     Redirect       │                      │
│                                 │      (Core)        │                      │
│                                 │                    │                      │
│                                 └─────────┬──────────┘                      │
│                                           │                                  │
│                                    [ED]   │  Event-Driven                    │
│                                           │                                  │
│                                           ▼                                  │
│                                 ┌────────────────────┐                      │
│                                 │                    │                      │
│                                 │    Analytics       │                      │
│                                 │    (Generic)       │                      │
│                                 │    [FUTURE]        │                      │
│                                 │                    │                      │
│                                 └────────────────────┘                      │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘

Legend:
  [CS]  = Customer/Supplier (Domain Registry supplies prefixes to Link Management)
  [PL]  = Published Language (shared DynamoDB read model)
  [ED]  = Event-Driven (async domain events via Azure Service Bus)
```

---

## Relationship Details

### Domain Registry → Link Management (Customer/Supplier)

| Aspect | Detail |
|--------|--------|
| **Pattern** | Customer/Supplier |
| **Direction** | Domain Registry (Upstream) → Link Management (Downstream) |
| **Mechanism** | Synchronous lookup via MongoDB `domains` collection |
| **Data Exchanged** | Domain prefix, active status |
| **Contract** | Link Management validates prefix exists and is active before link creation |
| **SLA** | <5ms lookup (MongoDB indexed query) |

**Rationale:** Link Management depends on valid prefixes. Domain Registry is the authority on prefix definitions. Simple lookup — no need for Anti-Corruption Layer.

---

### Link Management → Redirect (Published Language)

| Aspect | Detail |
|--------|--------|
| **Pattern** | Published Language (Shared Kernel — read model) |
| **Direction** | Link Management (Upstream/Writer) → Redirect (Downstream/Reader) |
| **Mechanism** | Shared MongoDB `links` collection — Link Management writes, Redirect reads |
| **Data Exchanged** | Short code, domain prefix, destination URL, status, expiry |
| **Contract** | MongoDB collection schema is the published language |
| **SLA** | Immediate consistency (single collection, same replica set) |

**Rationale:** Pragmatic choice. A separate event-sourced projection would add unnecessary complexity for a simple indexed lookup. Both services access the same MongoDB collection with clear read/write responsibilities.

**Trade-off:** Tight coupling at the data layer. Acceptable because:
1. Both contexts are owned by the same team
2. The read model is trivial (single indexed find)
3. Schema changes are infrequent and backwards-compatible

---

### Redirect → Analytics (Event-Driven)

| Aspect | Detail |
|--------|--------|
| **Pattern** | Event-Driven (fire-and-forget) |
| **Direction** | Redirect (Upstream/Publisher) → Analytics (Downstream/Consumer) |
| **Mechanism** | Azure Service Bus topic |
| **Data Exchanged** | `LinkRedirected` events |
| **Contract** | Event schema (see events catalog) |
| **SLA** | Best-effort delivery. Analytics is non-critical — redirect must not be blocked |

**Rationale:** High-volume event stream. Analytics failure must never impact redirect latency. Service Bus provides durability and decoupling.

---

### Link Management → Analytics (Event-Driven)

| Aspect | Detail |
|--------|--------|
| **Pattern** | Event-Driven |
| **Direction** | Link Management (Publisher) → Analytics (Consumer) |
| **Mechanism** | Azure Service Bus topic |
| **Data Exchanged** | `LinkCreated`, `LinkDisabled`, `LinkExpired` events |
| **Contract** | Event schemas (see events catalog) |

---

## Interaction Matrix

| | Domain Registry | Link Management | Redirect | Analytics |
|---|---|---|---|---|
| **Domain Registry** | — | CS (supplies prefixes) | — | — |
| **Link Management** | Consumes events | — | PL (shared table) | ED (publishes) |
| **Redirect** | — | PL (reads table) | — | ED (publishes) |
| **Analytics** | — | ED (consumes) | ED (consumes) | — |

---

## Integration Technology

| Concern | Technology | Justification |
|---------|-----------|---------------|
| Data Store | MongoDB (Azure Cosmos DB for MongoDB API or MongoDB Atlas) | Sub-5ms indexed reads, flexible document model, TTL indexes |
| Async Messaging | Azure Service Bus | Reliable delivery, topics/subscriptions, dead-letter |
| API Gateway | Azure API Management | Rate limiting, auth, routing |
| Compute | Azure Container Apps | Serverless scaling, .NET 8, cost-efficient |
