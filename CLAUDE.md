# CLAUDE.md

AI operating instructions for the Restaurant POS & QR Ordering System.

Read order: `CLAUDE.md` (rules) -> `AI_CONTEXT.md` (compact spec) -> `SKILL.md` (skill
routing). Read `docs/AI_CONTEXT_FULL.md` only when the compact spec lacks a detail. Do not
read every document for every task.

Current state: documentation only. Do not assume code, folders, or configs exist.

## Hard architecture rules

- Microservices only. One database per service. No shared tables or databases.
- Business flows use RabbitMQ events. No synchronous HTTP between services for core logic.
- YARP API Gateway on port 5000 is the only client entry point.
- SignalR from the Notification Service is the only real-time channel.
- Services: Gateway 5000, Identity 5003, Catalog 5001, Order 5002, Payment 5004,
  Notification 5005.
- Do not add a service, broker, database, or dependency without approval.

Planned layout: `services/<Service>/`, `apps/frontend/`, `infra/`.

## Domain invariants

- Orders: `PENDING -> COOKING -> READY -> COMPLETED`, or `CANCELLED`. Server-validated, no jumps.
- Payments: `PENDING -> PENDING_VERIFICATION -> COMPLETED`, plus `FAILED` / `REFUNDED`.
  QR slips are verified by a cashier or manager before completion.
- Staff roles: `CASHIER`, `KITCHEN`, `MANAGER`, `ADMIN`. Customers are unauthenticated guests
  and are not rows in `users`.
- Tables: `AVAILABLE`, `OCCUPIED`, `RESERVED`, `CLEANING`. A table can hold multiple guests;
  carts use a randomized session id, not the table number.
- `OrderCreated` deducts stock in Catalog. Sold-out items disappear from the public menu.
- Money is `decimal` / `DECIMAL(10,2)`. Server recalculates cart totals; client totals are
  display-only.
- Status transitions, prices, and stock are never trusted from the client.

## Security

- Validate JWT and role at the gateway and the service. Never trust client claims.
- Access token in memory (15 min), refresh token in HttpOnly Secure cookie (7 days).
- Parameterized queries only. No raw SQL without review.
- No secrets in source. Use environment variables and gitignored `.env` files.
- Never log passwords, tokens, payment slips, or personal data.
- Rate-limit public order and slip-upload endpoints. Validate uploaded file type, size, path.
- Enforce HTTPS, CSP, and strict CORS in production.

## Frontend and UX

- Mobile-first; assume a mid-range Android phone on restaurant Wi-Fi.
- Touch targets: >= 44x44 px customer, >= 48x48 px staff.
- Colors: success `#22c55e`, warning `#f59e0b`, danger `#ef4444`, dashboard `#f8fafc`,
  customer menu white.
- Every action needs loading, empty, success, and error states. No dead ends.
- Skeleton loaders for the menu. Accessibility is mandatory: labels, focus, keyboard, contrast.
- Stack: Next.js App Router, TypeScript strict, Tailwind, shadcn/ui, Zustand, React Query,
  SignalR client. No `any`.

## Backend

- .NET 10, C# 12+, Minimal APIs, EF Core, FluentValidation, MassTransit, async everywhere,
  constructor injection, records for DTOs, global exception middleware.
- Paginate large lists, index frequent queries, cache hot reads with Redis when justified.

## Quality gates

- 70% unit, 20% integration, 10% E2E. xUnit/Moq/FluentAssertions/Testcontainers; Vitest or Jest
  plus Playwright. Coverage gate 80%.
- Test cart math, stock deduction, order transitions, RBAC, and payment verification.
- Write the failing test first for bugs and business logic.
- A change is done only when lint, typecheck, and relevant tests pass.

## Git

- Conventional Commits: `feat(order): ...`, `fix(catalog): ...`, `docs(api): ...`.
- One logical change per commit. Never commit build output, secrets, or `.env`.
- Never commit unless explicitly asked. Update docs when contracts change.

## Skill routing

Skills are in `.agents/skills/`. Load the matching one before the work. Full matrix in
`SKILL.md`.

| Task | Skill |
| --- | --- |
| New page or visual direction | `design-taste-frontend`, `impeccable`, `ui-ux-pro-max` |
| Tokens and shadcn implementation | `design-system`, `ui-styling` |
| Reference real app patterns | `mobbin-best-practices`, `search`, `redesign-screen` |
| Pick a library | `pick-ui-library` |
| Motion | `transitions-dev`, `transitions-polish` |
| UI audit and accessibility | `web-design-guidelines`, `impeccable` |
| Production UI build | `frontend-ui-engineering` |
| API and contracts | `api-and-interface-design` |
| Spec and planning | `spec-driven-development`, `planning-and-task-breakdown` |
| Bug or failing test | `debugging-and-error-recovery`, `test-driven-development` |
| Security | `security-and-hardening` |
| Performance | `performance-optimization` |
| Review and git | `code-review-and-quality`, `git-workflow-and-versioning` |
| Browser verification | `webapp-testing` |

## Definition of done

1. Behavior matches `AI_CONTEXT.md` and the loaded skill guidance.
2. Types compile, lint passes, tests pass.
3. Loading, empty, error, and success states handled.
4. Authorization and validation enforced server-side.
5. Checked at 360 px, 390 px, and 768 px.
6. Docs and contracts updated.

## Never

- Never invent endpoints, tables, ports, or environment variables.
- Never add a shared database or synchronous cross-service dependency.
- Never disable auth, validation, or tests to make something pass.
- Never use `any`, `SELECT *`, or client-side price authority.
- Never copy a competitor's exact interface or assets.
