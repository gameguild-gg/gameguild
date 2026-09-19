#!/usr/bin/env node

import { readFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';

const liveMode = process.argv.includes('--live');
const timeoutMs = Number.parseInt(process.env.SMOKE_TIMEOUT_MS ?? '20000', 10);
const retryCount = Number.parseInt(process.env.SMOKE_RETRIES ?? (liveMode ? '2' : '0'), 10);

const defaults = liveMode
  ? {
      api: 'https://game-guild-api.matheusmartins.com',
      web: 'https://game-guild-website.matheusmartins.com',
    }
  : {
      api: 'http://localhost:8080',
      web: 'http://localhost:3005',
    };

const config = {
  api: process.env.GAMEGUILD_API_URL ?? process.env.API_URL ?? defaults.api,
  web: process.env.GAMEGUILD_WEB_URL ?? process.env.WEB_URL ?? defaults.web,
  adminEmail: process.env.GAMEGUILD_SMOKE_ADMIN_EMAIL,
  adminPassword: process.env.GAMEGUILD_SMOKE_ADMIN_PASSWORD,
};
const socialEvidencePath = process.env.SOCIAL_FEED_E2E_EVIDENCE_PATH ??
  fileURLToPath(new URL('../../.tmp/social-feed-browser-e2e/evidence.json', import.meta.url));
const socialEvidenceMaxAgeRaw = process.env.SMOKE_SOCIAL_EVIDENCE_MAX_AGE_MS ?? '86400000';
const socialEvidenceMaxAgeMs = Number(socialEvidenceMaxAgeRaw);
const socialEvidenceMaxAgeError = /^(0|[1-9]\d*)$/.test(socialEvidenceMaxAgeRaw) &&
  Number.isSafeInteger(socialEvidenceMaxAgeMs)
  ? null
  : 'SMOKE_SOCIAL_EVIDENCE_MAX_AGE_MS must be a finite non-negative integer in milliseconds.';

const expectedOrderOperations = [
  'get /v1/orders',
  'get /v1/orders/{orderId}',
  'post /v1/orders',
  'post /v1/orders/{orderId}/items',
  'post /v1/orders/{orderId}:capture',
  'post /v1/orders/{orderId}:complete',
  'post /v1/orders/{orderId}:payment-intent',
];
const expectedSocialOperations = [
  'delete /api/social/saved-posts/{postId}',
  'delete /api/social/stories/{storyId}',
  'delete /api/v1/posts/{postId}',
  'get /api/social/feed',
  'get /api/social/stories',
  'post /api/social/stories',
  'post /api/social/stories/{storyId}/views',
  'put /api/social/saved-posts/{postId}',
  'put /api/v1/posts/{postId}',
];
const expectedSocialCapabilities = [
  'nonAdminActors',
  'crossActorMutationForbidden',
  'published',
  'mediaPublished',
  'postEdited',
  'trendingTagVisible',
  'followed',
  'profileMetrics',
  'followingVisible',
  'reacted',
  'commented',
  'replyCreated',
  'commentEdited',
  'commentDeleted',
  'reposted',
  'saved',
  'shared',
  'permalinkOpened',
  'savedVisible',
  'storyPublished',
  'storyMediaDelivered',
  'storyViewed',
  'storyDeleted',
  'persistedAfterReload',
  'streamsSeparated',
  'unfollowed',
  'postDeleted',
];
const openApiMethods = new Set(['delete', 'get', 'head', 'options', 'patch', 'post', 'put', 'trace']);

const checks = [
  ['api live', config.api, '/live'],
  ['api ready', config.api, '/ready'],
  ['api documentation', config.api, '/documentation/index.html'],
  ['web health', config.web, '/api/health'],
  ['web auth csrf', config.web, '/api/auth/csrf'],
  ['web root', config.web, '/'],
  ['web favicon', config.web, '/favicon.svg'],
  ['web manifest', config.web, '/manifest.webmanifest'],
  ['course catalog', config.web, '/courses'],
  ['programs', config.web, '/programs'],
  ['learning catalog', config.web, '/learn/courses'],
];

function joinUrl(base, path) {
  return new URL(path, base.endsWith('/') ? base : `${base}/`).toString();
}

async function tryFetch(url) {
  const started = Date.now();

  try {
    const response = await fetch(url, {
      redirect: 'follow',
      signal: AbortSignal.timeout(timeoutMs),
      headers: {
        'User-Agent': 'gameguild-smoke/1.0',
      },
    });
    const elapsed = Date.now() - started;
    const ok = response.status >= 200 && response.status < 400;

    return {
      url,
      status: response.status,
      elapsed,
      ok,
    };
  } catch (error) {
    return {
      url,
      status: 'ERR',
      elapsed: Date.now() - started,
      ok: false,
      error: error instanceof Error ? error.message : String(error),
    };
  }
}

async function runCheck([name, base, path]) {
  const url = joinUrl(base, path);
  let lastResult;

  for (let attempt = 0; attempt <= retryCount; attempt += 1) {
    const result = await tryFetch(url);
    lastResult = result;

    if (result.ok || attempt === retryCount) {
      return {
        ...result,
        name,
        attempts: attempt + 1,
      };
    }

    await new Promise((resolve) => setTimeout(resolve, 1000 * (attempt + 1)));
  }

  return {
    ...lastResult,
    name,
    attempts: retryCount + 1,
  };
}

async function readJson(response) {
  try {
    return await response.json();
  } catch {
    return null;
  }
}

function getOrderOperations(document) {
  if (!document?.paths || typeof document.paths !== 'object' || Array.isArray(document.paths)) {
    return [];
  }

  return Object.entries(document.paths)
    .filter(([path]) => path.startsWith('/v1/orders'))
    .flatMap(([path, pathItem]) =>
      Object.keys(pathItem ?? {})
        .map((method) => method.toLowerCase())
        .filter((method) => openApiMethods.has(method))
        .map((method) => `${method} ${path}`),
    )
    .sort((left, right) => left.localeCompare(right));
}

function getSocialOperations(document) {
  if (!document?.paths || typeof document.paths !== 'object' || Array.isArray(document.paths)) {
    return [];
  }

  return Object.entries(document.paths)
    .flatMap(([path, pathItem]) =>
      Object.keys(pathItem ?? {})
        .map((method) => method.toLowerCase())
        .filter((method) => openApiMethods.has(method))
        .map((method) => `${method} ${path}`),
    );
}

async function runOrdersOpenApiCheck() {
  const started = Date.now();
  const url = joinUrl(config.api, '/swagger/v1/swagger.json');

  try {
    const response = await fetch(url, {
      signal: AbortSignal.timeout(timeoutMs),
      headers: {
        'User-Agent': 'gameguild-smoke/1.0',
      },
    });
    const document = await readJson(response);
    const actualOperations = getOrderOperations(document);
    const missing = expectedOrderOperations.filter((operation) => !actualOperations.includes(operation));
    const unexpected = actualOperations.filter((operation) => !expectedOrderOperations.includes(operation));
    const matches = response.ok && missing.length === 0 && unexpected.length === 0;

    return {
      name: 'Orders OpenAPI',
      url,
      status: response.status,
      elapsed: Date.now() - started,
      ok: matches,
      attempts: 1,
      error: matches
        ? undefined
        : `Orders OpenAPI operations mismatch; missing=[${missing.join(', ')}]; unexpected=[${unexpected.join(', ')}]`,
    };
  } catch (error) {
    return {
      name: 'Orders OpenAPI',
      url,
      status: 'ERR',
      elapsed: Date.now() - started,
      ok: false,
      attempts: 1,
      error: error instanceof Error ? error.message : String(error),
    };
  }
}

async function runSocialOpenApiCheck() {
  const started = Date.now();
  const url = joinUrl(config.api, '/swagger/v1/swagger.json');

  try {
    const response = await fetch(url, {
      signal: AbortSignal.timeout(timeoutMs),
      headers: { 'User-Agent': 'gameguild-smoke/1.0' },
    });
    const document = await readJson(response);
    const actualOperations = getSocialOperations(document);
    const missing = expectedSocialOperations.filter((operation) => !actualOperations.includes(operation));
    const matches = response.ok && missing.length === 0;
    return {
      name: 'Social production OpenAPI',
      url,
      status: response.status,
      elapsed: Date.now() - started,
      ok: matches,
      attempts: 1,
      error: matches ? undefined : `Social OpenAPI operations missing=[${missing.join(', ')}]`,
    };
  } catch (error) {
    return {
      name: 'Social production OpenAPI',
      url,
      status: 'ERR',
      elapsed: Date.now() - started,
      ok: false,
      attempts: 1,
      error: error instanceof Error ? error.message : String(error),
    };
  }
}

function normalizeOrigin(value) {
  return value.replace(/\/$/, '');
}

async function runSocialBrowserEvidenceCheck() {
  const started = Date.now();
  if (socialEvidenceMaxAgeError) {
    return {
      name: 'Social non-admin browser evidence',
      url: socialEvidencePath,
      status: 'CONFIG',
      elapsed: Date.now() - started,
      ok: false,
      attempts: 0,
      error: socialEvidenceMaxAgeError,
    };
  }
  try {
    const evidence = JSON.parse(await readFile(socialEvidencePath, 'utf8'));
    const missing = expectedSocialCapabilities.filter((key) => evidence.capabilities?.[key] !== true);
    const completedAt = Date.parse(evidence.metadata?.completedAt ?? '');
    const ageMs = Date.now() - completedAt;
    const problems = [];
    if (evidence.schemaVersion !== 1) problems.push('schemaVersion must be 1');
    if (evidence.status !== 'passed' || evidence.aggregate?.passed !== true) problems.push('aggregate status is not passed');
    if (missing.length > 0) problems.push(`missing capabilities=[${missing.join(', ')}]`);
    if (!Array.isArray(evidence.errors) || evidence.errors.length > 0) problems.push('errors must be an empty array');
    if (!Number.isFinite(completedAt) || ageMs < -300_000 || ageMs > socialEvidenceMaxAgeMs) {
      problems.push(`completedAt is invalid or older than ${socialEvidenceMaxAgeMs}ms`);
    }
    if (normalizeOrigin(evidence.metadata?.apiBaseUrl ?? '') !== normalizeOrigin(config.api)) {
      problems.push('API origin does not match this smoke target');
    }
    if (normalizeOrigin(evidence.metadata?.webBaseUrl ?? '') !== normalizeOrigin(config.web)) {
      problems.push('Web origin does not match this smoke target');
    }
    const ok = problems.length === 0;
    return {
      name: 'Social non-admin browser evidence',
      url: socialEvidencePath,
      status: 'FILE',
      elapsed: Date.now() - started,
      ok,
      attempts: 1,
      error: ok ? undefined : `Social browser evidence rejected: ${problems.join('; ')}`,
    };
  } catch (error) {
    return {
      name: 'Social non-admin browser evidence',
      url: socialEvidencePath,
      status: 'ERR',
      elapsed: Date.now() - started,
      ok: false,
      attempts: 1,
      error: `Social browser evidence could not be read: ${error instanceof Error ? error.message : String(error)}`,
    };
  }
}

function getCookieHeader(response) {
  const setCookies =
    typeof response.headers.getSetCookie === 'function'
      ? response.headers.getSetCookie()
      : [response.headers.get('set-cookie')].filter(Boolean);

  return setCookies
    .map((cookie) => cookie.split(';')[0])
    .filter(Boolean)
    .join('; ');
}

async function runApiAuthCheck() {
  const started = Date.now();
  const url = joinUrl(config.api, '/v1/auth/sign-in');
  const missing = [
    !config.adminEmail && 'GAMEGUILD_SMOKE_ADMIN_EMAIL',
    !config.adminPassword && 'GAMEGUILD_SMOKE_ADMIN_PASSWORD',
  ].filter(Boolean);
  if (missing.length > 0) {
    return {
      name: 'api admin auth',
      url,
      status: 'CONFIG',
      elapsed: Date.now() - started,
      ok: false,
      attempts: 0,
      error: `Required smoke credentials are missing: ${missing.join(', ')}`,
    };
  }

  try {
    const response = await fetch(url, {
      method: 'POST',
      signal: AbortSignal.timeout(timeoutMs),
      headers: {
        'Content-Type': 'application/json',
        'User-Agent': 'gameguild-smoke/1.0',
      },
      body: JSON.stringify({
        email: config.adminEmail,
        password: config.adminPassword,
      }),
    });
    const body = await readJson(response);

    return {
      name: 'api admin auth',
      url,
      status: response.status,
      elapsed: Date.now() - started,
      ok: response.ok && body?.success === true,
      attempts: 1,
    };
  } catch (error) {
    return {
      name: 'api admin auth',
      url,
      status: 'ERR',
      elapsed: Date.now() - started,
      ok: false,
      attempts: 1,
      error: error instanceof Error ? error.message : String(error),
    };
  }
}

async function runWebAuthBridgeCheck() {
  const started = Date.now();
  const csrfUrl = joinUrl(config.web, '/api/auth/csrf');
  const signInUrl = joinUrl(config.web, '/api/auth/signin/credentials');
  const missing = [
    !config.adminEmail && 'GAMEGUILD_SMOKE_ADMIN_EMAIL',
    !config.adminPassword && 'GAMEGUILD_SMOKE_ADMIN_PASSWORD',
  ].filter(Boolean);
  if (missing.length > 0) {
    return {
      name: 'web auth bridge',
      url: signInUrl,
      status: 'CONFIG',
      elapsed: Date.now() - started,
      ok: false,
      attempts: 0,
      error: `Required smoke credentials are missing: ${missing.join(', ')}`,
    };
  }

  try {
    const csrfResponse = await fetch(csrfUrl, {
      signal: AbortSignal.timeout(timeoutMs),
      headers: {
        'User-Agent': 'gameguild-smoke/1.0',
      },
    });
    const csrfBody = await readJson(csrfResponse);
    const cookie = getCookieHeader(csrfResponse);

    if (!csrfResponse.ok || !csrfBody?.csrfToken || !cookie) {
      return {
        name: 'web auth bridge',
        url: signInUrl,
        status: csrfResponse.status,
        elapsed: Date.now() - started,
        ok: false,
        attempts: 1,
        error: 'CSRF bootstrap failed',
      };
    }

    const response = await fetch(signInUrl, {
      method: 'POST',
      signal: AbortSignal.timeout(timeoutMs),
      headers: {
        'Content-Type': 'application/json',
        Cookie: cookie,
        'User-Agent': 'gameguild-smoke/1.0',
      },
      body: JSON.stringify({
        email: config.adminEmail,
        password: config.adminPassword,
        csrfToken: csrfBody.csrfToken,
        redirect: false,
      }),
    });

    return {
      name: 'web auth bridge',
      url: signInUrl,
      status: response.status,
      elapsed: Date.now() - started,
      ok: response.ok,
      attempts: 1,
    };
  } catch (error) {
    return {
      name: 'web auth bridge',
      url: signInUrl,
      status: 'ERR',
      elapsed: Date.now() - started,
      ok: false,
      attempts: 1,
      error: error instanceof Error ? error.message : String(error),
    };
  }
}

const results = [
  ...(await Promise.all(checks.map(runCheck))),
  await runOrdersOpenApiCheck(),
  await runSocialOpenApiCheck(),
  await runSocialBrowserEvidenceCheck(),
  await runApiAuthCheck(),
  await runWebAuthBridgeCheck(),
];
const nameWidth = Math.max(...results.map((result) => result.name.length));

for (const result of results) {
  const marker = result.ok ? 'PASS' : 'FAIL';
  const status = String(result.status).padEnd(3, ' ');
  const attempts = result.attempts > 1 ? ` attempts=${result.attempts}` : '';
  const message = result.error ? ` ${result.error}` : '';
  const line = `${marker} ${result.name.padEnd(nameWidth, ' ')} ${status} ${result.elapsed}ms ${result.url}${attempts}${message}`;
  if (result.ok) {
    console.log(line);
  } else {
    console.error(line);
  }
}

const failed = results.filter((result) => !result.ok);
if (failed.length > 0) {
  console.error(`Smoke check failed: ${failed.length}/${results.length} checks failed.`);
  process.exitCode = 1;
} else {
  console.log(`Smoke check passed: ${results.length}/${results.length} checks passed.`);
}
