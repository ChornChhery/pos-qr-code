# Phase 2 notes: decisions, file map, open items

Read this before building on Phase 2. The contract lives in `AI_CONTEXT.md`; this file records what Phase 2 added or
decided beyond it. Phase 1 notes: `docs/PHASE_1_NOTES.md`.

## Built

| Area | Where |
| --- | --- |
| Shared code (events, auth setup, envelope, money) | `shared/Pos.Shared` |
| Catalog Service (port 5001, `catalog_db`) | `services/CatalogService` |
| Order Service (port 5002, `order_db`) | `services/OrderService` |
| Notification Service (port 5005, SignalR, no database) | `services/NotificationService` |
| Gateway route tightening | `services/ApiGateway/appsettings.json` |
| Unit tests | `tests/CatalogService.Tests`, `tests/OrderService.Tests` |
| Smoke test | `scripts/smoke-test-phase2.ps1` |

## Decisions you gave me

- **Currency:** prices are shown in USD and Khmer riel. **No tax** (`tax` and `discount` are always 0).
- **Outbox:** the MassTransit EF Core transactional outbox is on for Catalog and Order. Consumers use the inbox, so a redelivered message is ignored.

## How money works

- Everything is **stored in USD**, `DECIMAL(10,2)`, as the data model says.
- Riel is **computed for display**: `usd x rate`, rounded to the nearest 100 riel. API fields: `price` + `priceKhr`, `priceAdd` + `priceAddKhr`,
  `unitPrice` + `unitPriceKhr`, `total` + `totalKhr`. The rate is `Currency:UsdToKhrRate` (default 4000, `USD_TO_KHR_RATE` in `.env`).
  Catalog and Order must use the same value.
- Cart math is on the server only: `unit price = product price + chosen options`, `line total = unit price x quantity`, `total = sum of lines`.

## Behavior

- **Catalog endpoints** as in `AI_CONTEXT.md` section 5. `GET /api/catalog/products` returns orderable products; add `?includeSoldOut=true`
  to see sold-out ones too. Stock counts are visible to ADMIN and MANAGER only.
- **Order endpoints** as in section 5. `POST /api/orders/cart` prices a cart the client holds and returns server totals (see decision 3).
  `POST /api/orders/checkout` creates the order, marks the table OCCUPIED, and publishes `OrderCreated`.
  `GET /api/orders/{id}` is public: the id is a random UUID v4, so guests can track their order but cannot guess another one.
- **Order status rules** (server-enforced): PENDING to COOKING or CANCELLED, COOKING to READY or CANCELLED, READY to COMPLETED.
  Anything else returns 409. When an order ends (COMPLETED or CANCELLED) and its table has no other active order, the table becomes AVAILABLE.
- **Events:** Catalog publishes `ProductCreated`, `ProductUpdated`, `StockChanged`, `LowStockAlert`. Order publishes `OrderCreated`,
  `OrderStatusChanged`. Order consumes the product and stock events. Catalog consumes `OrderCreated` and deducts stock.
  Notification consumes `OrderCreated`, `OrderStatusChanged`, `LowStockAlert`.
- **SignalR:** `/hubs/kitchen` (KITCHEN, MANAGER, ADMIN) and `/hubs/cashier` (CASHIER, MANAGER, ADMIN). Server-to-client events:
  `OrderCreated`, `OrderStatusChanged`, `LowStockAlert`. Browsers pass the token as `?access_token=`.
- **Gateway:** staff routes now require a valid JWT at the gateway: `POST/PUT/PATCH /api/catalog/products/*`, `GET /api/orders`,
  `PATCH /api/orders/{id}/status`. Services still check roles themselves.

## Decisions beyond the spec (need your approval or review)

1. **New table `catalog_products` in `order_db`.** The architecture forbids synchronous calls between services, so Order keeps its own copy
   of each product (name, price, availability, stock, modifiers as JSON), fed by Catalog events. This is the "catalog sync" in `AI_CONTEXT.md`.
   Without it Order cannot price or check an order.
