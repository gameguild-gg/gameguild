import { defineConfig, devices } from "playwright/test";

// ponytail: imports from "playwright/test" (re-exports @playwright/test).
// T1 only added the `playwright` lib, not `@playwright/test`. Using the lib's
// subpath avoids adding a duplicate devDependency.

export default defineConfig({
  testDir: "./tests",
  fullyParallel: false,
  workers: 1,
  reporter: "list",
  use: {
    baseURL: "http://localhost:3000",
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
  webServer: {
    command: "pnpm dev",
    url: "http://localhost:3000",
    reuseExistingServer: !process.env.CI,
    timeout: 60_000,
  },
});
