# Product Requirements Document (PRD)
## Personal Finance Tracker with Monthly Expenses, Income, Recurring Subscriptions, and AI Chat

## 1) Document Metadata
- **Product name:** Personal Finance Tracker
- **Version:** v1.1 (MVP)
- **Last updated:** 2026-02-25
- **Status:** Draft for implementation
- **Audience:** Product, engineering, QA, and AI coding agents

---

## 2) Problem Statement
Users need a simple way to track monthly expenses and income, automatically carry forward recurring subscriptions each month, and ask AI questions about their financial status.

Current alternatives (manual spreadsheets, disconnected apps) are slow and difficult to query in natural language.

---

## 3) Product Goals and Non-Goals

### Goals (MVP)
1. Track **expenses per month** with clean CRUD flows.
2. Track **income per month** with source/type labeling.
3. Support **recurring monthly subscriptions** and let users import them into a selected month.
4. Provide an **AI chat interface** where users can ask finance questions based on their own data.
5. Provide monthly insights and summaries (cash flow, top spend categories, anomalies).

### Non-Goals (MVP)
- Budget setup, budget limits, or budget alerts.
- Bank integrations (Plaid/Yodlee).
- Tax filing workflows.
- Shared household finance permissions.

---

## 4) Target Users
1. **Working professionals** who want monthly cash flow visibility.
2. **Freelancers** who need variable income tracking month-to-month.
3. **Subscription-heavy users** who need recurring charges imported each month without manual re-entry.

---

## 5) Core User Stories
1. As a user, I can create, edit, delete, and view expense records.
2. As a user, I can create, edit, delete, and view income records.
3. As a user, I can define recurring monthly subscriptions.
4. As a user, I can import recurring subscriptions into a month with one action.
5. As a user, I can see monthly totals: total income, total expenses, and net cash flow.
6. As a user, I can ask the AI chat: "How am I doing this month?" and get data-backed answers.
7. As a user, I can ask comparative questions like "Did I spend more on food than last month?"

---

## 6) Functional Requirements

### 6.1 Authentication & User Settings
- Email/password authentication.
- Session/JWT support.
- User preferences: currency, timezone, month start day.

### 6.2 Expense Management
- Expense fields: `id`, `userId`, `amount`, `currency`, `date`, `category`, `merchant`, `note`, `tags[]`, `paymentMethod`, timestamps.
- CRUD endpoints.
- Filtering by month, category, merchant, tags, and amount range.

### 6.3 Income Management
- Income fields: `id`, `userId`, `amount`, `currency`, `date`, `source`, `type`, `note`, timestamps.
- CRUD endpoints.
- Filtering by month, source, type, and amount range.

### 6.4 Recurring Subscription Management
- Subscription fields: `id`, `userId`, `name`, `amount`, `currency`, `billingDay`, `category`, `merchant`, `isActive`, `startMonth`, `endMonth (nullable)`, timestamps.
- CRUD endpoints for subscriptions.
- "Import recurring items" action for a target month:
  - Creates expense entries for active subscriptions.
  - Prevents duplicates if already imported for that month.
  - Returns import summary (`created`, `skipped`, `errors`).

### 6.5 Monthly Dashboard
- Monthly totals:
  - Total income
  - Total expenses
  - Net cash flow (`income - expenses`)
- Category-wise expense breakdown.
- Month-over-month trend comparison for income, expenses, and net flow.
- Top expense categories and unusual spikes.

### 6.6 AI Chat (Data-Grounded)
- Chat endpoint where user asks questions in natural language.
- AI answers must be grounded in user financial data for selected periods.
- Response format:
  - Direct answer
  - Supporting numbers (with period)
  - Optional recommendations
- Example supported queries:
  - "How much did I spend this month?"
  - "What is my net cash flow this month?"
  - "Which subscriptions cost me the most in the last 3 months?"
  - "Compare this month's income to last month."
- Guardrails:
  - No legal/tax/investment advice.
  - No fabricated values; if missing data, say so clearly.

### 6.7 CSV Import
- Import expenses and income via CSV.
- Header mapping step (user maps columns to supported fields).
- Validation + preview before commit.
- Duplicate detection heuristics.

---

## 7) Non-Functional Requirements
- **Performance:** Monthly dashboard response under 2s for up to 10k transactions/user.
- **Reliability:** 99.9% monthly API uptime target.
- **Security:** OWASP-aligned validation, encrypted transport/storage.
- **Privacy:** Strict user data isolation and access controls.
- **Observability:** Structured logs, request IDs, chat/audit traces.
- **Accessibility:** WCAG 2.1 AA for key web flows.

---

## 8) Conceptual Data Model

### Entities
1. **User**
   - `id`, `email`, `passwordHash`, `displayName`, `currency`, `timezone`, `monthStartDay`, timestamps

