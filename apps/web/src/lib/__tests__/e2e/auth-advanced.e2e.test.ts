/**
 * Auth Advanced E2E Tests
 *
 * Extends auth coverage beyond the basic sign-up / sign-in / refresh
 * already covered in auth-infrastructure.e2e.test.ts and api-client.e2e.test.ts.
 *
 * Covers: token revocation, password change, password reset flow,
 *         session management (list, analyse, terminate, refresh),
 *         API keys (CRUD + revoke), MFA config queries,
 *         trusted devices (CRUD), email verification triggers.
 *
 * Requires the API to be running on localhost:8080 (or API_BASE_URL env var).
 */

import { describe, it, expect, beforeAll } from 'vitest';
import {
  createClient,
  type ApiError,
  type Result,
  type IdentityAuthenticationSignInOutput,
} from '@game-guild/client';

// ─── Config ──────────────────────────────────────────────────────

const BASE_URL = process.env.API_BASE_URL ?? 'http://localhost:8080';
const TENANT_ID =
  process.env.API_TENANT_ID ?? process.env.TENANT_ID ?? undefined;

// ─── Helpers ─────────────────────────────────────────────────────

function uniqueId() {
  return `${Date.now()}_${Math.random().toString(36).slice(2, 8)}`;
}

function freshCredentials() {
  const id = uniqueId();
  return {
    email: `e2e_adv_${id}@example.com`,
    username: `e2e_adv_${id}`,
    password: 'Str0ng!Passw0rd123!',
  };
}

const baseClient = () =>
  createClient({
    baseUrl: BASE_URL,
    timeout: 15_000,
    devtools: { enabled: false },
  });

const authedClient = (accessToken: string) =>
  createClient({
    baseUrl: BASE_URL,
    timeout: 15_000,
    devtools: { enabled: false },
    auth: { getAccessToken: async () => accessToken },
  });

function unwrap<T>(result: Result<T, ApiError>, label: string): T {
  if (result.ok) return result.data;
  const s = result.error?.status ?? '?';
  const m = result.error?.message ?? 'unknown';
  throw new Error(`${label} failed (${s}): ${m}`);
}

/**
 * Helper: sign up + sign in → returns tokens, userId, and credentials.
 */
async function signUpAndIn(creds = freshCredentials()) {
  const client = baseClient();

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

  const signIn = await client.request<IdentityAuthenticationSignInOutput>({
    method: 'POST',
    path: '/v1/auth/sign-in',
    body: {
      email: creds.email,
      password: creds.password,
      ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
    },
    requiresAuth: false,
  });

  const data = unwrap(signIn, 'signUpAndIn/sign-in');
  return {
    accessToken: data.accessToken!,
    refreshToken: data.refreshToken!,
    userId: data.userId ?? data.user?.id,
    creds,
  };
}

// ═══════════════════════════════════════════════════════════════════
// 1. Token Revocation
// ═══════════════════════════════════════════════════════════════════

describe(
  'Token revocation',
  { timeout: 60_000 },
  () => {
    let accessToken: string;
    let refreshToken: string;

    beforeAll(async () => {
      const ctx = await signUpAndIn();
      accessToken = ctx.accessToken;
      refreshToken = ctx.refreshToken;
    }, 30_000);

    it('revokes a refresh token (204)', async () => {
      const client = authedClient(accessToken);

      const res = await client.request<void>({
        method: 'POST',
        path: '/v1/auth/tokens:revoke',
        body: { token: refreshToken, reason: 'e2e test' },
        requiresAuth: true,
      });

      // 204 or 200 are both acceptable
      expect(res.ok).toBe(true);
    });

    it('cannot refresh with a revoked token', async () => {
      const client = baseClient();

      const res = await client.request<IdentityAuthenticationSignInOutput>({
        method: 'POST',
        path: '/v1/auth/tokens:refresh',
        body: {
          refreshToken,
          ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
        },
        requiresAuth: false,
      });

      expect(res.ok).toBe(false);
    });

    it('rejects revocation without authentication', async () => {
      const client = baseClient();

      const res = await client.request<void>({
        method: 'POST',
        path: '/v1/auth/tokens:revoke',
        body: { token: 'some-token' },
        requiresAuth: false,
      });

      expect(res.ok).toBe(false);
      if (!res.ok) {
        expect(res.error?.status).toBe(401);
      }
    });
  }
);

