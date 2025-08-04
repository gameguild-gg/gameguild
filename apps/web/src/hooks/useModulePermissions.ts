import { useState, useEffect, useCallback } from 'react';
import { permissionService } from '@/lib/services/permissionService';
import { ModuleType, ModuleAction, UserRoleAssignment, ModuleRole } from '@/types/modulePermissions';

export interface UseModulePermissionsReturn {
  // Permission checking
  hasPermission: (module: ModuleType, action: ModuleAction, resourceId?: string) => Promise<boolean>;

  // Role management
  userRoles: UserRoleAssignment[];
  assignRole: (userId: string, tenantId: string | null, module: ModuleType, roleName: string, expiresAt?: Date) => Promise<void>;
  revokeRole: (userId: string, tenantId: string | null, module: ModuleType, roleName: string) => Promise<void>;

  // Testing Lab specific
  testingLabPermissions: {
    canAdminister: boolean;
    canCreateSessions: boolean;
    canEditSessions: boolean;
    canDeleteSessions: boolean;
    canManageRequests: boolean;
    canViewAnalytics: boolean;
    roles: UserRoleAssignment[];
  } | null;

  // Available roles
  availableRoles: ModuleRole[];
  testingLabRoles: ModuleRole[];

  // Loading states
  loading: boolean;
  rolesLoading: boolean;
  permissionsLoading: boolean;

  // Actions
  refreshRoles: () => Promise<void>;
  refreshPermissions: () => Promise<void>;
  makeTestingLabAdmin: (userId: string) => Promise<void>;
  makeTestingLabManager: (userId: string) => Promise<void>;
}

export function useModulePermissions(userId: string, tenantId?: string, autoLoad: boolean = true): UseModulePermissionsReturn {
  const [userRoles, setUserRoles] = useState<UserRoleAssignment[]>([]);
  const [testingLabPermissions, setTestingLabPermissions] = useState<UseModulePermissionsReturn['testingLabPermissions']>(null);
  const [availableRoles, setAvailableRoles] = useState<ModuleRole[]>([]);
  const [testingLabRoles, setTestingLabRoles] = useState<ModuleRole[]>([]);

  const [loading, setLoading] = useState(false);
  const [rolesLoading, setRolesLoading] = useState(false);
  const [permissionsLoading, setPermissionsLoading] = useState(false);

  // Load user roles
  const refreshRoles = useCallback(async () => {
    if (!userId) return;

    setRolesLoading(true);
    try {
      const roles = await permissionService.getUserRoles(userId, tenantId);
      setUserRoles(roles);
    } catch (error) {
      console.error('Failed to load user roles:', error);
    } finally {
      setRolesLoading(false);
    }
  }, [userId, tenantId]);

  // Load testing lab permissions
  const refreshPermissions = useCallback(async () => {
    if (!userId) return;

    setPermissionsLoading(true);
    try {
      const permissions = await permissionService.getUserTestingLabPermissions(userId, tenantId);
      setTestingLabPermissions(permissions);
    } catch (error) {
      console.error('Failed to load testing lab permissions:', error);
    } finally {
      setPermissionsLoading(false);
    }
  }, [userId, tenantId]);

  // Load available roles
  const loadAvailableRoles = useCallback(async () => {
    try {
      const [allRoles, testingRoles] = await Promise.all([permissionService.getAllPredefinedRoles(), permissionService.getTestingLabRoles()]);
      setAvailableRoles(allRoles);
      setTestingLabRoles(testingRoles);
    } catch (error) {
      console.error('Failed to load available roles:', error);
    }
  }, []);

  // Permission checking function
  const hasPermission = useCallback(
    async (module: ModuleType, action: ModuleAction, resourceId?: string): Promise<boolean> => {
      try {
        const result = await permissionService.hasModulePermission(module, action, tenantId, resourceId);
        return result;
      } catch (error) {
        console.error('Failed to check permission:', error);
        return false;
      }
    },
    [tenantId],
  );

  // Detailed permission checking
  const checkPermission = useCallback(
    async (module: ModuleType, action: ModuleAction, resourceId?: string): Promise<boolean> => {
      return permissionService.hasModulePermission(module, action, tenantId, resourceId);
    },
    [tenantId],
  );

  // Role assignment
  const assignRole = useCallback(
    async (userId: string, assignTenantId: string | null, module: ModuleType, roleName: string, expiresAt?: Date) => {
      try {
        await permissionService.assignRole(userId, assignTenantId, module, roleName, expiresAt);
        await refreshRoles();
        await refreshPermissions();
      } catch (error) {
        console.error('Failed to assign role:', error);
        throw error;
      }
    },
    [refreshRoles, refreshPermissions],
  );

  // Role revocation
  const revokeRole = useCallback(
    async (userId: string, assignTenantId: string | null, module: ModuleType, roleName: string) => {
      try {
        await permissionService.revokeRole(userId, assignTenantId, module, roleName);
        await refreshRoles();
        await refreshPermissions();
      } catch (error) {
        console.error('Failed to revoke role:', error);
        throw error;
      }
    },
    [refreshRoles, refreshPermissions],
  );

  // Testing Lab specific helpers
  const makeTestingLabAdmin = useCallback(
    async (userId: string) => {
      await assignRole(userId, tenantId || null, ModuleType.TestingLab, 'TestingLabAdmin');
    },
    [assignRole, tenantId],
  );

  const makeTestingLabManager = useCallback(
    async (userId: string) => {
      await assignRole(userId, tenantId || null, ModuleType.TestingLab, 'TestingLabManager');
    },
    [assignRole, tenantId],
  );

  // Initial load
  useEffect(() => {
    if (autoLoad && userId) {
      setLoading(true);
      Promise.all([refreshRoles(), refreshPermissions(), loadAvailableRoles()]).finally(() => {
        setLoading(false);
      });
    }
  }, [userId, tenantId, autoLoad, refreshRoles, refreshPermissions, loadAvailableRoles]);

  return {
    hasPermission,
    checkPermission,
    userRoles,
    assignRole,
    revokeRole,
    testingLabPermissions,
    availableRoles,
    testingLabRoles,
    loading,
    rolesLoading,
    permissionsLoading,
    refreshRoles,
    refreshPermissions,
    makeTestingLabAdmin,
    makeTestingLabManager,
  };
}

