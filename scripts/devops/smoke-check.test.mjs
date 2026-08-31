import assert from 'node:assert/strict';
import { spawn } from 'node:child_process';
import { once } from 'node:events';
import { createServer } from 'node:http';
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

async function runSmokeWithOpenApi(paths) {
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
  const child = spawn(process.execPath, [smokeScript], {
    env: {
      ...process.env,
      GAMEGUILD_API_URL: origin,
      GAMEGUILD_WEB_URL: origin,
      GAMEGUILD_LEARNING_URL: origin,
      SMOKE_RETRIES: '0',
      SMOKE_TIMEOUT_MS: '5000',
    },
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

  return { exitCode, requestedPaths, stderr, stdout };
}

test('deployment smoke checks liveness, readiness, and the verified Orders OpenAPI surface', async () => {
  const result = await runSmokeWithOpenApi(expectedOrderOperations);

  assert.equal(result.exitCode, 0, result.stderr || result.stdout);
  assert.ok(result.requestedPaths.includes('/live'));
  assert.ok(result.requestedPaths.includes('/ready'));
  assert.ok(result.requestedPaths.includes('/swagger/v1/swagger.json'));
});

test('deployment smoke fails when an expected Orders OpenAPI operation is missing', async () => {
  const paths = structuredClone(expectedOrderOperations);
  delete paths['/v1/orders/{orderId}:complete'];

  const result = await runSmokeWithOpenApi(paths);

  assert.equal(result.exitCode, 1, result.stdout);
  assert.match(result.stderr, /Orders OpenAPI operations mismatch/);
});

test('deployment smoke fails when an unexpected Orders OpenAPI operation is exposed', async () => {
  const paths = structuredClone(expectedOrderOperations);
  paths['/v1/orders/{orderId}:refund'] = { post: {} };

  const result = await runSmokeWithOpenApi(paths);

  assert.equal(result.exitCode, 1, result.stdout);
  assert.match(result.stderr, /Orders OpenAPI operations mismatch/);
});
