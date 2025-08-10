'use server';

import { auth } from '@/auth';
import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import { client } from '@/lib/api/generated/client.gen';
import { revalidatePath } from 'next/cache';

/**
 * Helper function to create permission templates array from permissions object
 */
function createPermissionTemplatesFromPermissions(permissions: any): Array<{action: string; resourceType: string}> {
  const templates: Array<{action: string; resourceType: string}> = [];
  
  // Sessions permissions
  if (permissions.canCreateSessions) templates.push({ action: 'Create', resourceType: 'Sessions' });
  if (permissions.canEditSessions) templates.push({ action: 'Edit', resourceType: 'Sessions' });
  if (permissions.canDeleteSessions) templates.push({ action: 'Delete', resourceType: 'Sessions' });
  if (permissions.canViewSessions) templates.push({ action: 'View', resourceType: 'Sessions' });
  
  // Locations permissions
  if (permissions.canCreateLocations) templates.push({ action: 'Create', resourceType: 'Locations' });
  if (permissions.canEditLocations) templates.push({ action: 'Edit', resourceType: 'Locations' });
  if (permissions.canDeleteLocations) templates.push({ action: 'Delete', resourceType: 'Locations' });
  if (permissions.canViewLocations) templates.push({ action: 'View', resourceType: 'Locations' });
  
  // Feedback permissions
  if (permissions.canCreateFeedback) templates.push({ action: 'Create', resourceType: 'Feedback' });
  if (permissions.canEditFeedback) templates.push({ action: 'Edit', resourceType: 'Feedback' });
  if (permissions.canDeleteFeedback) templates.push({ action: 'Delete', resourceType: 'Feedback' });
  if (permissions.canViewFeedback) templates.push({ action: 'View', resourceType: 'Feedback' });
  if (permissions.canModerateFeedback) templates.push({ action: 'Moderate', resourceType: 'Feedback' });
  
  // Requests permissions
  if (permissions.canCreateRequests) templates.push({ action: 'Create', resourceType: 'Requests' });
  if (permissions.canEditRequests) templates.push({ action: 'Edit', resourceType: 'Requests' });
  if (permissions.canDeleteRequests) templates.push({ action: 'Delete', resourceType: 'Requests' });
  if (permissions.canViewRequests) templates.push({ action: 'View', resourceType: 'Requests' });
  if (permissions.canApproveRequests) templates.push({ action: 'Approve', resourceType: 'Requests' });
  
  // Participants permissions
  if (permissions.canManageParticipants) templates.push({ action: 'Manage', resourceType: 'Participants' });
  if (permissions.canViewParticipants) templates.push({ action: 'View', resourceType: 'Participants' });
  
  return templates;
}

export interface RoleTemplate {
  id: string;
  name: string;
  description: string;
  isSystemRole: boolean;
  userCount?: number;
  permissions: {
    // Sessions
    canCreateSessions: boolean;
    canEditSessions: boolean;
    canDeleteSessions: boolean;
    canViewSessions: boolean;
    
    // Locations
    canCreateLocations: boolean;
    canEditLocations: boolean;
    canDeleteLocations: boolean;
    canViewLocations: boolean;
    
    // Feedback
    canCreateFeedback: boolean;
    canEditFeedback: boolean;
    canDeleteFeedback: boolean;
    canViewFeedback: boolean;
    canModerateFeedback: boolean;
    
    // Requests
    canCreateRequests: boolean;
    canEditRequests: boolean;
    canDeleteRequests: boolean;
    canViewRequests: boolean;
    canApproveRequests: boolean;
    
    // Participants
    canManageParticipants: boolean;
    canViewParticipants: boolean;
  };
  // For UI display purposes
  permissionTemplates: Array<{
    action: string;
    resourceType: string;
  }>;
}

/**
 * Convert form permissions to TestingLabPermissionsDto format expected by backend
 */
function convertFormPermissionsToBackendDto(formPermissions: any): any {
  return {
    // Sessions
    canCreateSessions: formPermissions.createSession || false,
    canEditSessions: formPermissions.editSession || false,
    canDeleteSessions: formPermissions.deleteSession || false,
    canViewSessions: formPermissions.readSession || false,
    
    // Locations
    canCreateLocations: formPermissions.createLocation || false,
    canEditLocations: formPermissions.editLocation || false,
    canDeleteLocations: formPermissions.deleteLocation || false,
    canViewLocations: formPermissions.readLocation || false,
    
    // Feedback
    canCreateFeedback: formPermissions.createFeedback || false,
    canEditFeedback: formPermissions.editFeedback || false,
    canDeleteFeedback: formPermissions.deleteFeedback || false,
    canViewFeedback: formPermissions.readFeedback || false,
    canModerateFeedback: formPermissions.moderateFeedback || false,
    
    // Requests
    canCreateRequests: formPermissions.createRequest || false,
    canEditRequests: formPermissions.editRequest || false,
    canDeleteRequests: formPermissions.deleteRequest || false,
    canViewRequests: formPermissions.readRequest || false,
    canApproveRequests: formPermissions.approveRequest || false,
    
    // Participants
    canManageParticipants: formPermissions.manageParticipant || false,
    canViewParticipants: formPermissions.readParticipant || false,
  };
}

