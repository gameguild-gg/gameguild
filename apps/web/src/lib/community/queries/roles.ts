import { getToken } from '@/auth';
import { createServerClient } from '@game-guild/client';

export interface PlatformPermission {
  value: string;
  label: string;
}

export interface PlatformPermissionGroup {
  area: string;
  description: string;
  permissions: PlatformPermission[];
}

export interface PlatformRole {
  id: string;
  name: string;
  description: string;
  permissions: string[];
  isActive: boolean;
  tenantId?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface PermissionTemplate {
  id: string;
  name: string;
  description: string;
  category: string;
  permissions: string[];
  isSystemTemplate: boolean;
  isActive: boolean;
  minimumTier?: string | null;
  createdAt: string;
}

export const PLATFORM_PERMISSION_MATRIX: PlatformPermissionGroup[] = [
  {
    area: 'Users',
    description: 'Identity directory and user lifecycle.',
    permissions: [
      { value: 'users:read', label: 'View users' },
      { value: 'users:create', label: 'Create users' },
      { value: 'users:update', label: 'Edit users' },
      { value: 'users:delete', label: 'Deactivate users' },
      { value: 'users:roles', label: 'Manage user access' },
    ],
  },
  {
    area: 'Roles',
    description: 'Platform roles and permission grants.',
    permissions: [
      { value: 'roles:read', label: 'View roles' },
      { value: 'roles:create', label: 'Create roles' },
      { value: 'roles:update', label: 'Edit roles' },
      { value: 'roles:delete', label: 'Delete roles' },
      { value: 'roles:assign', label: 'Assign roles' },
    ],
  },
  {
    area: 'Groups',
    description: 'Community groups, moderators, and members.',
    permissions: [
      { value: 'groups:read', label: 'View groups' },
      { value: 'groups:create', label: 'Create groups' },
      { value: 'groups:update', label: 'Edit groups' },
      { value: 'groups:delete', label: 'Archive groups' },
      { value: 'groups:members', label: 'Manage members' },
    ],
  },
  {
    area: 'Learning',
    description: 'Course catalog and instructor operations.',
    permissions: [
      { value: 'courses:read', label: 'View courses' },
      { value: 'courses:create', label: 'Create courses' },
      { value: 'courses:update', label: 'Edit courses' },
      { value: 'courses:publish', label: 'Publish courses' },
      { value: 'courses:enrollments', label: 'Manage enrollments' },
    ],
  },
  {
    area: 'Labs',
    description: 'Testing Lab and Launch Pad operations.',
    permissions: [
      { value: 'testing-lab:read', label: 'View testing lab' },
      { value: 'testing-lab:manage', label: 'Manage testing lab' },
      { value: 'launch-pad:read', label: 'View launch pad' },
      { value: 'launch-pad:manage', label: 'Manage launch pad' },
    ],
  },
  {
    area: 'Platform',
    description: 'Operational settings and audit surfaces.',
    permissions: [
      { value: 'platform:settings', label: 'Manage settings' },
      { value: 'platform:audit', label: 'View audit' },
      { value: 'platform:billing', label: 'Manage billing' },
    ],
  },
];

type RolesResponse = PlatformRole[];
type PermissionTemplatesResponse = PermissionTemplate[];

function getApiClient() {
  const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080';
  return createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
  });
}

function normalizeRole(role: Partial<PlatformRole>): PlatformRole {
  return {
    id: String(role.id ?? ''),
    name: String(role.name ?? 'Untitled role'),
    description: String(role.description ?? ''),
    permissions: Array.isArray(role.permissions) ? role.permissions.map(String) : [],
    isActive: role.isActive !== false,
    tenantId: role.tenantId ?? null,
    createdAt: String(role.createdAt ?? new Date(0).toISOString()),
    updatedAt: String(role.updatedAt ?? role.createdAt ?? new Date(0).toISOString()),
  };
}

function normalizeTemplate(template: Partial<PermissionTemplate>): PermissionTemplate {
  return {
    id: String(template.id ?? ''),
    name: String(template.name ?? 'Untitled template'),
    description: String(template.description ?? ''),
    category: String(template.category ?? 'Platform'),
    permissions: Array.isArray(template.permissions) ? template.permissions.map(String) : [],
    isSystemTemplate: Boolean(template.isSystemTemplate),
    isActive: template.isActive !== false,
    minimumTier: template.minimumTier ?? null,
    createdAt: String(template.createdAt ?? new Date(0).toISOString()),
  };
}

export async function getPlatformRoles(options?: { includeInactive?: boolean; tenantId?: string | null }): Promise<{ roles: PlatformRole[]; error?: string | null }> {
  try {
    const client = getApiClient();
    const result = await client.request<RolesResponse>({
      method: 'GET',
      path: '/v1/roles',
      params: {
        includeInactive: options?.includeInactive ?? true,
        tenantId: options?.tenantId || undefined,
      },
      requiresAuth: true,
    });

    if (!result.ok) return { roles: [], error: result.error.message };

    return {
      roles: (result.data ?? []).map(normalizeRole),
      error: null,
    };
  } catch (error) {
    return { roles: [], error: error instanceof Error ? error.message : 'Roles could not be loaded.' };
  }
}

export async function getUserPlatformRoles(userId: string): Promise<{ roles: PlatformRole[]; error?: string | null }> {
  try {
    const client = getApiClient();
    const result = await client.request<RolesResponse>({
      method: 'GET',
      path: `/v1/roles/user/${userId}`,
      params: { includeExpired: false },
      requiresAuth: true,
    });

    if (!result.ok) return { roles: [], error: result.error.message };

    return {
      roles: (result.data ?? []).map(normalizeRole),
      error: null,
    };
  } catch (error) {
    return { roles: [], error: error instanceof Error ? error.message : 'User roles could not be loaded.' };
  }
}

export async function getPermissionTemplates(): Promise<{ templates: PermissionTemplate[]; error?: string | null }> {
  try {
    const client = getApiClient();
    const result = await client.request<PermissionTemplatesResponse>({
      method: 'GET',
      path: '/v1/permissions/templates',
      requiresAuth: true,
    });

    if (!result.ok) return { templates: [], error: result.error.message };

    return {
      templates: (result.data ?? []).map(normalizeTemplate),
      error: null,
    };
  } catch (error) {
    return { templates: [], error: error instanceof Error ? error.message : 'Permission templates could not be loaded.' };
  }
}
