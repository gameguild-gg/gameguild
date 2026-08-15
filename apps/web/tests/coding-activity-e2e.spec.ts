/**
 * Coding activity — REAL end-to-end Playwright spec (student IDE).
 *
 * Drives the learner-facing coding activity page for the seeded
 * ai4games assessment: exercises the Test Cases tab, draft persistence
 * across reloads, the Reset-to-instructor-originals flow, the Run Tests
 * results auto-switch, and the full-width mount.
 *
 * Gated behind E2E_RUN so CI stays green without a live stack.
 *
 * Run manually (needs web + API + seeded admin on localhost:3000):
 *   cd apps/web && E2E_RUN=1 pnpm playwright test coding-activity-e2e --reporter=line
 *
 * Selectors verified against:
 *   packages/infrastructure/ui-emception/src/components/Ide.tsx
 *   packages/infrastructure/ui-emception/src/components/TestCasesPanel.tsx
 *   apps/web/src/app/[locale]/learn/courses/[slug]/activities/[activityId]/page.tsx
 */
import { test, expect, type Page } from "playwright/test";

// ponytail: env-overridable targets so the spec is not pinned to one host.
const WEB_BASE_URL = (
  process.env.PLAYWRIGHT_WEB_BASE_URL ??
  process.env.NEXT_PUBLIC_APP_URL ??
  "http://localhost:3000"
).replace(/\/$/, "");

const ADMIN_EMAIL = process.env.E2E_SYSTEM_ADMIN_EMAIL ?? "admin@game-guild.com";
const ADMIN_PASSWORD = process.env.E2E_SYSTEM_ADMIN_PASSWORD ?? "Admin123!";

const LEARN_ROUTE =
  "/learn/courses/ai4games/activities/assessment-f0a08827-faa0-4ada-beb0-6fc61c731ccb";

// Unique per run so a stale draft from an earlier run never passes the
// marker assertions.
const DRAFT_MARKER = `// DRAFT-PROOF ${Date.now()}`;

test.describe("Coding Activity E2E", () => {
  // Skip unless explicitly invoked with E2E_RUN=1 — runnable, but CI-safe.
  test.skip(!process.env.E2E_RUN, "set E2E_RUN=1 (needs web + API + seeded admin)");
  // Steps share state (draft survives reload only within one page) and one
  // emception boot; serial keeps ordering if more tests are added later.
  test.describe.configure({ mode: "serial" });

  test("student coding activity: tabs, draft, reset, results, full width", async ({
    page,
  }) => {
    // Emception boot (10-30s) + reload re-boot + compile/link/run (5-15s).
    test.setTimeout(240_000);
    page.setDefaultTimeout(60_000);
    page.setDefaultNavigationTimeout(180_000);

    await signInAsAdmin(page);

    // ── 1. Boot the student activity page. ──
    await page.goto(`${WEB_BASE_URL}${LEARN_ROUTE}`, {
      waitUntil: "domcontentloaded",
    });
    // The IDE boots lazily — wait for the skeleton to detach, then for the
    // toolchain to reach Ready before touching the workspace.
    await expect(page.getByTestId("ide-skeleton")).toBeHidden({
      timeout: 60_000,
    });
    // The coi-serviceworker reloads the page until it controls it. Wait for
    // cross-origin isolation so that cascade settles BEFORE we write drafts —
    // a mid-test reload would eat the localStorage write and fail the DRAFT
    // step. Once COI, the coi script's guard skips further reloads.
    await expect
      .poll(async () => page.evaluate(() => window.crossOriginIsolated), {
        timeout: 30_000,
      })
      .toBe(true);
    await expect(page.getByTestId("status").first()).toHaveText("Ready", {
      timeout: 60_000,
    });

    // ── 2. SELF-CONTAINED baseline: capture the instructor-saved main.cpp
    //    exactly once, before any edits. Reset must restore THIS content —
    //    no coupling to what coding-assessment-e2e may have saved earlier. ──
    const instructorMain = await readMainCpp(page);
    expect(instructorMain.length).toBeGreaterThan(0);

    // ── 3. TABS: Test Cases tab is rendered (fixture has ≥1 standard
    //    test) and opens the panel with at least one row. ──
    await page.getByRole("button", { name: "Test Cases", exact: true }).click();
    await expect(page.getByTestId("test-cases-panel")).toBeVisible();
    await expect(page.getByTestId("test-case-row").first()).toBeVisible();

    // ── 4. DRAFT: the marker survives a reload via localStorage. ──
    await setMainCpp(page, `${instructorMain}\n${DRAFT_MARKER}\n`);
    await expect
      .poll(async () => (await readMainCppRaw(page))?.includes(DRAFT_MARKER), {
        timeout: 30_000,
      })
      .toBe(true);
    await page.reload({ waitUntil: "domcontentloaded" });
    await expect(page.getByTestId("ide-skeleton")).toBeHidden({
      timeout: 60_000,
    });
    await expect
      .poll(async () => (await readMainCppRaw(page))?.includes(DRAFT_MARKER), {
        timeout: 90_000,
      })
      .toBe(true);

    // ── 5. RESET: accept the confirm; the editor must show the
    //    instructor-saved main.cpp again, without the marker. ──
    page.once("dialog", (dialog) => dialog.accept());
    await page.getByRole("button", { name: "Reset", exact: true }).click();
    await expect
      .poll(async () => (await readMainCppRaw(page)) === instructorMain, {
        timeout: 60_000,
      })
      .toBe(true);
    await expect
      .poll(async () => !(await readMainCppRaw(page))?.includes(DRAFT_MARKER), {
        timeout: 60_000,
      })
      .toBe(true);

    // ── 6. RESULTS: Run Tests; the results tab auto-activates and the
    //    report panel appears (fires on pass AND compile-error). ──
    const runTests = page.getByTestId("run-tests-button");
    await expect(runTests).toBeVisible();
    await expect(runTests).toBeEnabled();
    await runTests.click();
    await expect(page.getByTestId("test-results-slot")).toBeVisible({
      timeout: 90_000,
    });
    await expect(page.getByTestId("test-results-panel")).toBeVisible();

    // ── 7. WIDTH: the full-width mount spans the viewport. ──
    const viewport = page.viewportSize();
    const box = await page.getByTestId("ide-fullwidth-mount").boundingBox();
    expect(box).not.toBeNull();
    expect(box!.width).toBeGreaterThanOrEqual((viewport?.width ?? 1280) - 64);
  });
});

