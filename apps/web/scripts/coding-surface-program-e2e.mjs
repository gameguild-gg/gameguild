import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { createServer } from 'node:http';
import { mkdir, mkdtemp, readFile, realpath, writeFile } from 'node:fs/promises';
import { extname, resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';
import { chromium } from 'playwright';
import { build } from 'vite';
import { createCodeBrowserLaunchOptions } from '../../../tools/emception/grading/browser-launch-options.mjs';
import { verifyCodingCycleToolchain } from './coding-cycle-toolchain-support.mjs';

// One isolated runtime calibration. No Next/API build, users or course writes.
const repo = fileURLToPath(new URL('../../../', import.meta.url));
const evidenceRoot = resolve(repo, 'apps/web/test-results/coding-surface-refoundation/runtime');
await mkdir(evidenceRoot, { recursive: true });
const evidence = await mkdtemp(resolve(evidenceRoot, 'program-'));
const runtime = resolve(evidence, 'runtime');
const assets = await realpath(resolve(repo, 'apps/web/public/emception'));
await verifyCodingCycleToolchain(resolve(assets, 'manifest.json'));
const hash = bytes => createHash('sha256').update(bytes).digest('hex');
const sources = ['program-session.ts', 'program-session-types.ts', 'runtime-session.ts', 'runtime-types.ts', 'testing/program-browser.ts'];
const report = {
  candidateOnly: true, productionUi: false, officialGrading: false, passed: false,
  coldContextPerControl: true, repeatedMountPlateau: false,
  cases: ['CS-REF-11-text-port', 'CS-REF-12-text-port'], observations: [], errors: [],
  sourceHashes: Object.fromEntries(await Promise.all(sources.map(async path => [path, hash(await readFile(resolve(repo, 'packages/features/coding-surface/src', path)))]))),
  sdkBuiltHashes: Object.fromEntries(await Promise.all(['createEmception.js', 'presets.js', 'worker-client.js'].map(async path => [path, hash(await readFile(resolve(repo, 'tools/emception/packages/browser/dist', path)))]))),
  manifestHash: hash(await readFile(resolve(assets, 'manifest.json'))),
  harnessHash: hash(await readFile(fileURLToPath(import.meta.url))),
};
async function persist() { await writeFile(resolve(evidence, 'report.json'), JSON.stringify(report, null, 2) + '\n'); }
await persist();
await build({
  configFile: false, root: resolve(repo, 'apps/web'), base: '/runtime/', publicDir: false,
  build: { outDir: runtime, emptyOutDir: false, target: 'es2022', minify: false,
    lib: { entry: resolve(repo, 'packages/features/coding-surface/src/testing/program-browser.ts'), formats: ['es'], fileName: () => 'program.js' } },
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
      response.end('<!doctype html><html lang="en"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Program input calibration</title><main><h1>Program input calibration</h1><label>Program input<textarea id="stdin"></textarea></label><button id="send">Send input</button><button id="eof">End input</button></main></html>');
      return;
    }
    const prefix = ['/runtime/', '/emception/', '/cdn/'].find(value => url.pathname.startsWith(value));
    if (!prefix) { response.writeHead(404).end(); return; }
    const directory = prefix === '/runtime/' ? runtime : assets;
    if (directory === assets) assetRequests.push(url.pathname);
    const path = await realpath(resolve(directory, decodeURIComponent(url.pathname.slice(prefix.length))));
    if (!path.startsWith(directory + sep)) { response.writeHead(403).end(); return; }
    response.setHeader('Content-Type', ({ '.js': 'text/javascript', '.mjs': 'text/javascript', '.json': 'application/json', '.wasm': 'application/wasm' })[extname(path)] ?? 'application/octet-stream');
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
  const cases = [
    { id: 'interactive', control: 'prompt', input: { kind: 'interactive' } },
    { id: 'batch', control: 'prompt', input: { kind: 'batch', text: 'Ada\n' } },
    { id: 'batch-eof', control: 'prompt', input: { kind: 'batch', text: '' } },
    { id: 'cancel-input', control: 'prompt', input: { kind: 'interactive' } },
    { id: 'compile-error', control: 'compile-error', input: { kind: 'batch', text: '' } },
    { id: 'execution-timeout', control: 'infinite', input: { kind: 'batch', text: '' } },
  ];
  for (const testCase of cases) {
    context = await browser.newContext({ serviceWorkers: 'block', acceptDownloads: false });
    await context.route('**/*', route => new URL(route.request().url()).origin === origin ? route.continue() : route.abort('blockedbyclient'));
    await context.routeWebSocket('**/*', socket => socket.close());
    const page = await context.newPage();
    const metrics = await context.newCDPSession(page);
    page.setDefaultTimeout(180_000);
    const workers = new Set();
    page.on('worker', worker => { workers.add(worker); worker.on('close', () => workers.delete(worker)); });
    page.on('pageerror', error => report.errors.push(error.message));
    await page.goto(origin, { waitUntil: 'domcontentloaded' });
    console.log(`[coding-program] ${testCase.id}`);
    const start = Date.now();
    const before = assetRequests.length;
    await page.evaluate(async current => {
      const { createProgramBrowserFixture } = await import('/runtime/program.js');
      window.program = createProgramBrowserFixture(current.id, current.control);
      document.querySelector('#send').onclick = () => window.program.writeInput(document.querySelector('#stdin').value);
      document.querySelector('#eof').onclick = () => window.program.endInput();
    }, testCase);
    assert.equal(workers.size, 0, 'candidate construction started a worker');
    assert.equal(assetRequests.length, before, 'candidate construction fetched runtime assets');
    await page.evaluate(input => {
      window.completion = window.program.run(input).then(() => null, error => error.message);
    }, testCase.input);
    if (testCase.input.kind === 'interactive') {
      await page.waitForFunction(() => {
        const current = window.program.getState();
        return current.waitingForInput && current.output.stdout === 'Name? ';
      }, undefined, { timeout: 20_000 });
      const prompted = await page.evaluate(() => window.program.getState());
      assert.equal(prompted.output.stdout, 'Name? ');
      assert.equal(prompted.status, 'running');
      await page.waitForTimeout(100);
      assert.equal(await page.evaluate(() => window.program.getState().waitingForInput), true, 'stdin did not actually wait for user input');
      if (testCase.id === 'interactive') {
        await page.getByLabel('Program input').fill('Ana\n');
        await page.getByRole('button', { name: 'Send input', exact: true }).click();
      } else await page.evaluate(() => window.program.stop());
    }
    const failure = await page.evaluate(() => window.completion);
    const state = await page.evaluate(() => window.program.getState());
    if (testCase.id === 'batch' || testCase.id === 'interactive') {
      assert.equal(failure, null);
      assert.equal(state.status, 'completed');
      assert.equal(state.exitCode, 0);
      assert.equal(state.output.stdout, `Name? Hello, ${testCase.id === 'batch' ? 'Ada' : 'Ana'}\n`);
      assert.equal(state.output.stderr, 'input received\n');
    } else if (testCase.id === 'batch-eof') {
      assert.equal(failure, null);
      assert.equal(state.status, 'completed');
      assert.equal(state.exitCode, 3);
      assert.equal(state.output.stdout, 'Name? ');
    } else if (testCase.id === 'cancel-input') {
      assert.match(failure, /cancelled/);
      assert.equal(state.status, 'stopped');
    } else {
      assert.match(failure, new RegExp(testCase.id === 'compile-error' ? 'compile-failed' : 'execution-timeout'));
      assert.equal(state.status, 'failed');
      assert.equal(state.reason, testCase.id === 'compile-error' ? 'compile-failed' : 'execution-timeout');
    }
    assert.equal(state.waitingForInput, false);
    assert.equal('score' in state || 'passed' in state, false);
    await page.evaluate(async () => { await window.program.dispose(); window.program = null; window.completion = null; });
    const deadline = Date.now() + 5000;
    while (workers.size && Date.now() < deadline) await page.waitForTimeout(25);
    assert.equal(workers.size, 0, 'an owned runtime worker survived disposal');
    // A fresh compiler is expensive; do not carry collectible prior buffers into it.
    await metrics.send('HeapProfiler.collectGarbage');
    report.observations.push({ id: testCase.id, state, failure, elapsedMs: Date.now() - start, activeWorkers: workers.size });
    await persist();
    await context.close();
    context = undefined;
  }
  assert.deepEqual(report.errors, [], 'uncaught browser exceptions occurred');
  report.passed = true;
  console.log(`[coding-program] passed; evidence: ${evidence}`);
} catch (error) {
  try { report.lastState = await context?.pages()[0]?.evaluate(() => window.program?.getState()); }
  catch { /* A resource stop can close the browser before state is observable. */ }
  report.failure = error instanceof Error ? { message: error.message, stack: error.stack } : { message: String(error) };
  process.exitCode = 1;
  console.error(`[coding-program] failed: ${report.failure.message}; evidence: ${evidence}`);
} finally {
  try { await context?.close(); } finally {
    try { await browser?.close(); } finally {
      if (server.listening) await new Promise((done, reject) => server.close(error => error ? reject(error) : done()));
      await persist();
    }
  }
}
