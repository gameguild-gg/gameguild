import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import {
  clearGuestSession,
  guestAccessEnabled,
  loadGuestSession,
  saveGuestSession,
  type GuestSessionRecord,
} from './guest-session';

function makeRecord(overrides: Partial<GuestSessionRecord> = {}): GuestSessionRecord {
  return {
    guestSessionId: '11111111-2222-3333-4444-555555555555',
    accessToken: 'guest-token',
    expiresAt: new Date(Date.now() + 30 * 60 * 1000).toISOString(),
    permissions: ['guest:browse-catalog', 'guest:read-published-content'],
    ...overrides,
  };
}

describe('guest-session store', () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  afterEach(() => {
    window.localStorage.clear();
  });

  it('round-trips a saved guest session', () => {
    const record = makeRecord();
    saveGuestSession(record);

    expect(loadGuestSession()).toEqual(record);
  });

  it('returns null when nothing is stored', () => {
    expect(loadGuestSession()).toBeNull();
  });

  it('evicts a corrupt payload instead of throwing', () => {
    window.localStorage.setItem('gameguild.guest-session', '{not-json');
    expect(loadGuestSession()).toBeNull();
    expect(window.localStorage.getItem('gameguild.guest-session')).toBeNull();
  });

  it('evicts a payload with an invalid shape', () => {
    window.localStorage.setItem(
      'gameguild.guest-session',
      JSON.stringify({ guestSessionId: 'x', accessToken: 42 }),
    );
    expect(loadGuestSession()).toBeNull();
    expect(window.localStorage.getItem('gameguild.guest-session')).toBeNull();
  });

  it('evicts an expired guest session (expiry always ends the session)', () => {
    saveGuestSession(makeRecord({ expiresAt: new Date(Date.now() - 1000).toISOString() }));
    expect(loadGuestSession()).toBeNull();
    expect(window.localStorage.getItem('gameguild.guest-session')).toBeNull();
  });

  it('keeps a session that expires in the future', () => {
    saveGuestSession(makeRecord({ expiresAt: new Date(Date.now() + 1000).toISOString() }));
    expect(loadGuestSession()).not.toBeNull();
  });

  it('evicts a session with an unparseable expiry', () => {
    saveGuestSession(makeRecord({ expiresAt: 'not-a-date' }));
    expect(loadGuestSession()).toBeNull();
  });

  it('clearGuestSession removes the stored record', () => {
    saveGuestSession(makeRecord());
    clearGuestSession();
    expect(loadGuestSession()).toBeNull();
  });

  it('clearGuestSession is a no-op when nothing is stored', () => {
    expect(() => clearGuestSession()).not.toThrow();
  });
});

describe('guestAccessEnabled capability gate', () => {
  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it('defaults to enabled (matches the backend default posture)', () => {
    expect(guestAccessEnabled()).toBe(true);
  });

  it('is disabled only by the explicit opt-out', () => {
    vi.stubEnv('GUEST_ACCESS_ENABLED', 'false');
    expect(guestAccessEnabled()).toBe(false);
  });

  it('stays enabled for arbitrary truthy values', () => {
    vi.stubEnv('GUEST_ACCESS_ENABLED', 'true');
    expect(guestAccessEnabled()).toBe(true);
  });
});
