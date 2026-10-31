import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { existsSync, readFileSync, readdirSync, realpathSync } from 'node:fs';
import { createRequire } from 'node:module';
import { dirname, join, resolve } from 'node:path';
import { test } from 'node:test';
import { fileURLToPath } from 'node:url';
import { runInNewContext } from 'node:vm';
import { readPnpmSecurityConfig } from '../read-pnpm-config.mjs';

const repositoryRoot = resolve(dirname(fileURLToPath(import.meta.url)), '../../..');
const virtualStore = join(repositoryRoot, 'node_modules/.pnpm');
const require = createRequire(import.meta.url);
const packageRoots = new Set();
const consumers = [];
for (const entry of readdirSync(virtualStore, { withFileTypes: true })) {
  if (!entry.isDirectory() || entry.name.startsWith('deepmerge@')) continue;
  const candidate = join(virtualStore, entry.name, 'node_modules/deepmerge');
  if (!existsSync(candidate)) continue;
  const packageRoot = realpathSync(candidate);
  packageRoots.add(packageRoot);
  consumers.push({ name: entry.name, packageRoot, require: createRequire(join(virtualStore, entry.name, 'security-probe.cjs')) });
}
assert.ok(packageRoots.size > 0, 'The installed consumer graph must contain deepmerge');

test('the deepmerge mitigation requires the exact patched bytes on every installed consumer', () => {
  const config = readPnpmSecurityConfig(repositoryRoot);
  assert.equal(config.patchedDependencies?.['deepmerge@4.3.1'], 'patches/deepmerge@4.3.1.patch');
  assert.equal(config.auditConfig, undefined);
  assert.deepEqual(config.onlyBuiltDependencies, ['braces', 'deepmerge', 'eslint-plugin-import', 'glob', 'minimatch', 'sprintf-js']);
  assert.equal(createHash('sha256').update(readFileSync(join(repositoryRoot, 'patches/deepmerge@4.3.1.patch'))).digest('hex'), 'ce7bc8c614caad7eae9afd0ccb7c63767c693fed9df668af1a4dbb8516664919');
  const expected = {
    'index.js': 'dbef7cab6abdd1338228a08297f0047b3f56c9d7533b38358629b6024c78dbc7',
    'dist/cjs.js': '8983763adcccc772c023cb97bbf8830de52f7eeaa3c997b867345e1e62dd9d3b',
    'dist/umd.js': '61f125f6ee85c534ddafcd12d2e89c52acd0cd2c9cd292b111cd403ab58e5172',
  };
  for (const packageRoot of packageRoots) {
    assert.match(packageRoot, /deepmerge@4\.3\.1_patch_hash[=_]/);
    const manifest = JSON.parse(readFileSync(join(packageRoot, 'package.json'), 'utf8'));
    for (const hook of ['preinstall', 'install', 'postinstall']) assert.equal(manifest.scripts?.[hook], undefined);
    for (const [entry, digest] of Object.entries(expected)) {
      assert.equal(createHash('sha256').update(readFileSync(join(packageRoot, entry))).digest('hex'), digest, entry);
    }
  }
});

for (const consumer of consumers) {
  test(`${consumer.name}: actual dependency resolution uses the reviewed deepmerge version`, () => {
    assert.equal(consumer.require('deepmerge/package.json').version, '4.3.1');
    assert.equal(realpathSync(consumer.require.resolve('deepmerge')), join(consumer.packageRoot, 'dist/cjs.js'));
  });
}

for (const packageRoot of packageRoots) {
  const browser = {};
  runInNewContext(readFileSync(join(packageRoot, 'dist/umd.js'), 'utf8'), browser);
  const entries = [
    ['public CommonJS entry', require(packageRoot)],
    ['UMD CommonJS entry', require(join(packageRoot, 'dist/umd.js'))],
    ['browser UMD global', browser.deepmerge],
  ];
  for (const [entry, merge] of entries) {
    const expectedPrototype = Object.getPrototypeOf(merge({}, {}));
    const assertSafe = (value) => {
      assert.equal(Object.getPrototypeOf(value), expectedPrototype);
      assert.equal(value.isAdmin, undefined, 'Merged values must not inherit attacker-controlled fields');
      assert.equal(Object.hasOwn(value, '__proto__'), false);
      assert.equal({}.isAdmin, undefined);
    };
    const payload = () => JSON.parse('{"__proto__":{"isAdmin":true},"value":7}');
    test(`${entry}: null-prototype defaults cannot acquire the payload prototype`, () => {
      const target = Object.assign(Object.create(null), { enabled: false });
      const result = merge(target, payload());
      assertSafe(result);
      assert.equal(result.enabled, false);
      assert.equal(result.value, 7);
    });
    test(`${entry}: null and primitive placeholders cannot acquire the payload prototype`, () => {
      for (const placeholder of [null, undefined, false, 0, '']) {
        const result = merge({ session: placeholder }, { session: payload() });
        assertSafe(result.session);
        assert.equal(result.session.value, 7);
      }
    });
    test(`${entry}: target-owned proto keys are not copied to the destination`, () => {
      const result = merge(payload(), { enabled: true });
      assertSafe(result);
      assert.equal(result.value, 7);
      assert.equal(result.enabled, true);
    });
    test(`${entry}: own proto keys on both inputs cannot poison the destination`, () => {
      const result = merge(payload(), payload());
      assertSafe(result);
      assert.equal(result.value, 7);
    });
    test(`${entry}: array elements and merge.all retain ordinary prototypes`, () => {
      const result = merge({ list: [] }, { list: [payload()] });
      assertSafe(result.list[0]);
      assertSafe(merge.all([Object.create(null), payload()]));
    });
    test(`${entry}: symbols, nested values, arrays and documented options stay compatible`, () => {
      const symbol = Symbol('ordinary');
      const target = { nested: { a: 1 }, list: [1], [symbol]: 'target' };
      const source = { nested: { b: 2 }, list: [2], [symbol]: 'source' };
      const result = merge(target, source);
      assert.equal(result.nested.a, 1);
      assert.equal(result.nested.b, 2);
      assert.equal(result[symbol], 'source');
      assert.equal(result.list.join(','), '1,2');
      assert.equal(target.list.join(','), '1');
      assert.equal(merge(target, source, { arrayMerge: (_target, incoming) => incoming }).list, source.list);
      const shared = { value: 1 };
      assert.equal(merge({}, { shared }, { clone: false }).shared, shared);
      const custom = merge({ nested: { a: 1 } }, { nested: { b: 2 } }, { customMerge: (key) => key === 'nested' ? () => 'custom' : undefined });
      assert.equal(custom.nested, 'custom');
    });
  }
}
