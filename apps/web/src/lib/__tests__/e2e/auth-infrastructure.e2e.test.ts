/**
 * Auth Infrastructure E2E Tests
 *
 * Tests the @game-guild/client auth infrastructure against the real .NET API.
 * Covers: CredentialsProvider, JWT encode/decode, session pipeline,
 *         token refresh, CSRF, cookies, error handling.
 *
 * Requires the API to be running on localhost:8080 (or API_BASE_URL env var).
 */

import { describe, it, expect, beforeAll } from 'vitest';
import {
  createClient,
  CredentialsProvider,
  encodeJWT,
  decodeJWT,
  createJWTPayload,
  toSession,
  shouldRefreshToken,
  refreshAccessToken,
  processSession,
  encodeSession,
  createCSRFToken,
  validateCSRFToken,
  SessionStore,
  CsrfStore,
  resolveCookieOptions,
  CredentialsSignInError,
  isAuthError,
  isCredentialsError,
  TokenRefreshError,
  type ProviderResult,
  type JWTPayload,
  type ResolvedAuthConfig,
  type ApiError,
  type Result,
  type IdentityAuthenticationSignInOutput,
} from '@game-guild/client';

// ─── Config ──────────────────────────────────────────────────────

const BASE_URL = process.env.API_BASE_URL ?? 'http://localhost:8080';
const AUTH_SECRET =
  process.env.AUTH_SECRET ?? 'e2e-test-secret-must-be-at-least-32-chars-long!!';
const TENANT_ID =
  process.env.API_TENANT_ID ?? process.env.TENANT_ID ?? undefined;

/**
 * Generate unique credentials per test run to avoid collisions.
 */
function uniqueCredentials() {
  const id = `${Date.now()}_${Math.random().toString(36).slice(2, 8)}`;
  return {
    email: `e2e_auth_${id}@example.com`,
    username: `e2e_auth_${id}`,
    password: 'Str0ng!Passw0rd123!',
  };
}

/**
 * Build a resolved config for session processing tests.
 */
function makeResolvedConfig(
  overrides: Partial<ResolvedAuthConfig> = {}
): ResolvedAuthConfig {
  return {
    secret: AUTH_SECRET,
    apiUrl: BASE_URL,
    basePath: '/api/auth',
    maxAge: 30 * 24 * 60 * 60, // 30 days
    debug: false,
    providers: [],
    callbacks: {
      jwt: async ({ token }) => token,
      session: async ({ session }) => session,
      signIn: async () => true,
      redirect: async ({ url }) => url,
      authorized: async ({ auth }) => !!auth,
    },
    cookies: resolveCookieOptions(undefined, false),
    pages: {},
    trustHost: true,
    ...overrides,
  };
}

// ─── 1. CredentialsProvider against the real API ─────────────────

describe(
  'CredentialsProvider E2E',
  () => {
    const creds = uniqueCredentials();

    beforeAll(async () => {
      // Sign up the user first via raw HTTP
      const res = await fetch(`${BASE_URL}/v1/auth/sign-up`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          username: creds.username,
          email: creds.email,
          password: creds.password,
          ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
        }),
      });
      expect(res.ok).toBe(true);
    }, 30_000);

    it('authenticates with valid credentials', async () => {
      const provider = CredentialsProvider({ apiUrl: BASE_URL });
      const result = await provider.authorize!(
        {
          email: creds.email,
          password: creds.password,
          ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
        },
        new Request('http://localhost/api/auth/signin', { method: 'POST' })
      );

      expect(result).not.toBeNull();
      expect(result!.tokens.accessToken).toBeTruthy();
      expect(result!.tokens.refreshToken).toBeTruthy();
      expect(result!.user.id).toBeTruthy();
      expect(result!.user.email).toBe(creds.email);
    });

    it('rejects invalid credentials', async () => {
      const provider = CredentialsProvider({ apiUrl: BASE_URL });

      await expect(
        provider.authorize!(
          { email: creds.email, password: 'WrongPassword!!1' },
          new Request('http://localhost/api/auth/signin', { method: 'POST' })
        )
      ).rejects.toThrow(CredentialsSignInError);
    });

    it('throws typed error for invalid credentials', async () => {
      const provider = CredentialsProvider({ apiUrl: BASE_URL });

      try {
        await provider.authorize!(
          { email: creds.email, password: 'WrongPassword!!1' },
          new Request('http://localhost/api/auth/signin', { method: 'POST' })
        );
        expect.fail('Should have thrown');
      } catch (error) {
        expect(isAuthError(error)).toBe(true);
        expect(isCredentialsError(error)).toBe(true);
      }
    });

    it('rejects missing email/password', async () => {
      const provider = CredentialsProvider({ apiUrl: BASE_URL });

      await expect(
        provider.authorize!(
          { email: '', password: '' },
          new Request('http://localhost/api/auth/signin', { method: 'POST' })
        )
      ).rejects.toThrow(CredentialsSignInError);
    });
  },
  { timeout: 60_000 }
);

