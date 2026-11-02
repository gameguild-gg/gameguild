import { afterEach, describe, expect, it, vi } from 'vitest';

import {
  isUnrecognizedActionError,
  recoverFromStaleServerAction,
} from './server-action-errors';

function staleActionError(): Error {
  return new Error(
    'Failed to find Server Action "1234567890abcdef". This action is no longer available or was renamed.',
    { name: 'UnrecognizedActionError' } as ErrorOptions,
  );
}

const GUARD_KEY = 'gg-stale-server-action-reload-at';

describe('isUnrecognizedActionError', () => {
  it.each([
    ['the Next.js client error name', staleActionError()],
    ['the 409 body text', new Error('Server Action unavailable.')],
    ['a lowercase "failed to find server action" message', new Error('failed to find server action "x"')],
  ])('classifies %s', (_label, error) => {
    expect(isUnrecognizedActionError(error)).toBe(true);
  });

  it.each([
    ['a plain network error', new Error('Network request failed')],
    ['a fetch TypeError', new TypeError('Failed to fetch')],
    ['a string', 'Server Action unavailable'],
    ['null', null],
    ['undefined', undefined],
  ])('does not classify %s', (_label, error) => {
    expect(isUnrecognizedActionError(error)).toBe(false);
  });
});

describe('recoverFromStaleServerAction', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    window.sessionStorage.clear();
    vi.restoreAllMocks();
  });

  it('is a no-op for non-stale errors', () => {
    const reload = vi.fn();
    vi.stubGlobal('location', { ...window.location, reload });

    expect(recoverFromStaleServerAction(new Error('weak password'))).toBe(false);
    expect(reload).not.toHaveBeenCalled();
  });

  it('reloads exactly once for a stale action error', () => {
    const reload = vi.fn();
    vi.stubGlobal('location', { ...window.location, reload });

    expect(recoverFromStaleServerAction(staleActionError())).toBe(true);
    expect(reload).toHaveBeenCalledTimes(1);

    // Immediate second call (guard active): no reload, returns false.
    expect(recoverFromStaleServerAction(staleActionError())).toBe(false);
    expect(reload).toHaveBeenCalledTimes(1);
  });

  it('re-arms after the guard window elapses', () => {
    vi.useFakeTimers();
    try {
      const reload = vi.fn();
      vi.stubGlobal('location', { ...window.location, reload });

      expect(recoverFromStaleServerAction(staleActionError())).toBe(true);
      vi.advanceTimersByTime(31_000);
      expect(recoverFromStaleServerAction(staleActionError())).toBe(true);
      expect(reload).toHaveBeenCalledTimes(2);
    } finally {
      vi.useRealTimers();
    }
  });

  it('tolerates a throwing sessionStorage', () => {
    const reload = vi.fn();
    vi.stubGlobal('location', { ...window.location, reload });
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new DOMException('Access denied', 'SecurityError');
    });

    expect(recoverFromStaleServerAction(staleActionError())).toBe(true);
    expect(reload).toHaveBeenCalledTimes(1);
  });

  it('treats a corrupted guard value as absent', () => {
    window.sessionStorage.setItem(GUARD_KEY, 'not-a-timestamp');
    const reload = vi.fn();
    vi.stubGlobal('location', { ...window.location, reload });

    expect(recoverFromStaleServerAction(staleActionError())).toBe(true);
    expect(reload).toHaveBeenCalledTimes(1);
  });
});
