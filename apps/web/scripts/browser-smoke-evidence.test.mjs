import assert from 'node:assert/strict';
import { mkdtemp, readFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import test from 'node:test';

import { writeBrowserEvidence } from './browser-smoke-evidence.mjs';

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
