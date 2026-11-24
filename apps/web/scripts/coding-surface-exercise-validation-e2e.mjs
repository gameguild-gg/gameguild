import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { createReadStream } from 'node:fs';
import { mkdir, mkdtemp, readFile, realpath, writeFile } from 'node:fs/promises';
import { createServer } from 'node:http';
import { extname, resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';
import { pipeline } from 'node:stream/promises';
import { chromium, expect as baseExpect } from 'playwright/test';
import { buildCodingSurfaceFixture } from './coding-surface-fixture-build.mjs';
import { createCodeBrowserLaunchOptions } from '../../../tools/emception/grading/browser-launch-options.mjs';
import { verifyCodingCycleToolchain } from './coding-cycle-toolchain-support.mjs';

const expect = baseExpect.configure({ timeout: 125_000 });
const family = process.env.CODING_EXERCISE_FAMILY;
if (family !== undefined && family !== 'function' && family !== 'standard-io') throw new TypeError('Unknown coding exercise family.');
const families = family === undefined ? ['function', 'standard-io'] : [family];
const repo = fileURLToPath(new URL('../../../', import.meta.url));
const evidenceRoot = resolve(repo, 'apps/web/test-results/coding-surface-refoundation/runtime');
await mkdir(evidenceRoot, { recursive: true });
const evidence = await mkdtemp(resolve(evidenceRoot, 'exercise-check-'));
const bundle = resolve(evidence, 'bundle');
const assets = await realpath(resolve(repo, 'apps/web/public/emception'));
const monaco = await realpath(resolve(repo, 'apps/web/node_modules/monaco-editor/min/vs'));
await verifyCodingCycleToolchain(resolve(assets, 'manifest.json'));
const hash = (bytes) => createHash('sha256').update(bytes).digest('hex');
const sources = [
  'authoring-surface.tsx',
  'authoring-types.ts',
  'authoring-model.ts',
  'authoring-fields.tsx',
  'authoring-problem.tsx',
  'authoring-environment.tsx',
  'authoring-starter.tsx',
  'authoring-tests.tsx',
  'authoring-validation.tsx',
  'authoring-validation-results.tsx',
  'authoring-learner-preview.tsx',
  'authoring.css',
  'exercise-validation.ts',
  'exercise-validation-types.ts',
  'exercise-preparation.ts',
  'coding-surface.tsx',
  'coding-workbench.tsx',
  'workbench-session.ts',
  'preview-runner.ts',
  'execution-plan.ts',
  'runtime-session.ts',
  'testing/exercise-validation-browser.tsx',
  'testing/exercise-validation-fixtures.ts',
];
const report = {
  passed: false,
  productionComponent: true,
  actualRuntime: true,
  coldBrowserPerFamily: true,
  compilerMemoryPlateau: false,
  realMonaco: true,
  nextHost: false,
  persisted: false,
  officialGrading: false,
  partialSelection: family !== undefined,
  requiredCases: [
    ...families.map((value) => `UX04-B-${value}`),
    'UX04-B-redacted-preview',
    'UX04-B-layout',
    ...(families.includes('function') ? ['UX04-B-build-failure', 'UX04-B-cancel'] : []),
  ],
  cases: [],
  solutionObservations: [],
  observations: [],
  layouts: [],
  errors: [],
  runtimeConsole: [],
  omittedConsoleEntries: 0,
  academicRequests: [],
  sourceHashes: Object.fromEntries(
    await Promise.all(sources.map(async (path) => [path, hash(await readFile(resolve(repo, 'packages/features/coding-surface/src', path)))])),
  ),
  sdkBuiltHashes: Object.fromEntries(
    await Promise.all(
      ['createEmception.js', 'presets.js', 'worker-client.js', 'worker-entry.js', 'tool-runner.js'].map(async (path) => [
        path,
        hash(await readFile(resolve(repo, 'tools/emception/packages/browser/dist', path))),
      ]),
    ),
  ),
  sdkSourceHashes: Object.fromEntries(
    await Promise.all(
      [
        'core/src/types.ts',
        'core/src/worker-protocol.ts',
        'core/src/runtime/worker-orchestrator.ts',
        'browser/src/createEmception.ts',
        'browser/src/worker-client.ts',
        'browser/src/worker-entry.ts',
        'browser/src/tool-runner.ts',
      ].map(async (path) => [path, hash(await readFile(resolve(repo, 'tools/emception/packages', path)))]),
    ),
  ),
  orchestratorBuiltHash: hash(await readFile(resolve(repo, 'tools/emception/packages/core/dist/runtime/worker-orchestrator.js'))),
  sharedComposerHash: hash(await readFile(resolve(repo, 'packages/features/coding/src/native-function-support.ts'))),
  manifestHash: hash(await readFile(resolve(assets, 'manifest.json'))),
  harnessHash: hash(await readFile(fileURLToPath(import.meta.url))),
  fixtureBuilderHash: hash(await readFile(new URL('./coding-surface-fixture-build.mjs', import.meta.url))),
};
const persist = () => writeFile(resolve(evidence, 'report.json'), JSON.stringify(report, null, 2) + '\n');
await persist();
await buildCodingSurfaceFixture('exercise', bundle);
const server = createServer(async (request, response) => {
  response.setHeader('Cross-Origin-Opener-Policy', 'same-origin');
  response.setHeader('Cross-Origin-Embedder-Policy', 'require-corp');
  response.setHeader('Cache-Control', 'no-store');
  try {
    const url = new URL(request.url, 'http://localhost');
    if (url.pathname === '/') {
      response.setHeader('Content-Type', 'text/html');
      response.end(
        '<!doctype html><html lang="en"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Exercise preparation proof</title><link rel="stylesheet" href="/bundle/exercise.css"><style>*{box-sizing:border-box}body{margin:0;background:#09090b;color:#f4f4f5;font:14px system-ui}#root{width:100%;min-width:0;padding:16px 20px}button,input,textarea,select{font:inherit;color:inherit;background:#27272a;border:1px solid #52525b;border-radius:4px}button:disabled{opacity:.5}</style><div id="root"></div></html>',
      );
      return;
    }
    const prefix = ['/bundle/', '/monaco/vs/', '/emception/', '/cdn/'].find((value) => url.pathname.startsWith(value));
    if (!prefix) {
      response.writeHead(404).end();
      return;
    }
    const directory = prefix === '/bundle/' ? bundle : prefix === '/monaco/vs/' ? monaco : assets;
    const path = await realpath(resolve(directory, decodeURIComponent(url.pathname.slice(prefix.length))));
    if (!path.startsWith(directory + sep)) {
      response.writeHead(403).end();
      return;
    }
    response.setHeader(
      'Content-Type',
      { '.js': 'text/javascript', '.mjs': 'text/javascript', '.json': 'application/json', '.wasm': 'application/wasm', '.css': 'text/css', '.ttf': 'font/ttf' }[
        extname(path)
      ] ?? 'application/octet-stream',
    );
    await pipeline(createReadStream(path), response);
  } catch {
    if (!response.headersSent) response.writeHead(404);
    response.end();
  }
});
let browser;
let context;
let activePage;
try {
  await new Promise((done, reject) => {
    server.once('error', reject);
    server.listen(0, '127.0.0.1', done);
  });
  const address = server.address();
  assert.ok(address && typeof address !== 'string');
  const origin = `http://127.0.0.1:${address.port}`;
  for (const family of families) {
    console.log(`[exercise-check] ${family}`);
    browser = await chromium.launch(createCodeBrowserLaunchOptions(process.env.GAMEGUILD_CODE_WORKER_BROWSER_CHANNEL));
    context = await browser.newContext({ serviceWorkers: 'block', acceptDownloads: false, viewport: { width: 1440, height: 1000 } });
    await context.route('**/*', (route) => (new URL(route.request().url()).origin === origin ? route.continue() : route.abort('blockedbyclient')));
    await context.routeWebSocket('**/*', (socket) => socket.close());
    const page = await context.newPage();
    activePage = page;
    page.setDefaultTimeout(125_000);
    const compilers = new Set();
    const compilerRequests = [];
    let peak = 0;
    page.on('worker', (worker) => {
      if (!worker.url().includes('worker-entry')) return;
      compilers.add(worker);
      peak = Math.max(peak, compilers.size);
      worker.on('close', () => compilers.delete(worker));
    });
    page.on('pageerror', (error) => report.errors.push(error.message));
    page.on('console', (message) => {
      if (report.runtimeConsole.length >= 2000) {
        report.omittedConsoleEntries++;
        report.runtimeConsole.shift();
      }
      report.runtimeConsole.push({ family, at: Date.now(), type: message.type(), text: message.text().slice(0, 2000) });
    });
    page.on('request', (request) => {
      const path = new URL(request.url()).pathname;
      if (/^\/(?:emception|cdn)\//.test(path)) compilerRequests.push(path);
      if (/^\/api\//.test(path) || request.method() !== 'GET') report.academicRequests.push({ path, method: request.method() });
    });
    await page.goto(origin, { waitUntil: 'domcontentloaded' });
    await page.evaluate(async () => {
      const { createCodingExerciseValidationBrowserFixture } = await import('/bundle/exercise.js');
      window.exercise = createCodingExerciseValidationBrowserFixture(document.querySelector('#root'));
    });
    const key = await page.evaluate((family) => window.exercise.mount(family), family);
    await expect(page.locator('[data-authoring-context]')).toHaveAttribute('data-authoring-context', key);
    await expect(page.locator('.monaco-editor')).toBeVisible();
    const original = await page.evaluate(() => window.exercise.getDraft());
    assert.equal(compilerRequests.length, 0, 'editing booted compiler assets');
    assert.equal(compilers.size, 0);
    await page.getByRole('tab', { name: 'Problem', exact: true }).click();
    await page.getByLabel('Statement', { exact: true }).fill(`Edited ${family} problem: add two integers.`);
    await page.getByRole('tab', { name: 'Validate exercise', exact: true }).click();
    await page.getByRole('button', { name: 'Check all solutions', exact: true }).click();
    const reference = page
      .getByRole('region', { name: 'Exercise check results', exact: true })
      .locator('article')
      .filter({ has: page.getByText('Correct sum', { exact: true }) });
    await expect(reference).toBeVisible();
    assert.equal(await reference.getAttribute('data-solution-status'), 'reference-passed', await reference.innerText());
    await expect(reference).toContainText('2 passed / 0 failed / 2 total');
    report.solutionObservations.push({ family, role: 'reference', passed: 2, failed: 0, total: 2 });
    await persist();
    console.log(`[exercise-check] ${family} reference passed`);
    const incorrect = page
      .getByRole('region', { name: 'Exercise check results', exact: true })
      .locator('article')
      .filter({ has: page.getByText('Subtracts instead', { exact: true }) });
    await expect(incorrect).toBeVisible();
    assert.equal(await incorrect.getAttribute('data-solution-status'), 'incorrect-detected', await incorrect.innerText());
    await expect(incorrect).toContainText('0 passed / 2 failed / 2 total');
    report.solutionObservations.push({ family, role: 'incorrect', passed: 0, failed: 2, total: 2 });
    await persist();
    console.log(`[exercise-check] ${family} incorrect detected`);
    await expect(page.getByRole('button', { name: 'Check all solutions', exact: true })).toBeEnabled();
    await expect.poll(() => compilers.size).toBe(0);
    const results = page.getByRole('region', { name: 'Exercise check results', exact: true });
    await expect(results.locator('article')).toHaveCount(2);
    await expect(results.locator('article').first()).toContainText('2 passed / 0 failed / 2 total');
    await expect(results.locator('article').last()).toContainText('0 passed / 2 failed / 2 total');
    await expect(results).not.toContainText('Estimate:');
    const afterChecks = await page.evaluate(() => window.exercise.getDraft());
    assert.deepEqual(afterChecks.files, original.files, 'reference execution overwrote starter files');
    assert.deepEqual(afterChecks.validationSolutions, original.validationSolutions);
    await page.screenshot({ path: resolve(evidence, `${family}-checks-desktop.png`) });
    report.cases.push(`UX04-B-${family}`);
    await page.getByRole('tab', { name: 'Problem', exact: true }).click();
    await page.getByLabel('Statement', { exact: true }).fill(`Current ${family} problem after checking.`);
    await page.getByRole('tab', { name: 'Validate exercise', exact: true }).click();
    await expect(page.getByText('Outdated checks. Exercise changed.', { exact: true })).toBeVisible();
    await page.getByRole('button', { name: 'Learner preview', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Learner preview', exact: true })).toBeFocused();
    const preview = page.getByRole('region', { name: 'Learner preview workspace', exact: true });
    await expect(preview.getByText(`Current ${family} problem after checking.`, { exact: true })).toBeVisible();
    await expect(preview.getByText('Add both values.', { exact: true })).toBeVisible();
    await expect(preview.getByText('The values are between -10 and 10.', { exact: true })).toBeVisible();
    await expect(preview.getByRole('button', { name: /submit|release|approve|grade/i })).toHaveCount(0);
    assert.doesNotMatch(await preview.innerHTML(), /PRIVATE-|REFERENCE-CANARY|INCORRECT-CANARY/);
    const models = await page.evaluate(() => window.exercise.inspectEditorModels());
    assert.doesNotMatch(models.join('\n'), /PRIVATE-|REFERENCE-CANARY|INCORRECT-CANARY/);
    assert.ok(models.some((source) => source === original.files[0].content));
    await page.screenshot({ path: resolve(evidence, `${family}-preview-desktop.png`) });
    await page.setViewportSize({ width: 390, height: 844 });
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1)).toBe(true);
    await expect(preview.getByRole('button', { name: 'Run public tests', exact: true })).toBeInViewport();
    await page.screenshot({ path: resolve(evidence, `${family}-preview-mobile.png`) });
    report.layouts.push({ family, width: 390, preview: true });
    await page.setViewportSize({ width: 1440, height: 1000 });
    const solved = original.validationSolutions[0].files[0].content.replace('// REFERENCE-CANARY\n', '');
    await page.locator('.monaco-editor .view-lines').click();
    await page.keyboard.press('Control+KeyA');
    await page.keyboard.insertText(solved);
    await expect.poll(async () => (await page.evaluate(() => window.exercise.inspectEditorModels())).includes(solved)).toBe(true);
    await page.getByRole('button', { name: 'Run public tests', exact: true }).click();
    await expect(page.getByTestId('test-results-panel')).toContainText('1 passed');
    await expect(page.getByTestId('test-results-panel')).toContainText('1 total');
    console.log(`[exercise-check] ${family} public preview passed`);
    await expect.poll(() => compilers.size).toBe(0);
    await page.getByRole('button', { name: 'Back to authoring', exact: true }).click();
    await expect(page.getByRole('button', { name: 'Learner preview', exact: true })).toBeFocused();
    const beforeReopen = await page.evaluate(() => window.exercise.getDraft());
    assert.deepEqual(beforeReopen.files, original.files);
    assert.deepEqual(beforeReopen.validationSolutions, original.validationSolutions);
    await page.getByRole('button', { name: 'Learner preview', exact: true }).click();
    await expect(page.locator('.monaco-editor')).toBeVisible();
    const reopened = await page.evaluate(() => window.exercise.inspectEditorModels());
    assert.ok(reopened.includes(original.files[0].content), 'learner scratch persisted into a fresh preview');
    assert.ok(!reopened.includes(solved));
    const storage = await page.evaluate(() => ({ local: Object.keys(localStorage), session: Object.keys(sessionStorage) }));
    assert.deepEqual(storage, { local: [], session: [] });
    await page.getByRole('button', { name: 'Back to authoring', exact: true }).click();
    if (family === 'function') {
      await page.getByRole('button', { name: 'Check all solutions', exact: true }).click();
      await expect.poll(() => compilers.size).toBe(1);
      await page.getByRole('button', { name: 'Learner preview', exact: true }).click();
      await expect(page.getByRole('heading', { name: 'Learner preview', exact: true })).toBeVisible();
      assert.equal(compilers.size, 0, 'preview opened before previous runtime drained');
      await page.getByRole('button', { name: 'Back to authoring', exact: true }).click();
      report.cases.push('UX04-B-cancel');
      await page.getByRole('button', { name: 'Subtracts instead', exact: true }).click();
      await page.locator('.monaco-editor .view-lines').click();
      await page.keyboard.press('Control+KeyA');
      const invalidSource = '#error intentional_exercise_check_failure';
      await page.keyboard.insertText(invalidSource);
      await expect
        .poll(async () => (await page.evaluate(() => window.exercise.getDraft())).validationSolutions[1].files[0].content, { timeout: 5000 })
        .toBe(invalidSource);
      await page.getByRole('button', { name: 'Run selected solution', exact: true }).click();
      await expect(page.locator('[data-solution-status="error"]')).toContainText('Solution compilation failed');
      await expect(page.locator('[data-solution-status="incorrect-detected"]')).toHaveCount(0);
      await expect.poll(() => compilers.size).toBe(0);
      report.cases.push('UX04-B-build-failure');
    }
    await page.setViewportSize({ width: 390, height: 844 });
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1)).toBe(true);
    await expect(page.getByRole('tab', { name: 'Validate exercise', exact: true })).toBeInViewport();
    await page.screenshot({ path: resolve(evidence, `${family}-author-mobile.png`) });
    report.layouts.push({ family, width: 390, preview: false });
    assert.ok(peak <= 1, `observed ${peak} simultaneous compiler workers`);
    report.observations.push({
      family,
      fullCases: 2,
      publicCases: 1,
      peakWorkers: peak,
      remainingWorkers: compilers.size,
      protectedDataAbsentFromPreview: true,
      storageEntries: 0,
    });
    await page.evaluate(() => window.exercise.dispose());
    await expect.poll(() => compilers.size).toBe(0);
    await context.close();
    context = undefined;
    activePage = undefined;
    await browser.close();
    browser = undefined;
    await persist();
  }
  report.cases.push('UX04-B-redacted-preview', 'UX04-B-layout');
  assert.deepEqual([...report.cases].sort(), [...report.requiredCases].sort());
  assert.deepEqual(report.errors, []);
  assert.deepEqual(report.academicRequests, []);
  report.passed = true;
} catch (error) {
  report.errors.push(error instanceof Error ? error.stack : String(error));
  if (activePage && !activePage.isClosed()) await activePage.screenshot({ path: resolve(evidence, 'failed-screen.png'), fullPage: true });
  throw error;
} finally {
  await persist();
  await context?.close();
  await browser?.close();
  await new Promise((done, reject) => server.close((error) => (error ? reject(error) : done())));
  console.log(`[exercise-check] evidence: ${evidence}`);
}
