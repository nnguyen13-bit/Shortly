# Shortly

A URL shortening service built with .NET 10, Clean Architecture, and MongoDB.

## Architecture

```
src/
├── Shortly.Domain/           # Entities, Value Objects, Domain Events (no external dependencies)
├── Shortly.Application/      # Use Cases, Interfaces, Result types (depends on Domain only)
├── Shortly.Infrastructure/   # MongoDB persistence, code generation, event publishing
└── Shortly.Api/              # ASP.NET Core API, controllers, middleware

tests/
├── Shortly.Domain.Tests/
├── Shortly.Application.Tests/
├── Shortly.Infrastructure.Tests/
└── Shortly.Api.Tests/
```

## Domain Model

- **Link** — Aggregate root for shortened URLs (ShortCode, DomainPrefix, DestinationUrl, LinkMetadata)
- **CustomDomain** — Branded domain prefixes (e.g. `handover.link`)

## Tech Stack

- .NET 10 / ASP.NET Core
- MongoDB (via MongoDB.Driver)
- Azure Service Bus (event publishing)
- xUnit + Testcontainers (testing)

## Getting Started

```bash
dotnet build Shortly.slnx
dotnet test Shortly.slnx
dotnet run --project src/Shortly.Api
```

## Configuration

See `src/Shortly.Api/appsettings.json` for MongoDB and Service Bus connection settings.
