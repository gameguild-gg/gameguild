#!/usr/bin/env node

import { chromium } from 'playwright';

const apiBaseUrl = (process.env.API_BASE_URL ?? process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:8080').replace(/\/$/, '');
const webBaseUrl = (process.env.USER_SETTINGS_E2E_BASE_URL ?? process.env.NEXT_PUBLIC_APP_URL ?? 'http://localhost:3000').replace(/\/$/, '');
const adminEmail = process.env.E2E_SYSTEM_ADMIN_EMAIL ?? 'admin@game-guild.com';
const adminPassword = process.env.E2E_SYSTEM_ADMIN_PASSWORD ?? 'Admin123!';
const headless = !['0', 'false', 'no'].includes((process.env.USER_SETTINGS_E2E_HEADLESS ?? 'true').toLowerCase());

async function apiRequest(path, init = {}, accessToken) {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers: {
      'content-type': 'application/json',
      ...(accessToken ? { authorization: `Bearer ${accessToken}` } : {}),
      ...init.headers,
    },
  });
  const body = response.status === 204 ? null : await response.json().catch(() => null);
  if (!response.ok) {
    throw new Error(`${init.method ?? 'GET'} ${path} failed with ${response.status}: ${JSON.stringify(body)}`);
  }
  return body;
}

function hasNamedValue(value, expectedKey, expectedValue) {
  if (Array.isArray(value)) return value.some((item) => hasNamedValue(item, expectedKey, expectedValue));
  if (!value || typeof value !== 'object') return false;

  const normalizedKey = expectedKey.replace(/[^a-z0-9]/gi, '').toLowerCase();
  return Object.entries(value).some(([key, entry]) => {
    if (key.replace(/[^a-z0-9]/gi, '').toLowerCase() === normalizedKey && entry === expectedValue) {
      return true;
    }
    return hasNamedValue(entry, expectedKey, expectedValue);
  });
}

async function waitForPersistedValues(path, expected, accessToken, timeout = 45_000) {
  const deadline = Date.now() + timeout;
  let lastBody = null;
  while (Date.now() < deadline) {
    lastBody = await apiRequest(path, {}, accessToken);
    if (Object.entries(expected).every(([key, value]) => hasNamedValue(lastBody, key, value))) return;
    await new Promise((resolve) => setTimeout(resolve, 200));
  }
  throw new Error(`Timed out waiting for ${path} to persist ${JSON.stringify(expected)}. Last response keys: ${lastBody && typeof lastBody === 'object' ? Object.keys(lastBody).join(', ') : 'none'}`);
}

async function assertNoErrorSurface(page, label) {
  await page.waitForLoadState('domcontentloaded');
  await page.locator('body').waitFor({ state: 'visible' });
  const body = await page.locator('body').innerText();
  if (/This page could not be found|Unhandled Runtime Error|Build Error|Application error|Internal server error/i.test(body)) {
    throw new Error(`${label} rendered an error surface at ${page.url()}:\n${body.slice(0, 1600)}`);
  }
}

async function visit(page, path, label) {
  await page.goto(`${webBaseUrl}${path}`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  await assertNoErrorSurface(page, label);
  await page.waitForFunction(() => document.readyState !== 'loading');
  await page.waitForTimeout(5_000);
}

async function chooseValue(page, selector, name) {
  await page.locator(selector).click();
  await page.getByRole('option', { name, exact: typeof name === 'string' }).click();
}

async function setSwitch(page, selector, checked) {
  const toggle = page.locator(selector);
  if ((await toggle.getAttribute('data-state')) !== (checked ? 'checked' : 'unchecked')) {
    await toggle.click();
  }
}

async function setFontSize(page, value) {
  const slider = page.getByRole('slider').first();
  const current = Number(await slider.getAttribute('aria-valuenow'));
  if (current === value) return;
  await slider.focus();
  await slider.press(current < value ? 'End' : 'Home');
  await page.waitForFunction((target) => {
    const currentSlider = document.querySelector('[role="slider"]');
    return currentSlider?.getAttribute('aria-valuenow') === String(target);
  }, value);
}

async function authenticateBrowser(page) {
  const result = await page.evaluate(async ({ email, password }) => {
    const csrfResponse = await fetch('/api/auth/csrf', { credentials: 'include' });
    const csrfBody = await csrfResponse.json().catch(() => null);
    if (!csrfResponse.ok || !csrfBody || typeof csrfBody.csrfToken !== 'string') {
      return { ok: false, status: csrfResponse.status };
    }

    const response = await fetch('/api/auth/signin/credentials', {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        email,
        password,
        csrfToken: csrfBody.csrfToken,
        redirect: false,
        redirectTo: '/',
      }),
    });
    return { ok: response.ok, status: response.status };
  }, { email: adminEmail, password: adminPassword });

  if (!result.ok) throw new Error(`Browser authentication failed with HTTP ${result.status}.`);
}

