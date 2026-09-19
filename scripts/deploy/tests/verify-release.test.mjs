import assert from 'node:assert/strict';
import test from 'node:test';
import { healthPathForService, validateReleaseResponse } from '../verify-release.mjs';

const expected = {
  releaseSha: 'a'.repeat(40),
  treeSha: 'b'.repeat(40),
  imageDigest: `sha256:${'c'.repeat(64)}`,
};

test('uses the public health route for each service', () => {
  assert.equal(healthPathForService('api'), '/health');
  assert.equal(healthPathForService('web'), '/api/health');
});

test('accepts only an exact Web or API release identity', () => {
  const response = {
    status: 200,
    headers: new Headers({ 'X-GameGuild-Release-Sha': expected.releaseSha }),
    body: {
      version: '4.3.0',
      releaseSha: expected.releaseSha,
      sourceTree: expected.treeSha,
      imageDigest: expected.imageDigest,
      builtAt: '2026-09-01T12:00:00Z',
      deployedAt: '2026-09-01T12:05:00Z',
    },
  };

  assert.deepEqual(validateReleaseResponse('web', response, expected), { ok: true });
  assert.deepEqual(validateReleaseResponse('api', response, expected), { ok: true });
});

test('rejects a healthy response from a different image', () => {
  const result = validateReleaseResponse(
    'web',
    {
      status: 200,
      headers: new Headers({ 'X-GameGuild-Release-Sha': expected.releaseSha }),
      body: {
        version: '4.3.0',
        releaseSha: expected.releaseSha,
        sourceTree: expected.treeSha,
        imageDigest: `sha256:${'d'.repeat(64)}`,
        builtAt: '2026-09-01T12:00:00Z',
        deployedAt: '2026-09-01T12:05:00Z',
      },
    },
    expected,
  );

  assert.equal(result.ok, false);
  assert.match(result.error, /imageDigest/u);
});

test('rejects services outside the API and Web deployment topology', () => {
  assert.throws(() => healthPathForService('learning'), /unsupported service learning/u);
  assert.deepEqual(
    validateReleaseResponse('learning', { status: 200, headers: new Headers(), body: null }, expected),
    { ok: false, error: 'unsupported service learning' },
  );
});
