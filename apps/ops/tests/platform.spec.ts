import { expect, test, type Page } from "playwright/test";

// T11 smoke: platform pages render their primary surface without a runtime
// crash. Asserts the established container class (or the empty/loading state)
// for /alerts, /services, /backups. Only `pageerror` events fail the test
// unconditionally (see beforeEach).

test.beforeEach(async ({ page }: { page: Page }) => {
  page.on("pageerror", (error) => {
    throw new Error(`runtime pageerror: ${error.message}`);
  });
});

test("alerts page renders table or empty state", async ({ page }: { page: Page }) => {
  await page.goto("/alerts");
  await page.waitForLoadState("networkidle");
  await expect(
    page.locator(".alerts-table").or(page.locator(".no-alerts"))
  ).toBeVisible();
});

test("services page renders 6 service cards or loading state", async ({
  page,
}: {
  page: Page;
}) => {
  await page.goto("/services");
  await page.waitForLoadState("networkidle");
  const loading = page.getByText("Loading services…");
  const cards = page.locator(".service-card");
  // Either loading text is visible OR 6 cards rendered.
  const loadingVisible = await loading.isVisible().catch(() => false);
  if (!loadingVisible) {
    await expect(cards).toHaveCount(6);
  }
});

test("backups page renders schedule table or empty state", async ({
  page,
}: {
  page: Page;
}) => {
  await page.goto("/backups");
  await page.waitForLoadState("networkidle");
  await expect(
    page
      .locator(".backup-table")
      .or(page.getByText("No backup schedules configured."))
  ).toBeVisible();
});
