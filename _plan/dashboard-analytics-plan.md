# Dashboard Analytics Endpoints

## Summary
Add six read-only dashboard analytics endpoints under `/dashboard/analytics/*` inside the existing Dashboard module, using the same Carter + MediatR + Dapper pattern as `GET /dashboard/summary`. Keep all analytics reads in Dashboard Application, query `users`, `expenses`, `income`, and `investments` schemas directly through `IDbConnectionFactory`, and reuse the existing `dashboard:read` policy.

## Implementation Changes
- Add a small internal dashboard read-model support layer in Dashboard Application and move shared logic out of `GetDashboardSummaryQueryHandler`:
  - `DashboardUserSettings` loader for `currency`, `timezone`, `monthStartDay`.
  - Shared `DashboardPeriod` helper for current period, previous period, history windows, explicit `YYYY-MM` periods, and labels.
  - Shared math helpers for color-key cycling, change percentages, savings rate, and remainder-corrected percentage allocation.
- Keep `GetDashboardSummary` behavior intact, but switch it to the shared helpers so analytics and summary use one period/percentage implementation.

- Add six new query/response pairs plus six Carter endpoints with `[AsParameters]` request DTOs:
  - `cash-flow-trend`
    - `months`: allow `3|6|12`, default `6`.
    - Return the latest N dashboard periods including the current user-specific period.
    - Each row contains `label`, `income`, `expenses`, `netCashFlow`, `savingsRate`.
  - `income-breakdown`
    - `months`: allow `1|3|6|12`, default `3`.
    - Group by `income.type` (`Salary`, `Freelance`, etc.); expose that value in the response `source` field.
    - `period` string: `"Last N month"` / `"Last N months"`.
    - Percentages use remainder correction and sum to 100 when data exists.
  - `category-comparison`
    - `month`: valid `YYYY-MM`, otherwise fall back to the current dashboard period.
    - Interpret `YYYY-MM` the same way the existing monthly summary endpoints do: period starts on that calendar month’s `monthStartDay` and ends at the next month’s `monthStartDay`.
    - Return the union of categories with spend in either month, ordered by `currentAmount` desc, then `previousAmount` desc, then category name.
  - `top-categories`
    - `months`: allow `1|3|6|12`, default `3`.
    - `limit`: default `5`, clamp to `1..10`.
    - `totalSpent` is full-period spend across all categories.
    - `percentage` is share of full-period `totalSpent`, so listed percentages may sum to less than 100 when `limit` truncates results.
  - `investment-allocation`
    - Read from `investments.investment_accounts` joined to `investments.investment_categories`.
    - Exclude soft-deleted accounts.
    - Return all categories with active balances, ordered by `totalBalance` desc then `categoryName`.
    - Percentages use remainder correction and sum to 100 when data exists.
  - `investment-trend`
    - `months`: allow `3|6|12`, default `6`.
    - Exclude soft-deleted accounts and contributions.
    - Bucket contributions by timezone-adjusted local date from `investment_contributions.date`, then apply dashboard-period boundaries.
    - `accountCount` is the distinct number of active accounts contributing in each bucket.

## Presentation/API Contract
- Add route files under Dashboard Presentation for each endpoint, all tagged `Dashboard`, versioned with `InfrastructureConfiguration.V1`, and protected by `DashboardPolicyConsts.ReadPolicy`.
- No new write paths, migrations, or cross-module service abstractions.
- Generated public API surface will expand in NSwag with six new client methods and response models; do not hand-edit the generated client.

## Test Plan
- Dashboard unit tests:
  - Add focused handler tests for each endpoint using the existing in-memory SQLite pattern with attached `users`, `expenses`, `income`, and `investments` databases.
  - Cover defaults/fallbacks for invalid `months` and invalid `month`.
  - Cover timezone/month-start boundary behavior, especially non-1 `monthStartDay`.
  - Cover percentage math: remainder correction, zero-income savings rate, zero-previous change percentage, and category comparison when previous spend is zero.
  - Cover soft-delete filtering for investment accounts and contributions.
- Integration tests:
  - Extend the Dashboard Reqnroll feature/steps for all six endpoints.
  - Add happy-path scenarios for each endpoint, one invalid-token `401` scenario, empty-data scenarios, and at least one boundary scenario proving user timezone/month-start behavior.
  - Verify generated client usage for the new endpoints after NSwag regeneration.
- Verification commands:
  - `dotnet build Expensify.slnx -v minimal`
  - Targeted dashboard/investments unit tests
  - Relevant dashboard integration tests

## Assumptions
- No schema changes are needed; all analytics can be derived from existing read tables.
- `income-breakdown` groups by income `type`, not raw `source`.
- `top-categories.percentage` is based on full-period spend, not only the returned top N rows.
- Response labels use existing `MMM yyyy` formatting for month buckets.
- The current dashboard unit-test project appears to have an unrelated restore-time baseline issue in this environment; if that persists during implementation, treat it as separate plumbing to unblock the intended verification suite.
