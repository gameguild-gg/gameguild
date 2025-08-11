'use server';

import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import { client } from '@/lib/api/generated/client.gen';

export type PermissionType = 'Create' | 'Read' | 'Edit' | 'Delete' | 'Publish' | 'Approve' | 'Review' | 'Comment' | 'Vote' | 'Share' | 'Follow' | 'Bookmark';

export interface UserPermissions {
  userId: string;
  tenantId?: string;
  permissions: PermissionType[];
  isGlobalAdmin: boolean;
  isTenantAdmin: boolean;
}

export interface GrantPermissionsRequest {
  userId: string;
  tenantId?: string;
  permissions: PermissionType[];
  reason?: string;
}

export interface RevokePermissionsRequest {
  userId: string;
  tenantId?: string;
  permissions: PermissionType[];
  reason?: string;
}

/**
 * Get all permissions for a user
 */
export async function getUserPermissions(userId: string, tenantId?: string): Promise<UserPermissions> {
  try {
    await configureAuthenticatedClient();

    const response = await client.get({
      url: `/api/permissions/users/${userId}`,
      query: { tenantId },
    });

    if (response.error) {
      console.error('Permission API error:', response.error);
      throw new Error(`Failed to fetch user permissions: ${response.error}`);
    }

    return response.data as UserPermissions;
  } catch (error) {
    console.error('Error fetching user permissions:', error);
    // Return default permissions instead of throwing
    return {
      userId,
      tenantId,
      permissions: ['Read'],
      isGlobalAdmin: false,
      isTenantAdmin: false,
    };
  }
}

/**
 * Grant global admin permissions to a user
 */
export async function makeUserGlobalAdmin(userId: string, reason?: string): Promise<void> {
  try {
    await configureAuthenticatedClient();

    const adminPermissions: PermissionType[] = ['Create', 'Read', 'Edit', 'Delete', 'Publish', 'Approve', 'Review'];

    const response = await client.post({
      url: `/api/permissions/users/${userId}/grant`,
      body: {
        permissions: adminPermissions,
        reason: reason || 'Promoted to global admin',
      },
    });

    if (response.error) {
      console.error('Admin grant API error:', response.error);
      throw new Error(`Failed to grant admin permissions: ${response.error}`);
    }
  } catch (error) {
    console.error('Error granting admin permissions:', error);
    throw new Error(`Failed to grant admin permissions: ${error instanceof Error ? error.message : 'Unknown error'}`);
  }
}

/**
 * Revoke global admin permissions from a user
 */
export async function removeUserGlobalAdmin(userId: string, reason?: string): Promise<void> {
  try {
    await configureAuthenticatedClient();

    const adminPermissions: PermissionType[] = ['Create', 'Read', 'Edit', 'Delete', 'Publish', 'Approve', 'Review'];

    const response = await client.post({
      url: `/api/permissions/users/${userId}/revoke`,
      body: {
        permissions: adminPermissions,
        reason: reason || 'Removed from global admin',
      },
    });

    if (response.error) {
      console.error('Admin revoke API error:', response.error);
      throw new Error(`Failed to revoke admin permissions: ${response.error}`);
    }
  } catch (error) {
    console.error('Error revoking admin permissions:', error);
    throw new Error(`Failed to revoke admin permissions: ${error instanceof Error ? error.message : 'Unknown error'}`);
  }
}

/**
 * Grant specific permissions to a user
 */
export async function grantUserPermissions(request: GrantPermissionsRequest): Promise<void> {
  await configureAuthenticatedClient();

  const response = await client.post({
    url: `/api/permissions/users/${request.userId}/grant`,
    body: {
      tenantId: request.tenantId,
      permissions: request.permissions,
      reason: request.reason,
    },
  });

  if (response.error) {
    throw new Error(`Failed to grant permissions: ${response.error}`);
  }
}

/**
 * Revoke specific permissions from a user
 */
export async function revokeUserPermissions(request: RevokePermissionsRequest): Promise<void> {
  await configureAuthenticatedClient();

  const response = await client.post({
    url: `/api/permissions/users/${request.userId}/revoke`,
    body: {
      tenantId: request.tenantId,
      permissions: request.permissions,
      reason: request.reason,
    },
  });

  if (response.error) {
    throw new Error(`Failed to revoke permissions: ${response.error}`);
  }
}

/**
 * Check if user has specific permission
 */
export async function checkUserPermission(userId: string, permission: PermissionType, tenantId?: string): Promise<boolean> {
  await configureAuthenticatedClient();

  const response = await client.get({
    url: `/api/permissions/users/${userId}/check`,
    query: { permission, tenantId },
  });

  if (response.error) {
    throw new Error(`Failed to check user permission: ${response.error}`);
  }

  return response.data as boolean;
}

/**
 * Get all users with admin permissions
 */
export async function getAdminUsers(): Promise<UserPermissions[]> {
  await configureAuthenticatedClient();

  const response = await client.get({
    url: '/api/permissions/admins',
  });

  if (response.error) {
    throw new Error(`Failed to fetch admin users: ${response.error}`);
  }

  return response.data as UserPermissions[];
}
