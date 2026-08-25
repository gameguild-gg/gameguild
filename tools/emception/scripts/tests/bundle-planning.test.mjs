import assert from 'node:assert/strict';
import test from 'node:test';

test('fully deduplicated bundle groups are pruned before archive jobs are created', async () => {
  const { pruneFullyDeduplicatedBundles } = await import('../lib/bundle-planning.ts');
  const bundleFiles = new Map([
    ['cache-core', ['/usr/lib/libc.a']],
    ['usr-lib-misc', ['/usr/lib/libduplicate.a']],
  ]);
  const manifestFiles = {
    '/usr/lib/libc.a': { hash: 'a'.repeat(64), size: 4 },
    '/usr/lib/libduplicate.a': { symlink: '/usr/lib/libc.a' },
  };

  assert.deepEqual(pruneFullyDeduplicatedBundles(bundleFiles, manifestFiles), ['usr-lib-misc']);
  assert.deepEqual([...bundleFiles.keys()], ['cache-core']);
});
