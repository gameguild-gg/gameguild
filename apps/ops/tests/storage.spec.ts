import { expect, test, type Page } from "playwright/test";

// T10 smoke: each storage route renders its primary container (data table or
// cluster card) OR an Alert when the API is unreachable. Outside the cluster
// /api/* returns 500 → page shows an Alert; inside the cluster the data
// surface renders. Either is an acceptable pass; only `pageerror` fails.

test.beforeEach(async ({ page }: { page: Page }) => {
  page.on("pageerror", (error) => {
    throw new Error(`runtime pageerror: ${error.message}`);
  });
});

const ROUTES = [
  { path: "/longhorn", selector: ".volume-table" },
  { path: "/garage", selector: ".garage-node-table" },
  { path: "/postgres", selector: ".cnpg-cluster-card" },
] as const;

for (const route of ROUTES) {
  test(`${route.path} renders ${route.selector} or error alert`, async ({
    page,
  }: {
    page: Page;
  }) => {
    await page.goto(route.path);
    await page.waitForLoadState("networkidle");
    await expect(
      page.locator(`${route.selector}, [data-slot="alert"]`).first(),
    ).toBeVisible();
  });
}
