# SKILL.md

Skill routing for the Restaurant POS project. Rules: `CLAUDE.md`. Spec: `AI_CONTEXT.md`.
Skills live in `.agents/skills/<name>/SKILL.md` and load their bodies only when used.

## Installed skills (25)

| Group | Skills |
| --- | --- |
| Project | `restaurant-pos` |
| Design | `impeccable`, `design-taste-frontend`, `ui-ux-pro-max`, `design-system`, `ui-styling` |
| References | `mobbin-best-practices`, `search`, `redesign-screen`, `plain-writing` |
| Motion | `transitions-dev`, `transitions-polish`, `pick-ui-library` |
| Engineering | `frontend-ui-engineering`, `api-and-interface-design`, `spec-driven-development`, `planning-and-task-breakdown`, `test-driven-development`, `debugging-and-error-recovery`, `security-and-hardening`, `performance-optimization`, `code-review-and-quality`, `git-workflow-and-versioning` |
| Verification | `web-design-guidelines`, `webapp-testing`, `skill-creator` |

Notes:
- `frontend-design` and `test-driven-development` already exist in the global skill set, so the
  project copies were removed to avoid duplicate descriptions.
- Mobbin's `best-practices` was renamed `mobbin-best-practices` to avoid colliding with the
  global `best-practices`.
- Mobbin skills need the Mobbin connection and tools.
- `impeccable` ships a hook in `.codex/hooks.json` and an engine in `.agents/skills/impeccable`.
- `design-taste-frontend` needs Node 22+ and Chrome/Edge/Chromium for its finish gate.

## Task -> sequence

New page or screen:
1. `spec-driven-development` (scope, states, data, roles)
2. `impeccable` (Persuade for customer menu, Operate for staff)
3. `design-taste-frontend` or `ui-ux-pro-max` (direction)
4. `design-system` + `ui-styling` (tokens and shadcn)
5. `pick-ui-library` before adding dependencies
6. `transitions-dev` then `transitions-polish`
7. `frontend-ui-engineering`
8. `web-design-guidelines` + `webapp-testing`

Use `redesign-screen` instead of steps 2-4 when the user supplies a screen and Mobbin is
connected; read `mobbin-best-practices` and `search` first.

New endpoint or event:
1. `spec-driven-development`
2. `api-and-interface-design`
3. `test-driven-development` (failing test first)
4. `security-and-hardening`
5. `code-review-and-quality`
6. `git-workflow-and-versioning`

Bug fix:
1. `debugging-and-error-recovery`
2. `test-driven-development` (regression test)
3. Fix, then `code-review-and-quality`

Payment or auth change:
1. `security-and-hardening`
2. `spec-driven-development` if a contract changes
3. `test-driven-development`
4. `code-review-and-quality`

Performance:
1. `performance-optimization` (measure first)
2. `transitions-polish` if motion only
3. Re-measure. No claim without before/after evidence.

Before merge:
1. `code-review-and-quality`
2. `web-design-guidelines` if UI changed
3. `git-workflow-and-versioning`

## Project rules that override any skill

- One database per service. No shared DB or cross-service join.
- No synchronous HTTP between services for business logic.
- Order and payment transitions are server-validated; no jumps.
- Payment slips are untrusted; verify before completion.
- Cart totals are server-authoritative.
- Carts use a randomized session id, not the table number.
- Sold-out items disappear from the public menu immediately.
- Customer targets >= 44x44 px; staff >= 48x48 px.
- Money is `decimal` / `DECIMAL(10,2)`.
- No `any`, no `SELECT *`, no secrets, no disabled auth or tests.

## Verification

| Gate | Check |
| --- | --- |
| Types | frontend typecheck, backend build |
| Lint | linter for the touched stack |
| Tests | unit + integration green |
| E2E | Playwright on the touched journey |
| A11y | `web-design-guidelines`, keyboard path |
| Responsive | 360 px, 390 px, 768 px |
| Motion | `transitions-polish` when motion changed |
| Security | server-side auth and role check |

Commands that exist after Phase 1 (PowerShell, repo root):

| Purpose | Command |
| --- | --- |
| First-time setup | `.\scripts\dev-setup.ps1` |
| Build | `dotnet build POS.slnx` |
| Unit tests | `dotnet test POS.slnx` |
| Run gateway + identity | `.\scripts\run-all.ps1` |
| Phase 1 smoke test | `.\scripts\smoke-test.ps1` |
| Phase 2 smoke test | `.\scripts\smoke-test-phase2.ps1` |
| Phase 3 smoke test | `.\scripts\smoke-test-phase3.ps1` |
| Web menu: tests / lint / types | `cd apps\frontend; npm test; npm run lint; npm run typecheck` |
| Web menu without the backend | `cd apps\frontend; npm run dev:mock` (then `npm run dev`) |

If a command does not exist yet, say so. Do not invent one.

## Token discipline

- Do not read all docs or all skills. Load only the skills the current task needs.
- Ask for `AI_CONTEXT.md`; open `docs/AI_CONTEXT_FULL.md` only for details missing there.
- Descriptions are trimmed. `npx skills update` restores upstream descriptions; re-trim after
  updating if per-chat context matters.
