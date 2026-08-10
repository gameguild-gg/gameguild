#!/usr/bin/env node
// @ts-check
/**
 * Browser E2E for the coding-activity page (Task 11).
 *
 * Follows apps/web/scripts/*-browser-e2e.mjs convention: a standalone node
 * script that launches playwright as a library (no playwright.config).
 *
 * The Next.js app must be running (pnpm --filter @game-guild/web dev). The
 * backend API is mocked via page.route() so the script can run without a real
 * .NET stack — auth, learner context, coding definition, and submission
 * endpoints all return canned fixtures.
 *
 * Set CODING_ACTIVITY_E2E_URL to override the activity URL (defaults to the
 * fixture course/slug below).
 */

import { mkdir } from 'node:fs/promises';
import { resolve } from 'node:path';
import { chromium } from 'playwright';

const baseUrl = (
  process.env.PUBLIC_E2E_BASE_URL ??
  process.env.NEXT_PUBLIC_APP_URL ??
  'http://localhost:3011'
).replace(/\/$/, '');
const activityUrl = (
  process.env.CODING_ACTIVITY_E2E_URL ??
  `${baseUrl}/en/learn/courses/coding-fixture-course/activities/assessment-coding-fixture-1`
).replace(/\/$/, '');
const evidenceDir = resolve(
  process.env.CODING_ACTIVITY_E2E_EVIDENCE_DIR ??
    'apps/web/test-results/coding-activity',
);

const CODING_DEF = {
  kind: 'coding',
  language: 'cpp',
  workspaceConfig: {
    files: {
      'main.cpp': { encoding: 'text', content: '#include <iostream>\nint main(){ std::cout << 1; }' },
    },
    features: { canvas: false, terminalInput: true, showTestButton: true },
  },
  testPlan: {
    cases: [
      {
        kind: 'stdio',
        name: 'prints-one',
        stdin: '',
        expectedStdout: '1',
        hidden: false,
        weight: 1,
      },
    ],
  },
  maxScore: 100,
  passingScore: 60,
};

async function mockApi(page) {
  // Auth/session: serve a no-op next-auth session so the page treats the
  // visitor as signed-in.
  await page.route('**/api/auth/**', async (route) => {
    if (route.request().url().includes('session')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ user: { id: 'fixture-user' } }),
      });
    }
    return route.continue();
  });

  // Coding-definition public endpoint.
  await page.route('**/v1.0/assessments/**/coding-definition/public', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(CODING_DEF),
    }),
  );

  // Assessment submission lifecycle. The student page calls the generated
  // client which wraps these endpoints.
  await page.route('**/v1.0/assessments/my-submissions**', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ items: [] }),
    }),
  );
  await page.route('**/v1.0/assessments/submissions/start**', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        submission: { id: 'fixture-submission-1', status: 'InProgress' },
      }),
    }),
  );

  // Capture the submit POST so we can assert codePayload shape.
  /** @type {Record<string, unknown> | null} */
  let submitPayload = null;
  await page.route('**/v1.0/assessments/submissions/*/submit', async (route) => {
    const request = route.request();
    try {
      const body = request.postData();
      submitPayload = body ? JSON.parse(body) : null;
    } catch {
      submitPayload = null;
    }
    return route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ id: 'fixture-submission-1', status: 'Submitted' }),
    });
  });

  // Learner context (course access data + my records) — the page calls these
  // to find the assessment. We hand back a single coding assessment.
  await page.route('**/v1.0/workspaces/learner/**', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        enrollmentId: 'fixture-enrollment-1',
        course: {
          id: 'coding-fixture-course',
          slug: 'coding-fixture-course',
          title: 'Coding Fixture',
          modules: [],
        },
        assessments: [
          {
            assessmentId: 'coding-fixture-1',
            courseId: 'coding-fixture-course',
            title: 'Coding Fixture Assessment',
            description: 'Write code that prints 1.',
            type: 'Assignment',
            submissionModalities: 'Code',
            maxScore: 100,
            passingScore: 60,
          },
        ],
        submissions: [],
      }),
    }),
  );

  return {
    getSubmitPayload: () => submitPayload,
  };
}

