import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { test } from 'node:test';

const emceptionRoot = path.resolve(import.meta.dirname, '..', '..');
const repoRoot = path.resolve(emceptionRoot, '..', '..');

const EMCEPTION_PACKAGES = [
  'emception',
  '@gameguild/emception-toolchain',
  '@gameguild/emception-browser',
  '@gameguild/emception-xterm',
  '@gameguild/emception-react',
  '@gameguild/emception-webcomponent',
  '@gameguild/emception-ide',
];

async function json(filename) {
  return JSON.parse(await readFile(filename, 'utf8'));
}

test('published Emception packages have one version and the intended dependency direction', async () => {
  const packageDirectories = ['core', 'toolchain', 'browser', 'xterm', 'react', 'webcomponent', 'ide'];
  const packages = Object.fromEntries(await Promise.all(packageDirectories.map(async (directory) => {
    const manifest = await json(path.join(emceptionRoot, 'packages', directory, 'package.json'));
    return [manifest.name, manifest];
  })));
  const versions = new Set(EMCEPTION_PACKAGES.map((name) => packages[name].version));

  assert.equal(versions.size, 1);
  assert.equal(packages.emception.dependencies?.['@gameguild/emception-toolchain'], undefined);
  assert.equal(packages['@gameguild/emception-browser'].dependencies.emception, packages.emception.version);
  assert.equal(packages['@gameguild/emception-browser'].dependencies['@gameguild/emception-toolchain'], undefined);
  assert.equal(packages['@gameguild/emception-ide'].dependencies.emception, packages.emception.version);
  assert.equal(
    packages['@gameguild/emception-ide'].dependencies['@gameguild/emception-browser'],
    packages.emception.version,
  );
  assert.equal(packages['@gameguild/emception-ide'].dependencies['@gameguild/emception-toolchain'], undefined);
});

test('Changesets fixes only the seven public Emception packages together', async () => {
  const config = await json(path.join(repoRoot, '.changeset', 'config.json'));
  const expected = [...EMCEPTION_PACKAGES].sort();
  const emceptionGroups = config.fixed.filter((group) => group.includes('emception'));

  assert.equal(emceptionGroups.length, 1);
  assert.deepEqual([...emceptionGroups[0]].sort(), expected);
  assert.equal(config.fixed.some((group) => group.includes('emception-workspace')), false);
  assert.equal(config.fixed.some((group) => group.some((name) => name.startsWith('@gameguild/emception-demo-'))), false);
});

test('Emception versioning rejects package manifests outside the seven-package release group', async () => {
  const { assertOnlyEmceptionPackageManifests } = await import(
    '../../../../scripts/devops/emception-release-policy.mjs'
  );
  const allowed = [
    'tools/emception/packages/core/package.json',
    'tools/emception/packages/toolchain/package.json',
    'tools/emception/packages/browser/package.json',
    'tools/emception/packages/xterm/package.json',
    'tools/emception/packages/react/package.json',
    'tools/emception/packages/webcomponent/package.json',
    'tools/emception/packages/ide/package.json',
    'pnpm-lock.yaml',
  ];

  assert.doesNotThrow(() => assertOnlyEmceptionPackageManifests(allowed));
  assert.throws(
    () => assertOnlyEmceptionPackageManifests([...allowed, 'packages/infrastructure/ui-emception/package.json']),
    /outside the Emception release group/,
  );
  assert.throws(
    () => assertOnlyEmceptionPackageManifests([...allowed, 'tools/emception/package.json']),
    /outside the Emception release group/,
  );
});
