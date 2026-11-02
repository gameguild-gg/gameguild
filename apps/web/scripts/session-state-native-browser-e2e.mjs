#!/usr/bin/env node
// Executed only by the hosted .NET harness with a migrated, disposable database.
import assert from 'node:assert/strict';
import { verifyMetadataRoutes } from './metadata-route-http-acceptance.mjs';
import { spawn } from 'node:child_process';
import { randomBytes } from 'node:crypto';
import { mkdir, writeFile } from 'node:fs/promises';
import { createServer } from 'node:net';
import { createRequire } from 'node:module';
import path from 'node:path';
import { setTimeout as delay } from 'node:timers/promises';
import { chromium } from 'playwright';
import { GameGuildAuth, CredentialsProvider, encodeSession, SessionStore, resolveCookieOptions } from '@game-guild/client';

assert.equal(process.env.GITHUB_ACTIONS, 'true', 'This acceptance runner is hosted-only.');
const evidence = path.resolve(process.argv[2]);
await mkdir(evidence, { recursive: true });
let input = '';
for await (const chunk of process.stdin) {
  input += chunk;
  assert.ok(input.length < 65536, 'Unexpected input size.');
}
const { apiUrl, account, accessToken, accessTokenExpires, authSecret } = JSON.parse(input);
input = '';
const api = new URL(apiUrl);
assert.equal(api.protocol, 'http:');
assert.equal(api.hostname, '127.0.0.1');
assert.ok(Number(api.port) > 0);
assert.ok(accessTokenExpires > Date.now() + 10 * 60_000, 'The synthetic access token must outlive the acceptance run.');
const reserved = createServer();
await new Promise((resolve, reject) => reserved.once('error', reject).listen(0, '127.0.0.1', resolve));
const port = reserved.address().port;
await new Promise((resolve, reject) => reserved.close(error => error ? reject(error) : resolve()));
const webUrl = `http://127.0.0.1:${port}`;
const require = createRequire(import.meta.url);
const config = GameGuildAuth({
  providers: [CredentialsProvider()],
  apiUrl,
  secret: authSecret,
  cookies: { name: 'gameguild', secure: false, sameSite: 'lax', path: '/', httpOnly: true },
}).config;
const encrypted = await encodeSession({
  user: { id: account.userId, email: account.email, name: 'Synthetic session-state browser account', roles: [] },
  accessToken,
  accessTokenExpires,
  refreshToken: '',
  sessionId: account.sessionId,
  tenantId: account.tenantId,
  iat: Math.floor(Date.now() / 1000),
  exp: Math.floor(Date.now() / 1000) + 3600,
}, config);
const secrets = [accessToken, authSecret, encrypted];
const redact = value => secrets.reduce((text, secret) => text.replaceAll(secret, '[synthetic credential redacted]'), String(value));
let serverLog = '';
const next = spawn(process.execPath, [
  '--max-old-space-size=6144', require.resolve('next/dist/bin/next'),
  'dev', '--webpack', '--hostname', '127.0.0.1', '--port', String(port),
], {
  cwd: process.cwd(),
  env: { ...process.env, NODE_ENV: 'development', NODE_OPTIONS: '', NEXT_TELEMETRY_DISABLED: '1',
    AUTH_SECRET: authSecret, AUTH_COOKIE_SECURE: 'false', AUTH_COOKIE_DOMAIN: '',
    API_URL: apiUrl, NEXT_PUBLIC_API_URL: apiUrl, NEXT_PUBLIC_APP_URL: webUrl, NEXTAUTH_URL: webUrl },
  stdio: ['ignore', 'pipe', 'pipe'],
  detached: process.platform !== 'win32',
});
next.on('error', error => { serverLog += '\nNext process error: ' + error.message; });
for (const stream of [next.stdout, next.stderr])
  stream.on('data', chunk => { serverLog = (serverLog + chunk.toString()).slice(-2_000_000); });