async function mockEmceptionWorker(page) {
  // The IDE bootstraps a web worker; intercept its manifest + the worker
  // script so the page never tries to fetch the real wasm bundle.
  await page.route('**/emception/manifest.json', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ tools: [], presets: [] }),
    }),
  );
  await page.route('**/emception/**', (route) =>
    route.fulfill({ status: 200, contentType: 'text/plain', body: '' }),
  );
}

async function assertNoErrorSurface(page, label) {
  await page.waitForLoadState('domcontentloaded');
  const body = await page.locator('body').innerText();
  if (/404|page not found|Unhandled Runtime Error|Build Error|Application error|Internal server error/i.test(body)) {
    throw new Error(`${label} rendered an error surface:\n${body.slice(0, 1500)}`);
  }
}

async function run() {
  await mkdir(evidenceDir, { recursive: true });
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  const page = await context.newPage();
  page.setDefaultTimeout(45_000);
  page.setDefaultNavigationTimeout(90_000);

  const runtimeErrors = [];
  page.on('pageerror', (error) => runtimeErrors.push(error.message));
  page.on('console', (message) => {
    if (message.type() === 'error' && !/favicon|cloudflareinsights/i.test(message.text())) {
      runtimeErrors.push(message.text());
    }
  });

  const api = await mockApi(page);
  await mockEmceptionWorker(page);

  try {
    await page.goto(activityUrl, { waitUntil: 'domcontentloaded' });
    await assertNoErrorSurface(page, 'Coding activity');

    // Wait for either the IDE skeleton (loading) or the IDE itself.
    await page
      .locator('[data-testid="ide-skeleton"], [data-testid="run-tests-button"], canvas, [class*="monaco"]')
      .first()
      .waitFor({ timeout: 45_000 });
    await page.screenshot({ path: resolve(evidenceDir, 'ide-mounted.png'), fullPage: true });

    // Click Run Tests if the button exists (worker is mocked, may not fire —
    // assertion is best-effort).
    const runTests = page.getByRole('button', { name: /Run Tests/i });
    if (await runTests.count()) {
      await runTests.click().catch(() => {});
    }

    // Submit must always be available on the coding path.
    const submit = page.getByRole('button', { name: /^Submit$/ });
    await submit.waitFor({ timeout: 30_000 });
    await submit.click();

    // The Next.js server action fires fetch to the submit endpoint; assert it
    // received codePayload with the fixture file.
    await page.waitForFunction(
      () => !!window,
      undefined,
      { timeout: 10_000 },
    ).catch(() => {});
    // Give the mocked route handler a beat to record the payload.
    await page.waitForTimeout(2_000);
    const payload = api.getSubmitPayload();
    if (!payload || typeof payload !== 'object') {
      throw new Error('Submit was not invoked: no codePayload captured');
    }
    if (typeof payload.codePayload !== 'string') {
      throw new Error(`Submit payload missing codePayload: ${JSON.stringify(payload).slice(0, 300)}`);
    }
    const parsed = JSON.parse(payload.codePayload);
    if (!parsed['main.cpp'] || typeof parsed['main.cpp'] !== 'string') {
      throw new Error(`codePayload missing main.cpp: ${payload.codePayload.slice(0, 200)}`);
    }

    await page.screenshot({ path: resolve(evidenceDir, 'submit-done.png'), fullPage: true });

    if (runtimeErrors.length) {
      throw new Error(`Browser runtime errors:\n${runtimeErrors.join('\n')}`);
    }

    console.log(
      `Coding activity E2E passed at ${activityUrl} — codePayload captured with keys: ${Object.keys(parsed).join(', ')}`,
    );
  } finally {
    await browser.close();
  }
}

run().catch((error) => {
  console.error(error instanceof Error ? (error.stack ?? error.message) : error);
  process.exit(1);
});
