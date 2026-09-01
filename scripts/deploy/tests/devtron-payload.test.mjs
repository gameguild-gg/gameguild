import assert from 'node:assert/strict';
import test from 'node:test';
import { createDevtronExternalCiPayload } from '../devtron-payload.mjs';

test('creates the documented Devtron external CI webhook contract', () => {
  const payload = createDevtronExternalCiPayload({
    image: 'registry.example/gameguild-api',
    tag: `release-${'a'.repeat(40)}`,
    digest: `sha256:${'b'.repeat(64)}`,
    releaseSha: 'a'.repeat(40),
    repository: 'https://github.com/gameguild-gg/gameguild.git',
    commitTime: '2026-09-01T12:05:00Z',
    message: 'release main',
    author: 'GitHub Actions',
  });

  assert.deepEqual(payload, {
    dockerImage: `registry.example/gameguild-api:release-${'a'.repeat(40)}`,
    digest: `sha256:${'b'.repeat(64)}`,
    ciProjectDetails: [
      {
        gitRepository: 'https://github.com/gameguild-gg/gameguild.git',
        checkoutPath: './',
        commitHash: 'a'.repeat(40),
        commitTime: '2026-09-01T12:05:00Z',
        branch: 'main',
        sourceValue: 'main',
        message: 'release main',
        author: 'GitHub Actions',
      },
    ],
  });
});

test('rejects a mutable or malformed image digest', () => {
  assert.throws(
    () =>
      createDevtronExternalCiPayload({
        image: 'registry.example/gameguild-api',
        tag: 'release-main',
        digest: 'latest',
        releaseSha: 'a'.repeat(40),
        repository: 'https://github.com/gameguild-gg/gameguild.git',
        commitTime: '2026-09-01T12:05:00Z',
      }),
    /immutable sha256 digest/u,
  );
});