// Hook for Testing Lab specific permissions
export function useTestingLabPermissions(userId: string, tenantId?: string) {
  const { testingLabPermissions, hasPermission, checkPermission, refreshPermissions, permissionsLoading, makeTestingLabAdmin, makeTestingLabManager } = useModulePermissions(userId, tenantId);

  const canCreateSessions = useCallback(() => hasPermission(ModuleType.TestingLab, ModuleAction.Create, 'TestingSession'), [hasPermission]);

  const canEditSession = useCallback((sessionId: string) => hasPermission(ModuleType.TestingLab, ModuleAction.Edit, 'TestingSession', sessionId), [hasPermission]);

  const canDeleteSessions = useCallback(() => hasPermission(ModuleType.TestingLab, ModuleAction.Delete, 'TestingSession'), [hasPermission]);

  const canManageRequests = useCallback(() => hasPermission(ModuleType.TestingLab, ModuleAction.Manage, 'TestingRequest'), [hasPermission]);

  const canViewAnalytics = useCallback(() => hasPermission(ModuleType.TestingLab, ModuleAction.ViewAnalytics), [hasPermission]);

  const canAdminister = useCallback(() => hasPermission(ModuleType.TestingLab, ModuleAction.Administer), [hasPermission]);

  return {
    permissions: testingLabPermissions,
    loading: permissionsLoading,
    refresh: refreshPermissions,
    // Permission checkers
    canCreateSessions,
    canEditSession,
    canDeleteSessions,
    canManageRequests,
    canViewAnalytics,
    canAdminister,
    // Role assignment helpers
    makeAdmin: makeTestingLabAdmin,
    makeManager: makeTestingLabManager,
    // Detailed permission checking
    checkPermission: (action: ModuleAction, resourceType?: string, resourceId?: string) => checkPermission(ModuleType.TestingLab, action, resourceType, resourceId),
  };
}

export default useModulePermissions;
