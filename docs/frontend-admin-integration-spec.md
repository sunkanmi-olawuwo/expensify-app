# Frontend Admin Integration Spec

## Purpose

This spec describes the backend surface the frontend can use to build the admin interface.
It includes:

- implemented admin-accessible routes that exist today
- planned investment admin routes defined in [_specs/investment-module.md](C:\Users\sunka\source\repos\BigTechHQ\expensify-backend\_specs\investment-module.md)

Unless a section explicitly says `Planned`, treat it as currently implemented.

Base URL:

- `/api/v1`

Auth:

- Bearer JWT in `Authorization: Bearer <token>`
- Backend authorization is claim-based, not route-prefix-based
- The frontend can use JWT claims for feature gating, but `403 Forbidden` must remain the source of truth

Admin claims currently used by this surface:

- `admin:users:read`
- `admin:users:delete`
- `admin:users:preferences:manage`
- `admin:expenses:read`
- `admin:income:read`
- `admin:investments:read` planned
- `admin:investments:manage-categories` planned

Useful JWT claims if the frontend wants to gate admin UI early:

- `role`: `"Admin"` for seeded admin users
- the claim types above, each with value `"true"`

## Recommended Admin IA

The backend supports a read-heavy admin console with four areas:

1. User directory
2. User financial drill-down
3. Preference catalogs
4. Investment administration

Suggested screen model:

- `Admin / Users`
  - searchable, sortable, paginated table
  - row action: delete user
  - row action: open user detail workspace
- `Admin / Users / {userId}`
  - summary header from selected user row
  - expenses tab
  - expense monthly summary tab/card
  - income tab
  - income monthly summary tab/card
  - planned: investments tab
  - planned: investment portfolio summary tab/card
- `Admin / Catalogs`
  - currencies tab
  - timezones tab
  - planned: investment categories tab
  - create/edit modal or side panel
- `Admin / Investments`
  - planned global cross-user investment accounts table
  - filter by category
  - support/reporting oriented read-only surface

There is currently no admin endpoint to update another user's profile, role, expenses, income, or investments from the admin surface.

## Endpoint Matrix

### 1. List Users

- `GET /api/v1/users`
- Claim required: `admin:users:read`

Query params from OpenAPI:

- `FilterBy`: only `role` has effect
- `FilterQuery`: used when `FilterBy=role`
- `SortBy`: `Email`, `FirstName`, `LastName`, `Role`
- `SearchQuery`: case-insensitive match on email, first name, or last name
- `Page`: default `1`
- `PageSize`: default `10`, max `100`
- `SortOrder`: `asc` or `desc`, default `asc`

Backend behaviors:

- invalid `SortBy` falls back to email
- invalid `SortOrder` falls back to ascending
- invalid or non-positive `Page` falls back to `1`
- invalid or non-positive `PageSize` falls back to `10`
- search is case-insensitive

Response body:

```json
{
  "page": 1,
  "pageSize": 10,
  "totalCount": 2,
  "curentPage": 1,
  "totalPages": 1,
  "users": [
    {
      "id": "7c3cbaf6-70ec-4fab-9d0a-3fdc2af4b4bb",
      "email": "admin@test.com",
      "firstName": "Admin",
      "lastName": "User",
      "role": "Admin"
    }
  ]
}
```

Pagination headers:

- `X-Pagination-CurrentPage`
- `X-Pagination-PageSize`
- `X-Pagination-TotalCount`
- `X-Pagination-TotalPages`

Important API quirk:

- the body property is spelled `curentPage` instead of `currentPage`
- the header is spelled correctly as `X-Pagination-CurrentPage`

Frontend recommendation:

- treat header values as authoritative for table pagination
- still map `curentPage` because the generated API client uses it

### 2. Delete User

- `DELETE /api/v1/users/{id}`
- Claim required: `admin:users:delete`

Path params:

- `id`: user domain GUID from the users list response

Success:

- `204 No Content`

Failure cases to handle:

