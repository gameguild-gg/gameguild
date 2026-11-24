import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { mkdir, mkdtemp, readFile, realpath, writeFile } from 'node:fs/promises';
import { createServer } from 'node:http';
import { extname, resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';
import { chromium, expect } from 'playwright/test';
import { buildCodingSurfaceFixture } from './coding-surface-fixture-build.mjs';
import { createCodeBrowserLaunchOptions } from '../../../tools/emception/grading/browser-launch-options.mjs';

const repo = fileURLToPath(new URL('../../../', import.meta.url));
const evidenceRoot = resolve(repo, 'apps/web/test-results/coding-surface-refoundation');
await mkdir(evidenceRoot, { recursive: true });
const evidence = await mkdtemp(resolve(evidenceRoot, 'authoring-'));
const output = resolve(evidence, 'bundle');
const monaco = await realpath(resolve(repo, 'apps/web/node_modules/monaco-editor/min/vs'));
const sourceFiles = [
  'authoring-types.ts',
  'authoring-model.ts',
  'authoring-fields.tsx',
  'authoring-problem.tsx',
  'authoring-environment.tsx',
  'authoring-starter.tsx',
  'authoring-tests.tsx',
  'authoring-surface.tsx',
  'authoring.css',
  'authoring-validation.tsx',
  'authoring-validation-results.tsx',
  'authoring-learner-preview.tsx',
  'exercise-validation.ts',
  'exercise-validation-types.ts',
  'exercise-preparation.ts',
  'coding-surface.tsx',
  'coding-workbench.tsx',
  'testing/authoring-browser.tsx',
];
const hash = (bytes) => createHash('sha256').update(bytes).digest('hex');
const report = {
  productionComponent: true,
  realMonaco: true,
  nextHost: false,
  persisted: false,
  officialGrading: false,
  passed: false,
  requiredCases: ['UX04-A-function', 'UX04-A-standard-io', 'UX04-A-layout', 'UX04-A-keyboard', 'UX04-B-preview-ui'],
  cases: [],
  errors: [],
  compilerRequests: [],
  compilerWorkers: 0,
  layouts: [],
  sourceHashes: Object.fromEntries(
    await Promise.all(sourceFiles.map(async (path) => [path, hash(await readFile(resolve(repo, 'packages/features/coding-surface/src', path)))])),
  ),
  harnessHash: hash(await readFile(fileURLToPath(import.meta.url))),
  fixtureBuilderHash: hash(await readFile(new URL('./coding-surface-fixture-build.mjs', import.meta.url))),
};
const persist = () => writeFile(resolve(evidence, 'report.json'), JSON.stringify(report, null, 2) + '\n');
await persist();
await buildCodingSurfaceFixture('authoring', output);
const server = createServer(async (request, response) => {
  response.setHeader('Cache-Control', 'no-store');
  try {
    const url = new URL(request.url, 'http://localhost');
    if (url.pathname === '/') {
      response.setHeader('Content-Type', 'text/html');
      response.end(
        '<!doctype html><html lang="en"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Coding authoring proof</title><link rel="stylesheet" href="/bundle/authoring.css"><style>*{box-sizing:border-box}body{margin:0;background:#09090b;color:#f4f4f5;font:14px system-ui}#root{width:100%;padding:16px 24px;min-width:0}button,input,select,textarea{font:inherit}button{color:inherit}</style><div id="root"></div></html>',
      );
      return;
    }
    const prefix = ['/bundle/', '/monaco/vs/'].find((value) => url.pathname.startsWith(value));
    if (!prefix) {
      response.writeHead(404).end();
      return;
    }
    const base = prefix === '/bundle/' ? output : monaco;
    const path = await realpath(resolve(base, decodeURIComponent(url.pathname.slice(prefix.length))));
    if (!path.startsWith(base + sep)) {
      response.writeHead(403).end();
      return;
    }
    response.setHeader(
      'Content-Type',
      { '.js': 'text/javascript', '.mjs': 'text/javascript', '.css': 'text/css', '.json': 'application/json', '.ttf': 'font/ttf' }[extname(path)] ??
        'application/octet-stream',
    );
    response.end(await readFile(path));
  } catch {
    response.writeHead(404).end();
  }
});
let browser;
let context;
try {
  await new Promise((done, reject) => {
    server.once('error', reject);
    server.listen(0, '127.0.0.1', done);
  });
  const address = server.address();
  assert.ok(address && typeof address !== 'string');
  const origin = `http://127.0.0.1:${address.port}`;
  browser = await chromium.launch(createCodeBrowserLaunchOptions(process.env.GAMEGUILD_CODE_WORKER_BROWSER_CHANNEL));
  context = await browser.newContext({ serviceWorkers: 'block', acceptDownloads: false, viewport: { width: 1440, height: 1000 } });
  await context.route('**/*', (route) => (new URL(route.request().url()).origin === origin ? route.continue() : route.abort('blockedbyclient')));
  await context.routeWebSocket('**/*', (socket) => socket.close());
  const page = await context.newPage();
  page.on('pageerror', (error) => report.errors.push(error.message));
  page.on('request', (request) => {
    if (/\/(?:emception|cdn)\//.test(new URL(request.url()).pathname)) report.compilerRequests.push(request.url());
  });
  page.on('worker', (worker) => {
    if (worker.url().includes('worker-entry')) report.compilerWorkers++;
  });
  await page.goto(origin, { waitUntil: 'domcontentloaded' });
  await page.evaluate(async () => {
    const { createCodingAuthoringBrowserFixture } = await import('/bundle/authoring.js');
    window.authoringFixture = createCodingAuthoringBrowserFixture(document.querySelector('#root'));
  });
  const task = (name) => page.getByRole('tab', { name, exact: true }).click();
  const command = (name) => page.getByRole('button', { name, exact: true }).click();
  async function layout(name, width, height) {
    await page.setViewportSize({ width, height });
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1)).toBe(true);
    const controls = await page
      .locator('.cs-author-panel input:visible, .cs-author-panel select:visible, .cs-author-panel textarea:visible, .cs-author-task-heading button:visible')
      .evaluateAll((elements) => elements.map((element) => ({ left: element.getBoundingClientRect().left, right: element.getBoundingClientRect().right })));
    assert.ok(
      controls.every((rect) => rect.left >= -1 && rect.right <= width + 1),
      'an authoring control escaped the viewport',
    );
    report.layouts.push({ name, width, height, controls: controls.length });
    await page.screenshot({ path: resolve(evidence, `${name}.png`), fullPage: true });
  }
  for (const family of ['function', 'standard-io']) {
    const contextKey = await page.evaluate((family) => window.authoringFixture.mount(family), family);
    await expect(page.locator('[data-authoring-context]')).toHaveAttribute('data-authoring-context', contextKey);
    await expect(page.getByRole('tab', { name: 'Problem', exact: true })).toHaveAttribute('aria-selected', 'true');
    await page.getByLabel('Statement', { exact: true }).fill('Add two integers.');
    await expect.poll(() => page.evaluate(() => window.authoringFixture.getDraft().problem.statement)).toBe('Add two integers.');
    await command('Add example');
    await page.getByLabel('Example input', { exact: true }).fill('2, 3');
    await page.getByLabel('Example output', { exact: true }).fill('5');
    await command('Add constraint');
    await page.getByLabel('Constraint 1', { exact: true }).fill('Small integer inputs.');
    await task('Starter code');
    if (family === 'function') {
      await page.getByLabel('Function name', { exact: true }).fill('sum');
      await command('Add parameter');
      await page.getByLabel('Parameter 1 name', { exact: true }).fill('left');
      await command('Add parameter');
      await page.getByLabel('Parameter 2 name', { exact: true }).fill('right');
      await command('Apply signature');
      await command('Confirm change');
      await expect(page.locator('.cs-author-signature > summary')).toBeFocused();
    }
    await command('Generate starter');
    await expect(page.locator('.monaco-editor')).toBeVisible({ timeout: 30_000 });
    await page.locator('.monaco-editor .view-lines').click();
    await page.keyboard.press('Control+KeyA');
    const source =
      family === 'function'
        ? 'int sum(int left, int right) { return left + right; }'
        : '#include <cstdio>\nint main() { int a, b; if (std::scanf("%d %d", &a, &b) != 2) return 1; std::printf("%d\\n", a + b); return 0; }';
    await page.keyboard.insertText(source);
    await expect.poll(() => page.evaluate(() => window.authoringFixture.getDraft().files[0].content)).toBe(source);
    await layout(`${family}-source-desktop`, 1440, 1000);
    await layout(`${family}-source-mobile`, 390, 844);
    await page.setViewportSize({ width: 1440, height: 1000 });
    await task('Tests');
    await command('Add test');
    await page.getByLabel('Test name', { exact: true }).fill('Two plus three');
    if (family === 'function') {
      await page.getByLabel('left (integer)', { exact: true }).fill('2');
      await page.getByLabel('right (integer)', { exact: true }).fill('3');
      await page.getByLabel('Expected result (integer)', { exact: true }).fill('5');
    } else {
      await page.getByLabel('Standard input', { exact: true }).fill('2 3\n');
      await page.getByLabel('Expected stdout', { exact: true }).fill('5\n');
    }
    await command('Duplicate test');
    await page.getByLabel('Case visibility', { exact: true }).selectOption('private');
    const before = await page.evaluate(() => JSON.stringify(window.authoringFixture.getDraft()));
    await command('Delete test');
    await page.keyboard.press('Escape');
    assert.equal(await page.evaluate(() => JSON.stringify(window.authoringFixture.getDraft())), before);
    await layout(`${family}-tests-wide`, 1920, 1080);
    await layout(`${family}-tests-mobile`, 390, 844);
    await page.setViewportSize({ width: 1440, height: 1000 });
    await task('Problem');
    await expect(page.getByLabel('Statement', { exact: true })).toHaveValue('Add two integers.');
    await command('Check fields');
    await expect(page.getByRole('status')).toHaveText('Exercise fields are complete.');
    await task('Validate exercise');
    await command('Add reference solution');
    await expect(page.locator('.monaco-editor')).toBeVisible();
    await command('Learner preview');
    await expect(page.getByRole('heading', { name: 'Learner preview', exact: true })).toBeFocused();
    await expect(page.locator('.monaco-editor')).toBeVisible();
    await page.locator('.monaco-editor .view-lines').click();
    await page.keyboard.press('Control+KeyA');
    await page.keyboard.insertText('scratch must not persist');
    await command('Back to authoring');
    await expect(page.locator('.monaco-editor')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Learner preview', exact: true })).toBeFocused();
    report.cases.push(`UX04-A-${family}`);
  }
  await page.getByRole('tab', { name: 'Problem', exact: true }).focus();
  await page.keyboard.press('End');
  await expect(page.getByRole('tab', { name: 'Validate exercise', exact: true })).toBeFocused();
  await page.keyboard.press('Home');
  await expect(page.getByRole('tab', { name: 'Problem', exact: true })).toBeFocused();
  await page.getByLabel('Exercise type', { exact: true }).selectOption('function');
  await expect(page.getByRole('button', { name: 'Cancel', exact: true })).toBeFocused();
  await page.keyboard.press('Shift+Tab');
  await expect(page.getByRole('button', { name: 'Confirm change', exact: true })).toBeFocused();
  await page.keyboard.press('Escape');
  await expect(page.getByRole('dialog')).toHaveCount(0);
  report.cases.push('UX04-A-keyboard', 'UX04-A-layout', 'UX04-B-preview-ui');
  assert.equal(report.compilerRequests.length, 0, 'authoring fetched compiler assets');
  assert.equal(report.compilerWorkers, 0, 'authoring booted a compiler');
  assert.deepEqual(await page.evaluate(() => [localStorage.length, sessionStorage.length]), [0, 0]);
  assert.deepEqual(report.errors, []);
  assert.deepEqual([...report.cases].sort(), [...report.requiredCases].sort());
  await page.evaluate(() => window.authoringFixture.dispose());
  await expect(page.getByRole('region', { name: 'Coding exercise authoring' })).toHaveCount(0);
  report.passed = true;
} catch (error) {
  report.errors.push(error instanceof Error ? (error.stack ?? error.message) : String(error));
  process.exitCode = 1;
} finally {
  await context?.close();
  await browser?.close();
  await new Promise((done) => server.close(done));
  await persist();
  console.log(`[coding-authoring] ${report.passed ? 'passed' : 'failed'}: ${resolve(evidence, 'report.json')}`);
}
