'use server';

import { getToken } from '@/auth';
import { createServerClient } from '@game-guild/client';
import { revalidatePath } from 'next/cache';
import { redirect } from 'next/navigation';

const DASHBOARD_ROLES_PATH = '/console/platform/roles';

function buildRolesHref(params: { message?: string; error?: string }) {
  const searchParams = new URLSearchParams();
  if (params.message) searchParams.set('message', params.message);
  if (params.error) searchParams.set('error', params.error);

  const suffix = searchParams.toString();
  return `${DASHBOARD_ROLES_PATH}${suffix ? `?${suffix}` : ''}`;
}

function readText(formData: FormData, key: string) {
  const value = formData.get(key);
  return typeof value === 'string' ? value.trim() : '';
}

function readPermissions(formData: FormData) {
  return formData
    .getAll('permissions')
    .map((value) => (typeof value === 'string' ? value.trim() : ''))
    .filter((value, index, array) => value.length > 0 && array.indexOf(value) === index);
}

function createClient() {
  const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080';
  return createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
  });
}

function revalidateRoles() {
  revalidatePath('/dashboard');
  revalidatePath(DASHBOARD_ROLES_PATH);
}

export async function createPlatformRole(formData: FormData) {
  const name = readText(formData, 'name');
  const description = readText(formData, 'description');
  const tenantId = readText(formData, 'tenantId') || null;
  const permissions = readPermissions(formData);

  if (!name) {
    redirect(buildRolesHref({ error: 'Role name is required.' }));
  }

  const client = createClient();
  const result = await client.request({
    method: 'POST',
    path: '/v1/roles',
    body: {
      name,
      description,
      permissions,
      tenantId,
    },
    requiresAuth: true,
  });

  if (!result.ok) {
    redirect(buildRolesHref({ error: result.error.message }));
  }

  revalidateRoles();
  redirect(buildRolesHref({ message: `Created role ${name}.` }));
}

export async function updatePlatformRole(formData: FormData) {
  const roleId = readText(formData, 'roleId');
  const name = readText(formData, 'name');
  const description = readText(formData, 'description');
  const permissions = readPermissions(formData);
  const isActive = formData.get('isActive') === 'on';

  if (!roleId || !name) {
    redirect(buildRolesHref({ error: 'Role id and name are required.' }));
  }

  const client = createClient();
  const result = await client.request({
    method: 'PUT',
    path: `/v1/roles/${roleId}`,
    body: {
      name,
      description,
      permissions,
      isActive,
    },
    requiresAuth: true,
  });

  if (!result.ok) {
    redirect(buildRolesHref({ error: result.error.message }));
  }

  revalidateRoles();
  redirect(buildRolesHref({ message: `Updated role ${name}.` }));
}

export async function deletePlatformRole(formData: FormData) {
  const roleId = readText(formData, 'roleId');
  const name = readText(formData, 'name') || 'role';

  if (!roleId) {
    redirect(buildRolesHref({ error: 'Role id is required.' }));
  }

  const client = createClient();
  const result = await client.request({
    method: 'DELETE',
    path: `/v1/roles/${roleId}`,
    requiresAuth: true,
  });

  if (!result.ok) {
    redirect(buildRolesHref({ error: result.error.message }));
  }

  revalidateRoles();
  redirect(buildRolesHref({ message: `Deleted role ${name}.` }));
}

export async function assignPlatformRole(formData: FormData) {
  const userId = readText(formData, 'userId');
  const roleId = readText(formData, 'roleId');
  const roleName = readText(formData, 'roleName') || 'custom role';

  if (!userId || !roleId) {
    redirect(buildRolesHref({ error: 'User and role are required.' }));
  }

  const client = createClient();
  const result = await client.request({
    method: 'POST',
    path: '/v1/roles/:assign',
    body: {
      userId,
      roleId,
      expiresAt: null,
    },
    requiresAuth: true,
  });

  if (!result.ok) {
    redirect(buildRolesHref({ error: result.error.message }));
  }

  revalidateRoles();
  redirect(buildRolesHref({ message: `Assigned ${roleName}.` }));
}

export async function removePlatformRole(formData: FormData) {
  const userId = readText(formData, 'userId');
  const roleId = readText(formData, 'roleId');
  const roleName = readText(formData, 'roleName') || 'custom role';

  if (!userId || !roleId) {
    redirect(buildRolesHref({ error: 'User and role are required.' }));
  }

  const client = createClient();
  const result = await client.request({
    method: 'POST',
    path: '/v1/roles/:remove',
    body: {
      userId,
      roleId,
    },
    requiresAuth: true,
  });

  if (!result.ok) {
    redirect(buildRolesHref({ error: result.error.message }));
  }

  revalidateRoles();
  redirect(buildRolesHref({ message: `Removed ${roleName}.` }));
}