- `401` unauthenticated or invalid token
- `403` authenticated but missing admin delete claim
- `404` user no longer exists
- `429` rate limited

Frontend recommendation:

- require explicit confirmation
- optimistically remove only after `204`
- on `404`, close the modal and refresh the table

### 3. Read Currencies

- `GET /api/v1/currencies`
- Baseline claim required: `users:read`
- Admin-only behavior: `includeInactive=true` requires `admin:users:preferences:manage`

Query params:

- `includeInactive`: default `false`

Response item:

```json
{
  "code": "GBP",
  "name": "British Pound",
  "symbol": "GBP",
  "minorUnit": 2,
  "isActive": true,
  "isDefault": true,
  "sortOrder": 1
}
```

Frontend recommendation:

- admin catalog screens should always call with `includeInactive=true`
- user-facing preference pickers should keep using `includeInactive=false`

### 4. Create Currency

- `POST /api/v1/currencies`
- Claim required: `admin:users:preferences:manage`

Request body:

```json
{
  "code": "USD",
  "name": "US Dollar",
  "symbol": "$",
  "minorUnit": 2,
  "isActive": true,
  "isDefault": false,
  "sortOrder": 10
}
```

Validation and business rules:

- `code` must match `^[A-Za-z]{3}$`
- `name` required, max `100`
- `symbol` required, max `10`
- `minorUnit` must be `0..9`
- if `isDefault=true`, then `isActive` must also be `true`
- there must always be at least one active default currency
- if `isDefault=true`, the backend automatically clears the old default
- `code` is normalized to uppercase before persistence

Responses:

- `201` with `CurrencyResponse`
- `400` validation or business-rule failure
- `409` duplicate code or default conflict
- `429` rate limited

### 5. Update Currency

- `PUT /api/v1/currencies/{code}`
- Claim required: `admin:users:preferences:manage`

Path params:

- `code`: existing currency code

Request body:

```json
{
  "name": "US Dollar",
  "symbol": "$",
  "minorUnit": 2,
  "isActive": true,
  "isDefault": false,
  "sortOrder": 10
}
```

Extra backend rules:

- the current default currency cannot be updated to non-default
- the current default currency cannot be deactivated
- to change the default, update another active currency with `isDefault=true`

Responses:

- `200` with `CurrencyResponse`
- `400`, `404`, `409`, `429`

### 6. Read Timezones

- `GET /api/v1/timezones`
- Baseline claim required: `users:read`
- Admin-only behavior: `includeInactive=true` requires `admin:users:preferences:manage`

Query params:

- `includeInactive`: default `false`

Response item:

```json
{
  "ianaId": "Europe/London",
  "displayName": "Europe/London",
  "isActive": true,
  "isDefault": true,
  "sortOrder": 1
}
```

### 7. Create Timezone

- `POST /api/v1/timezones`
- Claim required: `admin:users:preferences:manage`

Request body:

```json
{
  "ianaId": "Europe/London",
  "displayName": "Europe/London",
  "isActive": true,
  "isDefault": false,
  "sortOrder": 10
}
```

Validation and business rules:

- `ianaId` required, max `100`
- `displayName` required, max `200`
- if `isDefault=true`, then `isActive` must also be `true`
- there must always be at least one active default timezone
- if `isDefault=true`, the backend automatically clears the old default

Responses:

- `201` with `TimezoneResponse`
- `400`, `409`, `429`

### 8. Update Timezone

- `PUT /api/v1/timezones/{ianaId}`
- Claim required: `admin:users:preferences:manage`

Path params:

- `ianaId`: timezone ID in the URL
- frontend should URL-encode this path segment because IDs can contain `/`

Request body:

```json
{
  "displayName": "Europe/London",
  "isActive": true,
  "isDefault": false,
  "sortOrder": 10
}
```

Extra backend rules:

- the current default timezone cannot be updated to non-default
- the current default timezone cannot be deactivated
- to change the default, update another active timezone with `isDefault=true`

Responses:

- `200` with `TimezoneResponse`
- `400`, `404`, `409`, `429`

