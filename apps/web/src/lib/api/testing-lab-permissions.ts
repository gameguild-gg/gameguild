import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import { client } from '@/lib/api/generated/client.gen';
import type { 
  ModuleType, 
  ModuleAction, 
  ModuleRole, 
  ModulePermission,
  UserRoleAssignment, 
  AssignRoleRequest, 
  CreateRoleRequest, 
  PermissionConstraint,
  TestingLabPermissions
} from '@/lib/api/generated/types.gen';

// Simplified interfaces for the frontend
export interface PermissionTemplate {
  action: string;
  resourceType: string;
  constraints?: PermissionConstraint[];
}

export interface RoleTemplate {
  id: string;
  name: string;
  description: string;
  permissionTemplates: PermissionTemplate[];
  isSystemRole: boolean;
  userCount?: number;
}

export interface CreateRoleTemplateRequest {
  name: string;
  description: string;
  permissionTemplates: PermissionTemplate[];
}

export interface UpdateRoleTemplateRequest {
  description: string;
  permissionTemplates: PermissionTemplate[];
}

// Constants for Testing Lab module
const TESTING_LAB_MODULE: ModuleType = 1; // TestingLab = 1

// TestingLab Permission API
export class TestingLabPermissionAPI {
  // Role Templates (using Module Role endpoints)
  static async getRoleTemplates(): Promise<RoleTemplate[]> {
    try {
      await configureAuthenticatedClient();

      const response = await client.get({
        url: '/testing-lab/permissions/role-templates',
      });

      if (response.error) {
        console.error('Failed to get role templates from API:', response.error);
        throw new Error(`Failed to get role templates: ${response.error}`);
      }

      // Convert backend format to frontend format
      const backendTemplates = response.data as any[];
      return backendTemplates.map(template => ({
        id: template.name, // Use name as ID for now
        name: template.name,
        description: template.description,
        isSystemRole: template.isSystemRole,
        userCount: 0, // TODO: Get actual user count from API
        permissionTemplates: this.convertBackendPermissionsToFrontend(template.permissions)
      }));
    } catch (error) {
      console.error('Failed to get role templates, falling back to mock data:', error);
      return this.getMockRoleTemplates();
    }
  }

  // Convert backend permissions format to frontend format
  private static convertBackendPermissionsToFrontend(backendPermissions: any): PermissionTemplate[] {
    const permissions: PermissionTemplate[] = [];
    
    if (backendPermissions?.canCreateSessions) permissions.push({ action: 'create', resourceType: 'TestingSession' });
    if (backendPermissions?.canEditSessions) permissions.push({ action: 'edit', resourceType: 'TestingSession' });
    if (backendPermissions?.canDeleteSessions) permissions.push({ action: 'delete', resourceType: 'TestingSession' });
    if (backendPermissions?.canViewSessions) permissions.push({ action: 'read', resourceType: 'TestingSession' });
    
    if (backendPermissions?.canCreateLocations) permissions.push({ action: 'create', resourceType: 'TestingLocation' });
    if (backendPermissions?.canEditLocations) permissions.push({ action: 'edit', resourceType: 'TestingLocation' });
    if (backendPermissions?.canDeleteLocations) permissions.push({ action: 'delete', resourceType: 'TestingLocation' });
    if (backendPermissions?.canViewLocations) permissions.push({ action: 'read', resourceType: 'TestingLocation' });
    
    if (backendPermissions?.canCreateFeedback) permissions.push({ action: 'create', resourceType: 'TestingFeedback' });
    if (backendPermissions?.canEditFeedback) permissions.push({ action: 'edit', resourceType: 'TestingFeedback' });
    if (backendPermissions?.canDeleteFeedback) permissions.push({ action: 'delete', resourceType: 'TestingFeedback' });
    if (backendPermissions?.canViewFeedback) permissions.push({ action: 'read', resourceType: 'TestingFeedback' });
    if (backendPermissions?.canModerateFeedback) permissions.push({ action: 'moderate', resourceType: 'TestingFeedback' });
    
    if (backendPermissions?.canCreateRequests) permissions.push({ action: 'create', resourceType: 'TestingRequest' });
    if (backendPermissions?.canEditRequests) permissions.push({ action: 'edit', resourceType: 'TestingRequest' });
    if (backendPermissions?.canDeleteRequests) permissions.push({ action: 'delete', resourceType: 'TestingRequest' });
    if (backendPermissions?.canViewRequests) permissions.push({ action: 'read', resourceType: 'TestingRequest' });
    if (backendPermissions?.canApproveRequests) permissions.push({ action: 'approve', resourceType: 'TestingRequest' });
    
    if (backendPermissions?.canManageParticipants) permissions.push({ action: 'manage', resourceType: 'TestingParticipant' });
    if (backendPermissions?.canViewParticipants) permissions.push({ action: 'read', resourceType: 'TestingParticipant' });
    
    return permissions;
  }