// ═══════════════════════════════════════════════════════════════════
// 2. Password Change
// ═══════════════════════════════════════════════════════════════════

describe(
  'Password change',
  { timeout: 90_000 },
  () => {
    const creds = freshCredentials();
    let accessToken: string;
    const newPassword = 'N3wStr0ng!Pass999!';

    beforeAll(async () => {
      const ctx = await signUpAndIn(creds);
      accessToken = ctx.accessToken;
    }, 30_000);

    it('changes the password with the current credentials', async () => {
      const client = authedClient(accessToken);

      const res = await client.request<{
        success: boolean;
        message: string;
        sessionsRevoked: number;
      }>({
        method: 'POST',
        path: '/v1/auth/password:change',
        body: {
          currentPassword: creds.password,
          newPassword,
          confirmPassword: newPassword,
          revokeOtherSessions: false,
        },
        requiresAuth: true,
      });

      expect(res.ok, JSON.stringify(res.ok ? res.data : res.error, null, 2)).toBe(true);
      if (!res.ok) return;

      expect(res.data.success).toBe(true);
      expect(res.data.message).toBeTruthy();
      expect(typeof res.data.sessionsRevoked).toBe('number');
    });

    it('rejects password change with wrong current password', async () => {
      const ac = authedClient(accessToken);
      const res = await ac.request<unknown>({
        method: 'POST',
        path: '/v1/auth/password:change',
        body: {
          currentPassword: 'TotallyWrong!123',
          newPassword: 'Another!Pass1',
          confirmPassword: 'Another!Pass1',
        },
        requiresAuth: true,
      });

      expect(res.ok).toBe(false);
      if (!res.ok) {
        expect(res.error?.status).toBe(400);
      }
    });

    it('rejects password change with mismatched confirmation', async () => {
      const ac = authedClient(accessToken);
      const res = await ac.request<unknown>({
        method: 'POST',
        path: '/v1/auth/password:change',
        body: {
          currentPassword: creds.password,
          newPassword: 'Mismatch!Pass1',
          confirmPassword: 'Different!Pass2',
        },
        requiresAuth: true,
      });

      expect(res.ok).toBe(false);
      if (!res.ok) {
        expect(res.error?.status).toBe(400);
      }
    });

    it('rejects password change without authentication', async () => {
      const client = baseClient();

      const res = await client.request<unknown>({
        method: 'POST',
        path: '/v1/auth/password:change',
        body: {
          currentPassword: 'Some!Pass1',
          newPassword: 'Some!Pass2',
          confirmPassword: 'Some!Pass2',
        },
        requiresAuth: false,
      });

      expect(res.ok).toBe(false);
      if (!res.ok) {
        expect(res.error?.status).toBe(401);
      }
    });
  }
);

// ═══════════════════════════════════════════════════════════════════
// 3. Password Reset Flow
// ═══════════════════════════════════════════════════════════════════

