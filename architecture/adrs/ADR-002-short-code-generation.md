# ADR-002: Short Code Generation Strategy

## Status
COMPLETE

## Impact
HIGH

## Context

Shortly must generate unique, minimal-length short codes at 60,000 links/day. Codes appear in SMS messages where every character costs money. The format is `/{domain-prefix}/{short-code}` — so the short code must be as compact as possible while remaining collision-free at scale.

## Problem Statement

How should we generate unique short codes that are minimal in length, URL-safe, and collision-free at 60K creates/day without coordination bottlenecks?

## Options

### Option A: Random Base62 (6 chars)
- **Description:** Generate random 6-character Base62 strings, check for collision before insert
- **Pros:** Simple, no central coordinator, uniform distribution
- **Cons:** Collision probability grows (~56.8B possibilities but birthday problem applies), requires check-and-retry
- **Estimated Cost:** SMALL

### Option B: Counter-Based Base62 Encoding
- **Description:** MongoDB atomic counter per domain prefix (`findOneAndUpdate` with `$inc`), Base62-encode the counter value
- **Pros:** Zero collisions guaranteed, predictable length growth, sequential (cache-friendly)
- **Cons:** Central counter is potential bottleneck, counter reveals link count
- **Estimated Cost:** SMALL

### Option C: Snowflake-style ID
- **Description:** Timestamp + machine ID + sequence number, Base62-encoded
- **Pros:** Distributed, no coordination, time-ordered
- **Cons:** Longer codes (10-12 chars), defeats "short as possible" requirement
- **Estimated Cost:** MEDIUM

### Option D: Pre-generated Code Pool
- **Description:** Background process pre-generates codes into a pool, services claim from pool
- **Pros:** Fast claim, no generation latency on request path
- **Cons:** Complex infrastructure, pool exhaustion risk, waste if codes expire unused
- **Estimated Cost:** MEDIUM

## Decision

**Option B: Counter-Based Base62 Encoding** with per-prefix counters and range pre-allocation.

- Each domain prefix has its own atomic counter in MongoDB (`counters` collection)
- Counter value is Base62-encoded to produce the short code
- 5 chars of Base62 = 916,132,832 possible codes per prefix (enough for 40+ years at 60K/day)
- Starting counter at 100,000,000 ensures consistent 5-char codes from day one (avoids 1-4 char codes that look odd)

**High-Throughput Optimisation: Range Pre-allocation**
- Each service instance claims a range (e.g., 1000 codes) via `$inc: 1000`
- Codes within the range are dispensed from in-memory without DB calls
- When the range is exhausted, claim the next range
- At 1000 req/s: only 1 MongoDB call/second instead of 1000
- Trade-off: if a service crashes, unused codes in the range are "wasted" (acceptable — code space is massive)

**Encoding:** `0-9` → 0-9, `a-z` → 10-35, `A-Z` → 36-61

## Consequences

- **Easier:** Zero collision handling, predictable performance, simple implementation
- **Easier:** Counter per prefix means no cross-prefix coordination
- **Harder:** Counter reveals approximate link creation order (acceptable — not a security concern for this use case)
- **Trade-off:** MongoDB `findOneAndUpdate` with `$inc` adds ~3-5ms to write path (negligible for write volume)

## Action Items

- [ ] Implement `IShortCodeGenerator` with MongoDB atomic counter (`$inc`)
- [ ] Seed counters at 100,000,000 for new domain prefixes
- [ ] Add integration test for concurrent code generation
- [ ] Monitor counter growth for capacity planning
