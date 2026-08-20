import { test, expect } from "playwright/test";

// Homepage smoke — proves the harness + live web app work.
// Skip unless explicitly invoked with E2E_RUN=1 (needs web on localhost:3000):
//   E2E_RUN=1 pnpm e2e
test.skip(!process.env.E2E_RUN, "set E2E_RUN=1 (needs web on :3000)");

test("homepage loads", async ({ page }) => {
  await page.goto("/");
  await expect(page).toHaveTitle(/.+/);
});