### 9. List Expenses For A User

- `GET /api/v1/expenses/users/{userId}`
- Claim required: `admin:expenses:read`

Path params:

- `userId`: user domain GUID

Query params:

- `period`: required, format `yyyy-MM`
- `categoryId`: optional GUID
- `merchant`: optional case-insensitive contains filter
- `tagIds`: optional repeated GUID query param
- `minAmount`: optional decimal
- `maxAmount`: optional decimal
- `paymentMethod`: optional exact string
- `sortBy`: `date` or `amount` or `merchant`
- `sortOrder`: `asc` or `desc`
- `page`: default `1`
- `pageSize`: default `20`, max `100`

Backend behaviors:

- soft-deleted expenses are excluded
- tag filtering is `ANY`, not `ALL`
- invalid `sortBy` falls back to `date`
- invalid `sortOrder` falls back to descending
- invalid `page` falls back to `1`
- invalid `pageSize` falls back to `20`
- `period` is interpreted using the selected user's `monthStartDay`, not the admin's

`paymentMethod` values to use:

- `Cash`
- `Card`
- `Transfer`
- `Wallet`
- `Other`
- `DirectDebit`

Response body:

```json
{
  "page": 1,
  "pageSize": 20,
  "totalCount": 1,
  "curentPage": 1,
  "totalPages": 1,
  "items": [
    {
      "id": "34d66a46-f8ab-4c53-9611-21da4f0b6a60",
      "amount": 42.50,
      "currency": "GBP",
      "date": "2026-03-15",
      "categoryId": "2b7fb4ab-1a24-4b7b-9af2-1b96d2f63991",
      "categoryName": "Travel",
      "merchant": "TfL",
      "note": "Tube",
      "paymentMethod": "Card",
      "tagIds": ["d969d2ef-b05c-4bf1-9186-f8e44946a180"],
      "tagNames": ["Commute"]
    }
  ]
}
```

Pagination headers:

- same four `X-Pagination-*` headers as users

Important API quirk:

- `curentPage` is also misspelled here

### 10. Expense Monthly Summary For A User

- `GET /api/v1/expenses/users/{userId}/summary/monthly`
- Claim required: `admin:expenses:read`

Query params:

- `period`: required, format `yyyy-MM`

Response body:

```json
{
  "period": "2026-03",
  "totalAmount": 420.75,
  "expenseCount": 9,
  "categories": [
    {
      "categoryId": "2b7fb4ab-1a24-4b7b-9af2-1b96d2f63991",
      "categoryName": "Travel",
      "amount": 120.25
    }
  ]
}
```

### 11. List Income For A User

- `GET /api/v1/income/users/{userId}`
- Claim required: `admin:income:read`

Path params:

- `userId`: user domain GUID

Query params:

- `period`: required, format `yyyy-MM`
- `source`: optional case-insensitive contains filter
- `type`: optional enum filter
- `minAmount`: optional decimal
- `maxAmount`: optional decimal
- `sortBy`: `date` or `amount` or `source`
- `sortOrder`: `asc` or `desc`
- `page`: default `1`
- `pageSize`: default `20`, max `100`

`type` values to use:

- `Salary`
- `Freelance`
- `Business`
- `Investment`
- `Gift`
- `Refund`
- `Other`

Backend behaviors:

- soft-deleted income rows are excluded
- invalid `sortBy` falls back to `date`
- invalid `sortOrder` falls back to descending
- invalid `page` falls back to `1`
- invalid `pageSize` falls back to `20`
- `period` is interpreted using the selected user's `monthStartDay`

Response body:

```json
{
  "page": 1,
  "pageSize": 20,
  "totalCount": 1,
  "curentPage": 1,
  "totalPages": 1,
  "items": [
    {
      "id": "2f111f89-79af-4c82-bf67-a5af0c2eab4c",
      "amount": 3500.00,
      "currency": "GBP",
      "date": "2026-03-01",
      "source": "Employer Ltd",
      "type": "Salary",
      "note": "March payroll"
    }
  ]
}
```

