/**
 * Testing Lab Permissions API - STUB implementation.
 * The Testing Lab module is disabled in GameGuild.Production.sln
 */

// Type stubs
export interface PermissionTemplate {
  action: string;
  resourceType: string;
  constraints?: Record<string, unknown>[];
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

export interface UserPermissionSummary {
  userId: string;
  roles: string[];
  permissions: string[];
}

// TestingLab Permission API - Stub implementation
export class TestingLabPermissionAPI {
  // Role Templates
  static async getRoleTemplates(): Promise<RoleTemplate[]> {
    return [];
  }

  static async createRoleTemplate(_request: CreateRoleTemplateRequest): Promise<RoleTemplate | null> {
    console.warn('[STUB] Testing Lab permissions are disabled');
    return null;
  }

  static async updateRoleTemplate(_roleId: string, _request: UpdateRoleTemplateRequest): Promise<RoleTemplate | null> {
    console.warn('[STUB] Testing Lab permissions are disabled');
    return null;
  }

  static async deleteRoleTemplate(_roleId: string): Promise<boolean> {
    console.warn('[STUB] Testing Lab permissions are disabled');
    return false;
  }

  // User Role Assignments
  static async getUserRoles(_userId: string): Promise<string[]> {
    return [];
  }

  static async assignRoleToUser(_userId: string, _roleName: string): Promise<boolean> {
    console.warn('[STUB] Testing Lab permissions are disabled');
    return false;
  }

  static async removeRoleFromUser(_userId: string, _roleName: string): Promise<boolean> {
    console.warn('[STUB] Testing Lab permissions are disabled');
    return false;
  }

  // Permission Checks
  static async checkUserPermission(_userId: string, _action: string, _resourceId?: string): Promise<boolean> {
    return false;
  }

  static async getUserPermissions(_userId: string): Promise<UserPermissionSummary | null> {
    return null;
  }

  // Role Template CRUD
  static async getRoleTemplate(_roleId: string): Promise<RoleTemplate | null> {
    return null;
  }

  // Bulk Operations
  static async assignMultipleRoles(_userId: string, _roleNames: string[]): Promise<boolean> {
    console.warn('[STUB] Testing Lab permissions are disabled');
    return false;
  }

  static async getModuleRoles(): Promise<RoleTemplate[]> {
    return [];
  }
}

export default TestingLabPermissionAPI;
