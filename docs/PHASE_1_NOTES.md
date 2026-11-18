# Phase 1 notes: decisions, file map, open items

Read this before building on Phase 1. The contract lives in `AI_CONTEXT.md`; this file records what Phase 1
added or decided beyond it.

## Built

| Area | Where |
| --- | --- |
| Solution and SDK pin | `POS.slnx`, `global.json`, `.config/dotnet-tools.json` (dotnet-ef 10.0.0) |
| API Gateway (YARP, port 5000) | `services/ApiGateway` |
| Identity Service (port 5003, `identity_db`) | `services/IdentityService` |
| Unit tests | `tests/IdentityService.Tests` |
| Docker Compose (PostgreSQL 16, RabbitMQ 3.13) | `infra/docker-compose.yml`, `infra/postgres/init` |
| Terraform (VPC, ECR, RDS, S3, Secrets Manager) | `infra/terraform` |
| CI | `.github/workflows/ci.yml` |
| Dev scripts | `scripts/` |

## Behavior

- Gateway routes (see `services/ApiGateway/appsettings.json`): `/api/auth/*` Identity, `/api/catalog/*`,
  `/api/orders/*`, `/api/payments/*`, `/hubs/*` to their future services.
  `/api/auth/login` and `/api/auth/refresh` are anonymous; every other `/api/auth/*` route needs a valid JWT at the gateway.
  Other routes pass through anonymously; the owning service enforces auth (the gateway cannot know which endpoints are public).
- Rate limits per client IP per minute: `login` 10, `public-write` 30 (checkout, slip upload), `general` 300.
- Identity endpoints exactly as in `AI_CONTEXT.md` section 5. Roles are stored and sent as `CASHIER`, `KITCHEN`, `MANAGER`, `ADMIN`.
- Access token: HS256, 15 minutes, claims `sub`, `name`, `role`, `jti`. Refresh token: 7 days, HttpOnly + Secure + SameSite=Strict
  cookie `refresh_token`, path `/api/auth`, never in a JSON body.
- Login failures return one message for unknown user, wrong password, and inactive user, and do equal hashing work.
- An admin cannot change their own role. Passwords: 10 to 128 characters, PBKDF2 (ASP.NET Core `PasswordHasher`).
- Audit log actions: `LOGIN_SUCCESS`, `LOGIN_FAILED`, `USER_REGISTERED`, `USER_ROLE_CHANGED`.
- Response envelope `{ success, data, message, errors }` on every Identity response, including errors.

## Decisions beyond the spec (need your approval or review)

1. **Refresh tokens are stateless signed JWTs** (audience `pos-api.refresh`). The spec has no refresh-token table, and a new
   table needs approval. Consequence: a refresh token cannot be revoked before it expires. Deactivated users and role changes are
   still enforced at each refresh because the user is re-read from `identity_db`. Proposed fix in Phase 6: a `refresh_tokens`
   table with rotation and reuse detection.
2. **No logout endpoint.** The spec lists none, and the cookie is HttpOnly, so a browser cannot clear it itself.
   Phase 4 needs `POST /api/auth/logout` that clears the cookie. Needs approval.
3. **Config keys and environment variables added** (all in `.env.example`): `POSTGRES_USER`, `POSTGRES_PASSWORD`,
   `POSTGRES_HOST_PORT`, `RABBITMQ_USER`, `RABBITMQ_PASSWORD`, `JWT_SIGNING_KEY`, `SEED_ADMIN_USERNAME`, `SEED_ADMIN_PASSWORD`.
   App settings: `Jwt:*`, `Seed:*`, `Database:MigrateOnStartup`, `Cors:AllowedOrigins`, `ConnectionStrings:IdentityDb`.
4. **First admin is seeded** on startup, only when the users table is empty and `Seed__AdminPassword` is set. Needed because
   `register` requires an ADMIN.
5. **`GET /health`** on both services (infrastructure, not part of the API contract).
6. **CI lives in the repository root** (`.github/workflows/`). GitHub ignores workflows under `infra/`.
7. **FluentAssertions is pinned to 7.2.0.** Version 8 and later need a paid license for commercial use.
8. **Shared HS256 key** between Identity and Gateway. Move to RS256 (Identity holds the private key) in Phase 6.
9. **Migrations are generated on the developer machine** by `dev-setup.ps1` (no SDK was available when this was written).
   Commit `services/IdentityService/Migrations` after the first run. Migrations run on startup (`Database:MigrateOnStartup`);
   production should use a separate migration job (Phase 6).
10. **Terraform stops at the foundation** (VPC, ECR, RDS, S3, secret). Amazon MQ, ECS Fargate, CloudFront, and a NAT gateway come in Phase 6.

## Verification status

- Not compiled and not run: the environment that produced this code had no .NET SDK, Docker, Terraform, or NuGet access.
  Treat the first `dotnet build`, `dotnet test`, `terraform validate`, and CI run as the real verification.
- Coverage gate (80%) is collected but not enforced yet.
- E2E (Playwright) starts with the frontend in Phase 3.

## Still open from `AI_CONTEXT.md` section 12

Currency and locale, tax model, QR payment gateway versus slip upload, offline behavior. The tax model and currency must be
decided before Phase 2 (order totals).
