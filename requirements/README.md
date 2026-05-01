# Shortly — Product Requirements Documentation

## Document Overview

This PRD suite defines the product requirements for **Shortly**, a high-throughput URL shortening platform. Shortly transforms long URLs into minimal-length short links with embedded business domain context, optimised for use in SMS/messaging where every character adds cost.

**Intended Audience:** Developers, AI implementation agents, architects, and stakeholders.

---

## Document Structure

| # | Document | Description | Audience | Reading Time |
|---|----------|-------------|----------|-------------|
| 00 | [Executive Summary](00-ExecutiveSummary-KeyRequirements.md) | Business problem, solution overview, business rules catalogue, success metrics | All stakeholders | ~10 min |
| 01 | [Functional Requirements](01-Functional-Requirements.md) | Detailed system requirements by functional area with priorities and phases | Developers, architects | ~20 min |
| 02 | [User Stories & Use Cases](02-User-Stories-UseCases.md) | Personas, epics, user stories (BDD), and detailed use case scenarios | Stakeholders, sprint planning | ~15 min |
| 03 | [Acceptance Criteria & Test Scenarios](03-Acceptance-Criteria-TestScenarios.md) | Given/When/Then scenarios, edge cases, performance criteria | QA, developers | ~15 min |
| 04 | [Implementation Phases & Questions](04-Implementation-Phases-Questions.md) | Phased delivery plan, open questions, risk assessment | All | ~10 min |

---

## Quick Reference

### Key Metrics

| Metric | Target |
|--------|--------|
| Links created per day | 60,000 |
| Peak throughput | 1,000 req/s (burst) |
| Redirect latency (p99) | <100ms |
| Short link path length | 8–13 characters |
| Code space per prefix | 916M+ codes |

### Priority Framework

| Priority | Label | Definition |
|----------|-------|-----------|
| P0 | Must Have | Required for MVP — system cannot launch without it |
| P1 | Should Have | Important for production readiness — deliver in Phase 1 or 2 |
| P2 | Could Have | Enhances value — schedule after core is stable |
| P3 | Won't Have (now) | Acknowledged but deferred beyond current scope |

### Acronyms and Terms

| Term | Definition |
|------|-----------|
| Short Code | Base62-encoded unique identifier (5–8 chars) |
| Domain Prefix | 2–4 char business context code in URL path |
| Short Link | Full URL: `{base-url}/{domain-prefix}/{short-code}` |
| Destination URL | The original long URL to redirect to |
| Link | Aggregate: short code + prefix + destination |
| TTL | Time To Live — automatic expiry mechanism |
| CQRS | Command Query Responsibility Segregation |
| Base62 | Encoding using `[a-zA-Z0-9]` (62 characters) |

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-05-01 | AI-Assisted | Initial PRD generation from architecture docs |

---

## How to Use This PRD

| Role | Recommended Reading Order |
|------|--------------------------|
| **Developer** | Doc 01 (Functional Requirements) → Doc 03 (Acceptance Criteria) → Doc 04 (Phases) |
| **Architect** | Doc 00 (Executive Summary) → Doc 01 → Doc 04 |
| **Stakeholder** | Doc 00 → Doc 02 (User Stories) → Doc 04 |
| **AI Agent (/.implement)** | Doc 01 (phase-filtered) → Doc 03 (matching acceptance criteria) |

---

## Source Documents

This PRD was generated from:
- `project-config/CONTEXT.md` — Project context and domain rules
- `architecture/bounded-contexts/BOUNDED-CONTEXT-CANVAS.md` — Bounded context definitions
- `architecture/bounded-contexts/specs/data-model.md` — Domain model and MongoDB design
- `architecture/bounded-contexts/specs/events-catalog.md` — Domain events
- `architecture/bounded-contexts/CONTEXT-MAP.md` — Integration patterns
- `architecture/adrs/ADR-001` through `ADR-005` — Architectural decisions

---

## Next Steps

After PRD review and approval:
1. Run `/.implement` to scaffold .NET 10 solution from Phase 1 requirements
2. Use Doc 03 acceptance criteria to drive TDD implementation
3. Validate with `/.architecture-compliance` after each feature