// ─── 2. JWT Encode / Decode ──────────────────────────────────────

describe('JWT encode/decode E2E', () => {
  it('round-trips a JWTPayload through encrypt → decrypt', async () => {
    const payload: JWTPayload = {
      user: { id: 'u1', email: 'test@example.com', name: 'Test', image: null },
      accessToken: 'at_test',
      refreshToken: 'rt_test',
      accessTokenExpires: Date.now() + 3600_000,
      tenantId: 'tenant-1',
      iat: Math.floor(Date.now() / 1000),
      exp: Math.floor(Date.now() / 1000) + 86400,
    };

    const encrypted = await encodeJWT({
      token: payload,
      secret: AUTH_SECRET,
      maxAge: 86400,
    });

    expect(typeof encrypted).toBe('string');
    expect(encrypted.length).toBeGreaterThan(100);

    const decrypted = await decodeJWT({
      token: encrypted,
      secret: AUTH_SECRET,
    });

    expect(decrypted).not.toBeNull();
    expect(decrypted!.user.id).toBe('u1');
    expect(decrypted!.user.email).toBe('test@example.com');
    expect(decrypted!.accessToken).toBe('at_test');
    expect(decrypted!.refreshToken).toBe('rt_test');
    expect(decrypted!.tenantId).toBe('tenant-1');
  });

  it('returns null for tampered token', async () => {
    const payload: JWTPayload = {
      user: { id: 'u1', email: 'a@b.com', name: null, image: null },
      accessToken: 'at',
      refreshToken: 'rt',
      accessTokenExpires: Date.now() + 3600_000,
      iat: Math.floor(Date.now() / 1000),
      exp: Math.floor(Date.now() / 1000) + 86400,
    };

    const encrypted = await encodeJWT({
      token: payload,
      secret: AUTH_SECRET,
      maxAge: 86400,
    });

    // Tamper with the encrypted token
    const tampered = encrypted.slice(0, -10) + 'AAAAAAAAAA';

    const result = await decodeJWT({
      token: tampered,
      secret: AUTH_SECRET,
    });

    expect(result).toBeNull();
  });

  it('returns null for wrong secret', async () => {
    const payload: JWTPayload = {
      user: { id: 'u1', email: 'a@b.com', name: null, image: null },
      accessToken: 'at',
      refreshToken: 'rt',
      accessTokenExpires: Date.now() + 3600_000,
      iat: Math.floor(Date.now() / 1000),
      exp: Math.floor(Date.now() / 1000) + 86400,
    };

    const encrypted = await encodeJWT({
      token: payload,
      secret: AUTH_SECRET,
      maxAge: 86400,
    });

    const result = await decodeJWT({
      token: encrypted,
      secret: 'wrong-secret-that-is-definitely-different-and-long',
    });

    expect(result).toBeNull();
  });
});

// ─── 3. Session Pipeline (sign-up → JWT → session → process) ────