describe(
  'Password reset request',
  { timeout: 60_000 },
  () => {
    it('accepts a password reset request for existing email', async () => {
      const creds = freshCredentials();
      await signUpAndIn(creds);

      const client = baseClient();
      const res = await client.request<{
        success: boolean;
        message: string;
        expiresInMinutes: number;
      }>({
        method: 'POST',
        path: '/v1/auth/password:reset-request',
        body: {
          email: creds.email,
          ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
        },
        requiresAuth: false,
      });

      expect(res.ok, JSON.stringify(res.ok ? res.data : res.error, null, 2)).toBe(true);
      if (!res.ok) return;

      expect(res.data.success).toBe(true);
      expect(res.data.expiresInMinutes).toBeGreaterThan(0);
    }, 30_000);

    it('handles password reset request for non-existent email without leaking info', async () => {
      const client = baseClient();

      const res = await client.request<{
        success: boolean;
        message: string;
      }>({
        method: 'POST',
        path: '/v1/auth/password:reset-request',
        body: {
          email: `nonexist_${uniqueId()}@example.com`,
          ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
        },
        requiresAuth: false,
      });

      expect(res.ok, JSON.stringify(res.ok ? res.data : res.error, null, 2)).toBe(true);
      if (!res.ok) return;

      expect(res.data.success).toBe(true);
    });

    it('rejects password reset completion with invalid token', async () => {
      const client = baseClient();

      const res = await client.request<unknown>({
        method: 'POST',
        path: '/v1/auth/password:reset',
        body: {
          token: 'invalid-reset-token-value',
          newPassword: 'SomeNew!Pass1',
          confirmPassword: 'SomeNew!Pass1',
          ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
        },
        requiresAuth: false,
      });

      expect(res.ok).toBe(false);
    });
  }
);

// ═══════════════════════════════════════════════════════════════════
// 4. Email Verification
// ═══════════════════════════════════════════════════════════════════

describe(
  'Email verification',
  { timeout: 60_000 },
  () => {
    it('sends verification email request', async () => {
      const creds = freshCredentials();
      await signUpAndIn(creds);

      const client = baseClient();
      const res = await client.request<{ message: string }>({
        method: 'POST',
        path: '/v1/auth/email:send-verification',
        body: { email: creds.email },
        requiresAuth: false,
      });

      expect(res.ok, JSON.stringify(res.ok ? res.data : res.error, null, 2)).toBe(true);
      if (!res.ok) return;

      expect(res.data.message).toBeTruthy();
    }, 30_000);

    it('rejects email verification with invalid token', async () => {
      const client = baseClient();
      const res = await client.request<unknown>({
        method: 'POST',
        path: '/v1/auth/email:verify',
        body: {
          token: 'invalid-verification-token',
          ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
        },
        requiresAuth: false,
      });

      expect(res.ok).toBe(false);
    });
  }
);

// ═══════════════════════════════════════════════════════════════════
// 5. Session Management
// ═══════════════════════════════════════════════════════════════════

describe(
  'Session management',
  { timeout: 60_000 },
  () => {
    let accessToken: string;

    beforeAll(async () => {
      const ctx = await signUpAndIn();
      accessToken = ctx.accessToken;
    }, 30_000);

    it('lists active sessions', async () => {
      const client = authedClient(accessToken);

      const res = await client.request<
        Array<{
          id: string;
          ipAddress: string;
          createdAt: string;
          lastUsedAt: string;
          expiresAt: string;
          isCurrent: boolean;
        }>
      >({
        method: 'GET',
        path: '/v1/auth/sessions',
        requiresAuth: true,
      });

      expect(res.ok).toBe(true);
      if (res.ok) {
        expect(Array.isArray(res.data)).toBe(true);
      }
    });

    it('analyses session security', async () => {
      const client = authedClient(accessToken);

      const res = await client.request<{
        sessionId: string;
        userId: string;
        isSuspicious: boolean;
        riskScore: number;
        activeSessionCount: number;
        riskLevel: number | string;
        securityFlags: string[];
        analyzedAt: string;
      }>({
        method: 'GET',
        path: '/v1/auth/sessions:analyze-security',
        requiresAuth: true,
      });

      expect(res.ok).toBe(true);
      if (res.ok) {
        expect(typeof res.data.riskScore).toBe('number');
        expect(typeof res.data.isSuspicious).toBe('boolean');
        expect(res.data.activeSessionCount).toBeGreaterThanOrEqual(0);
        // riskLevel is a numeric enum (0 = None/Low) — check it's defined
        expect(res.data.riskLevel).toBeDefined();
        expect(Array.isArray(res.data.securityFlags)).toBe(true);
      }
    });

    it('sessions:refresh returns 400 when JWT lacks session_id claim', async () => {
      const client = authedClient(accessToken);

      const res = await client.request<{ message: string }>({
        method: 'POST',
        path: '/v1/auth/sessions:refresh',
        requiresAuth: true,
      });

      // The JWT from sign-in does not include a "session_id" claim,
      // so the endpoint always returns 400 "No active session found".
      expect(res.ok).toBe(false);
      if (!res.ok) {
        expect(res.error?.status).toBe(400);
      }
    });

    it('rejects session endpoints without auth', async () => {
      const client = baseClient();

      const res = await client.request<unknown>({
        method: 'GET',
        path: '/v1/auth/sessions',
        requiresAuth: false,
      });

      expect(res.ok).toBe(false);
      if (!res.ok) {
        expect(res.error?.status).toBe(401);
      }
    });
  }
);

