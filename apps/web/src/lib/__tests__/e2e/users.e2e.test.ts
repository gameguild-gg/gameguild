import { describe, it, expect, beforeAll } from 'vitest';
import { createClient, type Result, type ApiError } from '@game-guild/client';
import type {
  IdentityAuthenticationSignInOutput,
  IdentityUsersUser,
  IdentityUsersUserProfile,
} from '@game-guild/client';

const BASE_URL = process.env.API_BASE_URL ?? 'http://localhost:5295';
const TENANT_ID = process.env.API_TENANT_ID ?? process.env.TENANT_ID ?? undefined;

const unwrapResult = <T>(result: Result<T, ApiError>, label: string): T => {
  if (result.ok) {
    return result.data;
  }
  throw new Error(`${label} failed: ${result.error?.message ?? 'Unknown'} (${result.error?.status})`);
};

describe('Users E2E', () => {
  let accessToken: string;
  let userId: string;
  let email: string;
  let password: string;
  let tenantId: string | undefined = TENANT_ID;

  const createAuthedClient = () => createClient({
    baseUrl: BASE_URL,
    timeout: 10_000,
    devtools: { enabled: false },
    auth: {
      getAccessToken: async () => accessToken,
    },
    tenant: {
      getTenantId: async () => tenantId,
    },
  });

  beforeAll(async () => {
    const client = createClient({
      baseUrl: BASE_URL,
      timeout: 10_000,
      devtools: { enabled: false },
    });

    const unique = `${Date.now()}_${Math.random().toString(36).slice(2, 8)}`;
    email = `users_test_${unique}@example.com`;
    password = 'Str0ng!Passw0rd123!';

    const signUpResult = await client.request<IdentityAuthenticationSignInOutput>({
      method: 'POST',
      path: '/v1/auth/sign-up',
      body: {
        username: `users_test_${unique}`,
        email,
        password,
        ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
      },
      requiresAuth: false,
    });

    const signUpData = unwrapResult(signUpResult, 'Users test sign-up');
    accessToken = signUpData.accessToken!;
    // userId may be empty GUID at top level; use user.id as fallback
    const rawUserId = signUpData.userId ?? signUpData.user?.id;
    userId = rawUserId && rawUserId !== '00000000-0000-0000-0000-000000000000'
      ? rawUserId
      : signUpData.user?.id ?? '';

    if (!TENANT_ID) {
      const authedClient = createAuthedClient();

      const tenantResult = await authedClient.request<{ id: string }>({
        method: 'POST',
        path: '/v1/tenants',
        body: {
          name: `Users E2E Tenant ${unique}`,
          slug: `users-e2e-${unique.replace(/_/g, '-')}`,
          adminEmail: email,
          description: 'Tenant created for users E2E coverage',
        },
        requiresAuth: true,
      });

      const tenant = unwrapResult(tenantResult, 'Create users E2E tenant');
      tenantId = tenant.id;
      const signInResult = await client.request<IdentityAuthenticationSignInOutput>({
        method: 'POST',
        path: '/v1/auth/sign-in',
        body: {
          email,
          password,
          tenantId,
        },
        requiresAuth: false,
      });

      accessToken = unwrapResult(signInResult, 'Users tenant-owner sign-in').accessToken!;
    }
  }, 30_000);

  it('lists users with pagination', async () => {
    const authedClient = createAuthedClient();

    const result = await authedClient.request<Record<string, unknown>>({
      method: 'GET',
      path: '/v1/users',
      params: { limit: 10 },
      requiresAuth: true,
    });

    expect(result.ok, JSON.stringify(result.ok ? result.data : result.error, null, 2)).toBe(true);
    if (result.ok) {
      expect(result.data).toBeDefined();
    }
  });

  it('gets user by ID', async () => {
    const authedClient = createAuthedClient();

    const result = await authedClient.request<IdentityUsersUser>({
      method: 'GET',
      path: `/v1/users/${userId}`,
      requiresAuth: true,
    });

    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.data.id).toBe(userId);
      expect(result.data.email).toBeDefined();
    }
  });

  it('gets user profile', async () => {
    const authedClient = createAuthedClient();

    const result = await authedClient.request<IdentityUsersUserProfile>({
      method: 'GET',
      path: `/v1/users/${userId}/profile`,
      requiresAuth: true,
    });

    // Profile may not exist for newly created users (404 is acceptable)
    if (result.ok) {
      expect(result.data).toBeDefined();
    } else {
      expect(result.error?.status).toBe(404);
    }
  });

  it('updates user profile partially', async () => {
    const authedClient = createAuthedClient();

    const result = await authedClient.request<void>({
      method: 'PATCH',
      path: `/v1/users/${userId}/profile`,
      body: {
        bio: 'E2E test bio update',
      },
      requiresAuth: true,
    });

    expect(result.ok).toBe(true);
  });
});
