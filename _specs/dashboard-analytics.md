# Dashboard Analytics Endpoints

## Overview

Extend the Dashboard module with dedicated analytics endpoints that return time-series, comparative, and categorical data optimized for frontend chart rendering. While the existing `GET /dashboard/summary` provides a high-level snapshot, these analytics endpoints deliver deeper, configurable data that powers line charts, bar charts, pie/donut charts, and KPI trend cards.

All new endpoints live under `GET /dashboard/analytics/*`, reuse the existing `dashboard:read` authorization policy, and follow the same Dapper-based read model pattern already established in the module.

## User Stories

- As a user, I want to see income vs. expenses over a configurable time range (3, 6, or 12 months) so I can spot long-term trends.
- As a user, I want to see my income broken down by source type over a period so I can understand my earning diversification.
- As a user, I want to see a category-level spending comparison between two months so I can understand where my habits have changed.
- As a user, I want to see my savings rate trend over time so I can track whether I am saving more or less each month.
- As a user, I want to see my top expense categories ranked by total spend over a period so I can focus on reducing the biggest line items.
- As a user, I want to see my investment portfolio allocation by category so I can visualize diversification.
- As a user, I want to see contribution trends for my investments over time so I can track my investing consistency.

## Endpoints

### 1. `GET /dashboard/analytics/cash-flow-trend`

Returns monthly income, expenses, and net cash flow over a configurable window.

**Suitable visualisations:** Area chart (stacked or layered) with income and expenses as filled areas and net cash flow as an overlaid line. Alternatively, a grouped bar chart with income/expenses side-by-side per month and a savings rate sparkline beneath each bar.

**Query parameters:**

| Parameter | Type   | Default | Description                           |
| --------- | ------ | ------- | ------------------------------------- |
| months    | int    | 6       | Number of months to return (3, 6, 12) |

**Response shape:**

| Field    | Type   | Description                              |
| -------- | ------ | ---------------------------------------- |
| months   | array  | Ordered list of monthly data points      |
| currency | string | User's configured currency               |

Each item in `months`:

| Field      | Type   | Description                                    |
| ---------- | ------ | ---------------------------------------------- |
| label      | string | Month label (e.g., "Mar 2026")                 |
| income     | number | Total income for the month                     |
| expenses   | number | Total expenses for the month                   |
| netCashFlow| number | Income minus expenses                          |
| savingsRate| number | Percentage of income saved (0 if no income)    |

### 2. `GET /dashboard/analytics/income-breakdown`

Returns income broken down by source type over a configurable window.

**Suitable visualisations:** Donut chart showing proportional income by source. Alternatively, a horizontal bar chart ranked by amount, or a treemap for users with many income sources.

**Query parameters:**

| Parameter | Type   | Default | Description                           |
| --------- | ------ | ------- | ------------------------------------- |
| months    | int    | 3       | Lookback window (1, 3, 6, 12)        |

**Response shape:**

| Field       | Type   | Description                              |
| ----------- | ------ | ---------------------------------------- |
| period      | string | Human-readable period description        |
| currency    | string | User's configured currency               |
| totalIncome | number | Total income across all sources          |
| sources     | array  | Per-source breakdown data                |

Each item in `sources`:

| Field      | Type   | Description                                  |
| ---------- | ------ | -------------------------------------------- |
| source     | string | Income source type (e.g., "Salary", "Freelance") |
| amount     | number | Total income from this source                |
| percentage | number | Percentage of total income                   |
| colorKey   | string | Color token for frontend rendering           |

### 3. `GET /dashboard/analytics/category-comparison`

Compares spending by category between two consecutive months, enabling delta visualisations.

**Suitable visualisations:** Grouped horizontal bar chart with current and previous month bars side-by-side per category. Alternatively, a butterfly/tornado chart with the previous month extending left and current month extending right. Change percentages can be shown as inline badge indicators (green for decrease, red for increase).

**Query parameters:**

| Parameter | Type   | Default       | Description                                  |
| --------- | ------ | ------------- | -------------------------------------------- |
| month     | string | current month | The "current" month in `YYYY-MM` format; the prior month is derived automatically |

**Response shape:**

| Field           | Type   | Description                              |
| --------------- | ------ | ---------------------------------------- |
| currentMonth    | string | Label for the current month              |
| previousMonth   | string | Label for the prior month                |
| currency        | string | User's configured currency               |
| categories      | array  | Per-category comparison data             |

Each item in `categories`:

| Field            | Type   | Description                                       |
| ---------------- | ------ | ------------------------------------------------- |
| category         | string | Category name                                     |
| currentAmount    | number | Spend in the current month                        |
| previousAmount   | number | Spend in the prior month                          |
| changeAmount     | number | Difference (current minus previous)               |
| changePercentage | number | Percentage change (0 if no prior spend)           |

### 4. `GET /dashboard/analytics/top-categories`

Returns the top N expense categories ranked by total spend over a configurable window.

**Suitable visualisations:** Horizontal bar chart ranked by spend (highest first). Alternatively, a pie/donut chart for proportional view, or a radial bar chart. A progress-bar list (each category as a filled bar relative to the top spender) also works well for dashboard cards.

**Query parameters:**

| Parameter | Type   | Default | Description                           |
| --------- | ------ | ------- | ------------------------------------- |
| months    | int    | 3       | Lookback window (1, 3, 6, 12)        |
| limit     | int    | 5       | Number of categories to return (1-10) |

**Response shape:**

