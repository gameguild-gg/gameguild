import assert from 'node:assert/strict';
import { mkdir, mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
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
  assert.equal(config.ignore.includes('@game-guild/emception-ui'), true);
  assert.equal(config.ignore.includes('emception-workspace'), true);
  assert.equal(config.ignore.filter((name) => name.startsWith('@gameguild/emception-demo-')).length, 4);
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

test('publication packs Toolchain first and waits for the registry before consumers', async (context) => {
  const { EMCEPTION_PUBLISH_ORDER, publishEmception } = await import(
    '../../../../scripts/devops/publish-emception.mjs'
  );
  const root = await mkdtemp(path.join(tmpdir(), 'emception-publish-'));
  context.after(() => rm(root, { recursive: true, force: true }));
  const published = [];
  let toolchainVisible = false;
  for (const entry of EMCEPTION_PUBLISH_ORDER) {
    const packageRoot = path.join(root, entry.directory);
    await mkdir(packageRoot, { recursive: true });
    await writeFile(path.join(packageRoot, 'package.json'), `${JSON.stringify({ name: entry.name, version: '1.2.3' })}\n`);
  }
  const runCommand = (_command, args) => {
    if (args[0] === 'view') {
      return { status: args[1].startsWith('@gameguild/emception-toolchain@') && toolchainVisible ? 0 : 1, stdout: '' };
    }
    if (args[0] === 'publish') {
      const entry = EMCEPTION_PUBLISH_ORDER.find(({ directory }) => args[1] === `./${directory}`);
      published.push(entry.name);
      if (entry.name === '@gameguild/emception-toolchain') toolchainVisible = true;
    }
    return { status: 0, stdout: '[]' };
  };

  await publishEmception({ repoRoot: root, runCommand, wait: async () => {} });

  assert.equal(EMCEPTION_PUBLISH_ORDER[0].name, '@gameguild/emception-toolchain');
  assert.deepEqual(published, EMCEPTION_PUBLISH_ORDER.map(({ name }) => name));
});
