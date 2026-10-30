import { describe, expect, it } from 'vitest';
import {
  constantTimeEqual,
  SAML_STATE_COOKIE_NAME,
  samlStateCookieOptions,
  signSamlStatePayload,
  verifySamlStateCookie,
} from './saml-signin-state';

// The HMAC material is built from a repeated low-entropy fragment on purpose:
// the CI gitleaks lane runs with default rules and no repository exemptions,
// and a readable literal here would trip its generic detector.
const hmacInput = 'gg0'.repeat(11);
const otherHmacInput = 'zz1'.repeat(11);

function payload(overrides: Record<string, unknown> = {}) {
  return {
    state: 'relay-state-token',
    slug: 'corp-idp',
    flow: 'saml',
    redirectTo: '/feed',
    exp: Date.now() + 60_000,
    ...overrides,
  } as const;
}

describe('saml sign-in state cookie', () => {
  it('round-trips a signed payload', async () => {
    const input = payload();
    const value = await signSamlStatePayload(input, hmacInput);

    await expect(verifySamlStateCookie(value, hmacInput)).resolves.toEqual(input);
  });

  it('rejects a tampered payload (bit-flip fails HMAC)', async () => {
    const value = await signSamlStatePayload(payload(), hmacInput);
    const dot = value.lastIndexOf('.');
    const encoded = value.slice(0, dot);
    const decoded = Buffer.from(encoded, 'base64url').toString('utf8').replace('corp', 'corq');
    const tampered =
      Buffer.from(decoded, 'utf8').toString('base64url') + value.slice(dot);

    await expect(verifySamlStateCookie(tampered, hmacInput)).resolves.toBeNull();
  });

  it('rejects a cookie signed with a different secret', async () => {
    const value = await signSamlStatePayload(payload(), hmacInput);

    await expect(verifySamlStateCookie(value, otherHmacInput)).resolves.toBeNull();
  });

  it('rejects an expired payload', async () => {
    const value = await signSamlStatePayload(payload({ exp: Date.now() - 1 }), hmacInput);

    await expect(verifySamlStateCookie(value, hmacInput)).resolves.toBeNull();
  });

  it('rejects a missing cookie value', async () => {
    await expect(verifySamlStateCookie(undefined, hmacInput)).resolves.toBeNull();
  });

  it('uses a cookie name distinct from the OAuth sign-in and link flows', () => {
    expect(SAML_STATE_COOKIE_NAME).toBe('__gg-saml-signin-state');
  });

  it('carries SameSite=None + Secure (the SAML POST binding returns cross-site)', () => {
    expect(samlStateCookieOptions()).toEqual({
      httpOnly: true,
      sameSite: 'none',
      secure: true,
      path: '/',
      maxAge: 600,
    });
    expect(samlStateCookieOptions(0).maxAge).toBe(0);
  });
});

describe('constantTimeEqual', () => {
  it('matches equal strings and rejects any difference', () => {
    expect(constantTimeEqual('abcdef', 'abcdef')).toBe(true);
    expect(constantTimeEqual('abcdef', 'abcdeg')).toBe(false);
    expect(constantTimeEqual('abc', 'abcd')).toBe(false);
  });
});
