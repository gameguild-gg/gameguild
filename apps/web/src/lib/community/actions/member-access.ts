'use server';

import { getToken } from '@/auth';
import { createServerClient } from '@game-guild/client';
import { revalidatePath } from 'next/cache';
import { redirect } from 'next/navigation';

const DASHBOARD_USERS_PATH = '/dashboard/community/members/users';

function buildUsersHref(params: { message?: string; error?: string }) {
  const searchParams = new URLSearchParams();
  if (params.message) searchParams.set('message', params.message);
  if (params.error) searchParams.set('error', params.error);

  const suffix = searchParams.toString();
  return `${DASHBOARD_USERS_PATH}${suffix ? `?${suffix}` : ''}`;
}

function readRequired(formData: FormData, key: string) {
  const value = formData.get(key);
  return typeof value === 'string' ? value.trim() : '';
}

function createClient() {
  const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5295';
  return createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
  });
}

export async function updateMemberAccessRole(formData: FormData) {
  const userId = readRequired(formData, 'userId');
  const tenantId = readRequired(formData, 'tenantId');
  const role = readRequired(formData, 'role');

  if (!userId || !tenantId || !role) {
    redirect(buildUsersHref({ error: 'User, tenant, and role are required to update access.' }));
  }

  const client = createClient();
  const result = await client.request<{ success?: boolean; message?: string | null }>({
    method: 'PATCH',
    path: `/v1/users/${userId}/memberships/${tenantId}/role`,
    body: { role },
    requiresAuth: true,
  });

  if (!result.ok) {
    redirect(buildUsersHref({ error: result.error.message }));
  }

  if (result.data?.success === false) {
    redirect(buildUsersHref({ error: result.data.message ?? 'Role update was rejected.' }));
  }

  revalidatePath('/dashboard');
  revalidatePath('/dashboard/community');
  revalidatePath(DASHBOARD_USERS_PATH);
  redirect(buildUsersHref({ message: `Updated member role to ${role}.` }));
}