  static async createRoleTemplate(request: CreateRoleTemplateRequest): Promise<RoleTemplate> {
    await configureAuthenticatedClient();
    
    try {
      const createRequest: CreateRoleRequest = {
        roleName: request.name,
        description: request.description,
        permissions: request.permissionTemplates.map(this.convertPermissionTemplateToModulePermission),
        priority: 0
      };

      const response = await client.post({
        url: '/module-permissions/modules/${TESTING_LAB_MODULE}/roles',
        body: createRequest
      });

      if (response.data) {
        return this.convertModuleRoleToRoleTemplate(response.data);
      }
      throw new Error('Failed to create role template');
    } catch (error) {
      console.error('Failed to create role template:', error);
      throw error;
    }
  }

  static async updateRoleTemplate(name: string, request: UpdateRoleTemplateRequest): Promise<RoleTemplate> {
    await configureAuthenticatedClient();
    
    try {
      const updateRequest = {
        permissions: request.permissionTemplates.map(this.convertPermissionTemplateToModulePermission)
      };

      const response = await client.PUT('/api/module-permissions/modules/{module}/roles/{roleName}', {
        params: {
          path: {
            module: TESTING_LAB_MODULE,
            roleName: name
          }
        },
        body: updateRequest
      });

      if (response.data) {
        return this.convertModuleRoleToRoleTemplate(response.data);
      }
      throw new Error('Failed to update role template');
    } catch (error) {
      console.error('Failed to update role template:', error);
      throw error;
    }
  }

  static async deleteRoleTemplate(name: string): Promise<void> {
    await configureAuthenticatedClient();
    
    try {
      await client.DELETE('/api/module-permissions/modules/{module}/roles/{roleName}', {
        params: {
          path: {
            module: TESTING_LAB_MODULE,
            roleName: name
          }
        }
      });
    } catch (error) {
      console.error('Failed to delete role template:', error);
      throw error;
    }
  }

  // User Role Management
  static async getUserRoles(userId: string, tenantId?: string): Promise<UserRoleAssignment[]> {
    await configureAuthenticatedClient();
    
    try {
      const response = await client.GET('/api/module-permissions/users/{userId}/roles', {
        params: {
          path: {
            userId: userId
          },
          query: {
            module: TESTING_LAB_MODULE,
            tenantId: tenantId
          }
        }
      });
      return response.data || [];
    } catch (error) {
      console.error('Failed to get user roles:', error);
      return [];
    }
  }

  static async assignRoleToUser(userId: string, roleName: string, tenantId?: string): Promise<UserRoleAssignment> {
    await configureAuthenticatedClient();
    
    try {
      const request: AssignRoleRequest = {
        userId: userId,
        tenantId: tenantId,
        module: TESTING_LAB_MODULE,
        roleName: roleName,
        constraints: null,
        expiresAt: null
      };

      const response = await client.POST('/api/module-permissions/assign-role', {
        body: request
      });

      if (response.data) {
        return response.data;
      }
      throw new Error('Failed to assign role');
    } catch (error) {
      console.error('Failed to assign role to user:', error);
      throw error;
    }
  }

  static async assignLocationSpecificRole(
    userId: string, 
    roleName: string, 
    locationId: string, 
    tenantId?: string
  ): Promise<UserRoleAssignment> {
    await configureAuthenticatedClient();
    
    try {
      // Create constraints for location-specific role assignment
      const locationConstraint: PermissionConstraint = {
        type: 'Location',
        value: locationId,
        expiresAt: null
      };

      const request: AssignRoleRequest = {
        userId: userId,
        tenantId: tenantId,
        module: TESTING_LAB_MODULE,
        roleName: roleName,
        constraints: [locationConstraint],
        expiresAt: null
      };

      const response = await client.POST('/api/module-permissions/assign-role', {
        body: request
      });

      if (response.data) {
        return response.data;
      }
      throw new Error('Failed to assign location-specific role');
    } catch (error) {
      console.error('Failed to assign location-specific role to user:', error);
      throw error;
    }
  }

