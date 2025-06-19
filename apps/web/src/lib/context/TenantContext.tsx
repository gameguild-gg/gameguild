'use client';

import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { useSession } from 'next-auth/react';
import { TenantInfo } from '@/types/auth';
import { TenantResponse } from '@/types/tenant';
import { apiClient } from '@/lib/api-client';

interface TenantContextType {
  currentTenant: TenantInfo | null;
  availableTenants: TenantInfo[];
  setCurrentTenant: (tenant: TenantInfo | null) => void;
  switchTenant: (tenantId: string) => Promise<void>;
  isLoading: boolean;
  error: string | null;
}

const TenantContext = createContext<TenantContextType | undefined>(undefined);

interface TenantProviderProps {
  children: ReactNode;
}

export function TenantProvider({ children }: TenantProviderProps) {
  const { data: session, update } = useSession();
  const [currentTenant, setCurrentTenant] = useState<TenantInfo | null>(null);
  const [availableTenants, setAvailableTenants] = useState<TenantInfo[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Initialize tenant data from session
  useEffect(() => {
    if (session?.user) {
      const sessionData = session as any;
      if (sessionData.currentTenant) {
        setCurrentTenant(sessionData.currentTenant);
      }
      if (sessionData.availableTenants) {
        setAvailableTenants(sessionData.availableTenants);
      }
    }
  }, [session]);

  const switchTenant = async (tenantId: string) => {
    if (!session?.accessToken) {
      setError('No access token available');
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      // Find the tenant in available tenants
      const tenant = availableTenants.find(t => t.id === tenantId);
      if (!tenant) {
        throw new Error('Tenant not found in available tenants');
      }

      // Update the current tenant
      setCurrentTenant(tenant);

      // Update the session with the new tenant
      await update({
        ...session,
        currentTenant: tenant,
      });

    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to switch tenant');
    } finally {
      setIsLoading(false);
    }
  };

  const value: TenantContextType = {
    currentTenant,
    availableTenants,
    setCurrentTenant,
    switchTenant,
    isLoading,
    error,
  };

  return (
    <TenantContext.Provider value={value}>
      {children}
    </TenantContext.Provider>
  );
}

export function useTenant() {
  const context = useContext(TenantContext);
  if (context === undefined) {
    throw new Error('useTenant must be used within a TenantProvider');
  }
  return context;
}

// Hook for making authenticated API calls with current tenant context
export function useAuthenticatedApi() {
  const { data: session } = useSession();
  const { currentTenant } = useTenant();

  const makeRequest = async function<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    if (!session?.accessToken) {
      throw new Error('No access token available');
    }

    // Add tenant header if current tenant is selected
    const headers: Record<string, string> = {
      ...((options.headers as Record<string, string>) || {}),
    };

    if (currentTenant) {
      headers['X-Tenant-Id'] = currentTenant.id;
    }

    return apiClient.authenticatedRequest<T>(endpoint, session.accessToken, {
      ...options,
      headers,
    });
  };

  return { makeRequest, isAuthenticated: !!session?.accessToken };
}
