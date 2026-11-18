# Phase 2 runbook (Windows)

How to install the Phase 2 zip, run it on your PC, and check that it works. Phase 1 must already be installed.

## 1. Put the zip into your project folder

The zip leaves out `.git` and `.agents`, so it is safe to extract over your existing folder.

```powershell
cd D:\pos-qr-code
Expand-Archive -Force C:\Users\Dell\Downloads\pos-phase2.zip D:\pos-qr-code
```

Your `.env` is not touched. It does not need new values: the riel rate defaults to 4000.
To change it, add this line to `.env`: `USD_TO_KHR_RATE=4100`

## 2. Setup (creates the two new database migrations and runs the tests)

If PowerShell blocks scripts, run once: `Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass`

```powershell
cd D:\pos-qr-code
.\scripts\dev-setup.ps1 -RunTests
```

Make sure Docker Desktop is running. The script builds the solution, creates the `Migrations` folders for Catalog and Order
(Identity already has one), runs the unit tests, and starts PostgreSQL and RabbitMQ.

**If the build or a test fails**, copy the first error message to your AI. This code was written without a .NET SDK,
so your first build is its first real compile.

## 3. Run

```powershell
.\scripts\run-all.ps1
```

Five windows open: Identity 5003, Catalog 5001, Order 5002, Notification 5005, Gateway 5000.
Wait until each window says `Now listening on` (about 20 seconds). In Development mode the first start creates a sample menu and 10 tables.

## 4. Check it works

```powershell
.\scripts\smoke-test.ps1          # Phase 1 checks
.\scripts\smoke-test-phase2.ps1   # Phase 2 checks, expect only PASS lines
```

The Phase 2 script waits up to 45 seconds for the Order service to receive the menu from Catalog, and up to 45 seconds for the stock to drop.
Each run adds one sold-out "Smoke item" product and one completed order. That is normal.

## 5. Try it by hand

Browse the menu (public). Every price comes back in dollars and riel:

```powershell
(Invoke-RestMethod http://localhost:5000/api/catalog/products).data | Select-Object name, price, priceKhr
```

Price a cart (the server does the math). Use real ids from the call above:

```powershell
$p = (Invoke-RestMethod http://localhost:5000/api/catalog/products).data | Where-Object name -eq 'Lemon Tea'
$body = @{ items = @(@{ productId = $p.id; quantity = 3 }) } | ConvertTo-Json -Depth 5
Invoke-RestMethod -Method Post -Uri http://localhost:5000/api/orders/cart -ContentType 'application/json' -Body $body
```

Watch the messages move: open http://localhost:15672 (user and password are `RABBITMQ_USER` and `RABBITMQ_PASSWORD` in `.env`) and look at the
Queues tab. You should see queues for Catalog, Order, and Notification, each with 0 ready messages when idle.

| What | URL |
| --- | --- |
| Gateway health | http://localhost:5000/health |
| Catalog / Order / Notification health | http://localhost:5001/health, http://localhost:5002/health, http://localhost:5005/health |
| RabbitMQ management | http://localhost:15672 |

## 6. Stop

Close the five windows, then `docker compose -f infra/docker-compose.yml --env-file .env down`.
Add `-v` for a full reset (deletes all databases; the sample menu and tables are created again on the next start).

## Optional: everything in Docker

```powershell
docker compose -f infra/docker-compose.yml --env-file .env --profile apps up -d --build
```

Use either this or `run-all.ps1`, not both (same ports). Run `dev-setup.ps1` first so the migrations exist before the images are built.
Containers run in Production mode, so the sample menu and tables are not created: set `Seed__SampleMenu=true` for catalog and `Seed__TableCount=10` for order
in `infra/docker-compose.yml` if you want them.

## Troubleshooting

| Problem | Fix |
| --- | --- |
| Smoke test says the Order service never received the menu | Check the Order window for errors and that RabbitMQ is healthy (`docker compose ... ps`). Catalog re-sends the menu every 10 minutes and 10 seconds after start; restart Catalog to resend now |
| `Sample menu products not found` | The sample menu is created only on an empty `catalog_db` in Development. Reset with `down -v` and start again |
| Login 429 in the smoke test | The login limit is 10 per minute per IP. Wait one minute |
| Order window: `ACCESS_REFUSED` or `connection refused` for RabbitMQ | Check `RABBITMQ_USER` and `RABBITMQ_PASSWORD` in `.env` match the running container. If you changed them after the first start, run `down -v` |
| `dotnet ef` complains about the tool version | Run `dotnet tool restore` in the repository root |
| Prices in riel look wrong | The rate is `USD_TO_KHR_RATE` in `.env`. Restart Catalog and Order after changing it |

## When a phase is finished

```powershell
.\scripts\make-phase-zip.ps1 -Phase 2
```
