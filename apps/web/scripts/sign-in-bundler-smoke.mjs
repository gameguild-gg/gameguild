#!/usr/bin/env node
// Hosted-only render/hydration and SDK-resolution regression. No credentials or real pairing.
import assert from 'node:assert/strict';
import { execFileSync, spawn } from 'node:child_process';
import { randomBytes } from 'node:crypto';
import { mkdir, writeFile } from 'node:fs/promises';
import { createRequire } from 'node:module';
import { createServer } from 'node:net';
import path from 'node:path';
import { setTimeout as delay } from 'node:timers/promises';
import { chromium } from 'playwright';

assert.equal(process.env.GITHUB_ACTIONS, 'true', 'Run this probe only in hosted CI.');
assert.equal(process.platform, 'linux', 'The process cleanup is scoped to a hosted Linux process group.');
const mode = process.argv[2];
assert.ok(['webpack', 'turbopack-fast'].includes(mode));
const sdkState = process.argv[4];
assert.ok(['absent', 'installed'].includes(sdkState));
const walletConnectEnabled = sdkState === 'installed';
const evidence = path.resolve(process.argv[3]);
await mkdir(evidence, { recursive: true });
const require = createRequire(import.meta.url);
const git = (...args) => execFileSync('git', ['-c', 'core.commitGraph=false', ...args], { encoding: 'utf8' }).trim();
// Shallow checkout hides parents from git show; the raw commit object retains them.
const commitHeaders = git('cat-file', '-p', 'HEAD').split(/\r?\n\r?\n/, 1)[0].split(/\r?\n/);
const source = { checkout: git('rev-parse', 'HEAD'), tree: git('rev-parse', 'HEAD^{tree}'),
  parents: commitHeaders.filter(line => line.startsWith('parent ')).map(line => line.slice(7)) };
