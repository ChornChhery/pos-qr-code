# Phase prompts

Paste one prompt into your AI tool (Claude Code, Codex, opencode) opened in `D:\pos-qr-code`.
Do the phases in order. After each phase the AI must hand you a zip, a PowerShell extract script, and run steps.

Phase status: **1, 2, and 3 built** (this repo). 4 to 6 not started.

---

## Phase 1: Foundation and infrastructure

> DONE. Use only to redo or extend Phase 1.

````text
Start Phase 1 (Foundation and infrastructure) of the Restaurant POS project in this repository.

Read, in this order and nothing else unless needed: `CLAUDE.md`, `AI_CONTEXT.md`, `SKILL.md`,
`docs/PHASE_1_NOTES.md`. Load only the skills that `SKILL.md` routes for the work below.
Follow every hard rule in `CLAUDE.md`. If something conflicts with `CLAUDE.md` or `AI_CONTEXT.md`, stop and ask.
Never commit unless I say so. Never invent endpoints, tables, ports, or environment variables: propose them and wait for approval.

## Scope
Repository layout, Docker Compose (PostgreSQL, RabbitMQ), YARP API Gateway with routing, Identity Service
(JWT, users, roles, RBAC, audit logs), GitHub Actions CI, Terraform foundation.

## Before you write code
Review `docs/PHASE_1_NOTES.md` section "Decisions beyond the spec" and tell me which ones you want changed.

## Done when
Gateway routes to Identity; login, refresh, register, users, role change work with RBAC; Compose runs PostgreSQL and RabbitMQ.

## Hand-off (required when the phase is done)
1. Run build, lint, and the relevant tests. Report what passed and what you could not run. Do not claim a check you did not run.
2. Write `docs/PHASE_1_NOTES.md` (what was built, decisions beyond the spec, open items) and `docs/PHASE_1_RUNBOOK.md` (Windows PowerShell steps to run and verify).
3. Update the "Current state" lines in `CLAUDE.md`, `AGENTS.md`, `AI_CONTEXT.md`, and the README status table.
4. Extend `scripts/smoke-test.ps1` (or add a phase script) so I can verify the phase with one command.
5. Create the zip with `scripts/make-phase-zip.ps1 -Phase 1` (or equivalent) and give me exactly this, with the real zip name:
```powershell
cd D:\pos-qr-code
Expand-Archive -Force C:\Users\Dell\Downloads\pos-phase1.zip D:\pos-qr-code
```
6. End with: the commands to run it on my PC, what I should see, and the first error to send you if it fails.
````

---

## Phase 2: Core business services

> DONE. Use only to redo or extend Phase 2.

````text
Start Phase 2 (Core business services) of the Restaurant POS project in this repository.

Read, in this order and nothing else unless needed: `CLAUDE.md`, `AI_CONTEXT.md`, `SKILL.md`,
`docs/PHASE_1_NOTES.md`, and every earlier `docs/PHASE_<n>_NOTES.md`. Load only the skills that `SKILL.md` routes for the work below.
Follow every hard rule in `CLAUDE.md`. If something conflicts with `CLAUDE.md` or `AI_CONTEXT.md`, stop and ask.
Never commit unless I say so. Never invent endpoints, tables, ports, or environment variables: propose them and wait for approval.

## Scope
Catalog Service (port 5001, `catalog_db`): categories, products, modifiers, stock, sold-out toggle.
Order Service (port 5002, `order_db`): tables, carts with a randomized session id, checkout, order status transitions (server-validated).
Notification Service (port 5005): SignalR hubs `/hubs/kitchen` and `/hubs/cashier`.
RabbitMQ with MassTransit: `OrderCreated`, `OrderStatusChanged`, `LowStockAlert`, `ProductCreated`, `ProductUpdated`.
EF Core migrations for both databases. Tighten gateway routes so staff-only endpoints require auth and public ones stay public.
Add unit tests for cart math, stock deduction, and order transitions before the code (failing test first).

## Before you write code
Ask me before coding and wait for answers:
1. Currency and locale (decimal places, display format).
2. Tax model: inclusive or exclusive, per item or per order, rate source.
3. Where carts are stored (Order database is the default; do not add Redis without approval).
4. Whether to use the MassTransit EF outbox so no order event is lost (recommended for the zero order data loss target).

## Done when
Place an order through the gateway and see stock drop and a SignalR message arrive; sold-out items vanish from the public menu; invalid status jumps are rejected.

## Hand-off (required when the phase is done)
1. Run build, lint, and the relevant tests. Report what passed and what you could not run. Do not claim a check you did not run.
2. Write `docs/PHASE_2_NOTES.md` (what was built, decisions beyond the spec, open items) and `docs/PHASE_2_RUNBOOK.md` (Windows PowerShell steps to run and verify).
3. Update the "Current state" lines in `CLAUDE.md`, `AGENTS.md`, `AI_CONTEXT.md`, and the README status table.
4. Extend `scripts/smoke-test.ps1` (or add a phase script) so I can verify the phase with one command.
5. Create the zip with `scripts/make-phase-zip.ps1 -Phase 2` (or equivalent) and give me exactly this, with the real zip name:
```powershell
cd D:\pos-qr-code
Expand-Archive -Force C:\Users\Dell\Downloads\pos-phase2.zip D:\pos-qr-code
```
6. End with: the commands to run it on my PC, what I should see, and the first error to send you if it fails.
````

