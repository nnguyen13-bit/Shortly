# ADR-003: MongoDB Collection Design

## Status
COMPLETE

## Impact
HIGH

## Context

Shortly uses MongoDB for its primary data store, chosen for low-latency indexed reads and flexible document modelling. The redirect path is extremely latency-sensitive (<100ms p99) and the write path handles 60K links/day. We need to decide on collection design strategy.

## Problem Statement

Should Shortly use a single MongoDB database with separate collections per entity type, or a single collection with discriminated documents, considering the access patterns and operational simplicity?

## Options

### Option A: Separate Collections Per Entity
- **Description:** Three collections: `links`, `domains`, `counters` in a single `shortly` database
- **Pros:** Clear separation, independent indexes per collection, easy to reason about, MongoDB best practice for distinct entities
- **Cons:** Cannot do cross-collection transactions without multi-document transactions (acceptable — rarely needed)
- **Estimated Cost:** SMALL

### Option B: Single Collection (Polymorphic Pattern)
- **Description:** All entities in one collection with a `type` discriminator field
- **Pros:** Single collection to manage, potential for single-shard locality
- **Cons:** Mixed index requirements, harder to reason about, indexes bloated by irrelevant documents
- **Estimated Cost:** SMALL

### Option C: Collection Per Bounded Context
- **Description:** Separate databases per bounded context with their own collections
- **Pros:** Maximum isolation, independent scaling per context
- **Cons:** Overkill for same-team, same-deployment contexts; connection pool overhead
- **Estimated Cost:** MEDIUM

## Decision

**Option A: Separate Collections Per Entity** in a single `shortly` database.

### Collection Design:

**`links` collection:**
- Document = one shortened link (aggregate)
- Unique compound index: `{ domainPrefix: 1, shortCode: 1 }`
- TTL index on `expiresAt` for automatic cleanup
- Secondary indexes for admin queries

**`domains` collection:**
- Document = one domain prefix registration
- Unique index: `{ prefix: 1 }`

**`counters` collection:**
- Document = one counter per domain prefix
- `_id` = prefix value (natural key, no extra index needed)
- Atomic `$inc` for code generation

### Index Strategy:

| Collection | Index | Type | Purpose |
|-----------|-------|------|---------|
| `links` | `{ domainPrefix: 1, shortCode: 1 }` | Unique | Redirect lookup + uniqueness |
| `links` | `{ status: 1, createdAt: 1 }` | Standard | Admin: find by status |
| `links` | `{ createdBy: 1, createdAt: 1 }` | Standard | Admin: find by creator |
| `links` | `{ expiresAt: 1 }` | TTL | Automatic expiry cleanup |
| `domains` | `{ prefix: 1 }` | Unique | Prefix lookup + uniqueness |
| `counters` | `_id` (default) | Unique | Counter by prefix |

## Consequences

- **Easier:** Clear mental model, one collection per aggregate type, standard MongoDB patterns
- **Easier:** Redirect is a single indexed find — sub-5ms with compound index
- **Easier:** TTL index handles link expiry automatically (no cron jobs)
- **Harder:** Multi-document transactions needed if cross-collection atomicity required (not needed for our patterns)
- **Trade-off:** Slightly more collections to manage vs single-collection complexity

## Action Items

- [ ] Create MongoDB database `shortly` with collections
- [ ] Define indexes in application startup (or IaC)
- [ ] Configure TTL index with appropriate expiry behaviour
- [ ] Set up MongoDB connection with appropriate read/write concerns
- [ ] Choose hosting: Azure Cosmos DB (MongoDB API) vs MongoDB Atlas on Azure