/** Sign in as the seeded admin (same helper as coding-assessment-e2e). */
async function signInAsAdmin(page: Page) {
  await page.goto(`${WEB_BASE_URL}/sign-in`, {
    waitUntil: "domcontentloaded",
  });
  // Wait for React hydration — filling pre-hydration silently no-ops.
  await page.waitForFunction(
    () => {
      if (document.readyState === "loading") return false;
      const controls = Array.from(
        document.querySelectorAll(
          "main button, main input, main textarea, form button, form input, form textarea",
        ),
      );
      return (
        controls.length > 0 &&
        controls.every((control) =>
          Object.keys(control).some((key) => key.startsWith("__reactProps$")),
        )
      );
    },
    undefined,
    { timeout: 30_000 },
  );
  await page.getByLabel("Email").fill(ADMIN_EMAIL);
  await page.getByLabel("Password").fill(ADMIN_PASSWORD);
  await page
    .getByRole("button", { name: "Sign in", exact: true })
    .click();
  await page.waitForURL("**/dashboard**", { timeout: 30_000 });
}

/** Read main.cpp content from the IDE's e2e files ref; undefined until mounted. */
async function readMainCppRaw(page: Page): Promise<string | undefined> {
  return page.evaluate(() => {
    const ref = (
      window as unknown as {
        __emception_filesRef__?: { current: Record<string, { content: string }> };
      }
    ).__emception_filesRef__;
    const entry = Object.entries(ref?.current ?? {}).find(([path]) =>
      path.endsWith("main.cpp"),
    );
    return entry?.[1]?.content;
  });
}

/** Poll until the IDE workspace has a main.cpp and return its content. */
async function readMainCpp(page: Page): Promise<string> {
  await expect
    .poll(async () => (await readMainCppRaw(page)) !== undefined)
    .toBe(true);
  const content = await readMainCppRaw(page);
  if (content === undefined) throw new Error("main.cpp missing from IDE state");
  return content;
}

/** Replace main.cpp through the IDE's purpose-built e2e hook. */
async function setMainCpp(page: Page, content: string) {
  const mainPath = await page.evaluate(() => {
    const ref = (
      window as unknown as {
        __emception_filesRef__?: { current: Record<string, { path: string }> };
      }
    ).__emception_filesRef__;
    return Object.keys(ref?.current ?? {}).find((p) => p.endsWith("main.cpp"));
  });
  if (!mainPath) throw new Error("main.cpp not found in IDE workspace state");
  await page.evaluate(
    ({ path, content }) => {
      (
        window as unknown as {
          __setFileContent: (p: string, c: string) => void;
        }
      ).__setFileContent(path, content);
    },
    { path: mainPath, content },
  );
}