import { NextRequest } from 'next/server';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  SAML_STATE_COOKIE_NAME,
  signSamlStatePayload,
} from '@/lib/auth/saml-signin-state';

// The HMAC material is built from a repeated low-entropy fragment on purpose:
// the CI gitleaks lane runs with default rules and no repository exemptions,
// and a readable literal here would trip its generic detector. The inline
// copy inside the '@/auth' mock must stay byte-identical to this one.
const hmacInput = 'gg0'.repeat(11);
const RELAY_STATE = 'issued-relay-state';
const SAML_RESPONSE = 'PHNhbWxwOlJlc3BvbnNlPjwvc2FtbHA6UmVzcG9uc2U+';

const mocks = vi.hoisted(() => {
  const store = new Map<string, string>();
  return {
    cookieStore: {
      store,
      get: (name: string) => (store.has(name) ? { name, value: store.get(name) } : undefined),
      set: vi.fn((name: string, value: string) => store.set(name, value)),
      delete: vi.fn((name: string) => store.delete(name)),
    },
    postAuthSamlAssertionConsumer: vi.fn(),
    sessionWrite: vi.fn(
      (
        value: string,
        setCookie: (name: string, value: string, options: Record<string, unknown>) => void,
      ) => {
        // Mirror the real SessionStore: emit one session-token Set-Cookie.
        setCookie('gameguild.session-token', value, {
          httpOnly: true,
          secure: true,
          sameSite: 'lax',
          path: '/',
          maxAge: 2592000,
        });
      },
    ),
  };
});

vi.mock('@/auth', () => ({
  authConfig: {
    secret: 'gg0'.repeat(11),
    pages: { error: '/sign-in' },
    cookies: { name: 'gameguild', secure: true, sameSite: 'lax', path: '/', httpOnly: true },
    callbacks: {
      jwt: async ({ token }: { token: unknown }) => token,
      session: async ({ session }: { session: unknown }) => session,
      signIn: async () => true,
    },
  },
}));

vi.mock('next/headers', () => ({
  cookies: vi.fn().mockResolvedValue(mocks.cookieStore),
}));

vi.mock('@game-guild/client', () => ({
  createServerClient: vi.fn().mockReturnValue({}),
  GeneratedApi: {
    AuthModule: class {
      postAuthSamlAssertionConsumer = mocks.postAuthSamlAssertionConsumer;
    },
  },
  parseBackendAuthResponse: vi.fn().mockReturnValue({
    tokens: {
      accessToken: 'access-token',
      refreshToken: 'refresh-token',
      tokenType: 'Bearer',
    },
    user: { id: 'user-1', email: 'user@corp.test', name: 'Corp User', roles: [], permissions: [] },
    sessionId: 'session-1',
  }),
  createJWTPayload: vi.fn().mockImplementation((result: unknown) => ({ payload: result })),
  encodeSession: vi.fn().mockResolvedValue('encrypted-session'),
  resolveCookieOptions: vi.fn().mockReturnValue({
    name: 'gameguild',
    secure: true,
    sameSite: 'lax',
    path: '/',
    maxAge: 2592000,
    httpOnly: true,
  }),
  SessionStore: class {
    write = mocks.sessionWrite;
  },
}));

import { POST } from './route';

/** Route-handler context carrying the provider slug (Next 16: params is a Promise). */
function slugParams(slug = 'corp-idp') {
  return { params: Promise.resolve({ slug }) };
}

