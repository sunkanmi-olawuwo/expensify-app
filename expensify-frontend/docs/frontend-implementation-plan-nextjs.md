# Frontend Implementation Plan (Next.js)
## Expensify UI: Award-Winning, Modern, Accessible

## 0) Target Standards
- Styling system: Tailwind CSS with app-level design tokens.
- UI foundation: `shadcn/ui` + Radix primitives as a base layer only.
- Component strategy: build encapsulated app-owned components on top of shadcn primitives to preserve consistent theme, colors, and behavior.
- Next.js approach: App Router, React Server Components first, Client Components only where needed.
- Engineering quality: FAANG-level execution discipline for architecture, testing, performance, security, and delivery.

## 1) Plan Intent
- Build a production-grade Next.js frontend integrated with the backend `/api/v1`.
- Deliver a premium, intentional, memorable UI while meeting WCAG 2.1 AA.
- Execute in small, achievable chunks with explicit acceptance criteria and copy-paste prompts.

## 2) Non-Negotiables
- Accessibility: keyboard-first UX, focus visibility, semantic markup, color contrast compliance.
- Reliability: robust loading/error/empty states and safe auth/session handling.
- Performance: strict Core Web Vitals focus and bundle discipline.
- Maintainability: typed boundaries, clear feature ownership, reusable primitives.

## 3) Platform Decisions (Modern Next.js Practices)
- Next.js App Router + TypeScript strict mode.
- React Server Components for read-heavy pages.
- Route-level `loading.tsx`, `error.tsx`, and `not-found.tsx`.
- Tailwind CSS as primary styling approach.
- `shadcn/ui` primitives wrapped by reusable app components (for example: `AppButton`, `AppInput`, `AppDialog`, `DataTable`, `MetricCard`) to avoid direct primitive sprawl across feature code.
- Centralized theming via CSS variables + Tailwind tokens for colors, spacing, typography, and motion.
- `next/font` for font optimization.
- React Query for client-side caching/mutations where server rendering is not ideal.
- Zod validation for forms and API input normalization.
- OpenAPI-based typed API layer where practical.
- Feature flags for progressive rollout.
- Charting stack: `Recharts` (default) with accessible custom wrappers and consistent theming tokens.

## 4) Working Assumptions
- API base URL uses version prefix: `/api/v1`.
- Auth is JWT bearer with `/users/login` and `/users/refresh`.
- Profile endpoints are JWT-user scoped (`/users/profile`).
- Development CORS already includes `http://localhost:3000` and `http://127.0.0.1:3000`.

## 5) Delivery Chunks

### Chunk 1: Bootstrap, Monorepo Hygiene, and UI Base
- Goal: initialize project and quality guardrails.
- Deliverables:
- Next.js App Router project with strict TypeScript.
- Initialize Tailwind + `shadcn/ui`.
- Define component layering:
- `ui/base` (wrapped shadcn primitives),
- `ui/composite` (domain-agnostic composites),
- `features/*` (feature-specific composition only).
- ESLint, Prettier, import rules, and path aliases.
- Unit test and E2E scaffolding.
- Acceptance:
- `lint`, `typecheck`, `test`, `build` are green.

Prompt:
```text
Create a Next.js App Router project with TypeScript strict mode. Initialize Tailwind and shadcn/ui, but enforce an app-owned component layer that wraps shadcn primitives (do not use raw shadcn components directly in feature pages). Set up ESLint, Prettier, Vitest, Testing Library, and Playwright. Add scripts for lint, typecheck, test, test:e2e, and build. Use a feature-oriented folder structure and document conventions.
```

### Chunk 2: Design System and Brand-Quality UI Language
- Goal: build a premium and distinct design system.
- Deliverables:
- Design tokens for color, type, spacing, radii, elevation, and motion.
- Customized `shadcn/ui` primitives (Button, Input, Select, Dialog, Tabs, Table, Toast, Tooltip).
- Encapsulated reusable components exported from app-owned UI packages/directories only.
- Accessibility states (hover, active, disabled, focus-visible).
- Acceptance:
- WCAG AA contrast for core text and interactions.
- Keyboard and screen-reader sanity checks pass.

Prompt:
```text
Build a premium design system on top of shadcn/ui for a finance product using Tailwind CSS. Define CSS variable tokens and customize primitives so the UI is distinct and high-end, not template-like. Create app-owned reusable wrappers and composites so feature code consumes only encapsulated components. Ensure full accessibility states, reduced-motion support, and responsive typography.
```

