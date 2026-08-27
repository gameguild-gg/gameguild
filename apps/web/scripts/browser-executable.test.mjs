import assert from 'node:assert/strict';
import test from 'node:test';

import { resolveChromiumExecutablePath } from './browser-executable.mjs';

test('uses an explicitly configured Chromium executable path', () => {
  assert.equal(
    resolveChromiumExecutablePath({
      env: { CODING_CYCLE_CHROMIUM_EXECUTABLE: 'D:/custom/chrome.exe' },
      platform: 'win32',
      exists: () => false,
    }),
    'D:/custom/chrome.exe',
  );
});

test('uses an installed Chrome fallback only on Windows', () => {
  const chrome = 'C:/Program Files/Google/Chrome/Application/chrome.exe';
  assert.equal(
    resolveChromiumExecutablePath({
      env: {},
      platform: 'win32',
      exists: (candidate) => candidate === chrome,
    }),
    chrome,
  );
  assert.equal(
    resolveChromiumExecutablePath({ env: {}, platform: 'linux', exists: () => true }),
    undefined,
  );
});