let browser;
const cases = [];
const trustCases = [];
const trustPassed = name => { trustCases.push({ name, outcome: 'Passed' }); console.log('PASS: ' + name); };
const passed = name => { cases.push({ name, outcome: 'Passed' }); console.log('PASS: ' + name); };
const timeout = 120_000;
const utc = value => new Date(value).toISOString().replace('T', ' ').slice(0, 19) + ' UTC';
function assertState(state) {
  assert.equal(state.userId, account.userId);
  assert.equal(state.sessionId, account.sessionId);
  assert.equal(state.sessionValid, true);
  assert.equal(new Date(state.sessionExpiresAtUtc).getTime(), new Date(account.expiresAt).getTime());
  assert.equal(new Date(state.sessionLastUsedAtUtc).getTime(), new Date(account.lastUsedAt).getTime());
  assert.equal(state.activeSessionCount, 2);
  assert.equal(state.tokenVersion, account.tokenVersion);
  assert.equal(state.tokenRevoked, false);
  assert.equal(state.mfaEnrolled, false);
  assert.equal(state.mfaVerifiedInCurrentToken, false);
  assert.equal(state.sessionTerminationReason, null);
}
async function cookiesFor(context, envelope) {
  const store = new SessionStore(resolveCookieOptions(config.cookies, false));
  const cookies = [];
  store.write(envelope, (name, value, options) => {
    if (options.maxAge === 0) return;
    cookies.push({ name, value, domain: '127.0.0.1', path: options.path,
      httpOnly: options.httpOnly, secure: options.secure, sameSite: 'Lax' });
  }, { storage: 'session' });
  assert.ok(cookies.length > 0);
  await context.addCookies(cookies);
}
const isState = response => new URL(response.url()).pathname === '/api/auth/session-state';
try {
  const deadline = Date.now() + 180_000;
  let ready = false;
  while (Date.now() < deadline) {
    if (next.exitCode !== null || next.signalCode !== null) throw new Error('Next exited before readiness.');
    try {
      const response = await fetch(webUrl + '/api/auth/session-state', { redirect: 'manual', signal: AbortSignal.timeout(5000) });
      if (response.status === 401) { ready = true; break; }
    } catch { /* Readiness only; no test scenario is retried. */ }
    await delay(500);
  }
  assert.ok(ready, 'Next never exposed the authenticated session-state route.');
  browser = await chromium.launch({ headless: true });
  const anonymous = await browser.newContext();
  const denial = await anonymous.request.get(webUrl + '/api/auth/session-state');
  assert.equal(denial.status(), 401);
  assert.equal((await denial.json()).error, 'Authentication required.');
  passed('Anonymous browser cannot read session state');
  await anonymous.close();

  const tampered = await browser.newContext();
  // Preserve the JWE shape but damage authenticated ciphertext, using independent random bytes.
  const segments = encrypted.split('.');
  assert.equal(segments.length, 5);
  segments[3] = randomBytes(Buffer.from(segments[3], 'base64url').length).toString('base64url');
  await cookiesFor(tampered, segments.join('.'));
  const invalid = await tampered.request.get(webUrl + '/api/auth/session-state');
  assert.equal(invalid.status(), 401);
  assert.equal((await invalid.json()).error, 'Authentication required.');
  passed('Tampered encrypted session cookie denied');
  await tampered.close();

  const authenticated = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  await cookiesFor(authenticated, encrypted);
  const page = await authenticated.newPage();
  page.setDefaultTimeout(timeout);
  // Issue #106 phase 1: the account page fetches the session-state projection on
  // the server (with the session's bearer) and renders the card filled on first
  // paint, so no browser request to /api/auth/session-state is expected during the
  // load. The browser→Next-BFF path stays covered by the anonymous and tampered
  // denials above and by the explicit Refresh, foreign-identity and post-revocation
  // scenarios below.
  await page.goto(webUrl + '/workspace/settings/account', { waitUntil: 'domcontentloaded', timeout });
  const card = page.getByTestId('session-state-card');
  const fields = card.getByTestId('session-state-fields');
  // Issue #106 phase 1 made the account page server-rendered: the fields are
  // present in the server HTML before React attaches its event handlers in the
  // development server, and a Refresh click landing in that gap dispatches on
  // an inert button without producing any browser request. Click-and-wait is
  // therefore retried within the original ~120s budget until the page is
  // interactive; the refresh request itself is an idempotent GET.
  async function refreshAndAwaitSessionState() {
    let lastError;
    for (let attempt = 0; attempt < 12; attempt += 1) {
      const pending = page.waitForResponse(isState, { timeout: 10_000 });
      try {
        await card.getByRole('button', { name: 'Refresh', exact: true }).click({ timeout: 10_000 });
      } catch (error) {
        lastError = error;
        await pending.catch(() => {});
        continue;
      }
      try {
        return await pending;
      } catch (error) {
        lastError = error;
      }
    }
    throw lastError;
  }
  await fields.waitFor({ state: 'visible', timeout });
  assert.equal(await fields.getByText('Valid', { exact: true }).count(), 1);
  for (const [label, value] of [
    ['Expires', utc(account.expiresAt)], ['Last used', utc(account.lastUsedAt)],
    ['Token version', String(account.tokenVersion)], ['Active sessions', '2'], ['MFA enrollment', 'Not enrolled'],
  ]) {
    const field = fields.locator('div').filter({ has: page.getByText(label, { exact: true }) }).locator('dd');
    assert.equal((await field.innerText()).trim(), value);
  }
  await card.screenshot({ path: path.join(evidence, 'session-state-valid.png') });
  passed('Real account page renders the persisted owner session from the server-fetched projection');
  await verifyMetadataRoutes({ webUrl, page, evidence });

  const refreshed = await refreshAndAwaitSessionState();
  assert.equal(refreshed.status(), 200);
  assert.equal(refreshed.headers()['cache-control'], 'no-store');
  assertState(await refreshed.json());
  passed('Refresh reads current persisted state with no-store');

  const foreign = await authenticated.request.get(webUrl + '/api/auth/session-state?userId=' +
    account.foreignUserId + '&sessionId=' + account.foreignSessionId);
  assert.equal(foreign.status(), 200);
  assertState(await foreign.json());
  passed('Foreign identity parameters cannot change the authenticated projection');

  // Exercise the existing page and real server actions before the session-state
  // scenarios revoke the token. The owner has expired trust; another owner has
  // active trust for exactly the same fingerprint.
  await page.goto(webUrl + '/workspace/settings/security', { waitUntil: 'domcontentloaded', timeout });
  // Dev-server compile of this route can race the first RSC stream and abort it
  // (ECONNRESET / "destination stream errored"); a deterministic reload once the
  // route is compiled gives a clean stream before the trust assertions begin.
  await page.waitForLoadState('networkidle', { timeout }).catch(() => {});
  await page.reload({ waitUntil: 'domcontentloaded', timeout });
  // A streamed reload can briefly retain hidden copies outside the active main.
  // Keep strict row uniqueness and owner isolation within the rendered page.
  const securityPage = page.getByRole('main');
  const currentRow = securityPage.getByTestId('session-row-' + account.sessionId);
  const otherRow = securityPage.getByTestId('session-row-' + account.otherSessionId);
  const trustButton = securityPage.getByRole('button', { name: 'Trust', exact: true });
  const trustedRow = securityPage.getByTestId('trusted-device-row-' + account.trustedDeviceId);
  const trustedBadge = currentRow.getByText('Trusted', { exact: true });
  await currentRow.waitFor({ state: 'visible', timeout });
  await otherRow.waitFor({ state: 'visible', timeout });
  await trustButton.waitFor({ state: 'visible', timeout });
  assert.equal(await securityPage.locator('[data-testid^="session-row-"]').count(), 2);
  assert.equal(await securityPage.getByTestId('session-row-' + account.foreignSessionId).count(), 0);
  assert.equal(await securityPage.getByTestId('trusted-device-row-' + account.foreignTrustedDeviceId).count(), 0);
  assert.equal(await trustedRow.count(), 0);
  assert.equal(await trustedBadge.count(), 0);
  assert.equal(await otherRow.getByText('Trusted', { exact: true }).count(), 0);
  trustPassed('Expired own trust and active foreign trust do not mark the current session trusted');

  const isSecurityAction = response => response.request().method() === 'POST' &&
    new URL(response.url()).pathname.endsWith('/workspace/settings/security') &&
    Boolean(response.request().headers()['next-action']);
  // Issue #1144: trusting the current device now requires a fresh step-up receipt
  // (challenge bound to operation/target/payload hash, verified with an MFA factor).
  // This acceptance account has no MFA factor, so the real Trust server action's API
  // call is rejected with 403 and creates nothing: the action itself completes (RSC
  // protocol 200, surfacing the failure as an error toast), the Trusted badge and
  // trusted-device row never appear, and the Trust affordance stays available.
  const trustAction = page.waitForResponse(isSecurityAction, { timeout });
  await trustButton.click();
  assert.equal((await trustAction).status(), 200);
  await page.waitForLoadState('networkidle', { timeout }).catch(() => {});
  await trustButton.waitFor({ state: 'visible', timeout });
  assert.equal(await trustedBadge.count(), 0);
  assert.equal(await trustedRow.count(), 0);
  assert.equal(await securityPage.locator('[data-testid^="trusted-device-row-"]').count(), 0);
  assert.equal(await otherRow.getByText('Trusted', { exact: true }).count(), 0);
  const trustedResponse = await fetch(apiUrl + '/v1/auth/trusted-devices', {
    headers: { authorization: 'Bearer ' + accessToken, 'X-Tenant-Id': account.tenantId },
    redirect: 'error', signal: AbortSignal.timeout(30000),
  });
  assert.equal(trustedResponse.status, 200);
  const trustedDevices = await trustedResponse.json();
  assert.equal(trustedDevices.length, 0);
  await currentRow.screenshot({ path: path.join(evidence, 'session-trust-active.png') });
  trustPassed('The real Trust server action without a step-up receipt is rejected and creates nothing');

  // With the renewal rejected there is no trusted row to revoke; the rejected attempt
  // must leave the current session authenticated and its persisted state untouched.
  const stillValid = await authenticated.request.get(webUrl + '/api/auth/session-state');
  assert.equal(stillValid.status(), 200);
  assertState(await stillValid.json());
  await currentRow.screenshot({ path: path.join(evidence, 'session-trust-revoked.png') });
  trustPassed('The rejected trust attempt leaves the current session valid and unchanged');

  await page.reload({ waitUntil: 'domcontentloaded', timeout });
  await currentRow.waitFor({ state: 'visible', timeout });
  await trustButton.waitFor({ state: 'visible', timeout });
  assert.equal(await trustedBadge.count(), 0);
  assert.equal(await securityPage.locator('[data-testid^="trusted-device-row-"]').count(), 0);
  assert.equal(await securityPage.locator('[data-testid^="session-row-"]').count(), 2);
  assert.equal(await securityPage.getByTestId('session-row-' + account.foreignSessionId).count(), 0);
  assert.equal(await securityPage.getByTestId('trusted-device-row-' + account.foreignTrustedDeviceId).count(), 0);
  trustPassed('A fresh security page preserves the rejected trust state and owner isolation');
  assert.equal(trustCases.length, 4);
  await writeFile(path.join(evidence, 'trust-browser.json'), JSON.stringify({
    passed: true, userId: account.userId, sessionId: account.sessionId,
    trustedDeviceId: account.trustedDeviceId, foreignTrustedDeviceId: account.foreignTrustedDeviceId,
    cases: trustCases, mockedAuthenticationOrTransport: false,
    mode: 'Actual Next development server actions, encrypted cookie, Chromium, Kestrel and PostgreSQL',
    productionBuildAcceptance: false,
  }, null, 2));
  // Resume the original seven session-state scenarios on their actual account page.
  // The card is server-rendered filled again (issue #106 phase 1); no mount fetch.
  await page.goto(webUrl + '/workspace/settings/account', { waitUntil: 'domcontentloaded', timeout });
  await fields.waitFor({ state: 'visible', timeout });

  const revoke = await fetch(apiUrl + '/v1/auth/sessions/' + account.sessionId, {
    method: 'DELETE',
    headers: { authorization: 'Bearer ' + accessToken, 'X-Tenant-Id': account.tenantId },
    redirect: 'error',
    signal: AbortSignal.timeout(30000),
  });
  assert.equal(revoke.status, 200);
  passed('Actual API revokes the authenticated session');
  const rejected = await refreshAndAwaitSessionState();
  assert.equal(rejected.status(), 401);
  const error = card.getByTestId('session-state-error');
  await error.waitFor({ state: 'visible', timeout });
  assert.ok((await error.innerText()).trim().length > 0);
  assert.equal(await fields.count(), 0);
  await card.screenshot({ path: path.join(evidence, 'session-state-revoked.png') });
  passed('Post-revocation Refresh forwards 401 and removes stale UI state');
  assert.equal(cases.length, 7);
  await writeFile(path.join(evidence, 'browser.json'), JSON.stringify({
    passed: true, userId: account.userId, sessionId: account.sessionId, cases,
    mode: 'Actual Next development server, encrypted cookie, Chromium, Kestrel and PostgreSQL',
    productionBuildAcceptance: false,
    mockedAuthenticationOrTransport: false,
  }, null, 2));
} catch (error) {
  await writeFile(path.join(evidence, 'trust-browser.json'), JSON.stringify({
    passed: false, sessionId: account.sessionId, cases: trustCases, failure: redact(error.stack ?? error),
  }, null, 2));
  await writeFile(path.join(evidence, 'browser.json'), JSON.stringify({
    passed: false, sessionId: account.sessionId, cases, failure: redact(error.stack ?? error),
  }, null, 2));
  process.exitCode = 1;
} finally {
  if (browser) await browser.close();
  if (next.exitCode === null && next.signalCode === null) {
    if (process.platform === 'win32') next.kill('SIGTERM');
    else process.kill(-next.pid, 'SIGTERM');
    await Promise.race([new Promise(resolve => next.once('exit', resolve)), delay(5000)]);
    if (next.exitCode === null && next.signalCode === null) {
      if (process.platform === 'win32') next.kill('SIGKILL');
      else process.kill(-next.pid, 'SIGKILL');
    }
  }
  await writeFile(path.join(evidence, 'next.log'), redact(serverLog));
}
