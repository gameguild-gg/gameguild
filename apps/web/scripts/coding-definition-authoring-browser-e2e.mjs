#!/usr/bin/env node

/**
 * Task 15 — Instructor coding-definition authoring browser E2E.
 *
 * Flow (per plan L201 acceptance criteria):
 *   1. Sign in as system admin (has Review permission on every course).
 *   2. Bootstrap a course + an assessment via the API.
 *   3. Open the coding-definition authoring route.
 *   4. Pick C++ preset → assert the C++ sample template loads.
 *   5. Add 2 stdio test cases (one hidden) via the case builder UI.
 *   6. Click Save → assert the PUT /definition endpoint returns 200.
 *   7. Re-open the route → assert both cases round-trip with the hidden
 *      flag intact.
 *
 * Evidence: writes apps/web/test-results/coding-definition-authoring/task-15.log
 *
 * Run with:
 *   node apps/web/scripts/coding-definition-authoring-browser-e2e.mjs
 */

import { appendFile, mkdir } from "node:fs/promises";
import { resolve } from "node:path";
import { chromium } from "playwright";
import {
  assertSharedAuthCookie,
  trackAppHttpFailures,
} from "./learning-browser-e2e-support.mjs";

const apiBaseUrl = (
  process.env.API_BASE_URL ??
  process.env.NEXT_PUBLIC_API_URL ??
  "http://localhost:8080"
).replace(/\/$/, "");
const webBaseUrl = (
  process.env.PROFESSOR_E2E_BASE_URL ??
  process.env.NEXT_PUBLIC_APP_URL ??
  "http://gameguild.localhost:3011"
).replace(/\/$/, "");
const adminEmail = process.env.E2E_SYSTEM_ADMIN_EMAIL ?? "admin@game-guild.com";
const adminPassword =
  process.env.E2E_SYSTEM_ADMIN_PASSWORD ?? "Admin123!";
const headless = !["0", "false", "no"].includes(
  (process.env.CODING_DEF_E2E_HEADLESS ?? "true").toLowerCase(),
);
const evidenceDir = resolve(
  process.env.CODING_DEF_E2E_EVIDENCE_DIR ??
    "apps/web/test-results/coding-definition-authoring",
);

