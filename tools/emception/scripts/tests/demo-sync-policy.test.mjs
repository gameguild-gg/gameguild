import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { test } from 'node:test';

test('demo synchronization consumes only the canonical Toolchain release', async () => {
  const repoRoot = path.resolve(import.meta.dirname, '..', '..', '..', '..');
  const source = await readFile(path.join(repoRoot, 'scripts', 'sync-emception-cdn.mjs'), 'utf8');

  assert.match(source, /path\.join\(emceptionRoot, 'artifacts', 'toolchain', 'release', 'cdn'\)/);
  assert.doesNotMatch(source, /sourceBuildCdnDir|sourceManifestFile|sourcePublicCdnDir/);
  assert.doesNotMatch(source, /path\.join\(emceptionRoot, 'build'/);
  assert.doesNotMatch(source, /mode: 'build'|mode: 'public'/);
});