---

## Phase 3: Public customer menu (Next.js)

> DONE. Use only to redo or extend Phase 3.

````text
Start Phase 3 (Public customer menu (Next.js)) of the Restaurant POS project in this repository.

Read, in this order and nothing else unless needed: `CLAUDE.md`, `AI_CONTEXT.md`, `SKILL.md`,
`docs/PHASE_1_NOTES.md`, and every earlier `docs/PHASE_<n>_NOTES.md`. Load only the skills that `SKILL.md` routes for the work below.
Follow every hard rule in `CLAUDE.md`. If something conflicts with `CLAUDE.md` or `AI_CONTEXT.md`, stop and ask.
Never commit unless I say so. Never invent endpoints, tables, ports, or environment variables: propose them and wait for approval.

## Scope
`apps/frontend` with Next.js App Router, TypeScript strict, Tailwind, shadcn/ui, Zustand, React Query.
Show every price in both USD and riel using the `price` and `priceKhr` fields from the API (never convert in the browser).
Customer flow: scan `/?table=12`, menu with skeleton loaders (SSR/ISR), category, product modal, modifiers, cart, checkout, order tracking.
Server totals are authoritative; the client total is display only. Mobile-first, touch targets at least 44x44 px, checked at 360, 390, 768 px.
Every action has loading, empty, success, and error states. Add Vitest unit tests and the first Playwright journey.

## Before you write code
Ask me before coding: restaurant name and logo, brand colors (default is warm deep orange or red), languages to support (for example Khmer and English),
and any reference apps I like. Use `spec-driven-development`, then the new-page sequence in `SKILL.md`.

## Done when
A phone can scan, browse, customize, and order; the order reaches the Order service; Playwright covers the ordering journey.

## Hand-off (required when the phase is done)
1. Run build, lint, and the relevant tests. Report what passed and what you could not run. Do not claim a check you did not run.
2. Write `docs/PHASE_3_NOTES.md` (what was built, decisions beyond the spec, open items) and `docs/PHASE_3_RUNBOOK.md` (Windows PowerShell steps to run and verify).
3. Update the "Current state" lines in `CLAUDE.md`, `AGENTS.md`, `AI_CONTEXT.md`, and the README status table.
4. Extend `scripts/smoke-test.ps1` (or add a phase script) so I can verify the phase with one command.
5. Create the zip with `scripts/make-phase-zip.ps1 -Phase 3` (or equivalent) and give me exactly this, with the real zip name:
```powershell
cd D:\pos-qr-code
Expand-Archive -Force C:\Users\Dell\Downloads\pos-phase3.zip D:\pos-qr-code
```
6. End with: the commands to run it on my PC, what I should see, and the first error to send you if it fails.
````

---

## Phase 4: Staff dashboard

````text
Start Phase 4 (Staff dashboard) of the Restaurant POS project in this repository.

Read, in this order and nothing else unless needed: `CLAUDE.md`, `AI_CONTEXT.md`, `SKILL.md`,
`docs/PHASE_1_NOTES.md`, and every earlier `docs/PHASE_<n>_NOTES.md`. Load only the skills that `SKILL.md` routes for the work below.
Follow every hard rule in `CLAUDE.md`. If something conflicts with `CLAUDE.md` or `AI_CONTEXT.md`, stop and ask.
Never commit unless I say so. Never invent endpoints, tables, ports, or environment variables: propose them and wait for approval.

## Scope
Staff login page (access token in memory, refresh cookie), unified Owner/Kitchen dashboard, live order queue through the SignalR client with
automatic reconnect, `PENDING -> COOKING -> READY` buttons (targets at least 48x48 px, elapsed time turns red after 15 minutes),
sold-out toggles, table status, menu CRUD and table management for ADMIN/MANAGER, receipt printing.

## Before you write code
Ask me before coding: approve adding `POST /api/auth/logout` (clears the refresh cookie; see `docs/PHASE_1_NOTES.md` item 2),
and the receipt paper width and printer type.

## Done when
A kitchen user sees new orders in under a second and can advance them; cashier and admin screens respect roles.

## Hand-off (required when the phase is done)
1. Run build, lint, and the relevant tests. Report what passed and what you could not run. Do not claim a check you did not run.
2. Write `docs/PHASE_4_NOTES.md` (what was built, decisions beyond the spec, open items) and `docs/PHASE_4_RUNBOOK.md` (Windows PowerShell steps to run and verify).
3. Update the "Current state" lines in `CLAUDE.md`, `AGENTS.md`, `AI_CONTEXT.md`, and the README status table.
4. Extend `scripts/smoke-test.ps1` (or add a phase script) so I can verify the phase with one command.
5. Create the zip with `scripts/make-phase-zip.ps1 -Phase 4` (or equivalent) and give me exactly this, with the real zip name:
```powershell
cd D:\pos-qr-code
Expand-Archive -Force C:\Users\Dell\Downloads\pos-phase4.zip D:\pos-qr-code
```
6. End with: the commands to run it on my PC, what I should see, and the first error to send you if it fails.
````

