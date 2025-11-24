'use server';

import { auth } from '@/auth';
import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import {
  deleteApiModulePermissionsRevokeRole,
  postApiModulePermissionsAssignRole
} from '@/lib/api/generated/sdk.gen';
import { getUsers } from '@/lib/api/users';
import { revalidatePath } from 'next/cache';

export interface UserRoleAssignment {
  id: string;
  userId: string;
  userEmail?: string;
  userName?: string;
  roleTemplateId?: string;
  roleName?: string;
  assignedAt?: string;
  isActive: boolean;
  module: string;
}

/**
 * Server action to get all testing lab user role assignments
 */
export async function getTestingLabUserRoleAssignmentsAction(): Promise<UserRoleAssignment[]> {
  const session = await auth();
  if (!session?.api.accessToken) {
    throw new Error('Authentication required');
  }

  // Ensure fresh data by revalidating
  revalidatePath('/testing-lab-settings');

  try {
    // Ensure client configured for downstream getUsers API call
    await configureAuthenticatedClient();

    const allUsers = await getUsers();

    // For now, return mock data since the backend endpoint structure is complex
    // TODO: Implement proper API calls when backend supports simplified role assignments
    console.log('Getting user role assignments - using mock data for now');

    return [
      {
        id: '1',
        userId: allUsers[0]?.id || 'user1',
        userEmail: allUsers[0]?.email || 'admin@example.com',
        userName: allUsers[0]?.name || 'Admin User',
        roleTemplateId: 'TestingLabAdmin',
        roleName: 'TestingLabAdmin',
        assignedAt: new Date().toISOString(),
        isActive: true,
        module: 'TestingLab'
      }
    ];
  } catch (error) {
    console.error('Error getting user role assignments:', error);
    if (error instanceof Error) {
      if (error.message.includes('Unauthorized')) {
        throw new Error('You do not have permission to access user role assignments. Please contact your administrator.');
      }
      throw error;
    }
    throw new Error('An unexpected error occurred while getting user role assignments');
  }
}

/**
 * Server action to remove a role from a user
 */
export async function removeUserRoleAction(userId: string, roleName: string): Promise<void> {
  const session = await auth();
  if (!session?.api.accessToken) {
    throw new Error('Authentication required');
  }
  await configureAuthenticatedClient();

  try {
    const response = await deleteApiModulePermissionsRevokeRole({
      body: {
        userId: userId,
        tenantId: session.currentTenant?.id,
        module: 1, // TestingLab module
        roleName: roleName
      }
    });

    if (response.error) {
      const errorMessage = typeof response.error === 'object'
        ? JSON.stringify(response.error)
        : String(response.error);
      throw new Error(`Failed to remove role: ${errorMessage}`);
    }

    // Revalidate the path to ensure fresh data on next load
    revalidatePath('/testing-lab-settings');

  } catch (error) {
    console.error('Error removing user role:', error);
    if (error instanceof Error) {
      if (error.message.includes('Unauthorized')) {
        throw new Error('You do not have permission to remove roles. Please contact your administrator.');
      }
      if (error.message.includes('404') || error.message.includes('Not Found')) {
        throw new Error('Role assignment not found or already removed.');
      }
      throw new Error(`Role removal failed: ${error.message}`);
    }
    throw new Error(`An unexpected error occurred while removing the role: ${String(error)}`);
  }
}

/**
 * Server action to assign a role to a user
 */
export async function assignUserRoleAction(assignment: {
  userId: string;
  roleName: string;
  userEmail?: string;
}): Promise<UserRoleAssignment> {
  const session = await auth();
  if (!session?.api.accessToken) {
    throw new Error('Authentication required');
  }
  await configureAuthenticatedClient();

  try {
    const response = await postApiModulePermissionsAssignRole({
      body: {
        userId: assignment.userId,
        tenantId: session.currentTenant?.id,
        module: 1, // TestingLab module
        roleName: assignment.roleName,
        constraints: null,
        expiresAt: null
      }
    });

    if (response.error) {
      const errorMessage = typeof response.error === 'object'
        ? JSON.stringify(response.error)
        : String(response.error);
      throw new Error(`Failed to assign role: ${errorMessage}`);
    }

    // Revalidate the path to ensure fresh data on next load
    revalidatePath('/testing-lab-settings');

    return {
      id: Date.now().toString(),
      userId: assignment.userId,
      userEmail: assignment.userEmail,
      userName: assignment.userEmail,
      roleTemplateId: assignment.roleName,
      roleName: assignment.roleName,
      assignedAt: new Date().toISOString(),
      isActive: true,
      module: 'TestingLab'
    };
  } catch (error) {
    console.error('Error assigning user role:', error);
    if (error instanceof Error) {
      if (error.message.includes('Unauthorized')) {
        throw new Error('You do not have permission to assign roles. Please contact your administrator.');
      }
      if (error.message.includes('400') || error.message.includes('Bad Request')) {
        throw new Error(`Invalid role assignment data: ${error.message}`);
      }
      if (error.message.includes('409') || error.message.includes('Conflict')) {
        throw new Error('This role is already assigned to the user.');
      }
      throw new Error(`Role assignment failed: ${error.message}`);
    }
    throw new Error(`An unexpected error occurred while assigning the role: ${String(error)}`);
  }
}