function unique() {
  return `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
}

function logStep(msg) {
  const line = `[coding-def-e2e ${new Date().toISOString()}] ${msg}`;
  console.log(line);
  return appendFile(resolve(evidenceDir, "task-15.log"), line + "\n").catch(
    () => {},
  );
}

async function apiRequest(path, init = {}, accessToken) {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers: {
      "content-type": "application/json",
      ...(accessToken ? { authorization: `Bearer ${accessToken}` } : {}),
      ...init.headers,
    },
  });
  const body =
    response.status === 204 ? null : await response.json().catch(() => null);
  if (!response.ok) {
    throw new Error(
      `${init.method ?? "GET"} ${path} failed with ${response.status}: ${JSON.stringify(body)}`,
    );
  }
  return body;
}

async function bootstrapCourseAndAssessment(accessToken) {
  const tag = unique();
  const courseSlug = `coding-def-${tag}`;
  // Create a minimal course via the same path the professor E2E uses.
  // ponytail: this is intentionally a thin shell — the rich course
  // creation flow is already exercised by learning-professor-browser-e2e.
  await apiRequest(
    "/v1/courses",
    {
      method: "POST",
      body: JSON.stringify({
        title: `Coding Definition E2E ${tag}`,
        slug: courseSlug,
        description: "Throwaway course for coding-definition E2E",
        estimatedHours: 1,
        skillsRequired: [],
        skillsProvided: [],
        maxEnrollments: 0,
      }),
    },
    accessToken,
  );

  const courseLookup = await apiRequest(
    `/v1/courses/slug/${encodeURIComponent(courseSlug)}`,
    {},
    accessToken,
  );
  const courseId = courseLookup.id;

  // Create a single assessment on this course.
  const assessment = await apiRequest(
    "/v1/assessments",
    {
      method: "POST",
      body: JSON.stringify({
        courseId,
        title: `Coding Assignment ${tag}`,
        type: "Assignment",
        maxScore: 100,
        passingScore: 60,
        isRequired: true,
      }),
    },
    accessToken,
  );
  const assessmentId = assessment.id;

  return { courseId, courseSlug, assessmentId, tag };
}

async function waitForClientHydration(page) {
  await page.waitForFunction(
    () => {
      if (document.readyState === "loading") return false;
      const controls = Array.from(
        document.querySelectorAll(
          'main button, main input, main textarea, main [role="combobox"]',
        ),
      );
      return controls.length > 0 && controls.every((control) =>
        Object.keys(control).some((key) => key.startsWith("__reactProps$")),
      );
    },
    undefined,
    { timeout: 60_000 },
  );
}

async function waitForLocation(page, predicate, timeout = 60_000) {
  const deadline = Date.now() + timeout;
  while (Date.now() < deadline) {
    const current = new URL(page.url());
    if (predicate(current)) return current;
    await page.waitForTimeout(100);
  }
  throw new Error(
    `Timed out waiting for location. Current URL: ${page.url()}`,
  );
}

async function signIn(page) {
  await page.goto(`${webBaseUrl}/sign-in`, { waitUntil: "domcontentloaded" });
  await waitForClientHydration(page);
  await page.getByLabel("Email").fill(adminEmail);
  await page.getByLabel("Password", { exact: true }).fill(adminPassword);
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await waitForLocation(page, (url) => url.pathname.includes("/dashboard"));
}

async function readPersistedDefinition(assessmentId, accessToken) {
  return apiRequest(
    `/v1.0/assessments/${assessmentId}/coding-definition/full`,
    {},
    accessToken,
  );
}

async function run() {
  await mkdir(evidenceDir, { recursive: true });
  await logStep("sign-in as system admin");
  const signInResp = await apiRequest("/v1/auth/sign-in", {
    method: "POST",
    body: JSON.stringify({ email: adminEmail, password: adminPassword }),
  });
  const accessToken = signInResp.accessToken;

  await logStep("bootstrap course + assessment");
  const fixture = await bootstrapCourseAndAssessment(accessToken);
  await logStep(
    `created course=${fixture.courseSlug} assessment=${fixture.assessmentId}`,
  );

  const browser = await chromium.launch({ headless });
  const context = await browser.newContext({
    viewport: { width: 1440, height: 1000 },
  });
  const page = await context.newPage();
  const httpFailures = trackAppHttpFailures(page, [webBaseUrl]);
  const runtimeErrors = [];
  page.on("pageerror", (e) => runtimeErrors.push(e.message));
  page.on("console", (m) => {
    if (m.type() === "error" && !/favicon|cloudflareinsights/i.test(m.text())) {
      runtimeErrors.push(m.text());
    }
  });

  try {
    page.setDefaultTimeout(60_000);
    page.setDefaultNavigationTimeout(180_000);

    await signIn(page);
    assertSharedAuthCookie(
      await context.cookies([webBaseUrl]),
      undefined,
    );

    const route = `/dashboard/learning/courses/${fixture.courseId}/assessments/${fixture.assessmentId}/coding-definition`;
    await logStep(`open coding-definition route: ${route}`);
    await page.goto(`${webBaseUrl}${route}`, { waitUntil: "domcontentloaded" });
    await waitForClientHydration(page);
    await page.getByText("Coding Definition Editor").waitFor();
    await page.getByTestId("language-select").waitFor();

    // Step 4: Pick C++. The editor defaults to cpp, but we switch away and
    // back to force the change handler to seed the sample template.
    await logStep("pick C++ preset (seed ASSIGNMENT_SAMPLES.cpp)");
    await page.getByTestId("language-select").click();
    // Pick a different language first to force a transition.
    await page.getByRole("option", { name: "C (clang + WASI)" }).click();
    await page.waitForTimeout(200);
    await page.getByTestId("language-select").click();
    await page.getByRole("option", { name: "C++ (clang + WASI)" }).click();
    // The C++ sample contains a "echo stdin" stdio case — its appearance
    // in the preview proves the sample template loaded.
    const preview = page.getByTestId("json-preview");
    await preview.waitFor();
    await page.waitForFunction(
      () => /"id": "cpp"/.test(
        document.querySelector('[data-testid="json-preview"]')?.textContent ??
          "",
      ),
      undefined,
      { timeout: 30_000 },
    );

    // Step 5: Add 2 stdio cases (one hidden). Clear existing cases first
    // so the assertion count is deterministic.
    await logStep("clear seeded cases + add 2 stdio cases (one hidden)");
    let removeButtons = await page
      .locator('[data-testid^="remove-case-"]')
      .all();
    for (const btn of removeButtons) {
      await btn.click();
      await page.waitForTimeout(50);
    }

    await page.getByTestId("add-case-stdio").click();
    await page.getByTestId("add-case-stdio").click();

    // Case 0 — visible.
    await page.getByTestId("case-stdin-0").fill("hello");
    await page.getByTestId("case-expectedStdout-0").fill("hello");

    // Case 1 — hidden.
    await page.getByTestId("case-stdin-1").fill("secret");
    await page.getByTestId("case-expectedStdout-1").fill("secret");
    await page.getByTestId("case-hidden-1").click();

    // Wait for preview to reflect the authored cases.
    await page.waitForFunction(
      () => {
        const text =
          document.querySelector('[data-testid="json-preview"]')?.textContent ??
          "";
        return (
          text.includes('"stdin": "hello"') &&
          text.includes('"stdin": "secret"') &&
          text.includes('"hidden": true')
        );
      },
      undefined,
      { timeout: 30_000 },
    );
    await logStep("preview reflects 2 stdio cases with hidden flag on case 1");

    // Step 6: Save.
    await logStep("click Save");
    const saveButton = page.getByTestId("save-button");
    await saveButton.waitFor({ state: "visible" });
    // The save button should be enabled (form is valid).
    await page.waitForFunction(
      () => {
        const btn = document.querySelector('[data-testid="save-button"]');
        return btn && !btn.disabled;
      },
      undefined,
      { timeout: 30_000 },
    );
    await saveButton.click();
    // Editor redirects back to the assessment page on success.
    await waitForLocation(
      page,
      (url) =>
        url.pathname.endsWith(
          `/assessments/${fixture.assessmentId}`,
        ) && !url.pathname.includes("coding-definition"),
      90_000,
    );
    await logStep("save succeeded; redirected to assessment editor");

    // Step 7: Re-open the route and assert round-trip.
    await logStep("re-open coding-definition route");
    await page.goto(`${webBaseUrl}${route}`, {
      waitUntil: "domcontentloaded",
    });
    await waitForClientHydration(page);
    await page.getByText("Coding Definition Editor").waitFor();
    await page.getByTestId("json-preview").waitFor();

    // Assert the persisted cases round-tripped via the API (source of truth).
    const persisted = await readPersistedDefinition(
      fixture.assessmentId,
      accessToken,
    );
    const cases = persisted?.testPlan?.cases ?? [];
    if (cases.length !== 2) {
      throw new Error(
        `Round-trip failed: expected 2 cases, got ${cases.length}`,
      );
    }
    const hiddenCount = cases.filter((c) => c.hidden === true).length;
    if (hiddenCount !== 1) {
      throw new Error(
        `Round-trip failed: expected 1 hidden case, got ${hiddenCount}`,
      );
    }
    const stdinValues = cases.map((c) => c.stdin).sort();
    if (stdinValues.join(",") !== "hello,secret") {
      throw new Error(
        `Round-trip failed: stdin values mismatch — ${JSON.stringify(stdinValues)}`,
      );
    }
    await logStep("round-trip verified: 2 cases, 1 hidden, stdin preserved");

    httpFailures.assertNone("Coding definition authoring E2E");
    if (runtimeErrors.length > 0) {
      throw new Error(
        `Browser runtime errors detected:\n${[...new Set(runtimeErrors)].join("\n")}`,
      );
    }

    await page.screenshot({
      path: resolve(evidenceDir, "task-15.png"),
      fullPage: true,
    });
    await logStep("TASK-15 PASSED");
  } finally {
    await browser.close();
  }
}

run().catch(async (error) => {
  await logStep(`TASK-15 FAILED: ${error?.message ?? error}`).catch(() => {});
  console.error(
    error instanceof Error ? (error.stack ?? error.message) : error,
  );
  process.exit(1);
});