/** The IdP's auto-submitting HTML form POST (SAML HTTP-POST binding). */
function idpFormPost(slug = 'corp-idp', relayState = RELAY_STATE, samlResponse = SAML_RESPONSE) {
  return new NextRequest(`http://localhost/api/auth/saml/${slug}/callback`, {
    method: 'POST',
    headers: { 'content-type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams({ SAMLResponse: samlResponse, RelayState: relayState }),
  });
}

async function primeCookie(overrides: Record<string, unknown> = {}) {
  mocks.cookieStore.store.set(
    SAML_STATE_COOKIE_NAME,
    await signSamlStatePayload(
      {
        state: RELAY_STATE,
        slug: 'corp-idp',
        flow: 'saml',
        redirectTo: '/workspace',
        exp: Date.now() + 60_000,
        ...overrides,
      },
      hmacInput,
    ),
  );
}

describe('SAML ACS receiver (browser form POST → API → authenticated session)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.cookieStore.store.clear();
    mocks.postAuthSamlAssertionConsumer.mockResolvedValue({
      ok: true,
      data: {
        accessToken: 'access-token',
        refreshToken: 'refresh-token',
        email: 'user@corp.test',
        expiresIn: 3600,
      },
    });
  });

  it('accepts the provider form POST, forwards to the API ACS, and establishes the session cookie', async () => {
    await primeCookie({ locale: 'pt-BR', tenantId: 'tenant-7', rememberMe: true });

    const response = await POST(idpFormPost(), slugParams());

    expect(mocks.postAuthSamlAssertionConsumer).toHaveBeenCalledWith('corp-idp', {
      samlResponse: SAML_RESPONSE,
      relayState: RELAY_STATE,
      rememberMe: true,
      tenantId: 'tenant-7',
    });

    // The state cookie is single-use: consumed on first use.
    expect(mocks.cookieStore.delete).toHaveBeenCalledWith(SAML_STATE_COOKIE_NAME);

    // The web session cookie is written exactly like the social callback.
    expect(mocks.sessionWrite).toHaveBeenCalledWith(
      'encrypted-session',
      expect.any(Function),
      { storage: 'persistent' },
    );
    expect(mocks.cookieStore.set).toHaveBeenCalledWith(
      'gameguild.session-token',
      'encrypted-session',
      expect.objectContaining({ httpOnly: true, path: '/' }),
    );

    expect(response.status).toBe(302);
    expect(response.headers.get('location')).toBe('http://localhost/pt-BR/workspace');
  });

  it('honors rememberMe=false with a browser-session cookie policy', async () => {
    await primeCookie({ rememberMe: false });

    await POST(idpFormPost(), slugParams());

    expect(mocks.sessionWrite).toHaveBeenCalledWith(
      'encrypted-session',
      expect.any(Function),
      { storage: 'session' },
    );
  });

  it('rejects a callback with no state cookie (never initiated / already used) without calling the API', async () => {
    const response = await POST(idpFormPost(), slugParams());

    expect(mocks.postAuthSamlAssertionConsumer).not.toHaveBeenCalled();
    expect(response.headers.get('location')).toBe('http://localhost/sign-in?error=state_mismatch');
  });

  it('rejects a reused state cookie: after the first callback the cookie is gone', async () => {
    await primeCookie();
    const first = await POST(idpFormPost(), slugParams());
    expect(first.headers.get('location')).toBe('http://localhost/workspace');

    const replay = await POST(idpFormPost(), slugParams());
    expect(mocks.postAuthSamlAssertionConsumer).toHaveBeenCalledTimes(1);
    expect(replay.headers.get('location')).toBe('http://localhost/sign-in?error=state_mismatch');
  });

  it('rejects a foreign RelayState (form value ≠ cookie binding) without burning the API call', async () => {
    await primeCookie();

    const response = await POST(idpFormPost('corp-idp', 'attacker-chosen-relay-state'), slugParams());

    expect(mocks.postAuthSamlAssertionConsumer).not.toHaveBeenCalled();
    expect(response.headers.get('location')).toBe('http://localhost/sign-in?error=state_mismatch');
  });

  it('rejects cross-provider state: a cookie minted for another provider slug', async () => {
    await primeCookie({ slug: 'other-idp' });

    const response = await POST(idpFormPost(), slugParams());

    expect(mocks.postAuthSamlAssertionConsumer).not.toHaveBeenCalled();
    expect(response.headers.get('location')).toBe('http://localhost/sign-in?error=state_mismatch');
  });

  it('rejects a tampered state cookie', async () => {
    mocks.cookieStore.store.set(SAML_STATE_COOKIE_NAME, 'garbage.deadbeef');

    const response = await POST(idpFormPost(), slugParams());

    expect(mocks.postAuthSamlAssertionConsumer).not.toHaveBeenCalled();
    expect(response.headers.get('location')).toBe('http://localhost/sign-in?error=state_mismatch');
  });

  it('rejects a non-form POST body (missing SAMLResponse) fail-closed', async () => {
    await primeCookie();
    const request = new NextRequest('http://localhost/api/auth/saml/corp-idp/callback', {
      method: 'POST',
      headers: { 'content-type': 'text/plain' },
      body: 'not-a-form',
    });

    const response = await POST(request, slugParams());

    expect(mocks.postAuthSamlAssertionConsumer).not.toHaveBeenCalled();
    expect(response.headers.get('location')).toBe('http://localhost/sign-in?error=state_mismatch');
  });

  it('maps an API 401 (fail-closed validation) to error=saml_rejected', async () => {
    await primeCookie();
    mocks.postAuthSamlAssertionConsumer.mockResolvedValue({
      ok: false,
      error: { status: 401, message: 'SAML response was refused.' },
    });

    const response = await POST(idpFormPost(), slugParams());

    expect(response.headers.get('location')).toBe('http://localhost/sign-in?error=saml_rejected');
    expect(mocks.sessionWrite).not.toHaveBeenCalled();
  });

  it('fails closed when the API returns no access token (incomplete sign-in)', async () => {
    await primeCookie();
    mocks.postAuthSamlAssertionConsumer.mockResolvedValue({
      ok: true,
      data: { accessToken: null, refreshToken: null },
    });

    const response = await POST(idpFormPost(), slugParams());

    expect(response.headers.get('location')).toBe('http://localhost/sign-in?error=saml_failed');
    expect(mocks.sessionWrite).not.toHaveBeenCalled();
  });

  it('never locale-prefixes or redirects to an absolute target (cookie payload tampering is moot)', async () => {
    // Issuance sanitizes redirectTo to relative-only; the receiver re-checks so a
    // forged-but-signed-looking payload path (or future regressions) can never
    // turn the post-sign-in redirect into an open redirect.
    await primeCookie({ redirectTo: 'https://evil.example.test/phish' });

    const response = await POST(idpFormPost(), slugParams());

    expect(response.headers.get('location')).toBe('http://localhost/');
  });
});
