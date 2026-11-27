/**
 * Email-verification verdict in the session user (#1298 J1).
 *
 * The backend reports `user.emailVerified` on sign-in and refresh responses.
 * It rides into the session only when the backend actually said it as a
 * boolean, so every provider that does not report it keeps producing the
 * exact session shape it produced before.
 */

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { resolveEmailVerification } from '../../src/runtime/auth/claims.js';
import { CredentialsProvider } from '../../src/runtime/auth/providers/credentials.js';
import { GoogleProvider } from '../../src/runtime/auth/providers/google.js';
import { refreshAccessToken, toSession } from '../../src/runtime/auth/session.js';
import type { JWTPayload, ResolvedAuthConfig } from '../../src/runtime/auth/types.js';

const config: ResolvedAuthConfig = {
  providers: [],
  callbacks: {
    jwt: async ({ token }) => token,
    session: async ({ session }) => session,
    signIn: async () => true,
    redirect: async ({ url }) => url,
    authorized: async () => true,
  },
  secret: 'test-secret-at-least-32-characters-long',
  apiUrl: 'http://localhost:8080',
  pages: {},
  cookies: {
    name: '__me',
    secure: false,
    sameSite: 'lax',
    path: '/',
    maxAge: 2592000,
    httpOnly: true,
  },
  maxAge: 2592000,
  updateAge: 0,
  basePath: '/api/auth',
  debug: false,
  trustHost: false,
  tenantHeader: 'X-Tenant-Id',
};

function sessionToken(emailVerified?: boolean): JWTPayload {
  return {
    user: { id: 'user-1', email: 'user@example.com', ...(emailVerified === undefined ? {} : { emailVerified }) },
    accessToken: 'access-token',
    refreshToken: 'refresh-token',
    accessTokenExpires: Date.now() + 3_600_000,
  };
}

describe('resolveEmailVerification', () => {
  it('passes the verdict through when the backend reported a boolean', () => {
    expect(resolveEmailVerification({ emailVerified: false })).toEqual({ emailVerified: false });
    expect(resolveEmailVerification({ emailVerified: true })).toEqual({ emailVerified: true });
  });

  it('adds nothing when the verdict is absent or not a boolean', () => {
    expect(resolveEmailVerification({ id: 'user-1' })).toEqual({});
    expect(resolveEmailVerification({ emailVerified: 'true' })).toEqual({});
    expect(resolveEmailVerification(undefined)).toEqual({});
  });
});

describe('session user email verification', () => {
  let originalFetch: typeof globalThis.fetch;

  beforeEach(() => {
    originalFetch = globalThis.fetch;
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
  });

  it('exposes the verdict in the client-safe session', () => {
    expect(toSession(sessionToken(false)).user.emailVerified).toBe(false);
    expect(toSession(sessionToken(true)).user.emailVerified).toBe(true);
  });

  it('leaves the session user untouched when no verdict was ever reported', () => {
    expect('emailVerified' in toSession(sessionToken()).user).toBe(false);
  });

  it('takes the verdict from the credentials sign-in response', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue(
      Response.json(
        {
          accessToken: 'access-token',
          refreshToken: 'refresh-token',
          expiresIn: 3600,
          userId: '11111111-1111-4111-8111-111111111111',
          email: 'user@example.com',
          user: { id: '11111111-1111-4111-8111-111111111111', emailVerified: false },
        },
        { status: 200 },
      ),
    );

    const result = await CredentialsProvider({ apiUrl: 'http://localhost:8080' }).authorize(
      { email: 'user@example.com', password: 'correct horse battery staple' },
      undefined,
    );

    expect(result?.user.emailVerified).toBe(false);
  });

  it('does not invent a verdict the sign-in response did not carry', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue(
      Response.json(
        {
          accessToken: 'access-token',
          refreshToken: 'refresh-token',
          expiresIn: 3600,
          userId: '11111111-1111-4111-8111-111111111111',
          email: 'user@example.com',
          user: { displayName: 'Test User' },
        },
        { status: 200 },
      ),
    );

    const result = await CredentialsProvider({ apiUrl: 'http://localhost:8080' }).authorize(
      { email: 'user@example.com', password: 'correct horse battery staple' },
      undefined,
    );

    expect('emailVerified' in (result?.user ?? {})).toBe(false);
  });

  it('updates the verdict when a token refresh brings a newer one', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue(
      Response.json({
        accessToken: 'new-access-token',
        refreshToken: 'new-refresh-token',
        expiresIn: 3600,
        user: { id: 'user-1', emailVerified: true },
      }),
    );

    const refreshed = await refreshAccessToken(sessionToken(false), config);

    expect(refreshed.user.emailVerified).toBe(true);
  });

  it('keeps the last known verdict when a token refresh response says nothing', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue(
      Response.json({ accessToken: 'new-access-token', refreshToken: 'new-refresh-token', expiresIn: 3600 }),
    );

    const refreshed = await refreshAccessToken(sessionToken(false), config);

    expect(refreshed.user.emailVerified).toBe(false);
  });

  it('takes the verdict from an OpenID Connect sign-in exchange', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue(
      Response.json({
        accessToken: 'google-access',
        refreshToken: 'google-refresh',
        expiresIn: 3600,
        userId: 'user-1',
        email: 'user@gmail.com',
        user: { displayName: 'Google User', emailVerified: true },
      }),
    );

    const result = await GoogleProvider({ clientId: 'id', clientSecret: 'secret' }).exchangeToken(
      'google-id-token',
      'http://localhost:5295',
    );

    expect(result.user.emailVerified).toBe(true);
  });
});
