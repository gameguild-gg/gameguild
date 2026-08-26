/**
 * Instructor coding-assessment authoring — Playwright spec.
 *
 * Verifies an instructor can:
 *   1. Author a STANDARD test (stdin/stdout): reads a string, prints "hello " + string.
 *   2. Author a FUNCTIONAL test: add(int a, int b) -> int.
 *   3. Save and round-trip both via the coding-definition editor.
 *
 * Skipped by default — needs a running web + API stack. Enable by removing
 * `.skip` (or running with PLAYWRIGHT_RUN_SKIPPED=1 after switching the
 * descriptor below). Selectors verified against
 *   src/.../coding-definition/{coding-definition-editor,standard-test-editor,functional-test-editor}.tsx
 *
 * Run manually:
 *   cd apps/web && pnpm playwright test coding-assessment-authoring --reporter=line
 *
 * Note: this project installs `playwright` (not `@playwright/test`); both
 * expose the same `test` / `expect` runtime, but the import must match the
 * installed package.
 */
import { test, expect } from "playwright/test";

// ponytail: env-overridable targets so the spec is not pinned to one host.
const WEB_BASE_URL = (
  process.env.PLAYWRIGHT_WEB_BASE_URL ??
  process.env.NEXT_PUBLIC_APP_URL ??
  "http://localhost:3000"
).replace(/\/$/, "");

const ADMIN_EMAIL = process.env.E2E_SYSTEM_ADMIN_EMAIL ?? "admin@game-guild.com";
const ADMIN_PASSWORD = process.env.E2E_SYSTEM_ADMIN_PASSWORD ?? "Admin123!";

// Known fixture from the user's authoring URL. Override per-env if needed.
const COURSE_ID = process.env.E2E_COURSE_ID ?? "ai4games-by-gameguild";
const ASSESSMENT_ID =
  process.env.E2E_ASSESSMENT_ID ?? "f0a08827-faa0-4ada-beb0-6fc61c731ccb";

const CODING_DEFINITION_ROUTE = `/dashboard/learning/courses/${COURSE_ID}/assessments/${ASSESSMENT_ID}/coding-definition`;

