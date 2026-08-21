import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import { deriveStorageKey, WORKSPACE_STORAGE_KEY } from '../dist/components/ide-types.js';

describe('WORKSPACE_STORAGE_KEY', () => {
  it('is package-specific without host-product branding', () => {
    assert.equal(WORKSPACE_STORAGE_KEY, 'emception.workspace.v1');
  });
});

describe('deriveStorageKey', () => {
  it('returns the default key when no workspace name is provided', () => {
    assert.equal(deriveStorageKey(), WORKSPACE_STORAGE_KEY);
    assert.equal(deriveStorageKey(undefined), WORKSPACE_STORAGE_KEY);
    assert.equal(deriveStorageKey(''), WORKSPACE_STORAGE_KEY);
  });

  it('namespaces a non-empty workspace name verbatim', () => {
    assert.equal(deriveStorageKey('demo'), 'emception:ws:demo');
    assert.equal(deriveStorageKey('hello world'), 'emception:ws:hello world');
  });

  it('keeps named workspaces isolated from one another and the default', () => {
    assert.notEqual(deriveStorageKey('a'), deriveStorageKey('b'));
    assert.notEqual(deriveStorageKey('any'), WORKSPACE_STORAGE_KEY);
  });
});