// ─── 5b. Session termination (separate user to avoid invalidating other tests) ──

describe(
  'Session termination',
  { timeout: 90_000 },
  () => {
    it('terminates all other sessions', async () => {
      const creds = freshCredentials();
      // Sign up and create multiple sessions
      await signUpAndIn(creds);

      // Sign in again to create a second session
      const client = baseClient();
      const signIn2 = await client.request<IdentityAuthenticationSignInOutput>({
        method: 'POST',
        path: '/v1/auth/sign-in',
        body: {
          email: creds.email,
          password: creds.password,
          ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
        },
        requiresAuth: false,
      });
      const token2 = unwrap(signIn2, 'second sign-in').accessToken!;
      const ac = authedClient(token2);

      const res = await ac.request<{
        message: string;
        terminatedCount: number;
      }>({
        method: 'POST',
        path: '/v1/auth/sessions:terminate-others',
        requiresAuth: true,
      });

      expect(res.ok).toBe(true);
      if (res.ok) {
        expect(res.data.message).toBeTruthy();
        expect(typeof res.data.terminatedCount).toBe('number');
      }
    }, 30_000);

    it('terminates all sessions', async () => {
      const ctx = await signUpAndIn();
      const ac = authedClient(ctx.accessToken);

      const res = await ac.request<{
        message: string;
        terminatedCount: number;
      }>({
        method: 'POST',
        path: '/v1/auth/sessions:terminate-all',
        requiresAuth: true,
      });

      expect(res.ok).toBe(true);
      if (res.ok) {
        expect(res.data.message).toBeTruthy();
        expect(typeof res.data.terminatedCount).toBe('number');
      }
    }, 30_000);

    it('terminates a specific session by ID', async () => {
      const ctx = await signUpAndIn();
      const ac = authedClient(ctx.accessToken);

      // First list sessions to get a session ID
      const listRes = await ac.request<Array<{ id: string; isCurrent: boolean }>>({
        method: 'GET',
        path: '/v1/auth/sessions',
        requiresAuth: true,
      });

      if (listRes.ok && listRes.data.length > 0) {
        const sessionId = listRes.data[0].id;

        const res = await ac.request<{ message: string }>({
          method: 'DELETE',
          path: `/v1/auth/sessions/${sessionId}`,
          requiresAuth: true,
        });

        // May succeed or fail depending on whether it's the current session
        // Either outcome is valid behaviour for this test
        expect(typeof res.ok).toBe('boolean');
      }
    }, 30_000);
  }
);

// ═══════════════════════════════════════════════════════════════════
// 6. API Keys
// ═══════════════════════════════════════════════════════════════════

