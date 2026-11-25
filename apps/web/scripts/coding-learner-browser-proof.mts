import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { createHash, randomUUID } from 'node:crypto';
import { mkdir, mkdtemp, readFile, writeFile } from 'node:fs/promises';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { isDeepStrictEqual } from 'node:util';
import { chromium, expect, type Browser, type BrowserContext, type Locator, type Page, type Worker } from 'playwright/test';
import type { CodingAssignmentContent, Test } from '../src/lib/coding-assignment/types';
import { CODE_TOOLCHAIN_V3 } from '../../../tools/emception/grading/toolchain-binding.mjs';
import { verifyCodingCycleToolchain } from './coding-cycle-toolchain-support.mjs';
import { assertRenderedEditorSource } from './coding-cycle-editor-support.mjs';
import { assertNoHorizontalOverflow, assertSharedAuthCookie, classifyAppHttpFailure } from './learning-browser-e2e-support.mjs';
import type {
  LearnerHostCaseEvidence,
  LearnerHostCaseId,
  LearnerHostEnvironment,
  LearnerHostEvidence,
  LearnerHostVerifier,
} from './coding-learner-browser-contracts.mjs';

type Family = 'function' | 'standard-io';
interface Api {
  readonly baseUrl: string;
  readonly token?: string;
  readonly tenantId?: string;
}
interface Persona {
  readonly api: Api;
  readonly email: string;
  readonly password: string;
  readonly userId: string;
}
interface Lesson {
  readonly id: string;
  readonly slug: string;
  readonly family: Family;
  assessmentId: string;
  revisionId: string;
}
interface OwnedPage {
  readonly page: Page;
  readonly context: BrowserContext;
  readonly workers: Set<Worker>;
  readonly pending: Promise<void>[];
}
interface HostReport extends LearnerHostEvidence {
  cases: LearnerHostCaseEvidence[];
  academicMutationsDuringPractice: number;
  authoringMutationsDuringPractice: number;
  privateLeaks: number;
  activeRuntimeWorkers: number;
  errors: string[];
  cleanupErrors: string[];
  readonly commit: string;
  readonly hashes: Record<string, string>;
  readonly environment: LearnerHostEnvironment;
  readonly screenshots: string[];
  readonly runtimeWorkerUrls: string[];
  readonly accountLifecycle: 'sign-up-only-no-account-deletion';
  ownedFixture: { courseId: string | null; lessonIds: string[] };
  passed: boolean;
}

const root = fileURLToPath(new URL('../../../', import.meta.url));
const sourcePath = '/home/user/main.cpp';

