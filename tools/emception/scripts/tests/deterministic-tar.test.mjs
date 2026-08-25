import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { test } from 'node:test';

import { createDeterministicTar } from '../lib/deterministic-tar.ts';

test('createDeterministicTar emits stable metadata and bytes', () => {
  const entries = [
    { path: '/usr/bin/tool', data: new TextEncoder().encode('tool'), executable: true },
    { path: '/usr/include/tool.h', data: new TextEncoder().encode('#define TOOL 1\n') },
  ];
  const first = createDeterministicTar(entries);
  const second = createDeterministicTar(entries);

  assert.equal(first.subarray(136, 148).toString('utf8'), '00000000000\0');
  assert.equal(createHash('sha256').update(first).digest('hex'), createHash('sha256').update(second).digest('hex'));
  assert.deepEqual(first, second);
});
