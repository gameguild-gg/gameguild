import { defineConfig } from "playwright/test";

// Root-level E2E suite (./e2e). App-level E2E stays in apps/web.
// Specs skip unless E2E_RUN=1. With E2E_RUN=1, globalSetup gates on the full
// stack — docker DBs (compose --wait) + API /health + web — before any test.
export default defineConfig({
  testDir: "./e2e",
  globalSetup: "./scripts/e2e-global-setup.mjs",
  timeout: 30_000,
  fullyParallel: true,
  reporter: [["line"]],
  use: {
    baseURL: (
      process.env.PLAYWRIGHT_WEB_BASE_URL ?? "http://localhost:3000"
    ).replace(/\/$/, ""),
  },
});
