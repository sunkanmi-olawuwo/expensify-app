# Project Instructions — Expensify Backend

## Architecture Overview

Modular monolith on **.NET 10 / C# 14** for a personal finance product (expense/income tracking, recurring subscriptions, monthly insights, AI-powered financial chat).

- **Composition root**: `src/API/Expensify.Api/Program.cs` — registers all modules, middleware, and infra.
- **Modules**: `src/Modules/{Users,Expenses,Income}/` — each follows Clean Architecture with five layers: Domain, Application, Infrastructure, Presentation, IntegrationEvents.
- **API style**: Minimal APIs via Carter + MediatR CQRS + `Result<T>`. Endpoints live in each module's Presentation layer.
- **Cross-cutting**: `src/Common/*` — JWT auth, Quartz jobs, MassTransit (in-memory), Redis/fallback cache, outbox/inbox pattern.

## Local Development Setup

### Docker Compose — Infrastructure Services

```bash
docker compose up -d    # starts PostgreSQL, Redis, Aspire dashboard
```

| Service | Container Name | Host Port | Notes |
|---|---|---|---|
| PostgreSQL 17 | `Expensify.Database` | `5432` | DB: `Expensify`, user/pass: `postgres`/`postgres` |
| Redis | `Expensify.Redis` | `6379` | Falls back to in-memory cache if unavailable |
| Aspire Dashboard | `Expensify.AspireDashboard` | `18888` (UI), `4317` (OTLP) | Anonymous access enabled in dev |
| API (containerized) | `Expensify.Api` | `5000` → `8080` | Only when running via compose |

Data volume: `./.containers/db` persists PostgreSQL data across restarts.

### Connection Strings (`appsettings.Development.json`)

```
Database: Host=Expensify.database;Port=5432;Database=Expensify;Username=postgres;Password=postgres;Include Error Detail=true
Cache:    Expensify.redis:6379
```

When running the API outside Docker (e.g., via `dotnet run` or IDE), update hosts to `localhost` instead of the container names.

### Launch Profiles (`Properties/launchSettings.json`)

- **http**: `http://localhost:5021`
- **https**: `https://localhost:7149` + `http://localhost:5021`
- Both set `ASPNETCORE_ENVIRONMENT=Development` and disable browser launch.

### Seed Data (Development Only)

On startup in Development mode, `Program.cs` auto-applies EF migrations and runs `UserSeedService.SeedUsersAsync()`. Two test accounts are created (skipped if they already exist):

| Email | Password | Role | Permissions |
|---|---|---|---|
| `admin@test.com` | `Test1234!` | Admin | Full CRUD on users/expenses/income + `users:read:all`, `admin:expenses:read`, `admin:income:read` |
| `user@test.com` | `Test1234!` | User | Own-data CRUD on users (read/update), expenses, income |

Bogus randomizer seed is fixed at `4503` for consistent fake data generation.

### CORS (Development)

Allowed origins: `http://localhost:3000`, `http://127.0.0.1:3000`.

### Logging

Serilog outputs to console, rolling file (`Logs/log-.txt`, 7-day retention), and OpenTelemetry (Aspire dashboard). Default level: `Debug`; Microsoft namespaces: `Warning`.

## Build, Test, and Run

```bash
dotnet build Expensify.slnx -v minimal          # builds + runs NSwag client generation
dotnet build Expensify.slnx -p:NoSwagGen=true    # skip client gen for faster iterations
dotnet test Expensify.slnx -v minimal            # all tests (unit + architecture + integration)
```

- NSwag auto-generates `src/API/Expensify.Api.Client/ExpensifyV1Client.g.cs` — **never hand-edit** this file.
- NuGet versions are centralized in `Directory.Packages.props`.
- `Directory.Build.props` enables treat-warnings-as-errors globally.

### CI Pipeline (`.github/workflows/ci.yml`)

Runs on push/PR to `main`:
1. Build (with NSwag generation)
2. Test matrix (parallel): Unit, Architecture, Integration
3. Coverage report — thresholds: **50% line**, **25% branch** (fails PR if unmet)

PR titles must use prefix: `Patch:`, `Feature:`, or `Breaking:` (enforced by `.github/workflows/pr-message-check.yml`).

## Module Pattern — How to Add or Modify Features

Mirror the Users module (`src/Modules/Users/`) as the reference implementation:

```
src/Modules/{ModuleName}/
  ├── Domain/           # Entities, value objects, domain events
  ├── Application/      # Commands, queries, handlers, validators (MediatR + FluentValidation)
  ├── Infrastructure/   # DbContext, repositories, EF migrations, module registration
  ├── Presentation/     # Carter endpoints (Minimal API)
  └── IntegrationEvents/  # Cross-module event contracts
```

**Adding a command (concrete example):**
1. `Application/{Feature}/Command/{Name}Command.cs` — record with MediatR `ICommand<T>`
2. `Application/{Feature}/Command/{Name}CommandHandler.cs` — `internal sealed` handler returning `Result<T>`
3. `Application/{Feature}/Command/{Name}CommandValidator.cs` — FluentValidation rules
4. `Presentation/{Feature}/{Name}.cs` — Carter module mapping HTTP → MediatR command
5. Register module assembly in `Program.cs` `moduleApplicationAssemblies` array if new module
6. Register module services via `builder.Services.Add{ModuleName}Module(builder.Configuration)` in `Program.cs`

### Outbox/Inbox Event Processing

Each module configures outbox/inbox batch processing in its `modules.{name}.Development.json`:
- Interval: `5 seconds`
- Batch size: `20` (dev) / `50` (production)

## Hard Conventions (Architecture Tests Enforce These)

- Layer dependencies are restricted: Domain has no outward deps, Application depends only on Domain, Infrastructure implements Application interfaces. Violations fail `tests/*ArchitectureTests/`.
- Application types (`*Command`, `*CommandHandler`, `*Query`, `*QueryHandler`, `*Validator`, `*DomainEventHandler`) must be `internal sealed`.
- Modules must not reference each other's internals — communicate via integration events only.
- Builds must be warning-free (warnings are errors).

## Data and Persistence

- Each module owns its `DbContext` (e.g., `UsersDbContext`) with outbox/inbox tables configured.
- Schema changes → add EF migration in the module's Infrastructure project.
- Domain events are captured by `InsertOutboxMessagesInterceptor` and processed by Quartz background jobs.

## Security Boundaries

- JWT identity is the source of truth for user-scoped operations — always derive user context from the token.
- Authorization policies are enforced per-endpoint via permission strings (e.g., `users:read`, `expenses:write`).
- Never expose cross-user data; enforce tenant isolation in every query.
- Never log secrets, tokens, or sensitive payloads.
- Dev JWT config: issuer/audience `DevTips`, secret key placeholder in `appsettings.Development.json`.

## API Versioning and Contracts

- All routes are versioned under `/api/v{version}` (currently v1).
- When endpoint contracts change: update Presentation layer + Application handler together, then rebuild to regenerate the OpenAPI client.
