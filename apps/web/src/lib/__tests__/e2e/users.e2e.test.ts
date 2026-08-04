import { describe, it, expect, beforeAll } from 'vitest';
import { createClient, type Result, type ApiError } from '@game-guild/client';
import type {
  IdentityAuthenticationSignInOutput,
  IdentityUsersUser,
  IdentityUsersUserProfile,
} from '@game-guild/client';

const BASE_URL = process.env.API_BASE_URL ?? 'http://localhost:8080';
const TENANT_ID = process.env.API_TENANT_ID ?? process.env.TENANT_ID ?? undefined;
const SYSTEM_ADMIN_EMAIL = process.env.E2E_SYSTEM_ADMIN_EMAIL ?? 'admin@game-guild.com';
const SYSTEM_ADMIN_PASSWORD = process.env.E2E_SYSTEM_ADMIN_PASSWORD ?? 'Admin123!';
const SYSTEM_ADMIN_TENANT_ID = process.env.E2E_SYSTEM_ADMIN_TENANT_ID ?? TENANT_ID;

const unwrapResult = <T>(result: Result<T, ApiError>, label: string): T => {
  if (result.ok) {
    return result.data;
  }
  throw new Error(`${label} failed: ${result.error?.message ?? 'Unknown'} (${result.error?.status})`);
};

