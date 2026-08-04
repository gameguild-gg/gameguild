#!/usr/bin/env node

const liveMode = process.argv.includes('--live');
const timeoutMs = Number.parseInt(process.env.SMOKE_TIMEOUT_MS ?? '20000', 10);
const retryCount = Number.parseInt(process.env.SMOKE_RETRIES ?? (liveMode ? '2' : '0'), 10);

const defaults = liveMode
  ? {
      api: 'https://game-guild-api.matheusmartins.com',
      web: 'https://game-guild-website.matheusmartins.com',
      learning: 'https://game-guild-learning.matheusmartins.com',
    }
  : {
      api: 'http://localhost:8080',
      web: 'http://localhost:3005',
      learning: 'http://localhost:3006',
    };

const config = {
  api: process.env.GAMEGUILD_API_URL ?? process.env.API_URL ?? defaults.api,
  web: process.env.GAMEGUILD_WEB_URL ?? process.env.WEB_URL ?? defaults.web,
  learning: process.env.GAMEGUILD_LEARNING_URL ?? process.env.LEARNING_URL ?? defaults.learning,
  adminEmail: process.env.GAMEGUILD_SMOKE_ADMIN_EMAIL ?? 'admin@game-guild.com',
  adminPassword: process.env.GAMEGUILD_SMOKE_ADMIN_PASSWORD ?? 'Admin123!',
};

const expectedOrderOperations = [
  'get /v1/orders/{orderId}',
  'post /v1/orders',
  'post /v1/orders/{orderId}/items',
  'post /v1/orders/{orderId}:capture',
  'post /v1/orders/{orderId}:complete',
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
  ['learning dashboard', config.web, '/dashboard/learning/courses'],
  ['learning root', config.learning, '/'],
  ['learning sign in', config.learning, '/sign-in'],
  ['learning favicon', config.learning, '/favicon.svg'],
  ['learning manifest', config.learning, '/manifest.webmanifest'],
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
  process.exit(1);
}

console.log(`Smoke check passed: ${results.length}/${results.length} checks passed.`);
