# Investment Module Plan

## Summary
Implement a new `Investments` module that follows the same Clean Architecture structure as `Expenses` and `Income`: `Domain`, `Application`, `Infrastructure`, and `Presentation`, with matching unit, integration, and architecture test coverage.

The module will own three core data sets in a dedicated `investments` schema:
- investment categories
- investment accounts
- investment contributions

It will expose user-scoped endpoints for managing accounts, contributions, and portfolio summary, plus admin endpoints for listing all investments and toggling category activation. The module will be registered in the API composition root, added to solution/module config wiring, and included in migration startup.

## Key Changes
- Add a new module tree:
  - `src/Modules/Investments/Expensify.Modules.Investments.Domain`
  - `src/Modules/Investments/Expensify.Modules.Investments.Application`
  - `src/Modules/Investments/Expensify.Modules.Investments.Infrastructure`
  - `src/Modules/Investments/Expensify.Modules.Investments.Presentation`
- Add matching test projects:
  - `tests/Expensify.Modules.Investments.UnitTests`
  - `tests/Expensify.Modules.Investments.ArchitectureTests`
- Register the module in:
  - `src/API/Expensify.Api/Program.cs`
  - `src/API/Expensify.Api/Extensions/MigrationExtensions.cs`
  - `src/API/Expensify.Api/modules.investments.json`
  - `src/API/Expensify.Api/modules.investments.Development.json`
  - `Expensify.slnx`
- Add module policy constants and policy factory entries for:
  - `investments:read`
  - `investments:write`
  - `investments:delete`
  - `admin:investments:read`
  - `admin:investments:manage-categories`
- Extend `Users` role seeding so:
  - `User` gets investment read/write/delete claims
  - `Admin` gets user claims plus admin investment claims
- Update top-level architecture isolation tests so `Investments` is treated as a peer module and cannot depend on other modules.

## Domain And Persistence
- Model `InvestmentCategory` with:
  - `Id`
  - `Name`
  - `Slug`
  - `IsActive`
  - audit timestamps
- Model `InvestmentAccount` with:
  - `Id`
  - `UserId`
  - `Name`
  - `Provider`
  - `CategoryId`
  - `Currency`
  - `InterestRate`
  - `MaturityDate`
  - `CurrentBalance`
  - `Notes`
  - audit timestamps
  - `DeletedAtUtc`
- Model `InvestmentContribution` with:
  - `Id`
  - `AccountId`
  - `Amount`
  - `Date`
  - `Notes`
  - audit timestamps
  - `DeletedAtUtc`
- Use a dedicated `InvestmentsDbContext` with:
  - default schema `investments`
  - query filters for soft-deleted accounts and contributions
  - module-local outbox/inbox configuration matching existing modules
  - module-local soft-delete purge configuration matching existing modules
- Seed investment categories in the migration using idempotent SQL inserts with fixed GUIDs and stable slugs for:
  - `isa`
  - `lisa`
  - `mutual-fund`
  - `fixed-deposit`
  - `other`
- Add indexes for expected read paths:
  - category `slug` unique
  - account `user_id`, `category_id`, and `deleted_at_utc`
  - contribution `account_id`, `date`, and `deleted_at_utc`

## Application And API Behavior
- Follow the existing CQRS pattern:
  - commands use repositories plus unit of work
  - queries use `IDbConnectionFactory` and Dapper
- Add these endpoints under v1:
  - `POST /investments`
  - `GET /investments`
  - `GET /investments/{id}`
  - `PUT /investments/{id}`
  - `DELETE /investments/{id}`
  - `POST /investments/{id}/contributions`
  - `GET /investments/{id}/contributions`
  - `GET /investments/summary`
  - `GET /investments/categories`
  - `PUT /admin/investments/categories/{id}`
  - `GET /admin/investments`