### 12. Income Monthly Summary For A User

- `GET /api/v1/income/users/{userId}/summary/monthly`
- Claim required: `admin:income:read`

Query params:

- `period`: required, format `yyyy-MM`

Response body:

```json
{
  "period": "2026-03",
  "totalAmount": 3500.00,
  "incomeCount": 1,
  "types": [
    {
      "type": "Salary",
      "amount": 3500.00
    }
  ]
}
```

### 13. Planned: Read Investment Categories

- `GET /api/v1/investments/categories`
- Status: planned in [_specs/investment-module.md](C:\Users\sunka\source\repos\BigTechHQ\expensify-backend\_specs\investment-module.md)
- Baseline claim required: `investments:read`
- Admin-only behavior: admins receive all categories, non-admin users receive only active categories

Spec notes:

- this is a shared route, not an `/admin` route
- regular users can call it, but only admins get the full catalog
- unlike currencies/timezones, the spec does not currently define an `includeInactive` query parameter

Expected response shape inferred from the module spec domain model:

```json
[
  {
    "id": "d01fb17f-a640-4d2b-abec-3d6bd79f99ce",
    "name": "ISA",
    "slug": "isa",
    "isActive": true
  },
  {
    "id": "40427e0c-49f3-48d5-81e3-95987254af95",
    "name": "Fixed Deposit",
    "slug": "fixed-deposit",
    "isActive": false
  }
]
```

Seeded categories from the investment spec:

- `ISA`
- `LISA`
- `MutualFund`
- `FixedDeposit`
- `Other`

Category-specific field relevance from the investment spec:

| Category | interestRate | maturityDate |
| --- | --- | --- |
| `ISA` | optional | not used |
| `LISA` | optional | not used |
| `MutualFund` | not used | not used |
| `FixedDeposit` | required | required |
| `Other` | optional | optional |

Frontend recommendation:

- investment account create/edit screens should source category options from this endpoint
- admin catalog screens should expect inactive categories to be present without an extra query flag

### 14. Planned: Update Investment Category Active State

- `PUT /api/v1/admin/investments/categories/{id}`
- Status: planned in [_specs/investment-module.md](C:\Users\sunka\source\repos\BigTechHQ\expensify-backend\_specs\investment-module.md)
- Claim required: `admin:investments:manage-categories`

Path params:

- `id`: investment category GUID

Request body from the spec:

```json
{
  "isActive": false
}
```

Expected response:

- `200 OK` with the updated category

Expected response shape inferred from the module spec domain model:

```json
{
  "id": "40427e0c-49f3-48d5-81e3-95987254af95",
  "name": "Fixed Deposit",
  "slug": "fixed-deposit",
  "isActive": false
}
```

Business rules from the investment spec:

- admins can activate or deactivate seeded categories
- admins cannot create categories at runtime
- accounts must reference an active category
- category-specific validation depends on category choice
- `FixedDeposit` requires both `interestRate` and `maturityDate`
- `MutualFund` does not use `interestRate` or `maturityDate`

Frontend recommendation:

- present this as a toggle in the admin catalog UI
- show warning text that deactivated categories cannot be selected for new accounts
- do not offer create/delete actions for investment categories

### 15. Planned: List Investment Accounts Across All Users

- `GET /api/v1/admin/investments`
- Status: planned in [_specs/investment-module.md](C:\Users\sunka\source\repos\BigTechHQ\expensify-backend\_specs\investment-module.md)
- Claim required: `admin:investments:read`

Spec notes:

- returns a paginated array of investment accounts
- this is intended for reporting and support purposes

Supported filtering from the investment spec:

- pagination
- optional `categoryId` filter is defined for user account listing and is likely to be relevant here
- no admin-specific filter contract is specified yet

Relationship note:

- investment accounts reference `categoryId`
- the frontend should resolve category label and slug by joining account rows with `GET /api/v1/investments/categories`
- do not design this as an account payload containing an investment-type enum string

Expected item shape inferred from the module spec domain model:

