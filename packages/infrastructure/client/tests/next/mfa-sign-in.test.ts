import { afterEach, describe, expect, it, vi } from 'vitest';
import { createHandlers } from '../../src/integrations/next/handlers.js';
import { CredentialsProvider } from '../../src/runtime/auth/providers/credentials.js';
import { decodeJWT } from '../../src/runtime/auth/jwt.js';
import { encodeSession } from '../../src/runtime/auth/session.js';
import type { JWTPayload } from '../../src/runtime/auth/session.js';
import type { ResolvedAuthConfig } from '../../src/runtime/auth/types.js';

vi.mock('../../src/runtime/auth/csrf.js', () => ({
  createCSRFToken: vi.fn(),
  validateCSRFToken: vi.fn(async (cookie, token) => cookie === 'csrf-cookie' && token === 'csrf-token'),
}));

const mfaToken = 'x'.repeat(43);
const config: ResolvedAuthConfig = {
  providers: [CredentialsProvider()],
  apiUrl: 'http://api.example',
  callbacks: {
    jwt: async ({ token }) => token,
    session: async ({ session }) => session,
    signIn: async () => true,
    authorized: async ({ auth }) => !!auth,
    redirect: async ({ url, baseUrl }) => new URL(url, baseUrl).href,
  },
  secret: 'a-synthetic-local-test-secret-with-32-characters',
  pages: {},
  cookies: { name: '__me', secure: false, sameSite: 'lax', path: '/', maxAge: 2592000, httpOnly: true },
  maxAge: 2592000,
  updateAge: 0,
  basePath: '/api/auth',
  debug: true,
  trustHost: false,
  tenantHeader: 'X-Tenant-Id',
};
const originalFetch = globalThis.fetch;
afterEach(() => {
  globalThis.fetch = originalFetch;
  vi.restoreAllMocks();
});
function request(path: string, body: Record<string, unknown>, csrf = true) {
  return new Request(`http://localhost/api/auth/${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...(csrf ? { Cookie: '__me.csrf-token=csrf-cookie' } : {}) },
    body: JSON.stringify({ ...body, ...(csrf ? { csrfToken: 'csrf-token' } : {}) }),
  });
}
async function signedInRequest(path: string, body: Record<string, unknown>, accessToken: string) {
  const token: JWTPayload = {
    user: { id: 'synthetic-user', email: 'user@example.test', name: 'Synthetic user', image: null },
    accessToken,
    refreshToken: 'synthetic-refresh',
    accessTokenExpires: Date.now() + 300_000,
  };
  const encrypted = await encodeSession(token, config);
  return new Request(`http://localhost/api/auth/${path}`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Cookie: `__me.csrf-token=csrf-cookie; __me.session-token=${encodeURIComponent(encrypted)}`,
    },
    body: JSON.stringify({ ...body, csrfToken: 'csrf-token' }),
  });
}
const setup = {
  success: true,
  secretKey: 'SYNTHETIC-SETUP',
  qrCodeUri: 'otpauth://totp/Example?secret=SYNTHETIC',
  expiresAt: new Date(Date.now() + 300000).toISOString(),
};