async function run() {
  const signIn = await apiRequest('/v1/auth/sign-in', {
    method: 'POST',
    body: JSON.stringify({ email: adminEmail, password: adminPassword }),
  });
  if (!signIn.accessToken || !signIn.userId) throw new Error('The seeded administrator could not authenticate.');

  const browser = await chromium.launch({ headless });
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  const page = await context.newPage();
  const browserErrors = [];
  const failedResponses = [];

  page.setDefaultTimeout(60_000);
  page.on('pageerror', (error) => browserErrors.push(error.message));
  page.on('console', (message) => {
    if (message.type() === 'error' && !/favicon|cloudflareinsights/i.test(message.text())) {
      browserErrors.push(message.text());
    }
  });
  page.on('response', (response) => {
    const url = new URL(response.url());
    if (url.searchParams.has('_rsc')) return;
    if (url.origin === webBaseUrl && response.status() >= 400) {
      failedResponses.push(`${response.status()} ${url.pathname}${url.search}`);
    }
  });

  try {
    console.log('[user-settings-e2e] authentication and hub navigation');
    await visit(page, '/sign-in', 'sign in');
    await page.getByLabel('Email').waitFor();
    await authenticateBrowser(page);

    await visit(page, '/workspace/settings', 'settings hub');
    await page.getByLabel('Display name').waitFor();
    for (const label of ['Profile', 'Account', 'Appearance', 'Language & region', 'Privacy', 'Accessibility']) {
      await page.getByRole('link', { name: label, exact: true }).first().waitFor();
    }

    console.log('[user-settings-e2e] profile');
    const displayName = `Settings QA ${Date.now()}`;
    await page.getByLabel('Display name').fill(displayName);
    await page.locator('form').getByRole('button', { name: /save/i }).click();
    await page.getByText('Profile saved', { exact: false }).waitFor();
    await waitForPersistedValues(`/v1/users/${signIn.userId}/profile`, { displayName }, signIn.accessToken);

    console.log('[user-settings-e2e] appearance');
    await visit(page, '/workspace/settings/appearance', 'appearance settings');
    await page.locator('#theme-dark').click();
    await page.waitForFunction(() => document.documentElement.classList.contains('dark'));
    await waitForPersistedValues(`/v1/users/${signIn.userId}/preferences`, { theme: 'dark' }, signIn.accessToken);

    console.log('[user-settings-e2e] localization');
    await visit(page, '/workspace/settings/localization', 'localization settings');
    await chooseValue(page, '#localization-language', 'Português (Brasil)');
    await page.locator('form').getByRole('button', { name: /save preferences/i }).click();
    await page.waitForFunction(() => window.location.pathname === '/pt-BR/workspace/settings/localization');
    await waitForPersistedValues(`/v1/users/${signIn.userId}/preferences/localization`, { language: 'pt-BR' }, signIn.accessToken);

    console.log('[user-settings-e2e] privacy');
    await visit(page, '/pt-BR/workspace/settings/privacy', 'privacy settings');
    await chooseValue(page, '#privacy-visibility', /private|privado/i);
    await setSwitch(page, '#privacy-analytics-cookies', false);
    await waitForPersistedValues(`/v1/users/${signIn.userId}/preferences/privacy`, {
      profileVisibility: 'private',
      analyticsCookies: false,
    }, signIn.accessToken);

    console.log('[user-settings-e2e] accessibility');
    await visit(page, '/pt-BR/workspace/settings/accessibility', 'accessibility settings');
    await setSwitch(page, '#accessibility-high-contrast', true);
    await waitForPersistedValues(`/v1/users/${signIn.userId}/preferences/accessibility`, { highContrast: true }, signIn.accessToken);
    await setSwitch(page, '#accessibility-large-text', true);
    await waitForPersistedValues(`/v1/users/${signIn.userId}/preferences/accessibility`, { largeText: true }, signIn.accessToken);
    await setSwitch(page, '#accessibility-reduced-motion', true);
    await waitForPersistedValues(`/v1/users/${signIn.userId}/preferences/accessibility`, { reducedMotion: true }, signIn.accessToken);
    await setFontSize(page, 20);
    await page.waitForFunction(() => {
      const root = document.documentElement;
      return root.dataset.accessibilityHighContrast === 'true'
        && root.dataset.accessibilityLargeText === 'true'
        && root.dataset.accessibilityReducedMotion === 'true';
    });
    await waitForPersistedValues(`/v1/users/${signIn.userId}/preferences/accessibility`, {
      highContrast: true,
      largeText: true,
      reducedMotion: true,
      fontSize: 20,
    }, signIn.accessToken);

    console.log('[user-settings-e2e] account destination');
    await visit(page, '/pt-BR/workspace/settings/account', 'account settings');
    await assertNoErrorSurface(page, 'account settings');

    const meaningfulFailures = [...new Set(failedResponses)].filter((value) => !/favicon|manifest\.webmanifest/.test(value));
    if (meaningfulFailures.length > 0) throw new Error(`HTTP failures detected:\n${meaningfulFailures.join('\n')}`);
    if (browserErrors.length > 0) throw new Error(`Browser errors detected:\n${[...new Set(browserErrors)].join('\n')}`);

    console.log('User settings browser E2E passed.');
  } catch (error) {
    const pageText = await page.locator('body').innerText().catch(() => 'Unable to read page body.');
    console.error(`[user-settings-e2e] failed at ${page.url()}`);
    console.error(`[user-settings-e2e] HTTP failures: ${[...new Set(failedResponses)].join(', ') || 'none'}`);
    console.error(`[user-settings-e2e] browser errors: ${[...new Set(browserErrors)].join(' | ') || 'none'}`);
    console.error(`[user-settings-e2e] page excerpt:\n${pageText.slice(0, 2400)}`);
    throw error;
  } finally {
    await browser.close();
  }
}

run().catch((error) => {
  console.error(error instanceof Error ? (error.stack ?? error.message) : error);
  process.exit(1);
});