| Field      | Type   | Description                              |
| ---------- | ------ | ---------------------------------------- |
| period     | string | Human-readable period description        |
| currency   | string | User's configured currency               |
| totalSpent | number | Total across all categories in period    |
| categories | array  | Ranked category data                     |

Each item in `categories`:

| Field      | Type   | Description                                  |
| ---------- | ------ | -------------------------------------------- |
| rank       | int    | 1-based rank                                 |
| category   | string | Category name                                |
| amount     | number | Total spend in this category                 |
| percentage | number | Percentage of total spend                    |
| colorKey   | string | Color token for frontend rendering           |

### 5. `GET /dashboard/analytics/investment-allocation`

Returns the user's investment portfolio broken down by category.

**Suitable visualisations:** Donut chart with category segments sized by portfolio percentage. Alternatively, a stacked bar (single horizontal bar divided by category), or a treemap where each rectangle represents a category with size proportional to balance.

**Response shape:**

| Field          | Type   | Description                              |
| -------------- | ------ | ---------------------------------------- |
| currency       | string | User's configured currency               |
| totalValue     | number | Sum of all investment current balances   |
| accountCount   | int    | Total number of active accounts          |
| categories     | array  | Per-category allocation data             |

Each item in `categories`:

| Field        | Type   | Description                                     |
| ------------ | ------ | ----------------------------------------------- |
| categoryName | string | Investment category name (e.g., "ISA", "LISA")  |
| categorySlug | string | Category slug                                   |
| totalBalance | number | Sum of current balances in this category        |
| accountCount | int    | Number of accounts in this category             |
| percentage   | number | Percentage of total portfolio value             |
| colorKey     | string | Color token for frontend rendering              |

### 6. `GET /dashboard/analytics/investment-trend`

Returns monthly investment contribution totals over a configurable window.

**Suitable visualisations:** Vertical bar chart with monthly contribution amounts. Alternatively, a line chart with markers at each month, or a combination chart with bars for contributions and a line for cumulative total. The `accountCount` field can power a secondary axis or tooltip detail.

**Query parameters:**

| Parameter | Type   | Default | Description                           |
| --------- | ------ | ------- | ------------------------------------- |
| months    | int    | 6       | Number of months to return (3, 6, 12) |

**Response shape:**

| Field            | Type   | Description                              |
| ---------------- | ------ | ---------------------------------------- |
| currency         | string | User's configured currency               |
| totalContributed | number | Sum of contributions in the window       |
| months           | array  | Monthly contribution data                |

Each item in `months`:

| Field         | Type   | Description                              |
| ------------- | ------ | ---------------------------------------- |
| label         | string | Month label (e.g., "Mar 2026")           |
| contributions | number | Total contributions for that month       |
| accountCount  | int    | Number of accounts contributed to        |

## Business Rules

- All amounts are numeric; the frontend controls currency formatting.
- Percentages are numbers (e.g., `12.5` for 12.5%).
- Period calculations must respect the user's configured `monthStartDay` and `timezone`, using the same `DashboardPeriod` logic already in the module.
- The `months` query parameter must be validated to accepted values only (3, 6, or 12 for trend endpoints; 1, 3, 6, or 12 for top-categories). Invalid values should fall back to the default.
- The `limit` query parameter for top-categories must be clamped to 1-10.
- The `month` parameter (for category-comparison) must be a valid `YYYY-MM` string; invalid values should fall back to the current month.
- Investment analytics read from the `investments` schema and must filter out soft-deleted accounts and contributions (`deleted_at_utc IS NULL`).
- If a user has no data for a given period, return empty arrays or zero values rather than errors.
- Spending breakdown percentages within a single response must sum to 100 (within rounding tolerance); use the same remainder-correction approach as the existing summary endpoint.
- Color keys should cycle through `chart-1` through `chart-6` in rank order, consistent with the existing spending breakdown.

## Authorization

- All endpoints require authentication (valid JWT) and the existing `dashboard:read` policy.
- All data is scoped to the authenticated user.

## Acceptance Criteria

- [ ] `GET /dashboard/analytics/cash-flow-trend` returns correct monthly data for 3, 6, and 12 month windows.
- [ ] `GET /dashboard/analytics/income-breakdown` returns correct per-source totals with percentages summing to 100.
- [ ] `GET /dashboard/analytics/category-comparison` correctly calculates deltas between two consecutive months.
- [ ] `GET /dashboard/analytics/top-categories` returns categories in descending order of spend with correct percentages.
- [ ] `GET /dashboard/analytics/investment-allocation` returns correct portfolio breakdown by investment category.
- [ ] `GET /dashboard/analytics/investment-trend` returns correct monthly contribution data.
- [ ] All endpoints return 401 for unauthenticated requests.
- [ ] All endpoints respect the user's timezone and month start day settings.
- [ ] Empty data scenarios return zero values and empty arrays, not errors.
- [ ] Invalid query parameters fall back to documented defaults.
- [ ] Unit tests cover calculation logic (savings rate, percentage breakdowns, change percentages, period boundaries).
- [ ] Integration tests cover happy paths and edge cases for each endpoint.

## Out of Scope

- Custom date range filtering beyond the provided `months`/`month` parameters.
- Exporting analytics data (CSV, PDF).
- Real-time/WebSocket streaming of analytics.
- Caching or materialized views (can be added as a performance optimization later).
- Budget vs. actual comparisons (requires a budgeting feature).
- Cross-currency aggregation (all amounts assume the user's single configured currency).
