# Expensify API

A **Modular Monolith** back-end built with **.NET 10** and **C# 14**, following Clean Architecture and Domain-Driven Design (DDD) principles.

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Technology Stack](#technology-stack)
- [Project Structure](#project-structure)
- [Key Patterns](#key-patterns)
- [Getting Started](#getting-started)
- [Running Tests](#running-tests)
- [Adding a New Module](#adding-a-new-module)
- [Contributing](#contributing)

---

## Architecture Overview

The solution is organised as a **Modular Monolith**. Each business capability (e.g. *Users*) lives in its own module with four layers that follow **Clean Architecture** dependency rules enforced by architecture tests:

```
Domain  ←  Application  ←  Presentation
                ↑
          Infrastructure
```

| Layer | Responsibility |
|---|---|
| **Domain** | Entities, value objects, domain events, repository interfaces |
| **Application** | Commands / queries (CQRS via MediatR), validators, domain-event handlers |
| **Infrastructure** | EF Core DbContext, repository implementations, Identity, Outbox/Inbox, Quartz jobs |
| **Presentation** | Minimal-API endpoints registered through Carter modules |

A **Common** set of libraries (`Common.Domain`, `Common.Application`, `Common.Infrastructure`, `Common.Presentation`) provides cross-cutting building blocks shared by every module.

---

## Technology Stack

| Category | Package / Tool |
|---|---|
| **Runtime** | .NET 10, C# 14 |
| **Web Framework** | ASP.NET Core Minimal APIs |
| **Endpoint Routing** | [Carter](https://github.com/CarterCommunity/Carter) |
| **CQRS / Mediator** | [MediatR](https://github.com/jbogard/MediatR) |
| **Validation** | [FluentValidation](https://docs.fluentvalidation.net/) |
| **ORM** | Entity Framework Core 10 (PostgreSQL via Npgsql) |
| **Raw SQL** | [Dapper](https://github.com/DapperLib/Dapper) |
| **Identity** | ASP.NET Core Identity (IdentityDbContext) |
| **Authentication** | JWT Bearer tokens |
| **Object Mapping** | [Mapster](https://github.com/MapsterMapper/Mapster) |
| **Messaging / Event Bus** | [MassTransit](https://masstransit.io/) (in-memory transport) |
| **Background Jobs** | [Quartz.NET](https://www.quartz-scheduler.net/) (Outbox & Inbox processors) |
| **Caching** | Redis (StackExchange.Redis) with in-memory fallback |
| **Logging** | [Serilog](https://serilog.net/) → Console, File, Seq |
| **Observability** | OpenTelemetry → Jaeger (traces), Seq (logs) |
| **API Docs** | NSwag + Scalar |
| **API Versioning** | Asp.Versioning |
| **Health Checks** | AspNetCore.HealthChecks (PostgreSQL, Redis) |
| **Static Analysis** | SonarAnalyzer.CSharp, `TreatWarningsAsErrors` |
| **Testing** | NUnit, NSubstitute, NetArchTest |
| **Containerisation** | Docker & Docker Compose |
| **Central Package Management** | `Directory.Packages.props` |
| **Fake Data** | [Bogus](https://github.com/bchavez/Bogus) (seed service) |

---

## Project Structure

```
├── src/
│   ├── API/
│   │   ├── Expensify.Api            # Host – Program.cs, middleware, migrations, Docker
│   │   └── Expensify.Api.Client     # Generated API client (NSwag)
│   ├── Common/
│   │   ├── Common.Domain            # Entity, Result, Error, DomainEvent base types
│   │   ├── Common.Application       # ICommand/IQuery, pipeline behaviours, interfaces
│   │   ├── Common.Infrastructure    # Auth, caching, outbox/inbox, EF interceptors, OpenTelemetry
│   │   └── Common.Presentation      # Shared API result helpers
│   └── Modules/
│       └── Users/
│           ├── Users.Domain          # User entity, RefreshToken, Identity types, domain events
│           ├── Users.Application     # CQRS handlers, validators, abstraction interfaces
│           ├── Users.Infrastructure  # EF DbContext, repositories, Identity, Outbox/Inbox jobs
│           ├── Users.Presentation    # Carter endpoint modules (register, login, refresh, etc.)
│           └── Users.IntegrationEvents  # Events published for cross-module consumption
├── tests/
│   ├── Expensify.ArchitectureTests           # Solution-level architecture rules
│   ├── Expensify.Modules.Users.ArchitectureTests  # Users module layer & convention tests
│   └── Expensify.Modules.Users.UnitTests     # Unit tests (NSubstitute mocks)
├── docker-compose.yml               # PostgreSQL, Redis, Seq, Jaeger, API
├── Directory.Build.props             # Shared build settings (TFM, analyzers)
├── Directory.Packages.props          # Centralised NuGet versions
└── .editorconfig                     # Code-style rules
```

---

## Key Patterns

### CQRS with MediatR

Commands and queries are modelled as records implementing `ICommand<T>` / `IQuery<T>`. Each has a dedicated handler (`ICommandHandler<T>`, `IQueryHandler<T>`). Pipeline behaviours provide **validation** (`ValidationPipelineBehavior`), **logging** (`RequestLoggingPipelineBehavior`), and **exception handling** (`ExceptionHandlingPipelineBehavior`) automatically.

### Result Pattern

Domain and application layers return `Result` / `Result<T>` instead of throwing exceptions. Errors are strongly typed (`Error`, `ValidationError`, `ErrorType`).

### Domain Events & Outbox Pattern

Entities raise domain events via the `Entity.Raise()` base method. An EF Core `SaveChangesInterceptor` (`InsertOutboxMessagesInterceptor`) serialises those events into an **outbox table** on the same transaction. A Quartz background job (`ProcessOutboxJob`) picks them up and dispatches them through MediatR. An analogous **inbox** pattern exists for integration events to guarantee idempotent processing.

### Transactional Outbox / Inbox

- **Outbox** – domain events are persisted alongside the aggregate change and later dispatched.
- **Inbox** – inbound integration events are de-duplicated via `InboxMessageConsumer` records before handling.

### Module Registration

Each module exposes an `AddXxxModule(IServiceCollection, IConfiguration)` extension method (e.g. `UsersModule.AddUsersModule`) that registers its DbContext, repositories, Identity, Quartz jobs, Carter modules, and Mapster profiles.

### Architecture Tests

`NetArchTest` enforces layer dependency rules at compile/test time:
- Domain **must not** reference Application, Infrastructure, or Presentation.
- Application **must not** reference Infrastructure or Presentation.
- Presentation **must not** reference Infrastructure.
- Domain events must be `sealed` and end with `DomainEvent`.
- Entities must have a private parameterless constructor.

---

## Getting Started

### Prerequisites

| Tool | Version |
|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | **10.0** or later |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | Latest |

### 1. Clone the repository

```bash
git clone https://github.com/Expensify/Expensify.API.git
cd Expensify.API
```

### 2. Start infrastructure with Docker Compose

```bash
docker compose up -d
```

This brings up:

| Service | Port |
|---|---|
| **PostgreSQL 17** | `localhost:5432` |
| **Redis** | `localhost:6379` |
| **Seq** (structured logs) | `localhost:5341` (ingest), `localhost:8081` (UI) |
| **Jaeger** (traces) | `localhost:16686` (UI), `localhost:4317` (OTLP gRPC) |

### 3. Run the API locally

```bash
cd src/API/Expensify.Api
dotnet run
```

On first run in `Development` mode the API will:
1. Apply EF Core migrations automatically.
2. Seed default users via `UserSeedService`.

> **Tip:** If you are running the API via Docker Compose the container already handles this. Use `docker compose up` to launch everything together.

### 4. Explore the API

- **Scalar UI** – available in Development mode for interactive API exploration.
- **NSwag** – an OpenAPI spec (`nswag_v1.json`) is generated at build time.

### Configuration

| Setting | Location | Purpose |
|---|---|---|
| `ConnectionStrings:Database` | `appsettings.Development.json` / User Secrets | PostgreSQL connection string |
| `ConnectionStrings:Cache` | `appsettings.Development.json` / User Secrets | Redis connection string |
| `AuthSettings:Key` | `appsettings.Development.json` / User Secrets | JWT signing key (**change in production**) |
| `Users:Outbox` | Module config (`users.Development.json`) | Outbox polling interval |
| `Users:Inbox` | Module config (`users.Development.json`) | Inbox polling interval |


## Running Tests

```bash
dotnet test
```

The test suite includes:

| Project | Scope |
|---|---|
| `Expensify.Modules.Users.UnitTests` | Unit tests for command/query handlers and services (NSubstitute mocks) |
| `Expensify.Modules.Users.ArchitectureTests` | Layer dependency & convention rules for the Users module |
| `Expensify.ArchitectureTests` | Solution-wide module isolation rules |

---

## Running Migrations

EF Core migrations are managed via the `dotnet-ef` CLI. The startup project is the API host and the `--context` flag selects the target `DbContext`.

### Prerequisites

Install (or update) the EF Core CLI tool globally:

```bash
dotnet tool install --global dotnet-ef   # first time
dotnet tool update  --global dotnet-ef   # update to latest
```

### Add a new migration (Users module)

Run from the **solution root**:

```bash
dotnet ef migrations add <MigrationName> \
  --project src/Modules/Users/Expensify.Modules.Users.Infrastructure/Expensify.Modules.Users.Infrastructure.csproj \
  --startup-project src/API/Expensify.Api/Expensify.Api.csproj \
  --output-dir Database/Migrations \
  --context UsersDbContext
```

> **Note:** If the generated migration file triggers static-analysis warnings (e.g. `IDE0161`), add `// <auto-generated />` as the very first line of the file.

### Apply migrations

Migrations are applied automatically on application startup in **Development** mode. To apply them manually:

```bash
dotnet ef database update \
  --project src/Modules/Users/Expensify.Modules.Users.Infrastructure/Expensify.Modules.Users.Infrastructure.csproj \
  --startup-project src/API/Expensify.Api/Expensify.Api.csproj \
  --context UsersDbContext
```

### Remove the last migration

```bash
dotnet ef migrations remove \
  --project src/Modules/Users/Expensify.Modules.Users.Infrastructure/Expensify.Modules.Users.Infrastructure.csproj \
  --startup-project src/API/Expensify.Api/Expensify.Api.csproj \
  --context UsersDbContext
```

---

## Adding a New Module

1. **Create the four layer projects** under `src/Modules/<ModuleName>/`:
   - `Expensify.Modules.<Name>.Domain`
   - `Expensify.Modules.<Name>.Application`
   - `Expensify.Modules.<Name>.Infrastructure`
   - `Expensify.Modules.<Name>.Presentation`
   - *(optional)* `Expensify.Modules.<Name>.IntegrationEvents`

2. **Follow the same dependency graph** as the Users module:
   - Domain → Common.Domain
   - Application → Common.Application, Domain, IntegrationEvents
   - Presentation → Common.Presentation, Common.Infrastructure, Application
   - Infrastructure → Common.Infrastructure, Application, Presentation

3. **Create a `<Name>Module` static class** in Infrastructure with `Add<Name>Module(IServiceCollection, IConfiguration)` to register all services, DbContext, Carter modules, and Quartz jobs.

4. **Register the module** in `Program.cs`:
   - Add the Application assembly to `moduleApplicationAssemblies`.
   - Call `builder.Services.Add<Name>Module(builder.Configuration)`.
   - Add the module name to `builder.Configuration.AddModuleConfiguration(["users", "<name>"])`.
   - Add the migration call in `MigrationExtensions.ApplyMigrations`.

5. **Add architecture tests** under `tests/Expensify.Modules.<Name>.ArchitectureTests` to enforce layer rules.

6. **Add a database schema** constant (like `Schemas.Users`) to keep tables namespaced.

---

## Contributing

### Branch Strategy

Development happens on feature branches off `Milan-Template`. Open a pull request targeting `Milan-Template` for review.

### Coding Standards

- **Warnings are errors** – the build enforces `TreatWarningsAsErrors` and SonarAnalyzer rules.
- **EditorConfig** – follow the `.editorconfig` at the repo root for formatting and style.
- **Central package versions** – add or update package versions **only** in `Directory.Packages.props`.
- **Nullable reference types** are enabled globally.
- **Sealed by default** – domain events and handlers should be `sealed` (enforced by architecture tests).

### Pull Request Checklist

- [ ] Code compiles with zero warnings.
- [ ] All existing tests pass (`dotnet test`).
- [ ] New logic has corresponding unit tests.
- [ ] Architecture tests pass (layer boundaries, naming conventions).
- [ ] No secrets or credentials committed – use User Secrets or environment variables.
- [ ] If a new NuGet package is needed, add the version to `Directory.Packages.props`.
- [ ] Migrations are included if the schema changed.

---

## License

*To be determined – please check with the repository maintainers.*
