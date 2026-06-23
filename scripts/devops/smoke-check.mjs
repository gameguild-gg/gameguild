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
      api: 'http://localhost:5296',
      web: 'http://localhost:3005',
      learning: 'http://localhost:3006',
    };

const config = {
  api: process.env.GAMEGUILD_API_URL ?? process.env.API_URL ?? defaults.api,
  web: process.env.GAMEGUILD_WEB_URL ?? process.env.WEB_URL ?? defaults.web,
  learning: process.env.GAMEGUILD_LEARNING_URL ?? process.env.LEARNING_URL ?? defaults.learning,
};

const checks = [
  ['api live', config.api, '/live'],
  ['api health', config.api, '/health'],
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

const results = await Promise.all(checks.map(runCheck));
const nameWidth = Math.max(...results.map((result) => result.name.length));

for (const result of results) {
  const marker = result.ok ? 'PASS' : 'FAIL';
  const status = String(result.status).padEnd(3, ' ');
  const attempts = result.attempts > 1 ? ` attempts=${result.attempts}` : '';
  const message = result.error ? ` ${result.error}` : '';
  console.log(`${marker} ${result.name.padEnd(nameWidth, ' ')} ${status} ${result.elapsed}ms ${result.url}${attempts}${message}`);
}

const failed = results.filter((result) => !result.ok);
if (failed.length > 0) {
  console.error(`Smoke check failed: ${failed.length}/${results.length} checks failed.`);
  process.exit(1);
}

console.log(`Smoke check passed: ${results.length}/${results.length} checks passed.`);
