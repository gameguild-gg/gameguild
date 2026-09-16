#!/usr/bin/env node

import { pathToFileURL } from 'node:url';

const supportedServices = new Set(['api', 'web', 'learning']);

export function healthPathForService(service) {
  if (service === 'api') return '/health';
  if (service === 'web') return '/api/health';
  if (service === 'learning') return '/';
  throw new TypeError(`unsupported service ${service}`);
}

export function validateReleaseResponse(service, response, expected) {
  if (!supportedServices.has(service)) return { ok: false, error: `unsupported service ${service}` };
  if (!Number.isInteger(response?.status) || response.status < 200 || response.status >= 400) {
    return { ok: false, error: `unexpected HTTP status ${response?.status ?? 'unknown'}` };
  }
  if (service === 'learning') return { ok: true };

  const failures = [];
  const headerReleaseSha = response.headers?.get?.('X-GameGuild-Release-Sha');
  if (headerReleaseSha !== expected.releaseSha) failures.push('X-GameGuild-Release-Sha');
  if (response.body?.releaseSha !== expected.releaseSha) failures.push('releaseSha');
  if (response.body?.sourceTree !== expected.treeSha) failures.push('sourceTree');
  if (response.body?.imageDigest !== expected.imageDigest) failures.push('imageDigest');

  for (const field of ['version', 'builtAt', 'deployedAt']) {
    if (!response.body?.[field] || response.body[field] === 'Unknown') failures.push(field);
  }

  return failures.length === 0
    ? { ok: true }
    : { ok: false, error: `release identity mismatch: ${failures.join(', ')}` };
}

function parseArguments(argv) {
  const options = {};
  for (let index = 0; index < argv.length; index += 2) {
    const name = argv[index];
    const value = argv[index + 1];
    if (!name?.startsWith('--') || value === undefined) {
      throw new TypeError(`Unknown or incomplete argument: ${name ?? ''}`);
    }
    options[name.slice(2)] = value;
  }
  return options;
}

async function readBody(response) {
  try {
    return await response.json();
  } catch {
    return null;
  }
}

async function main() {
  const options = parseArguments(process.argv.slice(2));
  const service = options.service;
  const baseUrl = options.url;
  if (!supportedServices.has(service)) throw new TypeError(`unsupported service ${service ?? ''}`);
  if (!baseUrl) throw new TypeError('url is required');

  const expected = {
    releaseSha: options['release-sha'],
    treeSha: options['tree-sha'],
    imageDigest: options.digest,
  };
  const timeoutMs = Number.parseInt(options['timeout-ms'] ?? '300000', 10);
  const intervalMs = Number.parseInt(options['interval-ms'] ?? '5000', 10);
  const deadline = Date.now() + timeoutMs;
  const url = new URL(healthPathForService(service), baseUrl.endsWith('/') ? baseUrl : `${baseUrl}/`);
  let lastError = 'release did not become healthy';

  while (Date.now() <= deadline) {
    try {
      const response = await fetch(url, {
        cache: 'no-store',
        redirect: 'follow',
        signal: AbortSignal.timeout(Math.min(intervalMs, 15000)),
        headers: {
          'Cache-Control': 'no-cache, no-store, max-age=0',
          Pragma: 'no-cache',
          'User-Agent': 'gameguild-release-verifier/1.0',
        },
      });
      const result = validateReleaseResponse(
        service,
        { status: response.status, headers: response.headers, body: await readBody(response) },
        expected,
      );
      if (result.ok) {
        process.stdout.write(`Verified ${service} release ${expected.releaseSha} at ${url}\n`);
        return;
      }
      lastError = result.error;
    } catch (error) {
      lastError = error instanceof Error ? error.message : String(error);
    }

    if (Date.now() + intervalMs <= deadline) {
      await new Promise((resolve) => setTimeout(resolve, intervalMs));
    }
  }

  throw new Error(`Timed out verifying ${service} at ${url}: ${lastError}`);
}

if (import.meta.url === pathToFileURL(process.argv[1] ?? '').href) {
  await main();
}
