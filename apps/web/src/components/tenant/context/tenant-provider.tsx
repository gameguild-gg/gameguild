'use client';

import React, { createContext, Dispatch, PropsWithChildren, useCallback, useContext, useEffect, useReducer } from 'react';
import { useSession } from 'next-auth/react';
import type { TenantResponse } from '@/lib/tenants';

export const TenantActionType = {
  SET_CURRENT_TENANT: 'SET_CURRENT_TENANT',
  SET_AVAILABLE_TENANTS: 'SET_AVAILABLE_TENANTS',
  SET_LOADING: 'SET_LOADING',
  SET_ERROR: 'SET_ERROR',
  CLEAR_ERROR: 'CLEAR_ERROR',
  INVALIDATE_CACHE: 'INVALIDATE_CACHE',
} as const;

export type TenantActionType = (typeof TenantActionType)[keyof typeof TenantActionType];

export type TenantAction =
  | { type: typeof TenantActionType.SET_CURRENT_TENANT; payload: TenantResponse | null }
  | { type: typeof TenantActionType.SET_AVAILABLE_TENANTS; payload: TenantResponse[] }
  | { type: typeof TenantActionType.SET_LOADING; payload: boolean }
  | { type: typeof TenantActionType.SET_ERROR; payload: string }
  | { type: typeof TenantActionType.CLEAR_ERROR }
  | { type: typeof TenantActionType.INVALIDATE_CACHE };

export type SwitchCurrentTenant = (tenantId: string) => Promise<TenantResponse | null>;
export type SetCurrentTenant = (tenant: TenantResponse | null) => Promise<TenantResponse | null>;
export type OnSessionUpdate = (session: unknown) => Promise<void>;
export type UpdateAvailableTenants = (availableTenants: TenantResponse[]) => Promise<TenantResponse[]>;
export type SetLoading = (loading: boolean) => void;
export type SetError = (error: string) => void;
export type ClearError = () => void;
export type TenantReducer = (initialState: Partial<TenantState>) => [TenantState, Dispatch<TenantAction>];

interface TenantContextValue {
  currentTenant: TenantResponse | null;
  availableTenants: TenantResponse[];
  loading: boolean;
  error: string | null;
  switchCurrentTenant: SwitchCurrentTenant;
  updateAvailableTenants: UpdateAvailableTenants;
  setError: SetError;
  clearError: ClearError;
}

export interface TenantState {
  currentTenant: TenantResponse | null;
  availableTenants: TenantResponse[];
  loading: boolean;
  error: string | null;
}

export const defaultTenantState: TenantState = {
  currentTenant: null,
  availableTenants: [],
  loading: false,
  error: null,
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
        error: null,
      };
    }
    case TenantActionType.SET_AVAILABLE_TENANTS: {
      return {
        ...state,
        availableTenants: action.payload,
        error: null,
      };
    }
    case TenantActionType.SET_LOADING: {
      return {
        ...state,
        loading: action.payload,
      };
    }
    case TenantActionType.SET_ERROR: {
      return {
        ...state,
        error: action.payload,
        loading: false,
      };
    }
    case TenantActionType.CLEAR_ERROR: {
      return {
        ...state,
        error: null,
      };
    }
    case TenantActionType.INVALIDATE_CACHE: {
      return {
        ...state,
        availableTenants: [],
        currentTenant: null,
        error: null,
      };
    }
    default: {
      throw new Error(`Unhandled action type: ${(action as { type: string }).type}`);
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

  const setError: SetError = useCallback(
    (error: string): void => {
      dispatch({ type: 'SET_ERROR', payload: error });
    },
    [dispatch],
  );

  const clearError: ClearError = useCallback((): void => {
    dispatch({ type: 'CLEAR_ERROR' });
  }, [dispatch]);

  const updateAvailableTenants: UpdateAvailableTenants = useCallback(
    async (availableTenants: TenantResponse[]): Promise<TenantResponse[]> => {
      setLoading(true);
      dispatch({ type: 'SET_AVAILABLE_TENANTS', payload: availableTenants });
      setLoading(false);
      return availableTenants;
    },
    [dispatch, setLoading],
  );

  const setCurrentTenant: SetCurrentTenant = useCallback(
    async (tenant: TenantResponse | null): Promise<TenantResponse | null> => {
      setLoading(true);
      dispatch({ type: 'SET_CURRENT_TENANT', payload: tenant });
      setLoading(false);
      return tenant;
    },
    [dispatch, setLoading],
  );

  const switchCurrentTenant: SwitchCurrentTenant = useCallback(
    async (tenantId: string): Promise<TenantResponse | null> => {
      setLoading(true);

      try {
        // Find a tenant by ID from available tenants
        const tenant = state.availableTenants.find((t) => t.id === tenantId);

        if (!tenant) {
          setError('Tenant not found in available tenants');
          return null;
        }

        // Update the session with a new tenant
        await updateSession({ ...session, currentTenant: tenant });

        // Update local state
        dispatch({ type: 'SET_CURRENT_TENANT', payload: tenant });

        return tenant;
      } catch (error) {
        const errorMessage = error instanceof Error ? error.message : 'Failed to switch tenant';
        setError(errorMessage);
        console.error('Failed to switch tenant:', error);
        return null;
      } finally {
        setLoading(false);
      }
    },
    [state.availableTenants, session, updateSession, setLoading, setError, dispatch],
  );

  const onSessionUpdate: OnSessionUpdate = useCallback(
    async (sessionData: unknown): Promise<void> => {
      const session = sessionData as {
        user?: unknown;
        availableTenants?: TenantResponse[];
        currentTenant?: TenantResponse;
      };

      setLoading(true);

      try {
        if (!session?.user) {
          await updateAvailableTenants([]);
          await setCurrentTenant(null);
        } else {
          await updateAvailableTenants(session.availableTenants ?? []);
          await setCurrentTenant(session.currentTenant ?? null);
        }
      } catch (error) {
        const errorMessage = error instanceof Error ? error.message : 'Failed to update session';
        setError(errorMessage);
      } finally {
        setLoading(false);
      }
    },
    [updateAvailableTenants, setCurrentTenant, setLoading, setError],
  );

  useEffect(() => {
    if (session !== undefined) void onSessionUpdate(session);
  }, [session, onSessionUpdate]);

  const value: TenantContextValue = {
    currentTenant: state.currentTenant,
    availableTenants: state.availableTenants,
    loading: state.loading,
    error: state.error,
    switchCurrentTenant,
    updateAvailableTenants,
    setError,
    clearError,
  };

  return <TenantContext.Provider value={value}>{children}</TenantContext.Provider>;
};

export function useTenant(): TenantContextValue {
  if (!TenantContext) throw new Error('React Context is unavailable in Server Components');

  const context = useContext(TenantContext);

  if (context === undefined) throw new Error('`useTenant` must be used within a `TenantProvider`');

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
        ...(currentTenant ? { 'X-Tenant-Id': currentTenant.id } : {}),
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
  const tenantId = currentTenant?.id;

  return { makeRequest, isAuthenticated, tenantId, currentTenant };
}
