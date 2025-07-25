'use client';

import { useMemo } from 'react';
import { useTenant } from './context/tenant-provider';
import type { TenantResponse } from './types';

/**
 * Hook that provides tenant-aware utilities and checks
 */
export function useTenantUtils() {
  const { currentTenant, availableTenants, loading, error } = useTenant();

  const tenantUtils = useMemo(() => {
    return {
      // Check if a specific tenant is available
      isTenantAvailable: (tenantId: string): boolean => {
        return availableTenants.some((tenant) => tenant.id === tenantId);
      },

      // Get tenant by ID
      getTenantById: (tenantId: string): TenantResponse | undefined => {
        return availableTenants.find((tenant) => tenant.id === tenantId);
      },

      // Check if current user has access to tenant
      hasAccessToTenant: (tenantId: string): boolean => {
        return availableTenants.some((tenant) => tenant.id === tenantId && tenant.isActive);
      },

      // Get active tenants only
      getActiveTenants: (): TenantResponse[] => {
        return availableTenants.filter((tenant) => tenant.isActive);
      },

      // Check if current tenant is active
      isCurrentTenantActive: (): boolean => {
        return currentTenant?.isActive === true;
      },

      // Get tenant display name with fallback
      getTenantDisplayName: (tenant?: TenantResponse | null): string => {
        return tenant?.name || 'Unknown Tenant';
      },

      // Check if user has multiple tenants available
      hasMultipleTenants: (): boolean => {
        return availableTenants.length > 1;
      },

      // Get default tenant (first active tenant)
      getDefaultTenant: (): TenantResponse | null => {
        const activeTenants = availableTenants.filter((tenant) => tenant.isActive);
        return activeTenants[0] || null;
      },
    };
  }, [currentTenant, availableTenants]);

  return {
    currentTenant,
    availableTenants,
    loading,
    error,
    ...tenantUtils,
  };
}

/**
 * Hook that provides tenant-scoped data fetching utilities
 */
export function useTenantScoped() {
  const { currentTenant } = useTenant();

  const scopeToTenant = useMemo(() => {
    return {
      // Add tenant ID to request params
      addTenantToParams: (params: Record<string, unknown> = {}): Record<string, unknown> => {
        if (!currentTenant) return params;
        return {
          ...params,
          tenantId: currentTenant.id,
        };
      },

      // Add tenant ID to URL search params
      addTenantToSearchParams: (searchParams: URLSearchParams = new URLSearchParams()): URLSearchParams => {
        if (currentTenant) {
          searchParams.set('tenantId', currentTenant.id);
        }
        return searchParams;
      },

      // Add tenant ID to headers
      addTenantToHeaders: (headers: Record<string, string> = {}): Record<string, string> => {
        if (!currentTenant) return headers;
        return {
          ...headers,
          'X-Tenant-Id': currentTenant.id,
        };
      },

      // Check if data belongs to current tenant
      belongsToCurrentTenant: (data: { tenantId?: string }): boolean => {
        return data.tenantId === currentTenant?.id;
      },

      // Filter array by current tenant
      filterByCurrentTenant: <T extends { tenantId?: string }>(items: T[]): T[] => {
        if (!currentTenant) return [];
        return items.filter((item) => item.tenantId === currentTenant.id);
      },
    };
  }, [currentTenant]);

  return {
    currentTenant,
    tenantId: currentTenant?.id,
    hasTenant: !!currentTenant,
    ...scopeToTenant,
  };
}

/**
 * Hook for tenant permission checks
 */
export function useTenantPermissions() {
  const { currentTenant, availableTenants } = useTenant();

  const permissions = useMemo(() => {
    return {
      // Check if user can switch to a specific tenant
      canSwitchToTenant: (tenantId: string): boolean => {
        const tenant = availableTenants.find((t) => t.id === tenantId);
        return tenant?.isActive === true;
      },

      // Check if user can access tenant admin features
      canAccessTenantAdmin: (): boolean => {
        // This would typically check user roles/permissions
        // For now, just check if tenant is active
        return currentTenant?.isActive === true;
      },

      // Check if user can create resources in current tenant
      canCreateInTenant: (): boolean => {
        return currentTenant?.isActive === true;
      },

      // Check if user can modify tenant settings
      canModifyTenantSettings: (): boolean => {
        // This would typically check specific admin permissions
        return currentTenant?.isActive === true;
      },

      // Get accessible tenant IDs
      getAccessibleTenantIds: (): string[] => {
        return availableTenants.filter((tenant) => tenant.isActive).map((tenant) => tenant.id);
      },
    };
  }, [currentTenant, availableTenants]);

  return {
    currentTenant,
    ...permissions,
  };
}