describe(
  'Session pipeline E2E',
  () => {
    let providerResult: ProviderResult;
    const creds = uniqueCredentials();

    beforeAll(async () => {
      // Sign up + sign in via CredentialsProvider
      await fetch(`${BASE_URL}/v1/auth/sign-up`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          username: creds.username,
          email: creds.email,
          password: creds.password,
          ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
        }),
      });

      const provider = CredentialsProvider({ apiUrl: BASE_URL });
      const result = await provider.authorize!(
        {
          email: creds.email,
          password: creds.password,
          ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
        },
        new Request('http://localhost/api/auth/signin', { method: 'POST' })
      );
      providerResult = result!;
    }, 30_000);

    it('creates a JWTPayload from ProviderResult', () => {
      const config = makeResolvedConfig();
      const payload = createJWTPayload(providerResult, config);

      expect(payload.user.id).toBeTruthy();
      expect(payload.user.email).toBe(creds.email);
      expect(payload.accessToken).toBeTruthy();
      expect(payload.refreshToken).toBeTruthy();
      expect(payload.accessTokenExpires).toBeGreaterThan(Date.now());
      expect(payload.iat).toBeDefined();
      expect(payload.exp).toBeDefined();
    });

    it('converts JWTPayload to a client-safe Session', () => {
      const config = makeResolvedConfig();
      const payload = createJWTPayload(providerResult, config);
      const session = toSession(payload);

      expect(session.user.id).toBe(payload.user.id);
      expect(session.user.email).toBe(creds.email);
      expect(session.expires).toBeTruthy();
      // Session must NOT include tokens
      expect((session as unknown as Record<string, unknown>).accessToken).toBeUndefined();
      expect((session as unknown as Record<string, unknown>).refreshToken).toBeUndefined();
    });

    it('fresh access token does not need refresh', () => {
      const config = makeResolvedConfig();
      const payload = createJWTPayload(providerResult, config);

      expect(shouldRefreshToken(payload)).toBe(false);
    });

    it('near-expiry access token needs refresh', () => {
      const config = makeResolvedConfig();
      const payload = createJWTPayload(providerResult, config);

      // Force the access token to expire in 10 seconds
      payload.accessTokenExpires = Date.now() + 10_000;
      expect(shouldRefreshToken(payload)).toBe(true);
    });

    it('encodes + processes a full session round-trip', async () => {
      const config = makeResolvedConfig();
      const payload = createJWTPayload(providerResult, config);

      // Encode to encrypted cookie value
      const encrypted = await encodeSession(payload, config);
      expect(typeof encrypted).toBe('string');
      expect(encrypted.length).toBeGreaterThan(100);

      // Process the encrypted session (decode + callbacks)
      const { session, token, updated } = await processSession(
        encrypted,
        config
      );

      expect(session).not.toBeNull();
      expect(session!.user.email).toBe(creds.email);
      expect(token).not.toBeNull();
      expect(token!.accessToken).toBeTruthy();
      // Token was fresh, so should NOT have been updated
      expect(updated).toBe(false);
    });

    it('processSession returns null for expired JWT envelope', async () => {
      const config = makeResolvedConfig();
      const payload = createJWTPayload(providerResult, config);

      // Force the outer JWT to have already expired
      payload.exp = Math.floor(Date.now() / 1000) - 100;

      const encrypted = await encodeJWT({
        token: payload,
        secret: config.secret,
        maxAge: 1, // very short max age (but jose might not enforce this on encrypt)
      });

      const { session, token } = await processSession(encrypted, config);

      expect(session).toBeNull();
      expect(token).toBeNull();
    });
  },
  { timeout: 60_000 }
);

// ─── 4. Token Refresh E2E ────────────────────────────────────────

describe(
  'Token refresh E2E',
  () => {
    const creds = uniqueCredentials();

    /**
     * Helper to get a fresh payload (signs in each time so refresh tokens
     * aren't consumed by earlier tests).
     */
    async function getFreshPayload(): Promise<JWTPayload> {
      const provider = CredentialsProvider({ apiUrl: BASE_URL });
      const result = await provider.authorize!(
        {
          email: creds.email,
          password: creds.password,
          ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
        },
        new Request('http://localhost/api/auth/signin', { method: 'POST' })
      );
      return createJWTPayload(result!, makeResolvedConfig());
    }

    beforeAll(async () => {
      await fetch(`${BASE_URL}/v1/auth/sign-up`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          username: creds.username,
          email: creds.email,
          password: creds.password,
          ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
        }),
      });
    }, 30_000);

    it('refreshes the access token via the backend', async () => {
      const config = makeResolvedConfig();
      const payload = await getFreshPayload();

      const refreshed = await refreshAccessToken(payload, config);

      expect(refreshed.accessToken).toBeTruthy();
      expect(refreshed.accessToken).not.toBe(payload.accessToken);
      expect(refreshed.accessTokenExpires).toBeGreaterThan(Date.now());
      // User info is preserved
      expect(refreshed.user.email).toBe(creds.email);
    });

    it('processSession auto-refreshes near-expiry tokens', async () => {
      const config = makeResolvedConfig();
      const payload = await getFreshPayload();

      // Create a payload with a nearly-expired access token
      const nearExpiry = { ...payload };
      nearExpiry.accessTokenExpires = Date.now() + 5_000; // 5 seconds

      const encrypted = await encodeSession(nearExpiry, config);
      const { session, token, updated } = await processSession(
        encrypted,
        config
      );

      expect(session).not.toBeNull();
      expect(token).not.toBeNull();
      // Token should have been auto-refreshed
      expect(updated).toBe(true);
      expect(token!.accessToken).not.toBe(payload.accessToken);
    });

    it('throws TokenRefreshError for invalid refresh token', async () => {
      const config = makeResolvedConfig();
      const payload = await getFreshPayload();

      const badPayload: JWTPayload = {
        ...payload,
        refreshToken: 'invalid-refresh-token',
      };

      await expect(
        refreshAccessToken(badPayload, config)
      ).rejects.toThrow(TokenRefreshError);
    });

    it('throws TokenRefreshError when no refresh token', async () => {
      const config = makeResolvedConfig();
      const payload = await getFreshPayload();

      const noRefresh: JWTPayload = {
        ...payload,
        refreshToken: '',
      };

      await expect(
        refreshAccessToken(noRefresh, config)
      ).rejects.toThrow(TokenRefreshError);
    });
  },
  { timeout: 60_000 }
);