2. **`products.stock_count` can be NULL**, meaning "stock is not tracked". The data model has `INT DEFAULT 0`, which would make every
   untracked dish look sold out. A product is orderable when switched on and (stock is NULL or above 0).
3. **The cart lives in the browser.** `POST /api/orders/cart` prices it and checkout prices it again. There is no cart table and no session id,
   so there is nothing server-side to guess or steal. Server-side carts (the "temporary storage" line in the full spec) need a table or Redis: not added.
4. **Seed data in Development only:** a sample menu (`Seed:SampleMenu`) and 10 tables (`Seed:TableCount`). The spec has no endpoint to create a
   category or a table, so there would otherwise be nothing to order. Phase 4 adds table and menu management screens.
5. **Catalog re-publishes every product 10 seconds after start and then every 10 minutes** (`CatalogResyncService`). RabbitMQ drops events that
   are published before a consumer's queue exists, so without this Order could miss products depending on start-up order.
6. **Oversell window.** Order lowers its own stock copy at checkout and Catalog sets the real number a moment later. Two checkouts that race for the
   last item are stopped by an optimistic-concurrency check (PostgreSQL `xmin`) and the loser gets a 409 "stock just changed". Catalog never goes below zero.
7. **MassTransit 8.5.0 and EF Core 9 in Catalog and Order.** MassTransit v9 is a commercial product, so I pinned the last free line (v8, Apache-2.0).
   Its maintainers say v8 gets security patches only until the end of 2026, and 8.5 is built against EF Core 9, so these two services use EF Core 9
   (Identity stays on EF Core 10). **Decide before Phase 6:** buy a v9 license, or replace MassTransit with another bus library or a small RabbitMQ client.
   The messaging code is confined to `Program.cs`, the `Messaging` folders, and `Pos.Shared/Events`.
8. **Outbox tables.** MassTransit adds `inbox_state`, `outbox_message`, `outbox_state` to `catalog_db` and `order_db` through the migrations.
9. **Config keys and variables added:** `USD_TO_KHR_RATE` (`.env`), `Currency:UsdToKhrRate`, `RabbitMq:*` (`RabbitMq__Password` from `.env`),
   `ConnectionStrings:CatalogDb`, `ConnectionStrings:OrderDb`, `Seed:SampleMenu`, `Seed:TableCount`.
10. **`Pos.Shared` library.** One shared project holds event contracts and small helpers so the three new services do not copy them.
    Its Docker builds use the repository root as the build context.

## Known gaps (not bugs, not built yet)

- **Cancelling an order does not return stock.** No event for it exists in the spec. Decide in Phase 5 with payments and refunds.
- **No duplicate-order guard.** Two taps on "Order" create two orders. The frontend must disable the button; a server-side key needs a new column.
- **Customer order tracking is by polling** `GET /api/orders/{id}`. There is no public hub. Phase 3 polls.
- **Notification is single-instance.** With two instances each event reaches only one of them. Fix in Phase 6 (a queue per instance or a backplane).
- **Hub tokens in the query string** can appear in proxy logs. The gateway logs paths only, not queries. Review in Phase 6.
- **Order history sorts oldest first**, which suits the kitchen queue. Add newest-first for reports in Phase 5.
- **No audit log in Catalog and Order.** Identity has one. Menu edits and status changes are not audited yet.
- **Snapshot ordering.** If two Catalog events for one product arrive out of order, Order briefly shows the older one until the next resync (at most 10 minutes).

## Verification status

- Not compiled and not run: the environment that produced this code had no .NET SDK, Docker, or NuGet access.
  The first `dotnet build`, `dotnet test`, and `.\scripts\smoke-test-phase2.ps1` are the real verification.
  The riskiest parts to watch: the MassTransit outbox wiring in `Program.cs`, the migrations that `dotnet ef` generates, and the SignalR checks.
- No integration tests yet (they need Testcontainers). Role checks are covered by the smoke test.
