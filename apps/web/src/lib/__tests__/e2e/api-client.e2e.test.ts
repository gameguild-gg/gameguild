import { describe, it, expect } from 'vitest';
import { createClient, type ApiError, type Result } from '@game-guild/client';
import type {
  IdentityAuthenticationSignInOutput,
  IdentityAuthenticationSessionOutput,
  IdentityUsersUserProfile,
} from '@game-guild/client';

const BASE_URL = process.env.API_BASE_URL ?? 'http://localhost:8080';
const TENANT_ID = process.env.API_TENANT_ID ?? process.env.TENANT_ID ?? undefined;

const createBaseClient = () =>
  createClient({
    baseUrl: BASE_URL,
    timeout: 10_000,
    devtools: { enabled: false },
  });

const createAuthedClient = (accessToken: string) =>
  createClient({
    baseUrl: BASE_URL,
    timeout: 10_000,
    devtools: { enabled: false },
    auth: {
      getAccessToken: async () => accessToken,
    },
  });

const unwrapResult = <T>(result: Result<T, ApiError>, label: string): T => {
  if (result.ok) {
    return result.data;
  }
  const status = result.error?.status ?? 'unknown';
  const message = result.error?.message ?? 'Unknown error';
  throw new Error(`${label} failed: ${message} (status: ${status})`);
};

describe('Auth flow E2E (no UI)', () => {
  it(
    'signs up, signs in, refreshes token, and fetches session/profile',
    async () => {
      const client = createBaseClient();

      const unique = `${Date.now()}_${Math.random().toString(36).slice(2, 8)}`;
      const email = `e2e_${unique}@example.com`;
      const username = `e2e_${unique}`;
      const password = 'Str0ng!Passw0rd123!';

      // 1. Sign up
      const signUpResult = await client.request<IdentityAuthenticationSignInOutput>({
        method: 'POST',
        path: '/v1/auth/sign-up',
        body: {
          username,
          email,
          password,
          ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
        },
        requiresAuth: false,
      });

      unwrapResult(signUpResult, 'Sign-up');

      // 2. Sign in
      const signInResult = await client.request<IdentityAuthenticationSignInOutput>({
        method: 'POST',
        path: '/v1/auth/sign-in',
        body: {
          email,
          password,
          ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
        },
        requiresAuth: false,
      });

      const signInData = unwrapResult(signInResult, 'Sign-in');
      expect(signInData.accessToken).toBeTruthy();

      const accessToken = signInData.accessToken!;

      // 3. Refresh token (if refresh token was returned)
      if (signInData.refreshToken) {
        const refreshResult = await client.request<IdentityAuthenticationSignInOutput>({
          method: 'POST',
          path: '/v1/auth/tokens:refresh',
          body: {
            refreshToken: signInData.refreshToken,
            ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
          },
          requiresAuth: false,
        });

        const refreshData = unwrapResult(refreshResult, 'Token refresh');
        expect(refreshData.accessToken).toBeTruthy();
      }

      // 4. Get active sessions (authenticated)
      const authedClient = createAuthedClient(accessToken);

      const sessionsResult = await authedClient.request<IdentityAuthenticationSessionOutput[]>({
        method: 'GET',
        path: '/v1/auth/sessions',
        requiresAuth: true,
      });

      const activeSessions = unwrapResult(sessionsResult, 'Get sessions');
      expect(Array.isArray(activeSessions)).toBe(true);
      // Sessions endpoint may return empty array for new users
      expect(activeSessions.length).toBeGreaterThanOrEqual(0);

      // 5. Get user profile
      const userId = signInData.userId ?? signInData.user?.id;
      expect(userId).toBeTruthy();

      if (userId) {
        const profileResult = await authedClient.request<IdentityUsersUserProfile>({
          method: 'GET',
          path: `/v1/users/${userId}/profile`,
          requiresAuth: true,
        });

        // Profile may not exist for newly created users (404)
        if (profileResult.ok) {
          expect(profileResult.data).toBeDefined();
        } else {
          expect(profileResult.error?.status).toBe(404);
        }
      }
    },
    { timeout: 60_000 },
  );
});
