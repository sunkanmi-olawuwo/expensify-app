# AGENTS.md (Frontend)

## Scope
- Applies to `expensify-frontend/` and all subdirectories.
- Overrides root guidance where frontend-specific rules are required.

## Product and UX Bar
- Build a premium, modern, accessible finance UI.
- Design quality target: polished, intentional, and differentiated.
- Accessibility target: WCAG 2.1 AA on all core user journeys.

## Core Stack and Architecture
- Framework: Next.js App Router + TypeScript strict mode.
- Styling: Tailwind CSS with centralized design tokens.
- UI primitives: `shadcn/ui` and Radix as base building blocks.
- Data layer: typed API client for backend `/api/v1` + query/mutation orchestration.

## Component System Rules (Non-Negotiable)
- Do not scatter direct raw `shadcn/ui` usage in feature pages.
- Build app-owned reusable wrappers and composites (for example: `AppButton`, `AppInput`, `AppDialog`, `MetricCard`, `DataTable`).
- Feature modules should consume app-owned UI components, not low-level primitives.
- Keep theming/colors/spacing/motion centralized via tokens and shared styles.

## Next.js Best Practices
- Prefer Server Components for read-heavy views.
- Use Client Components only for interactive behavior.
- Use route-level `loading.tsx`, `error.tsx`, and `not-found.tsx`.
- Optimize fonts/images and avoid unnecessary client-side JS.
- Keep state local where possible; avoid global state unless clearly needed.

## Charting Guidance
- Use a consistent chart stack (Recharts unless project decides otherwise).
- Build chart wrappers/components that align with theme tokens and accessibility rules.
- Provide textual summaries and avoid color-only encoding of meaning.
- Include graceful empty/loading/error chart states.

## Accessibility Rules
- Keyboard-first interactions with visible focus states.
- Semantic headings/landmarks and proper ARIA usage.
- Color contrast must meet AA requirements.
- Respect `prefers-reduced-motion`.

## Quality and Testing
- Required checks before completion:
- Lint + typecheck + tests + build pass.
- Test pyramid:
- Unit tests for utilities and component logic.
- Integration tests for data hooks and auth/session behavior.
- E2E tests for key flows (auth, expenses, income, profile).

## Security and Data Handling
- Respect auth boundaries and never expose sensitive tokens in logs.
- Handle token expiry and refresh deterministically.
- Normalize and surface API errors clearly without leaking internals.

## Delivery Discipline
- Keep changes small and reviewable.
- Document major architectural decisions.
- Preserve consistency with existing design system and feature conventions.