```json
{
  "id": "717e24c3-9897-4f8e-a85a-d17109d7bdd8",
  "userId": "7c3cbaf6-70ec-4fab-9d0a-3fdc2af4b4bb",
  "name": "Vanguard ISA",
  "provider": "Vanguard",
  "categoryId": "d01fb17f-a640-4d2b-abec-3d6bd79f99ce",
  "currency": "GBP",
  "interestRate": null,
  "maturityDate": null,
  "currentBalance": 12450.32,
  "notes": "Stocks and shares ISA"
}
```

Pagination guidance:

- the investment spec says pagination is supported but does not define header names or page envelope fields yet
- unless the implementation specifies otherwise, the frontend should expect this to align with the existing paged admin conventions used by users, expenses, and income

Frontend recommendation:

- design this as a read-only global investment accounts table first
- include user, account name, provider, category, currency, and current balance columns
- leave room for later drill-down into contributions or per-user investment summaries, but those admin endpoints are not planned yet in the spec

## Cross-Cutting UI Rules

### Period handling

- The admin UI should use a month picker that emits `yyyy-MM`
- Do not assume calendar-month semantics when explaining results to users
- Expense and income periods are shifted by the target user's `monthStartDay`
- The summary response echoes the requested `period`; it does not return the computed date boundaries
- the planned investment admin endpoints do not currently define any period parameter

Practical implication:

- two users can see different effective date windows for the same requested `period`

### Error handling

Problem responses are standard `ProblemDetails`:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Users.DefaultCurrencyRequired",
  "status": 400,
  "detail": "At least one active default currency is required."
}
```

Validation failures may also include an `errors` extension payload.

Frontend guidance:

- `401`: route to login or refresh session
- `403`: hide admin action and show "insufficient permissions"
- `400`: show inline form or filter error
- `404`: show stale-data message and refresh
- `409`: show retryable conflict message
- `429`: show retry-later toast/banner

### Data typing

Use these frontend types:

```ts
type Guid = string;
type MonthPeriod = string; // yyyy-MM
type IsoDate = string; // yyyy-MM-dd

type UserListItem = {
  id: Guid;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
};

type PagedUsers = {
  page: number;
  pageSize: number;
  totalCount: number;
  curentPage: number;
  totalPages: number;
  users: UserListItem[];
};

type Currency = {
  code: string;
  name: string;
  symbol: string;
  minorUnit: number;
  isActive: boolean;
  isDefault: boolean;
  sortOrder: number;
};

type Timezone = {
  ianaId: string;
  displayName: string;
  isActive: boolean;
  isDefault: boolean;
  sortOrder: number;
};

type ExpenseListItem = {
  id: Guid;
  amount: number;
  currency: string;
  date: IsoDate;
  categoryId: Guid;
  categoryName: string;
  merchant: string;
  note: string;
  paymentMethod: string;
  tagIds: Guid[];
  tagNames: string[];
};

type IncomeListItem = {
  id: Guid;
  amount: number;
  currency: string;
  date: IsoDate;
  source: string;
  type: string;
  note: string;
};

type InvestmentCategory = {
  id: Guid;
  name: string;
  slug: string;
  isActive: boolean;
};

type InvestmentAccountListItem = {
  id: Guid;
  userId: Guid;
  name: string;
  provider: string | null;
  categoryId: Guid;
  currency: string;
  interestRate: number | null;
  maturityDate: string | null;
  currentBalance: number;
  notes: string | null;
};
```

## Gaps The Frontend Should Expect

- No admin endpoint exists to fetch a full user profile for an arbitrary user by ID
- No admin endpoint exists to edit another user's profile or role
- No admin endpoint exists to create, update, delete, or restore another user's expenses or income
- No planned admin endpoint exists yet for per-user investment drill-down, contribution history, or portfolio summary
- The planned investment admin routes are not implemented yet, and their exact response envelopes are still subject to implementation details
- No aggregate admin dashboard endpoint exists across all users

If the admin UI needs those workflows, they require backend additions rather than frontend composition.
