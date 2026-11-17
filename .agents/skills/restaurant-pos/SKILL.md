---
name: restaurant-pos
description: "Restaurant POS project context. Use for any work here: QR menu, KDS, cashier payments, microservices, schemas, RabbitMQ events, SignalR."
---

# Restaurant POS project context

A single-URL restaurant POS and QR ordering system for small and medium restaurants. Customers
scan a table QR and order without an account. Staff log in to a unified Owner/Kitchen dashboard,
cashier, and admin.

The repository is documentation-only today. Do not assume code, folders, or configs exist.

## Read the authoritative files

1. `CLAUDE.md` - hard rules for all work in this repo.
2. `AI_CONTEXT.md` - compact specification: flows, services, schemas, API, phases.
3. `SKILL.md` - installed skills and the task -> skill routing matrix.
4. `README.md` - public overview for humans. Skip it for normal engineering work.

Open `docs/AI_CONTEXT_FULL.md` only for details missing from the compact spec.

## Hard rules

- Microservices only. One database per service. No shared tables.
- Services communicate through RabbitMQ events. No synchronous HTTP for business logic.
- YARP API Gateway on port 5000 is the only client entry point.
- SignalR from the Notification Service is the only real-time channel.
- Order states: `PENDING -> COOKING -> READY -> COMPLETED`, or `CANCELLED`. Server-validated.
- Payments: `PENDING -> PENDING_VERIFICATION -> COMPLETED`, plus `FAILED` / `REFUNDED`.
- Customers are unauthenticated guests. Staff roles: `CASHIER`, `KITCHEN`, `MANAGER`, `ADMIN`.
- Money is `decimal` / `DECIMAL(10,2)`. Never float.
- Server recalculates cart totals. Client price is display-only.
- No `any`, no `SELECT *`, no secrets in source, no disabled auth or tests.
- Never commit unless explicitly asked.

## Before changing anything

- Check whether the change alters an architecture or API contract. If it does, ask first.
- Load the task-specific skill listed in `SKILL.md` before writing code.
- Verify with the commands in `SKILL.md`; do not invent scripts that do not exist.
