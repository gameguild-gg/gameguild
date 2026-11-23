import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { createServer } from 'node:http';
import { mkdir, mkdtemp, readFile, realpath, writeFile } from 'node:fs/promises';
import { extname, resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';
import { chromium, expect as baseExpect } from 'playwright/test';
import { build } from 'vite';
import { createCodeBrowserLaunchOptions } from '../../../tools/emception/grading/browser-launch-options.mjs';
import { verifyCodingCycleToolchain } from './coding-cycle-toolchain-support.mjs';

// Cold preparation is bounded at 120 s by the owned executor, not Playwright's 5 s default.
const expect = baseExpect.configure({ timeout: 125_000 });

const repo = fileURLToPath(new URL('../../../', import.meta.url));
const evidenceRoot = resolve(repo, 'apps/web/test-results/coding-surface-refoundation/runtime');
await mkdir(evidenceRoot, { recursive: true });
const evidence = await mkdtemp(resolve(evidenceRoot, 'workbench-'));
const runtime = resolve(evidence, 'runtime');
const assets = await realpath(resolve(repo, 'apps/web/public/emception'));
const monaco = await realpath(resolve(repo, 'apps/web/node_modules/monaco-editor/min/vs'));
await verifyCodingCycleToolchain(resolve(assets, 'manifest.json'));
const hash = bytes => createHash('sha256').update(bytes).digest('hex');
const sources = ['coding-surface.tsx', 'coding-workbench.tsx', 'editor-files.ts', 'program-output.tsx', 'preview-runner.ts', 'session.ts', 'workbench-session.ts', 'workbench-session-types.ts', 'types.ts', 'runtime-session.ts', 'runtime-types.ts', 'runtime-capabilities.ts', 'runtime-capability-types.ts', 'execution-plan.ts', 'program-session.ts', 'program-session-types.ts', 'testing/workbench-browser.tsx'];
const report = {
  productionComponent: true, nextHost: false, officialGrading: false, passed: false,
  localMonacoLoader: true, coldContextPerControl: true, compilerMemoryPlateau: false,
  cases: ['CS-REF-10-text-workbench', 'CS-REF-11-text-workbench', 'CS-REF-12-editor-cycles'],
  observations: [], errors: [], editorCycles: [],
  sourceHashes: Object.fromEntries(await Promise.all(sources.map(async path => [path, hash(await readFile(resolve(repo, 'packages/features/coding-surface/src', path)))]))),
  sdkBuiltHashes: Object.fromEntries(await Promise.all(['createEmception.js', 'presets.js', 'worker-client.js'].map(async path => [path, hash(await readFile(resolve(repo, 'tools/emception/packages/browser/dist', path)))]))),
  manifestHash: hash(await readFile(resolve(assets, 'manifest.json'))), harnessHash: hash(await readFile(fileURLToPath(import.meta.url))),
  sharedComposerHash: hash(await readFile(resolve(repo, 'packages/features/coding/src/native-function-support.ts'))),
};
const persist = () => writeFile(resolve(evidence, 'report.json'), JSON.stringify(report, null, 2) + '\n');
await persist();
await build({
  configFile: false, root: resolve(repo, 'apps/web'), base: '/runtime/', publicDir: false,
  define: { 'process.env.NODE_ENV': JSON.stringify('production') },
  resolve: { dedupe: ['react', 'react-dom'] },
  build: { outDir: runtime, emptyOutDir: false, target: 'es2022', minify: false,
    lib: { entry: resolve(repo, 'packages/features/coding-surface/src/testing/workbench-browser.tsx'), formats: ['es'], fileName: () => 'workbench.js', cssFileName: 'workbench' } },
  worker: { format: 'es' },
});
const assetRequests = [];
const server = createServer(async (request, response) => {
  response.setHeader('Cross-Origin-Opener-Policy', 'same-origin');
  response.setHeader('Cross-Origin-Embedder-Policy', 'require-corp');
  response.setHeader('Cache-Control', 'no-store');
  try {
    const url = new URL(request.url, 'http://localhost');
    if (url.pathname === '/') {
      response.setHeader('Content-Type', 'text/html');
      response.end('<!doctype html><html lang="en"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Coding workbench proof</title><link rel="stylesheet" href="/runtime/workbench.css"><style>*{box-sizing:border-box}body{margin:0;background:#09090b;color:#f4f4f5;font:14px system-ui}#root{height:100dvh;min-height:640px;width:100%}button,input,textarea,select{font:inherit;color:inherit;background:#27272a;border:1px solid #52525b;padding:5px 8px;border-radius:4px}button:disabled{opacity:.5}input[type=radio]{accent-color:#38bdf8}</style><div id="root"></div></html>');
      return;
    }
    const prefix = ['/runtime/', '/emception/', '/cdn/', '/monaco/vs/'].find(value => url.pathname.startsWith(value));
    if (!prefix) { response.writeHead(404).end(); return; }
    const directory = prefix === '/runtime/' ? runtime : prefix === '/monaco/vs/' ? monaco : assets;
    if (directory === assets) assetRequests.push(url.pathname);
    const path = await realpath(resolve(directory, decodeURIComponent(url.pathname.slice(prefix.length))));
    if (!path.startsWith(directory + sep)) { response.writeHead(403).end(); return; }
    response.setHeader('Content-Type', ({ '.js': 'text/javascript', '.mjs': 'text/javascript', '.json': 'application/json', '.wasm': 'application/wasm', '.css': 'text/css', '.ttf': 'font/ttf' })[extname(path)] ?? 'application/octet-stream');
    response.end(await readFile(path));
  } catch { response.writeHead(404).end(); }
});
let browser;
let context;
try {
  await new Promise((done, reject) => { server.once('error', reject); server.listen(0, '127.0.0.1', done); });
  const address = server.address();
  assert.ok(address && typeof address !== 'string');
  const origin = `http://127.0.0.1:${address.port}`;
  browser = await chromium.launch(createCodeBrowserLaunchOptions(process.env.GAMEGUILD_CODE_WORKER_BROWSER_CHANNEL));

  async function open() {
    context = await browser.newContext({ serviceWorkers: 'block', acceptDownloads: false, viewport: { width: 1440, height: 960 } });
    await context.route('**/*', route => new URL(route.request().url()).origin === origin ? route.continue() : route.abort('blockedbyclient'));
    await context.routeWebSocket('**/*', socket => socket.close());
    const page = await context.newPage();
    page.setDefaultTimeout(120_000);
    const compilers = new Set();
    let peakCompilers = 0;
    page.on('worker', worker => {
      if (!worker.url().includes('worker-entry')) return;
      compilers.add(worker);
      peakCompilers = Math.max(peakCompilers, compilers.size);
      worker.on('close', () => compilers.delete(worker));
    });
    page.on('pageerror', error => report.errors.push(error.message));
    await page.goto(origin, { waitUntil: 'domcontentloaded' });
    await page.evaluate(async () => {
      const { createCodingWorkbenchBrowserFixture } = await import('/runtime/workbench.js');
      window.workbench = createCodingWorkbenchBrowserFixture(document.querySelector('#root'));
    });
    return { page, compilers, peakCompilers: () => peakCompilers, metrics: await context.newCDPSession(page) };
  }
  async function mount(page, kind, key) {
    await page.evaluate(({ kind, key }) => window.workbench.mount(kind, key), { kind, key });
    await page.waitForFunction(() => window.workbench.ready());
    await expect(page.locator('.monaco-editor')).toBeVisible();
  }
  async function edit(page, source) {
    await page.locator('.monaco-editor .view-lines').click();
    await page.keyboard.press('Control+KeyA');
    await page.keyboard.insertText(source);
    await expect.poll(async () => (await page.evaluate(() => window.workbench.getSubmission())).files.map(file => file.content).join('\n')).toContain(source);
  }
  async function drained(page, compilers) {
    await expect.poll(() => compilers.size).toBe(0);
    await expect(page.getByRole('button', { name: 'Run public tests', exact: true })).toBeEnabled();
  }

  const editing = await open();
  for (let cycle = 0; cycle < 5; cycle++) {
    await mount(editing.page, 'io', `offline-${cycle}`);
    await edit(editing.page, `int main() { return ${cycle}; }`);
    assert.equal(assetRequests.length, 0, 'editing or offline capture acquired compiler assets');
    assert.equal(editing.compilers.size, 0);
    if (cycle === 0) {
      await editing.page.screenshot({ path: resolve(evidence, 'editor-desktop.png') });
      await editing.page.setViewportSize({ width: 390, height: 844 });
      await expect.poll(() => editing.page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1)).toBe(true);
      await editing.page.screenshot({ path: resolve(evidence, 'editor-mobile.png') });
      await editing.page.setViewportSize({ width: 1440, height: 960 });
    }
    await editing.page.evaluate(() => window.workbench.unmount());
    await expect(editing.page.getByTestId('coding-workbench')).toHaveCount(0);
    await editing.metrics.send('HeapProfiler.collectGarbage');
    report.editorCycles.push({ cycle, compilers: editing.compilers.size,
      dom: await editing.metrics.send('Memory.getDOMCounters'),
      heap: await editing.page.evaluate(() => performance.memory.usedJSHeapSize) });
  }
  const warmed = report.editorCycles.slice(1);
  assert.ok(warmed.at(-1).heap - warmed[0].heap < 8 * 1024 * 1024, 'unexecuted editor mount heap did not plateau');
  assert.ok(warmed.at(-1).dom.nodes - warmed[0].dom.nodes < 100, 'unexecuted editor DOM grew across mounts');
  await context.close(); context = undefined;

  const controls = [
    { id: 'interactive', kind: 'io' }, { id: 'cancel-input', kind: 'io' }, { id: 'context-switch', kind: 'io' },
    { id: 'io-pass', kind: 'io', expected: '1 passed' },
    { id: 'io-wrong', kind: 'io', source: '#include <cstdio>\nint main(){std::puts("wrong");}', expected: '1 failed' },
    { id: 'function-pass', kind: 'function', expected: '1 passed' },
    { id: 'function-wrong', kind: 'function', source: 'int sum(int left, int right){return left-right;}', expected: '1 failed' },
    { id: 'compile-error', kind: 'io', source: 'int main( { invalid', expected: '1 failed', diagnostic: /Compilation failed/ },
  ];
  for (const control of controls) {
    console.log(`[coding-workbench] ${control.id}`);
    const current = await open();
    const { page, compilers } = current;
    await mount(page, control.kind, control.id);
    if (control.source) await edit(page, control.source);
    const start = Date.now();
    if (control.id === 'interactive' || control.id === 'cancel-input' || control.id === 'context-switch') {
      await page.getByLabel('Interactive', { exact: true }).check();
      await page.getByRole('button', { name: 'Run program', exact: true }).click();
      await expect(page.getByTestId('program-stdout')).toContainText('Name?');
      await expect(page.getByRole('button', { name: 'Run public tests', exact: true })).toBeDisabled();
      if (control.id === 'interactive') {
        await page.getByLabel('Interactive input', { exact: true }).fill('Ana');
        await page.getByRole('button', { name: 'Send', exact: true }).click();
        await expect(page.getByTestId('program-stdout')).toContainText('Hello, Ana');
        await expect(page.getByRole('status').filter({ hasText: 'Program: completed (exit 0)' })).toBeVisible();
        await page.getByRole('tab', { name: 'Standard error', exact: true }).click();
        await expect(page.getByTestId('program-stderr')).toContainText('input received');
        await page.screenshot({ path: resolve(evidence, 'terminal-desktop.png') });
      } else if (control.id === 'cancel-input') {
        await page.getByRole('button', { name: 'Stop', exact: true }).click();
        await expect(page.getByRole('status').filter({ hasText: 'Program: stopped' })).toBeVisible();
      } else {
        await mount(page, 'function', 'replacement-function-context');
        await expect(page.getByTestId('program-stdout')).toHaveCount(0);
        await expect(page.getByTestId('test-results-panel')).toHaveCount(0);
        await page.getByRole('button', { name: 'Run public tests', exact: true }).click();
        await expect(page.getByTestId('test-results-panel')).toContainText('1 passed');
      }
      if (control.id !== 'context-switch') await expect(page.getByTestId('test-results-panel')).toHaveCount(0);
    } else {
      await page.getByRole('button', { name: 'Run public tests', exact: true }).click();
      await expect(page.getByTestId('test-results-panel')).toContainText(control.expected);
      await expect(page.getByTestId('test-results-panel')).toContainText('not an official grade');
      if (control.diagnostic) {
        await page.getByTestId('test-case-0').click();
        await expect(page.getByTestId('test-case-diagnostic-0')).toContainText(control.diagnostic);
      }
      if (control.id === 'io-pass') {
        await edit(page, 'int main(){return 7;}');
        await expect(page.getByRole('status').filter({ hasText: 'Outdated result.' })).toBeVisible();
        await page.screenshot({ path: resolve(evidence, 'results-desktop.png') });
      }
    }
    await drained(page, compilers);
    assert.equal(current.peakCompilers(), 1, 'compiler runtime did not acquire exactly one worker at a time');
    report.observations.push({ id: control.id, elapsedMs: Date.now() - start, peakCompilers: current.peakCompilers(), retainedCompilers: compilers.size });
    await persist();
    await page.evaluate(() => window.workbench.dispose());
    await context.close(); context = undefined;
  }
  assert.deepEqual(report.errors, [], 'uncaught browser exceptions occurred');
  report.passed = true;
  console.log(`[coding-workbench] passed; evidence: ${evidence}`);
} catch (error) {
  report.failure = error instanceof Error ? { message: error.message, stack: error.stack } : { message: String(error) };
  try { await context?.pages()[0]?.screenshot({ path: resolve(evidence, 'failure.png') }); } catch { /* Preserve the original failure if the OS guard closed the page. */ }
  process.exitCode = 1;
  console.error(`[coding-workbench] failed: ${report.failure.message}; evidence: ${evidence}`);
} finally {
  try { await context?.close(); } finally {
    try { await browser?.close(); } finally {
      if (server.listening) await new Promise((done, reject) => server.close(error => error ? reject(error) : done()));
      await persist();
    }
  }
}
