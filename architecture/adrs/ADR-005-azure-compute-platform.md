# ADR-005: Azure Compute Platform

## Status
COMPLETE

## Impact
MEDIUM

## Context

Shortly needs to deploy two services with very different scaling profiles:
- **Redirect Service**: Latency-critical (<100ms), read-heavy, must handle bursts, always-on
- **Link Management Service**: Write path at 60K/day (~0.7 req/s average), can tolerate slightly higher latency

Both services are .NET 10 applications deployed to Azure.

## Problem Statement

Which Azure compute platform should host Shortly's microservices, considering the different scaling profiles of the redirect (latency-critical) vs link management (throughput-focused) services?

## Options

### Option A: Azure Container Apps
- **Description:** Serverless containers with KEDA-based auto-scaling
- **Pros:** Scale-to-zero for link management, fast scale-up, container-based (portable), HTTP scaling triggers, no cold start for min-replica=1
- **Cons:** Less control than AKS, newer platform
- **Estimated Cost:** SMALL (pay-per-use)

### Option B: Azure App Service
- **Description:** PaaS web hosting with built-in scaling
- **Pros:** Mature, simple deployment, built-in health checks, deployment slots
- **Cons:** Always-on billing, less granular scaling, heavier for microservices
- **Estimated Cost:** MEDIUM (always-on instances)

### Option C: Azure Functions
- **Description:** Serverless functions triggered by HTTP
- **Pros:** True pay-per-execution, built-in DynamoDB/Service Bus bindings
- **Cons:** Cold start latency (bad for redirects), 10-second timeout pressure, limited middleware
- **Estimated Cost:** SMALL (but cold start is a blocker for redirect)

### Option D: Azure Kubernetes Service (AKS)
- **Description:** Full Kubernetes orchestration
- **Pros:** Maximum control, advanced networking, service mesh
- **Cons:** Massive over-engineering for 2 services, operational overhead, cost
- **Estimated Cost:** LARGE

## Decision

**Option A: Azure Container Apps**

- **Redirect Service**: min-replicas=1 (no cold start), scale on HTTP concurrent requests
- **Link Management + Domain Registry**: min-replicas=0 (scale to zero when idle), scale on HTTP requests
- Both services as .NET 10 containers with Dockerfiles
- Revision-based deployments for zero-downtime updates

## Consequences

- **Easier:** Simple deployment (container push), auto-scaling, cost-efficient at low traffic
- **Easier:** Redirect service always warm with min-replica=1
- **Harder:** Debugging is slightly harder than App Service (less built-in diagnostics)
- **Trade-off:** Less mature than App Service but more appropriate for microservices pattern

## Action Items

- [ ] Create Dockerfiles for each service
- [ ] Define Container Apps environment in IaC
- [ ] Configure scaling rules (HTTP concurrent requests)
- [ ] Set up Azure Container Registry
- [ ] Configure health probes and readiness checks