  static async removeUserRole(userId: string, roleName: string, tenantId?: string): Promise<void> {
    await configureAuthenticatedClient();
    
    try {
      const request = {
        userId: userId,
        tenantId: tenantId,
        module: TESTING_LAB_MODULE,
        roleName: roleName
      };

      await client.DELETE('/api/module-permissions/revoke-role', {
        body: request
      });
    } catch (error) {
      console.error('Failed to remove user role:', error);
      throw error;
    }
  }

  // Testing Lab Specific Permissions
  static async getMyTestingLabPermissions(tenantId?: string): Promise<TestingLabPermissions> {
    await configureAuthenticatedClient();
    
    try {
      const response = await client.GET('/api/module-permissions/testing-lab/my-permissions', {
        params: {
          query: {
            tenantId: tenantId
          }
        }
      });
      return response.data || {};
    } catch (error) {
      console.error('Failed to get my testing lab permissions:', error);
      return {};
    }
  }

  static async getUserTestingLabPermissions(userId: string, tenantId?: string): Promise<TestingLabPermissions> {
    await configureAuthenticatedClient();
    
    try {
      const response = await client.GET('/api/module-permissions/testing-lab/users/{userId}/permissions', {
        params: {
          path: {
            userId: userId
          },
          query: {
            tenantId: tenantId
          }
        }
      });
      return response.data || {};
    } catch (error) {
      console.error('Failed to get user testing lab permissions:', error);
      return {};
    }
  }

  // Permission checks
  static async canCreateTestingSessions(tenantId?: string): Promise<boolean> {
    await configureAuthenticatedClient();
    
    try {
      const response = await client.GET('/api/module-permissions/testing-lab/can-create-sessions', {
        params: {
          query: {
            tenantId: tenantId
          }
        }
      });
      return response.data || false;
    } catch (error) {
      console.error('Failed to check create sessions permission:', error);
      return false;
    }
  }

  static async canDeleteTestingSessions(tenantId?: string): Promise<boolean> {
    await configureAuthenticatedClient();
    
    try {
      const response = await client.GET('/api/module-permissions/testing-lab/can-delete-sessions', {
        params: {
          query: {
            tenantId: tenantId
          }
        }
      });
      return response.data || false;
    } catch (error) {
      console.error('Failed to check delete sessions permission:', error);
      return false;
    }
  }

  static async canManageTesters(tenantId?: string): Promise<boolean> {
    await configureAuthenticatedClient();
    
    try {
      const response = await client.GET('/api/module-permissions/testing-lab/can-manage-testers', {
        params: {
          query: {
            tenantId: tenantId
          }
        }
      });
      return response.data || false;
    } catch (error) {
      console.error('Failed to check manage testers permission:', error);
      return false;
    }
  }

  static async canViewTestingReports(tenantId?: string): Promise<boolean> {
    await configureAuthenticatedClient();
    
    try {
      const response = await client.GET('/api/module-permissions/testing-lab/can-view-reports', {
        params: {
          query: {
            tenantId: tenantId
          }
        }
      });
      return response.data || false;
    } catch (error) {
      console.error('Failed to check view reports permission:', error);
      return false;
    }
  }

  static async canExportTestingData(tenantId?: string): Promise<boolean> {
    await configureAuthenticatedClient();
    
    try {
      const response = await client.GET('/api/module-permissions/testing-lab/can-export-data', {
        params: {
          query: {
            tenantId: tenantId
          }
        }
      });
      return response.data || false;
    } catch (error) {
      console.error('Failed to check export data permission:', error);
      return false;
    }
  }

  // Utility methods for data conversion
  private static convertModuleRoleToRoleTemplate(moduleRole: ModuleRole): RoleTemplate {
    return {
      id: moduleRole.name || '',
      name: moduleRole.name || '',
      description: moduleRole.description || '',
      permissionTemplates: (moduleRole.permissions || []).map(this.convertModulePermissionToPermissionTemplate),
      isSystemRole: moduleRole.isSystemRole || false,
      userCount: 0 // This would need to be fetched separately if needed
    };
  }

  private static convertPermissionTemplateToModulePermission(template: PermissionTemplate): ModulePermission {
    return {
      module: TESTING_LAB_MODULE,
      action: this.convertActionStringToModuleAction(template.action),
      constraints: template.constraints || null,
      isGranted: true,
      expiresAt: null
    };
  }

