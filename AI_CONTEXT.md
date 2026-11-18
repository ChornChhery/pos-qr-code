# AI_CONTEXT.md

Compact specification for the Restaurant POS & QR Ordering System.
Full original specification: `docs/AI_CONTEXT_FULL.md` (read only when this file lacks detail).

Current state: Phase 1 (foundation) is built: `services/ApiGateway`, `services/IdentityService`,
`tests/IdentityService.Tests`, `infra/` (Compose, Terraform), CI, and `scripts/`. Phases 2-6 are not
built. There is no Catalog, Order, Payment, or Notification service and no `apps/frontend` yet.
See `docs/PHASE_1_NOTES.md` for decisions and commands that exist.

---

## 1. Purpose

A single-URL restaurant POS for small and medium restaurants. Customers scan a table QR and
order without an account. Staff log in to one dashboard for orders, kitchen display, cashier,
and admin.

Success targets: order to kitchen display < 1 s, mobile menu < 2 s on 3G, 99.9% uptime,
zero order data loss.

## 2. Users and flows

- Customer (public, no login): scan `/?table=12` -> menu -> category -> product modal ->
  modifiers -> cart -> checkout -> "kitchen is preparing" screen.
- Owner/Kitchen (one login): live order queue (KDS) with `Pending -> Cooking -> Ready`, large
  targets, elapsed time turns red after 15 minutes, sold-out toggles, table status, reports.
- Cashier: pending payments, cash with change, QR slip verification, receipt printing.
- Admin: staff and roles, tables and QR generation, menu CRUD, orders, reports, settings,
  audit logs.

## 3. Architecture

Microservices, one database per service, asynchronous communication through RabbitMQ, one
client entry point through YARP.

| Service | Port | Database | Responsibility |
| --- | --- | --- | --- |
| API Gateway (YARP) | 5000 | none | routing, rate limiting, CORS, JWT pre-validation, logging |
| Identity | 5003 | `identity_db` | login, JWT, roles, audit logs |
| Catalog | 5001 | `catalog_db` | categories, products, modifiers, stock, sold-out |
| Order | 5002 | `order_db` | tables, carts, orders, status transitions |
| Payment | 5004 | `payment_db` | cash, QR, slip upload, verification, refunds |
| Notification | 5005 | none | SignalR hubs, live updates |

Gateway routes: `/api/auth/*` Identity, `/api/catalog/*` Catalog, `/api/orders/*` Order,
`/api/payments/*` Payment, `/hubs/*` Notification.

Frontend: Next.js 14+ App Router on port 3000. SSR/ISR for the public menu, CSR for staff.

### Events

- `OrderCreated` -> Catalog deducts stock, Notification pushes to kitchen/cashier.
- `OrderStatusChanged` -> customer tracking updates.
- `LowStockAlert` -> manager notification.
- `ProductCreated` / `ProductUpdated` -> catalog sync.
- `PaymentCompleted` -> order completion.

No synchronous HTTP between services for business logic.

### SignalR

- `/hubs/kitchen` and `/hubs/cashier`.
- Client events: `OrderCreated`, `OrderStatusChanged`, `LowStockAlert`.
- Automatic reconnect required.

## 4. Data model

All IDs are UUID. All money is `DECIMAL(10,2)`. All timestamps are `TIMESTAMPTZ`.

Identity: `users(id, username unique, password_hash, role, is_active, created_at, updated_at)`,
`audit_logs(id, user_id, action, details jsonb, ip_address, user_agent, created_at)`.

Catalog: `categories(id, name, description, display_order, is_active)`,
`products(id, category_id, name, description, price, image_url, is_available, stock_count,
low_stock_threshold)`, `modifiers(id, product_id, group_name, is_required, min_selections,
max_selections, display_order)`, `modifier_options(id, modifier_id, name, price_add,
display_order, is_available)`.

Order: `tables(id, table_number unique, capacity, qr_code_url, status)`,
`orders(id, table_id, status, notes, subtotal, tax, discount, total, created_at, updated_at,
completed_at)`, `order_items(id, order_id, product_id, product_name, quantity, unit_price,
selected_modifiers jsonb, notes, total_price)`.

Payment: `payments(id, order_id, method, amount, received_amount, change_amount,
slip_image_url, status, verified_by, verified_at, created_at, updated_at)`.

Enums:
- `users.role`: `CASHIER`, `KITCHEN`, `MANAGER`, `ADMIN`.
- `tables.status`: `AVAILABLE`, `OCCUPIED`, `RESERVED`, `CLEANING`.
- `orders.status`: `PENDING`, `COOKING`, `READY`, `COMPLETED`, `CANCELLED`.
- `payments.method`: `CASH`, `QR`, `CARD`.
- `payments.status`: `PENDING`, `PENDING_VERIFICATION`, `COMPLETED`, `FAILED`, `REFUNDED`.

Note: the full spec sometimes lists `GUEST` as a role. Customers are unauthenticated and are
not rows in `users`. Treat `GUEST` as "unauthenticated customer", never a database role.

