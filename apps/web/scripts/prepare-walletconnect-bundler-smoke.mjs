#!/usr/bin/env node
// Hosted fixture preparation only. Never remove a developer's installed package.
import assert from 'node:assert/strict';
import { existsSync, readFileSync, realpathSync } from 'node:fs';
import { mkdir, readdir, rm, writeFile } from 'node:fs/promises';
import { createRequire } from 'node:module';
import path from 'node:path';

assert.equal(process.env.GITHUB_ACTIONS, 'true');
assert.equal(process.platform, 'linux');
const root = realpathSync(process.env.GITHUB_WORKSPACE);
const state = process.argv[2];
assert.ok(['absent', 'installed'].includes(state));
const evidence = path.resolve(process.argv[3]);
assert.ok(evidence.startsWith(root + path.sep));
await mkdir(evidence, { recursive: true });
const sdk = '@walletconnect/ethereum-provider';
const web3 = path.join(root, 'packages/features/web3');
const probes = [root, path.join(root, 'apps/web'), web3].map(directory =>
  createRequire(path.join(directory, 'package.json')));
const removed = [];
let sdkVersion;
if (state === 'absent') {
  // pnpm can expose the SDK through direct and hoisted links. Remove its actual
  // package as well, so a hidden virtual-store copy cannot satisfy the smoke.
  const store = path.join(root, 'node_modules/.pnpm');
  for (const entry of await readdir(store)) {
    if (!entry.startsWith('@walletconnect+ethereum-provider@')) continue;
    const packagePath = path.join(store, entry, 'node_modules', sdk);
    if (!existsSync(packagePath)) continue;
    const resolved = realpathSync(packagePath);
    assert.ok(resolved.startsWith(store + path.sep));
    const manifest = JSON.parse(readFileSync(path.join(resolved, 'package.json'), 'utf8'));
    assert.equal(manifest.name, sdk);
    await rm(resolved, { recursive: true });
    removed.push(path.relative(root, resolved));
  }
  for (const probe of probes) {
    assert.throws(() => probe.resolve(sdk), { code: 'MODULE_NOT_FOUND' },
      'The disabled test must run with an unresolvable, physically absent SDK.');
  }
} else {
  const probe = probes[2];
  const entry = probe.resolve(sdk);
  let directory = path.dirname(entry);
  while (!existsSync(path.join(directory, 'package.json'))) directory = path.dirname(directory);
  const manifest = JSON.parse(readFileSync(path.join(directory, 'package.json'), 'utf8'));
  assert.equal(manifest.name, sdk);
  sdkVersion = manifest.version;
}
await writeFile(path.join(evidence, 'sdk-installation.json'), JSON.stringify({ state, removed, sdkVersion }, null, 2));