2. **Expense**
   - `id`, `userId`, `amount`, `currency`, `date`, `category`, `merchant`, `note`, `tags`, `paymentMethod`, `sourceType` (`manual|csv|subscription-import`), timestamps

3. **Income**
   - `id`, `userId`, `amount`, `currency`, `date`, `source`, `type`, `note`, `sourceType` (`manual|csv`), timestamps

4. **Subscription**
   - `id`, `userId`, `name`, `amount`, `currency`, `billingDay`, `category`, `merchant`, `isActive`, `startMonth`, `endMonth`, timestamps

5. **SubscriptionImportRun**
   - `id`, `userId`, `month`, `createdCount`, `skippedCount`, `errorCount`, `executedAt`

6. **ChatSession**
   - `id`, `userId`, `title`, timestamps

7. **ChatMessage**
   - `id`, `sessionId`, `role` (`user|assistant|system`), `content`, `groundingMeta`, `createdAt`

---

## 9) API Surface (MVP Draft)

### Auth
- `POST /api/v1/auth/register`
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh`

### Expenses
- `GET /api/v1/expenses?month=YYYY-MM`
- `POST /api/v1/expenses`
- `PATCH /api/v1/expenses/{expenseId}`
- `DELETE /api/v1/expenses/{expenseId}`

### Income
- `GET /api/v1/income?month=YYYY-MM`
- `POST /api/v1/income`
- `PATCH /api/v1/income/{incomeId}`
- `DELETE /api/v1/income/{incomeId}`

### Subscriptions
- `GET /api/v1/subscriptions`
- `POST /api/v1/subscriptions`
- `PATCH /api/v1/subscriptions/{subscriptionId}`
- `DELETE /api/v1/subscriptions/{subscriptionId}`
- `POST /api/v1/subscriptions/import?month=YYYY-MM`

### Dashboard & Insights
- `GET /api/v1/dashboard?month=YYYY-MM`
- `GET /api/v1/insights?month=YYYY-MM`

### AI Chat
- `POST /api/v1/chat/sessions`
- `GET /api/v1/chat/sessions/{sessionId}`
- `POST /api/v1/chat/sessions/{sessionId}/messages`

### CSV Import
- `POST /api/v1/import/csv/expenses`
- `POST /api/v1/import/csv/income`

---

## 10) AI-Agent-Friendly Implementation Plan

### Epic A: Transactions Core
- Build expense and income entities + migrations.
- Add CRUD APIs with validation.
- Add filtering by month and category/source.

### Epic B: Recurring Subscriptions
- Build subscription entities + APIs.
- Build monthly import job/endpoint with idempotency and duplicate checks.
- Add import-run logging.

### Epic C: Dashboard & Insights
- Build aggregation service for monthly totals and trends.
- Build endpoint for monthly financial snapshot and category analysis.

### Epic D: AI Chat
- Build chat session/message storage.
- Build data retrieval layer for grounded responses.
- Build prompt orchestration and response guardrails.

### Epic E: Import and Quality
- Add CSV import for expense and income.
- Add robust validation and error reporting.
- Add integration tests for import + chat grounding correctness.

---

## 11) Acceptance Criteria (MVP)
1. User can manage monthly expenses and income with accurate totals.
2. User can create recurring subscriptions and import them into any month.
3. Subscription import is idempotent for the same month and user.
4. Dashboard returns total income, total expenses, and net cash flow for selected month.
5. AI chat answers finance queries with explicit supporting numbers.
6. CSV import supports both expenses and income with clear validation feedback.

---

## 12) Success Metrics
- **Activation:** % users adding at least 3 expenses and 1 income in first week.
- **Feature adoption:** % users creating at least one recurring subscription.
- **Import usage:** monthly count of subscription import runs.
- **AI usefulness:** % chat responses marked helpful by users.
- **Quality:** dashboard p95 latency, chat grounding error rate, import failure rate.

---

## 13) Risks & Mitigations
1. **Inaccurate AI responses**
   - Mitigation: strict grounding with explicit computed facts and period metadata.
2. **Recurring import duplicates**
   - Mitigation: idempotency keys and per-month subscription import markers.
3. **Data quality issues in CSV**
   - Mitigation: mapping preview, schema validation, and actionable row-level errors.
4. **Privacy concerns**
   - Mitigation: tenant isolation, audit logs, and encrypted data handling.

---

## 14) Open Questions
1. Should the subscription import run automatically on month start or only manually in MVP?
2. Should subscription imports be editable in bulk after import?
3. Should AI chat support downloadable monthly summaries in MVP or Phase 2?
4. Do we need multi-currency before v1.0 GA?

---

## 15) AI Handoff Notes (for Coding Agents)
- Implement epics in order: A → B → C → D → E.
- Keep controllers thin; place logic in domain/services.
- Make subscription import idempotent and test it thoroughly.
- Every AI answer must include computed evidence values.
- Keep API versioning stable under `/api/v1`.
