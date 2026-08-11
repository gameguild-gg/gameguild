import { expect, test, type Page } from "playwright/test";

// T8 smoke: sidebar renders with 9 links; clicking each navigates without a
// runtime crash. Pages (T9-T11) do not exist yet, so Next returns 404 — that
// is NOT a failure; only `pageerror` events fail the test.

const NAV_LINKS = [
  "/",
  "/nodes",
  "/pods",
  "/longhorn",
  "/garage",
  "/postgres",
  "/alerts",
  "/services",
  "/backups",
] as const;

test.beforeEach(async ({ page }: { page: Page }) => {
  page.on("pageerror", (error) => {
    throw new Error(`runtime pageerror: ${error.message}`);
  });
});

test("sidebar renders all 9 navigation links", async ({ page }: { page: Page }) => {
  await page.goto("/");
  const links = page.locator("nav a");
  await expect(links).toHaveCount(NAV_LINKS.length);
});

for (const href of NAV_LINKS) {
  test(`navigating to ${href} does not throw a runtime error`, async ({
    page,
  }: {
    page: Page;
  }) => {
    await page.goto("/");
    await page.locator(`nav a[href="${href}"]`).click();
    await page.waitForLoadState("networkidle");
    // 404 is acceptable for unbuilt routes — only pageerror fails (see beforeEach).
    expect(page.url()).toContain(href === "/" ? "localhost:3000" : href);
  });
}
