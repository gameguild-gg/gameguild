import assert from 'node:assert/strict';
import { spawn } from 'node:child_process';
import { once } from 'node:events';
import { mkdtemp, rm, writeFile } from 'node:fs/promises';
import { createServer } from 'node:http';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import test from 'node:test';

const smokeScript = fileURLToPath(new URL('./smoke-check.mjs', import.meta.url));

const expectedOrderOperations = {
  '/v1/orders': { get: {}, post: {} },
  '/v1/orders/{orderId}': { get: {} },
  '/v1/orders/{orderId}/items': { post: {} },
  '/v1/orders/{orderId}:capture': { post: {} },
  '/v1/orders/{orderId}:complete': { post: {} },
  '/v1/orders/{orderId}:payment-intent': { post: {} },
};

const expectedSocialOperations = {
  '/api/social/feed': { get: {} },
  '/api/social/saved-posts/{postId}': { delete: {}, put: {} },
  '/api/social/stories': { get: {}, post: {} },
  '/api/social/stories/{storyId}': { delete: {} },
  '/api/social/stories/{storyId}/views': { post: {} },
  '/api/v1/posts/{postId}': { delete: {}, put: {} },
};

const socialCapabilities = [
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

async function runSmokeWithOpenApi(paths, options = {}) {
  const requestedPaths = [];
  const server = createServer((request, response) => {
    requestedPaths.push(request.url);

    if (request.url === '/swagger/v1/swagger.json') {
      response.setHeader('Content-Type', 'application/json');
      response.end(JSON.stringify({ openapi: '3.0.1', paths }));
      return;
    }

    if (request.url === '/api/auth/csrf') {
      response.setHeader('Content-Type', 'application/json');
      response.setHeader('Set-Cookie', 'smoke-csrf=test; Path=/; HttpOnly');
      response.end(JSON.stringify({ csrfToken: 'test-token' }));
      return;
    }

    if (request.url === '/v1/auth/sign-in') {
      response.setHeader('Content-Type', 'application/json');
      response.end(JSON.stringify({ success: true }));
      return;
    }

    response.statusCode = 200;
    response.end('ok');
  });

  server.listen(0, '127.0.0.1');
  await once(server, 'listening');

  const { port } = server.address();
  const origin = `http://127.0.0.1:${port}`;
  const directory = await mkdtemp(join(tmpdir(), 'social-smoke-contract-'));
  const evidencePath = join(directory, 'evidence.json');
  if (options.writeEvidence !== false) {
    const capabilityOverrides = options.capabilities ?? {};
    const capabilities = Object.fromEntries(
      socialCapabilities.map((key) => [key, capabilityOverrides[key] ?? true]),
    );
    const passedCount = Object.values(capabilities).filter(Boolean).length;
    const passed = passedCount === socialCapabilities.length;
    await writeFile(evidencePath, JSON.stringify({
      schemaVersion: 1,
      status: passed ? 'passed' : 'failed',
      capabilities,
      aggregate: { passed, requiredCount: socialCapabilities.length, passedCount },
      errors: passed ? [] : ['fixture capability failed'],
      metadata: {
        journey: 'social-feed-browser-e2e',
        startedAt: new Date(Date.now() - 1000).toISOString(),
        completedAt: new Date().toISOString(),
        durationMs: 1000,
        apiBaseUrl: origin,
        webBaseUrl: origin,
        actorIds: ['actor-a', 'actor-b'],
      },
    }));
  }
  const childEnv = {
    ...process.env,
    GAMEGUILD_API_URL: origin,
    GAMEGUILD_WEB_URL: origin,
    GAMEGUILD_LEARNING_URL: origin,
    GAMEGUILD_SMOKE_ADMIN_EMAIL: 'admin@example.test',
    GAMEGUILD_SMOKE_ADMIN_PASSWORD: 'env-only-secret',
    SOCIAL_FEED_E2E_EVIDENCE_PATH: evidencePath,
    SMOKE_RETRIES: '0',
    SMOKE_TIMEOUT_MS: '5000',
  };
  delete childEnv.SMOKE_SOCIAL_EVIDENCE_MAX_AGE_MS;
  if (options.maxAge !== undefined) childEnv.SMOKE_SOCIAL_EVIDENCE_MAX_AGE_MS = options.maxAge;
  if (options.credentials === false) {
    delete childEnv.GAMEGUILD_SMOKE_ADMIN_EMAIL;
    delete childEnv.GAMEGUILD_SMOKE_ADMIN_PASSWORD;
  }
  const child = spawn(process.execPath, [smokeScript], {
    env: childEnv,
    stdio: ['ignore', 'pipe', 'pipe'],
  });

  let stdout = '';
  let stderr = '';
  child.stdout.setEncoding('utf8');
  child.stderr.setEncoding('utf8');
  child.stdout.on('data', (chunk) => {
    stdout += chunk;
  });
  child.stderr.on('data', (chunk) => {
    stderr += chunk;
  });

  const [exitCode] = await once(child, 'exit');
  server.close();
  await once(server, 'close');
  await rm(directory, { recursive: true, force: true });

  return { exitCode, requestedPaths, stderr, stdout };
}

test('deployment smoke checks health plus verified Orders and social OpenAPI surfaces and browser evidence', async () => {
  const result = await runSmokeWithOpenApi({ ...expectedOrderOperations, ...expectedSocialOperations });

  assert.equal(result.exitCode, 0, result.stderr || result.stdout);
  assert.ok(result.requestedPaths.includes('/live'));
  assert.ok(result.requestedPaths.includes('/ready'));
  assert.ok(result.requestedPaths.includes('/swagger/v1/swagger.json'));
  assert.match(result.stdout, /PASS Social production OpenAPI/);
  assert.match(result.stdout, /PASS Social non-admin browser evidence/);
});

test('deployment smoke fails when an expected Orders OpenAPI operation is missing', async () => {
  const paths = structuredClone(expectedOrderOperations);
  delete paths['/v1/orders/{orderId}:complete'];

  Object.assign(paths, expectedSocialOperations);
  const result = await runSmokeWithOpenApi(paths);

  assert.equal(result.exitCode, 1, result.stdout);
  assert.match(result.stderr, /Orders OpenAPI operations mismatch/);
});

test('deployment smoke fails when an unexpected Orders OpenAPI operation is exposed', async () => {
  const paths = structuredClone(expectedOrderOperations);
  paths['/v1/orders/{orderId}:refund'] = { post: {} };

  Object.assign(paths, expectedSocialOperations);
  const result = await runSmokeWithOpenApi(paths);

  assert.equal(result.exitCode, 1, result.stdout);
  assert.match(result.stderr, /Orders OpenAPI operations mismatch/);
});

test('deployment smoke fails when a required social OpenAPI mutation is missing', async () => {
  const paths = { ...expectedOrderOperations, ...structuredClone(expectedSocialOperations) };
  delete paths['/api/social/stories/{storyId}/views'].post;

  const result = await runSmokeWithOpenApi(paths);

  assert.equal(result.exitCode, 1, result.stdout);
  assert.match(result.stderr, /Social OpenAPI operations missing.*post \/api\/social\/stories\/\{storyId\}\/views/);
});

test('deployment smoke rejects browser evidence that did not prove cross-actor authorization', async () => {
  const paths = { ...expectedOrderOperations, ...expectedSocialOperations };
  const result = await runSmokeWithOpenApi(paths, {
    capabilities: { crossActorMutationForbidden: false },
  });

  assert.equal(result.exitCode, 1, result.stdout);
  assert.match(result.stderr, /Social browser evidence.*crossActorMutationForbidden/);
});

test('deployment smoke fails closed when auth credentials are not configured', async () => {
  const paths = { ...expectedOrderOperations, ...expectedSocialOperations };
  const result = await runSmokeWithOpenApi(paths, { credentials: false });

  assert.equal(result.exitCode, 1, result.stdout);
  assert.match(result.stderr, /GAMEGUILD_SMOKE_ADMIN_EMAIL.*GAMEGUILD_SMOKE_ADMIN_PASSWORD/);
  assert.equal(result.requestedPaths.includes('/v1/auth/sign-in'), false);
});

test('deployment smoke fails closed for malformed social evidence age configuration', async () => {
  const paths = { ...expectedOrderOperations, ...expectedSocialOperations };
  for (const maxAge of ['NaN', '-1', '1.5', 'Infinity']) {
    const result = await runSmokeWithOpenApi(paths, { maxAge });
    assert.equal(result.exitCode, 1, `${maxAge}: ${result.stdout}`);
    assert.match(result.stderr, /SMOKE_SOCIAL_EVIDENCE_MAX_AGE_MS must be a finite non-negative integer/);
  }
});
