import assert from 'node:assert/strict';
import { mkdtemp, readFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import test from 'node:test';

import { writeBrowserEvidence, writeSocialFeedEvidence } from './browser-smoke-evidence.mjs';

test('writes Playwright-compatible passing evidence', async () => {
  const directory = await mkdtemp(join(tmpdir(), 'browser-evidence-'));
  const output = join(directory, 'playwright.json');

  try {
    await writeBrowserEvidence(output, { passed: true, errors: [] });
    const evidence = JSON.parse(await readFile(output, 'utf8'));

    assert.deepEqual(evidence.stats, { expected: 1, unexpected: 0, skipped: 0 });
    assert.deepEqual(evidence.errors, []);
  } finally {
    await rm(directory, { recursive: true, force: true });
  }
});

test('writes failed evidence with captured browser errors', async () => {
  const directory = await mkdtemp(join(tmpdir(), 'browser-evidence-'));
  const output = join(directory, 'playwright.json');

  try {
    await writeBrowserEvidence(output, { passed: false, errors: ['GET /economy returned 500'] });
    const evidence = JSON.parse(await readFile(output, 'utf8'));

    assert.deepEqual(evidence.stats, { expected: 0, unexpected: 1, skipped: 0 });
    assert.deepEqual(evidence.errors, ['GET /economy returned 500']);
  } finally {
    await rm(directory, { recursive: true, force: true });
  }
});

test('writes structured social evidence with capability totals and safe run metadata', async () => {
  const directory = await mkdtemp(join(tmpdir(), 'browser-evidence-'));
  const output = join(directory, 'social.json');

  try {
    await writeSocialFeedEvidence(output, {
      capabilities: { nonAdminActors: true, crossActorMutationForbidden: true, published: true },
      errors: [],
      metadata: {
        startedAt: '2026-09-10T12:00:00.000Z',
        completedAt: '2026-09-10T12:00:03.000Z',
        durationMs: 3000,
        apiBaseUrl: 'https://api.example.test',
        webBaseUrl: 'https://web.example.test',
        tenantId: 'tenant-id',
        actorIds: ['actor-a', 'actor-b'],
        runTag: 'safe-run-tag',
        accessToken: 'must-not-leak',
        password: 'must-not-leak',
      },
      requiredCapabilities: ['nonAdminActors', 'crossActorMutationForbidden', 'published'],
    });
    const evidence = JSON.parse(await readFile(output, 'utf8'));

    assert.equal(evidence.status, 'passed');
    assert.deepEqual(evidence.aggregate, { passed: true, requiredCount: 3, passedCount: 3 });
    assert.deepEqual(evidence.capabilities, {
      nonAdminActors: true,
      crossActorMutationForbidden: true,
      published: true,
    });
    assert.equal(evidence.metadata.durationMs, 3000);
    assert.equal(evidence.metadata.accessToken, undefined);
    assert.equal(evidence.metadata.password, undefined);
    assert.doesNotMatch(JSON.stringify(evidence), /must-not-leak/);
  } finally {
    await rm(directory, { recursive: true, force: true });
  }
});

test('writes failed social evidence with partial capabilities and errors', async () => {
  const directory = await mkdtemp(join(tmpdir(), 'browser-evidence-'));
  const output = join(directory, 'social.json');

  try {
    await writeSocialFeedEvidence(output, {
      capabilities: { nonAdminActors: true, crossActorMutationForbidden: false },
      errors: ['DELETE /api/v1/posts/id unexpectedly returned 204'],
      metadata: { startedAt: '2026-09-10T12:00:00.000Z', completedAt: '2026-09-10T12:00:01.000Z' },
      requiredCapabilities: ['nonAdminActors', 'crossActorMutationForbidden'],
    });
    const evidence = JSON.parse(await readFile(output, 'utf8'));

    assert.equal(evidence.status, 'failed');
    assert.deepEqual(evidence.aggregate, { passed: false, requiredCount: 2, passedCount: 1 });
    assert.deepEqual(evidence.errors, ['DELETE /api/v1/posts/id unexpectedly returned 204']);
  } finally {
    await rm(directory, { recursive: true, force: true });
  }
});