  private static convertModulePermissionToPermissionTemplate(permission: ModulePermission): PermissionTemplate {
    return {
      action: this.convertModuleActionToActionString(permission.action),
      resourceType: this.convertActionToResourceType(permission.action),
      constraints: permission.constraints || []
    };
  }

  private static convertActionStringToModuleAction(action: string): ModuleAction {
    const actionMap: Record<string, ModuleAction> = {
      'create': 1, // Create
      'read': 2,   // Read
      'edit': 3,   // Edit
      'delete': 4, // Delete
      'manage': 5, // Manage
      'administer': 6, // Administer
      'execute': 7, // Execute
      'review': 8, // Review
      'approve': 9, // Approve
      'publish': 10, // Publish
      'archive': 11, // Archive
      'restore': 12, // Restore
      'moderate': 8 // Map moderate to review
    };
    return actionMap[action] || 2; // Default to Read
  }

  private static convertModuleActionToActionString(action?: ModuleAction): string {
    const actionMap: Record<number, string> = {
      1: 'create',
      2: 'read',
      3: 'edit',
      4: 'delete',
      5: 'manage',
      6: 'administer',
      7: 'execute',
      8: 'review',
      9: 'approve',
      10: 'publish',
      11: 'archive',
      12: 'restore'
    };
    return actionMap[action || 2] || 'read';
  }

  private static convertActionToResourceType(action?: ModuleAction): string {
    // For Testing Lab, most actions are on these resource types
    // This is a simplified mapping - in practice, you might need more context
    return 'TestingSession'; // Default resource type
  }

  // Utility to find user by email (fallback method)
  static async findUserByEmail(email: string): Promise<{ id: string; email: string; name: string } | null> {
    try {
      await configureAuthenticatedClient();
      
      // TODO: This endpoint might not exist yet, implement when available
      const response = await client.GET('/api/users/search', {
        params: {
          query: {
            email: email
          }
        }
      });
      return response.data as { id: string; email: string; name: string };
    } catch (error) {
      console.warn('User search endpoint not available, using mock data');
      return {
        id: 'user-' + Date.now(),
        email: email,
        name: email.split('@')[0] || email
      };
    }
  }

  // Get all role assignments for TestingLab (admin view)
  static async getAllTestingLabRoleAssignments(): Promise<UserRoleAssignment[]> {
    await configureAuthenticatedClient();
    
    try {
      // Get all roles first, then get users for each role
      const roles = await this.getRoleTemplates();
      const allAssignments: UserRoleAssignment[] = [];
      
      for (const role of roles) {
        try {
          const response = await client.GET('/api/module-permissions/roles/{roleName}/users', {
            params: {
              path: {
                roleName: role.name
              },
              query: {
                module: TESTING_LAB_MODULE
              }
            }
          });
          
          if (response.data) {
            allAssignments.push(...response.data);
          }
        } catch (error) {
          console.warn(`Failed to get users for role ${role.name}:`, error);
        }
      }
      
      return allAssignments;
    } catch (error) {
      console.warn('Failed to get all role assignments, returning mock data');
      return this.getMockUserRoleAssignments();
    }
  }

