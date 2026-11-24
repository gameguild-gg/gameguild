import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { createHash, randomUUID } from 'node:crypto';
import { mkdir, mkdtemp, readFile, writeFile } from 'node:fs/promises';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { isDeepStrictEqual } from 'node:util';
import { chromium, expect, type Browser, type BrowserContext, type Page } from 'playwright/test';
import { assertSharedAuthCookie, assertNoHorizontalOverflow, classifyAppHttpFailure, waitForInteractiveCredentials } from './learning-browser-e2e-support.mjs';
import type {
  AuthoringHostCaseEvidence,
  AuthoringHostCaseId,
  AuthoringHostEnvironment,
  AuthoringHostEvidence,
  AuthoringHostVerifier,
} from './coding-authoring-browser-contracts.mjs';

type Family = 'function' | 'standard-io';

interface ApiContext {
  readonly baseUrl: string;
  readonly accessToken?: string;
  readonly tenantId?: string;
}

interface Instructor {
  readonly client: ApiContext;
  readonly email: string;
  readonly password: string;
  readonly userId: string;
}

interface Lesson {
  readonly id: string;
  readonly slug: string;
  readonly title: string;
  readonly family: Family;
}

interface ObservedDraft {
  readonly revision: number;
  readonly basePublishedVersion: number;
  readonly payload: Record<string, unknown>;
}

interface HostReport extends AuthoringHostEvidence {
  cases: AuthoringHostCaseEvidence[];
  compilerRequests: number;
  academicBrowserRequests: number;
  cleanupErrors: string[];
  errors: string[];
  readonly commit: string;
  readonly environment: AuthoringHostEnvironment;
  readonly hashes: Record<string, string>;
  readonly screenshots: string[];
  readonly accountLifecycle: 'sign-up-only-no-account-deletion';
  passed: boolean;
}

const root = fileURLToPath(new URL('../../../', import.meta.url));

function isRecord(value: unknown): value is Record<string, unknown> {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
}

function record(value: unknown, label: string): Record<string, unknown> {
  if (!isRecord(value)) throw new TypeError(`${label} must be an object.`);
  return value;
}

function text(value: unknown, label: string): string {
  if (typeof value !== 'string' || !value.trim()) throw new TypeError(`${label} must be a nonempty string.`);
  return value;
}

function integer(value: unknown, label: string): number {
  if (typeof value !== 'number' || !Number.isSafeInteger(value) || value < 0) throw new TypeError(`${label} must be a nonnegative integer.`);
  return value;
}

function objects(value: unknown, label: string): Record<string, unknown>[] {
  if (!Array.isArray(value)) throw new TypeError(`${label} must be an array.`);
  const values: readonly unknown[] = value;
  return values.map((entry) => record(entry, label));
}

async function apiRequest(client: ApiContext, path: string, method: 'GET' | 'PUT' | 'POST' | 'DELETE' = 'GET', body?: unknown): Promise<unknown> {
  const response = await fetch(`${client.baseUrl}${path}`, {
    method,
    redirect: 'error',
    signal: AbortSignal.timeout(30_000),
    headers: {
      ...(client.accessToken ? { Authorization: `Bearer ${client.accessToken}` } : {}),
      ...(client.tenantId ? { 'X-Tenant-Id': client.tenantId } : {}),
      ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
    },
    ...(body === undefined ? {} : { body: JSON.stringify(body) }),
  });
  if (!response.ok) throw new Error(`Authoring proof API ${method}: HTTP ${response.status}.`);
  return response.status === 204 ? null : response.json();
}

async function apiRecord(client: ApiContext, path: string, method: 'GET' | 'PUT' | 'POST' = 'GET', body?: unknown): Promise<Record<string, unknown>> {
  return record(await apiRequest(client, path, method, body), 'API response');
}

function authoringPath(courseId: string, contentId: string): string {
  return `/v1/courses/${encodeURIComponent(courseId)}/content/${encodeURIComponent(contentId)}/authoring`;
}

async function readDraft(client: ApiContext, courseId: string, lesson: Lesson): Promise<ObservedDraft> {
  const draft = await apiRecord(client, authoringPath(courseId, lesson.id));
  assert.ok(draft.programId === courseId && draft.contentId === lesson.id, 'The API returned another lesson scope.');
  return {
    revision: integer(draft.revision, 'Draft revision'),
    basePublishedVersion: integer(draft.basePublishedVersion, 'Publication base'),
    payload: record(draft.payload, 'Draft payload'),
  };
}

