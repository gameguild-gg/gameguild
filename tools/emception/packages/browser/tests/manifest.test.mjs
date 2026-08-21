import assert from 'node:assert/strict';
import { test } from 'node:test';

import { DEFAULT_MANIFEST_URL, ManifestCompatibilityError, RUNTIME_ABI, parseManifest } from '../dist/manifest.js';

function releaseManifest(overrides = {}) {
  return {
    schemaVersion: 2,
    version: 2,
    artifactVersion: '4.2.0',
    runtimeAbi: RUNTIME_ABI,
    patchSetVersion: 'emception-glue-v2',
    buildFingerprint: 'a'.repeat(64),
    generated: '2026-08-20T00:00:00Z',
    baseUrl: '/cdn',
    toolVersions: { pythonMajorMinor: '3.13', pythonMajorMinorCompact: '313' },
    profiles: {},
    files: {},
    bundles: {},
    ...overrides,
  };
}

test('parseManifest accepts a schema-v2 manifest with the supported runtime ABI', () => {
  const parsed = parseManifest(releaseManifest());
  assert.equal(parsed.schemaVersion, 2);
  assert.equal(parsed.runtimeAbi, RUNTIME_ABI);
});

test('default manifest URL is versioned and owned by the toolchain package', () => {
  assert.equal(
    DEFAULT_MANIFEST_URL,
    'https://cdn.jsdelivr.net/npm/@gameguild/emception-toolchain@4.2.0/cdn/manifest.json',
  );
});

test('parseManifest rejects a schema-v2 manifest built for another runtime ABI', () => {
  assert.throws(
    () => parseManifest(releaseManifest({ runtimeAbi: 'future-runtime-v9' })),
    (error) => error instanceof ManifestCompatibilityError && /future-runtime-v9/.test(error.message),
  );
});

test('parseManifest accepts manifest v1 only through the explicit legacy path', () => {
  const notices = [];
  const parsed = parseManifest({
    version: 1,
    generated: '2026-08-20T00:00:00Z',
    baseUrl: '/cdn',
    files: {},
    bundles: {},
  }, { onLegacy: (message) => notices.push(message) });

  assert.equal(parsed.version, 1);
  assert.equal(notices.length, 1);
  assert.match(notices[0], /deprecated manifest schema v1/);
});

test('parseManifest rejects malformed release maps before VFS initialization', () => {
  assert.throws(
    () => parseManifest(releaseManifest({ files: [] })),
    /manifest\.files must be an object map/,
  );
});

test('parseManifest rejects profiles that do not point to the matched release files', () => {
  assert.throws(
    () => parseManifest(releaseManifest({
      profiles: {
        clang: {
          kind: 'compiler',
          glue: '/usr/lib/clang.mjs',
          wasm: '/usr/lib/clang.wasm',
          profileHash: 'b'.repeat(64),
          imports: [],
          exports: [],
        },
      },
    })),
    /profile 'clang' references missing release file/,
  );
});