  // Mock data methods (fallbacks)
  private static getMockRoleTemplates(): RoleTemplate[] {
      return [
        {
          id: '1',
          name: 'TestingLabAdmin',
          description: 'Full administrative access to all TestingLab resources',
          permissionTemplates: [
            { action: 'create', resourceType: 'TestingSession' },
            { action: 'read', resourceType: 'TestingSession' },
            { action: 'edit', resourceType: 'TestingSession' },
            { action: 'delete', resourceType: 'TestingSession' },
            { action: 'create', resourceType: 'TestingLocation' },
            { action: 'read', resourceType: 'TestingLocation' },
            { action: 'edit', resourceType: 'TestingLocation' },
            { action: 'delete', resourceType: 'TestingLocation' },
            { action: 'create', resourceType: 'TestingFeedback' },
            { action: 'read', resourceType: 'TestingFeedback' },
            { action: 'edit', resourceType: 'TestingFeedback' },
            { action: 'delete', resourceType: 'TestingFeedback' },
            { action: 'moderate', resourceType: 'TestingFeedback' },
            { action: 'create', resourceType: 'TestingRequest' },
            { action: 'read', resourceType: 'TestingRequest' },
            { action: 'edit', resourceType: 'TestingRequest' },
            { action: 'delete', resourceType: 'TestingRequest' },
            { action: 'approve', resourceType: 'TestingRequest' },
            { action: 'manage', resourceType: 'TestingParticipant' },
            { action: 'read', resourceType: 'TestingParticipant' },
          ],
          isSystemRole: true,
          userCount: 3,
        },
        {
          id: '2',
          name: 'TestingLabManager',
          description: 'Management access with some administrative privileges',
          permissionTemplates: [
            { action: 'create', resourceType: 'TestingSession' },
            { action: 'read', resourceType: 'TestingSession' },
            { action: 'edit', resourceType: 'TestingSession' },
            { action: 'read', resourceType: 'TestingLocation' },
            { action: 'edit', resourceType: 'TestingLocation' },
            { action: 'read', resourceType: 'TestingFeedback' },
            { action: 'moderate', resourceType: 'TestingFeedback' },
            { action: 'create', resourceType: 'TestingRequest' },
            { action: 'read', resourceType: 'TestingRequest' },
            { action: 'edit', resourceType: 'TestingRequest' },
            { action: 'approve', resourceType: 'TestingRequest' },
            { action: 'manage', resourceType: 'TestingParticipant' },
            { action: 'read', resourceType: 'TestingParticipant' },
          ],
          isSystemRole: true,
          userCount: 5,
        },
        {
          id: '3',
          name: 'TestingLabCoordinator',
          description: 'Coordination role with moderate permissions',
          permissionTemplates: [
            { action: 'create', resourceType: 'TestingSession' },
            { action: 'read', resourceType: 'TestingSession' },
            { action: 'edit', resourceType: 'TestingSession' },
            { action: 'read', resourceType: 'TestingLocation' },
            { action: 'create', resourceType: 'TestingFeedback' },
            { action: 'read', resourceType: 'TestingFeedback' },
            { action: 'create', resourceType: 'TestingRequest' },
            { action: 'read', resourceType: 'TestingRequest' },
            { action: 'read', resourceType: 'TestingParticipant' },
          ],
          isSystemRole: true,
          userCount: 8,
        },
        {
          id: '4',
          name: 'TestingLabTester',
          description: 'Basic testing role for participants',
          permissionTemplates: [
            { action: 'read', resourceType: 'TestingSession' },
            { action: 'read', resourceType: 'TestingLocation' },
            { action: 'create', resourceType: 'TestingFeedback' },
            { action: 'read', resourceType: 'TestingFeedback' },
            { action: 'create', resourceType: 'TestingRequest' },
            { action: 'read', resourceType: 'TestingRequest' },
          ],
          isSystemRole: true,
          userCount: 25,
        },
        {
          id: '5',
          name: 'TestingLabLocationManager',
          description: 'Specialized role for location management',
          permissionTemplates: [
            { action: 'read', resourceType: 'TestingSession' },
            { action: 'create', resourceType: 'TestingLocation' },
            { action: 'read', resourceType: 'TestingLocation' },
            { action: 'edit', resourceType: 'TestingLocation' },
            { action: 'read', resourceType: 'TestingFeedback' },
            { action: 'read', resourceType: 'TestingRequest' },
            { action: 'read', resourceType: 'TestingParticipant' },
          ],
          isSystemRole: true,
          userCount: 2,
        },
        {
          id: '6',
          name: 'TestingLabReadOnly',
          description: 'Read-only access for observers and stakeholders',
          permissionTemplates: [
            { action: 'read', resourceType: 'TestingSession' },
            { action: 'read', resourceType: 'TestingLocation' },
            { action: 'read', resourceType: 'TestingFeedback' },
            { action: 'read', resourceType: 'TestingRequest' },
            { action: 'read', resourceType: 'TestingParticipant' },
          ],
          isSystemRole: true,
          userCount: 12,
        },
      ];
    }

