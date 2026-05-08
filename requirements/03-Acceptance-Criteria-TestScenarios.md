# 03 — Acceptance Criteria and Test Scenarios

> **Cross-reference:** Business rules (BR-*) are in [Document 00](00-ExecutiveSummary-KeyRequirements.md). Functional requirements (FR-*) are in [Document 01](01-Functional-Requirements.md). User stories (US-*) are in [Document 02](02-User-Stories-UseCases.md).

---

## Organisation

Test scenarios are organised by feature area, matching the functional requirements structure. Each scenario uses **Given/When/Then** BDD format with explicit data values for unambiguous test implementation.

Priority tags:
- **[SMOKE]** — Critical path, must pass for deployment
- **[REGRESSION]** — Core functionality, run on every build
- **[EDGE]** — Edge case or boundary condition
- **[PERFORMANCE]** — Non-functional performance validation

---

## 1. Link Creation (FR-LNK-001)

### TC-LNK-001: Create link with valid inputs [SMOKE]

```gherkin
Given a registered active domain with prefix "ho"
And a destination URL "https://orders.example.com/handover/ORD-2026-1234567"
When I send POST /api/links with body:
  | domainPrefix   | ho                                                              |
  | destinationUrl | https://orders.example.com/handover/ORD-2026-1234567           |
Then the response status is 201 Created
And the response body contains:
  | field        | assertion                           |
  | shortLink    | matches pattern https://.+/ho/\w{5} |
  | shortCode    | is 5 Base62 characters [a-zA-Z0-9]  |
  | domainPrefix | equals "ho"                         |
  | linkId       | is a non-empty string               |
  | status       | equals "Active"                     |
```

### TC-LNK-002: Create link with invalid URL scheme [REGRESSION]

```gherkin
Given a registered active domain with prefix "ho"
When I send POST /api/links with body:
  | domainPrefix   | ho                       |
  | destinationUrl | javascript:alert('xss')  |
Then the response status is 400 Bad Request
And the response body contains error message referencing URL validation
```
**Validates:** BR-LNK-005

### TC-LNK-003: Create link with data: URI scheme [REGRESSION]

```gherkin
Given a registered active domain with prefix "ho"
When I send POST /api/links with body:
  | domainPrefix   | ho                              |
  | destinationUrl | data:text/html,<h1>test</h1>    |
Then the response status is 400 Bad Request
```
**Validates:** BR-LNK-005

### TC-LNK-004: Create link with URL exceeding max length [EDGE]

```gherkin
Given a registered active domain with prefix "ho"
And a destination URL of exactly 2049 characters
When I send POST /api/links
Then the response status is 400 Bad Request
And the error references maximum URL length of 2048 characters
```
**Validates:** BR-LNK-004

### TC-LNK-005: Create link with URL at exact max length [EDGE]

```gherkin
Given a registered active domain with prefix "ho"
And a destination URL of exactly 2048 characters (valid HTTPS URL)
When I send POST /api/links
Then the response status is 201 Created
```
**Validates:** BR-LNK-004 boundary

### TC-LNK-006: Create link with inactive domain [REGRESSION]

```gherkin
Given a domain with prefix "old" that has been deactivated
When I send POST /api/links with domainPrefix "old"
Then the response status is 400 Bad Request
And the error message indicates the domain is not active
```
**Validates:** BR-LNK-007, BR-DOM-003

### TC-LNK-007: Create link with unregistered domain [REGRESSION]

```gherkin
Given no domain is registered with prefix "zz"
When I send POST /api/links with domainPrefix "zz"
Then the response status is 400 Bad Request
And the error message indicates the domain prefix does not exist
```
**Validates:** BR-LNK-007

### TC-LNK-008: Create link with past expiry date [REGRESSION]

```gherkin
Given a registered active domain with prefix "ho"
When I send POST /api/links with expiresAt "2020-01-01T00:00:00Z"
Then the response status is 400 Bad Request
And the error message indicates expiry date cannot be in the past
```
**Validates:** BR-LNK-006

