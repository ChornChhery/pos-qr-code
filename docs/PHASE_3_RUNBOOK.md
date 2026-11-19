# Phase 3 runbook (Windows)

How to install the Phase 3 zip, run the web menu on your PC (and on your phone), and check that it works.
Phases 1 and 2 must already be installed.

## 0. Install once

| Tool | Where | Check |
| --- | --- | --- |
| Node.js 22 LTS (new in Phase 3) | https://nodejs.org | `node --version` prints `v22` or higher |

## 1. Put the zip into your project folder

```powershell
cd D:\pos-qr-code
Expand-Archive -Force C:\Users\Dell\Downloads\pos-phase3.zip D:\pos-qr-code
```

## 2. Setup (installs the web packages and runs all the tests)

If PowerShell blocks scripts, run once: `Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass`

```powershell
cd D:\pos-qr-code
.\scripts\dev-setup.ps1 -RunTests
```

Docker Desktop must be running. The first run downloads about 400 packages for the web menu, so it needs internet and takes a few minutes.
It then runs the .NET tests and the web tests (type-check, lint, 85 tests).

If a step fails, copy the first error message to your AI.

## 3. Run

```powershell
.\scripts\run-all.ps1
```

Six windows open. Wait until the backend windows say `Now listening on` and the web window says `Ready` (about 30 seconds).

## 4. Check it works

```powershell
.\scripts\smoke-test.ps1          # Phase 1
.\scripts\smoke-test-phase2.ps1   # Phase 2
.\scripts\smoke-test-phase3.ps1   # Phase 3: web menu, riel prices, Khmer, and that the browser calls are allowed
```

## 5. Try the menu yourself

Open **http://localhost:3000/?table=1** in Chrome or Edge. Things to try:

1. You see Drinks, Mains, Desserts, with dollars and riel on every price.
2. Press **ខ្មែរ** at the top right. The whole screen changes to Khmer. Press **EN** to go back.
3. Tap **Iced Latte**. The "Add to cart" button is greyed out until you pick a size. Pick Large and an extra, then add.
4. A bar appears at the bottom with the count and a "≈" estimated total. Open it. The total becomes the server's price (no "≈").
5. Press **Place order**. You land on the order page showing "Order received".
6. Make the kitchen move the order (the kitchen screen comes in Phase 4, so do it from PowerShell). The order id is the last part of the page address.
   The page updates by itself within 5 seconds:

```powershell
$login = Invoke-RestMethod -Method Post -Uri http://localhost:5000/api/auth/login -ContentType 'application/json' -Body (@{ username = 'admin'; password = '<SEED_ADMIN_PASSWORD from .env>' } | ConvertTo-Json)
$h = @{ Authorization = "Bearer $($login.data.accessToken)" }
$id = '<the order id from the page address>'
Invoke-RestMethod -Method Patch -Uri "http://localhost:5000/api/orders/$id/status" -Headers $h -ContentType 'application/json' -Body (@{ status = 'COOKING' } | ConvertTo-Json)
# then READY, then COMPLETED
```

7. Close the tab and open `http://localhost:3000/` again: the cart and the table are remembered, and "My order" appears in the header.

## 6. Try it on your phone

The phone and the PC must be on the same Wi-Fi.

1. Find the PC's address: run `ipconfig` and read **IPv4 Address** (for example `192.168.1.50`).
2. Open PowerShell **as Administrator** once and allow the two ports on private networks:

```powershell
New-NetFirewallRule -DisplayName "POS dev" -Direction Inbound -Protocol TCP -LocalPort 3000,5000 -Action Allow -Profile Private
```

3. Stop the services (close the windows) and start them again with your address:

```powershell
.\scripts\run-all.ps1 -LanIp 192.168.1.50
```

4. On the phone open `http://192.168.1.50:3000/?table=1`. To test a real QR scan, make a QR code of that address with any free QR generator and scan it
   with the phone camera. (The built-in QR generator for tables arrives in Phase 5.)

## 7. Work on the menu without the backend

```powershell
cd apps\frontend
npm run dev:mock     # window 1: fake API on port 5000 (stop the real gateway first)
npm run dev          # window 2: web menu on port 3000
```

## 8. Run the browser test (optional)

```powershell
cd apps\frontend
npx playwright install chromium    # once, about 150 MB
npm run test:e2e
```

It opens a phone-sized browser, orders a latte, follows the order, and switches to Khmer, against the mock API. This test has never been run by the AI that
wrote it, so if it fails, copy me the error.

## 9. Stop

Close all the windows, then `docker compose -f infra/docker-compose.yml --env-file .env down`.

## Troubleshooting

| Problem | Fix |
| --- | --- |
| `Node.js not found` or older than 22 | Install Node.js 22 LTS, open a new PowerShell window, run setup again. Or add `-SkipFrontend` to skip the web menu |
| The menu says "We could not load the menu" | The gateway is not running or not on port 5000. Check the Gateway window. Then press **Try again** |
| Menu is empty | The sample menu is created on an empty `catalog_db` in Development mode. See the Phase 2 runbook for a reset |
| Order button stays grey | The cart is being priced (wait a second) or no table is chosen: use the table list in the cart, or open the menu from a `?table=` link |
| Browser console shows a CORS error | The page address is not in the gateway's allowed list. For a phone use `-LanIp`; for another PC add its origin to `Cors:AllowedOrigins` in `services/ApiGateway/appsettings.json` |
| Phone cannot open the page | Same Wi-Fi? Firewall rule added (step 6)? Corporate or guest Wi-Fi often blocks device-to-device traffic |
| Service windows close at once with `SigningKey ... minimum length of '32'` | `.env` holds placeholders (copied by hand from `.env.example`). `run-all.ps1` now says so. Fix: `docker compose -f infra/docker-compose.yml --env-file .env down -v`, then `Remove-Item .env`, then `.\scripts\dev-setup.ps1` (deletes the local databases only) |
| Catalog or Order window crashes with `password authentication failed for user "pos"` | Another PostgreSQL installed on your PC is using port 5432, so the services reach it instead of Docker. Check: `Get-NetTCPConnection -LocalPort 5432 -State Listen \| ForEach-Object { Get-Process -Id $_.OwningProcess } \| Select Id, ProcessName`. Fix: `(Get-Content .env) -replace '^POSTGRES_HOST_PORT=.*','POSTGRES_HOST_PORT=5433' \| Set-Content .env`, then `docker compose -f infra/docker-compose.yml --env-file .env up -d --wait postgres rabbitmq`, then `.\scripts\run-all.ps1` |
| Port 3000 in use | Close the other program. The port is fixed by the architecture |
| Wrong restaurant name | Set `NEXT_PUBLIC_RESTAURANT_NAME` in `apps\frontend\.env.local`, restart the web window |

## When a phase is finished

```powershell
.\scripts\make-phase-zip.ps1 -Phase 3
```