/**
 * Server action to get all role templates
 */
export async function getTestingLabRoleTemplatesAction(): Promise<RoleTemplate[]> {
  const session = await auth();
  if (!session?.api.accessToken) {
    throw new Error('Authentication required');
  }

  // Ensure fresh data by revalidating the paths
  revalidatePath('/dashboard/testing-lab/settings');
  revalidatePath('/testing-lab/settings');
  
  try {
    await configureAuthenticatedClient();
    
    console.log('Fetching role templates from API...');
    const response = await client.get({
      url: '/api/testing-lab/permissions/role-templates',
      headers: {
        'Cache-Control': 'no-cache',
        'Pragma': 'no-cache',
      },
    });
    
    console.log('API response:', { status: response.response?.status, hasData: !!response.data, hasError: !!response.error });

    if (response.error) {
      console.error('Failed to get role templates from API:', response.error);
      const errorMessage = typeof response.error === 'object' 
        ? JSON.stringify(response.error) 
        : String(response.error);
      throw new Error(`Failed to get role templates: ${errorMessage}`);
    }

    // Convert backend format to frontend format
    const backendTemplates = response.data as any[];
    console.log('Backend templates:', backendTemplates);

    const mapTemplate = (template: any, index: number): RoleTemplate => {
      const perms = template.permissions || template.Permissions || {};
      const rawId = template.id ?? template.Id; // Backend should now always supply this (Guid)

      if (!rawId) {
        // Hard fail instead of silently falling back to name; forces backend / client alignment
        console.error('Backend role template payload missing id property; cannot safely perform CRUD.', template);
        throw new Error('Backend role template payload missing id property; please refresh after deployment.');
      }

      const guidRegex = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-5][0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$/;
      if (!guidRegex.test(rawId)) {
        console.warn('Role template id is not a GUID (unexpected). CRUD endpoints expect GUID. Value:', rawId);
      }

      return {
        id: rawId,
        name: template.name,
        description: template.description,
        isSystemRole: template.isSystemRole,
        userCount: 0,
        permissions: {
          canCreateSessions: !!(perms.canCreateSessions ?? perms.CanCreateSessions),
          canEditSessions: !!(perms.canEditSessions ?? perms.CanEditSessions),
          canDeleteSessions: !!(perms.canDeleteSessions ?? perms.CanDeleteSessions),
          canViewSessions: !!(perms.canViewSessions ?? perms.CanViewSessions),
          canCreateLocations: !!(perms.canCreateLocations ?? perms.CanCreateLocations),
          canEditLocations: !!(perms.canEditLocations ?? perms.CanEditLocations),
          canDeleteLocations: !!(perms.canDeleteLocations ?? perms.CanDeleteLocations),
            canViewLocations: !!(perms.canViewLocations ?? perms.CanViewLocations),
          canCreateFeedback: !!(perms.canCreateFeedback ?? perms.CanCreateFeedback),
          canEditFeedback: !!(perms.canEditFeedback ?? perms.CanEditFeedback),
          canDeleteFeedback: !!(perms.canDeleteFeedback ?? perms.CanDeleteFeedback),
          canViewFeedback: !!(perms.canViewFeedback ?? perms.CanViewFeedback),
          canModerateFeedback: !!(perms.canModerateFeedback ?? perms.CanModerateFeedback),
          canCreateRequests: !!(perms.canCreateRequests ?? perms.CanCreateRequests),
          canEditRequests: !!(perms.canEditRequests ?? perms.CanEditRequests),
          canDeleteRequests: !!(perms.canDeleteRequests ?? perms.CanDeleteRequests),
          canViewRequests: !!(perms.canViewRequests ?? perms.CanViewRequests),
          canApproveRequests: !!(perms.canApproveRequests ?? perms.CanApproveRequests),
          canManageParticipants: !!(perms.canManageParticipants ?? perms.CanManageParticipants),
          canViewParticipants: !!(perms.canViewParticipants ?? perms.CanViewParticipants),
        },
        permissionTemplates: createPermissionTemplatesFromPermissions(perms)
      };
    };

    return backendTemplates.map(mapTemplate);
  } catch (error) {
    console.error('Error getting role templates:', error);
    if (error instanceof Error) {
      if (error.message.includes('Unauthorized')) {
        throw new Error('You do not have permission to access role templates. Please contact your administrator.');
      }
      throw error;
    }
    throw new Error('An unexpected error occurred while getting role templates');
  }
}

/**
 * Server action to create a new role template
 */
