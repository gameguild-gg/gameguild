import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import test from 'node:test';
import { codingSurfaceFixtureOptions, buildCodingSurfaceFixture } from './coding-surface-fixture-build.mjs';

const repo = fileURLToPath(new URL('../../../', import.meta.url));
const output = resolve(repo, 'apps/web/test-results/coding-surface-refoundation/fixture-contract/bundle');
for (const kind of ['authoring', 'exercise'])
  test(`${kind} fixture uses the exact production test entry without runtime boot`, () => {
    const options = codingSurfaceFixtureOptions(kind, output);
    assert.equal(options.build.outDir, output);
    assert.equal(options.build.lib.fileName(), `${kind}.js`);
    assert.equal(options.build.lib.cssFileName, kind);
    assert.equal(options.configFile, false);
    assert.equal(options.publicDir, false);
    assert.equal(dirname(options.build.lib.entry), resolve(repo, 'packages/features/coding-surface/src/testing'));
  });
test('unknown fixtures and output outside owned evidence fail before spawning', async () => {
  assert.throws(() => codingSurfaceFixtureOptions('unknown', output), /Unknown/);
  assert.throws(() => codingSurfaceFixtureOptions('exercise', '/tmp/other-evidence'), /owned test evidence/);
  await assert.rejects(buildCodingSurfaceFixture('exercise', '/tmp/other-evidence'), /owned test evidence/);
});
test('parent harnesses do not keep Vite resident alongside the browser', async () => {
  for (const name of ['coding-surface-authoring-e2e.mjs', 'coding-surface-exercise-validation-e2e.mjs']) {
    const source = await readFile(new URL(name, import.meta.url), 'utf8');
    assert.doesNotMatch(source, /(?:from|import\()\s*['"]vite['"]/);
    assert.match(source, /await buildCodingSurfaceFixture\(/);
  }
});