// ─── 5. CSRF Token E2E ──────────────────────────────────────────

describe('CSRF tokens', () => {
  it('creates and validates a CSRF token pair', async () => {
    const { cookie, token } = await createCSRFToken(AUTH_SECRET);

    expect(cookie).toBeTruthy();
    expect(token).toBeTruthy();
    // Cookie contains `randomValue|hash`, token is just the hash
    expect(cookie.includes('|')).toBe(true);

    const isValid = await validateCSRFToken(cookie, token, AUTH_SECRET);
    expect(isValid).toBe(true);
  });

  it('rejects mismatched CSRF tokens', async () => {
    const { cookie } = await createCSRFToken(AUTH_SECRET);
    const { token: otherToken } = await createCSRFToken(AUTH_SECRET);

    const isValid = await validateCSRFToken(cookie, otherToken, AUTH_SECRET);
    expect(isValid).toBe(false);
  });

  it('rejects tampered CSRF cookie', async () => {
    const { token } = await createCSRFToken(AUTH_SECRET);

    const isValid = await validateCSRFToken(
      'tampered-cookie-value',
      token,
      AUTH_SECRET
    );
    expect(isValid).toBe(false);
  });
});

// ─── 6. Cookie SessionStore ──────────────────────────────────────

describe('SessionStore', () => {
  it('writes and reads a session cookie', () => {
    const opts = resolveCookieOptions(undefined, false);
    const store = new SessionStore(opts);

    const cookies = new Map<string, string>();

    const payload = 'encrypted-jwt-session-payload-here';

    store.write(payload, (name, value, _opts) => {
      cookies.set(name, value);
    });

    const read = store.read((name) => cookies.get(name));

    expect(read).toBe(payload);
  });

  it('chunks large payloads and reassembles them', () => {
    const opts = resolveCookieOptions(undefined, false);
    const store = new SessionStore(opts);

    const cookies = new Map<string, string>();

    // Create a large payload (>3800 bytes to trigger chunking)
    const largePayload = 'x'.repeat(8000);

    store.write(largePayload, (name, value, _opts) => {
      cookies.set(name, value);
    });

    // Should have created multiple cookie chunks
    const chunkedKeys = [...cookies.keys()].filter((k) =>
      k.includes('.session-token')
    );
    expect(chunkedKeys.length).toBeGreaterThan(1);

    // Reassemble
    const read = store.read((name) => cookies.get(name));
    expect(read).toBe(largePayload);
  });
});

// ─── 7. Full sign-up → session cookie flow ───────────────────────