- Enforce user scoping from JWT claims for all non-admin endpoints.
- Require account currency to equal the authenticated user's active/default currency, consistent with existing Expenses/Income handling.
- Category validation rules:
  - create requires an active category
  - update requires an active category only when changing category
  - existing accounts may keep an inactive category during updates
  - fixed deposit requires both `interestRate` and `maturityDate`
  - non-applicable `interestRate` and `maturityDate` values are accepted but cleared before persistence
- Contribution rules:
  - amounts must be positive
  - contributions are append-only in v1
  - contributions belong to exactly one account
- Delete behavior:
  - deleting an account performs soft-delete
  - all contributions for that account are soft-deleted in the same transaction
- List/query behavior:
  - account list is paged and supports optional `categoryId`
  - account list returns pagination headers like existing Income/Expenses endpoints
  - account list sorts by `updated_at_utc DESC`, then `created_at_utc DESC`
  - contribution list is paged and sorts by `date DESC`, then `created_at_utc DESC`
  - account detail includes computed `totalContributed`
- Summary behavior:
  - `totalContributed` is the sum of non-deleted contributions
  - `currentValue` is the sum of account `CurrentBalance`
  - `totalGainLoss` is `currentValue - totalContributed`
  - `gainLossPercentage` is `0` when `totalContributed == 0`, otherwise `(totalGainLoss / Math.Abs(totalContributed)) * 100`
  - `accountCount` counts non-deleted accounts
  - `currency` is the authenticated user's currency
- Categories endpoint behavior:
  - regular users see active categories only
  - admins see all categories
  - no runtime category creation endpoint is added
- Admin investments endpoint:
  - returns paged accounts across all users
  - uses admin-only policy

## Public API / Contract Decisions
- Use plural route base `/investments` for consistency with the spec and to avoid future nesting awkwardness.
- Keep response amounts as raw decimals; formatting remains a frontend concern.
- Use `DateTimeOffset` semantics at the contract level for maturity date and contribution date, but persist using the same conventions already used across the backend.
- Include computed `totalContributed` on the single-account response, not on the paged list response.
- Do not add restore endpoints or recycle-bin endpoints in v1 because they are not required by the spec.
- Do not add contribution update/delete endpoints in v1.
- Do not implement FX conversion, projected returns, withdrawals, or tax logic.

## Test Plan
- Add unit tests for:
  - account creation with valid category rules
  - fixed deposit creation/update requiring `interestRate` and `maturityDate`
  - clearing ignored fields for non-applicable categories
  - rejecting deactivated categories on create and category change
  - allowing updates that keep the same inactive category
  - positive-only contribution amounts
  - portfolio summary calculations including zero-contribution edge case
  - soft-delete cascading from account to contributions
  - admin category toggle behavior
- Add architecture tests mirroring Expenses/Income coverage for:
  - layer boundaries
  - presentation constraints
  - domain constraints
  - module isolation
- Add integration tests covering:
  - create/list/get/update/delete account happy path
  - contributions create/list happy path
  - paginated list headers for accounts and contributions
  - category filtering
  - summary totals and gain/loss percentage
  - regular-user category visibility vs admin visibility
  - admin category activation toggle
  - admin list-all-investments authorization
  - `401` for unauthenticated requests
  - `403` for unauthorized requests
  - inactive category rejection
  - fixed deposit validation failures
  - delete cascade removing account/contributions from reads
- Run normal API build/test flow so NSwag regenerates the client and integration tests use the generated client surface.

## Assumptions
- Module name is `Investments` and schema name is `investments`.
- Existing module conventions for Carter, MediatR, Mapster, policy factories, and soft-delete jobs are reused rather than generalized first.
- Category seed data is migration-based, not startup-seeder-based, because the repo already seeds reference catalogs that way.
- Investment accounts always use the user's current allowed currency, so summary aggregation stays valid without multi-currency rules.
- No additional cross-module dependency is introduced beyond the same user-settings lookup pattern already used by Expenses/Income.
