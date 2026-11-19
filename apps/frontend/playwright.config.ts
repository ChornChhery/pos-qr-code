import { defineConfig, devices } from "@playwright/test";

const ci = Boolean(process.env.CI);

// The journey runs against the mock API (mock-api/server.mjs), so no backend or database is needed.
export default defineConfig({
  testDir: "./e2e",
  fullyParallel: false,
  retries: ci ? 1 : 0,
  reporter: ci ? [["github"], ["html", { open: "never" }]] : "list",
  use: {
    baseURL: "http://localhost:3000",
    trace: "retain-on-failure",
  },
  projects: [
    // Mid-range Android phone, the main target for the customer menu.
    { name: "mobile-chrome", use: { ...devices["Pixel 5"] } },
  ],
  webServer: [
    {
      command: "node mock-api/server.mjs",
      url: "http://localhost:5000/health",
      env: { MOCK_STEP_MS: "1500" },
      reuseExistingServer: !ci,
    },
    {
      command: ci ? "npm run build && npm run start" : "npm run dev",
      url: "http://localhost:3000",
      env: { API_URL: "http://localhost:5000", NEXT_PUBLIC_API_URL: "http://localhost:5000" },
      reuseExistingServer: !ci,
      timeout: 180_000,
    },
  ],
});