describe('Users E2E', () => {
  let accessToken: string;
  let userId: string;
  let managedUserId: string;
  let email: string;
  let password: string;
  let tenantId: string | undefined = TENANT_ID;
  let systemAdminAccessToken: string;
  let systemAdminUserId: string;
  let systemAdminTenantId: string | undefined;

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

  const createSystemAdminClient = () => createClient({
    baseUrl: BASE_URL,
    timeout: 10_000,
    devtools: { enabled: false },
    auth: {
      getAccessToken: async () => systemAdminAccessToken,
    },
    tenant: {
      getTenantId: async () => systemAdminTenantId,
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

    const managedUnique = `${Date.now()}_${Math.random().toString(36).slice(2, 8)}`;
    const managedSignUpResult = await client.request<IdentityAuthenticationSignInOutput>({
      method: 'POST',
      path: '/v1/auth/sign-up',
      body: {
        username: `managed_user_${managedUnique}`,
        email: `managed_user_${managedUnique}@example.com`,
        password: 'Str0ng!Passw0rd123!',
        ...(tenantId ? { tenantId } : {}),
      },
      requiresAuth: false,
    });

    const managedSignUp = unwrapResult(managedSignUpResult, 'Managed user sign-up');
    const rawManagedUserId = managedSignUp.userId ?? managedSignUp.user?.id;
    managedUserId = rawManagedUserId && rawManagedUserId !== '00000000-0000-0000-0000-000000000000'
      ? rawManagedUserId
      : managedSignUp.user?.id ?? '';

    if (tenantId && managedUserId) {
      const addMembershipResult = await createAuthedClient().request<{ success?: boolean; message?: string | null }>({
        method: 'POST',
        path: `/v1/users/${managedUserId}/memberships`,
        body: {
          tenantId,
          role: 'Member',
          invitedByEmail: email,
        },
        requiresAuth: true,
      });

      if (!addMembershipResult.ok && addMembershipResult.error?.status !== 409) {
        throw new Error(
          `Managed user membership add failed: ${addMembershipResult.error?.message ?? 'Unknown'} (${addMembershipResult.error?.status})`,
        );
      }
    }

    const systemAdminSignInResult = await client.request<IdentityAuthenticationSignInOutput>({
      method: 'POST',
      path: '/v1/auth/sign-in',
      body: {
        email: SYSTEM_ADMIN_EMAIL,
        password: SYSTEM_ADMIN_PASSWORD,
        ...(SYSTEM_ADMIN_TENANT_ID ? { tenantId: SYSTEM_ADMIN_TENANT_ID } : {}),
      },
      requiresAuth: false,
    });
    const systemAdminSignIn = unwrapResult(systemAdminSignInResult, 'System administrator sign-in');
    systemAdminAccessToken = systemAdminSignIn.accessToken!;
    systemAdminUserId = systemAdminSignIn.userId || systemAdminSignIn.user?.id || '';
    systemAdminTenantId = systemAdminSignIn.tenantId || SYSTEM_ADMIN_TENANT_ID;
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

  it('promotes and demotes a tenant member through the dashboard membership role endpoint', async () => {
    const authedClient = createAuthedClient();
    expect(tenantId).toBeTruthy();
    expect(managedUserId).toBeTruthy();

    const promoteResult = await authedClient.request<{ success: boolean; message?: string | null }>({
      method: 'PATCH',
      path: `/v1/users/${managedUserId}/memberships/${tenantId}/role`,
      body: { role: 'TenantAdmin' },
      requiresAuth: true,
    });

    expect(promoteResult.ok, JSON.stringify(promoteResult.ok ? promoteResult.data : promoteResult.error, null, 2)).toBe(true);
    if (promoteResult.ok) {
      expect(promoteResult.data.success).toBe(true);
    }

    const promotedMemberships = await authedClient.request<{
      memberships: Array<{ tenantId: string; role: string; isActive: boolean }>;
    }>({
      method: 'GET',
      path: `/v1/users/${managedUserId}/memberships`,
      requiresAuth: true,
    });

    expect(promotedMemberships.ok).toBe(true);
    if (promotedMemberships.ok) {
      expect(promotedMemberships.data.memberships).toEqual(
        expect.arrayContaining([
          expect.objectContaining({
            tenantId,
            role: 'TenantAdmin',
            isActive: true,
          }),
        ]),
      );
    }

    const demoteResult = await authedClient.request<{ success: boolean; message?: string | null }>({
      method: 'PATCH',
      path: `/v1/users/${managedUserId}/memberships/${tenantId}/role`,
      body: { role: 'Member' },
      requiresAuth: true,
    });

    expect(demoteResult.ok, JSON.stringify(demoteResult.ok ? demoteResult.data : demoteResult.error, null, 2)).toBe(true);
    if (demoteResult.ok) {
      expect(demoteResult.data.success).toBe(true);
    }
  });

  it('manages custom roles, effective assignments, groups, and group membership end to end', async () => {
    const authedClient = createSystemAdminClient();
    const suffix = `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
    expect(tenantId).toBeTruthy();
    expect(userId).toBeTruthy();
    expect(managedUserId).toBeTruthy();
    expect(systemAdminUserId).toBeTruthy();

    const roleResult = await authedClient.request<{ id: string; name: string; permissions: string[] }>({
      method: 'POST',
      path: '/v1/roles',
      body: {
        name: `E2E Course Operator ${suffix}`,
        description: 'Created by the administration lifecycle E2E test.',
        permissions: ['courses:read', 'courses:update', 'groups:members'],
        tenantId: null,
      },
      requiresAuth: true,
    });
    const role = unwrapResult(roleResult, 'Create custom role');

    const assignResult = await authedClient.request<{ roleId: string; userId: string; assignedBy?: string | null }>({
      method: 'POST',
      path: '/v1/roles/:assign',
      body: { userId: managedUserId, roleId: role.id, expiresAt: null },
      requiresAuth: true,
    });
    const assignment = unwrapResult(assignResult, 'Assign custom role');
    expect(assignment.roleId).toBe(role.id);
    expect(assignment.userId).toBe(managedUserId);
    expect(assignment.assignedBy).toBe(systemAdminUserId);

    const userRolesResult = await authedClient.request<Array<{ id: string; name: string; permissions: string[] }>>({
      method: 'GET',
      path: `/v1/roles/user/${managedUserId}`,
      params: { includeExpired: false },
      requiresAuth: true,
    });
    const userRoles = unwrapResult(userRolesResult, 'List user custom roles');
    expect(userRoles).toEqual(expect.arrayContaining([expect.objectContaining({ id: role.id })]));

    const groupResult = await authedClient.request<{
      id: string;
      ownerId: string;
      status: string;
      memberCount: number;
    }>({
      method: 'POST',
      path: '/api/social/groups',
      body: {
        ownerId: userId,
        tenantId,
        name: `E2E Administration Group ${suffix}`,
        slug: `e2e-administration-group-${suffix}`,
        description: 'Created by the administration lifecycle E2E test.',
        type: 'ProjectTeam',
        visibility: 'Public',
      },
      requiresAuth: true,
    });
    const group = unwrapResult(groupResult, 'Create social group');
    expect(group.ownerId).toBe(systemAdminUserId);
    expect(group.memberCount).toBe(1);

    const addMemberResult = await authedClient.request<{ userId: string; role: string; status: string }>({
      method: 'POST',
      path: `/api/social/groups/${group.id}/members`,
      body: { userId: managedUserId, requestedRole: 'Moderator' },
      requiresAuth: true,
    });
    const groupMember = unwrapResult(addMemberResult, 'Add social group member');
    expect(groupMember).toMatchObject({ userId: managedUserId, role: 'Moderator', status: 'Active' });

    const changeRoleResult = await authedClient.request<void>({
      method: 'PUT',
      path: `/api/social/groups/${group.id}/members/${managedUserId}/role`,
      body: { role: 'Admin' },
      requiresAuth: true,
    });
    expect(changeRoleResult.ok).toBe(true);

    const groupMembersResult = await authedClient.request<Array<{ userId: string; role: string; status: string }>>({
      method: 'GET',
      path: `/api/social/groups/${group.id}/members`,
      requiresAuth: true,
    });
    const groupMembers = unwrapResult(groupMembersResult, 'List social group members');
    expect(groupMembers).toEqual(
      expect.arrayContaining([expect.objectContaining({ userId: managedUserId, role: 'Admin', status: 'Active' })]),
    );

    const removeMemberResult = await authedClient.request<void>({
      method: 'DELETE',
      path: `/api/social/groups/${group.id}/members/${managedUserId}`,
      requiresAuth: true,
    });
    expect(removeMemberResult.ok).toBe(true);

    const archiveResult = await authedClient.request<void>({
      method: 'POST',
      path: `/api/social/groups/${group.id}/archive`,
      requiresAuth: true,
    });
    expect(archiveResult.ok).toBe(true);

    const removeRoleResult = await authedClient.request<void>({
      method: 'POST',
      path: '/v1/roles/:remove',
      body: { userId: managedUserId, roleId: role.id },
      requiresAuth: true,
    });
    expect(removeRoleResult.ok).toBe(true);

    const deleteRoleResult = await authedClient.request<void>({
      method: 'DELETE',
      path: `/v1/roles/${role.id}`,
      requiresAuth: true,
    });
    expect(deleteRoleResult.ok).toBe(true);
  });
});
