# Investment Module

## Overview

Add a new Investment module to the modular monolith that allows users to track savings and investment accounts such as ISAs, LISAs, Mutual Funds, and fixed-term deposits. Users can hold multiple accounts per type (e.g., ISAs with different providers), log individual contributions over time, and view a portfolio summary. The module follows the same Clean Architecture pattern as Expenses and Income modules.

## User Stories

- As a user, I want to create investment accounts so I can track my ISA, LISA, mutual fund, and fixed deposit holdings.
- As a user, I want to hold multiple accounts of the same type (e.g., ISAs with different providers or across tax years).
- As a user, I want to log individual contributions to an account so I have a full deposit history.
- As a user, I want to update an account's current balance so my portfolio reflects actual values.
- As a user, I want to see a portfolio summary with total contributed, current value, and gain/loss.
- As a user, I want to delete an account I no longer hold.
- As an admin, I want to view all users' investment data for reporting and support purposes.

## Domain Concepts

### Investment Account

Represents a single savings or investment account.

| Field           | Type            | Description                                                        |
| --------------- | --------------- | ------------------------------------------------------------------ |
| id              | Guid            | Unique identifier                                                  |
| userId          | Guid            | Owner of the account                                               |
| name            | string          | User-given name (e.g., "Vanguard ISA", "Nationwide LISA")          |
| provider        | string?         | Optional provider/platform name                                    |
| categoryId      | Guid            | Reference to an Investment Category                                |
| currency        | string          | ISO 4217 currency code                                             |
| interestRate    | decimal?        | Annual interest rate as percentage (e.g., 4.5 for 4.5%), if applicable |
| maturityDate    | DateTimeOffset? | When locked funds become accessible, if applicable                 |
| currentBalance  | decimal         | Current total value of the account (user-updated)                  |
| notes           | string?         | Optional notes                                                     |
| createdAt       | DateTimeOffset  | Record creation timestamp                                          |
| updatedAt       | DateTimeOffset  | Record last-updated timestamp                                      |

### Investment Category

Predefined catalog of investment types, seeded at startup. Admins can activate or deactivate categories but cannot create new ones at runtime.

| Field       | Type           | Description                                              |
| ----------- | -------------- | -------------------------------------------------------- |
| id          | Guid           | Unique identifier                                        |
| name        | string         | Display name (e.g., "ISA", "LISA", "Fixed Deposit")      |
| slug        | string         | Machine-friendly key (e.g., "isa", "lisa", "fixed-deposit") |
| isActive    | bool           | Whether users can select this category for new accounts  |
| createdAt   | DateTimeOffset | Record creation timestamp                                |
| updatedAt   | DateTimeOffset | Record last-updated timestamp                            |

**Seeded categories**: ISA, LISA, MutualFund, FixedDeposit, Other.

Category-specific field relevance:

| Category     | interestRate | maturityDate |
| ------------ | ------------ | ------------ |
| ISA          | optional     | not used     |
| LISA         | optional     | not used     |
| MutualFund   | not used     | not used     |
| FixedDeposit | required     | required     |
| Other        | optional     | optional     |

### Contribution

Represents a single deposit into an investment account.

| Field           | Type           | Description                                          |
| --------------- | -------------- | ---------------------------------------------------- |
| id              | Guid           | Unique identifier                                    |
| accountId       | Guid           | The investment account this contribution belongs to   |
| amount          | decimal        | Amount deposited                                     |
| date            | DateTimeOffset | When the contribution was made                       |
| notes           | string?        | Optional notes (e.g., "monthly standing order")      |
| createdAt       | DateTimeOffset | Record creation timestamp                            |

### Portfolio Summary

Aggregated view computed from all investment accounts.

| Field              | Type   | Description                                           |
| ------------------ | ------ | ----------------------------------------------------- |
| totalContributed   | number | Sum of all contributions across all accounts          |
| currentValue       | number | Sum of currentBalance across all accounts             |
| totalGainLoss      | number | currentValue minus totalContributed                   |
| gainLossPercentage | number | Percentage gain or loss                               |
| accountCount       | number | Number of active investment accounts                  |
| currency           | string | User's default currency                               |

## Endpoints

### Accounts

#### `POST /api/v1/investments`

Create a new investment account.

- **Auth**: Authenticated user
- **Policy**: `investments:write`
- **Request body**: name, provider, categoryId, currency, interestRate, maturityDate, currentBalance, notes
- **Validation**: categoryId must reference an active category; interestRate and maturityDate validated per category rules
- **Response**: 201 Created with the created account

#### `GET /api/v1/investments`

List all investment accounts for the authenticated user.

- **Auth**: Authenticated user
- **Policy**: `investments:read`
- **Response**: 200 OK with array of investment accounts
- **Supports**: pagination, optional categoryId filter

#### `GET /api/v1/investments/{id}`

Get a single investment account by ID.

- **Auth**: Authenticated user
- **Policy**: `investments:read`
- **Response**: 200 OK with the account (including total contributed computed from contributions), or 404 Not Found

#### `PUT /api/v1/investments/{id}`

Update an existing investment account.