assert.ok(source.parents.length > 0, 'Retain the actual commit parents in the source evidence.');
const reservation = createServer();
await new Promise((resolve, reject) => reservation.once('error', reject).listen(0, '127.0.0.1', resolve));
const port = reservation.address().port;
await new Promise((resolve, reject) => reservation.close(error => error ? reject(error) : resolve()));
const origin = `http://127.0.0.1:${port}`;
const secret = randomBytes(48).toString('base64url');
const redact = text => String(text).replaceAll(secret, '[synthetic secret redacted]');
let serverLog = '';
const child = spawn(process.execPath, ['--max-old-space-size=6144', require.resolve('next/dist/bin/next'),
  'dev', mode === 'webpack' ? '--webpack' : '--turbopack', '--hostname', '127.0.0.1', '--port', String(port)], {
  cwd: process.cwd(), detached: true, stdio: ['ignore', 'pipe', 'pipe'],
  env: { ...process.env, NODE_ENV: 'development', NODE_OPTIONS: '', NEXT_TELEMETRY_DISABLED: '1',
    GAMEGUILD_FAST_DEV: mode === 'turbopack-fast' ? '1' : '0', npm_lifecycle_event: '',
    AUTH_SECRET: secret, AUTH_COOKIE_SECURE: 'false', AUTH_COOKIE_DOMAIN: '',
    API_URL: 'http://127.0.0.1:9', NEXT_PUBLIC_API_URL: 'http://127.0.0.1:9',
    NEXT_PUBLIC_APP_URL: origin, NEXTAUTH_URL: origin,
    NEXT_PUBLIC_WALLET_TORUS_ENABLED: 'false',
    NEXT_PUBLIC_WALLETCONNECT_PROJECT_ID: walletConnectEnabled ? '0'.repeat(32) : '',
    GOOGLE_CLIENT_ID: '', GOOGLE_CLIENT_SECRET: '', NEXT_PUBLIC_GOOGLE_CLIENT_ID: '',
    DISCORD_CLIENT_ID: '', DISCORD_CLIENT_SECRET: '', GITHUB_CLIENT_ID: '', GITHUB_CLIENT_SECRET: '',
  },
});
child.on('error', error => { serverLog += '\n' + redact(error.message); });
for (const stream of [child.stdout, child.stderr]) {
  stream.on('data', chunk => { serverLog = (serverLog + redact(chunk.toString())).slice(-2_000_000); });
}
const cases = [];
const errors = [];
const sdkRequests = [];
let browser;
let failure;
const cleanupFailures = [];
const recordCleanupFailure = (operation, error) => {
  cleanupFailures.push(`${operation}: ${redact(error?.stack ?? error)}`);
};
try {
  const deadline = Date.now() + 180_000;
  let ready = false;
  while (Date.now() < deadline) {
    assert.equal(child.exitCode, null, 'Next exited before readiness.');
    assert.equal(child.signalCode, null, 'Next stopped before readiness.');
    try {
      const response = await fetch(origin + '/api/auth/session-state', { redirect: 'manual', signal: AbortSignal.timeout(5000) });
      if (response.status === 401) { ready = true; break; }
    } catch { /* Only readiness is retried; test navigation is not retried. */ }
    await delay(500);
  }
  assert.ok(ready, 'The anonymous session endpoint never became ready.');
  browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  // The installed SDK must actually initialize when clicked. Record and block its
  // external requests: resolving/initializing it does not require a real pairing.
  const recordSdkRequest = url => {
    if (/(^|\.)(walletconnect\.(com|org)|reown\.com)$/.test(url.hostname)) {
      sdkRequests.push(url.origin + url.pathname);
    }
  };
  await context.route('**/*', route => {
    const url = new URL(route.request().url());
    if (url.origin !== origin) recordSdkRequest(url);
    return url.origin === origin ? route.continue() : route.abort('blockedbyclient');
  });
  await context.routeWebSocket('**/*', route => {
    const url = new URL(route.url());
    if (url.hostname === '127.0.0.1' && url.port === String(port)) {
      route.connectToServer();
    } else {
      recordSdkRequest(url);
      route.close();
    }
  });
  const page = await context.newPage();
  page.setDefaultTimeout(120_000);
  page.on('pageerror', error => errors.push(redact(error.message)));
  const response = await page.goto(origin + '/sign-in', { waitUntil: 'domcontentloaded', timeout: 180_000 });
  assert.equal(response?.status(), 200, 'The real sign-in route must compile and render.');
  await page.getByRole('heading', { name: 'Welcome back', exact: true }).waitFor();
  await page.locator('form[data-auth-ready="true"]').waitFor();
  await page.getByLabel('Email', { exact: true }).fill('bundler-probe@example.test');
  assert.equal(await page.getByLabel('Email', { exact: true }).inputValue(), 'bundler-probe@example.test');
  cases.push({ name: 'Anonymous sign-in compiles, renders and hydrates', outcome: 'Passed' });
  await page.getByRole('button', { name: 'Show password', exact: true }).click();
  assert.equal(await page.locator('#password').getAttribute('type'), 'text');
  await page.getByRole('button', { name: 'Hide password', exact: true }).click();
  assert.equal(await page.locator('#password').getAttribute('type'), 'password');
  cases.push({ name: 'Hydrated password visibility control remains interactive', outcome: 'Passed' });
  if (walletConnectEnabled) {
    const walletButton = page.getByTestId('walletconnect-sign-in-button');
    await walletButton.waitFor({ state: 'visible' });
    sdkRequests.length = 0;
    await walletButton.click();
    const sdkDeadline = Date.now() + 30_000;
    while (sdkRequests.length === 0 && Date.now() < sdkDeadline) await delay(100);
    assert.ok(sdkRequests.length > 0,
      'The real installed SDK must load and reach provider initialization; a stub or unresolved import cannot pass.');
    cases.push({ name: 'Enabled WalletConnect resolves the installed SDK and begins provider initialization', outcome: 'Passed' });
  } else {
    assert.equal(await page.getByTestId('walletconnect-sign-in-button').count(), 0);
    assert.deepEqual(sdkRequests, []);
    cases.push({ name: 'Disabled WalletConnect compiles and hydrates with its SDK physically absent', outcome: 'Passed' });
  }
  assert.equal(await page.getByTestId('torus-sign-in-button').count(), 0);
  assert.deepEqual(errors, [], 'Sign-in must not produce a browser runtime error.');
  await page.screenshot({ path: path.join(evidence, 'sign-in.png'), fullPage: true });
} catch (error) {
  failure = redact(error?.stack ?? error);
} finally {
  try { await browser?.close(); } catch (error) { recordCleanupFailure('Close browser', error); }
  if (child.pid && child.exitCode === null && child.signalCode === null) {
    try { process.kill(-child.pid, 'SIGTERM'); } catch (error) {
      if (error?.code !== 'ESRCH') recordCleanupFailure('Terminate Next process group', error);
    }
    const deadline = Date.now() + 5000;
    while (child.exitCode === null && child.signalCode === null && Date.now() < deadline) await delay(100);
    if (child.exitCode === null && child.signalCode === null) {
      try { process.kill(-child.pid, 'SIGKILL'); } catch (error) {
        if (error?.code !== 'ESRCH') recordCleanupFailure('Kill Next process group', error);
      }
    }
  }
  try { await writeFile(path.join(evidence, 'next.log'), serverLog); }
  catch (error) { recordCleanupFailure('Write Next log', error); }
  try {
    await writeFile(path.join(evidence, 'result.json'), JSON.stringify({ ...source, mode, sdkState, sdkRequests,
      passed: !failure && cleanupFailures.length === 0,
      cases, browserErrors: errors, failure: failure ?? null, cleanupFailures, credentialsSubmitted: false,
      backendResponsesMocked: false, externalBrowserRequestsBlocked: true, realWalletPaired: false }, null, 2));
  } catch (error) { recordCleanupFailure('Write regression result', error); }
}
if (failure || cleanupFailures.length > 0) {
  throw new Error([failure, ...cleanupFailures].filter(Boolean).join('\n\n'));
}
console.log(`PASS: ${mode} sign-in rendering and hydration (${cases.length} cases).`);
