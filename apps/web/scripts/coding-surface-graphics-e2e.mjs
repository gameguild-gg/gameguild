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

// Isolated public-API runtime proof: no Next/API build, accounts or course writes.
const repo = fileURLToPath(new URL('../../../', import.meta.url));
const evidenceRoot = resolve(repo, 'apps/web/test-results/coding-surface-refoundation/runtime');
await mkdir(evidenceRoot, { recursive: true });
const evidence = await mkdtemp(resolve(evidenceRoot, 'graphics-'));
const runtime = resolve(evidence, 'runtime');
const assets = await realpath(resolve(repo, 'apps/web/public/emception'));
await verifyCodingCycleToolchain(resolve(assets, 'manifest.json'));
const hash = bytes => createHash('sha256').update(bytes).digest('hex');
const sources = ['graphics-session.ts', 'graphics-session-types.ts', 'runtime-session.ts', 'runtime-types.ts', 'runtime-capabilities.ts', 'testing/graphics-browser.ts'];
const report = {
  candidateOnly: true, cases: ['CS-REF-10-graphics-port', 'CS-REF-12-graphics-port'], passed: false,
  productionUi: false, officialGrading: false, observations: [], errors: [], channelChecks: [],
  sourceHashes: Object.fromEntries(await Promise.all(sources.map(async path => [path, hash(await readFile(resolve(repo, 'packages/features/coding-surface/src', path)))]))),
  sdkSourceHashes: Object.fromEntries(await Promise.all(['canvas-runtime.ts', 'canvas-wasi.ts'].map(async path => [path, hash(await readFile(resolve(repo, 'tools/emception/packages/browser/src', path)))]))),
  sdkBuiltHashes: Object.fromEntries(await Promise.all(['canvas-runtime.js', 'canvas-wasi.js'].map(async path => [path, hash(await readFile(resolve(repo, 'tools/emception/packages/browser/dist', path)))]))),
  manifestHash: hash(await readFile(resolve(assets, 'manifest.json'))),
  harnessHash: hash(await readFile(fileURLToPath(import.meta.url))),
};
async function persist() { await writeFile(resolve(evidence, 'report.json'), JSON.stringify(report, null, 2) + '\n'); }
await persist();
await build({
  configFile: false, root: resolve(repo, 'apps/web'), base: '/runtime/', publicDir: false,
  build: {
    outDir: runtime, emptyOutDir: false, target: 'es2022', minify: false,
    lib: { entry: resolve(repo, 'packages/features/coding-surface/src/testing/graphics-browser.ts'), formats: ['es'], fileName: () => 'graphics.js' },
  },
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
      response.end('<!doctype html><html lang="en"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Local graphics runtime proof</title><style>body{margin:0;padding:16px;background:#111418;color:#eee;font:14px system-ui}h1{font-size:20px;margin:0 0 12px}main{max-width:960px}canvas{display:block;width:100%;height:auto;aspect-ratio:16/9;outline:1px solid #666}canvas:focus{outline:2px solid #39b6df}</style><main><h1>Local graphics runtime proof</h1><div id="target"></div></main></html>');
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
let page;
async function browserRss(connection) {
  const { processInfo } = await connection.send('SystemInfo.getProcessInfo');
  assert.ok(processInfo.length > 0, 'browser process metrics are unavailable');
  const measurements = await Promise.all(processInfo.map(async process => {
    try {
      const status = await readFile(`/proc/${process.id}/status`, 'utf8');
      const match = status.match(/^VmRSS:\s+(\d+)\s+kB/m);
      return match ? Number(match[1]) * 1024 : 0;
    } catch (error) {
      if (error.code === 'ENOENT' || error.code === 'ESRCH') return 0;
      throw error;
    }
  }));
  const rss = measurements.reduce((sum, bytes) => sum + bytes, 0);
  assert.ok(rss > 0, 'browser RSS could not be measured');
  return rss;
}
async function pixelSnapshot() {
  return page.evaluate(async () => {
    const { inspectGraphicsPixels } = await import('/runtime/graphics.js');
    return inspectGraphicsPixels(document.querySelector('canvas'));
  });
}
async function waitForBackground(expected) {
  const deadline = Date.now() + 3000;
  let observed;
  do {
    observed = await pixelSnapshot();
    if (JSON.stringify(observed.background) === JSON.stringify(expected)) return observed;
    await page.waitForTimeout(50);
  } while (Date.now() < deadline);
  assert.deepEqual(observed.background, expected, 'compiled canvas did not render the expected background');
  return observed;
}
try {
  await new Promise((done, reject) => { server.once('error', reject); server.listen(0, '127.0.0.1', done); });
  const address = server.address();
  assert.ok(address && typeof address !== 'string');
  const origin = `http://127.0.0.1:${address.port}`;
  browser = await chromium.launch(createCodeBrowserLaunchOptions(process.env.GAMEGUILD_CODE_WORKER_BROWSER_CHANNEL));
  const browserConnection = await browser.newBrowserCDPSession();
  context = await browser.newContext({ viewport: { width: 1280, height: 800 }, serviceWorkers: 'block', acceptDownloads: false });
  await context.route('**/*', route => new URL(route.request().url()).origin === origin ? route.continue() : route.abort('blockedbyclient'));
  await context.routeWebSocket('**/*', socket => socket.close());
  page = await context.newPage();
  page.on('pageerror', error => report.errors.push(error.message));
  const connection = await context.newCDPSession(page);
  await page.goto(origin, { waitUntil: 'domcontentloaded' });
  const memory = [];
  for (let cycle = 0; cycle < 5; cycle++) {
    console.log(`[coding-graphics] cycle ${cycle + 1}/5`);
    const initial = await page.evaluate(async key => {
      const { createGraphicsBrowserFixture } = await import('/runtime/graphics.js');
      const canvas = document.createElement('canvas');
      canvas.id = 'canvas'; canvas.width = 320; canvas.height = 180; canvas.tabIndex = 0;
      canvas.setAttribute('aria-label', 'Program graphics');
      document.getElementById('target').replaceChildren(canvas);
      globalThis.graphicsProbe = createGraphicsBrowserFixture(canvas, key);
      return { isolated: crossOriginIsolated, state: globalThis.graphicsProbe.session.getState() };
    }, `candidate-${cycle}`);
    assert.equal(initial.isolated, true);
    assert.equal(initial.state.status, 'idle');
    assert.equal(page.workers().length, 0);
    if (cycle === 0) assert.equal(assetRequests.length, 0, 'mounting the port fetched compiler assets');
    const started = Date.now();
    await page.evaluate(() => globalThis.graphicsProbe.run());
    const before = await waitForBackground([20, 48, 80, 255]);
    assert.deepEqual(before.background, [20, 48, 80, 255]);
    assert.equal(before.marker.length, 20);
    await page.waitForTimeout(125);
    const moved = await pixelSnapshot();
    assert.notDeepEqual(moved.marker, before.marker, 'real compiled animation did not move');
    await page.locator('canvas').press('Space');
    await page.waitForTimeout(100);
    const afterInput = await waitForBackground([170, 45, 65, 255]);
    assert.deepEqual(afterInput.background, [170, 45, 65, 255], 'keyboard input did not reach the compiled program');
    if (cycle === 0) {
      await page.screenshot({ path: resolve(evidence, 'desktop.png') });
      await page.setViewportSize({ width: 390, height: 844 });
      const mobile = await pixelSnapshot();
      assert.deepEqual(mobile.background, [170, 45, 65, 255]);
      assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
      await page.screenshot({ path: resolve(evidence, 'mobile.png') });
      await page.setViewportSize({ width: 1280, height: 800 });
    }
    const live = await page.evaluate(() => globalThis.graphicsProbe.session.getState());
    assert.equal(live.status, 'running');
    // Keep this independent failure visible while still measuring teardown.
    report.channelChecks.push({ cycle, passed: live.output.stdout.includes('space received') && !live.output.stderr.includes('space received'), output: live.output });
    const stopping = Date.now();
    await page.evaluate(async () => {
      await globalThis.graphicsProbe.session.dispose();
      document.getElementById('target').replaceChildren();
      delete globalThis.graphicsProbe;
    });
    while (page.workers().length && Date.now() - stopping < 5000) await page.waitForTimeout(50);
    assert.equal(page.workers().length, 0, 'owned runtime worker survived disposal');
    assert.ok(Date.now() - stopping < 5000, 'disposal exceeded the cleanup budget');
    await connection.send('HeapProfiler.collectGarbage');
    await page.waitForTimeout(200);
    const rss = await browserRss(browserConnection);
    const counters = await connection.send('Memory.getDOMCounters');
    memory.push(rss);
    report.observations.push({ cycle, elapsedMs: Date.now() - started, cleanupMs: Date.now() - stopping, before, moved, afterInput, live, rss, counters, forcedBrowserGc: true });
    await persist();
  }
  report.finalChecks = {
    browserErrors: report.errors.length === 0,
    postCleanupRssPlateau: Math.max(...memory.slice(1)) - memory[0] <= 128 * 1024 ** 2,
    eventListenerPlateau: report.observations.every(item => item.counters.jsEventListeners <= report.observations[0].counters.jsEventListeners),
    stdoutChannel: report.channelChecks.every(check => check.passed),
  };
  assert.deepEqual(Object.entries(report.finalChecks).filter(([, passed]) => !passed).map(([name]) => name), [], 'one or more independent runtime oracles failed');
  report.passed = true;
} catch (error) {
  report.errors.push(error.stack ?? String(error));
  if (page && !page.isClosed()) {
    report.failureState = await page.evaluate(() => globalThis.graphicsProbe?.session.getState());
    await page.screenshot({ path: resolve(evidence, 'failure.png') });
  }
  throw error;
} finally {
  await context?.close();
  await browser?.close();
  server.closeAllConnections();
  if (server.listening) await new Promise(done => server.close(done));
  await persist();
  console.log(`[coding-graphics] evidence: ${evidence}`);
}
