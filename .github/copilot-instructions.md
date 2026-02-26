# Copilot instructions for Expensify backend

## Big picture
- This repo is a **modular monolith** on **.NET 10 / C# 14** with Clean Architecture boundaries enforced by NetArchTest.
- Main host is `src/API/Expensify.Api`; cross-cutting code is under `src/Common/*`; business modules live under `src/Modules/*`.
- Current module pattern is exemplified by `src/Modules/Users/*` (Domain, Application, Infrastructure, Presentation, IntegrationEvents).
- API endpoints are Minimal APIs via Carter modules; handlers use MediatR CQRS + `Result/Result<T>`.

## Request/data flow
- Carter endpoint (e.g., `Users/Command/RegisterUser.cs`) maps HTTP request and sends MediatR command/query.
- Application handler (`internal sealed`) executes use case and returns `Result<T>`.
- Infrastructure persists via `UsersDbContext` / repositories and `IUnitOfWork.SaveChangesAsync`.
- Domain events are captured by `InsertOutboxMessagesInterceptor`, then processed by Quartz outbox/inbox jobs.

## Startup and module wiring patterns
- `src/API/Expensify.Api/Program.cs` is the composition root:
  - Add module application assemblies for MediatR + FluentValidation discovery.
  - Load module config via `builder.Configuration.AddModuleConfiguration(["users", ...])`.
  - Register module via `builder.Services.AddUsersModule(builder.Configuration)`.
  - Development startup applies migrations + seeds users (`ApplyMigrations`, `UserSeedService`).
- Module registration pattern is in `src/Modules/Users/Expensify.Modules.Users.Infrastructure/UsersModule.cs`.

## Hard conventions (tests enforce these)
- Layer references are restricted (`tests/Expensify.Modules.Users.ArchitectureTests/Layers/LayerTests.cs`).
- Application types must be sealed/named and non-public where required (`*Command`, `*CommandHandler`, `*Query`, `*QueryHandler`, `*Validator`, `*DomainEventHandler`).
- See `tests/Expensify.Modules.Users.ArchitectureTests/Application/ApplicationTests.cs` for exact rules.
- Module isolation is checked in `tests/Expensify.ArchitectureTests/Layer/ModuleTests.cs`.
- Treat warnings as errors is enabled globally in `Directory.Build.props`.

## Build, test, and generation workflow
- Build/test from repo root: `dotnet build`, `dotnet test`.
- API build triggers NSwag (`src/API/Expensify.Api/Expensify.Api.csproj`, target `NSwag`) and updates OpenAPI client artifacts.
- Use `-p:NoSwagGen=true` when you intentionally need a faster build without client generation.
- Do not manually hand-edit generated client output (`src/API/Expensify.Api.Client/ExpensifyV1Client.g.cs`).
- Keep NuGet versions centralized in `Directory.Packages.props`.

## Runtime/dependency integration points
- Local infra via `docker-compose.yml`: PostgreSQL, Redis, Aspire dashboard, API.
- Shared infrastructure registration is in `src/Common/Expensify.Common.Infrastructure/InfrastructureConfiguration.cs`:
  - JWT auth, policy-based authorization, Quartz hosted jobs, MassTransit (in-memory transport).
  - Redis is preferred; code falls back to distributed in-memory cache if Redis connection fails.
- API versioned routes are under `/api/v{version}` (`WebApplicationExtensions.Configure`).
- Outbox/Inbox tables are configured inside module DbContext (`UsersDbContext`).

## Practical editing guidance for agents
- When adding a module, mirror Users module shape and registration pattern before adding features.
- Prefer touching one layer per change unless the feature explicitly crosses layers.
- For endpoint changes, update Presentation + corresponding Application command/query/handler together.
- For schema changes, add EF migration in module Infrastructure and ensure startup migration path remains valid.
- For behavior changes, run the narrowest affected tests first (module unit tests/architecture tests), then broader `dotnet test`.