export async function createRoleTemplateAction(roleData: {
  name: string;
  description: string;
  permissions: any;
}): Promise<RoleTemplate> {
  const session = await auth();
  if (!session?.api.accessToken) {
    throw new Error('Authentication required');
  }
  
  try {
    await configureAuthenticatedClient();

    // Convert form permissions to the format expected by backend
    const backendPermissions = convertFormPermissionsToBackendDto(roleData.permissions);
    
    console.log('Creating role with data:', JSON.stringify({
      name: roleData.name,
      description: roleData.description,
      permissions: backendPermissions
    }, null, 2));

    const response = await client.post({
      url: '/api/testing-lab/permissions/role-templates',
      body: {
        name: roleData.name,
        description: roleData.description,
        permissions: backendPermissions,
      },
    });

    if (response.error) {
      const errorMessage = typeof response.error === 'object' 
        ? JSON.stringify(response.error) 
        : String(response.error);
      throw new Error(`Failed to create role template: ${errorMessage}`);
    }

    // Revalidate the paths to ensure fresh data on next load
    revalidatePath('/dashboard/testing-lab/settings');
    revalidatePath('/testing-lab/settings');

    return response.data as RoleTemplate;
  } catch (error) {
    console.error('Error creating role template:', error);
    if (error instanceof Error) {
      if (error.message.includes('Unauthorized')) {
        throw new Error('You do not have permission to create role templates. Please contact your administrator.');
      }
      if (error.message.includes('400') || error.message.includes('Bad Request')) {
        throw new Error(`Invalid role template data: ${error.message}`);
      }
      if (error.message.includes('409') || error.message.includes('Conflict')) {
        throw new Error('A role template with this name already exists.');
      }
      throw new Error(`Role template creation failed: ${error.message}`);
    }
    throw new Error(`An unexpected error occurred while creating the role template: ${String(error)}`);
  }
}

/**
 * Server action to update a role template
 */
export async function updateRoleTemplateAction(roleId: string, roleData: {
  name?: string;
  description: string;
  permissions: any;
}): Promise<RoleTemplate> {
  const session = await auth();
  if (!session?.api.accessToken) {
    throw new Error('Authentication required');
  }
  
  try {
    await configureAuthenticatedClient();

    // Convert form permissions to the format expected by backend
    const backendPermissions = convertFormPermissionsToBackendDto(roleData.permissions);

    console.log('Updating role template with data:', {
      roleId,
      name: roleData.name,
      description: roleData.description,
      permissions: backendPermissions
    });

    const guidRegex = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-5][0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$/;
    if (!guidRegex.test(roleId)) {
      throw new Error('Invalid role id supplied for update (expected GUID). Refresh the page to reload proper identifiers.');
    }

    const response = await client.put({
      url: `/api/testing-lab/permissions/role-templates/${encodeURIComponent(roleId)}`,
      body: {
        ...(roleData.name && { name: roleData.name }),
        description: roleData.description,
        permissions: backendPermissions,
      },
    });

    console.log('API Response:', response);

    if (response.error) {
      const errorMessage = typeof response.error === 'object' 
        ? JSON.stringify(response.error) 
        : String(response.error);
      throw new Error(`Failed to update role template: ${errorMessage}`);
    }

    // Revalidate the paths to ensure fresh data on next load
    revalidatePath('/dashboard/settings/testing-lab');
    revalidatePath('/settings/testing-lab');
    revalidatePath('/dashboard/testing-lab/settings');
    revalidatePath('/testing-lab/settings');
    revalidatePath('/', 'layout'); // Revalidate entire layout to clear all caches
    
    console.log('Cache revalidated successfully');

    return response.data as RoleTemplate;
  } catch (error) {
    console.error('Error updating role template:', error);
    if (error instanceof Error) {
      if (error.message.includes('Unauthorized')) {
        throw new Error('You do not have permission to update role templates. Please contact your administrator.');
      }
      if (error.message.includes('404') || error.message.includes('Not Found')) {
        throw new Error('Role template not found.');
      }
      throw new Error(`Role template update failed: ${error.message}`);
    }
    throw new Error(`An unexpected error occurred while updating the role template: ${String(error)}`);
  }
}

/**
 * Server action to delete a role template
 */
export async function deleteRoleTemplateAction(roleId: string): Promise<void> {
  const session = await auth();
  if (!session?.api.accessToken) {
    throw new Error('Authentication required');
  }
  
  try {
    await configureAuthenticatedClient();

    const guidRegex = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-5][0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$/;
    if (!guidRegex.test(roleId)) {
      throw new Error('Invalid role id supplied for delete (expected GUID). Refresh the page to reload proper identifiers.');
    }

    const response = await client.delete({
      url: `/api/testing-lab/permissions/role-templates/${encodeURIComponent(roleId)}`,
    });

    if (response.error) {
      const errorMessage = typeof response.error === 'object' 
        ? JSON.stringify(response.error) 
        : String(response.error);
      throw new Error(`Failed to delete role template: ${errorMessage}`);
    }

    // Revalidate the paths to ensure fresh data on next load
    revalidatePath('/dashboard/testing-lab/settings');
    revalidatePath('/testing-lab/settings');
  } catch (error) {
    console.error('Error deleting role template:', error);
    if (error instanceof Error) {
      if (error.message.includes('Unauthorized')) {
        throw new Error('You do not have permission to delete role templates. Please contact your administrator.');
      }
      if (error.message.includes('404') || error.message.includes('Not Found')) {
        throw new Error('Role template not found.');
      }
      throw new Error(`Role template deletion failed: ${error.message}`);
    }
    throw new Error(`An unexpected error occurred while deleting the role template: ${String(error)}`);
  }
}