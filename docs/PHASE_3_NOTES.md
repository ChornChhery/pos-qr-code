# Phase 3 notes: decisions, file map, open items

Read this before building on Phase 3. The contract lives in `AI_CONTEXT.md`; this file records what Phase 3 added or
decided beyond it. Earlier notes: `docs/PHASE_1_NOTES.md`, `docs/PHASE_2_NOTES.md`.

## Built

| Area | Where |
| --- | --- |
| Customer web menu (Next.js 16, React 19, TypeScript strict, Tailwind 4) | `apps/frontend` |
| Mock POS API (develop and test the menu without the backend) | `apps/frontend/mock-api/server.mjs` |
| Unit and component tests (Vitest, 85 tests) | `apps/frontend/src/**/*.test.ts(x)` |
| Playwright journey (3 tests, runs against the mock API) | `apps/frontend/e2e` |
| Phase 3 smoke test | `scripts/smoke-test-phase3.ps1` |
| Web in CI, Docker, and the scripts | `.github/workflows/ci.yml`, `infra/docker-compose.yml`, `scripts/*.ps1` |

## What a guest can do

1. Scan a table QR code that opens `/?table=12`. The table is remembered on that phone.
2. Browse the menu by category (Khmer or English). Prices show in dollars and riel. Sold-out dishes are greyed out.
3. Open a dish, choose options (the rules from Catalog are enforced: required groups, maximum choices), set a quantity and a note, add to cart.
4. Open the cart. The server prices it and returns the total (USD and riel). Place the order.
5. Follow the order on `/order/{id}`: Order received, Cooking, Ready, Completed. The page checks the server every 5 seconds.

## Decisions you gave me

- **Languages:** Khmer and English. **Brand color:** warm deep orange (`#c2410c`, white text on it is 5.2:1, which passes WCAG AA).

## How it works

- **Server rendering.** The menu page is rendered on the server and its data is cached for 30 seconds. If the API is down when the page is
  rendered, the page still loads and the browser fetches the menu itself, with a retry button if that fails too.
- **Language.** Chosen language is a cookie (`lang`). First visit: the phone's language (Khmer if it asks for Khmer), else English.
  All screen text is in `src/lib/i18n/dictionaries.ts`; a test fails if the two languages have different keys or placeholders.
  Dish names and descriptions are shown exactly as entered by staff.
- **Money.** The browser never converts currency. Every price on screen is a value the API returned (`price` + `priceKhr`,
  `total` + `totalKhr`). While the server price is loading the cart shows an estimate marked with "≈".
- **Cart.** Kept in the browser (localStorage, per phone). Identical dishes with the same options and note merge into one line.
  The server prices the cart again at checkout, so a changed price or a sold-out dish is caught there.
- **Double tap.** The "Place order" button is disabled while the request runs and a second tap is ignored, so one tap-happy guest
  cannot create two orders (this was a known gap from Phase 2).
- **Accessibility.** Buttons and rows are at least 44 px tall, keyboard focus is visible, the product and cart sheets trap focus and close with Escape
  (Radix Dialog), status changes are announced to screen readers, and animations stop for people who ask for reduced motion.

## Decisions beyond the spec (need your approval or review)

1. **Khmer text was written by me, not a native speaker.** Please have someone who reads Khmer review `km` in `src/lib/i18n/dictionaries.ts` before launch.
2. **Dish names are not translated.** The Catalog data model has one name and one description per product. A Khmer name needs new columns
   (for example `name_km`). Not added.
3. **Western digits** (1, 2, 3) are used for prices in both languages, as on most Cambodian menus. Khmer numerals would be a small change.
4. **shadcn/ui components are hand-written.** The shadcn tool downloads components from its own server, which I could not reach, so `Button`, `Sheet`,
   and `Skeleton` in `src/components/ui` follow the shadcn pattern (Radix + class-variance-authority + tailwind-merge) by hand.
5. **Table number comes from the link.** Anyone can open `/?table=7` from home and order for table 7. This is how the spec's QR flow works, and it is
   limited only by the gateway rate limits. A signed per-table token would fix it but needs a new column and endpoint (decide in Phase 5 with the QR generator).
6. **Guests have no account.** The id of their last order is kept in the browser so they can find it again ("My order" in the header).
7. **Package choices.** Next.js 16, React 19, Tailwind 4, TypeScript 5.9 (npm also offers TypeScript 7, a rewrite; I used the 5.9 line these tools are built for and did not test 7),
   ESLint 9 (npm marks 9.x as past support; move to ESLint 10 when `eslint-config-next` supports it). Node.js 22 or newer is required.
8. **New settings:** `API_URL`, `NEXT_PUBLIC_API_URL`, `NEXT_PUBLIC_RESTAURANT_NAME`, `NEXT_PUBLIC_LOGO_URL` (in `apps/frontend/.env.local`),
   `ALLOWED_DEV_ORIGINS` (phone testing), and `RESTAURANT_NAME`, `PUBLIC_API_URL` for Docker. `NEXT_PUBLIC_*` values are compiled into the page.
9. **Fonts are self-hosted** (Noto Sans Khmer through the `@fontsource-variable` package), so the menu makes no request to Google and works on a restaurant network that blocks it.
10. **No Content-Security-Policy yet.** Basic security headers are on (`nosniff`, `DENY` framing, referrer policy). A strict CSP needs per-request nonces; planned for Phase 6.

## Known gaps (not bugs, not built yet)

- **No payment screen.** Payment arrives in Phase 5. For now an order is placed and the guest pays staff as usual.
- **The menu is not live.** It refreshes every 60 seconds and when the guest returns to the tab. A sold-out change shows up within a minute; the cart
  check at checkout is always exact.
- **No product photos in the sample menu.** Rows with `imageUrl` show a thumbnail; rows without one are text only on purpose.
- **Order status polls** every 5 seconds instead of a live push. A public hub would need an unauthenticated SignalR endpoint; deferred.
- **Server messages are English.** When the server refuses a cart ("Only 3 of Lemon Tea left.") that sentence is shown as the server wrote it.
- **No dark mode, no offline mode.**

## Verification status

Run here, all passing:
- `npm run typecheck`, `npm run lint`, `npm test` (85 tests), `npm run build` (production build, Next.js 16).
- The production server against the mock API: HTML contains the menu, riel prices, Khmer by cookie and by `Accept-Language`, security headers,
  and the page still loads when the API is down.

**Not run, because this environment has no browser and no backend:**
- The Playwright journey (`npm run test:e2e`). It is written but never executed.
- Looking at it on a phone. The layout is one column up to 48 rem wide; I have **not** checked it at 360, 390, and 768 px in a real browser.
- An accessibility audit (axe or Lighthouse).
- The web menu against the real gateway and services, and the new PowerShell script changes. `scripts\smoke-test-phase3.ps1` checks the first of these.