describe('native limited MFA through Next handlers', () => {
  it('rejects enrollment without valid CSRF before calling the API', async () => {
    globalThis.fetch = vi.fn();
    const response = await createHandlers(config).POST(request('mfa/enrollment', { mfaToken }, false));
    expect(response.status).toBe(403);
    expect(globalThis.fetch).not.toHaveBeenCalled();
    expect(response.headers.get('set-cookie')).toBeNull();
  });

  it('starts enrollment on the real totp:setup endpoint through the generated client and sets no session', async () => {
    const backend = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ isSuccess: true, ...setup, accessToken: 'must-not-forward', userId: 'must-not-forward' }), {
        status: 200,
        headers: { 'content-type': 'application/json' },
      }),
    );
    globalThis.fetch = backend;
    const response = await createHandlers(config).POST(
      await signedInRequest('mfa/enrollment', { mfaToken, tenantId: 'untrusted', password: 'must-not-forward' }, 'session-access'),
    );
    expect(response.status).toBe(200);
    expect(await response.json()).toEqual(setup);
    expect(backend.mock.calls[0][0]).toBe('http://api.example/v1/auth/mfa/totp:setup');
    expect(backend.mock.calls[0][1].headers.get('authorization')).toBe('Bearer session-access');
    expect(backend.mock.calls[0][1].body).toBeUndefined();
    expect(response.headers.get('set-cookie')).toBeNull();
    expect(response.headers.get('cache-control')).toContain('no-store');
  });

  it('rejects enrollment without an authenticated session instead of posting to an unserved route', async () => {
    globalThis.fetch = vi.fn();
    const response = await createHandlers(config).POST(request('mfa/enrollment', { mfaToken }));
    expect(response.status).toBe(401);
    expect(globalThis.fetch).not.toHaveBeenCalled();
    expect(response.headers.get('set-cookie')).toBeNull();
  });

  it('maps a rejected setup start to the enrollment error without leaking a session', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ title: 'Unauthorized' }), {
        status: 401,
        headers: { 'content-type': 'application/json' },
      }),
    );
    const response = await createHandlers(config).POST(await signedInRequest('mfa/enrollment', {}, 'stale-access'));
    expect(response.status).toBe(401);
    expect(response.headers.get('set-cookie')).toBeNull();
  });

  it('returns the first-factor challenge without a session cookie or logging the bearer', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ requiresMfa: true, mfaToken, availableMethods: ['TOTP'] }), {
        status: 200,
        headers: { 'content-type': 'application/json' },
      }),
    );
    const log = vi.spyOn(console, 'error').mockImplementation(() => {});
    const response = await createHandlers(config).POST(request('signin/credentials', { email: 'a@example.com', password: 'pw', redirect: false }));
    expect(response.status).toBe(403);
    expect(await response.json()).toMatchObject({ error: 'MfaRequired', mfaToken, availableMethods: ['TOTP'] });
    expect(response.headers.get('set-cookie')).toBeNull();
    expect(JSON.stringify(log.mock.calls)).not.toContain(mfaToken);
  });

  it('persists the verified session but never its one-time recovery codes or limited bearer', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue(
      new Response(
        JSON.stringify({
          accessToken: 'verified-access',
          refreshToken: 'verified-refresh',
          expiresIn: 3600,
          userId: 'verified-user',
          email: 'server@example.com',
          mfaEnrollmentBackupCodes: ['one-time-code'],
        }),
        { status: 200, headers: { 'content-type': 'application/json' } },
      ),
    );
    const response = await createHandlers(config).POST(request('signin/credentials', { mfaToken, method: 'Totp', code: '123456', redirect: false }));
    expect(response.status).toBe(200);
    const body = await response.json();
    expect(body).not.toHaveProperty('mfaEnrollmentBackupCodes');
    expect(body).not.toHaveProperty('mfaToken');
    const cookie = response.headers.get('set-cookie')!;
    const sessionToken = decodeURIComponent(cookie.match(/__me\.session-token=([^;]+)/)![1]);
    const jwt = await decodeJWT({ token: sessionToken, secret: config.secret });
    expect(jwt?.accessToken).toBe('verified-access');
    expect(JSON.stringify(jwt)).not.toContain('one-time-code');
    expect(JSON.stringify(jwt)).not.toContain(mfaToken);
  });

  it('sets no session on denied verification', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ message: 'Invalid authentication code' }), {
        status: 401,
        headers: { 'content-type': 'application/json' },
      }),
    );
    const response = await createHandlers(config).POST(request('signin/credentials', { mfaToken, method: 'Totp', code: '123456', redirect: false }));
    expect(response.status).toBe(401);
    expect(response.headers.get('set-cookie')).toBeNull();
  });
});
