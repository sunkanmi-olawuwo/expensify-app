# Product Requirements Document (PRD)
## Monthly Expense Manager & Insights App (AI-Agent Friendly)

## 1) Document Metadata
- **Product name:** Monthly Expense Manager
- **Version:** v1.0 (MVP-focused)
- **Author:** Product Team
- **Last updated:** 2026-02-25
- **Status:** Draft for implementation planning
- **Primary audience:** Engineers, designers, QA, data/analytics, AI coding agents

---

## 2) Problem Statement
Users struggle to understand where their money goes each month. Existing approaches (spreadsheets/manual notes) are time-consuming, error-prone, and do not provide actionable insights.

The app should help users:
1. Log and categorize expenses quickly.
2. Track monthly spending vs budget.
3. Receive clear insights and trends to improve spending behavior.

---

## 3) Goals and Non-Goals

### Goals (MVP)
- Let users create, edit, delete, and view expenses.
- Let users define monthly budgets by category.
- Show monthly dashboards with key insights (top categories, trends, budget overrun alerts).
- Support import of transactions from CSV.
- Provide AI-generated plain-language spending insights and recommendations.

### Non-Goals (MVP)
- Bank account direct integrations (Plaid/Yodlee/etc.).
- Multi-currency accounting and FX conversion.
- Tax filing workflows.
- Household/shared wallets with complex permission models.
- Automated investment advice.

---

## 4) Target Users & Personas

1. **Young Professional (Primary)**
   - Wants a quick monthly snapshot and overspending alerts.
   - Values simplicity over deep accounting features.

2. **Family Budget Planner (Secondary)**
   - Tracks category-wise monthly limits.
   - Needs month-over-month comparison and recurring expense visibility.

3. **Freelancer (Secondary)**
   - Wants to separate personal categories and tag business-related expenses.

---

## 5) Core User Stories

1. As a user, I can add an expense with amount, date, category, and note.
2. As a user, I can edit or delete incorrect expense entries.
3. As a user, I can set category-wise monthly budgets.
4. As a user, I can view this month’s total spend and remaining budget.
5. As a user, I can see which categories increased most compared to last month.
6. As a user, I can upload a CSV and map columns to import expenses.
7. As a user, I can view AI-generated insights in simple language.
8. As a user, I can receive alerts for budget threshold breaches (e.g., 80%, 100%).

---

## 6) Functional Requirements

### 6.1 Authentication & User Profile
- Email/password sign-up and login.
- Secure session/JWT management.
- Profile settings: preferred currency, month start date.

### 6.2 Expense Management
- Fields: `id`, `userId`, `amount`, `currency`, `date`, `category`, `merchant`, `note`, `tags[]`, `paymentMethod`, `isRecurring`, `createdAt`, `updatedAt`.
- CRUD operations for expenses.
- Filter/search by date range, category, amount, merchant, and tags.

### 6.3 Category & Budget Management
- System categories + custom categories.
- Monthly budget assignment per category and optional overall budget.
- Budget progress indicators (% used, amount left).

### 6.4 Dashboard & Insights
- Current month summary: total spent, total budget, remaining budget.
- Category breakdown (table + chart-ready API output).
- Month-over-month change percentages.
- Top 3 unusual spend spikes.
- AI narrative insights:
  - "You spent 22% more on dining vs last month."
  - "Utilities are stable, within 5% of average."
  - "If this pace continues, you'll exceed groceries budget by $80."

### 6.5 CSV Import
- Upload CSV (size limit configurable, default 5MB).
- User maps CSV headers to expected fields.
- Preview + validation errors before final import.
- Duplicate detection (same date + amount + merchant heuristic).

### 6.6 Notifications
- Budget threshold notifications (in-app for MVP).
- Threshold defaults: 80%, 100%.
- Notification center with read/unread state.

### 6.7 AI Insight Engine (MVP)
- Input: aggregated monthly and historical spending data.
- Output: concise bullet insights + 1-3 recommendations.
- Guardrails:
  - No financial guarantee language.
  - No investment/tax/legal advice.
  - Explain insight basis with concrete numbers.

---

## 7) Non-Functional Requirements
- **Performance:** Dashboard load under 2s for up to 10k expense rows/user.
- **Reliability:** 99.9% monthly API availability target.
- **Security:** OWASP-aligned input validation, encrypted data in transit and at rest.
- **Privacy:** User data isolation by tenant/user ID.
- **Observability:** Structured logs, request IDs, and key business metrics.
- **Accessibility:** WCAG 2.1 AA for core flows.

---

## 8) Data Model (Conceptual)

### Entities
1. **User**
   - `id`, `email`, `passwordHash`, `displayName`, `currency`, `monthStartDay`, timestamps

