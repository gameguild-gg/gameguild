import { describe, expect, it } from 'vitest';
import {
  constantTimeEqual,
  LINK_STATE_COOKIE_NAME,
  settingsAccountPath,
  signLinkStatePayload,
  verifyLinkStateCookie,
} from './oauth-link-state';

const SECRET = 'test-secret-min-32-chars-long-ok';

function payload(overrides: Record<string, unknown> = {}) {
  return {
    state: 'state-abc',
    flow: 'link',
    exp: Date.now() + 60_000,
    locale: 'pt-BR',
    ...overrides,
  } as const;
}

describe('oauth link-state cookie', () => {
  it('round-trips a signed payload', async () => {
    const input = payload();
    const value = await signLinkStatePayload(input, SECRET);

    await expect(verifyLinkStateCookie(value, SECRET)).resolves.toEqual(input);
  });

  it('rejects a tampered payload (bit-flip fails HMAC)', async () => {
    const value = await signLinkStatePayload(payload(), SECRET);
    const dot = value.lastIndexOf('.');
    const encoded = value.slice(0, dot);
    const decoded = Buffer.from(encoded, 'base64url').toString('utf8').replace('link', 'lini');
    const tampered =
      Buffer.from(decoded, 'utf8').toString('base64url') + value.slice(dot);

    await expect(verifyLinkStateCookie(tampered, SECRET)).resolves.toBeNull();
  });

  it('rejects a cookie signed with a different secret', async () => {
    const value = await signLinkStatePayload(payload(), SECRET);

    await expect(verifyLinkStateCookie(value, 'other-secret-min-32-chars-long-!!')).resolves.toBeNull();
  });

  it('rejects an expired payload', async () => {
    const value = await signLinkStatePayload(payload({ exp: Date.now() - 1 }), SECRET);

    await expect(verifyLinkStateCookie(value, SECRET)).resolves.toBeNull();
  });

  it('uses a cookie name distinct from the sign-in flow cookie', () => {
    expect(LINK_STATE_COOKIE_NAME).toBe('__gg-oauth-link-state-discord');
  });
});

describe('constantTimeEqual', () => {
  it('matches equal strings and rejects any difference', () => {
    expect(constantTimeEqual('abcdef', 'abcdef')).toBe(true);
    expect(constantTimeEqual('abcdef', 'abcdeg')).toBe(false);
    expect(constantTimeEqual('abc', 'abcd')).toBe(false);
  });
});

describe('settingsAccountPath', () => {
  it('prefixes non-default locales and leaves en-US unprefixed', () => {
    expect(settingsAccountPath('pt-BR')).toBe('/pt-BR/workspace/settings/account');
    expect(settingsAccountPath('en-US')).toBe('/workspace/settings/account');
    expect(settingsAccountPath(undefined)).toBe('/workspace/settings/account');
  });
});
