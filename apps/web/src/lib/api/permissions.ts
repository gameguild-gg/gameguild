'use server';

import {
  getApiAdminPermissionsUsersByUserIdPermissions,
  postApiAdminPermissionsUsersByUserIdPermissions,
  deleteApiAdminPermissionsUsersByUserIdPermissions,
  postApiAdminPermissionsUsersByUserIdRoles,
  deleteApiAdminPermissionsUsersByUserIdRolesByRoleName,
  getApiAdminPermissionsUsersByUserIdCheck
} from '@/lib/api/generated/sdk.gen';
import type { UserPermission } from '@/lib/api/generated/types.gen';

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
    const response = await getApiAdminPermissionsUsersByUserIdPermissions({
      path: { userId },
      query: { tenantId },
    });

    if (response.error) {
      console.error('Permission API error:', response.error);
      throw new Error(`Failed to fetch user permissions: ${response.error}`);
    }

    const userPermissions = response.data as UserPermission[];
    // Convert UserPermission objects to PermissionType strings
    const permissions: PermissionType[] = userPermissions.map(p => p.action || 'Read').filter(action => 
      ['Create', 'Read', 'Edit', 'Delete', 'Publish', 'Approve', 'Review', 'Comment', 'Vote', 'Share', 'Follow', 'Bookmark'].includes(action)
    ) as PermissionType[];
    
    return {
      userId,
      tenantId,
      permissions,
      isGlobalAdmin: false, // This would need to be determined from the actual permission structure
      isTenantAdmin: false, // This would need to be determined from the actual permission structure
    };
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
    const response = await postApiAdminPermissionsUsersByUserIdRoles({
      path: { userId },
      body: {
        roleName: 'GlobalAdmin',
      },
    });

    if (response.error) {
      throw new Error(`Failed to make user global admin: ${response.error}`);
    }
  } catch (error) {
    console.error('Error making user global admin:', error);
    throw error;
  }
}

/**
 * Revoke global admin permissions from a user
 */
export async function removeUserGlobalAdmin(userId: string, reason?: string): Promise<void> {
  try {
    const response = await deleteApiAdminPermissionsUsersByUserIdRolesByRoleName({
      path: { userId, roleName: 'GlobalAdmin' },
    });

    if (response.error) {
      throw new Error(`Failed to remove user global admin: ${response.error}`);
    }
  } catch (error) {
    console.error('Error removing user global admin:', error);
    throw error;
  }
}

/**
 * Grant specific permissions to a user
 */
export async function grantUserPermissions(request: GrantPermissionsRequest): Promise<void> {
  try {
    const response = await postApiAdminPermissionsUsersByUserIdPermissions({
       path: { userId: request.userId },
       body: {
         tenantId: request.tenantId,
         action: request.permissions.join(','), // Convert permissions array to action string
         resourceType: 'User',
       },
     });

    if (response.error) {
      throw new Error(`Failed to grant permissions: ${response.error}`);
    }
  } catch (error) {
    console.error('Error granting permissions:', error);
    throw error;
  }
}

/**
 * Revoke specific permissions from a user
 */
export async function revokeUserPermissions(request: RevokePermissionsRequest): Promise<void> {
  try {
    const response = await deleteApiAdminPermissionsUsersByUserIdPermissions({
       path: { userId: request.userId },
       body: {
         tenantId: request.tenantId,
         action: request.permissions.join(','), // Convert permissions array to action string
         resourceType: 'User',
       },
     });

    if (response.error) {
      throw new Error(`Failed to revoke permissions: ${response.error}`);
    }
  } catch (error) {
    console.error('Error revoking permissions:', error);
    throw error;
  }
}

/**
 * Check if user has specific permission
 */
export async function checkUserPermission(userId: string, permission: PermissionType, tenantId?: string): Promise<boolean> {
  try {
    const response = await getApiAdminPermissionsUsersByUserIdCheck({
       path: { userId },
       query: { action: permission, tenantId },
     });

    if (response.error) {
      throw new Error(`Failed to check user permission: ${response.error}`);
    }

    return response.data as boolean;
  } catch (error) {
    console.error('Error checking user permission:', error);
    throw error;
  }
}

/**
 * Get all users with admin permissions
 */
export async function getAdminUsers(): Promise<UserPermissions[]> {
  try {
    // This function needs to be implemented with the correct API endpoint
    // For now, return empty array as the current API structure doesn't have a direct admin users endpoint
    return [];
  } catch (error) {
    console.error('Error fetching admin users:', error);
    throw error;
  }
}