2. **Expense**
   - `id`, `userId`, `amount`, `currency`, `date`, `categoryId`, `merchant`, `note`, `tags`, `paymentMethod`, `isRecurring`, timestamps

3. **Category**
   - `id`, `userId (nullable for system category)`, `name`, `icon`, `color`, timestamps

4. **Budget**
   - `id`, `userId`, `month (YYYY-MM)`, `categoryId (nullable for total budget)`, `limitAmount`, timestamps

5. **Notification**
   - `id`, `userId`, `type`, `title`, `message`, `readAt`, timestamps

6. **Insight**
   - `id`, `userId`, `month`, `summary`, `details`, `confidence`, `createdAt`

---

## 9) API Surface (MVP Draft)

### Auth
- `POST /api/v1/auth/register`
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh`

### Expenses
- `GET /api/v1/expenses`
- `POST /api/v1/expenses`
- `PATCH /api/v1/expenses/{expenseId}`
- `DELETE /api/v1/expenses/{expenseId}`

### Categories & Budgets
- `GET /api/v1/categories`
- `POST /api/v1/categories`
- `GET /api/v1/budgets?month=YYYY-MM`
- `PUT /api/v1/budgets/{budgetId}`

### Insights & Dashboard
- `GET /api/v1/dashboard?month=YYYY-MM`
- `POST /api/v1/insights/generate?month=YYYY-MM`
- `GET /api/v1/insights?month=YYYY-MM`

### Import & Notifications
- `POST /api/v1/import/csv`
- `GET /api/v1/notifications`
- `PATCH /api/v1/notifications/{notificationId}/read`

---

## 10) AI-Agent Implementation Backlog (Execution-Friendly)

### Epic A: Expense Tracking Foundation
- Implement expense domain model + migrations.
- Add expense CRUD endpoints + validation.
- Add unit/integration tests for CRUD and filtering.

### Epic B: Budgeting
- Implement category and budget models.
- Add endpoints for monthly budget setup and retrieval.
- Add budget progress calculation service.

### Epic C: Dashboard & Insights
- Build monthly aggregation service.
- Add dashboard endpoint returning totals, category splits, trends.
- Add insight generation orchestrator (rules-first + LLM narrative layer).

### Epic D: CSV Import
- Implement CSV parser + mapping flow.
- Add validation and duplicate detection.
- Add import summary response (`imported`, `skipped`, `errors`).

### Epic E: Notifications
- Trigger threshold notifications from budget calculations.
- Add notification read/unread APIs.

---

## 11) Acceptance Criteria (MVP)
1. User can create and retrieve expenses with correct filtering by month/category.
2. Budget usage is calculated accurately and matches stored expenses.
3. Dashboard endpoint returns totals and category-level breakdown for selected month.
4. Insight endpoint generates human-readable, data-backed insights with no prohibited advice.
5. CSV import handles valid files and provides actionable validation errors.
6. Budget threshold alerts appear in notification center.

---

## 12) Success Metrics
- **Activation:** % users who add ≥5 expenses in first 7 days.
- **Engagement:** Monthly active users who view dashboard at least 2 times/month.
- **Retention:** 30-day returning users.
- **Behavior change proxy:** % users reducing overspent categories month-over-month.
- **Quality:** CSV import failure rate, dashboard API p95 latency, insight generation error rate.

---

## 13) Risks & Mitigations
1. **Low-quality AI insights**
   - Mitigation: use deterministic rule layer and numeric grounding before narrative generation.
2. **Messy CSV formats**
   - Mitigation: guided column mapping and robust validation messages.
3. **User distrust of recommendations**
   - Mitigation: show transparent "why" with numbers and period comparisons.
4. **Data privacy concerns**
   - Mitigation: strict access controls, audit logs, encryption.

---

## 14) Open Questions
1. Should recurring expenses be auto-generated monthly in MVP or Phase 2?
2. Should we allow account sharing (family mode) in MVP?
3. Is push/email notification support required at launch, or only in-app?
4. Should insights be generated on-demand or on a scheduled batch job?

---

## 15) Out-of-Scope for This PRD Iteration
- Detailed UI wireframes and design system specs.
- Final database indexing strategy.
- Provider-specific LLM prompt and model selection details.
- Deployment and infra cost model.

---

## 16) AI Handoff Notes (for Coding Agents)
- Prefer incremental delivery by epics A → E.
- Keep APIs backward-compatible once published (`/api/v1`).
- Add test coverage alongside each feature slice.
- Keep business logic in domain/services; keep controllers thin.
- For insight generation, log intermediate computed facts for auditability.
- Ensure every recommendation references at least one computed metric.