- **Auth**: Authenticated user (must own the account)
- **Policy**: `investments:write`
- **Request body**: name, provider, categoryId, currency, interestRate, maturityDate, currentBalance, notes
- **Validation**: same category-specific rules as creation
- **Response**: 200 OK with the updated account

#### `DELETE /api/v1/investments/{id}`

Soft-delete an investment account and its contributions.

- **Auth**: Authenticated user (must own the account)
- **Policy**: `investments:delete`
- **Response**: 204 No Content

### Contributions

#### `POST /api/v1/investments/{id}/contributions`

Log a new contribution to an investment account.

- **Auth**: Authenticated user (must own the account)
- **Policy**: `investments:write`
- **Request body**: amount, date, notes
- **Response**: 201 Created with the created contribution

#### `GET /api/v1/investments/{id}/contributions`

List all contributions for an investment account.

- **Auth**: Authenticated user (must own the account)
- **Policy**: `investments:read`
- **Response**: 200 OK with array of contributions ordered by date descending
- **Supports**: pagination

### Summary

#### `GET /api/v1/investments/summary`

Get portfolio summary for the authenticated user.

- **Auth**: Authenticated user
- **Policy**: `investments:read`
- **Response**: 200 OK with portfolio summary

### Categories (Admin)

#### `GET /api/v1/investments/categories`

List all investment categories (active ones for regular users, all for admins).

- **Auth**: Authenticated user
- **Policy**: `investments:read`
- **Response**: 200 OK with array of categories (non-admin users see only active categories)

#### `PUT /api/v1/admin/investments/categories/{id}`

Activate or deactivate an investment category.

- **Auth**: Admin
- **Policy**: `admin:investments:manage-categories`
- **Request body**: isActive
- **Response**: 200 OK with the updated category

### Admin

#### `GET /api/v1/admin/investments`

List all investment accounts across all users (admin only).

- **Auth**: Admin
- **Policy**: `admin:investments:read`
- **Response**: 200 OK with paginated array of investment accounts

## Business Rules

- All currency amounts are numbers; the frontend controls formatting.
- Gain/loss percentage uses `Math.Abs(totalContributed)` as denominator to handle edge cases.
- If a user has no investment accounts, the summary returns zeroes.
- Soft-delete follows the existing recycle-bin pattern used by other modules. Deleting an account also soft-deletes its contributions.
- Accounts must reference an active investment category. Categories are seeded at startup; admins can activate/deactivate but not create new ones.
- Category-specific validation: FixedDeposit requires interestRate and maturityDate; other categories treat them as optional or ignored per the field relevance table.
- A user can have multiple accounts of the same category (e.g., ISAs with different providers or for different tax years).
- Contribution amounts must be positive values.
- Current balance is user-managed; the system does not auto-calculate it from contributions (contributions may not account for interest, bonuses, or fees).
- Interest rate and maturity date are informational fields; the system does not auto-compound or calculate projected values.
- Each account belongs to exactly one user; users can only access their own data unless they have admin privileges.

## Authorization

- All endpoints require authentication (valid JWT).
- Data is scoped to the authenticated user for non-admin endpoints.
- Policies: `investments:read`, `investments:write`, `investments:delete`, `admin:investments:read`, `admin:investments:manage-categories`.
- Admin and User roles must be seeded with appropriate claims.

## Acceptance Criteria

- [ ] Investment module follows Clean Architecture with Domain, Application, Infrastructure, and Presentation layers.
- [ ] Architecture tests enforce layer dependency rules.
- [ ] `POST /api/v1/investments` creates an account and returns 201.
- [ ] `GET /api/v1/investments` returns the user's accounts with pagination and categoryId filter.
- [ ] `GET /api/v1/investments/{id}` returns 200 for owned account with total contributed, 404 for missing.
- [ ] `PUT /api/v1/investments/{id}` updates an owned account.
- [ ] `DELETE /api/v1/investments/{id}` soft-deletes an owned account and its contributions.
- [ ] `POST /api/v1/investments/{id}/contributions` logs a contribution and returns 201.
- [ ] `GET /api/v1/investments/{id}/contributions` returns contributions ordered by date descending with pagination.
- [ ] `GET /api/v1/investments/summary` returns correct portfolio summary.
- [ ] `GET /api/v1/investments/categories` returns active categories for users, all categories for admins.
- [ ] `PUT /api/v1/admin/investments/categories/{id}` allows admins to toggle category active status.
- [ ] FixedDeposit accounts require interestRate and maturityDate; validation rejects missing fields.
- [ ] Accounts cannot be created with a deactivated category.
- [ ] `GET /api/v1/admin/investments` is restricted to admin role.
- [ ] Returns 401 for unauthenticated requests, 403 for unauthorized.
- [ ] EF Core migrations create the investment tables in a module-specific schema.
- [ ] Unit tests cover handlers, validators, and domain logic.
- [ ] Integration tests cover happy path and edge cases.
- [ ] Module is registered in Program.cs and solution file.

## Out of Scope

- Auto-calculation of balance from contributions and interest.
- Real-time price feeds or external API integration.
- LISA government bonus calculation or annual ISA allowance enforcement.
- Creating new categories at runtime (requires code change + migration seed).
- Tax calculation or reporting.
- Multi-currency conversion.
- Withdrawal tracking.
- Projected growth or interest compounding calculations.
