'use server';

import { getToken } from '@/auth';
import { createServerClient, GeneratedApi } from '@game-guild/client';
import { revalidatePath } from 'next/cache';
import { redirect } from 'next/navigation';

const DASHBOARD_ROLES_PATH = '/dashboard/platform/roles';
const DASHBOARD_USERS_PATH = '/dashboard/community/members/users';

function buildRolesHref(params: { message?: string; error?: string }) {
  const searchParams = new URLSearchParams();
  if (params.message) searchParams.set('message', params.message);
  if (params.error) searchParams.set('error', params.error);

  const suffix = searchParams.toString();
  return `${DASHBOARD_ROLES_PATH}${suffix ? `?${suffix}` : ''}`;
}

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

function isValidEmail(value: string) {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
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
    redirect(buildRolesHref({ error: 'User, tenant, and role are required to update access.' }));
  }

  const client = createClient();
  const result = await client.request<{ success?: boolean; message?: string | null }>({
    method: 'PATCH',
    path: `/v1/users/${userId}/memberships/${tenantId}/role`,
    body: { role },
    requiresAuth: true,
  });

  if (!result.ok) {
    redirect(buildRolesHref({ error: result.error.message }));
  }

  if (result.data?.success === false) {
    redirect(buildRolesHref({ error: result.data.message ?? 'Role update was rejected.' }));
  }

  revalidatePath('/dashboard');
  revalidatePath('/dashboard/community');
  revalidatePath(DASHBOARD_USERS_PATH);
  revalidatePath(DASHBOARD_ROLES_PATH);
  redirect(buildRolesHref({ message: `Updated member role to ${role}.` }));
}

export async function invitePlatformUser(formData: FormData) {
  const email = readRequired(formData, 'email');
  const name = readRequired(formData, 'name') || email.split('@')[0] || 'Invited user';
  const tenantId = readRequired(formData, 'tenantId');
  const role = readRequired(formData, 'role') || 'Member';
  const invitedByEmail = readRequired(formData, 'invitedByEmail');

  if (!isValidEmail(email) || !tenantId) {
    redirect(buildUsersHref({ error: 'A valid email and workspace are required to invite a user.' }));
  }

  const client = createClient();
  const users = new GeneratedApi.UsersModule(client);
  const createResult = await users.postUsers({
    email,
    name,
    phoneNumber: null,
  });

  if (!createResult.ok) {
    redirect(buildUsersHref({ error: createResult.error.message }));
  }

  const userId = createResult.data.id;
  if (!userId) {
    redirect(buildUsersHref({ error: 'The user was created but the API did not return a user id.' }));
  }

  const membershipResult = await client.request<{ success?: boolean; message?: string | null }>({
    method: 'POST',
    path: `/v1/users/${userId}/memberships`,
    requiresAuth: true,
    body: {
      tenantId,
      role,
      invitedByEmail: invitedByEmail || null,
      requiresAcceptance: true,
      inviteeEmail: email,
      inviteeName: name,
    },
  });

  if (!membershipResult.ok) {
    redirect(buildUsersHref({ error: membershipResult.error.message }));
  }

  if (membershipResult.data?.success === false) {
    redirect(buildUsersHref({ error: membershipResult.data.message ?? 'The user was created, but workspace access was rejected.' }));
  }

  revalidatePath('/dashboard');
  revalidatePath('/dashboard/community');
  revalidatePath(DASHBOARD_USERS_PATH);
  revalidatePath(DASHBOARD_ROLES_PATH);
  redirect(buildUsersHref({ message: `Invited ${name} as ${role}.` }));
}

async function updateInvite(formData: FormData, action: 'resend' | 'cancel' | 'accept', message: string) {
  const userId = readRequired(formData, 'userId');
  const tenantId = readRequired(formData, 'tenantId');

  if (!userId || !tenantId) {
    redirect(buildUsersHref({ error: 'User and workspace are required to update an invite.' }));
  }

  const client = createClient();
  const result = await client.request<{ success?: boolean; message?: string | null }>({
    method: 'POST',
    path: `/v1/users/${userId}/memberships/${tenantId}/invite:${action}`,
    body: {},
    requiresAuth: true,
  });

  if (!result.ok) {
    redirect(buildUsersHref({ error: result.error.message }));
  }

  if (result.data?.success === false) {
    redirect(buildUsersHref({ error: result.data.message ?? 'Invite update was rejected.' }));
  }

  revalidatePath('/dashboard');
  revalidatePath('/dashboard/community');
  revalidatePath(DASHBOARD_USERS_PATH);
  revalidatePath(DASHBOARD_ROLES_PATH);
  redirect(buildUsersHref({ message }));
}

export async function resendPlatformInvite(formData: FormData) {
  await updateInvite(formData, 'resend', 'Invite resent.');
}

export async function cancelPlatformInvite(formData: FormData) {
  await updateInvite(formData, 'cancel', 'Invite cancelled.');
}

export async function acceptPlatformInvite(formData: FormData) {
  await updateInvite(formData, 'accept', 'Invite accepted.');
}
