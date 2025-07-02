'use client';

import React, { createContext, Dispatch, PropsWithChildren, useCallback, useContext, useEffect, useReducer } from 'react';
import { useSession } from 'next-auth/react';

export const TenantActionType = {
  SET_CURRENT_TENANT: 'SET_CURRENT_TENANT',
  SET_AVAILABLE_TENANTS: 'SET_AVAILABLE_TENANTS',
  SET_LOADING: 'SET_LOADING',
  CLEAR_ERROR: 'CLEAR_ERROR',
  INVALIDATE_CACHE: 'INVALIDATE_CACHE',
} as const;

export type TenantActionType = (typeof TenantActionType)[keyof typeof TenantActionType];

export type TenantAction =
  | { type: typeof TenantActionType.SET_CURRENT_TENANT; payload: unknown } // TODO: Replace with actual TenantInfo type
  | { type: typeof TenantActionType.SET_AVAILABLE_TENANTS; payload: Set<unknown> } // TODO: Replace with actual TenantInfo type[]
  | { type: typeof TenantActionType.SET_LOADING; payload: boolean }
  | { type: typeof TenantActionType.CLEAR_ERROR }
  | { type: typeof TenantActionType.INVALIDATE_CACHE };

// TODO: Replace with actual TenantInfo type
export type SwitchCurrentTenant = (tenantId: string) => Promise<unknown | null>;
export type SetCurrentTenant = (tenant: unknown | null) => Promise<unknown | null>;

export type OnSessionUpdate = (session: unknown) => Promise<void>;

// TODO: Replace with actual TenantInfo type
export type UpdateAvailableTenants = (availableTenants: Set<unknown>) => Promise<Set<unknown>>;
export type SetLoading = (loading: boolean) => void;
export type TenantReducer = (initialState: Partial<TenantState>) => [TenantState, Dispatch<TenantAction>];

interface TenantContextValue {
  currentTenant: unknown | null; // TODO: Replace with actual TenantInfo type
  availableTenants: Set<unknown>; // TODO: Replace with actual TenantInfo type[]
  switchCurrentTenant: SwitchCurrentTenant;
  updateAvailableTenants: UpdateAvailableTenants;
}

export interface TenantState {
  currentTenant: unknown | null; // TODO: Replace with actual TenantInfo type
  availableTenants: Set<unknown>; // TODO: Replace with actual TenantInfo type[]
  loading: boolean;
}

export const defaultTenantState: TenantState = {
  currentTenant: null,
  availableTenants: new Set(),
  loading: false,
};

export const createInitialContentState = (data: Partial<TenantState>): TenantState => {
  return { ...defaultTenantState, ...data };
};

export const tenantReducer = (state: TenantState, action: TenantAction): TenantState => {
  switch (action.type) {
    case TenantActionType.SET_CURRENT_TENANT: {
      return {
        ...state,
        currentTenant: action.payload,
      };
    }
    case TenantActionType.SET_AVAILABLE_TENANTS: {
      return {
        ...state,
        availableTenants: action.payload,
      };
    }
    case TenantActionType.SET_LOADING: {
      return {
        ...state,
        loading: action.payload,
      };
    }
    default: {
      throw new Error(`Unhandled action type: ${action}`);
    }
  }
};

export const useTenantReducer: TenantReducer = (initialState: Partial<TenantState> = {}): [TenantState, Dispatch<TenantAction>] => {
  return useReducer(tenantReducer, { ...initialState }, createInitialContentState);
};

const TenantContext = createContext<TenantContextValue | undefined>(undefined);

interface TenantProviderProps {
  initialState?: Partial<TenantState>;
}

export const TenantProvider = ({ children, initialState = {} }: PropsWithChildren<TenantProviderProps>): React.JSX.Element => {
  const [state, dispatch] = useTenantReducer(initialState);
  const { data: session, update: updateSession } = useSession();

  const setLoading: SetLoading = useCallback(
    (loading: boolean): void => {
      dispatch({ type: 'SET_LOADING', payload: loading });
    },
    [dispatch],
  );

  const updateAvailableTenants: UpdateAvailableTenants = useCallback(
    async (availableTenants: Set<unknown>): Promise<Set<unknown>> => {
      setLoading(true);
      dispatch({ type: 'SET_AVAILABLE_TENANTS', payload: availableTenants });
      setLoading(false);
      return availableTenants;
    },
    [dispatch],
  );

  const setCurrentTenant: SetCurrentTenant = useCallback(
    async (tenant: unknown): Promise<unknown | null> => {
      setLoading(true);
      dispatch({ type: 'SET_CURRENT_TENANT', payload: tenant });
      setLoading(false);
      return tenant;
    },
    [dispatch],
  );

  const switchCurrentTenant: SwitchCurrentTenant = useCallback(
    async (tenantId: string): Promise<unknown | null> => {
      // If the session data is not available, we can return early.
      // if (!session?.accessToken) {
      //   return null;
      // }

      setLoading(true);

      try {
        // Find tenant by ID from available tenants
        const tenantArray = Array.from(state.availableTenants);
        const tenant = tenantArray.find((t: any) => t?.id === tenantId);

        if (!tenant) throw new Error('Tenant not found in available tenants');

        // Update the session with new tenant
        await updateSession({ ...session, currentTenant: tenant });

        // Update local state
        dispatch({ type: 'SET_CURRENT_TENANT', payload: tenant });

        return tenant;
      } catch (error) {
        console.error('Failed to switch tenant:', error);
        return null;
      } finally {
        setLoading(false);
      }
    },
    [state.availableTenants, session, updateSession, setLoading],
  );

  const onSessionUpdate: OnSessionUpdate = useCallback(
    async (session: any): Promise<void> => {
      setLoading(true);
      if (!session?.user) {
        await updateAvailableTenants(new Set());
        await setCurrentTenant(null);
      } else {
        await updateAvailableTenants(new Set(session.availableTenants ?? []));
        await setCurrentTenant(session.currentTenant ?? null);
      }
      setLoading(false);
    },
    [updateAvailableTenants, setCurrentTenant, setLoading],
  );

  useEffect(() => {
    if (session !== undefined) void onSessionUpdate(session);
  }, [session]);

  const value = {
    currentTenant: state.currentTenant,
    availableTenants: state.availableTenants,
    switchCurrentTenant,
    updateAvailableTenants,
  };

  return <TenantContext.Provider value={value}>{children}</TenantContext.Provider>;
};

export function useTenant(): TenantContextValue {
  if (!TenantContext) throw new Error('React Context is unavailable in Server Components');

  const context = useContext(TenantContext);

  if (context === undefined) throw new Error('`useSession` must be used within a `TenantProvider`');

  return context;
}

// Hook to make authenticated API requests with tenant context
export function useAuthenticatedApi() {
  const { data: session } = useSession();
  const { currentTenant } = useTenant();

  const makeRequest = useCallback(
    async (endpoint: string, options: RequestInit = {}) => {
      if (!session?.accessToken) {
        throw new Error('No authentication token available');
      }

      const headers = {
        Authorization: `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
        ...(currentTenant && typeof currentTenant === 'object' && currentTenant !== null && 'id' in currentTenant
          ? { 'X-Tenant-Id': (currentTenant as any).id }
          : {}),
        ...options.headers,
      };

      const response = await fetch(endpoint, {
        ...options,
        headers,
      });

      if (!response.ok) {
        throw new Error(`API request failed: ${response.status} ${response.statusText}`);
      }

      return response.json();
    },
    [session, currentTenant],
  );

  const isAuthenticated = !!session?.accessToken;

  return { makeRequest, isAuthenticated };
}
