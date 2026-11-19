# Web menu (apps/frontend)

Customer menu for the restaurant: scan a table QR code, browse the menu in Khmer or English, order, and follow the order.
Next.js (App Router), React 19, TypeScript (strict), Tailwind CSS 4, Zustand, TanStack Query, Radix UI.

## Run

From the repository root, `.\scripts\dev-setup.ps1` installs the packages and `.\scripts\run-all.ps1` starts everything,
including this app at <http://localhost:3000/?table=1>.

Without the backend, use the built-in mock API:

```powershell
cd apps\frontend
npm install
npm run dev:mock     # window 1: fake API on port 5000
npm run dev          # window 2: the web menu on port 3000
```

## Commands

| Command | What it does |
| --- | --- |
| `npm run dev` | Development server on port 3000 |
| `npm run dev:mock` | Fake POS API on port 5000 (menu, cart, checkout, order progress) |
| `npm run typecheck` | TypeScript check |
| `npm run lint` | ESLint |
| `npm test` | Unit and component tests (Vitest) |
| `npm run test:e2e` | Playwright journey against the mock API (run `npx playwright install chromium` once) |
| `npm run build` / `npm start` | Production build and server |

## Settings (`.env.local`, copy from `.env.example`)

| Name | Meaning |
| --- | --- |
| `API_URL` | Where the Next.js server calls the API while rendering pages |
| `NEXT_PUBLIC_API_URL` | Where the guest's browser calls the API. Must be reachable from the guest's phone |
| `NEXT_PUBLIC_RESTAURANT_NAME` | Name in the header |
| `NEXT_PUBLIC_LOGO_URL` | Optional logo (https URL) |

## How it is built

- `src/app`: pages (`/` menu, `/order/[id]` tracking). The menu is rendered on the server and cached for 30 seconds; if the API is down the browser fetches it instead.
- `src/lib`: API client, money formatting, option rules, i18n dictionaries.
- `src/store/cart.ts`: the cart (kept in the browser, per device). The server prices it again at checkout.
- `src/components`: UI. `ui/` holds the small shadcn-style building blocks (button, sheet, skeleton).
- Prices come from the API in USD and riel. The browser never converts between them.
- All text is in `src/lib/i18n/dictionaries.ts`. Add a key to both languages; a test fails if they differ.