### TC-LNK-009: Create link with future expiry date [REGRESSION]

```gherkin
Given a registered active domain with prefix "ho"
And a valid destination URL
When I send POST /api/links with expiresAt 7 days from now
Then the response status is 201 Created
And the response body contains expiresAt matching the provided value
```

### TC-LNK-010: Create link with metadata [REGRESSION]

```gherkin
Given a registered active domain with prefix "ho"
And a valid destination URL
When I send POST /api/links with metadata:
  | key       | value                |
  | orderId   | ORD-2026-1234567     |
  | channel   | sms                  |
Then the response status is 201 Created
And the link details include the metadata tags
```
**Validates:** FR-LNK-001.7

### TC-LNK-011: Create link with too many metadata tags [EDGE]

```gherkin
Given a registered active domain with prefix "ho"
And a valid destination URL
When I send POST /api/links with 11 metadata tags
Then the response status is 400 Bad Request
And the error references maximum of 10 metadata tags
```
**Validates:** BR-LNK-008

### TC-LNK-012: Create link with metadata key exceeding max length [EDGE]

```gherkin
Given a registered active domain with prefix "ho"
When I send POST /api/links with a metadata key of 51 characters
Then the response status is 400 Bad Request
```
**Validates:** BR-LNK-008

### TC-LNK-013: Short code uniqueness under concurrent load [PERFORMANCE]

```gherkin
Given a registered active domain with prefix "ho"
When 1000 concurrent POST /api/links requests are sent for prefix "ho"
Then all 1000 requests succeed with status 201
And all 1000 returned short codes are unique
And no duplicate key errors occur in the database
```
**Validates:** BR-LNK-001, FR-LNK-005

### TC-LNK-014: Create link with HTTP (non-HTTPS) destination [REGRESSION]

```gherkin
Given a registered active domain with prefix "ho"
When I send POST /api/links with destinationUrl "http://example.com/page"
Then the response status is 201 Created
```
**Validates:** BR-LNK-004 (HTTP and HTTPS both valid)

### TC-LNK-015: Create link with missing required fields [REGRESSION]

```gherkin
When I send POST /api/links with body:
  | domainPrefix | ho |
Then the response status is 400 Bad Request
And the error message indicates destinationUrl is required
```

---

## 2. Disable Link (FR-LNK-002)

### TC-DIS-001: Disable an active link [SMOKE]

```gherkin
Given an active link with prefix "ho" and code "a3Bx9"
When I send DELETE /api/links/ho/a3Bx9
Then the response status is 200 OK
And the link status is now "Disabled"
And a LinkDisabled event is published
```

### TC-DIS-002: Disable an already disabled link (idempotent) [REGRESSION]

```gherkin
Given a disabled link with prefix "ho" and code "a3Bx9"
When I send DELETE /api/links/ho/a3Bx9
Then the response status is 200 OK
```

### TC-DIS-003: Disable a non-existent link [REGRESSION]

```gherkin
When I send DELETE /api/links/ho/ZZZZZ
Then the response status is 404 Not Found
```

---

## 3. Get Link Details (FR-LNK-004)

### TC-GET-001: Get details of an existing link [SMOKE]

```gherkin
Given an active link with prefix "ho", code "a3Bx9", destination "https://example.com"
When I send GET /api/links/ho/a3Bx9
Then the response status is 200 OK
And the response body contains:
  | field          | assertion                 |
  | shortCode      | equals "a3Bx9"           |
  | domainPrefix   | equals "ho"              |
  | destinationUrl | equals "https://example.com" |
  | status         | equals "Active"          |
  | createdAt      | is a valid ISO 8601 date |
```

### TC-GET-002: Get details of a non-existent link [REGRESSION]

```gherkin
When I send GET /api/links/ho/ZZZZZ
Then the response status is 404 Not Found
```

---

## 4. Redirect (FR-RDR-001 to FR-RDR-004)

### TC-RDR-001: Redirect active link [SMOKE]