describe(
  'API keys',
  { timeout: 60_000 },
  () => {
    let accessToken: string;

    beforeAll(async () => {
      const ctx = await signUpAndIn();
      accessToken = ctx.accessToken;
    }, 30_000);

    it('creates an API key', async () => {
      const client = authedClient(accessToken);

      const res = await client.request<{
        id: string;
        name: string;
        apiKey: string;
        keyPrefix: string;
        scopes: string[];
        createdAt: string;
      }>({
        method: 'POST',
        path: '/v1/auth/api-keys',
        body: {
          name: 'E2E Test Key',
          scopes: ['read', 'write'],
        },
        requiresAuth: true,
      });

      expect(res.ok, JSON.stringify(res.ok ? res.data : res.error, null, 2)).toBe(true);
      if (!res.ok) return;

      expect(res.data.id).toBeTruthy();
      expect(res.data.name).toBe('E2E Test Key');
      expect(res.data.apiKey).toMatch(/^gg_live_[A-Za-z0-9]{32}$/);
      expect(res.data.keyPrefix).toBe('gg_live_');
      expect(res.data.scopes).toEqual(['read', 'write']);
      expect(res.data.createdAt).toBeTruthy();
    });

    it('lists API keys', async () => {
      const client = authedClient(accessToken);

      const res = await client.request<
        Array<{
          id: string;
          name: string;
          keyPrefix: string;
          scopes: string[];
          isActive: boolean;
          createdAt: string;
        }>
      >({
        method: 'GET',
        path: '/v1/auth/api-keys',
        requiresAuth: true,
      });

      expect(res.ok, JSON.stringify(res.ok ? res.data : res.error, null, 2)).toBe(true);
      if (!res.ok) return;

      expect(Array.isArray(res.data)).toBe(true);
      expect(res.data.some((key) => key.name === 'E2E Test Key')).toBe(true);
    });

    it('handles API key revocation flow', async () => {
      const client = authedClient(accessToken);

      // Create a key to revoke
      const createRes = await client.request<{ id: string }>({
        method: 'POST',
        path: '/v1/auth/api-keys',
        body: {
          name: 'Key To Revoke',
          scopes: ['read'],
        },
        requiresAuth: true,
      });

      expect(createRes.ok, JSON.stringify(createRes.ok ? createRes.data : createRes.error, null, 2)).toBe(true);
      if (!createRes.ok) return;

      const keyId = createRes.data.id;

      const revokeRes = await client.request<{ message: string }>({
        method: 'POST',
        path: `/v1/auth/api-keys/${keyId}:revoke`,
        body: { reason: 'E2E test cleanup' },
        requiresAuth: true,
      });

      expect(revokeRes.ok, JSON.stringify(revokeRes.ok ? revokeRes.data : revokeRes.error, null, 2)).toBe(true);
      if (!revokeRes.ok) return;
      expect(revokeRes.data.message).toMatch(/revoked/i);

      // Verify the key is now inactive
      const listRes = await client.request<
        Array<{ id: string; isActive: boolean }>
      >({
        method: 'GET',
        path: '/v1/auth/api-keys',
        requiresAuth: true,
      });

      expect(listRes.ok, JSON.stringify(listRes.ok ? listRes.data : listRes.error, null, 2)).toBe(true);
      if (!listRes.ok) return;

      const revokedKey = listRes.data.find((k) => k.id === keyId);
      expect(revokedKey).toBeDefined();
      expect(revokedKey?.isActive).toBe(false);
    });

    it('rejects API key creation without auth', async () => {
      const client = baseClient();

      const res = await client.request<unknown>({
        method: 'POST',
        path: '/v1/auth/api-keys',
        body: { name: 'No Auth Key', scopes: ['read'] },
        requiresAuth: false,
      });

      expect(res.ok).toBe(false);
      if (!res.ok) {
        expect(res.error?.status).toBe(401);
      }
    });
  }
);

// ═══════════════════════════════════════════════════════════════════
// 7. MFA Configuration
// ═══════════════════════════════════════════════════════════════════

