import { NextRequest } from 'next/server';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  SAML_STATE_COOKIE_NAME,
  verifySamlStateCookie,
} from '@/lib/auth/saml-signin-state';

// The HMAC material is built from a repeated low-entropy fragment on purpose:
// the CI gitleaks lane runs with default rules and no repository exemptions,
// and a readable literal here would trip its generic detector. The inline
// copy inside the '@/auth' mock must stay byte-identical to this one.
const hmacInput = 'gg0'.repeat(11);

const mocks = vi.hoisted(() => {
  const store = new Map<string, string>();
  return {
    cookieStore: {
      store,
      get: (name: string) => (store.has(name) ? { name, value: store.get(name) } : undefined),
      set: vi.fn((name: string, value: string) => store.set(name, value)),
      delete: vi.fn((name: string) => store.delete(name)),
    },
    postAuthSamlSignInAuthorize: vi.fn(),
  };
});

vi.mock('@/auth', () => ({
  authConfig: { secret: 'gg0'.repeat(11), pages: { error: '/sign-in' } },
}));

vi.mock('next/headers', () => ({
  cookies: vi.fn().mockResolvedValue(mocks.cookieStore),
}));

vi.mock('@game-guild/client', () => ({
  createServerClient: vi.fn().mockReturnValue({}),
  GeneratedApi: {
    AuthModule: class {
      postAuthSamlSignInAuthorize = mocks.postAuthSamlSignInAuthorize;
    },
  },
}));

import { GET } from './route';

const RELAY_STATE = 'issued-relay-state';

function request(query = '') {
  return new NextRequest(`http://localhost/api/auth/saml/corp-idp/signin${query}`);
}

/** Route-handler context carrying the provider slug (Next 16: params is a Promise). */
function slugParams() {
  return { params: Promise.resolve({ slug: 'corp-idp' }) };
}

describe('SAML sign-in initiation route', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.cookieStore.store.clear();
    mocks.postAuthSamlSignInAuthorize.mockResolvedValue({
      ok: true,
      data: {
        redirectUrl: 'https://idp.corp.test/sso?SAMLRequest=abc&RelayState=' + RELAY_STATE,
        relayState: RELAY_STATE,
      },
    });
  });

  it('issues the AuthnRequest, binds its RelayState into a signed cookie, and redirects to the IdP', async () => {
    const response = await GET(request('?redirectTo=/workspace&locale=pt-BR&rememberMe=true'), slugParams());

    expect(mocks.postAuthSamlSignInAuthorize).toHaveBeenCalledWith('corp-idp', {});
    expect(response.status).toBe(302);
    expect(response.headers.get('location')).toBe(
      `https://idp.corp.test/sso?SAMLRequest=abc&RelayState=${RELAY_STATE}`,
    );

    expect(mocks.cookieStore.set).toHaveBeenCalledTimes(1);
    const [name, value, options] = mocks.cookieStore.set.mock.calls[0]!;
    expect(name).toBe(SAML_STATE_COOKIE_NAME);
    expect(options).toMatchObject({ httpOnly: true, sameSite: 'none', secure: true, path: '/' });
    await expect(verifySamlStateCookie(value, hmacInput)).resolves.toMatchObject({
      state: RELAY_STATE,
      slug: 'corp-idp',
      flow: 'saml',
      redirectTo: '/workspace',
      locale: 'pt-BR',
      rememberMe: true,
    });
  });

  it('sanitizes an absolute redirectTo to the relative-only fallback', async () => {
    await GET(request('?redirectTo=https://evil.example.test/phish'), slugParams());

    const [, value] = mocks.cookieStore.set.mock.calls[0]!;
    await expect(verifySamlStateCookie(value, hmacInput)).resolves.toMatchObject({
      redirectTo: '/feed',
    });
  });

  it('fails closed to the error page when the provider is unknown or disabled', async () => {
    mocks.postAuthSamlSignInAuthorize.mockResolvedValue({
      ok: false,
      error: { status: 500, message: 'SAML federation provider is not configured.' },
    });

    const response = await GET(request(), slugParams());

    expect(response.status).toBe(302);
    expect(response.headers.get('location')).toBe('http://localhost/sign-in?error=saml_unavailable');
    expect(mocks.cookieStore.set).not.toHaveBeenCalled();
  });

  it('fails closed when the API returns no RelayState to bind', async () => {
    mocks.postAuthSamlSignInAuthorize.mockResolvedValue({
      ok: true,
      data: { redirectUrl: 'https://idp.corp.test/sso', relayState: null },
    });

    const response = await GET(request(), slugParams());

    expect(response.headers.get('location')).toBe('http://localhost/sign-in?error=saml_unavailable');
    expect(mocks.cookieStore.set).not.toHaveBeenCalled();
  });

  it('rejects a malformed provider slug without calling the API', async () => {
    const response = await GET(
      new NextRequest('http://localhost/api/auth/saml/bad-slug/signin'),
      { params: Promise.resolve({ slug: 'bad slug' }) },
    );

    expect(mocks.postAuthSamlSignInAuthorize).not.toHaveBeenCalled();
    expect(response.headers.get('location')).toBe('http://localhost/sign-in?error=saml_invalid_provider');
  });

  it('enforces the shared federation-slug contract: uppercase and trailing-hyphen slugs are rejected', async () => {
    for (const slug of ['Corp-Idp', 'corp-idp-', '-corp-idp', 'corp-idp!']) {
      const response = await GET(
        new NextRequest(`http://localhost/api/auth/saml/${slug}/signin`),
        { params: Promise.resolve({ slug }) },
      );

      expect(mocks.postAuthSamlSignInAuthorize).not.toHaveBeenCalled();
      expect(response.headers.get('location')).toBe('http://localhost/sign-in?error=saml_invalid_provider');
    }
  });

  it('drops a non-UUID tenantId instead of poisoning the callback forwarding', async () => {
    await GET(request('?tenantId=not-a-uuid'), slugParams());

    const [, value] = mocks.cookieStore.set.mock.calls[0]!;
    const payload = await verifySamlStateCookie(value, hmacInput);
    expect(payload).not.toBeNull();
    expect(payload!.tenantId).toBeUndefined();
  });
});
