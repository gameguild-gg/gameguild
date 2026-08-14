/**
 * Coding assessment — REAL end-to-end Playwright spec.
 *
 * Boots the emception toolchain in a real browser, authors a hello program
 * in main.cpp, adds a standard stdin/stdout test, clicks "Run Tests", and
 * verifies the compiled WASI binary passes the test inside the IDE.
 *
 * Gated behind E2E_RUN so CI stays green without a live stack — NOT
 * permanently skipped like coding-assessment-authoring.spec.ts.
 *
 * Run manually (needs web + API + seeded admin on localhost:3000):
 *   cd apps/web && E2E_RUN=1 pnpm playwright test coding-assessment-e2e --reporter=line
 *
 * Selectors verified against:
 *   packages/infrastructure/ui-emception/src/components/{Ide,TestResultsPanel}.tsx
 *   src/.../coding-definition/{coding-definition-editor,standard-test-editor}.tsx
 *
 * Note: this project installs `playwright` (not `@playwright/test`); both
 * expose the same `test` / `expect` runtime, but the import must match the
 * installed package.
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

// Known fixture from the authoring URL. Override per-env if needed.
const COURSE_ID = process.env.E2E_COURSE_ID ?? "ai4games-by-gameguild";
const ASSESSMENT_ID =
  process.env.E2E_ASSESSMENT_ID ?? "f0a08827-faa0-4ada-beb0-6fc61c731ccb";

const CODING_DEFINITION_ROUTE = `/dashboard/learning/courses/${COURSE_ID}/assessments/${ASSESSMENT_ID}/coding-definition`;

const HELLO_PROGRAM =
  "#include <iostream>\n" +
  "#include <string>\n" +
  "int main() {\n" +
  "    std::string line;\n" +
  "    std::getline(std::cin, line);\n" +
  '    std::cout << "hello " << line << std::endl;\n' +
  "    return 0;\n" +
  "}\n";

// Functional-test program: only a function, no main — doctest supplies main.
const ADD_PROGRAM = "int add(int a, int b) {\n    return a + b;\n}\n";

// SDL3 sample — verbatim from ui-emception assignment-samples.ts (sdl-cpp preset).
const SDL3_PROGRAM = `// SDL3 graphics starter — compile with emcc -sUSE_SDL=3
#define SDL_MAIN_USE_CALLBACKS
#include <SDL3/SDL.h>
#include <SDL3/SDL_main.h>

static SDL_Window   *window   = NULL;
static SDL_Renderer *renderer = NULL;

SDL_AppResult SDL_AppInit(void **appstate, int argc, char *argv[]) {
  SDL_Init(SDL_INIT_VIDEO);
  SDL_CreateWindowAndRenderer("SDL3 Assignment", 640, 480, 0, &window, &renderer);
  return SDL_APP_CONTINUE;
}

SDL_AppResult SDL_AppIterate(void *appstate) {
  SDL_SetRenderDrawColor(renderer, 30, 30, 45, 255);
  SDL_RenderClear(renderer);
  SDL_RenderPresent(renderer);
  return SDL_APP_CONTINUE;
}

SDL_AppResult SDL_AppEvent(void *appstate, SDL_Event *event) {
  return event->type == SDL_EVENT_QUIT ? SDL_APP_SUCCESS : SDL_APP_CONTINUE;
}

void SDL_AppQuit(void *appstate, SDL_AppResult result) {
  SDL_DestroyRenderer(renderer);
  SDL_DestroyWindow(window);
  SDL_Quit();
}
`;

// raylib sample — verbatim from ui-emception assignment-samples.ts (raylib-cpp preset).
const RAYLIB_PROGRAM = `// raylib graphics starter
#include "raylib.h"

int main(void) {
  InitWindow(640, 480, "raylib Assignment");
  SetTargetFPS(60);
  while (!WindowShouldClose()) {
    BeginDrawing();
    ClearBackground((Color){30, 30, 45, 255});
    DrawText("Hello raylib", 240, 220, 24, RAYWHITE);
    EndDrawing();
  }
  CloseWindow();
  return 0;
}
`;

test.describe("Coding Assessment E2E", () => {
  // Skip unless explicitly invoked with E2E_RUN=1 — runnable, but CI-safe.
  test.skip(!process.env.E2E_RUN, "set E2E_RUN=1 (needs web + API + seeded admin)");
  // Emception boot (10-30s) + compile/link/run (5-15s) needs a long leash.
  test.describe.configure({ timeout: 120_000 });

  test.beforeEach(async ({ page }) => {
    page.setDefaultTimeout(60_000);
    page.setDefaultNavigationTimeout(180_000);

    // 1. Sign in as admin.
    await page.goto(`${WEB_BASE_URL}/sign-in`, {
      waitUntil: "domcontentloaded",
    });
    // Wait for React hydration — filling pre-hydration silently no-ops
    // (same trick as scripts/coding-definition-authoring-browser-e2e.mjs).
    await page.waitForFunction(
      () => {
        if (document.readyState === "loading") return false;
        const controls = Array.from(
          document.querySelectorAll(
            'main button, main input, main textarea, form button, form input, form textarea',
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
  });

  test("standard stdin/stdout test passes end-to-end", async ({ page }) => {
    // 2. Open the coding-definition editor.
    await page.goto(`${WEB_BASE_URL}${CODING_DEFINITION_ROUTE}`, {
      waitUntil: "domcontentloaded",
    });
    await expect(
      page.getByText("Coding Definition Editor"),
    ).toBeVisible({ timeout: 30_000 });

    // 3. Wait for emception to finish booting — the IDE header status pill
    //    goes "Initializing..." → "Booting toolchain..." → "Ready".
    await expect(page.getByTestId("status").first()).toHaveText("Ready", {
      timeout: 60_000,
    });
    await page.waitForTimeout(1_000); // small buffer after ready

    // 4. Replace main.cpp with the hello + getline program. keyboard.type
    //    drops characters in Monaco at speed; the IDE exposes a purpose-built
    //    e2e hook (window.__setFileContent) that updates the same React state
    //    handleRunTests reads.
    await page
      .getByText("main.cpp")
      .first()
      .click()
      .catch(() => {
        // File-tree label may differ in some IDE skins; fall through below.
      });

    const mainPath = await page.evaluate(() => {
      const ref = (
        window as unknown as {
          __emception_filesRef__?: { current: Record<string, { path: string }> };
        }
      ).__emception_filesRef__;
      return Object.keys(ref?.current ?? {}).find((p) => p.endsWith("main.cpp"));
    });
    const setFileContent = (path: string, content: string) =>
      page.evaluate(
        ({ path, content }) => {
          (
            window as unknown as {
              __setFileContent: (p: string, c: string) => void;
            }
          ).__setFileContent(path, content);
        },
        { path, content },
      );
    if (!mainPath) throw new Error("main.cpp not found in IDE workspace state");
    await setFileContent(mainPath, HELLO_PROGRAM);
    await page.waitForTimeout(500);

    // 5. Open the Tests tab in the bottom panel (hosts the authoring slot).
    await page
      .getByRole("button", { name: "Tests", exact: true })
      .click();
    await expect(page.getByTestId("tests-panel-slot")).toBeVisible();

    // 6. Clear any seeded test rows.
    await removeAllTestRows(page);

    // 7. Add a standard test: stdin "world" → stdout "hello world".
    await page.getByTestId("add-standard").click();
    await page.getByTestId("standard-stdin-0").fill("world");
    await page.getByTestId("standard-stdout-0").fill("hello world");

    // 8. Run Tests — the button only renders once a test plan exists.
    const runTests = page.getByTestId("run-tests-button");
    await expect(runTests).toBeVisible();
    await expect(runTests).toBeEnabled();
    await runTests.click();

    // 9. Wait for results — compile + link + wasi-run takes 5-15s.
    const results = page.getByTestId("test-results-panel");
    await expect(results).toBeVisible({ timeout: 90_000 });

    // 10. Verify the test passed.
    await expect(results).toContainText("1 passed");
    await expect(results).toContainText("0 failed");
    await expect(page.getByTestId("test-case-0")).toContainText("✓");
  });

  test("functional test (doctest) passes end-to-end", async ({ page }) => {
    // 2. Open the coding-definition editor + wait for emception boot.
    await page.goto(`${WEB_BASE_URL}${CODING_DEFINITION_ROUTE}`, {
      waitUntil: "domcontentloaded",
    });
    await expect(
      page.getByText("Coding Definition Editor"),
    ).toBeVisible({ timeout: 30_000 });
    await expect(page.getByTestId("status").first()).toHaveText("Ready", {
      timeout: 60_000,
    });
    await page.waitForTimeout(1_000);

    // 3. Replace main.cpp with the main()-less add() function.
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
      { path: mainPath, content: ADD_PROGRAM },
    );
    await page.waitForTimeout(500);

    // 4. Tests tab + clear seeded rows.
    await page.getByRole("button", { name: "Tests", exact: true }).click();
    await expect(page.getByTestId("tests-panel-slot")).toBeVisible();
    await removeAllTestRows(page);

    // 5. Author a functional test: int add(int a, int b), case add(2,3)==5.
    //    Param type defaults to String — must select Integer explicitly;
    //    return type defaults to Integer (handleAddFunctional).
    await page.getByTestId("add-functional").click();
    await page.getByTestId("functional-functionName-0").fill("add");
    await page.getByTestId("functional-add-param-0").click();
    await page.getByTestId("functional-add-param-0").click();
    await page.getByTestId("functional-param-name-0-0").fill("a");
    await selectRadixOption(page, "functional-param-type-0-0", "Integer");
    await page.getByTestId("functional-param-name-0-1").fill("b");
    await selectRadixOption(page, "functional-param-type-0-1", "Integer");
    await page.getByTestId("functional-add-case-0").click();
    await page.getByTestId("functional-case-input-value-0-0-0").fill("2");
    await page.getByTestId("functional-case-input-value-0-0-1").fill("3");
    await page.getByTestId("functional-case-expected-value-0-0").fill("5");

    // 6. Run Tests.
    const runTests = page.getByTestId("run-tests-button");
    await expect(runTests).toBeVisible();
    await expect(runTests).toBeEnabled();
    await runTests.click();

    // 7. Wait for results — combined-TU compile + link + wasi-run.
    const results = page.getByTestId("test-results-panel");
    await expect(results).toBeVisible({ timeout: 90_000 });

    // 8. Verify the doctest case passed.
    await expect(results).toContainText("1 passed");
    await expect(results).toContainText("0 failed");
  });

  test("SDL3 canvas program renders end-to-end", async ({ page }) => {
    test.setTimeout(240_000);
    const consoleErrors: string[] = [];
    page.on("console", (msg) => {
      if (msg.type() === "error") consoleErrors.push(msg.text());
    });
    page.on("pageerror", (err) => consoleErrors.push(String(err)));

    // 2. Open the coding-definition editor + wait for emception boot.
    await page.goto(`${WEB_BASE_URL}${CODING_DEFINITION_ROUTE}`, {
      waitUntil: "domcontentloaded",
    });
    await expect(
      page.getByText("Coding Definition Editor"),
    ).toBeVisible({ timeout: 30_000 });
    await expect(page.getByTestId("status").first()).toHaveText("Ready", {
      timeout: 60_000,
    });

    // 3. Switch preset to SDL3 — the page re-seeds the workspace (file tree,
    //    tabs, config) via onPresetChange → ASSIGNMENT_SAMPLES["sdl-cpp"].
    await page.getByTestId("workspace-picker").selectOption("sdl-cpp");
    await expect(page.getByText("sdl-main.cpp").first()).toBeVisible({
      timeout: 15_000,
    });
    await page.waitForTimeout(1_000); // let applyWorkspace sync the VFS

    // 4. Force the SDL3 starter content (guards against stale localStorage).
    const sdlPath = await page.evaluate(() => {
      const ref = (
        window as unknown as {
          __emception_filesRef__?: { current: Record<string, { path: string }> };
        }
      ).__emception_filesRef__;
      return Object.keys(ref?.current ?? {}).find((p) =>
        p.endsWith("sdl-main.cpp"),
      );
    });
    if (!sdlPath) throw new Error("sdl-main.cpp not found in IDE workspace state");
    await page.evaluate(
      ({ path, content }) => {
        (
          window as unknown as {
            __setFileContent: (p: string, c: string) => void;
          }
        ).__setFileContent(path, content);
      },
      { path: sdlPath, content: SDL3_PROGRAM },
    );
    await page.waitForTimeout(500);

    // 5. ▶ Compile & Run — bundle download + clang + wasm-ld + instantiate.
    await page.getByTestId("compile-button").click();

    // 6. Success marker: only the happy path writes this line. Cold CDN bundle
    //    fetch + compile + link + instantiate needs the long leash.
    const terminalLog = page.getByTestId("terminal");
    await expect(terminalLog).toContainText("rendering in canvas tab", {
      timeout: 90_000,
    });

    // 7. Canvas tab active + running state surfaced.
    await expect
      .poll(() =>
        page
          .getByTestId("sdl-canvas")
          .evaluate((el) => el.dataset.sdlRunning ?? ""),
      )
      .toBe("true");
    await expect(page.getByTestId("status").first()).toContainText("running");

    // 8. No LinkError (the _abort_js import bug), no failed instantiation.
    expect(consoleErrors.filter((e) => e.includes("LinkError"))).toEqual([]);
    await expect(terminalLog).not.toContainText("instantiation failed");
    await expect(terminalLog).not.toContainText("compile step failed");
    await expect(terminalLog).not.toContainText("link step failed");
  });

  test("raylib canvas program renders end-to-end", async ({ page }) => {
    test.setTimeout(240_000);
    const consoleErrors: string[] = [];
    page.on("console", (msg) => {
      if (msg.type() === "error") consoleErrors.push(msg.text());
    });
    page.on("pageerror", (err) => consoleErrors.push(String(err)));

    // 2. Open the coding-definition editor + wait for emception boot.
    await page.goto(`${WEB_BASE_URL}${CODING_DEFINITION_ROUTE}`, {
      waitUntil: "domcontentloaded",
    });
    await expect(
      page.getByText("Coding Definition Editor"),
    ).toBeVisible({ timeout: 30_000 });
    await expect(page.getByTestId("status").first()).toHaveText("Ready", {
      timeout: 60_000,
    });

    // 3. Switch preset to raylib — page re-seeds workspace from sample.
    await page.getByTestId("workspace-picker").selectOption("raylib-cpp");
    await expect(page.getByText("raylib-main.cpp").first()).toBeVisible({
      timeout: 15_000,
    });
    await page.waitForTimeout(1_000); // let applyWorkspace sync the VFS

    // 4. Force the raylib starter content (guards against stale localStorage).
    const raylibPath = await page.evaluate(() => {
      const ref = (
        window as unknown as {
          __emception_filesRef__?: { current: Record<string, { path: string }> };
        }
      ).__emception_filesRef__;
      return Object.keys(ref?.current ?? {}).find((p) =>
        p.endsWith("raylib-main.cpp"),
      );
    });
    if (!raylibPath) {
      throw new Error("raylib-main.cpp not found in IDE workspace state");
    }
    await page.evaluate(
      ({ path, content }) => {
        (
          window as unknown as {
            __setFileContent: (p: string, c: string) => void;
          }
        ).__setFileContent(path, content);
      },
      { path: raylibPath, content: RAYLIB_PROGRAM },
    );
    await page.waitForTimeout(500);

    // 5. ▶ Compile & Run.
    await page.getByTestId("compile-button").click();

    // 6. Success marker (same reasoning as the SDL3 test).
    const terminalLog = page.getByTestId("terminal");
    await expect(terminalLog).toContainText("rendering in canvas tab", {
      timeout: 90_000,
    });

    // 7. Canvas tab active + running state surfaced.
    await expect
      .poll(() =>
        page
          .getByTestId("sdl-canvas")
          .evaluate((el) => el.dataset.sdlRunning ?? ""),
      )
      .toBe("true");
    await expect(page.getByTestId("status").first()).toContainText("running");

    // 8. Header materialized (no 'raylib.h' file not found), clean link.
    await expect(terminalLog).not.toContainText("raylib.h' file not found");
    await expect(terminalLog).not.toContainText("compile step failed");
    await expect(terminalLog).not.toContainText("link step failed");
    expect(consoleErrors.filter((e) => e.includes("LinkError"))).toEqual([]);
  });
});

/**
 * Remove every standard + functional test row currently rendered.
 * ponytail: indexes reshuffle after each remove, so always re-query and
 * click the first available remove button until none remain.
 */
async function removeAllTestRows(page: Page) {
  for (;;) {
    const next = page
      .locator('[data-testid^="standard-remove-"], [data-testid^="functional-remove-"]')
      .first();
    if (!(await next.isVisible().catch(() => false))) return;
    await next.click().catch(() => {});
    await page.waitForTimeout(30);
  }
}

/** Open a radix Select by trigger testid and pick the option by label. */
async function selectRadixOption(
  page: Page,
  triggerTestId: string,
  label: string,
) {
  await page.getByTestId(triggerTestId).click();
  await page.getByRole("option", { name: label, exact: true }).click();
}