function assertMemberToken(token: string): void {
  const encoded = token.split('.')[1];
  assert.ok(encoded, 'Sign-in did not return a JWT.');
  const claims = record(JSON.parse(Buffer.from(encoded, 'base64url').toString('utf8')), 'Token claims');
  const claim = claims.role ?? claims.roles ?? claims['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
  const values: readonly unknown[] = Array.isArray(claim) ? claim : [claim];
  const roles = values.filter((value): value is string => typeof value === 'string');
  assert.ok(roles.includes('Member') && !roles.some((role) => /admin/i.test(role)), 'The instructor must be a non-admin Member.');
}

async function createInstructor(environment: AuthoringHostEnvironment, nonce: string): Promise<Instructor> {
  const email = `ca-${nonce}@qgs.test`;
  const password = `Ca!1${randomUUID()}`;
  const anonymous = { baseUrl: environment.apiBaseUrl };
  await apiRecord(anonymous, '/v1/auth/sign-up', 'POST', { email, username: `ca-${nonce}`, password });
  const identity = await apiRecord(anonymous, '/v1/auth/sign-in', 'POST', { email, password });
  assert.ok(!identity.requiresMfa && !identity.requiresStepUp, 'The isolated Member requires an unsupported authentication continuation.');
  const token = text(identity.accessToken, 'Access token');
  const tenantId = text(identity.tenantId, 'Tenant');
  assertMemberToken(token);
  return {
    email,
    password,
    userId: text(identity.userId, 'User'),
    client: {
      baseUrl: environment.apiBaseUrl,
      accessToken: token,
      tenantId,
    },
  };
}

async function signIn(page: Page, context: BrowserContext, environment: AuthoringHostEnvironment, instructor: Instructor): Promise<void> {
  page.setDefaultTimeout(30_000);
  page.setDefaultNavigationTimeout(180_000);
  await page.goto(`${environment.webBaseUrl}/sign-in?callbackUrl=%2Fworkspace`, { waitUntil: 'domcontentloaded' });
  await waitForInteractiveCredentials(page);
  await page.getByLabel('Email', { exact: true }).fill(instructor.email);
  await page.getByLabel('Password', { exact: true }).fill(instructor.password);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect.poll(async () => (await context.cookies()).some((cookie) => /gameguild\.session-token/.test(cookie.name)), { timeout: 60_000 }).toBe(true);
  assertSharedAuthCookie(await context.cookies());
  await page.waitForURL((url) => url.pathname.endsWith('/workspace'), { waitUntil: 'load' });
}

async function task(page: Page, name: string): Promise<void> {
  const tab = page.getByRole('tab', { name, exact: true });
  await tab.click();
  await expect(tab).toHaveAttribute('aria-selected', 'true');
}

async function saved(page: Page): Promise<void> {
  await page.keyboard.press('ControlOrMeta+KeyS');
  await expect(page.getByText('Saved', { exact: true })).toBeVisible();
}

async function openLesson(page: Page, url: string): Promise<void> {
  await page.goto(url, { waitUntil: 'load' });
  await expect(page.getByRole('region', { name: 'Coding exercise authoring' })).toBeVisible({ timeout: 120_000 });
  await expect(page.getByLabel('Statement', { exact: true })).toBeVisible();
}

function statement(family: Family): string {
  return family === 'function' ? 'Return twice the integer.' : 'Read an integer and print its double.';
}

async function authorLesson(page: Page, instructor: Instructor, courseId: string, lesson: Lesson, url: string): Promise<void> {
  await openLesson(page, url);
  if (lesson.family === 'function') {
    await page.getByLabel('Exercise type', { exact: true }).selectOption('function');
    await page.getByRole('button', { name: 'Confirm change', exact: true }).click();
  }
  await page.getByLabel('Statement', { exact: true }).fill(statement(lesson.family));
  await page.getByRole('button', { name: 'Add example', exact: true }).click();
  await page.getByLabel('Example input', { exact: true }).fill('3');
  await page.getByLabel('Example output', { exact: true }).fill('6');
  await task(page, 'Environment');
  await page.getByLabel('Time limit (ms)', { exact: true }).fill('-');
  await saved(page);
  const incomplete = record((await readDraft(instructor.client, courseId, lesson)).payload.jsonBody, 'Incomplete document');
  assert.ok(record(incomplete.limits, 'Limits').timeLimitMs === '-', 'The API did not preserve the incomplete numeric buffer.');
  await openLesson(page, url);
  await task(page, 'Environment');
  await expect(page.getByLabel('Time limit (ms)', { exact: true })).toHaveValue('-');
  await page.getByLabel('Time limit (ms)', { exact: true }).fill('5000');
  await task(page, 'Starter code');
  if (lesson.family === 'function') {
    await page.getByLabel('Function name', { exact: true }).fill('twice');
    await page.getByRole('button', { name: 'Add parameter', exact: true }).click();
    await page.getByLabel('Parameter 1 name', { exact: true }).fill('value');
    await page.getByRole('button', { name: 'Apply signature', exact: true }).click();
    await page.getByRole('button', { name: 'Confirm change', exact: true }).click();
  }
  await page.getByRole('button', { name: 'Generate starter', exact: true }).click();
  await expect(page.locator('.monaco-editor')).toBeVisible({ timeout: 60_000 });
  await page.locator('.monaco-editor .view-lines').click();
  await page.keyboard.press('ControlOrMeta+KeyA');
  await page.keyboard.insertText(
    lesson.family === 'function'
      ? 'int twice(int value) { return value * 2; }'
      : '#include <cstdio>\nint main() { int value; if (std::scanf("%d", &value) != 1) return 1; std::printf("%d\\n", value * 2); return 0; }',
  );
  await task(page, 'Tests');
  await page.getByRole('button', { name: 'Add test', exact: true }).click();
  await page.getByLabel('Test name', { exact: true }).fill('Double three');
  if (lesson.family === 'function') {
    await page.getByLabel('value (integer)', { exact: true }).fill('3');
    await page.getByLabel('Expected result (integer)', { exact: true }).fill('6');
  } else {
    await page.getByLabel('Standard input', { exact: true }).fill('3\n');
    await page.getByLabel('Expected stdout', { exact: true }).fill('6\n');
  }
  await page.getByRole('button', { name: 'Duplicate test', exact: true }).click();
  await page.getByLabel('Test name', { exact: true }).fill('Negative double');
  await page.getByLabel('Case visibility', { exact: true }).selectOption('private');
  if (lesson.family === 'function') {
    await page.getByLabel('value (integer)', { exact: true }).fill('-2');
    await page.getByLabel('Expected result (integer)', { exact: true }).fill('-4');
  } else {
    await page.getByLabel('Standard input', { exact: true }).fill('-2\n');
    await page.getByLabel('Expected stdout', { exact: true }).fill('-4\n');
  }
  await saved(page);
  const before = await readDraft(instructor.client, courseId, lesson);
  const document = record(before.payload.jsonBody, 'Current draft');
  assert.ok(document.exerciseType === lesson.family, 'Exercise family did not persist.');
  assert.ok(record(document.problem, 'Problem').statement === statement(lesson.family), 'Statement did not persist.');
  const tests = objects(document.tests, 'Tests');
  assert.ok(tests.length === 2 && tests[0]?.visibility === 'public' && tests[1]?.visibility === 'private', 'Case visibility changed.');
  await openLesson(page, url);
  const after = await readDraft(instructor.client, courseId, lesson);
  assert.ok(after.revision === before.revision && isDeepStrictEqual(after.payload, before.payload), 'Reopening changed the persisted draft.');
  await expect(page.getByLabel('Statement', { exact: true })).toHaveValue(statement(lesson.family));
  await task(page, 'Tests');
  await page.getByRole('button', { name: /Negative double/ }).click();
  await expect(page.getByLabel('Case visibility', { exact: true })).toHaveValue('private');
  await expect(page.getByLabel(lesson.family === 'function' ? 'Expected result (integer)' : 'Expected stdout', { exact: true })).toHaveValue(
    lesson.family === 'function' ? '-4' : '-4\n',
  );
}

async function publishLesson(page: Page, instructor: Instructor, courseId: string, lesson: Lesson, url: string): Promise<void> {
  await openLesson(page, url);
  await saved(page);
  const before = await readDraft(instructor.client, courseId, lesson);
  await page.getByRole('button', { name: 'Publish changes', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Publish changes', exact: true })).toBeEnabled();
  const after = await readDraft(instructor.client, courseId, lesson);
  const published = await apiRecord(instructor.client, `/v1/courses/${encodeURIComponent(courseId)}/content/${encodeURIComponent(lesson.id)}`);
  assert.ok(published.id === lesson.id && published.programId === courseId, 'Publication returned a different content scope.');
  const version = integer(published.version, 'Published content version');
  assert.ok(version > before.basePublishedVersion, 'Publication did not advance the published content version.');
  assert.equal(after.basePublishedVersion, version, 'The draft must be rebased to the actual published content version.');
  assert.equal(after.revision, before.revision + 2, 'Candidate save and publication must each advance one draft revision.');
  await expect(page.getByText('Saved', { exact: true })).toBeVisible();
  const definition = record(after.payload.jsonBody, 'Published definition');
  assert.ok(
    definition.Type === 'coding-assignment' && definition.ExerciseType === lesson.family && definition.Execution,
    'Publication did not compile the current definition.',
  );
  const tests = record(definition.Tests, 'Published cases');
  assert.ok(
    objects(tests.Public, 'Public cases').length === 1 && objects(tests.Private, 'Private cases').length === 1,
    'Publication changed the case buckets.',
  );
  await openLesson(page, url);
  await expect(page.getByLabel('Statement', { exact: true })).toHaveValue(statement(lesson.family));
  await expect(page.getByLabel('Exercise type', { exact: true })).toHaveValue(lesson.family);
}

async function conflictRecovery(page: Page, instructor: Instructor, courseId: string, lesson: Lesson, url: string): Promise<void> {
  const client = instructor.client;
  await openLesson(page, url);
  const before = await readDraft(client, courseId, lesson);
  const remotePayload = { ...before.payload, title: 'Other editor revision' };
  await apiRecord(client, authoringPath(courseId, lesson.id), 'PUT', { revision: before.revision, payload: remotePayload });
  await page.getByLabel('Statement', { exact: true }).fill('Keep my local statement.');
  await page.keyboard.press('ControlOrMeta+KeyS');
  await expect(page.getByText('Conflict', { exact: true })).toBeVisible();
  await expect(page.getByLabel('Statement', { exact: true })).toHaveValue('Keep my local statement.');
  await page.getByRole('button', { name: 'Load latest', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Restore my changes', exact: true })).toBeVisible();
  await expect(page.getByLabel('Statement', { exact: true })).toHaveValue(statement(lesson.family));
  await page.getByRole('button', { name: 'Restore my changes', exact: true }).click();
  await expect(page.getByLabel('Statement', { exact: true })).toHaveValue('Keep my local statement.');
  await saved(page);
  await openLesson(page, url);
  await expect(page.getByLabel('Statement', { exact: true })).toHaveValue('Keep my local statement.');
  await page.getByLabel('Statement', { exact: true }).fill(statement(lesson.family));
  await saved(page);
}

async function failedSave(context: BrowserContext, page: Page, url: string, family: Family): Promise<void> {
  await openLesson(page, url);
  await context.setOffline(true);
  try {
    await page.getByLabel('Statement', { exact: true }).fill('Retain this edit during a network failure.');
    await page.keyboard.press('ControlOrMeta+KeyS');
    await expect(page.getByText('Offline', { exact: true })).toBeVisible();
    await expect(page.getByLabel('Statement', { exact: true })).toHaveValue('Retain this edit during a network failure.');
    await expect(page.getByText('Saved', { exact: true })).toHaveCount(0);
  } finally {
    await context.setOffline(false);
  }
  await page.getByRole('button', { name: 'Retry', exact: true }).click();
  await expect(page.getByText('Saved', { exact: true })).toBeVisible();
  await openLesson(page, url);
  await expect(page.getByLabel('Statement', { exact: true })).toHaveValue('Retain this edit during a network failure.');
  await page.getByLabel('Statement', { exact: true }).fill(statement(family));
  await saved(page);
}

async function captureLayouts(page: Page, directory: string, report: HostReport): Promise<void> {
  for (const viewport of [
    { name: 'desktop', width: 1920, height: 1080 },
    { name: 'mobile', width: 390, height: 844 },
  ]) {
    await page.setViewportSize(viewport);
    for (const name of ['Problem', 'Starter code', 'Tests', 'Validate exercise']) {
      await task(page, name);
      await assertNoHorizontalOverflow(page, `Authoring ${name} ${viewport.name}`);
      const escaped = await page
        .locator(
          '.cs-author-panel .cs-author-field input, .cs-author-panel .cs-author-field select, .cs-author-panel .cs-author-field textarea, .cs-author-panel .cs-author-action, .cs-author-panel .cs-author-icon, .monaco-editor',
        )
        .evaluateAll(
          (elements) =>
            elements.filter((element) => {
              const box = element.getBoundingClientRect();
              return box.width > 0 && box.height > 0 && (box.left < -1 || box.right > window.innerWidth + 1);
            }).length,
        );
      assert.ok(escaped === 0, 'An authoring control escaped the viewport.');
      const screenshot = `${viewport.name}-${name.toLowerCase().replaceAll(' ', '-')}.png`;
      await page.screenshot({ path: resolve(directory, screenshot), fullPage: true });
      report.screenshots.push(screenshot);
    }
  }
  await page.setViewportSize({ width: 1920, height: 1080 });
}

export async function runAuthoringHostProof(environment: AuthoringHostEnvironment, verify: AuthoringHostVerifier): Promise<AuthoringHostEvidence> {
  const evidenceRoot = resolve(root, 'apps/web/test-results/coding-surface-refoundation');
  await mkdir(evidenceRoot, { recursive: true });
  const directory = await mkdtemp(resolve(evidenceRoot, 'host-authoring-'));
  const sourcePaths = [
    'apps/web/scripts/coding-authoring-browser-contracts.mts',
    'apps/web/scripts/coding-authoring-browser-proof.mts',
    'apps/web/src/components/learning/authoring/lesson-authoring-workspace.tsx',
    'apps/web/src/lib/coding-assignment/learning-authoring-document.ts',
  ];
  const hashes: Record<string, string> = {};
  for (const path of sourcePaths)
    hashes[path] = createHash('sha256')
      .update(await readFile(resolve(root, path)))
      .digest('hex');
  const report: HostReport = {
    kind: 'actual-host-coding-authoring',
    nextHost: true,
    realApi: true,
    persona: 'non-admin-course-owner',
    officialGrading: false,
    cases: [],
    compilerRequests: 0,
    academicBrowserRequests: 0,
    cleanupErrors: [],
    errors: [],
    screenshots: [],
    commit: execFileSync('git', ['rev-parse', 'HEAD'], { cwd: root, encoding: 'utf8' }).trim(),
    environment,
    hashes,
    accountLifecycle: 'sign-up-only-no-account-deletion',
    passed: false,
  };
  const persist = () => writeFile(resolve(directory, 'report.json'), JSON.stringify(report, null, 2) + '\n');
  const passed = (id: AuthoringHostCaseId) => {
    report.cases.push({ id, passed: true });
    console.log(`[coding-authoring-host] ${id}`);
  };
  let browser: Browser | undefined;
  let page: Page | undefined;
  let instructor: Instructor | undefined;
  let courseId: string | undefined;
  const lessons: Lesson[] = [];
  try {
    for (const url of [`${environment.apiBaseUrl}/health`, `${environment.webBaseUrl}/api/auth/session`]) {
      const response = await fetch(url, { signal: AbortSignal.timeout(10_000) });
      assert.ok(response.ok, 'The real local API/web stack is not healthy.');
    }
    const nonce = randomUUID().slice(0, 8);
    instructor = await createInstructor(environment, nonce);
    const course = await apiRecord(instructor.client, '/v1/courses', 'POST', { title: `Coding authoring proof ${nonce}`, slug: `ca-${nonce}` });
    courseId = text(course.id, 'Course id');
    assert.ok(course.creatorId === instructor.userId, 'The instructor is not the course owner.');
    const courseSlug = text(course.slug, 'Course slug');
    for (const family of ['function', 'standard-io'] as const) {
      const title = `Authoring ${family}`;
      const content = await apiRecord(instructor.client, `/v1/courses/${encodeURIComponent(courseId)}/content`, 'POST', {
        programId: courseId,
        title,
        slug: family,
        type: 'Code',
        visibility: 'Public',
      });
      lessons.push({ id: text(content.id, 'Content id'), slug: text(content.slug, 'Content slug'), title, family });
    }
    browser = await chromium.launch({ headless: true, executablePath: process.env.CODING_CYCLE_CHROMIUM_EXECUTABLE });
    const context = await browser.newContext({ viewport: { width: 1920, height: 1080 } });
    page = await context.newPage();
    await signIn(page, context, environment, instructor);
    page.on('pageerror', (error) => report.errors.push(`Browser ${error.name}: ${error.message}`));
    page.on('request', (request) => {
      const url = new URL(request.url());
      if (/\/emception\/.*\.(?:wasm|wpk|data)(?:\.gz)?$/.test(url.pathname) || /emception.*worker/i.test(url.pathname)) report.compilerRequests++;
      if (request.method() !== 'GET' && /\/(?:submissions|attempts|grading-jobs|release)(?:\/|$)/.test(url.pathname)) report.academicBrowserRequests++;
    });
    page.on('response', (response) => {
      const failure = classifyAppHttpFailure({
        origins: [environment.webBaseUrl, environment.apiBaseUrl],
        resourceType: response.request().resourceType(),
        status: response.status(),
        url: response.url(),
      });
      if (failure) report.errors.push(failure);
    });
    const urls = new Map<Family, string>();
    for (const lesson of lessons) {
      const url = `${environment.webBaseUrl}/workspace/learning/courses/${encodeURIComponent(courseSlug)}/content/${encodeURIComponent(lesson.slug)}`;
      urls.set(lesson.family, url);
      await authorLesson(page, instructor, courseId, lesson, url);
      passed(lesson.family === 'function' ? 'UX04-C-function-draft' : 'UX04-C-standard-io-draft');
      await persist();
    }
    for (const lesson of lessons) {
      const url = urls.get(lesson.family);
      assert.ok(url, 'Each authored family requires a real host URL.');
      await publishLesson(page, instructor, courseId, lesson, url);
      passed(lesson.family === 'function' ? 'UX04-C-function-publish' : 'UX04-C-standard-io-publish');
      await persist();
    }
    const lesson = lessons[0];
    const url = urls.get('function');
    assert.ok(lesson && url, 'Both authoring families are required.');
    await conflictRecovery(page, instructor, courseId, lesson, url);
    passed('UX04-C-conflict-recovery');
    await failedSave(context, page, url, lesson.family);
    passed('UX04-C-failed-save');
    for (const target of [...lessons].reverse()) {
      await page.getByRole('button', { name: target.title, exact: true }).click();
      await expect(page.getByLabel('Statement', { exact: true })).toHaveValue(statement(target.family), { timeout: 60_000 });
      await expect(page.getByLabel('Exercise type', { exact: true })).toHaveValue(target.family);
    }
    passed('UX04-C-context');
    await captureLayouts(page, directory, report);
    passed('UX04-C-layout');
  } catch (error: unknown) {
    report.errors.push(error instanceof Error ? error.message : 'The real authoring proof failed.');
    if (page && !page.isClosed()) {
      try {
        await page.screenshot({ path: resolve(directory, 'failure.png'), fullPage: true });
        report.screenshots.push('failure.png');
      } catch {
        report.errors.push('The diagnostic browser screenshot failed.');
      }
    }
  } finally {
    try {
      await browser?.close();
    } catch {
      report.cleanupErrors.push('The owned browser failed to close.');
    }
    if (instructor && courseId) {
      for (const lesson of [...lessons].reverse()) {
        try {
          await apiRequest(instructor.client, `/v1/courses/${encodeURIComponent(courseId)}/content/${encodeURIComponent(lesson.id)}`, 'DELETE');
        } catch (error: unknown) {
          report.cleanupErrors.push(error instanceof Error ? error.message : 'Owned content cleanup failed.');
        }
      }
      try {
        await apiRequest(instructor.client, `/v1/courses/${encodeURIComponent(courseId)}`, 'DELETE');
      } catch (error: unknown) {
        report.cleanupErrors.push(error instanceof Error ? error.message : 'Owned course cleanup failed.');
      }
    }
    try {
      verify(report);
      report.passed = true;
    } catch (error: unknown) {
      report.errors.push(error instanceof Error ? error.message : 'The authoring acceptance gate failed.');
      report.passed = false;
    }
    await persist();
    console.log(`[coding-authoring-host] ${report.passed ? 'passed' : 'failed'}: ${resolve(directory, 'report.json')}`);
  }
  return report;
}
