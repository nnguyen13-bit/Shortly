# Shortly — Data Model

## Link Management Context

### Aggregate: Link

The primary aggregate root for the URL shortening domain.

| Attribute | Value |
|-----------|-------|
| **Identity** | `LinkId` (Guid) |
| **Lookup Key** | Compound index on `{DomainPrefix, ShortCode}` (unique) |
| **Consistency Boundary** | Single link with its metadata |

#### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Id` | `LinkId` (Guid) | Yes | Internal unique identifier |
| `ShortCode` | `ShortCode` (Value Object) | Yes | The Base62-encoded short identifier (5-8 chars) |
| `DomainPrefix` | `DomainPrefix` (Value Object) | Yes | Business context prefix (2-4 chars) |
| `DestinationUrl` | `DestinationUrl` (Value Object) | Yes | The target URL to redirect to |
| `CreatedAt` | `DateTimeOffset` | Yes | UTC timestamp of creation |
| `ExpiresAt` | `DateTimeOffset?` | No | Optional expiry timestamp |
| `Status` | `LinkStatus` (Enum) | Yes | Active, Expired, Disabled |
| `CreatedBy` | `string` | Yes | Identifier of the creating system/user |
| `Metadata` | `LinkMetadata` (Value Object) | No | Optional key-value pairs for business context |

#### Invariants

| ID | Rule | Enforced By |
|----|------|-------------|
| DOM-001 | Short code must be unique within its domain prefix | Link aggregate + MongoDB unique compound index |
| DOM-002 | Short code must be 5-8 Base62 characters | `ShortCode` value object |
| DOM-003 | Domain prefix must be 2-4 lowercase alphanumeric characters | `DomainPrefix` value object |
| DOM-004 | Destination URL must be a valid absolute HTTP/HTTPS URL | `DestinationUrl` value object |
| DOM-005 | Link cannot be created with a past expiry date | Link aggregate |
| DOM-006 | Expired links cannot be reactivated | Link aggregate |

#### Behaviours

| Method | Command | Effects | Events |
|--------|---------|---------|--------|
| `Create()` | CreateLink | Generates short code, validates uniqueness, persists | `LinkCreated` |
| `Disable()` | DisableLink | Sets status to Disabled | `LinkDisabled` |
| `Expire()` | — (scheduled) | Sets status to Expired when ExpiresAt reached | `LinkExpired` |

---

### Value Objects

#### ShortCode

| Property | Type | Constraints |
|----------|------|-------------|
| `Value` | `string` | 5-8 chars, Base62 only (`[a-zA-Z0-9]`) |

**Validation:** Must match regex `^[a-zA-Z0-9]{5,8}$`

**Generation Strategy:** Counter-based encoding with Base62 conversion. A distributed counter (MongoDB `findOneAndUpdate` with `$inc`) provides sequential IDs that are Base62-encoded to produce short, unique codes.

---

#### DomainPrefix

| Property | Type | Constraints |
|----------|------|-------------|
| `Value` | `string` | 2-4 chars, lowercase alphanumeric only |

**Validation:** Must match regex `^[a-z0-9]{2,4}$`

**Examples:** `ho` (handover order), `inv` (invoice), `del` (delivery), `gen` (general)

---

#### DestinationUrl

| Property | Type | Constraints |
|----------|------|-------------|
| `Value` | `string` | Valid absolute URI, HTTP or HTTPS scheme, max 2048 chars |

**Validation:**
- Must parse as valid `Uri` with `UriKind.Absolute`
- Scheme must be `http` or `https`
- Length ≤ 2048 characters
- No JavaScript or data URIs (security)

---

#### LinkMetadata

| Property | Type | Constraints |
|----------|------|-------------|
| `Tags` | `Dictionary<string, string>` | Max 10 entries, key max 50 chars, value max 200 chars |

**Purpose:** Allows consuming systems to attach business context (e.g., `orderId`, `customerId`) without polluting the core model.

---

### Enums

#### LinkStatus

| Value | Description |
|-------|-------------|
| `Active` | Link is live and will redirect |
| `Expired` | Link has passed its expiry date |
| `Disabled` | Link was manually deactivated |

---

## Domain Registry Context

### Aggregate: Domain

| Attribute | Value |
|-----------|-------|
| **Identity** | `DomainId` (Guid) |
| **Lookup Key** | Unique index on `Prefix` |

#### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Id` | `DomainId` (Guid) | Yes | Internal identifier |
| `Prefix` | `DomainPrefix` (Value Object) | Yes | The URL path prefix |
| `Name` | `string` | Yes | Human-readable name (e.g., "Handover Order") |
| `Description` | `string` | No | Extended description of this domain's purpose |
| `IsActive` | `bool` | Yes | Whether new links can use this prefix |
| `CreatedAt` | `DateTimeOffset` | Yes | When the domain was registered |

#### Invariants

| ID | Rule |
|----|------|
| DOM-010 | Prefix must be globally unique |
| DOM-011 | Prefix cannot be changed after creation (immutable identity) |
| DOM-012 | Domain cannot be deactivated while active links exist (soft constraint — warning only) |

#### Behaviours

| Method | Effects | Events |
|--------|---------|--------|
| `Register()` | Creates new domain prefix | `DomainRegistered` |
| `Deactivate()` | Marks domain inactive | `DomainDeactivated` |

---

## Redirect Context (Read Model)

The Redirect context does **not** own an aggregate — it reads from the MongoDB `links` collection optimised with a compound index.

### Read Model: LinkLookup

| Field | Type | Description |
|-------|------|-------------|
| `domainPrefix` | `string` | Domain prefix (part of compound index) |
| `shortCode` | `string` | Short code (part of compound index) |
| `destinationUrl` | `string` | Target URL |
| `status` | `string` | Active/Expired/Disabled |
| `expiresAt` | `DateTime?` | Optional expiry (for TTL index) |

**Access Pattern:** `Find({domainPrefix, shortCode})` using compound index — O(1), sub-5ms latency.

---

## MongoDB Collection Design

### Database: `shortly`

### Collection: `links`

```json
{
  "_id": "ObjectId",
  "linkId": "guid",
  "domainPrefix": "ho",
  "shortCode": "a3Bx9",
  "destinationUrl": "https://example.com/very/long/url",
  "status": "Active",
  "createdAt": "ISODate",
  "expiresAt": "ISODate | null",
  "createdBy": "system-name",
  "metadata": { "orderId": "12345" }
}
```

**Indexes:**
- `{ domainPrefix: 1, shortCode: 1 }` — **unique compound** (redirect lookup + uniqueness)
- `{ status: 1, createdAt: 1 }` — admin queries by status
- `{ createdBy: 1, createdAt: 1 }` — per-system queries
- `{ expiresAt: 1 }` — **TTL index** (automatic document expiry)

### Collection: `domains`

```json
{
  "_id": "ObjectId",
  "domainId": "guid",
  "prefix": "ho",
  "name": "Handover Order",
  "description": "Links for customer handover orders",
  "isActive": true,
  "createdAt": "ISODate"
}
```

**Indexes:**
- `{ prefix: 1 }` — **unique** (prefix lookup + uniqueness)

### Collection: `counters`

```json
{
  "_id": "ho",
  "currentValue": 100000042
}
```

**Access Pattern:** `findOneAndUpdate({ _id: prefix }, { $inc: { currentValue: 1 } }, { upsert: true, returnDocument: 'after' })` — atomic increment, <5ms.

**Notes:**
- Counter `_id` is the domain prefix itself (natural key)
- Seeded at 100,000,000 for consistent 5-char Base62 codes
- `$inc` is atomic in MongoDB — safe for concurrent writes
