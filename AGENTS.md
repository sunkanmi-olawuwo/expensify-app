# AGENTS.md (Backend)

## Scope
- Applies to `expensify-backend/` and its subdirectories.
- Overrides root guidance where backend-specific details are required.

## Architecture Snapshot
- Modular monolith on `.NET 10` / `C# 14`.
- Clean Architecture boundaries are enforced by architecture tests.
- Main host: `src/API/Expensify.Api`.
- Shared cross-cutting code: `src/Common/*`.
- Modules: `src/Modules/*` (Domain, Application, Infrastructure, Presentation, optional IntegrationEvents).
- API style: Minimal APIs via Carter + MediatR CQRS + `Result/Result<T>`.

## Request/Execution Flow
- Presentation endpoint maps HTTP input and sends command/query.
- Application handler executes use case and returns `Result`.
- Infrastructure persists via DbContext/repositories + `IUnitOfWork`.
- Domain events are persisted via outbox interceptor and processed by background jobs.

## Composition and Startup
- `src/API/Expensify.Api/Program.cs` is the composition root.
- Register module assemblies for MediatR/validators.
- Load module config via `AddModuleConfiguration([...])`.
- Register each module via `Add<Module>Module(...)`.
- In Development, startup applies migrations and seeds users.

## Hard Conventions (Do Not Break)
- Respect layer boundaries and module isolation.
- Keep application handlers/validators/domain-event handlers aligned with naming/sealing rules enforced by tests.
- Warnings are errors; keep builds warning-free.
- Keep package versions centralized in `Directory.Packages.props`.

## Build/Test Workflow
- From backend root:
- `dotnet build Expensify.slnx -v minimal`
- `dotnet test Expensify.slnx -v minimal`
- API build runs NSwag generation.
- For faster local cycles when appropriate: `-p:NoSwagGen=true`.
- Do not manually edit generated code in:
- `src/API/Expensify.Api.Client/ExpensifyV1Client.g.cs`

## Endpoint and Contract Rules
- Versioned APIs live under `/api/v{version}`.
- Keep OpenAPI output consistent with runtime routes.
- When endpoint contracts change:
- Update presentation + handler logic together.
- Update integration tests and generated client usage.

## Data and Persistence Rules
- Schema changes require module migration updates.
- Keep migrations in module Infrastructure projects.
- Preserve idempotency and consistency for outbox/inbox and recurring processing patterns.

## Security and Privacy Rules
- JWT user identity is the source of truth for user-scoped operations.
- Enforce authorization policies explicitly per endpoint.
- Never expose another user's data.
- Do not log secrets, tokens, or sensitive payloads.

## Practical Editing Guidance
- Prefer module-local changes over cross-module coupling.
- Touch only required layers for the requested behavior.
- Keep command/query model explicit and predictable.
- Add or update tests for behavior changes before closing work.
