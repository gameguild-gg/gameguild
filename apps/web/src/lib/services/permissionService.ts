import {
  PermissionType,
  EffectivePermission,
  ResourceUserPermission,
  ShareResourceRequest,
  ShareResult,
  InviteUserRequest,
  InvitationResult,
  PermissionUpdateResult,
  ResourceInvitation,
  PermissionHierarchy,
  ProjectCollaborator,
  AddCollaboratorRequest,
  UpdateCollaboratorRequest,
  ShareProjectWithRoleRequest,
  ProjectRoleTemplate,
  PROJECT_ROLE_TEMPLATES,
} from '@/types/permissions';

import { ModuleType, ModuleAction, RoleLevel, ModuleRole, UserRoleAssignment, ALL_PREDEFINED_ROLES, TESTING_LAB_ROLES, GLOBAL_ROLES, MODULE_RESOURCES } from '@/types/modulePermissions';

/**
 * Service for managing permissions and resource sharing
 */
export class PermissionService {
  private baseUrl: string;
  private token: string | null = null;

  constructor(baseUrl: string = '/api') {
    this.baseUrl = baseUrl;
  }

  setAuthToken(token: string) {
    this.token = token;
  }

  private getHeaders(): HeadersInit {
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
    };

    if (this.token) {
      headers['Authorization'] = `Bearer ${this.token}`;
    }

