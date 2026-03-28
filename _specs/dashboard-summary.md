# Dashboard Summary Endpoint

## Overview

Add a `GET /api/v1/dashboard/summary` endpoint that returns the complete dashboard payload in a single request. This aggregates monthly income, monthly expenses, net cash flow, spending breakdown by category, monthly performance history, and recent transactions into one response.

## User Stories

- As a user, I want to load my dashboard with a single API call so the UI renders quickly without multiple round-trips.
- As a user, I want to see my monthly income, expenses, and net cash flow with change percentages so I can track trends.
- As a user, I want to see a spending breakdown by category so I can understand where my money goes.
- As a user, I want to see a 6-month performance chart so I can visualize income vs. expenses over time.
- As a user, I want to see my 5 most recent transactions so I can stay on top of recent activity.

## Response Shape

### `monthlyIncome`

| Field            | Type   | Description                              |
| ---------------- | ------ | ---------------------------------------- |
| totalAmount      | number | Total income for the current month       |
| currency         | string | ISO 4217 currency code                   |
| changePercentage | number | Percentage change vs. the prior month    |

### `monthlyExpenses`

| Field            | Type   | Description                              |
| ---------------- | ------ | ---------------------------------------- |
| totalAmount      | number | Total expenses for the current month     |
| currency         | string | ISO 4217 currency code                   |
| changePercentage | number | Percentage change vs. the prior month    |

### `netCashFlow`

| Field            | Type   | Description                              |
| ---------------- | ------ | ---------------------------------------- |
| totalAmount      | number | Income minus expenses for current month  |
| currency         | string | ISO 4217 currency code                   |
| changePercentage | number | Percentage change vs. the prior month    |

### `spendingBreakdown`

Array of objects:

| Field      | Type   | Description                                  |
| ---------- | ------ | -------------------------------------------- |
| category   | string | Category name                                |
| amount     | number | Total spent in this category                 |
| percentage | number | Percentage of total spending (e.g., `12.5`)  |
| colorKey   | string | Color token key for frontend rendering       |

### `monthlyPerformance`

Array of objects (most recent 6 months):

| Field    | Type   | Description              |
| -------- | ------ | ------------------------ |
| month    | string | Month label (e.g., "Mar 2026") |
| income   | number | Total income for the month     |
| expenses | number | Total expenses for the month   |

### `recentTransactions`

Array of the 5 most recent transactions:

| Field     | Type   | Description                                |
| --------- | ------ | ------------------------------------------ |
| id        | string | Transaction identifier                     |
| merchant  | string | Merchant name                              |
| category  | string | Transaction category                       |
| amount    | number | Transaction amount                         |
| type      | string | `income` or `expense`                      |
| status    | string | Transaction status                         |
| timestamp | string | ISO 8601 timestamp                         |

## Business Rules

- All currency amounts are numbers (not pre-formatted strings); the frontend controls formatting.
- Percentages are numbers (e.g., `12.5` for 12.5%).
- The hero section reads from `netCashFlow`; there is no separate available-capital field.
- Change percentages compare the current calendar month to the immediately prior calendar month.
- If there is no prior-month data, change percentage should be `0`.
- Spending breakdown percentages must sum to 100 (within rounding tolerance).
- Monthly performance returns exactly 6 months, including the current month. Months with no data return `0` for income and expenses.
- Recent transactions are ordered by timestamp descending, limited to 5.

## Authorization

- Endpoint requires authentication (valid JWT).
- Data is scoped to the authenticated user.

## Acceptance Criteria

- [ ] `GET /api/v1/dashboard/summary` returns 200 with the documented response shape for an authenticated user.
- [ ] Returns 401 for unauthenticated requests.
- [ ] Monthly income, expenses, and net cash flow are calculated correctly for the current month.
- [ ] Change percentages accurately reflect comparison with the prior month.
- [ ] Spending breakdown covers all expense categories for the current month.
- [ ] Monthly performance returns exactly 6 months of data.
- [ ] Recent transactions returns at most 5 transactions, ordered by most recent first.
- [ ] All amounts are numeric; no pre-formatted currency strings in the response.
- [ ] Integration tests cover the happy path and edge cases (no data, single month of data).

## Out of Scope

- Filtering by date range or custom periods.
- Pagination of transactions beyond the top 5.
- Real-time/WebSocket updates.
- Caching strategy (can be added later).
