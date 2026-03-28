# Dashboard Summary Endpoint Plan

## Summary
Implement `GET /api/v1/dashboard/summary` as an API composition-layer read feature, not inside `Expenses` or `Income`. This keeps cross-module aggregation out of the feature modules, which are explicitly isolated from each other by architecture tests.

No schema changes are needed. The endpoint will read from `users`, `expenses`, and `income` tables, assemble a single dashboard response, expose it through Carter, and regenerate the NSwag client so integration tests can call it through `IExpensifyV1Client`.

## Key Changes
- Add a new dashboard feature in the API project with:
  - `DashboardSummaryResponse`
  - `DashboardMetricResponse`
  - `DashboardSpendingBreakdownItemResponse`
  - `DashboardMonthlyPerformanceItemResponse`
  - `DashboardRecentTransactionResponse`
  - `GetDashboardSummaryQuery` and `GetDashboardSummaryQueryHandler`
  - `GetDashboardSummary` Carter endpoint
- Register the API assembly for both:
  - MediatR/validators in `Program.cs`
  - Carter module scanning in the API project
- Require authenticated access and keep policy enforcement aligned with existing conventions by requiring both expense and income read access.
- Use `IDbConnectionFactory` plus `IDateTimeProvider` in the handler; do not call existing expense/income endpoints or MediatR queries from other modules.
- Load user settings first from `users.users` to get `currency`, `timezone`, and `month_start_day`.
- Derive the current dashboard period from `IDateTimeProvider.UtcNow` converted into the user’s timezone:
  - If local day is before `month_start_day`, current period starts in the previous month.
  - Otherwise, it starts in the current month.
  - Previous period is the immediately preceding user-month window.
  - Six-month history is the current period plus the previous five periods.
- Query non-deleted expenses and income for the six-period window, then aggregate in C# so the logic stays unit-testable with in-memory SQLite.
- Compute:
  - `monthlyIncome`, `monthlyExpenses`, `netCashFlow` for current period
  - change percentages against previous period using `((current - previous) / previous) * 100`, with `0` when previous is `0`
  - `spendingBreakdown` from current-period expenses only
  - `monthlyPerformance` as exactly six ordered buckets, including zero-value months
  - `recentTransactions` from a normalized union over expenses and income, ordered by `created_at_utc` descending, limited to 5
- Regenerate the API client through the normal build/NSwag flow; do not edit generated client code manually.

## Public API / Contract Decisions
- Route: `GET /api/v1/dashboard/summary`
- Response shape follows the spec exactly, with numeric amounts and percentages.
- `currency` for all three hero metrics comes from the user profile currency.
- `recentTransactions` mapping defaults:
  - expense `merchant` => expense merchant
  - income `merchant` => income source
  - expense `category` => expense category name
  - income `category` => income type
  - `type` => `"expense"` or `"income"`
  - `status` => fixed `"posted"`
  - `timestamp` => `created_at_utc` serialized as ISO 8601 UTC
- `spendingBreakdown.colorKey` uses a deterministic generated palette such as `chart-1`, `chart-2`, ... assigned by the sorted breakdown order.
- `monthlyPerformance.month` labels use the start month of each user-month period, formatted like `MMM yyyy`.

## Test Plan
- Add unit tests for the dashboard query handler in a new API-focused unit test project.
- Unit-test scenarios:
  - computes current-period income, expenses, and net cash flow correctly
  - returns `0` change percentages when the previous period total is `0`
  - computes positive/negative percentage deltas correctly when previous data exists
  - returns exactly six monthly performance entries and fills missing months with zeros
  - builds spending breakdown percentages correctly and keeps the rounded sum within tolerance
  - merges recent income and expense rows, orders by timestamp descending, and limits to 5
  - respects user timezone and `month_start_day` when selecting the current period
  - excludes soft-deleted expense/income rows
- Add integration tests in Reqnroll plus step definitions for:
  - authenticated happy path with both expense and income data across current and previous periods
  - unauthenticated request returns `401`
  - no-data user returns zeroed hero metrics, empty breakdown/recent transactions, and six zeroed monthly performance entries
  - single-period-only data returns correct totals and `0` change percentages
  - recent transactions are capped at 5 and ordered newest-first
  - data is scoped to the authenticated user only
- Verify the generated client surface is used from integration tests rather than raw HTTP calls.

## Assumptions
- Dashboard periods will follow the existing user-configured `month_start_day`, not strict calendar months.
- “Current” is based on the user’s configured timezone, not server local time.
- No migration is needed because `status` and `colorKey` are derived, not persisted.
- The API composition layer is the chosen home for this feature because existing modules must remain isolated from one another.