### Chunk 3: App Shell, Navigation, and Information Architecture
- Goal: deliver a resilient layout system.
- Deliverables:
- Responsive shell with sidebar/topbar and command navigation.
- Route groups for public/authenticated/admin spaces.
- Global loading, error, empty-state components.
- Acceptance:
- No layout shift issues; polished behavior on mobile and desktop.

Prompt:
```text
Implement a responsive app shell in Next.js App Router using shadcn/ui with sidebar/topbar navigation, route groups, and reusable loading/error/empty states. Keep interactions accessible and visually polished.
```

### Chunk 4: Typed API Integration and Data Contracts
- Goal: enforce robust backend contracts.
- Deliverables:
- Typed client for `/api/v1`.
- Shared HTTP client with auth header, refresh handling hooks, error normalization.
- Query/mutation hooks and pagination header utilities.
- Acceptance:
- Typed integration for auth, profile, expenses, income, categories, tags.

Prompt:
```text
Create a typed API layer for /api/v1 with a shared HTTP client, structured error handling, pagination header parsing, and React Query hooks for auth/profile/expenses/income/categories/tags. Keep it strongly typed end-to-end.
```

### Chunk 4.1: Automated API Contract Export and Frontend Client Generation
- Goal: keep frontend contracts automatically synchronized with backend API changes.
- Deliverables:
- Add frontend script to generate TypeScript contracts/client from backend OpenAPI spec (`specification_v1.json`).
- Add CI step to regenerate and fail when generated output is stale.
- Document local workflow (`build backend -> generate frontend contracts -> run checks`).
- Acceptance:
- Contract generation is one command.
- CI fails if contracts are out of sync with backend spec.

Prompt:
```text
Set up automated frontend API contract generation from the backend OpenAPI spec in a monorepo. Add npm scripts for generate:api and generate:api:check, generate TypeScript contracts/client into a dedicated api folder, and enforce freshness in CI by failing if generated files change.
```

### Chunk 5: Authentication and Session Lifecycle
- Goal: secure and frictionless auth.
- Deliverables:
- Login/logout UI and route protection.
- Silent refresh flow with retry on token expiry.
- Session bootstrap and invalid session fallback UX.
- Acceptance:
- Protected routes are reliable.
- Expired tokens refresh without user disruption.

Prompt:
```text
Implement JWT auth in Next.js with login/logout, protected routes, and automatic refresh using /users/login and /users/refresh. Handle invalid, expired, and revoked-token scenarios with clean UX and deterministic state transitions.
```

### Chunk 6: Dashboard Experience (Flagship UI)
- Goal: create a signature first impression.
- Deliverables:
- Monthly KPI cards (income, expense, net flow).
- Period selector and trend cards.
- Chart surfaces:
- Expenses by category (donut/pie from `/expenses/summary/monthly`).
- Income by type (donut/pie from `/income/summary/monthly`).
- Net flow trend (line/area across selected periods, composed from monthly summary calls).
- Skeleton loading and smooth state transitions.
- Acceptance:
- High visual quality and responsive behavior across breakpoints.
- Charts have accessible labels/legends/tooltips and no color-only meaning.

Prompt:
```text
Build a premium dashboard page with monthly KPI cards, period controls, and polished loading states. Add charting with Recharts: expenses-by-category donut, income-by-type donut, and multi-period net-flow trend. Emphasize visual hierarchy, accessibility, and smooth interactions. Integrate backend summary data from /api/v1.
```

### Chunk 7: Expenses Module (CRUD + Filter + Pagination)
- Goal: complete expense workflows.
- Deliverables:
- Expense list, filter bar, sorting, pagination controls.
- Create/edit/delete flows with validated forms.
- Category/tag selection and payment method UX.
- Acceptance:
- All flows are reliable and instantly reflected in UI state.

Prompt:
```text
Implement expenses UI with list/filter/sort/pagination and create/edit/delete forms using shadcn/ui patterns. Integrate category/tag data and parse X-Pagination-* headers for pagination controls. Provide robust error and empty states.
```

### Chunk 8: Income Module (CRUD + Filter + Pagination)
- Goal: complete income workflows.
- Deliverables:
- Income list and filters.
- Create/edit/delete flows.
- Monthly income summary surface.
- Acceptance:
- UX and interaction quality match expenses module standards.

Prompt:
```text
Implement income UI with list/filter/sort/pagination and create/edit/delete workflows, matching the quality and accessibility of the expenses module. Integrate monthly summary data and production-grade state handling.
```

### Chunk 9: Profile and Preferences
- Goal: user preference ownership.
- Deliverables:
- Profile page for name, currency, timezone, month start day.
- Strong client + server error messaging.
- Acceptance:
- Updates persist and are reflected correctly in dependent views.