describe(
  'Full auth flow: sign-up → provider → JWT → cookie → session',
  () => {
    it(
      'completes the entire pipeline end-to-end',
      async () => {
        const creds = uniqueCredentials();
        const config = makeResolvedConfig();

        // 1. Sign up via raw HTTP
        const signUpRes = await fetch(`${BASE_URL}/v1/auth/sign-up`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            username: creds.username,
            email: creds.email,
            password: creds.password,
            ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
          }),
        });
        expect(signUpRes.ok).toBe(true);

        // 2. Sign in via CredentialsProvider
        const provider = CredentialsProvider({ apiUrl: BASE_URL });
        const result = await provider.authorize!(
          {
            email: creds.email,
            password: creds.password,
            ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
          },
          new Request('http://localhost/api/auth/signin', { method: 'POST' })
        );
        expect(result).not.toBeNull();

        // 3. Create JWT payload
        const payload = createJWTPayload(result!, config);
        expect(payload.user.email).toBe(creds.email);
        expect(payload.accessToken).toBeTruthy();

        // 4. Encode to encrypted session cookie
        const encrypted = await encodeSession(payload, config);
        expect(encrypted.length).toBeGreaterThan(100);

        // 5. Write to cookie store
        const cookies = new Map<string, string>();
        const opts = resolveCookieOptions(undefined, false);
        const store = new SessionStore(opts);
        store.write(encrypted, (name, value) => {
          cookies.set(name, value);
        });

        // 6. Read back and process session
        const readBack = store.read((name) => cookies.get(name));
        expect(readBack).toBe(encrypted);

        const { session, token } = await processSession(readBack!, config);

        expect(session).not.toBeNull();
        expect(session!.user.email).toBe(creds.email);
        expect(session!.user.id).toBeTruthy();
        expect(session!.expires).toBeTruthy();

        // Session must NOT leak tokens
        expect(
          (session as unknown as Record<string, unknown>).accessToken
        ).toBeUndefined();

        // Token should have the real access token
        expect(token).not.toBeNull();
        expect(token!.accessToken).toBeTruthy();

        // 7. Verify the access token works against the backend
        const authedClient = createClient({
          baseUrl: BASE_URL,
          timeout: 10_000,
          devtools: { enabled: false },
          auth: { getAccessToken: async () => token!.accessToken },
        });

        const sessionsResult = await authedClient.request<unknown[]>({
          method: 'GET',
          path: '/v1/auth/sessions',
          requiresAuth: true,
        });

        // Sessions endpoint should succeed (even if empty)
        if (sessionsResult.ok) {
          expect(Array.isArray(sessionsResult.data)).toBe(true);
        }

        // 8. Token refresh should work
        const refreshed = await refreshAccessToken(payload, config);
        expect(refreshed.accessToken).toBeTruthy();
        expect(refreshed.accessToken).not.toBe(payload.accessToken);
      },
      { timeout: 60_000 }
    );
  }
);

// ─── 8. Generated module integration ─────────────────────────────

describe(
  'Generated AuthenticationModule E2E',
  () => {
    it(
      'signs up and signs in via the generated module',
      async () => {
        const creds = uniqueCredentials();

        const client = createClient({
          baseUrl: BASE_URL,
          timeout: 10_000,
          devtools: { enabled: false },
        });

        // Sign up using raw request (the generated module)
        const signUpResult =
          await client.request<IdentityAuthenticationSignInOutput>({
            method: 'POST',
            path: '/v1/auth/sign-up',
            body: {
              username: creds.username,
              email: creds.email,
              password: creds.password,
              ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
            },
            requiresAuth: false,
          });

        expect(signUpResult.ok).toBe(true);
        if (signUpResult.ok) {
          expect(signUpResult.data.accessToken).toBeTruthy();
        }

        // Sign in
        const signInResult =
          await client.request<IdentityAuthenticationSignInOutput>({
            method: 'POST',
            path: '/v1/auth/sign-in',
            body: {
              email: creds.email,
              password: creds.password,
              ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
            },
            requiresAuth: false,
          });

        expect(signInResult.ok).toBe(true);
        if (signInResult.ok) {
          expect(signInResult.data.accessToken).toBeTruthy();
          expect(signInResult.data.userId).toBeTruthy();
          expect(signInResult.data.user).toBeDefined();

          // Feed the result into our auth pipeline
          const config = makeResolvedConfig();
          const provider = CredentialsProvider({ apiUrl: BASE_URL });
          const providerResult = await provider.authorize!(
            {
              email: creds.email,
              password: creds.password,
              ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
            },
            new Request('http://localhost', { method: 'POST' })
          );

          const payload = createJWTPayload(providerResult!, config);
          const session = toSession(payload);

          expect(session.user.email).toBe(creds.email);
          expect(session.expires).toBeTruthy();
        }
      },
      { timeout: 60_000 }
    );
  }
);
