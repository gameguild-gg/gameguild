import { expect, test, type Page } from "playwright/test";

// T9 smoke: overview / nodes / pods render without runtime crashes.
// API routes may be unreachable against a dev environment with no in-cluster
// creds, so loading/error states are acceptable outcomes — the test only
// asserts structure is on the DOM and no `pageerror` fires.

test.beforeEach(async ({ page }: { page: Page }) => {
  page.on("pageerror", (error) => {
    throw new Error(`runtime pageerror: ${error.message}`);
  });
});

test("overview renders 6 summary cards", async ({ page }: { page: Page }) => {
  await page.goto("/");
  // Wait for either real data or the loading skeleton — both render the
  // .summary-card class.
  await expect(page.locator(".summary-card").first()).toBeVisible({
    timeout: 15_000,
  });
  const count = await page.locator(".summary-card").count();
  expect(count).toBeGreaterThanOrEqual(6);
});

test("nodes page renders node cards or a retry alert", async ({
  page,
}: {
  page: Page;
}) => {
  await page.goto("/nodes");
  await expect(page.locator("h1")).toContainText("Node Topology", {
    timeout: 15_000,
  });
  // Settle whatever loading state we started in.
  await page.waitForLoadState("networkidle");
  const nodeCards = await page.locator(".node-card").count();
  const alert = await page.locator('[role="alert"]').count();
  // Either we got real nodes, or the error alert with retry copy. Both are
  // valid; only a runtime crash (pageerror) fails the test.
  expect(nodeCards >= 1 || alert === 1).toBeTruthy();
});

test("pods page renders the table with namespace filter", async ({
  page,
}: {
  page: Page;
}) => {
  await page.goto("/pods");
  await expect(page.locator(".pod-table table")).toBeVisible({
    timeout: 15_000,
  });
  // shadcn Select renders its trigger with this slot.
  await expect(
    page.locator(".pod-table [data-slot='select-trigger']"),
  ).toBeVisible();
});
