/** @vitest-environment happy-dom */
import { afterEach, describe, expect, it, vi } from 'vitest';
import { createMfaEnrollmentCompleteAction } from '../../src/integrations/next/actions.js';
import { CredentialsProvider } from '../../src/runtime/auth/providers/credentials.js';
import { createJWTPayload } from '../../src/runtime/auth/session.js';
import type { ResolvedAuthConfig } from '../../src/runtime/auth/types.js';

vi.mock('../../src/runtime/auth/csrf.js', () => ({
  createCSRFToken: vi.fn(),
  validateCSRFToken: vi.fn(async () => true),
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
  debug: false,
  trustHost: false,
  tenantHeader: 'X-Tenant-Id',
};
const originalFetch = globalThis.fetch;
afterEach(() => {
  globalThis.fetch = originalFetch;
  vi.restoreAllMocks();
});

describe('createMfaEnrollmentCompleteAction', () => {
  it('finalizes the session once and returns the fresh recovery codes exactly once', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue(
      new Response(
        JSON.stringify({
          accessToken: 'verified-access',
          refreshToken: 'verified-refresh',
          expiresIn: 3600,
          userId: '33333333-3333-4333-8333-333333333333',
          email: 'enrolled@example.com',
          mfaEnrollmentBackupCodes: ['first-once-code', 'second-once-code'],
        }),
        // The provider signs in through the generated client, whose runtime only
        // decodes JSON bodies for application/json responses — match the wire
        // contract the API actually serves.
        { status: 200, headers: { 'content-type': 'application/json' } },
      ),
    );
    const result = await createMfaEnrollmentCompleteAction(config)(mfaToken, '123456');
    expect(result).toEqual({ success: true, backupCodes: ['first-once-code', 'second-once-code'] });
    expect(JSON.parse((globalThis.fetch as ReturnType<typeof vi.fn>).mock.calls[0][1].body)).toEqual({
      mfaToken,
      method: 'Totp',
      code: '123456',
    });
  });

  it('never persists the codes into the JWT session claims', async () => {
    const token = createJWTPayload(
      {
        tokens: { accessToken: 'verified-access', refreshToken: 'verified-refresh', tokenType: 'Bearer' },
        user: { id: 'verified-user', email: 'enrolled@example.com', name: null, image: null, roles: [], permissions: [] },
        mfaEnrollmentBackupCodes: ['first-once-code'],
      },
      config,
    );
    expect(token.accessToken).toBe('verified-access');
    expect(JSON.stringify(token)).not.toContain('first-once-code');
  });

  it('reports a denial from the backend without finalizing a session', async () => {
    // The API rejects a bad completion code with RFC 7807 ProblemDetails (the
    // generated client surfaces title/detail), not a bare { message } body.
    globalThis.fetch = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ title: 'Additional verification failed', status: 401 }), {
        status: 401,
        headers: { 'content-type': 'application/json' },
      }),
    );
    const result = await createMfaEnrollmentCompleteAction(config)(mfaToken, 'wrong-code');
    expect(result).toEqual({ success: false, message: 'Additional verification failed' });
  });

  it('does not expose codes when the completion carries none', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ accessToken: 'verified-access', refreshToken: 'verified-refresh', userId: '33333333-3333-4333-8333-333333333333' }), {
        status: 200,
        headers: { 'content-type': 'application/json' },
      }),
    );
    const result = await createMfaEnrollmentCompleteAction(config)(mfaToken, '123456');
    expect(result).toEqual({ success: true, backupCodes: [] });
  });
});
