import { describe, it, expect, beforeAll } from 'vitest';
import { createClient, type Result, type ApiError } from '@game-guild/client';
import type {
  IdentityAuthenticationSignInOutput,
  CQRSPagedResult,
  IdentityTenantsTenantValidationOutput,
} from '@game-guild/client';

const BASE_URL = process.env.API_BASE_URL ?? 'http://localhost:8080';
const TENANT_ID = process.env.API_TENANT_ID ?? process.env.TENANT_ID ?? undefined;

const unwrapResult = <T>(result: Result<T, ApiError>, label: string): T => {
  if (result.ok) {
    return result.data;
  }
  throw new Error(`${label} failed: ${result.error?.message ?? 'Unknown'} (${result.error?.status})`);
};

describe('Tenants E2E', () => {
  let accessToken: string;

  beforeAll(async () => {
    const client = createClient({
      baseUrl: BASE_URL,
      timeout: 10_000,
      devtools: { enabled: false },
    });

    const unique = `${Date.now()}_${Math.random().toString(36).slice(2, 8)}`;

    const signUpResult = await client.request<IdentityAuthenticationSignInOutput>({
      method: 'POST',
      path: '/v1/auth/sign-up',
      body: {
        username: `tenant_test_${unique}`,
        email: `tenant_test_${unique}@example.com`,
        password: 'Str0ng!Passw0rd123!',
        ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
      },
      requiresAuth: false,
    });

    const signUpData = unwrapResult(signUpResult, 'Tenant test sign-up');
    accessToken = signUpData.accessToken!;
  }, 30_000);

  it('lists tenants with pagination', async () => {
    const authedClient = createClient({
      baseUrl: BASE_URL,
      timeout: 10_000,
      devtools: { enabled: false },
      auth: {
        getAccessToken: async () => accessToken,
      },
    });

    const result = await authedClient.request<CQRSPagedResult>({
      method: 'GET',
      path: '/v1/tenants',
      params: { page: 1, pageSize: 10 },
      requiresAuth: true,
    });

    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.data.items).toBeDefined();
      expect(Array.isArray(result.data.items)).toBe(true);
      expect(result.data.totalCount).toBeGreaterThanOrEqual(0);
    }
  });

  it('validates tenant data', async () => {
    const authedClient = createClient({
      baseUrl: BASE_URL,
      timeout: 10_000,
      devtools: { enabled: false },
      auth: {
        getAccessToken: async () => accessToken,
      },
    });

    const result = await authedClient.request<IdentityTenantsTenantValidationOutput>({
      method: 'POST',
      path: '/v1/tenants:validate',
      body: {
        name: 'Test Tenant',
        slug: 'test-tenant',
        adminEmail: 'admin@test-tenant.com',
      },
      requiresAuth: true,
    });

    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.data.isValid).toBeDefined();
    }
  });
});
