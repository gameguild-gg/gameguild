import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import {
  CONSENT_STATE_VERSION,
  CONSENT_STORAGE_KEY,
  type ConsentState,
  parseConsentState,
  serializeConsentState,
  toConsentMode,
} from './state';
import { isDoNotTrackEnabled, readConsent, saveConsent, subscribeConsent } from './store';

function grantedState(overrides: Partial<ConsentState> = {}): ConsentState {
  return { version: CONSENT_STATE_VERSION, analytics: true, marketing: false, updatedAt: '2026-01-01T00:00:00.000Z', ...overrides };
}

describe('consent state', () => {
  it('parses a valid payload', () => {
    const state = parseConsentState(serializeConsentState(grantedState()));
    expect(state).toEqual(grantedState());
  });

  it('returns null for missing, malformed, or wrong-version payloads', () => {
    expect(parseConsentState(null)).toBeNull();
    expect(parseConsentState('')).toBeNull();
    expect(parseConsentState('not json')).toBeNull();
    expect(parseConsentState('[]')).toBeNull();
    expect(parseConsentState('{"version":2,"analytics":true,"marketing":true}')).toBeNull();
    expect(parseConsentState(`{"version":${CONSENT_STATE_VERSION + 1},"analytics":true,"marketing":true,"updatedAt":"x"}`)).toBeNull();
    expect(parseConsentState(`{"version":${CONSENT_STATE_VERSION},"analytics":"yes","marketing":true}`)).toBeNull();
  });

  it('version bump invalidates prior consent (renewed consent)', () => {
    // A stored record from a previous schema version reads as "no choice".
    const legacy = JSON.stringify({ version: CONSENT_STATE_VERSION - 1, analytics: true, marketing: true, updatedAt: 'x' });
    expect(parseConsentState(legacy)).toBeNull();
  });

  it('maps granular state to consent mode v2 without leaking marketing from analytics', () => {
    expect(toConsentMode(grantedState())).toEqual({
      analytics_storage: 'granted',
      ad_storage: 'denied',
      ad_user_data: 'denied',
      ad_personalization: 'denied',
    });
    expect(toConsentMode(grantedState({ analytics: false, marketing: false }))).toEqual({
      analytics_storage: 'denied',
      ad_storage: 'denied',
      ad_user_data: 'denied',
      ad_personalization: 'denied',
    });
  });
});

describe('consent store', () => {
  beforeEach(() => {
    window.localStorage.clear();
    document.cookie = '_ga=GA1.1.1.1; path=/';
    window.localStorage.setItem('_ga', 'GA1.1.1.1');
    Object.defineProperty(navigator, 'doNotTrack', { value: undefined, configurable: true });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('defaults to denied (no persisted choice)', () => {
    expect(readConsent()).toBeNull();
  });

  it('reads back a persisted grant', () => {
    saveConsent({ analytics: true, marketing: false });
    const state = readConsent();
    expect(state?.analytics).toBe(true);
    expect(state?.marketing).toBe(false);
    expect(state?.version).toBe(CONSENT_STATE_VERSION);
  });

  it('Do Not Track keeps consent denied and read returns null', () => {
    Object.defineProperty(navigator, 'doNotTrack', { value: '1', configurable: true });
    expect(isDoNotTrackEnabled()).toBe(true);
    saveConsent({ analytics: true, marketing: true });
    expect(readConsent()).toBeNull();
  });

  it('saveConsent dispatches gg:consent-changed', () => {
    const handler = vi.fn();
    window.addEventListener('gg:consent-changed', handler);
    saveConsent({ analytics: true, marketing: false });
    expect(handler).toHaveBeenCalledTimes(1);
    window.removeEventListener('gg:consent-changed', handler);
  });

  it('revocation clears tracking identifiers (GA cookies/storage, not auth cookies)', () => {
    document.cookie = 'session=keep-me; path=/';
    saveConsent({ analytics: true, marketing: true });
    expect(document.cookie).toContain('_ga=');
    expect(document.cookie).toContain('session=');

    saveConsent({ analytics: false, marketing: false });

    expect(document.cookie).not.toContain('_ga=');
    expect(document.cookie).toContain('session=');
    expect(window.localStorage.getItem('_ga')).toBeNull();
  });

  it('syncs across tabs via the storage event', () => {
    const listener = vi.fn();
    const unsubscribe = subscribeConsent(listener);
    expect(listener).toHaveBeenCalledWith(null);

    const remote = serializeConsentState(grantedState({ analytics: true, marketing: true }));
    window.dispatchEvent(new StorageEvent('storage', { key: CONSENT_STORAGE_KEY, newValue: remote }));
    expect(listener).toHaveBeenCalledWith(expect.objectContaining({ analytics: true, marketing: true }));

    unsubscribe();
    window.dispatchEvent(new StorageEvent('storage', { key: CONSENT_STORAGE_KEY, newValue: remote }));
    expect(listener).toHaveBeenCalledTimes(2);
  });

  it('ignores storage events for other keys', () => {
    const listener = vi.fn();
    const unsubscribe = subscribeConsent(listener);
    window.dispatchEvent(new StorageEvent('storage', { key: 'other', newValue: 'x' }));
    expect(listener).toHaveBeenCalledTimes(1); // only the immediate call
    unsubscribe();
  });
});