// Skip unless a live stack is present — keeps CI green without a server.
test.describe.skip("Coding Assessment Authoring", () => {
  test("standard test: stdin -> 'hello ' + string, round-trips", async ({
    page,
  }) => {
    page.setDefaultTimeout(60_000);
    page.setDefaultNavigationTimeout(180_000);

    // 1. Sign in as admin.
    await page.goto(`${WEB_BASE_URL}/sign-in`, { waitUntil: "domcontentloaded" });
    await page.getByLabel("Email").fill(ADMIN_EMAIL);
    await page.getByLabel("Password", { exact: true }).fill(ADMIN_PASSWORD);
    await page
      .getByRole("button", { name: "Sign in", exact: true })
      .click();
    await page.waitForURL("**/dashboard/**");

    // 2. Open the coding-definition editor.
    await page.goto(`${WEB_BASE_URL}${CODING_DEFINITION_ROUTE}`, {
      waitUntil: "domcontentloaded",
    });
    await expect(
      page.getByText("Coding Definition Editor"),
    ).toBeVisible();

    // 3. Replace main.cpp with a hello + stdin program.
    // The IDE wraps Monaco; click into the editor, select all, type.
    const fileTreeEntry = page.getByText("main.cpp").first();
    await fileTreeEntry.click().catch(() => {
      // File-tree label may differ in some IDE skins; fall through to editor.
    });

    const editor = page.locator(".monaco-editor").first();
    await editor.click();
    await page.keyboard.press("Meta+A");
    await page.keyboard.type(
      '#include <iostream>\n' +
        "#include <string>\n" +
        "int main() {\n" +
        "    std::string name;\n" +
        '    std::cin >> name;\n' +
        '    std::cout << "hello " << name << std::endl;\n' +
        "    return 0;\n" +
        "}\n",
    );

    // 4. Clear any seeded test rows, then add a standard test.
    await removeAllTestRows(page);

    await page.getByTestId("add-standard").click();

    // 5. stdin="world" -> stdout="hello world".
    await page.getByTestId("standard-stdin-0").fill("world");
    await page.getByTestId("standard-stdout-0").fill("hello world");

    // 6. Save -> editor redirects back to the assessment page.
    await waitForSaveEnabled(page);
    await page.getByTestId("save-button").click();
    await page.waitForURL(`**/assessments/${ASSESSMENT_ID}`, {
      timeout: 90_000,
    });

    // 7. Round-trip: reopen the editor and verify the standard test persisted.
    await page.goto(`${WEB_BASE_URL}${CODING_DEFINITION_ROUTE}`, {
      waitUntil: "domcontentloaded",
    });
    await expect(
      page.getByText("Coding Definition Editor"),
    ).toBeVisible();

    await expect(page.getByTestId("standard-stdin-0")).toHaveValue("world");
    await expect(page.getByTestId("standard-stdout-0")).toHaveValue(
      "hello world",
    );
  });

  test("functional test: add(int a, int b) -> int, round-trips", async ({
    page,
  }) => {
    page.setDefaultTimeout(60_000);
    page.setDefaultNavigationTimeout(180_000);

    // 1. Sign in as admin.
    await page.goto(`${WEB_BASE_URL}/sign-in`, { waitUntil: "domcontentloaded" });
    await page.getByLabel("Email").fill(ADMIN_EMAIL);
    await page.getByLabel("Password", { exact: true }).fill(ADMIN_PASSWORD);
    await page
      .getByRole("button", { name: "Sign in", exact: true })
      .click();
    await page.waitForURL("**/dashboard/**");

    // 2. Open the coding-definition editor.
    await page.goto(`${WEB_BASE_URL}${CODING_DEFINITION_ROUTE}`, {
      waitUntil: "domcontentloaded",
    });
    await expect(
      page.getByText("Coding Definition Editor"),
    ).toBeVisible();

    // 3. Replace main.cpp with an add() definition.
    const fileTreeEntry = page.getByText("main.cpp").first();
    await fileTreeEntry.click().catch(() => {});

    const editor = page.locator(".monaco-editor").first();
    await editor.click();
    await page.keyboard.press("Meta+A");
    await page.keyboard.type(
      "#include <iostream>\n" +
        "int add(int a, int b) {\n" +
        "    return a + b;\n" +
        "}\n",
    );

    // 4. Clear any seeded rows, then add a functional test.
    await removeAllTestRows(page);

    await page.getByTestId("add-functional").click();

    // 5. Signature: add(int) -> int.
    await page.getByTestId("functional-functionName-0").fill("add");
    // returnType defaults to integer — no select needed.

    // Parameter a (integer).
    await page.getByTestId("functional-add-param-0").click();
    await page.getByTestId("functional-param-name-0-0").fill("a");

    // Parameter b (integer).
    await page.getByTestId("functional-add-param-0").click();
    await page.getByTestId("functional-param-name-0-1").fill("b");

    // Case: add(2, 3) -> 5.
    await page.getByTestId("functional-add-case-0").click();
    await page
      .getByTestId("functional-case-input-value-0-0-0")
      .fill("2");
    await page
      .getByTestId("functional-case-input-value-0-0-1")
      .fill("3");
    await page
      .getByTestId("functional-case-expected-value-0-0")
      .fill("5");

    // 6. Save -> redirects to the assessment page.
    await waitForSaveEnabled(page);
    await page.getByTestId("save-button").click();
    await page.waitForURL(`**/assessments/${ASSESSMENT_ID}`, {
      timeout: 90_000,
    });

    // 7. Round-trip.
    await page.goto(`${WEB_BASE_URL}${CODING_DEFINITION_ROUTE}`, {
      waitUntil: "domcontentloaded",
    });
    await expect(
      page.getByText("Coding Definition Editor"),
    ).toBeVisible();

    await expect(page.getByTestId("functional-functionName-0")).toHaveValue(
      "add",
    );
    await expect(page.getByTestId("functional-param-name-0-0")).toHaveValue(
      "a",
    );
    await expect(page.getByTestId("functional-param-name-0-1")).toHaveValue(
      "b",
    );
    await expect(
      page.getByTestId("functional-case-input-value-0-0-0"),
    ).toHaveValue("2");
    await expect(
      page.getByTestId("functional-case-input-value-0-0-1"),
    ).toHaveValue("3");
    await expect(
      page.getByTestId("functional-case-expected-value-0-0"),
    ).toHaveValue("5");
  });
});

/**
 * Remove every standard + functional test row currently rendered.
 * ponytail: indexes reshuffle after each remove, so always re-query and
 * click the first available remove button until none remain.
 */
async function removeAllTestRows(page: import("playwright/test").Page) {
  for (;;) {
    const next = page
      .locator('[data-testid^="standard-remove-"], [data-testid^="functional-remove-"]')
      .first();
    if (!(await next.isVisible().catch(() => false))) return;
    await next.click().catch(() => {});
    await page.waitForTimeout(30);
  }
}

/** Wait for the Save button to be present and enabled (form valid, not pending). */
async function waitForSaveEnabled(page: import("playwright/test").Page) {
  const save = page.getByTestId("save-button");
  await save.waitFor({ state: "visible" });
  await expect(save).toBeEnabled({ timeout: 30_000 });
}