    return headers;
  }

  private async handleResponse<T>(response: Response): Promise<T> {
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`HTTP ${response.status}: ${errorText}`);
    }

    const contentType = response.headers.get('content-type');
    if (contentType && contentType.includes('application/json')) {
      return response.json();
    }

    return response.text() as unknown as T;
  }

  // Resource Permission Management

  async getMyPermissions(resourceType: string, resourceId: string): Promise<EffectivePermission[]> {
    const response = await fetch(`${this.baseUrl}/resources/${resourceType}/${resourceId}/permissions/my-permissions`, {
      method: 'GET',
      headers: this.getHeaders(),
    });

    return this.handleResponse<EffectivePermission[]>(response);
  }

  async getResourceUsers(resourceType: string, resourceId: string): Promise<ResourceUserPermission[]> {
    const response = await fetch(`${this.baseUrl}/resources/${resourceType}/${resourceId}/permissions/users`, {
      method: 'GET',
      headers: this.getHeaders(),
    });

    return this.handleResponse<ResourceUserPermission[]>(response);
  }

  async shareResource(resourceType: string, resourceId: string, shareRequest: ShareResourceRequest): Promise<ShareResult> {
    const response = await fetch(`${this.baseUrl}/resources/${resourceType}/${resourceId}/permissions/share`, {
      method: 'POST',
      headers: this.getHeaders(),
      body: JSON.stringify(shareRequest),
    });

    return this.handleResponse<ShareResult>(response);
  }

  async updateUserPermissions(resourceType: string, resourceId: string, targetUserId: string, permissions: PermissionType[], expiresAt?: Date): Promise<PermissionUpdateResult> {
    const response = await fetch(`${this.baseUrl}/resources/${resourceType}/${resourceId}/permissions/users/${targetUserId}`, {
      method: 'PUT',
      headers: this.getHeaders(),
      body: JSON.stringify({ permissions, expiresAt }),
    });

    return this.handleResponse<PermissionUpdateResult>(response);
  }

  async removeUserAccess(resourceType: string, resourceId: string, targetUserId: string): Promise<PermissionUpdateResult> {
    const response = await fetch(`${this.baseUrl}/resources/${resourceType}/${resourceId}/permissions/users/${targetUserId}`, {
      method: 'DELETE',
      headers: this.getHeaders(),
    });

    return this.handleResponse<PermissionUpdateResult>(response);
  }

  async inviteUser(resourceType: string, resourceId: string, inviteRequest: InviteUserRequest): Promise<InvitationResult> {
    const response = await fetch(`${this.baseUrl}/resources/${resourceType}/${resourceId}/permissions/invite`, {
      method: 'POST',
      headers: this.getHeaders(),
      body: JSON.stringify(inviteRequest),
    });

    return this.handleResponse<InvitationResult>(response);
  }

  async getPendingInvitations(resourceType: string, resourceId: string): Promise<ResourceInvitation[]> {
    const response = await fetch(`${this.baseUrl}/resources/${resourceType}/${resourceId}/permissions/invitations`, {
      method: 'GET',
      headers: this.getHeaders(),
    });

    return this.handleResponse<ResourceInvitation[]>(response);
  }

  async getPermissionHierarchy(resourceType: string, resourceId: string, permission: PermissionType): Promise<PermissionHierarchy> {
    const response = await fetch(`${this.baseUrl}/resources/${resourceType}/${resourceId}/permissions/hierarchy?permission=${permission}`, {
      method: 'GET',
      headers: this.getHeaders(),
    });

    return this.handleResponse<PermissionHierarchy>(response);
  }

  // Project-Specific Permission Management

  async getProjectCollaborators(projectId: string): Promise<ProjectCollaborator[]> {
    const response = await fetch(`${this.baseUrl}/projects/${projectId}/permissions/collaborators`, {
      method: 'GET',
      headers: this.getHeaders(),
    });

    return this.handleResponse<ProjectCollaborator[]>(response);
  }

  async addProjectCollaborator(projectId: string, request: AddCollaboratorRequest): Promise<InvitationResult> {
    const response = await fetch(`${this.baseUrl}/projects/${projectId}/permissions/collaborators`, {
      method: 'POST',
      headers: this.getHeaders(),
      body: JSON.stringify(request),
    });

    return this.handleResponse<InvitationResult>(response);
  }

  async updateCollaboratorPermissions(projectId: string, collaboratorUserId: string, request: UpdateCollaboratorRequest): Promise<PermissionUpdateResult> {
    const response = await fetch(`${this.baseUrl}/projects/${projectId}/permissions/collaborators/${collaboratorUserId}`, {
      method: 'PUT',
      headers: this.getHeaders(),
      body: JSON.stringify(request),
    });

    return this.handleResponse<PermissionUpdateResult>(response);
  }

  async removeProjectCollaborator(projectId: string, collaboratorUserId: string): Promise<PermissionUpdateResult> {
    const response = await fetch(`${this.baseUrl}/projects/${projectId}/permissions/collaborators/${collaboratorUserId}`, {
      method: 'DELETE',
      headers: this.getHeaders(),
    });

    return this.handleResponse<PermissionUpdateResult>(response);
  }

  async getProjectRoleTemplates(): Promise<ProjectRoleTemplate[]> {
    return Promise.resolve(PROJECT_ROLE_TEMPLATES);
  }

  async shareProjectWithRole(projectId: string, request: ShareProjectWithRoleRequest): Promise<ShareResult> {
    const response = await fetch(`${this.baseUrl}/projects/${projectId}/permissions/share-with-role`, {
      method: 'POST',
      headers: this.getHeaders(),
      body: JSON.stringify(request),
    });

    return this.handleResponse<ShareResult>(response);
  }

  // Invitation Management

  async acceptInvitation(invitationId: string): Promise<PermissionUpdateResult> {
    const response = await fetch(`${this.baseUrl}/invitations/${invitationId}/accept`, {
      method: 'POST',
      headers: this.getHeaders(),
    });

    return this.handleResponse<PermissionUpdateResult>(response);
  }

  async declineInvitation(invitationId: string): Promise<PermissionUpdateResult> {
    const response = await fetch(`${this.baseUrl}/invitations/${invitationId}/decline`, {
      method: 'POST',
      headers: this.getHeaders(),
    });

    return this.handleResponse<PermissionUpdateResult>(response);
  }

  // Utility Methods

  async hasPermission(resourceType: string, resourceId: string, permission: PermissionType): Promise<boolean> {
    try {
      const permissions = await this.getMyPermissions(resourceType, resourceId);
      return permissions.some((p) => p.permission === permission && p.isGranted);
    } catch (error) {
      console.error('Error checking permission:', error);
      return false;
    }
  }

  async hasAnyPermission(resourceType: string, resourceId: string, permissions: PermissionType[]): Promise<boolean> {
    try {
      const userPermissions = await this.getMyPermissions(resourceType, resourceId);
      return permissions.some((permission) => userPermissions.some((p) => p.permission === permission && p.isGranted));
    } catch (error) {
      console.error('Error checking permissions:', error);
      return false;
    }
  }

  async hasAllPermissions(resourceType: string, resourceId: string, permissions: PermissionType[]): Promise<boolean> {
    try {
      const userPermissions = await this.getMyPermissions(resourceType, resourceId);
      return permissions.every((permission) => userPermissions.some((p) => p.permission === permission && p.isGranted));
    } catch (error) {
      console.error('Error checking permissions:', error);
      return false;
    }
  }

  async bulkCheckPermissions(resourceType: string, resourceIds: string[], permission: PermissionType): Promise<Record<string, boolean>> {
    const results: Record<string, boolean> = {};

    const promises = resourceIds.map(async (resourceId) => {
      const hasPermission = await this.hasPermission(resourceType, resourceId, permission);
      results[resourceId] = hasPermission;
    });

    await Promise.all(promises);
    return results;
  }

  // ===== MODULE-BASED PERMISSION SYSTEM =====

  // Role Management

  async assignRole(userId: string, tenantId: string | null, module: ModuleType, roleName: string, expiresAt?: Date): Promise<UserRoleAssignment> {
    const response = await fetch(`${this.baseUrl}/module-permissions/assign-role`, {
      method: 'POST',
      headers: this.getHeaders(),
      body: JSON.stringify({ userId, tenantId, module, roleName, expiresAt }),
    });

    return this.handleResponse<UserRoleAssignment>(response);
  }

  async revokeRole(userId: string, tenantId: string | null, module: ModuleType, roleName: string): Promise<void> {
    const response = await fetch(`${this.baseUrl}/module-permissions/revoke-role`, {
      method: 'DELETE',
      headers: this.getHeaders(),
      body: JSON.stringify({ userId, tenantId, module, roleName }),
    });

    await this.handleResponse<void>(response);
  }

  async getUserRoles(userId: string, module: ModuleType, tenantId?: string): Promise<UserRoleAssignment[]> {
    const params = new URLSearchParams();
    params.set('module', module);
    if (tenantId) params.set('tenantId', tenantId);

    const response = await fetch(`${this.baseUrl}/module-permissions/users/${userId}/roles?${params}`, {
      method: 'GET',
      headers: this.getHeaders(),
    });

    return this.handleResponse<UserRoleAssignment[]>(response);
  }

  // Module Permission Checks

  async hasModulePermission(module: ModuleType, action: ModuleAction, tenantId?: string, resourceId?: string): Promise<boolean> {
    const params = new URLSearchParams();
    params.set('module', module);
    params.set('action', action);
    if (tenantId) params.set('tenantId', tenantId);
    if (resourceId) params.set('resourceId', resourceId);

    const response = await fetch(`${this.baseUrl}/module-permissions/check?${params}`, {
      method: 'GET',
      headers: this.getHeaders(),
    });

    return this.handleResponse<boolean>(response);
  }

  async bulkCheckModulePermissions(
    checks: Array<{
      module: ModuleType;
      action: ModuleAction;
      resourceId?: string;
    }>,
    tenantId?: string,
  ): Promise<Record<string, boolean>> {
    const response = await fetch(`${this.baseUrl}/module-permissions/bulk-check`, {
      method: 'POST',
      headers: this.getHeaders(),
      body: JSON.stringify({ checks, tenantId }),
    });

    return this.handleResponse<Record<string, boolean>>(response);
  }

  // Role Templates

  async getAllRoles(level?: RoleLevel, module?: ModuleType): Promise<ModuleRole[]> {
    const params = new URLSearchParams();
    if (level) params.set('level', level);
    if (module) params.set('module', module);

    const response = await fetch(`${this.baseUrl}/roles?${params}`, {
      method: 'GET',
      headers: this.getHeaders(),
    });

    return this.handleResponse<ModuleRole[]>(response);
  }

  async getTestingLabRoles(): Promise<ModuleRole[]> {
    return Promise.resolve(TESTING_LAB_ROLES);
  }

  async getGlobalRoles(): Promise<ModuleRole[]> {
    return Promise.resolve(GLOBAL_ROLES);
  }

  async getAllPredefinedRoles(): Promise<ModuleRole[]> {
    return Promise.resolve(ALL_PREDEFINED_ROLES);
  }

  async getModuleResources(module: ModuleType): Promise<string[]> {
    const moduleResource = MODULE_RESOURCES.find((mr) => mr.module === module);
    return Promise.resolve(moduleResource?.resourceTypes ?? []);
  }

  // Custom Role Management

  async createCustomRole(role: Omit<ModuleRole, 'id' | 'isSystemRole'>): Promise<ModuleRole> {
    const response = await fetch(`${this.baseUrl}/roles`, {
      method: 'POST',
      headers: this.getHeaders(),
      body: JSON.stringify({ ...role, isSystemRole: false }),
    });

    return this.handleResponse<ModuleRole>(response);
  }

  async updateCustomRole(roleId: string, updates: Partial<ModuleRole>): Promise<ModuleRole> {
    const response = await fetch(`${this.baseUrl}/roles/${roleId}`, {
      method: 'PUT',
      headers: this.getHeaders(),
      body: JSON.stringify(updates),
    });

    return this.handleResponse<ModuleRole>(response);
  }

  async deleteCustomRole(roleId: string): Promise<void> {
    const response = await fetch(`${this.baseUrl}/roles/${roleId}`, {
      method: 'DELETE',
      headers: this.getHeaders(),
    });

    await this.handleResponse<void>(response);
  }

  // Convenience Methods for Testing Lab

  async isTestingLabAdmin(tenantId?: string): Promise<boolean> {
    return this.hasModulePermission(ModuleType.TestingLab, ModuleAction.Administer, tenantId);
  }

  async canCreateTestingSessions(tenantId?: string): Promise<boolean> {
    return this.hasModulePermission(ModuleType.TestingLab, ModuleAction.Create, tenantId);
  }

  async canDeleteTestingSessions(tenantId?: string): Promise<boolean> {
    return this.hasModulePermission(ModuleType.TestingLab, ModuleAction.Delete, tenantId);
  }

  async canEditTestingSession(sessionId: string, tenantId?: string): Promise<boolean> {
    return this.hasModulePermission(ModuleType.TestingLab, ModuleAction.Edit, tenantId, sessionId);
  }

  async canManageTestingRequests(tenantId?: string): Promise<boolean> {
    return this.hasModulePermission(ModuleType.TestingLab, ModuleAction.Manage, tenantId);
  }

  async canViewTestingAnalytics(tenantId?: string): Promise<boolean> {
    return this.hasModulePermission(ModuleType.TestingLab, ModuleAction.ViewAnalytics, tenantId);
  }

  // User Role Assignment Helpers

  async assignTestingLabRole(userId: string, roleType: 'admin' | 'manager' | 'coordinator' | 'tester', tenantId?: string): Promise<UserRoleAssignment> {
    const roleMap = {
      admin: 'TestingLabAdmin',
      manager: 'TestingLabManager',
      coordinator: 'TestingLabCoordinator',
      tester: 'TestingLabTester',
    };

    const roleName = roleMap[roleType];
    return this.assignRole(userId, tenantId || null, ModuleType.TestingLab, roleName);
  }

  async makeUserTestingLabAdmin(userId: string, tenantId?: string): Promise<UserRoleAssignment> {
    return this.assignTestingLabRole(userId, 'admin', tenantId);
  }

  async makeUserTestingLabManager(userId: string, tenantId?: string): Promise<UserRoleAssignment> {
    return this.assignTestingLabRole(userId, 'manager', tenantId);
  }

  async getUserTestingLabPermissions(
    userId: string,
    tenantId?: string,
  ): Promise<{
    canAdminister: boolean;
    canCreateSessions: boolean;
    canEditSessions: boolean;
    canDeleteSessions: boolean;
    canManageRequests: boolean;
    canViewAnalytics: boolean;
    roles: UserRoleAssignment[];
  }> {
    const [roles, canAdminister, canCreateSessions, canEditSessions, canDeleteSessions, canManageRequests, canViewAnalytics] = await Promise.all([
      this.getUserRoles(userId, ModuleType.TestingLab, tenantId),
      this.isTestingLabAdmin(tenantId),
      this.canCreateTestingSessions(tenantId),
      this.hasModulePermission(ModuleType.TestingLab, ModuleAction.Edit, tenantId),
      this.canDeleteTestingSessions(tenantId),
      this.canManageTestingRequests(tenantId),
      this.canViewTestingAnalytics(tenantId),
    ]);

    return {
      canAdminister,
      canCreateSessions,
      canEditSessions,
      canDeleteSessions,
      canManageRequests,
      canViewAnalytics,
      roles: roles.filter((r) => r.roleId.includes('TestingLab')),
    };
  }
}

export const permissionService = new PermissionService();
export default permissionService;
