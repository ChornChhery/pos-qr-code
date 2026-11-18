# AGENTS.md

Entry point for agents that read `AGENTS.md` (including opencode) instead of `CLAUDE.md`.

Read in this order:

1. `CLAUDE.md` - hard rules for this repository.
2. `AI_CONTEXT.md` - compact specification (business rules, architecture, API, schemas).
3. `SKILL.md` - installed skills and the task -> skill routing matrix.
4. `README.md` - public overview for humans. Skip it for normal engineering work.

Do not read every document or every skill. Open `docs/AI_CONTEXT_FULL.md` only when the
compact spec lacks a detail, and load only the skills the current task needs.

Current state: Phase 1 (foundation) is built: `services/ApiGateway`, `services/IdentityService`,
`tests/IdentityService.Tests`, `infra/` (Compose, Terraform), CI, and `scripts/`. Phases 2-6 are not
built. There is no Catalog, Order, Payment, or Notification service and no `apps/frontend` yet.
See `docs/PHASE_1_NOTES.md` for decisions and commands that exist.

Hard constraints, repeated because they are easy to break:

- Microservices only; one database per service; no shared tables.
- Business flows use RabbitMQ events, not synchronous HTTP between services.
- The YARP API Gateway on port 5000 is the only client entry point.
- Real-time goes through SignalR from the Notification Service.
- Order states: `PENDING -> COOKING -> READY -> COMPLETED`, or `CANCELLED`.
- Payments: `PENDING -> PENDING_VERIFICATION -> COMPLETED`, plus `FAILED` / `REFUNDED`.
- Money is `decimal` / `DECIMAL(10,2)`.
- No `any`, no `SELECT *`, no secrets in source, no client-side price authority.
- Never commit unless explicitly asked.

Skills are in `.agents/skills/`. Load the matching skill before doing the work. See
`SKILL.md` for the sequence to follow for screens, endpoints, bugs, payments, and reviews.

If a request conflicts with `CLAUDE.md` or `AI_CONTEXT.md`, stop and ask before changing
architecture or contracts.