```gherkin
Given an active link with prefix "ho", code "a3Bx9"
And destination URL "https://orders.example.com/ORD-123"
When I send GET /ho/a3Bx9
Then the response status is 301 Moved Permanently
And the Location header equals "https://orders.example.com/ORD-123"
And a LinkRedirected event is published asynchronously
```

### TC-RDR-002: Redirect non-existent link [SMOKE]

```gherkin
When I send GET /ho/ZZZZZ
Then the response status is 404 Not Found
And the response body does not contain stack traces
```
**Validates:** BR-RDR-003

### TC-RDR-003: Redirect expired link [REGRESSION]

```gherkin
Given a link with prefix "ho", code "b4Cx8" that expired 1 hour ago
When I send GET /ho/b4Cx8
Then the response status is 410 Gone
```
**Validates:** BR-LFC-001

### TC-RDR-004: Redirect disabled link [REGRESSION]

```gherkin
Given a disabled link with prefix "ho", code "c5Dx7"
When I send GET /ho/c5Dx7
Then the response status is 410 Gone
```
**Validates:** BR-LFC-002

### TC-RDR-005: Redirect latency under load [PERFORMANCE]

```gherkin
Given 10,000 active links in the database
When 500 concurrent redirect requests are sent
Then the p99 response time is less than 100ms
And the p50 response time is less than 20ms
And all responses are HTTP 301
```
**Validates:** BR-RDR-002

### TC-RDR-006: Redirect event failure does not block response [REGRESSION]

```gherkin
Given an active link with prefix "ho", code "a3Bx9"
And the Service Bus is temporarily unavailable
When I send GET /ho/a3Bx9
Then the response status is 301 Moved Permanently
And the redirect completes successfully despite event publishing failure
```
**Validates:** BR-RDR-004, FR-RDR-004.2

### TC-RDR-007: Redirect with invalid prefix format [EDGE]

```gherkin
When I send GET /INVALID_PREFIX/a3Bx9
Then the response status is 404 Not Found
```

### TC-RDR-008: Redirect with empty code [EDGE]

```gherkin
When I send GET /ho/
Then the response status is 404 Not Found
```

---

## 5. Domain Registry (FR-DOM-001 to FR-DOM-003)

### TC-DOM-001: Register a new domain [SMOKE]

```gherkin
Given no domain with prefix "inv" exists
When I send POST /api/domains with body:
  | prefix      | inv                    |
  | name        | Invoice                |
  | description | Invoice-related links  |
Then the response status is 201 Created
And the response body contains:
  | field    | assertion         |
  | prefix   | equals "inv"     |
  | name     | equals "Invoice" |
  | isActive | equals true      |
And a DomainRegistered event is published
```

### TC-DOM-002: Register domain with duplicate prefix [REGRESSION]

```gherkin
Given a domain with prefix "ho" already exists
When I send POST /api/domains with prefix "ho"
Then the response status is 409 Conflict
```
**Validates:** BR-DOM-001

### TC-DOM-003: Register domain with invalid prefix format [REGRESSION]

```gherkin
When I send POST /api/domains with prefix "HO" (uppercase)
Then the response status is 400 Bad Request
And the error references lowercase alphanumeric requirement
```
**Validates:** BR-LNK-003

### TC-DOM-004: Register domain with prefix too short [EDGE]

```gherkin
When I send POST /api/domains with prefix "a" (1 character)
Then the response status is 400 Bad Request
And the error references minimum length of 2 characters
```
**Validates:** BR-LNK-003

### TC-DOM-005: Register domain with prefix too long [EDGE]

```gherkin
When I send POST /api/domains with prefix "abcde" (5 characters)
Then the response status is 400 Bad Request
And the error references maximum length of 4 characters
```
**Validates:** BR-LNK-003

### TC-DOM-006: Register domain with prefix at boundary lengths [EDGE]

```gherkin
When I send POST /api/domains with prefix "ab" (2 characters)
Then the response status is 201 Created

When I send POST /api/domains with prefix "abcd" (4 characters)
Then the response status is 201 Created
```
**Validates:** BR-LNK-003 boundaries