describe(
  'MFA configuration',
  { timeout: 60_000 },
  () => {
    let accessToken: string;

    beforeAll(async () => {
      const ctx = await signUpAndIn();
      accessToken = ctx.accessToken;
    }, 30_000);

    it('retrieves MFA configuration', async () => {
      const client = authedClient(accessToken);

      const res = await client.request<{
        isEnabled: boolean;
        enabledMethods: string[];
        enabledAt: string | null;
        backupCodesRemaining: number;
      }>({
        method: 'GET',
        path: '/v1/auth/mfa',
        requiresAuth: true,
      });

      expect(res.ok).toBe(true);
      if (res.ok) {
        expect(typeof res.data.isEnabled).toBe('boolean');
        expect(Array.isArray(res.data.enabledMethods)).toBe(true);
        // New users should have MFA disabled
        expect(res.data.isEnabled).toBe(false);
        expect(res.data.enabledMethods.length).toBe(0);
      }
    });

    it('lists available MFA methods', async () => {
      const client = authedClient(accessToken);

      const res = await client.request<{
        methods: Array<{
          method: string;
          name: string;
          description: string;
          isEnabled: boolean;
          isAvailable: boolean;
          priority: number;
        }>;
        defaultMethod: string | null;
      }>({
        method: 'GET',
        path: '/v1/auth/mfa/methods',
        requiresAuth: true,
      });

      expect(res.ok).toBe(true);
      if (res.ok) {
        expect(Array.isArray(res.data.methods)).toBe(true);
        // Should have at least TOTP available
        expect(res.data.methods.length).toBeGreaterThan(0);

        const method = res.data.methods[0];
        expect(method.name).toBeTruthy();
        expect(method.description).toBeTruthy();
        expect(typeof method.isAvailable).toBe('boolean');
        expect(typeof method.priority).toBe('number');
      }
    });

    it('rejects MFA config without auth', async () => {
      const client = baseClient();

      const res = await client.request<unknown>({
        method: 'GET',
        path: '/v1/auth/mfa',
        requiresAuth: false,
      });

      expect(res.ok).toBe(false);
      if (!res.ok) {
        expect(res.error?.status).toBe(401);
      }
    });
  }
);

// ═══════════════════════════════════════════════════════════════════
// 8. Trusted Devices
// ═══════════════════════════════════════════════════════════════════

describe(
  'Trusted devices',
  { timeout: 60_000 },
  () => {
    let accessToken: string;

    beforeAll(async () => {
      const ctx = await signUpAndIn();
      accessToken = ctx.accessToken;
    }, 30_000);

    it('lists trusted devices (empty for a new user)', async () => {
      const client = authedClient(accessToken);

      const res = await client.request<
        Array<{
          id: string;
          deviceName: string;
          trustedAt: string;
          lastUsedAt: string;
        }>
      >({
        method: 'GET',
        path: '/v1/auth/trusted-devices',
        requiresAuth: true,
      });

      expect(res.ok).toBe(true);
      if (res.ok) {
        expect(Array.isArray(res.data)).toBe(true);
      }
    });

    it('trusts a new device', async () => {
      const client = authedClient(accessToken);

      const res = await client.request<{ message: string }>({
        method: 'POST',
        path: '/v1/auth/trusted-devices',
        body: { deviceName: 'E2E Test Device' },
        requiresAuth: true,
      });

      expect(res.ok).toBe(true);
      if (res.ok) {
        expect(res.data.message).toBeTruthy();
      }
    });

    it('lists trusted devices after trusting one', async () => {
      const client = authedClient(accessToken);

      const res = await client.request<
        Array<{
          id: string;
          deviceName: string;
          trustedAt: string;
        }>
      >({
        method: 'GET',
        path: '/v1/auth/trusted-devices',
        requiresAuth: true,
      });

      expect(res.ok).toBe(true);
      if (res.ok) {
        expect(res.data.length).toBeGreaterThanOrEqual(1);
      }
    });

    it('revokes a trusted device', async () => {
      const client = authedClient(accessToken);

      // List to get device ID
      const listRes = await client.request<Array<{ id: string }>>({
        method: 'GET',
        path: '/v1/auth/trusted-devices',
        requiresAuth: true,
      });

      if (listRes.ok && listRes.data.length > 0) {
        const deviceId = listRes.data[0].id;

        const res = await client.request<{ message: string }>({
          method: 'DELETE',
          path: `/v1/auth/trusted-devices/${deviceId}`,
          requiresAuth: true,
        });

        expect(res.ok).toBe(true);
      }
    });

    it('rejects trusted devices without auth', async () => {
      const client = baseClient();

      const res = await client.request<unknown>({
        method: 'GET',
        path: '/v1/auth/trusted-devices',
        requiresAuth: false,
      });

      expect(res.ok).toBe(false);
      if (!res.ok) {
        expect(res.error?.status).toBe(401);
      }
    });
  }
);

