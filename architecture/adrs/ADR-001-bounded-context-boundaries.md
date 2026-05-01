# ADR-001: Bounded Context Boundaries

## Status
COMPLETE

## Impact
HIGH

## Context

Shortly is a URL shortening platform handling 60,000 links/day with strict requirements on link length minimisation and sub-100ms redirect latency. The system needs clear boundaries to enable independent scaling of the write path (link creation) vs read path (redirects).

## Problem Statement

How should we decompose the Shortly platform into bounded contexts to support independent scaling, clear ownership, and pragmatic DDD without over-engineering for a focused domain?

## Options

### Option A: Single Monolithic Context
- **Description:** All functionality in one bounded context
- **Pros:** Simple, no inter-service communication, easy to develop initially
- **Cons:** Cannot scale read/write independently, all changes coupled, harder to reason about
- **Estimated Cost:** SMALL (initial), LARGE (long-term scaling)

### Option B: Four Bounded Contexts (Link Management, Redirect, Domain Registry, Analytics)
- **Description:** Separate contexts by responsibility with clear read/write split
- **Pros:** Independent scaling (redirect can scale to millions), clear boundaries, future-proof
- **Cons:** More infrastructure, cross-context communication needed
- **Estimated Cost:** MEDIUM

### Option C: Two Contexts (Write Service + Read Service)
- **Description:** Simple CQRS split without domain modelling
- **Pros:** Simple, achieves scaling goal
- **Cons:** Domain Registry concerns mixed in, Analytics tightly coupled, less clear boundaries
- **Estimated Cost:** SMALL

## Decision

**Option B: Four Bounded Contexts** — but with pragmatic implementation:
- Link Management and Domain Registry can share a deployment unit initially (same service, separate namespaces)
- Redirect is a separate service (different scaling profile)
- Analytics is deferred to a future phase
- Shared DynamoDB table between Link Management and Redirect (Published Language pattern)

## Consequences

- **Easier:** Independent scaling of redirect (read-heavy) vs link creation (write)
- **Easier:** Adding Analytics later without touching core services
- **Harder:** Need to manage cross-context communication (events)
- **Trade-off:** Shared DynamoDB table creates data-layer coupling (acceptable — same team, simple schema)

## Action Items

- [ ] Scaffold .NET solution with separate projects per bounded context
- [ ] Set up DynamoDB single-table design
- [ ] Configure Azure Service Bus topics for domain events