### TC-DOM-007: List all domains [REGRESSION]

```gherkin
Given 3 active domains ("ho", "inv", "del") and 2 inactive domains ("old", "tmp")
When I send GET /api/domains
Then the response status is 200 OK
And 5 domains are returned
```

### TC-DOM-008: List active domains only [REGRESSION]

```gherkin
Given 3 active and 2 inactive domains
When I send GET /api/domains?active=true
Then the response status is 200 OK
And 3 domains are returned
And all returned domains have isActive = true
```

### TC-DOM-009: Deactivate a domain [REGRESSION]

```gherkin
Given an active domain with prefix "old"
When I send DELETE /api/domains/old
Then the response status is 200 OK
And the domain isActive is now false
And a DomainDeactivated event is published
```

### TC-DOM-010: Deactivate domain with active links [REGRESSION]

```gherkin
Given an active domain with prefix "old" and 500 active links
When I send DELETE /api/domains/old
Then the response status is 200 OK
And the response includes a warning about 500 active links
And existing links under "old" still redirect successfully
```
**Validates:** BR-DOM-003, BR-DOM-004

### TC-DOM-011: Create link after domain deactivation [REGRESSION]

```gherkin
Given a deactivated domain with prefix "old"
When I send POST /api/links with domainPrefix "old"
Then the response status is 400 Bad Request
```
**Validates:** BR-DOM-003

### TC-DOM-012: Deactivate non-existent domain [REGRESSION]

```gherkin
When I send DELETE /api/domains/zzz
Then the response status is 404 Not Found
```

---

## 6. Security (FR-SEC-001 to FR-SEC-003)

### TC-SEC-001: Management API requires authentication [SMOKE]

```gherkin
When I send POST /api/links without an API key or bearer token
Then the response status is 401 Unauthorised
```
**Validates:** FR-SEC-001

### TC-SEC-002: Redirect endpoint is public [SMOKE]

```gherkin
Given an active link with prefix "ho", code "a3Bx9"
When I send GET /ho/a3Bx9 without any authentication
Then the response status is 301 Moved Permanently
```
**Validates:** FR-SEC-001.2

### TC-SEC-003: Rate limit exceeded [REGRESSION]

```gherkin
Given a consumer with rate limit of 100 requests/second
When the consumer sends 150 requests within 1 second
Then the first 100 requests succeed
And subsequent requests receive HTTP 429 Too Many Requests
And the 429 response includes a Retry-After header
```
**Validates:** FR-SEC-003

### TC-SEC-004: Request body size limit [EDGE]

```gherkin
When I send POST /api/links with a request body of 65KB
Then the response status is 413 Payload Too Large
```
**Validates:** FR-SEC-002.3

---

## 7. Infrastructure (FR-INF-001 to FR-INF-004)

### TC-INF-001: Health check — healthy [SMOKE]

```gherkin
Given MongoDB is reachable
When I send GET /health/ready
Then the response status is 200 OK
```

### TC-INF-002: Health check — MongoDB unavailable [REGRESSION]

```gherkin
Given MongoDB is unreachable
When I send GET /health/ready
Then the response status is 503 Service Unavailable
```

### TC-INF-003: Liveness probe [SMOKE]

```gherkin
When I send GET /health/live
Then the response status is 200 OK
```

### TC-INF-004: Indexes created on startup [REGRESSION]

```gherkin
Given a fresh MongoDB database with no indexes
When the application starts
Then the links collection has a unique compound index on { domainPrefix: 1, shortCode: 1 }
And the links collection has a TTL index on { expiresAt: 1 }
And the domains collection has a unique index on { prefix: 1 }
```
**Validates:** FR-INF-002

### TC-INF-005: Circuit breaker opens after failures [REGRESSION]

```gherkin
Given MongoDB becomes unavailable
When 5 consecutive requests fail due to MongoDB timeout
Then the circuit breaker opens
And subsequent requests fail immediately without waiting for MongoDB timeout
And after 30 seconds, the circuit breaker enters half-open state
```
**Validates:** FR-INF-004