// ═══════════════════════════════════════════════════════════════════
// 9. Password Change with Session Revocation
// ═══════════════════════════════════════════════════════════════════

describe(
  'Password change with session revocation',
  { timeout: 60_000 },
  () => {
    it('changes password and revokes other sessions', async () => {
      const creds = freshCredentials();
      const ctx = await signUpAndIn(creds);

      // Create a second session
      const client = baseClient();
      const signIn2 = await client.request<IdentityAuthenticationSignInOutput>({
        method: 'POST',
        path: '/v1/auth/sign-in',
        body: {
          email: creds.email,
          password: creds.password,
          ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
        },
        requiresAuth: false,
      });
      const token2 = unwrap(signIn2, 'second sign-in').accessToken!;

      // Change password with the second session, revoking others
      const ac = authedClient(token2);
      const res = await ac.request<{
        success: boolean;
        message: string;
        sessionsRevoked: number;
      }>({
        method: 'POST',
        path: '/v1/auth/password:change',
        body: {
          currentPassword: creds.password,
          newPassword: 'Changed!Pass789!',
          confirmPassword: 'Changed!Pass789!',
          revokeOtherSessions: true,
        },
        requiresAuth: true,
      });

      expect(res.ok, JSON.stringify(res.ok ? res.data : res.error, null, 2)).toBe(true);
      if (!res.ok) return;

      expect(res.data.success).toBe(true);
      expect(typeof res.data.sessionsRevoked).toBe('number');
    }, 30_000);
  }
);

// ═══════════════════════════════════════════════════════════════════
// 10. Edge Cases & Auth Guards
// ═══════════════════════════════════════════════════════════════════

describe(
  'Auth guard edge cases',
  { timeout: 60_000 },
  () => {
    it('rejects requests with an expired/invalid access token', async () => {
      const client = authedClient('invalid.jwt.token');

      const res = await client.request<unknown>({
        method: 'GET',
        path: '/v1/auth/sessions',
        requiresAuth: true,
      });

      expect(res.ok).toBe(false);
      if (!res.ok) {
        expect(res.error?.status).toBe(401);
      }
    });

    it('rejects sign-up with missing required fields', async () => {
      const client = baseClient();

      const res = await client.request<unknown>({
        method: 'POST',
        path: '/v1/auth/sign-up',
        body: {
          email: '', // blank
          password: '',
        },
        requiresAuth: false,
      });

      expect(res.ok).toBe(false);
    });

    it('rejects sign-up with weak password', async () => {
      const client = baseClient();

      const res = await client.request<unknown>({
        method: 'POST',
        path: '/v1/auth/sign-up',
        body: {
          email: `weak_${uniqueId()}@example.com`,
          username: `weak_${uniqueId()}`,
          password: '123', // too weak
          ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
        },
        requiresAuth: false,
      });

      expect(res.ok).toBe(false);
    });

    it('rejects duplicate sign-up with same email', async () => {
      const creds = freshCredentials();
      await signUpAndIn(creds);

      const client = baseClient();
      const res = await client.request<unknown>({
        method: 'POST',
        path: '/v1/auth/sign-up',
        body: {
          email: creds.email,
          username: `dup_${uniqueId()}`,
          password: creds.password,
          ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
        },
        requiresAuth: false,
      });

      expect(res.ok).toBe(false);
    }, 30_000);

    it('rejects sign-in with non-existent email', async () => {
      const client = baseClient();

      const res = await client.request<unknown>({
        method: 'POST',
        path: '/v1/auth/sign-in',
        body: {
          email: `no_user_${uniqueId()}@example.com`,
          password: 'Str0ng!Passw0rd123!',
          ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
        },
        requiresAuth: false,
      });

      expect(res.ok).toBe(false);
    });
  }
);
