/** @vitest-environment happy-dom */
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { createMfaEnrollmentCompleteAction } from '../../src/integrations/next/actions.js';
import { CredentialsProvider } from '../../src/runtime/auth/providers/credentials.js';
import { createJWTPayload } from '../../src/runtime/auth/session.js';
import { decodeJWT } from '../../src/runtime/auth/jwt.js';
import { SessionStore, resolveCookieOptions } from '../../src/runtime/auth/cookies.js';
import type { ResolvedAuthConfig } from '../../src/runtime/auth/types.js';

const cookieAdapter = vi.hoisted(() => {
  const values = new Map<string, string>();
  return {
    values,
    set: vi.fn((name: string, value: string) => {
      values.set(name, value);
    }),
    redirect: vi.fn((url: string): never => {
      throw new Error(`NEXT_REDIRECT:${url}`);
    }),
  };
});

vi.mock('next/headers', () => ({
  cookies: () =>
    Promise.resolve({
      get: (name: string) => (cookieAdapter.values.has(name) ? { value: cookieAdapter.values.get(name) } : undefined),
      set: cookieAdapter.set,
      delete: (name: string) => {
        cookieAdapter.values.delete(name);
      },
    }),
}));
vi.mock('next/navigation', () => ({ redirect: cookieAdapter.redirect }));

vi.mock('../../src/runtime/auth/csrf.js', () => ({
  createCSRFToken: vi.fn(),
  validateCSRFToken: vi.fn(() => Promise.resolve(true)),
}));

const mfaToken = 'x'.repeat(43);
const config: ResolvedAuthConfig = {
  providers: [CredentialsProvider()],
  apiUrl: 'http://api.example',
  callbacks: {
    jwt: ({ token }) => Promise.resolve(token),
    session: ({ session }) => Promise.resolve(session),
    signIn: () => Promise.resolve(true),
    authorized: ({ auth }) => Promise.resolve(!!auth),
    redirect: ({ url, baseUrl }) => Promise.resolve(new URL(url, baseUrl).href),
  },
  secret: 'a-synthetic-local-test-secret-with-32-characters',
  pages: {},
  cookies: { name: '__me', secure: false, sameSite: 'lax', path: '/', maxAge: 2592000, httpOnly: true },
  maxAge: 2592000,
  updateAge: 0,
  basePath: '/api/auth',
  debug: false,
  trustHost: false,
  tenantHeader: 'X-Tenant-Id',
};
const originalFetch = globalThis.fetch;
beforeEach(() => {
  cookieAdapter.values.clear();
  cookieAdapter.set.mockClear();
  cookieAdapter.redirect.mockClear();
});
afterEach(() => {
  globalThis.fetch = originalFetch;
  vi.restoreAllMocks();
});

describe('createMfaEnrollmentCompleteAction', () => {
  it('finalizes the session once and returns the fresh recovery codes exactly once', async () => {
    const jwtCallback = vi.spyOn(config.callbacks, 'jwt');
    const backend = vi.fn<typeof fetch>().mockResolvedValue(
      Response.json({
        accessToken: 'verified-access',
        refreshToken: 'verified-refresh',
        expiresIn: 3600,
        userId: '33333333-3333-4333-8333-333333333333',
        email: 'enrolled@example.com',
        mfaEnrollmentBackupCodes: ['first-once-code', 'second-once-code'],
      }),
    );
    globalThis.fetch = backend;
    const result = await createMfaEnrollmentCompleteAction(config)(mfaToken, '123456');
    expect(result).toEqual({ success: true, backupCodes: ['first-once-code', 'second-once-code'] });
    expect(JSON.parse(backend.mock.calls[0][1]?.body as string)).toEqual({
      mfaToken,
      method: 'Totp',
      code: '123456',
    });
    expect(jwtCallback).toHaveBeenCalledOnce();
    const encrypted = new SessionStore(resolveCookieOptions(config.cookies, config.cookies.secure)).read((name) => cookieAdapter.values.get(name));
    expect(encrypted).toBeDefined();
    const token = await decodeJWT({ token: encrypted!, secret: config.secret });
    expect(token?.user.id).toBe('33333333-3333-4333-8333-333333333333');
    expect(token?.accessToken).toBe('verified-access');
    expect(JSON.stringify(token)).not.toContain('first-once-code');
    expect(JSON.stringify(token)).not.toContain('second-once-code');
  });

  it('does not issue a session or expose recovery codes when the sign-in callback denies', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue(
      Response.json({
        accessToken: 'verified-access',
        refreshToken: 'verified-refresh',
        userId: '33333333-3333-4333-8333-333333333333',
        mfaEnrollmentBackupCodes: ['first-once-code'],
      }),
    );
    const deniedConfig = { ...config, callbacks: { ...config.callbacks, signIn: () => Promise.resolve(false) } };

    const result = await createMfaEnrollmentCompleteAction(deniedConfig)(mfaToken, '123456');

    expect(result).toEqual({ success: false, message: 'Sign-in denied' });
    expect(cookieAdapter.set).not.toHaveBeenCalled();
  });

  it('honors a callback redirect before issuing a session or exposing recovery codes', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue(
      Response.json({
        accessToken: 'verified-access',
        refreshToken: 'verified-refresh',
        userId: '33333333-3333-4333-8333-333333333333',
        mfaEnrollmentBackupCodes: ['first-once-code'],
      }),
    );
    const redirectConfig = { ...config, callbacks: { ...config.callbacks, signIn: () => Promise.resolve('/approval-required') } };

    await expect(createMfaEnrollmentCompleteAction(redirectConfig)(mfaToken, '123456')).rejects.toThrow('NEXT_REDIRECT:/approval-required');

    expect(cookieAdapter.redirect).toHaveBeenCalledWith('/approval-required');
    expect(cookieAdapter.set).not.toHaveBeenCalled();
  });

  it('never persists the codes into the JWT session claims', () => {
    const token = createJWTPayload(
      {
        tokens: { accessToken: 'verified-access', refreshToken: 'verified-refresh', tokenType: 'Bearer' },
        user: { id: '33333333-3333-4333-8333-333333333333', email: 'enrolled@example.com', name: null, image: null, roles: [], permissions: [] },
        mfaEnrollmentBackupCodes: ['first-once-code'],
      },
      config,
    );
    expect(token.accessToken).toBe('verified-access');
    expect(JSON.stringify(token)).not.toContain('first-once-code');
  });

  it('reports a denial from the backend without finalizing a session', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue(Response.json({ message: 'Additional verification failed' }, { status: 401 }));
    const result = await createMfaEnrollmentCompleteAction(config)(mfaToken, 'wrong-code');
    expect(result).toEqual({ success: false, message: 'Additional verification failed' });
  });

  it('does not expose codes when the completion carries none', async () => {
    globalThis.fetch = vi
      .fn()
      .mockResolvedValue(Response.json({ accessToken: 'verified-access', refreshToken: 'verified-refresh', userId: '33333333-3333-4333-8333-333333333333' }));
    const result = await createMfaEnrollmentCompleteAction(config)(mfaToken, '123456');
    expect(result).toEqual({ success: true, backupCodes: [] });
  });
});
