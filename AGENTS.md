# AGENTS.md (Monorepo Root)

## Scope
- Applies to the full monorepo unless a deeper `AGENTS.md` overrides it.
- Repositories in scope:
- `expensify-backend/`
- `expensify-frontend/`

## Product Direction (MVP)
- Build a personal finance product focused on:
- Expense tracking (CRUD + filtering)
- Income tracking (CRUD + filtering)
- Recurring subscriptions and monthly import
- Monthly insights (income, expenses, net cash flow, trends)
- AI chat grounded in user financial data

## Outcome Priorities
- Correctness over speed for money-related logic.
- UX clarity for financial data interpretation.
- Strong tenant/user data isolation.
- Reliability and observability by default.

## Cross-Cutting Quality Bar
- Security:
- Follow OWASP-aligned input validation and auth checks.
- Never leak cross-user data.
- Avoid storing or logging secrets.
- Reliability:
- Prefer deterministic, idempotent workflows where duplicates are possible.
- Handle partial failures with explicit error surfaces.
- Performance:
- Target fast monthly summary experiences for typical user data volumes.
- Avoid N+1 patterns and unnecessary round trips.
- Accessibility:
- User-facing web flows should meet WCAG 2.1 AA baseline.

## AI/Agent Working Rules
- Keep changes scoped and modular.
- Prefer small, reviewable PR-sized edits.
- Preserve existing architecture patterns instead of introducing one-off structures.
- If a requirement is ambiguous, choose the safer behavior and document the assumption.
- Update docs/tests with behavior changes.

## Delivery Expectations
- Every feature should include:
- Clear acceptance criteria
- Test coverage at the right level (unit/integration/e2e as applicable)
- Operational visibility (logs/metrics/traces where relevant)
- If adding new endpoints or contracts, keep frontend/backend integration in mind.

## Monorepo Coordination
- Shared conventions should live at this root file.
- App-specific implementation rules belong in each app folder:
- `expensify-backend/AGENTS.md`
- `expensify-frontend/AGENTS.md`