  private static getMockUserRoleAssignments(): UserRoleAssignment[] {
    return [
      {
        id: '1',
        userId: 'user-1',
        module: TESTING_LAB_MODULE,
        roleName: 'TestingLabAdmin',
        createdAt: '2024-01-01T10:00:00Z',
        isActive: true,
      },
      {
        id: '2',
        userId: 'user-2',
        module: TESTING_LAB_MODULE,
        roleName: 'TestingLabManager',
        createdAt: '2024-01-05T14:30:00Z',
        isActive: true,
      },
      {
        id: '3',
        userId: 'user-3',
        module: TESTING_LAB_MODULE,
        roleName: 'TestingLabCoordinator',
        createdAt: '2024-01-10T09:15:00Z',
        isActive: true,
      },
      {
        id: '4',
        userId: 'user-4',
        module: TESTING_LAB_MODULE,
        roleName: 'TestingLabTester',
        createdAt: '2024-01-15T16:45:00Z',
        isActive: true,
      },
      {
        id: '5',
        userId: 'user-5',
        module: TESTING_LAB_MODULE,
        roleName: 'TestingLabLocationManager',
        createdAt: '2024-01-20T11:20:00Z',
        isActive: true,
      },
      {
        id: '6',
        userId: 'user-6',
        module: TESTING_LAB_MODULE,
        roleName: 'TestingLabReadOnly',
        createdAt: '2024-01-25T13:10:00Z',
        isActive: false,
      },
    ];
  }
}

// Helper function to convert frontend permission form to API format
export function convertFormPermissionsToAPI(formPermissions: any): PermissionTemplate[] {
  const permissions: PermissionTemplate[] = [];

  // Convert form permissions to PermissionTemplate array
  Object.entries(formPermissions).forEach(([key, value]) => {
    if (value) {
      switch (key) {
        case 'createSession':
          permissions.push({ action: 'create', resourceType: 'TestingSession' });
          break;
        case 'readSession':
          permissions.push({ action: 'read', resourceType: 'TestingSession' });
          break;
        case 'editSession':
          permissions.push({ action: 'edit', resourceType: 'TestingSession' });
          break;
        case 'deleteSession':
          permissions.push({ action: 'delete', resourceType: 'TestingSession' });
          break;
        case 'createLocation':
          permissions.push({ action: 'create', resourceType: 'TestingLocation' });
          break;
        case 'readLocation':
          permissions.push({ action: 'read', resourceType: 'TestingLocation' });
          break;
        case 'editLocation':
          permissions.push({ action: 'edit', resourceType: 'TestingLocation' });
          break;
        case 'deleteLocation':
          permissions.push({ action: 'delete', resourceType: 'TestingLocation' });
          break;
        case 'createFeedback':
          permissions.push({ action: 'create', resourceType: 'TestingFeedback' });
          break;
        case 'readFeedback':
          permissions.push({ action: 'read', resourceType: 'TestingFeedback' });
          break;
        case 'editFeedback':
          permissions.push({ action: 'edit', resourceType: 'TestingFeedback' });
          break;
        case 'deleteFeedback':
          permissions.push({ action: 'delete', resourceType: 'TestingFeedback' });
          break;
        case 'moderateFeedback':
          permissions.push({ action: 'moderate', resourceType: 'TestingFeedback' });
          break;
        case 'createRequest':
          permissions.push({ action: 'create', resourceType: 'TestingRequest' });
          break;
        case 'readRequest':
          permissions.push({ action: 'read', resourceType: 'TestingRequest' });
          break;
        case 'editRequest':
          permissions.push({ action: 'edit', resourceType: 'TestingRequest' });
          break;
        case 'deleteRequest':
          permissions.push({ action: 'delete', resourceType: 'TestingRequest' });
          break;
        case 'approveRequest':
          permissions.push({ action: 'approve', resourceType: 'TestingRequest' });
          break;
        case 'manageParticipant':
          permissions.push({ action: 'manage', resourceType: 'TestingParticipant' });
          break;
        case 'readParticipant':
          permissions.push({ action: 'read', resourceType: 'TestingParticipant' });
          break;
      }
    }
  });

  return permissions;
}

// Helper function to convert API permissions to form format
export function convertAPIPermissionsToForm(permissions: PermissionTemplate[]): any {
  const formPermissions = {
    createSession: false,
    readSession: false,
    editSession: false,
    deleteSession: false,
    createLocation: false,
    readLocation: false,
    editLocation: false,
    deleteLocation: false,
    createFeedback: false,
    readFeedback: false,
    editFeedback: false,
    deleteFeedback: false,
    moderateFeedback: false,
    createRequest: false,
    readRequest: false,
    editRequest: false,
    deleteRequest: false,
    approveRequest: false,
    manageParticipant: false,
    readParticipant: false,
  };

  permissions.forEach(p => {
    const key = `${p.action}${p.resourceType.replace('Testing', '')}`;
    if (key in formPermissions) {
      (formPermissions as any)[key] = true;
    }
  });

  return formPermissions;
}
