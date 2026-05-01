# ADR-004: Domain Prefix URL Strategy

## Status
COMPLETE

## Impact
MEDIUM

## Context

Shortly must encode business domain context into the shortened URL while keeping total link length minimal. The format needs to be unambiguous, URL-safe, and meaningful to operators/support staff who see these links in logs.

## Problem Statement

How should domain context be encoded in the short URL path to balance minimal length with business traceability?

## Options

### Option A: Path Prefix — `/{prefix}/{code}`
- **Description:** Domain prefix as first path segment, code as second. E.g., `https://sh.rt/ho/a3Bx9`
- **Pros:** Clear separation, prefix is human-readable, easy to route/filter, easy regex parsing
- **Cons:** One extra `/` character and prefix adds 3-5 chars to URL
- **Estimated Cost:** SMALL

### Option B: Code Prefix — `/{prefix}{code}` (concatenated)
- **Description:** Prefix and code merged into single path segment. E.g., `https://sh.rt/hoa3Bx9`
- **Pros:** Slightly shorter (saves one `/`), single path segment
- **Cons:** Ambiguous boundary (is `ho` the prefix or part of code?), harder to parse, needs fixed-width prefix
- **Estimated Cost:** SMALL

### Option C: Subdomain — `{prefix}.sh.rt/{code}`
- **Description:** Domain prefix as subdomain. E.g., `https://ho.sh.rt/a3Bx9`
- **Pros:** Clean separation, code path is pure short code
- **Cons:** DNS wildcard required, SSL cert complexity, longer total URL, mobile rendering issues
- **Estimated Cost:** MEDIUM

### Option D: No Domain Context in URL
- **Description:** Domain is metadata only, not in URL. E.g., `https://sh.rt/a3Bx9`
- **Pros:** Shortest possible URL
- **Cons:** Loses business context requirement, support cannot identify domain from URL
- **Estimated Cost:** SMALL

## Decision

**Option A: Path Prefix — `/{prefix}/{code}`**

Format: `https://{base-domain}/{domain-prefix}/{short-code}`
Example: `https://sh.rt/ho/a3Bx9`

- Domain prefix: 2-4 lowercase alphanumeric characters
- Short code: 5-8 Base62 characters
- Total path length: 8-13 characters (minimal for SMS)
- Clear, unambiguous, easy to parse: split on `/`

## Consequences

- **Easier:** Log filtering by prefix, routing rules, human readability
- **Easier:** Prefix acts as namespace — short codes only need uniqueness within their prefix
- **Harder:** One extra character (`/`) compared to concatenated approach
- **Trade-off:** 1 byte cost per link for clarity — acceptable given the alternative parsing ambiguity

## Action Items

- [ ] Document URL format in API specification
- [ ] Implement URL parsing in Redirect service
- [ ] Configure Azure API Management route: `GET /{prefix}/{code}`
- [ ] Add URL format validation in Link Management
