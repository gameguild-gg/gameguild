import assert from 'node:assert/strict';
import test from 'node:test';
import { createReleaseManifest } from '../release-manifest.mjs';

const releaseSha = 'a'.repeat(40);
const treeSha = 'b'.repeat(40);
const imageDigest = `sha256:${'c'.repeat(64)}`;

test('creates a deterministic manifest for the exact promoted digests', () => {
  const manifest = createReleaseManifest({
    releaseSha,
    treeSha,
    releasedAt: '2026-09-01T12:05:00Z',
    migrationRequired: true,
    verificationRunIds: ['1234'],
    services: [
      {
        service: 'web',
        image: 'registry.example/gameguild-web',
        imageDigest,
        sourceSha: releaseSha,
        releaseSha,
        treeSha,
      },
    ],
  });

  assert.deepEqual(manifest, {
    schemaVersion: 1,
    releaseSha,
    treeSha,
    releasedAt: '2026-09-01T12:05:00Z',
    migrationRequired: true,
    verificationRunIds: ['1234'],
    services: [
      {
        service: 'web',
        image: 'registry.example/gameguild-web',
        imageDigest,
        sourceSha: releaseSha,
        releaseSha,
        treeSha,
      },
    ],
  });
});

test('rejects a digest that is not immutable', () => {
  assert.throws(
    () =>
      createReleaseManifest({
        releaseSha,
        treeSha,
        releasedAt: '2026-09-01T12:05:00Z',
        migrationRequired: false,
        verificationRunIds: [],
        services: [
          {
            service: 'api',
            image: 'registry.example/gameguild-api',
            imageDigest: 'latest',
            sourceSha: releaseSha,
          },
        ],
      }),
    /immutable sha256 digest/u,
  );
});

test('rejects duplicate services in one release', () => {
  const service = {
    service: 'api',
    image: 'registry.example/gameguild-api',
    imageDigest,
    sourceSha: releaseSha,
  };

  assert.throws(
    () =>
      createReleaseManifest({
        releaseSha,
        treeSha,
        releasedAt: '2026-09-01T12:05:00Z',
        migrationRequired: false,
        verificationRunIds: [],
        services: [service, service],
      }),
    /duplicate service api/u,
  );
});