---

## 8. Event Publishing (FR-INF-003)

### TC-EVT-001: LinkCreated event published [REGRESSION]

```gherkin
Given a valid link creation request
When the link is successfully created
Then a LinkCreated event is published to Azure Service Bus
And the event contains linkId, domainPrefix, shortCode, and destinationUrl
```

### TC-EVT-002: DomainRegistered event published [REGRESSION]

```gherkin
Given a valid domain registration request
When the domain is successfully registered
Then a DomainRegistered event is published to Azure Service Bus
And the event contains domainId, prefix, and name
```

### TC-EVT-003: Event publishing failure is non-blocking [REGRESSION]

```gherkin
Given Azure Service Bus is temporarily unavailable
When a link is created
Then the link is created successfully (HTTP 201)
And the failure to publish the event is logged at ERROR level
```
**Validates:** FR-INF-003.3

---

## 9. Infrastructure as Code (FR-IAC-001 to FR-IAC-005)

### TC-IAC-001: Terraform plan on clean subscription [SMOKE]

```gherkin
Given a clean Azure subscription with no Shortly resources
And the dev.tfvars environment file
When I run "terraform plan -var-file=environments/dev.tfvars"
Then the plan succeeds with no errors
And the plan shows resources to be created:
  | resource                          |
  | azurerm_resource_group            |
  | azurerm_key_vault                 |
  | azurerm_container_registry        |
  | azurerm_servicebus_namespace      |
  | azurerm_servicebus_topic          |
  | azurerm_servicebus_subscription   |
  | azurerm_container_app_environment |
  | azurerm_container_app             |
```
**Validates:** FR-IAC-001

### TC-IAC-002: Terraform apply provisions all resources [SMOKE]

```gherkin
Given a clean Azure subscription
And the dev.tfvars environment file
When I run "terraform apply -var-file=environments/dev.tfvars -auto-approve"
Then the apply completes successfully
And all resources exist in the specified resource group
And all resources are tagged with project="shortly", environment="dev", managed-by="terraform"
```
**Validates:** FR-IAC-001.4

### TC-IAC-003: Terraform plan is idempotent [REGRESSION]

```gherkin
Given all Shortly resources have been provisioned via Terraform
When I run "terraform plan -var-file=environments/dev.tfvars"
Then the plan shows "No changes. Your infrastructure matches the configuration."
```
**Validates:** FR-IAC-001

### TC-IAC-004: Service Bus topic and subscription provisioned correctly [REGRESSION]

```gherkin
Given the Terraform configuration has been applied
When I inspect the Service Bus namespace
Then a topic named "shortly-events" exists with 7-day message TTL
And a subscription named "event-processor" exists with dead-lettering enabled
And a Send-only authorisation rule named "app-send" exists
And the Send-only connection string is stored in Key Vault
```
**Validates:** FR-IAC-003

### TC-IAC-005: Key Vault provisioned with RBAC [REGRESSION]

```gherkin
Given the Terraform configuration has been applied
When I inspect the Key Vault
Then the Key Vault uses RBAC-based access control
And soft delete is enabled
And the Container App managed identity has "Key Vault Secrets User" role
```
**Validates:** FR-IAC-004

### TC-IAC-006: Container App pulls image via managed identity [REGRESSION]

```gherkin
Given the Terraform configuration has been applied
And a container image has been pushed to the Container Registry
When the Container App is deployed
Then the Container App pulls the image from ACR via managed identity (no admin credentials)
And the Container App is accessible via its FQDN over HTTPS
```
**Validates:** FR-IAC-002, FR-IAC-005

### TC-IAC-007: Container App health probes configured [REGRESSION]

```gherkin
Given the Terraform configuration has been applied
When I inspect the Container App configuration
Then a liveness probe is configured at "/health/live"
And a readiness probe is configured at "/health/ready"
```
**Validates:** FR-IAC-002.4

### TC-IAC-008: Container App auto-scaling configured [EDGE]

