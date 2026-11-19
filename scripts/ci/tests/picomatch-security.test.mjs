import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { existsSync, readFileSync, readdirSync, realpathSync } from 'node:fs';
import { createRequire } from 'node:module';
import { dirname, join, resolve } from 'node:path';
import { test } from 'node:test';
import { fileURLToPath } from 'node:url';
import { readPnpmSecurityConfig } from '../read-pnpm-config.mjs';

const repositoryRoot = resolve(dirname(fileURLToPath(import.meta.url)), '../../..');
const virtualStore = join(repositoryRoot, 'node_modules/.pnpm');

// GHSA-fc5q-39xc-vjpf / CVE-2026-112366: single-branch repeated extglobs
// (`+(*)`, `+(*a)`) leave analyzeRepeatedExtglob unclassified and compile to a
// catastrophically backtracking regex. No upstream picomatch release fixes it,
// so both installed majors carry a reviewed local patch that classifies every
// lone `*`-bearing branch as risky (collapsing the extglob to a safe output).
const patchedVersions = [
  {
    version: '4.0.7',
    storePattern: /^picomatch@4\.0\.7_patch_hash[=_]/,
    parseJsSha256: '40143298127b16c13d49f3d80718a94070ac9b8f0f8f0c74141059591040f5c5',
    patchFile: 'patches/picomatch@4.0.7.patch',
    patchSha256: 'beffacc02fc9379335e984cf8fbe952d97efc9f2fa83eb02a2591e041e185ac7',
  },
  {
    version: '2.3.2',
    storePattern: /^picomatch@2\.3\.2_patch_hash[=_]/,
    parseJsSha256: '5b421fa2f2d131fb47b8b4d3816380b4413917c45f583515f1652d73ff818865',
    patchFile: 'patches/picomatch@2.3.2.patch',
    patchSha256: 'd95faf7f0405debf81f0a98c7bd9a5e5164aa27c3590fb8c97b2cecb1473fb1a',
  },
];

const installedRoots = [];
for (const entry of readdirSync(virtualStore, { withFileTypes: true })) {
  if (!entry.isDirectory()) continue;
  for (const expected of patchedVersions) {
    if (!expected.storePattern.test(entry.name)) continue;
    const candidate = join(virtualStore, entry.name, 'node_modules/picomatch');
    if (!existsSync(candidate)) continue;
    installedRoots.push({ entryName: entry.name, packageRoot: realpathSync(candidate), expected });
  }
}
assert.ok(installedRoots.length === patchedVersions.length, 'Every installed picomatch major must be the locally patched copy');

test('the picomatch mitigation requires the exact patched bytes on every installed major', () => {
  const config = readPnpmSecurityConfig(repositoryRoot);
  assert.equal(config.patchedDependencies?.['picomatch@4.0.7'], 'patches/picomatch@4.0.7.patch');
  assert.equal(config.patchedDependencies?.['picomatch@2.3.2'], 'patches/picomatch@2.3.2.patch');
  assert.equal(config.auditConfig, undefined);
  for (const expected of patchedVersions) {
    assert.equal(
      createHash('sha256').update(readFileSync(join(repositoryRoot, expected.patchFile))).digest('hex'),
      expected.patchSha256,
      expected.patchFile,
    );
  }
  for (const { packageRoot, expected } of installedRoots) {
    const manifest = JSON.parse(readFileSync(join(packageRoot, 'package.json'), 'utf8'));
    assert.equal(manifest.version, expected.version);
    for (const hook of ['preinstall', 'install', 'postinstall']) assert.equal(manifest.scripts?.[hook], undefined);
    assert.equal(
      createHash('sha256').update(readFileSync(join(packageRoot, 'lib/parse.js'))).digest('hex'),
      expected.parseJsSha256,
      `picomatch@${expected.version} lib/parse.js`,
    );
  }
});

for (const { entryName, packageRoot, expected } of installedRoots) {
  const require = createRequire(join(packageRoot, 'index.js'));
  const picomatch = require(join(packageRoot));
  const isMatch = picomatch.isMatch;

  test(`${entryName}: single-branch repeated extglobs collapse instead of compiling a backtracking regex`, () => {
    // The advisory's PoC shapes collapse to an escaped literal, so the pattern
    // matches only its own literal text and can never enter catastrophic
    // backtracking (unpatched 4.0.7 fails to complete a 40-char input in 10s).
    assert.equal(isMatch('+(*a)', '+(*a)'), true, 'collapsed pattern must match its literal text');
    assert.equal(isMatch('*a', '+(*)'), false, 'collapsed pattern must not match other inputs');
    assert.equal(isMatch('aaa', '+(*a)'), false);
    assert.equal(isMatch('abc', '+(a*)'), false);
    const started = process.hrtime.bigint();
    assert.equal(isMatch('a'.repeat(300) + '!', '+(*a)'), false);
    const elapsedMs = Number(process.hrtime.bigint() - started) / 1e6;
    assert.ok(elapsedMs < 500, `advisory input must complete fast (took ${elapsedMs.toFixed(2)}ms)`);
  });

  test(`${entryName}: star-extglob sequences still emit the flat safe char-class output`, () => {
    if (expected.version === '4.0.7') {
      // 4.x accumulates every branch into one char class: `+(*(a)|*(b))` -> `[ab]*`.
      assert.equal(isMatch('ab', '+(*(a)|*(b))'), true);
      assert.equal(isMatch('ba', '+(*(a)|*(b))'), true);
    } else {
      // 2.x returns the FIRST branch's flat output immediately: `+(*(a)|*(b))` -> `[a]*`.
      // Pre-existing semantics (unrelated to this patch) and still ReDoS-safe.
      assert.equal(isMatch('aa', '+(*(a)|*(b))'), true);
      assert.equal(isMatch('ab', '+(*(a)|*(b))'), false);
    }
    assert.equal(isMatch('abc', '+(*(a)|*(b))'), false);
  });

  test(`${entryName}: non-star extglob semantics are unchanged by the patch`, () => {
    assert.equal(isMatch('aaa', '+(a)'), true);
    assert.equal(isMatch('a', '+(a)'), true);
    assert.equal(isMatch('b', '+(a)'), false);
    assert.equal(isMatch('ab', '+(a|b)'), true);
    assert.equal(isMatch('ac', '+(a|b)'), false);
    assert.equal(isMatch('abcabc', '+(abc)'), true);
  });
}
