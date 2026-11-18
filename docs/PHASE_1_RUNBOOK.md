# Phase 1 runbook (Windows)

How to install the Phase 1 zip, run it on your PC, and check that it works.

## 0. Install once

| Tool | Where | Check |
| --- | --- | --- |
| .NET 10 SDK | https://dotnet.microsoft.com/download/dotnet/10.0 | `dotnet --version` prints `10.x` |
| Docker Desktop | https://www.docker.com/products/docker-desktop | `docker info` works (Docker Desktop is running) |

Git and Node.js are not needed for Phase 1.

## 1. Put the zip into your project folder

The zip holds only the project files (it leaves out `.git` and `.agents`), so it is safe to extract on top of
your existing folder.

```powershell
cd D:\pos-qr-code
Expand-Archive -Force C:\Users\Dell\Downloads\pos-phase1.zip D:\pos-qr-code
```

## 2. First-time setup

If PowerShell blocks scripts, run this once in the same window: `Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass`

```powershell
cd D:\pos-qr-code
.\scripts\dev-setup.ps1 -RunTests
```

This script:

1. checks .NET 10 and Docker,
2. creates `.env` with random passwords (never committed),
3. builds the solution,
4. creates the Identity database migration (the `services\IdentityService\Migrations` folder),
5. runs the unit tests (`-RunTests`),
6. starts PostgreSQL and RabbitMQ in Docker.

**If the build or a test fails**, copy the first error message to your AI. This code was written without
access to a .NET SDK, so the first build on your PC is the first real compile.

## 3. Run

```powershell
.\scripts\run-all.ps1
```

Two windows open: Identity (port 5003) and Gateway (port 5000). Wait until both print `Now listening on`.

## 4. Check it works

```powershell
.\scripts\smoke-test.ps1
```

You should see only `PASS` lines. The last check expects the catalog route to answer 502, because the
Catalog service does not exist until Phase 2.

Manual login (the admin password is `SEED_ADMIN_PASSWORD` in `.env`):

```powershell
$body = @{ username = 'admin'; password = '<SEED_ADMIN_PASSWORD from .env>' } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://localhost:5000/api/auth/login -ContentType 'application/json' -Body $body
```

| What | URL |
| --- | --- |
| Gateway health | http://localhost:5000/health |
| Identity health (direct) | http://localhost:5003/health |
| RabbitMQ management | http://localhost:15672 (user and password from `.env`) |

## 5. Stop

Close the two service windows, then:

```powershell
docker compose -f infra/docker-compose.yml --env-file .env down
```

Add `-v` to also delete the database volume (a full reset; the admin account is created again on the next start).

## Optional: run the services in Docker too

```powershell
docker compose -f infra/docker-compose.yml --env-file .env --profile apps up -d --build
```

Use either this or `run-all.ps1`, not both (they use the same ports).
Run `dev-setup.ps1` first so the migration exists before the image is built.

## Troubleshooting

| Problem | Fix |
| --- | --- |
| `Docker is installed but not running` | Start Docker Desktop, wait for the whale icon to settle, run the script again |
| Port 5432 already in use | Set `POSTGRES_HOST_PORT=5433` in `.env`, then `docker compose ... down` and run setup again |
| Port 5000, 5003, or 15672 in use | Close the other program. Ports are fixed by the architecture |
| Admin login returns 401 | The admin is created only when the users table is empty. Use the password in `.env`, or do a full reset with `down -v` |
| `Jwt:SigningKey` validation error at startup | `.env` is missing or `JWT_SIGNING_KEY` is shorter than 32 characters. Run `dev-setup.ps1` |
| Smoke test hits 429 | The login limit is 10 per minute per IP. Wait one minute |
| `dotnet ef` not found | Run `dotnet tool restore` in the repository root |

## When a phase is finished

```powershell
.\scripts\make-phase-zip.ps1 -Phase 1
```

Creates `pos-phase1.zip` in your Downloads folder, without `.git`, `.agents`, build output, or `.env`.