Prompt:
```text
Build the /users/profile UI with accessible forms for first name, last name, currency, timezone, and month start day. Add inline validation, server error mapping, and polished success/error feedback.
```

### Chunk 10: Admin Experience (Optional MVP Scope)
- Goal: role-aware admin capabilities.
- Deliverables:
- User listing with filtering/pagination.
- Admin monthly summaries per user (income/expenses).
- Role-protected routes and navigation visibility.
- Acceptance:
- Non-admin cannot access admin routes or data.

Prompt:
```text
Create admin pages for user management and per-user monthly summaries using /users, /expenses/users/{userId}/summary/monthly, and /income/users/{userId}/summary/monthly. Add role-aware route protection and production-ready table/filter UX.
```

### Chunk 11: Accessibility and Motion Polish Pass
- Goal: move from good to exceptional.
- Deliverables:
- Keyboard and screen-reader audit fixes.
- ARIA improvements and form error announcements.
- Motion polish with `prefers-reduced-motion` support.
- Acceptance:
- Accessibility QA passes for all core journeys.

Prompt:
```text
Run a full accessibility and interaction polish pass. Fix keyboard flow, ARIA semantics, focus management, and error announcements. Add tasteful motion choreography that respects prefers-reduced-motion and enhances perceived quality.
```

### Chunk 12: Production Hardening and Release Readiness
- Goal: launch quality.
- Deliverables:
- Error monitoring and web-vitals instrumentation.
- Performance optimization pass (bundle splits, caching, dynamic loading).
- Chart performance hardening (memoized transformations, adaptive tick density, lazy-loaded chart modules).
- CI pipeline and deployment checklist.
- Acceptance:
- Stable production build and green CI with enforced quality gates.

Prompt:
```text
Harden the frontend for production with observability, performance optimization, robust error boundaries, and CI quality gates. Include charting performance and accessibility hardening (lazy-load charts, reduce re-renders, keyboard and screen-reader support for chart summaries). Produce a deployment checklist and rollback plan.
```

## 9) Charting Implementation Notes
- Data sources:
- `GET /api/v1/expenses/summary/monthly?period=YYYY-MM`
- `GET /api/v1/income/summary/monthly?period=YYYY-MM`
- For trends, aggregate repeated monthly summary calls over a selected period range.
- Initial chart set:
- Donut: expense category distribution.
- Donut: income type distribution.
- Line/area: monthly income vs expenses vs net.
- Bar: top categories/types for selected month.
- Accessibility requirements:
- Provide textual summary above each chart.
- Ensure legends and data labels are keyboard reachable where interactive.
- Never rely only on color; include pattern/labels/value annotations.
- Provide table fallback for critical chart data.
- Performance requirements:
- Lazy-load non-critical charts below the fold.
- Memoize chart data transformations.
- Keep chart props stable to minimize re-renders.
- Testing requirements:
- Unit tests for transformation helpers.
- Visual regression snapshot for key chart states.
- E2E test to confirm charts render for known seeded data periods.

## 6) FAANG-Style Ways of Working (Required)
- Architecture governance:
- Short RFC/design doc required before major features.
- ADRs for irreversible decisions.
- Frontend system ownership:
- No direct raw `shadcn/ui` primitive imports in feature modules; use app-owned wrappers/components.
- Token/theming changes go through design-system review to maintain visual consistency.
- Code review discipline:
- Mandatory reviews with clear ownership.
- No direct merges to protected branches.
- Testing pyramid:
- Unit tests for logic, integration tests for API hooks/auth, E2E for business-critical journeys.
- Performance governance:
- Define budgets for LCP, CLS, INP, and bundle size; fail CI on regressions.
- Security governance:
- Threat modeling for auth/session and sensitive data exposure.
- Dependency audits and secret scanning in CI.
- Release discipline:
- Trunk-based development, small PRs, feature flags, staged rollouts, rollback playbooks.

## 7) Suggested Execution Order
1. Chunks 1-4 (foundation + typed API baseline)
2. Chunk 4.1 (contract automation and CI enforcement)
3. Chunk 5 (auth)
4. Chunks 6-9 (core user experience)
5. Chunk 10 (admin if in scope)
6. Chunks 11-12 (polish + production hardening)

## 8) Definition of Done (Frontend MVP)
- Users can authenticate and manage expenses/income/profile end-to-end.
- UI quality is modern, distinctive, responsive, and polished.
- Accessibility baseline (WCAG 2.1 AA) is met on core flows.
- CI gates pass for lint/typecheck/tests/build.
- Frontend integration with backend `/api/v1` is stable and observable.
