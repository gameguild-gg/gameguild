import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import {
  deriveStorageKey,
  mergeRestoredWorkspaceFiles,
  resolveWorkspaceStorageKey,
  shouldPersistWorkspace,
  workspaceFilesForStorage,
  WORKSPACE_STORAGE_KEY,
} from '../dist/components/ide-types.js';

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

describe('resolveWorkspaceStorageKey', () => {
  it('uses an explicit storage key verbatim when a host needs to preserve an existing workspace', () => {
    assert.equal(resolveWorkspaceStorageKey('ignored', 'host:workspace:42'), 'host:workspace:42');
  });

  it('falls back to the neutral derived key when no explicit key is provided', () => {
    assert.equal(resolveWorkspaceStorageKey('demo'), 'emception:ws:demo');
    assert.equal(resolveWorkspaceStorageKey('demo', ''), 'emception:ws:demo');
  });
});

describe('shouldPersistWorkspace', () => {
  it('waits for restoration before writing so an initial workspace cannot replace a draft', () => {
    assert.equal(shouldPersistWorkspace(true, false), false);
    assert.equal(shouldPersistWorkspace(true, true), true);
    assert.equal(shouldPersistWorkspace(false, true), false);
  });
});

describe('workspace persistence files', () => {
  const starter = {
    '/user/main.cpp': { path: '/user/main.cpp', type: 'text', content: '// starter' },
    '/user/logo.png': { path: '/user/logo.png', type: 'image', content: 'data:image/png;base64,large' },
  };

  it('does not persist image data into localStorage', () => {
    assert.deepEqual(workspaceFilesForStorage(starter), {
      '/user/main.cpp': starter['/user/main.cpp'],
    });
  });

  it('restores saved text while retaining missing static images from the workspace descriptor', () => {
    assert.deepEqual(
      mergeRestoredWorkspaceFiles(starter, {
        '/user/main.cpp': { path: '/user/main.cpp', type: 'text', content: '// draft' },
      }),
      {
        '/user/main.cpp': { path: '/user/main.cpp', type: 'text', content: '// draft' },
        '/user/logo.png': starter['/user/logo.png'],
      },
    );
  });
});
