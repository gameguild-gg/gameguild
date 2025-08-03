'use server';

import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import { getApiTenantDomainsUserGroups, getApiTenantDomainsUsersByUserIdGroups, postApiTenantDomainsUserGroupsMemberships, deleteApiTenantDomainsUserGroupsMemberships, getApiUsersById } from '@/lib/api/generated/sdk.gen';
import type { TenantUserGroupDto, UserResponseDto } from '@/lib/api/generated/types.gen';
import { revalidatePath } from 'next/cache';

export async function getUserGroups(): Promise<TenantUserGroupDto[]> {
  try {
    await configureAuthenticatedClient();
    const response = await getApiTenantDomainsUserGroups();

    return response.data || [];
  } catch (error) {
    console.error('Failed to fetch user groups:', error);
    return [];
  }
}

export async function getUserGroupMemberships(userId: string): Promise<TenantUserGroupDto[]> {
  try {
    await configureAuthenticatedClient();
    const response = await getApiTenantDomainsUsersByUserIdGroups({
      path: { userId },
    });

    return response.data || [];
  } catch (error) {
    console.error('Failed to fetch user group memberships:', error);
    return [];
  }
}

export async function addUserToGroup(userId: string, userGroupId: string): Promise<boolean> {
  try {
    await configureAuthenticatedClient();
    await postApiTenantDomainsUserGroupsMemberships({
      body: {
        userId,
        userGroupId,
      },
    });

    revalidatePath(`/users/${userId}`);
    revalidatePath('/users');
    return true;
  } catch (error) {
    console.error('Failed to add user to group:', error);
    return false;
  }
}

export async function removeUserFromGroup(userId: string, userGroupId: string): Promise<boolean> {
  try {
    await configureAuthenticatedClient();
    await deleteApiTenantDomainsUserGroupsMemberships({
      query: {
        userId,
        userGroupId,
      },
    });

    revalidatePath(`/users/${userId}`);
    revalidatePath('/users');
    return true;
  } catch (error) {
    console.error('Failed to remove user from group:', error);
    return false;
  }
}

export async function getUserWithPermissions(userId: string): Promise<UserResponseDto | null> {
  try {
    await configureAuthenticatedClient();
    const response = await getApiUsersById({
      path: { id: userId },
    });

    return response.data || null;
  } catch (error) {
    console.error('Failed to fetch user with permissions:', error);
    return null;
  }
}