```gherkin
Given the Terraform configuration has been applied
When I inspect the Container App scaling rules
Then the minimum replica count is configured (default: 1)
And the maximum replica count is configured (default: 10)
And an HTTP scaling rule triggers at 50 concurrent requests per instance
```
**Validates:** FR-IAC-002.6

### TC-IAC-009: Environment separation via tfvars [REGRESSION]

```gherkin
Given dev.tfvars with environment="dev" and prod.tfvars with environment="prod"
When I compare the terraform plans for each
Then resource names include the environment suffix (e.g., "shortly-events-dev" vs "shortly-events-prod")
And the resources are isolated from each other
```
**Validates:** FR-IAC-001.3

---

## Test Coverage Matrix

| Requirement | Test Cases | Coverage |
|-------------|-----------|----------|
| FR-LNK-001 | TC-LNK-001 to TC-LNK-015 | Full |
| FR-LNK-002 | TC-DIS-001 to TC-DIS-003 | Full |
| FR-LNK-003 | TC-LNK-008, TC-LNK-009, TC-RDR-003 | Full |
| FR-LNK-004 | TC-GET-001, TC-GET-002 | Full |
| FR-LNK-005 | TC-LNK-013 | Full |
| FR-RDR-001 | TC-RDR-001, TC-RDR-005 | Full |
| FR-RDR-002 | TC-RDR-002 | Full |
| FR-RDR-003 | TC-RDR-003, TC-RDR-004 | Full |
| FR-RDR-004 | TC-RDR-006, TC-EVT-003 | Full |
| FR-DOM-001 | TC-DOM-001 to TC-DOM-006 | Full |
| FR-DOM-002 | TC-DOM-007, TC-DOM-008 | Full |
| FR-DOM-003 | TC-DOM-009 to TC-DOM-012 | Full |
| FR-SEC-001 | TC-SEC-001, TC-SEC-002 | Full |
| FR-SEC-002 | TC-SEC-004 | Partial (sanitisation not tested) |
| FR-SEC-003 | TC-SEC-003 | Full |
| FR-OBS-001 | TC-INF-001 to TC-INF-003 | Full |
| FR-INF-002 | TC-INF-004 | Full |
| FR-INF-003 | TC-EVT-001 to TC-EVT-003 | Full |
| FR-INF-004 | TC-INF-005 | Full |
| FR-IAC-001 | TC-IAC-001 to TC-IAC-003, TC-IAC-009 | Full |
| FR-IAC-002 | TC-IAC-006 to TC-IAC-008 | Full |
| FR-IAC-003 | TC-IAC-004 | Full |
| FR-IAC-004 | TC-IAC-005 | Full |
| FR-IAC-005 | TC-IAC-006 | Full |

### Business Rules Traceability

| Business Rule | Test Cases |
|--------------|-----------|
| BR-LNK-001 | TC-LNK-013 |
| BR-LNK-002 | TC-LNK-001 |
| BR-LNK-003 | TC-DOM-003 to TC-DOM-006 |
| BR-LNK-004 | TC-LNK-004, TC-LNK-005, TC-LNK-014 |
| BR-LNK-005 | TC-LNK-002, TC-LNK-003 |
| BR-LNK-006 | TC-LNK-008 |
| BR-LNK-007 | TC-LNK-006, TC-LNK-007 |
| BR-LNK-008 | TC-LNK-010, TC-LNK-011, TC-LNK-012 |
| BR-LFC-001 | TC-RDR-003 |
| BR-LFC-002 | TC-RDR-004 |
| BR-RDR-001 | TC-RDR-001 |
| BR-RDR-002 | TC-RDR-005 |
| BR-RDR-003 | TC-RDR-002 |
| BR-RDR-004 | TC-RDR-006 |
| BR-DOM-001 | TC-DOM-002 |
| BR-DOM-002 | (Architectural constraint — no direct API to change prefix) |
| BR-DOM-003 | TC-DOM-010, TC-DOM-011 |
| BR-DOM-004 | TC-DOM-010 |
