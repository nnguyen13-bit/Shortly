# Shortly — Events Catalog

## Overview

Events follow a lightweight domain event pattern. For inter-service communication via Azure Service Bus, events are serialised as JSON with a standard envelope.

---

## Event Envelope

```json
{
  "eventId": "uuid",
  "eventType": "Shortly.LinkManagement.LinkCreated",
  "source": "shortly/link-management",
  "timestamp": "2026-05-01T12:00:00Z",
  "data": { ... }
}
```

---

## Published Events

### Link Management Context

#### LinkCreated

| Attribute | Value |
|-----------|-------|
| **Trigger** | `CreateLink` command completes successfully |
| **Publisher** | Link Management Service |
| **Consumers** | Analytics (future) |

**Schema:**
```json
{
  "linkId": "guid",
  "shortCode": "string",
  "domainPrefix": "string",
  "destinationUrl": "string",
  "createdBy": "string",
  "createdAt": "datetime",
  "expiresAt": "datetime | null"
}
```

---

#### LinkDisabled

| Attribute | Value |
|-----------|-------|
| **Trigger** | `DisableLink` command completes |
| **Publisher** | Link Management Service |
| **Consumers** | Redirect (cache invalidation), Analytics (future) |

**Schema:**
```json
{
  "linkId": "guid",
  "shortCode": "string",
  "domainPrefix": "string",
  "disabledAt": "datetime",
  "reason": "string | null"
}
```

---

#### LinkExpired

| Attribute | Value |
|-----------|-------|
| **Trigger** | Scheduled expiry process or DynamoDB TTL |
| **Publisher** | Link Management Service |
| **Consumers** | Redirect (cache invalidation), Analytics (future) |

**Schema:**
```json
{
  "linkId": "guid",
  "shortCode": "string",
  "domainPrefix": "string",
  "expiredAt": "datetime"
}
```

---

### Domain Registry Context

#### DomainRegistered

| Attribute | Value |
|-----------|-------|
| **Trigger** | `RegisterDomain` command completes |
| **Publisher** | Domain Registry Service |
| **Consumers** | Link Management (prefix cache refresh) |

**Schema:**
```json
{
  "domainId": "guid",
  "prefix": "string",
  "name": "string",
  "registeredAt": "datetime"
}
```

---

#### DomainDeactivated

| Attribute | Value |
|-----------|-------|
| **Trigger** | `DeactivateDomain` command completes |
| **Publisher** | Domain Registry Service |
| **Consumers** | Link Management (prevent new links with this prefix) |

**Schema:**
```json
{
  "domainId": "guid",
  "prefix": "string",
  "deactivatedAt": "datetime"
}
```

---

### Redirect Context

#### LinkRedirected

| Attribute | Value |
|-----------|-------|
| **Trigger** | Successful redirect (HTTP 301/302 returned) |
| **Publisher** | Redirect Service |
| **Consumers** | Analytics (future) |

**Schema:**
```json
{
  "shortCode": "string",
  "domainPrefix": "string",
  "destinationUrl": "string",
  "redirectedAt": "datetime",
  "userAgent": "string | null",
  "ipCountry": "string | null"
}
```

**Note:** This is a high-volume event (~60K+ per day). Published asynchronously to avoid impacting redirect latency. Fire-and-forget pattern with Azure Service Bus.

---

## Consumed Events

### Link Management Context

| Event | Source | Handler | Effect |
|-------|--------|---------|--------|
| `DomainRegistered` | Domain Registry | `DomainRegisteredHandler` | Refresh local prefix cache |
| `DomainDeactivated` | Domain Registry | `DomainDeactivatedHandler` | Mark prefix as unavailable for new links |

### Redirect Context

| Event | Source | Handler | Effect |
|-------|--------|---------|--------|
| `LinkDisabled` | Link Management | `LinkStatusChangedHandler` | Invalidate cached redirect entry |
| `LinkExpired` | Link Management | `LinkStatusChangedHandler` | Invalidate cached redirect entry |

### Analytics Context (Future)

| Event | Source | Handler | Effect |
|-------|--------|---------|--------|
| `LinkCreated` | Link Management | `LinkCreatedHandler` | Initialise click counter |
| `LinkRedirected` | Redirect | `RedirectTrackedHandler` | Increment click metrics |

---

## Event Flow Diagram

```
Domain Registry                Link Management                 Redirect                    Analytics
     │                              │                            │                           │
     │──DomainRegistered──────────▶│                            │                           │
     │──DomainDeactivated─────────▶│                            │                           │
     │                              │                            │                           │
     │                              │──LinkCreated──────────────────────────────────────────▶│
     │                              │──LinkDisabled────────────▶│                           │
     │                              │──LinkDisabled────────────────────────────────────────▶│
     │                              │──LinkExpired─────────────▶│                           │
     │                              │──LinkExpired─────────────────────────────────────────▶│
     │                              │                            │                           │
     │                              │                            │──LinkRedirected─────────▶│
     │                              │                            │                           │
```
