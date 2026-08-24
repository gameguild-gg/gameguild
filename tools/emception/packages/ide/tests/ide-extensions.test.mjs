import assert from 'node:assert/strict';
import test from 'node:test';

import { activateIdeExtensions, validateIdeExtensions } from '../dist/components/ide-extensions.js';

test('IDE extensions activate in declaration order and clean up in reverse order', () => {
  const events = [];
  const controller = { api: {}, getFiles: async () => [], replaceFiles: async () => {}, setFilesReadOnly: () => {} };
  const extensions = validateIdeExtensions([
    {
      id: 'first',
      onReady: () => {
        events.push('ready:first');
        return () => events.push('dispose:first');
      },
    },
    {
      id: 'second',
      onReady: () => {
        events.push('ready:second');
        return () => events.push('dispose:second');
      },
    },
  ]);

  const dispose = activateIdeExtensions(extensions, controller);
  assert.deepEqual(events, ['ready:first', 'ready:second']);

  dispose();
  assert.deepEqual(events, ['ready:first', 'ready:second', 'dispose:second', 'dispose:first']);
});

test('IDE extensions reject duplicate ids before any lifecycle hook runs', () => {
  assert.throws(
    () => validateIdeExtensions([{ id: 'duplicate' }, { id: 'duplicate' }]),
    /duplicate IDE extension id: duplicate/i,
  );
});