function isRecord(value: unknown): value is Record<string, unknown> {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
}
function record(value: unknown): Record<string, unknown> {
  if (!isRecord(value)) throw new TypeError('The solving API response must be an object.');
  return value;
}
function text(value: unknown): string {
  if (typeof value !== 'string' || !value.trim()) throw new TypeError('The solving API response requires a nonempty identity.');
  return value;
}
function objects(value: unknown): Record<string, unknown>[] {
  if (!Array.isArray(value)) throw new TypeError('The solving API response must be an array.');
  const entries: readonly unknown[] = value;
  return entries.map(record);
}
async function request(client: Api, path: string, method: 'GET' | 'POST' | 'PUT' | 'DELETE' = 'GET', body?: unknown): Promise<unknown> {
  const response = await fetch(`${client.baseUrl}${path}`, {
    method,
    redirect: 'error',
    signal: AbortSignal.timeout(30_000),
    headers: {
      ...(client.token ? { Authorization: `Bearer ${client.token}` } : {}),
      ...(client.tenantId ? { 'X-Tenant-Id': client.tenantId } : {}),
      ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
    },
    ...(body === undefined ? {} : { body: JSON.stringify(body) }),
  });
  if (!response.ok) throw new Error(`Solving proof API ${method} ${path}: HTTP ${response.status}.`);
  return response.status === 204 || response.headers.get('content-length') === '0' ? null : response.json();
}
async function member(environment: LearnerHostEnvironment, name: string): Promise<Persona> {
  const email = `cs-${name}@qgs.test`;
  const password = `Cs!1${randomUUID()}`;
  const anonymous = { baseUrl: environment.apiBaseUrl };
  await request(anonymous, '/v1/auth/sign-up', 'POST', { email, username: `cs-${name}`, password });
  const identity = record(await request(anonymous, '/v1/auth/sign-in', 'POST', { email, password }));
  assert.ok(!identity.requiresMfa && !identity.requiresStepUp, 'The isolated Member needs an unsupported auth continuation.');
  const token = text(identity.accessToken);
  const encoded = token.split('.')[1];
  assert.ok(encoded, 'A real Member JWT is required.');
  const claims = record(JSON.parse(Buffer.from(encoded, 'base64url').toString('utf8')));
  const role = claims.role ?? claims.roles ?? claims['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
  const roles: readonly unknown[] = Array.isArray(role) ? role : [role];
  assert.ok(roles.includes('Member') && !roles.some((value) => typeof value === 'string' && /admin/i.test(value)), 'SystemAdmin cannot substitute a persona.');
  return { api: { baseUrl: environment.apiBaseUrl, token, tenantId: text(identity.tenantId) }, email, password, userId: text(identity.userId) };
}
function solution(family: Family, correct: boolean): string {
  return family === 'function'
    ? `int twice(int value) { return ${correct ? 'value * 2' : '0'}; }\n`
    : `#include <cstdio>\nint main() {\n  int value = 0;\n  if (std::scanf("%d", &value) != 1) return 1;\n  std::printf("%d\\n", ${correct ? 'value * 2' : '0'});\n  return 0;\n}\n`;
}
export function createLearnerHostDefinition(family: Family, secret: string): CodingAssignmentContent {
  function test(name: string, input: number, expected: number): Test {
    return family === 'function'
      ? {
          kind: 'functional',
          Id: randomUUID(),
          Name: name,
          Weight: 1,
          Function: { FunctionName: 'twice', Parameters: [{ Name: 'value', Type: 'integer' }], ReturnType: { Type: 'integer' } },
          Cases: [{ Id: randomUUID(), Inputs: [{ Type: 'integer', Content: input }], Expected: { Type: 'integer', Content: expected } }],
        }
      : { kind: 'standard', Id: randomUUID(), Name: name, Weight: 1, Stdin: `${input}\n`, Stdout: `${expected}\n`, ExitCode: 0 };
  }
  const fileId = randomUUID();
  return {
    Type: 'coding-assignment',
    Version: 1,
    ExerciseType: family,
    Problem: {
      Statement: family === 'function' ? 'Return twice the integer.' : 'Read an integer and print its double.',
      Examples: [{ Id: 'example', Input: '3', Output: '6', Explanation: 'Three doubled is six.' }],
      Constraints: ['The integer is between -100 and 100.'],
    },
    Environment: { Language: 'cpp', Runtime: 'wasi', Tools: 'clang', LibBundle: null, AllowStudentCreateFiles: false },
    Execution: {
      Limits: { TimeLimitMs: 5000, OutputLimitBytes: 1_048_576, MaxFileCount: 10 },
      EntryPoint: { Path: sourcePath, ...(family === 'function' ? { Symbol: 'twice' } : {}) },
      Toolchain: {
        Key: 'cpp-emception',
        Version: CODE_TOOLCHAIN_V3.artifactVersion,
        RuntimeAbi: CODE_TOOLCHAIN_V3.runtimeAbi,
        ArtifactDigest: `sha256:${CODE_TOOLCHAIN_V3.toolchainLockHash}`,
      },
    },
    Data: {
      Files: {
        [sourcePath]: { Id: fileId, Content: solution(family, false), Encoding: 'text', Visibility: 'Public', Modifiable: true },
        '/home/user/teacher-notes.txt': { Id: randomUUID(), Content: secret, Encoding: 'text', Visibility: 'Private', Modifiable: false },
      },
    },
    Tests: { Public: [test('Double three', 3, 6)], Private: [test(secret, -7, -14)] },
    ValidationSolutions: [
      { Id: randomUUID(), Name: secret, Role: 'reference', Files: [{ FileId: fileId, Path: sourcePath, Content: solution(family, true) }] },
    ],
    Grading: { MaxScore: 100 },
  };
}

function runtimeWorkers(owned: OwnedPage): Worker[] {
  return [...owned.workers].filter((worker) => /worker-entry|emception.*worker/i.test(worker.url()));
}
async function drained(owned: OwnedPage): Promise<void> {
  await expect.poll(() => runtimeWorkers(owned).length, { timeout: 30_000 }).toBe(0);
}
async function closePage(owned: OwnedPage): Promise<void> {
  await Promise.all(owned.pending);
  await owned.context.close();
  await expect.poll(() => owned.workers.size).toBe(0);
}
async function openPage(
  browser: Browser,
  environment: LearnerHostEnvironment,
  persona: Persona,
  secret: string,
  report: HostReport,
  privacy: boolean,
  ownedPages: OwnedPage[],
): Promise<OwnedPage> {
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  const page = await context.newPage();
  const owned: OwnedPage = { page, context, workers: new Set(), pending: [] };
  ownedPages.push(owned);
  page.setDefaultTimeout(60_000);
  page.setDefaultNavigationTimeout(180_000);
  page.on('pageerror', (error) => report.errors.push(`Browser ${error.name}: ${error.message}`));
  page.on('worker', (worker) => {
    owned.workers.add(worker);
    if (/worker-entry|emception.*worker/i.test(worker.url())) report.runtimeWorkerUrls.push(worker.url());
    worker.on('close', () => owned.workers.delete(worker));
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
  if (privacy)
    page.on('requestfinished', (completed) => {
      if (!['document', 'fetch', 'xhr'].includes(completed.resourceType())) return;
      const scan = (async () => {
        const response = await completed.response();
        if (!response?.ok() || !/text\/html|text\/x-component|application\/json/.test(response.headers()['content-type'] ?? '')) return;
        if ((await response.text()).includes(secret)) {
          report.privateLeaks++;
          throw new Error('A learner-visible response contains protected fixture data.');
        }
      })().catch((error: unknown) => {
        report.errors.push(error instanceof Error ? error.message : 'Response privacy observation failed.');
      });
      owned.pending.push(scan);
    });
  const csrfResponse = await context.request.get(`${environment.webBaseUrl}/api/auth/csrf`, { timeout: 180_000 });
  assert.ok(csrfResponse.ok(), 'The real browser CSRF route failed.');
  const csrf = record(await csrfResponse.json());
  const authenticated = await context.request.post(`${environment.webBaseUrl}/api/auth/signin/credentials`, {
    timeout: 180_000,
    data: { email: persona.email, password: persona.password, csrfToken: text(csrf.csrfToken), redirect: false, redirectTo: '/learn' },
  });
  assert.ok(authenticated.ok(), `The real Member browser sign-in failed (HTTP ${authenticated.status()}).`);
  assertSharedAuthCookie(await context.cookies());
  const sessionResponse = await context.request.get(`${environment.webBaseUrl}/api/auth/session`);
  assert.ok(sessionResponse.ok(), 'The browser session could not be verified.');
  const session = record(await sessionResponse.json());
  assert.equal(record(session.user).id, persona.userId, 'The browser authenticated another actor.');
  assert.equal(session.tenantId, persona.api.tenantId, 'The browser authenticated another tenant.');
  return owned;
}
async function solver(owned: OwnedPage, url: string): Promise<Locator> {
  await owned.page.goto(url, { waitUntil: 'load' });
  const surface = owned.page.locator('[data-coding-mode="learner"]');
  await expect(surface.getByRole('region', { name: 'Coding solving workspace' })).toBeVisible({ timeout: 120_000 });
  await expect(surface.locator('.monaco-editor')).toBeAttached();
  await expect(surface.getByRole('button', { name: 'Run public tests', exact: true })).toBeEnabled();
  return surface;
}
async function view(surface: Locator, name: 'Problem' | 'Code' | 'Cases' | 'Results'): Promise<void> {
  await surface.getByRole('button', { name, exact: true }).click();
  await expect(surface.getByRole('button', { name, exact: true })).toHaveAttribute('aria-pressed', 'true');
}
async function editSource(page: Page, surface: Locator, value: string): Promise<void> {
  await view(surface, 'Code');
  const editor = surface.locator('.monaco-editor');
  await editor.locator('.view-lines').click();
  await page.keyboard.press('ControlOrMeta+KeyA');
  await page.keyboard.insertText(value);
  await assertRenderedEditorSource(page, editor, value);
}
async function publicTests(surface: Locator, passed: boolean): Promise<void> {
  const command = surface.getByRole('button', { name: 'Run public tests', exact: true });
  await command.click();
  await expect(surface.getByRole('button', { name: 'Stop', exact: true })).toBeEnabled();
  await expect(command).toBeEnabled({ timeout: 180_000 });
  const panel = surface.getByTestId('test-results-panel');
  await expect(panel.getByText(`${passed ? 1 : 0} passed`, { exact: true })).toBeVisible();
  await expect(panel.getByText(`${passed ? 0 : 1} failed`, { exact: true })).toBeVisible();
}
async function runSelected(surface: Locator): Promise<void> {
  const available = surface.getByRole('button', { name: 'Run public tests', exact: true });
  await surface.getByRole('button', { name: 'Run selected case', exact: true }).click();
  await expect(available).toBeDisabled();
  await expect(available).toBeEnabled({ timeout: 180_000 });
}
async function personal(surface: Locator, family: Family, name: string): Promise<void> {
  await view(surface, 'Cases');
  await surface.getByRole('button', { name: 'Add personal case', exact: true }).click();
  await surface.getByLabel('Case name', { exact: true }).fill(name);
  await surface.getByLabel(family === 'function' ? 'value (integer)' : 'Standard input', { exact: true }).fill(family === 'function' ? '4' : '4\n');
}
async function academic(persona: Persona, enrollmentId: string, lesson: Lesson): Promise<unknown> {
  const submissions = objects(await request(persona.api, `/v1/assessments/my-submissions/${enrollmentId}`)).filter(
    (value) => value.assessmentId === lesson.assessmentId,
  );
  assert.equal(submissions.length, 1, 'Opening the exercise must supply one ordinary baseline attempt.');
  const runtime = record(await request(persona.api, `/v1/assessments/runtime-submissions/${text(submissions[0]?.id)}`));
  assert.equal(runtime.definitionRevisionId, lesson.revisionId, 'The learner must use the published frozen revision.');
  assert.equal(record(runtime.execution).submittedResponse, null, 'Practice must not submit a response.');
  return { submissions, runtime };
}
async function authoring(owner: Persona, courseId: string, lesson: Lesson): Promise<unknown> {
  return request(owner.api, `/v1/courses/${courseId}/content/${lesson.id}/authoring`);
}
async function layouts(owned: OwnedPage, surface: Locator, family: Family, directory: string, report: HostReport): Promise<void> {
  for (const width of [1440, 390, 320]) {
    await owned.page.setViewportSize({ width, height: width === 1440 ? 1000 : 844 });
    for (const name of ['Problem', 'Code', 'Cases', 'Results'] as const) {
      await view(surface, name);
      await assertNoHorizontalOverflow(owned.page, `Learner ${family} ${name} ${width}`);
      if (name === 'Code') await expect(surface.locator('.monaco-editor')).toBeVisible();
      const screenshot = `${family}-${name.toLowerCase()}-${width}.png`;
      await owned.page.screenshot({ path: resolve(directory, screenshot), fullPage: true });
      report.screenshots.push(screenshot);
    }
  }
  await owned.page.setViewportSize({ width: 1440, height: 1000 });
}

export async function runLearnerHostProof(environment: LearnerHostEnvironment, verify: LearnerHostVerifier): Promise<LearnerHostEvidence> {
  const base = resolve(root, 'apps/web/test-results/coding-surface-refoundation');
  await mkdir(base, { recursive: true });
  const directory = await mkdtemp(resolve(base, 'host-solving-'));
  const hashes: Record<string, string> = {};
  for (const path of [
    'apps/web/scripts/coding-host-environment.mjs',
    'apps/web/scripts/coding-activity-browser-e2e.mjs',
    'apps/web/scripts/coding-authoring-browser-contracts.mts',
    'apps/web/scripts/coding-learner-browser-contracts.mts',
    'apps/web/scripts/coding-learner-browser-proof.mts',
    'packages/features/coding-surface/src/coding-surface.tsx',
    'packages/features/coding-surface/src/practice-runner.ts',
    'packages/features/coding-surface/src/workbench-session.ts',
    'packages/features/coding-surface/src/solving-layout.tsx',
    'apps/web/src/components/learning/coding-activity-client.tsx',
    'apps/api/Source/GameGuild.API/bin/Debug/net10.0/GameGuild.API.dll',
    'apps/api/Source/GameGuild.API/bin/Debug/net10.0/GameGuild.Learning.Assessments.CodeAdapter.dll',
    'apps/web/public/emception/manifest.json',
  ])
    hashes[path] = createHash('sha256')
      .update(await readFile(resolve(root, path)))
      .digest('hex');
  const report: HostReport = {
    kind: 'actual-host-coding-solving',
    nextHost: true,
    realApi: true,
    authentication: 'real-browser-credentials-route',
    personas: ['non-admin-course-owner', 'enrolled-member', 'independent-enrolled-member'],
    officialGrading: false,
    cases: [],
    academicMutationsDuringPractice: 0,
    authoringMutationsDuringPractice: 0,
    privateLeaks: 0,
    activeRuntimeWorkers: 0,
    errors: [],
    cleanupErrors: [],
    screenshots: [],
    runtimeWorkerUrls: [],
    hashes,
    environment,
    commit: execFileSync('git', ['rev-parse', 'HEAD'], { cwd: root, encoding: 'utf8' }).trim(),
    accountLifecycle: 'sign-up-only-no-account-deletion',
    ownedFixture: { courseId: null, lessonIds: [] },
    passed: false,
  };
  const persist = () => writeFile(resolve(directory, 'report.json'), JSON.stringify(report, null, 2) + '\n');
  const passed = (id: LearnerHostCaseId) => {
    report.cases.push({ id, passed: true });
    console.log(`[coding-solving-host] ${id}`);
  };
  let browser: Browser | undefined;
  let owner: Persona | undefined;
  let courseId: string | undefined;
  let publishedCourse = false;
  const lessons: Lesson[] = [];
  const pages: OwnedPage[] = [];
  const nonce = randomUUID().slice(0, 8);
  const secret = `teacher-only-${randomUUID()}`;
  try {
    await verifyCodingCycleToolchain(resolve(root, 'apps/web/public/emception/manifest.json'));
    for (const url of [`${environment.apiBaseUrl}/health`, `${environment.webBaseUrl}/api/auth/session`]) {
      const response = await fetch(url, { signal: AbortSignal.timeout(10_000) });
      assert.ok(response.ok, 'The real local API/web stack is not healthy.');
    }
    owner = await member(environment, `${nonce}-owner`);
    const learner = await member(environment, `${nonce}-a`);
    const independent = await member(environment, `${nonce}-b`);
    assert.ok(
      owner.api.tenantId === learner.api.tenantId && learner.api.tenantId === independent.api.tenantId,
      'Fixture personas must share their actual request tenant.',
    );
    const course = record(await request(owner.api, '/v1/courses', 'POST', { title: `Coding solving proof ${nonce}`, slug: `cs-${nonce}` }));
    courseId = text(course.id);
    assert.equal(course.creatorId, owner.userId);
    const slug = text(course.slug);
    for (const family of ['standard-io', 'function'] as const) {
      const content = record(
        await request(owner.api, `/v1/courses/${courseId}/content`, 'POST', {
          programId: courseId,
          title: `Double ${family}`,
          slug: family,
          type: 'Code',
          visibility: 'Public',
          isRequired: true,
        }),
      );
      const lesson: Lesson = { id: text(content.id), slug: text(content.slug), family, assessmentId: '', revisionId: '' };
      lessons.push(lesson);
      const path = `/v1/courses/${courseId}/content/${lesson.id}/authoring`;
      const initial = record(await request(owner.api, path));
      const saved = record(
        await request(owner.api, path, 'PUT', {
          revision: initial.revision,
          payload: { ...record(initial.payload), jsonBody: createLearnerHostDefinition(family, secret) },
        }),
      );
      const published = record(await request(owner.api, `${path}/publish`, 'POST', { revision: saved.revision }));
      const contentVersion = record(published.publishedContent);
      assert.equal(contentVersion.id, lesson.id, 'Publication returned another lesson.');
      assert.equal(record(published.draft).basePublishedVersion, contentVersion.version, 'Publication did not rebase to the actual content version.');
      const assessments = objects(await request(owner.api, `/v1/assessments/course/${courseId}`));
      lesson.assessmentId = text(assessments.find((value) => value.contentId === lesson.id)?.id);
      const state = record(await request(owner.api, `/v1/assessments/${lesson.assessmentId}/authoring-state`));
      assert.equal(state.publishedMatchesDraft, true, 'The current authoring workflow did not publish its assessment candidate.');
      lesson.revisionId = text(record(state.published).revisionId);
    }
    for (const action of ['submit', 'approve', 'publish']) await request(owner.api, `/v1/courses/${courseId}:${action}`, 'POST');
    publishedCourse = true;
    const enrollments = new Map<string, string>();
    for (const persona of [learner, independent]) {
      await request(owner.api, `/v1/courses/${courseId}/users/${persona.userId}`, 'POST');
      const enrollment = record(await request(persona.api, '/api/learning/enrollments', 'POST', { courseId, userId: persona.userId, cohortId: null }));
      assert.equal(enrollment.courseId, courseId);
      enrollments.set(persona.userId, text(enrollment.id));
    }
    browser = await chromium.launch({ headless: true, executablePath: process.env.CODING_CYCLE_CHROMIUM_EXECUTABLE });
    const teacher = await openPage(browser, environment, owner, secret, report, false, pages);
    for (const lesson of lessons) {
      const before = await authoring(owner, courseId, lesson);
      const beforeSubmissions = await request(owner.api, `/v1/assessments/${lesson.assessmentId}/submissions`);
      await teacher.page.goto(`${environment.webBaseUrl}/workspace/learning/courses/${slug}/content/${lesson.slug}`, { waitUntil: 'load' });
      await teacher.page.getByRole('tab', { name: 'Validate exercise', exact: true }).click();
      await teacher.page.getByRole('button', { name: 'Learner preview', exact: true }).click();
      const surface = teacher.page.getByRole('region', { name: 'Learner preview workspace' }).locator('[data-coding-mode="learner"]');
      await expect(surface.getByRole('region', { name: 'Coding solving workspace' })).toBeVisible();
      assert.ok(!(await surface.innerText()).includes(secret));
      await expect(surface.getByRole('button', { name: 'Submit solution', exact: true })).toHaveCount(0);
      await personal(surface, lesson.family, 'Teacher scratch only');
      await runSelected(surface);
      await expect(surface.getByText('Output observed', { exact: true })).toBeVisible();
      await expect(surface.getByRole('region', { name: lesson.family === 'function' ? 'Return value' : 'stdout', exact: true }).locator('pre')).toHaveText(
        lesson.family === 'function' ? '0' : '0\n',
      );
      await drained(teacher);
      await teacher.page.getByRole('button', { name: 'Back to authoring', exact: true }).click();
      if (!isDeepStrictEqual(before, await authoring(owner, courseId, lesson))) report.authoringMutationsDuringPractice++;
      if (!isDeepStrictEqual(beforeSubmissions, await request(owner.api, `/v1/assessments/${lesson.assessmentId}/submissions`)))
        report.academicMutationsDuringPractice++;
    }
    passed('UX05-A3-preview-isolation');
    await closePage(teacher);
    const student = await openPage(browser, environment, learner, secret, report, true, pages);
    const enrollmentId = text(enrollments.get(learner.userId));
    for (const lesson of lessons) {
      const surface = await solver(student, `${environment.webBaseUrl}/learn/courses/${slug}/activities/assessment-${lesson.assessmentId}`);
      await expect(surface.getByRole('heading', { name: 'Problem', exact: true })).toBeVisible();
      const before = await academic(learner, enrollmentId, lesson);
      const original = await authoring(owner, courseId, lesson);
      await publicTests(surface, false);
      await drained(student);
      await editSource(student.page, surface, solution(lesson.family, true));
      await view(surface, 'Results');
      await expect(surface.getByText('Outdated result. Code or exercise settings changed.', { exact: true })).toBeVisible();
      await publicTests(surface, true);
      await drained(student);
      await personal(surface, lesson.family, 'Learner scratch only');
      await runSelected(surface);
      await expect(surface.getByText('Output observed', { exact: true })).toBeVisible();
      await expect(surface.getByRole('region', { name: lesson.family === 'function' ? 'Return value' : 'stdout', exact: true }).locator('pre')).toHaveText(
        lesson.family === 'function' ? '8' : '8\n',
      );
      await drained(student);
      await view(surface, 'Cases');
      if (lesson.family === 'function') {
        await surface.getByLabel('value (integer)', { exact: true }).fill('-');
        const workerCount = student.workers.size;
        await surface.getByRole('button', { name: 'Run selected case', exact: true }).click();
        await expect(surface.getByText('Enter a complete 32-bit integer.', { exact: true }).first()).toBeVisible();
        assert.equal(student.workers.size, workerCount, 'Incomplete input started a worker.');
        await view(surface, 'Cases');
        await expect(surface.getByLabel('value (integer)', { exact: true })).toHaveValue('-');
        await surface.getByLabel('value (integer)', { exact: true }).fill('4');
        await surface.getByLabel('Compare return value', { exact: true }).check();
        await surface.getByLabel('Expected result (integer)', { exact: true }).fill('999');
      } else {
        await surface.getByLabel('Compare stdout', { exact: true }).check();
        await surface.getByLabel('Expected stdout', { exact: true }).fill('999\n');
      }
      await runSelected(surface);
      await expect(surface.getByText('Does not match expectation', { exact: true })).toBeVisible();
      await drained(student);
      await layouts(student, surface, lesson.family, directory, report);
      await view(surface, 'Cases');
      await surface.getByRole('button', { name: 'Run selected case', exact: true }).click();
      await expect(surface.getByRole('button', { name: 'Stop', exact: true })).toBeEnabled();
      await surface.getByRole('button', { name: 'Stop', exact: true }).click();
      await expect(surface.getByText('Run cancelled.', { exact: true })).toBeVisible();
      await drained(student);
      if (!isDeepStrictEqual(before, await academic(learner, enrollmentId, lesson))) report.academicMutationsDuringPractice++;
      if (!isDeepStrictEqual(original, await authoring(owner, courseId, lesson))) report.authoringMutationsDuringPractice++;
      passed(lesson.family === 'function' ? 'UX05-A3-function-solve' : 'UX05-A3-standard-io-solve');
      await persist();
    }
    passed('UX05-A3-stale-cancel');
    passed('UX05-A3-scratch-isolation');
    passed('UX05-A3-layouts');
    await closePage(student);
    const other = await openPage(browser, environment, independent, secret, report, true, pages);
    for (const lesson of [...lessons].reverse()) {
      const surface = await solver(other, `${environment.webBaseUrl}/learn/courses/${slug}/activities/assessment-${lesson.assessmentId}`);
      await view(surface, 'Cases');
      await surface.getByRole('tab', { name: 'My cases', exact: true }).click();
      await expect(surface.getByText('Learner scratch only', { exact: true })).toHaveCount(0);
      await expect(surface.getByText('Teacher scratch only', { exact: true })).toHaveCount(0);
      await academic(independent, text(enrollments.get(independent.userId)), lesson);
      await view(surface, 'Code');
      await assertRenderedEditorSource(other.page, surface.locator('.monaco-editor'), solution(lesson.family, false));
      await drained(other);
    }
    passed('UX05-A3-context-isolation');
    await closePage(other);
    assert.ok(report.runtimeWorkerUrls.length > 0, 'The actual runtime worker was never observed.');
    await Promise.all(pages.flatMap((owned) => owned.pending));
    report.activeRuntimeWorkers = pages.reduce((sum, owned) => sum + runtimeWorkers(owned).length, 0);
    passed('UX05-A3-runtime-cleanup');
  } catch (error: unknown) {
    report.errors.push(error instanceof Error ? error.message : 'Actual host solving failed.');
    const active = pages.findLast((owned) => !owned.page.isClosed());
    if (active) {
      try {
        await active.page.screenshot({ path: resolve(directory, 'failure.png'), fullPage: true });
        report.screenshots.push('failure.png');
      } catch {
        report.errors.push('Solving failure screenshot could not be captured.');
      }
    }
  } finally {
    await Promise.all(pages.flatMap((owned) => owned.pending));
    report.ownedFixture = { courseId: courseId ?? null, lessonIds: lessons.map((lesson) => lesson.id) };
    try {
      await browser?.close();
    } catch {
      report.cleanupErrors.push('The owned solving browser failed to close.');
    }
    report.activeRuntimeWorkers = pages.reduce((sum, owned) => sum + runtimeWorkers(owned).length, 0);
    if (owner && courseId) {
      for (const lesson of [...lessons].reverse()) {
        try {
          if (lesson.revisionId) {
            const current = record(await request(owner.api, `/v1/assessments/${lesson.assessmentId}/authoring-state`));
            assert.equal(record(current.published).revisionId, lesson.revisionId, 'Cleanup cannot unpublish another revision.');
            await request(owner.api, `/v1/assessments/${lesson.assessmentId}/revisions/unpublish`, 'POST', {
              expectedRevisionId: lesson.revisionId,
              expectedAssessmentVersion: current.assessmentVersion,
              idempotencyKey: `cs-cleanup-${nonce}-${lesson.family}`,
            });
          }
          await request(owner.api, `/v1/courses/${courseId}/content/${lesson.id}`, 'DELETE');
        } catch (error: unknown) {
          report.cleanupErrors.push(error instanceof Error ? error.message : 'Owned lesson cleanup failed.');
        }
      }
      try {
        if (publishedCourse) await request(owner.api, `/v1/courses/${courseId}:unpublish`, 'POST');
        await request(owner.api, `/v1/courses/${courseId}`, 'DELETE');
      } catch (error: unknown) {
        report.cleanupErrors.push(error instanceof Error ? error.message : 'Owned course cleanup failed.');
      }
    }
    try {
      verify(report);
      report.passed = true;
    } catch (error: unknown) {
      report.errors.push(error instanceof Error ? error.message : 'The actual solving gate failed.');
    }
    await persist();
    console.log(`[coding-solving-host] ${report.passed ? 'passed' : 'failed'}: ${resolve(directory, 'report.json')}`);
  }
  return report;
}