---

## Phase 5: Payments and admin

````text
Start Phase 5 (Payments and admin) of the Restaurant POS project in this repository.

Read, in this order and nothing else unless needed: `CLAUDE.md`, `AI_CONTEXT.md`, `SKILL.md`,
`docs/PHASE_1_NOTES.md`, and every earlier `docs/PHASE_<n>_NOTES.md`. Load only the skills that `SKILL.md` routes for the work below.
Follow every hard rule in `CLAUDE.md`. If something conflicts with `CLAUDE.md` or `AI_CONTEXT.md`, stop and ask.
Never commit unless I say so. Never invent endpoints, tables, ports, or environment variables: propose them and wait for approval.

## Scope
Payment Service (port 5004, `payment_db`): cash with change, QR payment via slip upload, cashier/manager verification
(`PENDING -> PENDING_VERIFICATION -> COMPLETED`, `FAILED`, `REFUNDED`), `PaymentCompleted` event completes the order.
Slip upload is an untrusted file: validate type, size, and storage path; rate limited. Admin: audit log viewer, sales reports, Z-report, table QR generation.

## Before you write code
Ask me before coding: slip storage (local disk for development, S3 for cloud), whether any QR payment gateway should be integrated
or slip upload plus manual verification is final, and the report definitions I need.

## Done when
A guest uploads a slip, a cashier verifies it, and the order completes; reports and Z-report match the orders in the database.

## Hand-off (required when the phase is done)
1. Run build, lint, and the relevant tests. Report what passed and what you could not run. Do not claim a check you did not run.
2. Write `docs/PHASE_5_NOTES.md` (what was built, decisions beyond the spec, open items) and `docs/PHASE_5_RUNBOOK.md` (Windows PowerShell steps to run and verify).
3. Update the "Current state" lines in `CLAUDE.md`, `AGENTS.md`, `AI_CONTEXT.md`, and the README status table.
4. Extend `scripts/smoke-test.ps1` (or add a phase script) so I can verify the phase with one command.
5. Create the zip with `scripts/make-phase-zip.ps1 -Phase 5` (or equivalent) and give me exactly this, with the real zip name:
```powershell
cd D:\pos-qr-code
Expand-Archive -Force C:\Users\Dell\Downloads\pos-phase5.zip D:\pos-qr-code
```
6. End with: the commands to run it on my PC, what I should see, and the first error to send you if it fails.
````

---

## Phase 6: Hardening and production

````text
Start Phase 6 (Hardening and production) of the Restaurant POS project in this repository.

Read, in this order and nothing else unless needed: `CLAUDE.md`, `AI_CONTEXT.md`, `SKILL.md`,
`docs/PHASE_1_NOTES.md`, and every earlier `docs/PHASE_<n>_NOTES.md`. Load only the skills that `SKILL.md` routes for the work below.
Follow every hard rule in `CLAUDE.md`. If something conflicts with `CLAUDE.md` or `AI_CONTEXT.md`, stop and ask.
Never commit unless I say so. Never invent endpoints, tables, ports, or environment variables: propose them and wait for approval.

## Scope
Reach the 80 percent coverage gate and enforce it in CI, add the Playwright suite to CI, load testing against the targets in `AI_CONTEXT.md` section 1,
OpenTelemetry, Prometheus, Grafana, alerts, a security review against OWASP Top Ten, and the production deploy (ECS Fargate, Amazon MQ, CloudFront, NAT or VPC endpoints).
Carry-over items from `docs/PHASE_1_NOTES.md`: refresh-token table with rotation and reuse detection, RS256 signing, trusted proxies,
a separate migration job, tuned rate limits for restaurant Wi-Fi, and the tag-based release workflow.

## Before you write code
Ask me which cloud account and region to target and who receives alerts.

## Done when
Release pipeline passes on a tag, staging E2E passes, load test meets the targets, and the security review has no open high findings.

## Hand-off (required when the phase is done)
1. Run build, lint, and the relevant tests. Report what passed and what you could not run. Do not claim a check you did not run.
2. Write `docs/PHASE_6_NOTES.md` (what was built, decisions beyond the spec, open items) and `docs/PHASE_6_RUNBOOK.md` (Windows PowerShell steps to run and verify).
3. Update the "Current state" lines in `CLAUDE.md`, `AGENTS.md`, `AI_CONTEXT.md`, and the README status table.
4. Extend `scripts/smoke-test.ps1` (or add a phase script) so I can verify the phase with one command.
5. Create the zip with `scripts/make-phase-zip.ps1 -Phase 6` (or equivalent) and give me exactly this, with the real zip name:
```powershell
cd D:\pos-qr-code
Expand-Archive -Force C:\Users\Dell\Downloads\pos-phase6.zip D:\pos-qr-code
```
6. End with: the commands to run it on my PC, what I should see, and the first error to send you if it fails.
````