## 5. API

Base URL local `http://localhost:5000`. Protected calls use `Authorization: Bearer <token>`.
Envelope: `{ "success": bool, "data": ..., "message": string, "errors": [] }`.

- Auth: `POST /api/auth/login`, `POST /api/auth/register` (ADMIN), `POST /api/auth/refresh`,
  `GET /api/auth/users` (ADMIN), `PUT /api/auth/users/{id}/role` (ADMIN).
- Catalog: `GET /api/catalog/categories` (public), `GET /api/catalog/products` (public),
  `GET /api/catalog/products/{id}` (public), `POST /api/catalog/products` (ADMIN/MANAGER),
  `PUT /api/catalog/products/{id}` (ADMIN/MANAGER),
  `PATCH /api/catalog/products/{id}/sold-out` (ADMIN/MANAGER).
- Orders: `GET /api/orders/tables` (public), `POST /api/orders/cart` (public),
  `POST /api/orders/checkout` (public), `GET /api/orders` (staff),
  `GET /api/orders/{id}`, `PATCH /api/orders/{id}/status` (KITCHEN/MANAGER/ADMIN).
- Payments: `POST /api/payments/cash` (CASHIER+), `POST /api/payments/qr`,
  `POST /api/payments/upload-slip` (public), `POST /api/payments/{id}/verify` (CASHIER+).

## 6. Frontend rules

Stack: Next.js App Router, TypeScript strict, React 18+, Tailwind, shadcn/ui, Zustand,
React Query, SignalR client. No `any`.

Design:
- Primary brand color warm (deep orange/red). Success `#22c55e`, warning `#f59e0b`,
  danger `#ef4444`, dashboard background `#f8fafc`, customer menu white.
- Customer touch targets >= 44x44 px, staff >= 48x48 px.
- Skeleton loaders for the menu, never a blocking spinner.
- Every action has loading, empty, success, and error states.
- Mobile-first; assume a mid-range Android phone on restaurant Wi-Fi.
- Cart total is server-authoritative; the client total is display-only.
- Accessibility required: labels, focus rings, keyboard paths, contrast.

## 7. Backend rules

.NET 10, C# 12+, Minimal APIs, EF Core, FluentValidation, MassTransit, async everywhere,
constructor injection, records for DTOs, global exception middleware, XML docs on public APIs.

Never: monolithic patterns, shared databases, synchronous cross-service business calls,
raw SQL, `SELECT *`, secrets in source, logging tokens or payment data.

## 8. Security

- Access token 15 min in memory; refresh token 7 days in HttpOnly Secure cookie.
- JWT validated at the gateway and at sensitive services. RBAC on every staff endpoint.
- Parameterized queries only. EF Core by default; raw SQL forbidden without review.
- Rate limit the gateway, especially public order and slip-upload endpoints.
- Payment slips are untrusted files: validate MIME, size, and storage path.
- HTTPS, CSP, strict CORS in production. Audit log all sensitive actions.
- OWASP Top Ten mitigations: broken auth, IDOR, SQL injection, XSS, DoS.

## 9. Testing

70% unit, 20% integration, 10% E2E. xUnit, Moq, FluentAssertions, Testcontainers; Vitest or
Jest plus Playwright. Critical paths: cart math, stock deduction, order transitions, RBAC,
payment verification. Coverage gate 80%. Merge blocked by failing E2E or lint errors.

## 10. Infrastructure

Docker Compose locally (PostgreSQL, RabbitMQ, services, frontend). Terraform for cloud.
GitHub Actions: lint -> test -> build images -> registry; tag -> staging -> E2E -> production.
Target AWS: ECS Fargate or EKS, RDS PostgreSQL Multi-AZ, Amazon MQ, S3, CloudFront.
Observability: OpenTelemetry traces, Prometheus metrics, Grafana dashboards, structured logs.

## 11. Phases

1. Foundation: repo, Compose, gateway, Identity, CI/CD, Terraform.
2. Core services: Catalog, Order, RabbitMQ events, SignalR, migrations.
3. Public menu: Next.js, SSR, cart, modifiers, mobile optimization.
4. Staff dashboard: login, KDS, menu/table management, receipt printing.
5. Payments and admin: Payment Service, verification, audit, reports, Z-report, QR generation.
6. Hardening: tests, load testing, monitoring, security audit, production deploy.

Timeline: 8-9 weeks, 10-11 with buffer.

## 12. Known gaps to confirm before coding

- Currency and locale are not specified.
- Tax model (inclusive vs exclusive, per-item vs order) is not specified.
- QR payments: gateway integration is undecided; current design is slip upload + manual verify.
- Offline behavior is undecided.
- The docs disagree on whether `GUEST` is a role; this file treats it as unauthenticated.
- README links to `PROJECT.md`, `ARCHITECTURE.md`, `API.md`, `DATABASE.md`, `UX_UI.md`,
  `SECURITY.md`, `TESTING_STRATEGY.md`, `INFRASTRUCTURE.md`, and `ONBOARDING.md`. These files
  do not exist yet.
