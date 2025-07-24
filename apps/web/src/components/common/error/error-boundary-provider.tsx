'use client';

import React, { createContext, useCallback, useContext, useReducer } from 'react';
import {
  ErrorBoundaryAction,
  ErrorBoundaryActionTypes,
  ErrorBoundaryContextValue,
  ErrorBoundaryProviderProps,
  ErrorBoundaryReducer,
  ErrorEntry,
  ErrorLevel,
  ErrorLevels,
  ErrorState,
  defaultErrorBoundaryState,
} from './types';

/**
 * Error boundary reducer function to handle state updates
 */
const errorBoundaryReducer: ErrorBoundaryReducer = (state: ErrorState, action: ErrorBoundaryAction): ErrorState => {
  switch (action.type) {
    case ErrorBoundaryActionTypes.REPORT_ERROR: {
      const { error, errorInfo, boundaryId, level } = action.payload;
      const errorId = `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;

      const newError: ErrorEntry = {
        id: errorId,
        error,
        errorInfo,
        timestamp: Date.now(),
        boundaryId,
        level,
        retryCount: 0,
      };

      return {
        ...state,
        errors: [...state.errors, newError],
        errorStats: {
          totalErrors: state.errorStats.totalErrors + 1,
          errorsByLevel: {
            ...state.errorStats.errorsByLevel,
            [level]: (state.errorStats.errorsByLevel[level] || 0) + 1,
          },
          errorsByBoundary: boundaryId
            ? {
                ...state.errorStats.errorsByBoundary,
                [boundaryId]: (state.errorStats.errorsByBoundary[boundaryId] || 0) + 1,
              }
            : state.errorStats.errorsByBoundary,
        },
      };
    }

    case ErrorBoundaryActionTypes.CLEAR_ERROR:
      return {
        ...state,
        errors: state.errors.filter((error) => error.id !== action.payload.errorId),
      };

    case ErrorBoundaryActionTypes.CLEAR_ALL_ERRORS:
      return {
        ...state,
        errors: [],
      };

    case ErrorBoundaryActionTypes.INCREMENT_RETRY:
      return {
        ...state,
        errors: state.errors.map((error) =>
          error.id === action.payload.errorId
            ? {
                ...error,
                retryCount: error.retryCount + 1,
              }
            : error,
        ),
      };

    case ErrorBoundaryActionTypes.UPDATE_CONFIG:
      return {
        ...state,
        globalConfig: { ...state.globalConfig, ...action.payload },
      };

    case ErrorBoundaryActionTypes.DISABLE_GLOBALLY:
      return {
        ...state,
        isGloballyDisabled: true,
      };

    case ErrorBoundaryActionTypes.ENABLE_GLOBALLY:
      return {
        ...state,
        isGloballyDisabled: false,
      };

    case ErrorBoundaryActionTypes.RESET_STATS:
      return {
        ...state,
        errorStats: {
          totalErrors: 0,
          errorsByLevel: {
            [ErrorLevels.PAGE]: 0,
            [ErrorLevels.COMPONENT]: 0,
            [ErrorLevels.CRITICAL]: 0,
          },
          errorsByBoundary: {},
        },
      };

    default: {
      console.error(`Unhandled action type: ${(action as any).type}`);
      return state;
    }
  }
};

/**
 * Default context to handle errors when context is used outside the provider
 */
const defaultContextValue: ErrorBoundaryContextValue = {
  state: defaultErrorBoundaryState,
  dispatch: () => console.error('ErrorBoundaryContext used outside of provider'),
  reportError: () => console.error('ErrorBoundaryContext used outside of provider'),
  clearError: () => console.error('ErrorBoundaryContext used outside of provider'),
  clearAllErrors: () => console.error('ErrorBoundaryContext used outside of provider'),
  updateConfig: () => console.error('ErrorBoundaryContext used outside of provider'),
  getErrorsByBoundary: () => [],
  getErrorsByLevel: () => [],
  isNested: false,
  propagateToParent: () => {},
};

const ErrorBoundaryContext = createContext<ErrorBoundaryContextValue>(defaultContextValue);

/**
 * Initialize error boundary state with potential localStorage/cookie values
 */
const createInitialErrorBoundaryState = (initialState: ErrorState): ErrorState => {
  let enhancedState = { ...initialState };

  // Check for persisted configuration in localStorage (client-side only)
  if (typeof window !== 'undefined') {
    try {
      const persistedConfig = localStorage.getItem('errorBoundaryConfig');
      if (persistedConfig) {
        const parsedConfig = JSON.parse(persistedConfig);
        enhancedState = {
          ...enhancedState,
          globalConfig: {
            ...enhancedState.globalConfig,
            ...parsedConfig,
          },
        };
      }

      // Check for globally disabled state
      const isDisabled = localStorage.getItem('errorBoundaryGloballyDisabled');
      if (isDisabled === 'true') {
        enhancedState.isGloballyDisabled = true;
      }
    } catch (error) {
      console.warn('Failed to load error boundary configuration from localStorage:', error);
    }
  }

  return enhancedState;
};

/**
 * Generate a unique scope ID based on component stack or random ID
 */
const generateScopeId = (): string => {
  // Try to get component name from stack trace for better debugging
  const stack = new Error().stack;
  const componentMatch = stack?.match(/at (\w+)/g)?.[2]; // Get calling component
  const componentName = componentMatch?.replace('at ', '') || 'unknown';
  const randomId = Math.random().toString(36).substr(2, 6);
  return `${componentName}-${randomId}`;
};

/**
 * Provider that manages global error boundary state using a reducer
 */
export function ErrorBoundaryProvider({
  children,
  config = {},
  initialState = defaultErrorBoundaryState,
  propagateUp, // Now optional - auto-detects
  scopeId, // Now optional - auto-generates
  forceNested = false,
}: ErrorBoundaryProviderProps) {
  // Auto-detect if this is a nested provider
  const existingContext = useContext(ErrorBoundaryContext);
  const hasParentProvider = existingContext && existingContext !== defaultContextValue;
  const isNested = forceNested || hasParentProvider;

  // Auto-generate scopeId if not provided
  const autoScopeId = scopeId || (isNested ? generateScopeId() : 'root');

  // Auto-set propagateUp based on nesting (defaults to true for nested)
  const shouldPropagateUp = propagateUp ?? (isNested ? true : false);

  // Get parent context if this is a nested provider
  const parentContext = isNested ? existingContext : undefined;

  // Merge configurations: parent config < initial state < provided config
  const mergedConfig =
    isNested && parentContext
      ? {
          ...parentContext.state.globalConfig,
          ...initialState.globalConfig,
          ...config,
        }
      : {
          ...initialState.globalConfig,
          ...config,
        };

  const stateWithConfig: ErrorState = {
    ...initialState,
    globalConfig: mergedConfig,
  };

  const [state, dispatch] = useReducer(
    errorBoundaryReducer,
    stateWithConfig,
    // Only apply localStorage initialization for root provider
    isNested ? (state) => state : createInitialErrorBoundaryState,
  );

  const reportError = useCallback(
    (error: Error, errorInfo = { componentStack: '' }, boundaryId?: string, level: ErrorLevel = ErrorLevels.COMPONENT) => {
      // Report to local state
      dispatch({
        type: ErrorBoundaryActionTypes.REPORT_ERROR,
        payload: { error, errorInfo, boundaryId: boundaryId || autoScopeId, level },
      });

      // Propagate to parent if enabled and not isolated
      if (isNested && shouldPropagateUp && parentContext && !state.globalConfig.isolate) {
        parentContext.reportError(error, errorInfo, boundaryId || autoScopeId, level);
      }

      // Enhanced error reporting
      if (state.globalConfig.reportToAnalytics) {
        console.error(`Error reported to analytics ${isNested ? `(nested: ${autoScopeId})` : '(global)'}:`, {
          error: error.message,
          stack: error.stack,
          componentStack: errorInfo.componentStack,
          boundaryId: boundaryId || autoScopeId,
          level,
          timestamp: new Date().toISOString(),
          scope: autoScopeId,
          isNested,
        });
      }
    },
    [state.globalConfig.reportToAnalytics, state.globalConfig.isolate, isNested, shouldPropagateUp, parentContext, autoScopeId],
  );

  const propagateToParent = useCallback(
    (error: Error, errorInfo) => {
      if (isNested && parentContext && shouldPropagateUp) {
        parentContext.propagateToParent(error, errorInfo);
      }
    },
    [isNested, parentContext, shouldPropagateUp],
  );

  const clearError = useCallback((errorId: string) => {
    dispatch({ type: ErrorBoundaryActionTypes.CLEAR_ERROR, payload: { errorId } });
  }, []);

  const clearAllErrors = useCallback(() => {
    dispatch({ type: ErrorBoundaryActionTypes.CLEAR_ALL_ERRORS });
  }, []);

  const updateConfig = useCallback(
    (newConfig) => {
      dispatch({ type: ErrorBoundaryActionTypes.UPDATE_CONFIG, payload: newConfig });

      // Persist configuration to localStorage (client-side only)
      if (typeof window !== 'undefined') {
        try {
          const mergedConfig = { ...state.globalConfig, ...newConfig };
          localStorage.setItem('errorBoundaryConfig', JSON.stringify(mergedConfig));
        } catch (error) {
          console.warn('Failed to persist error boundary configuration to localStorage:', error);
        }
      }
    },
    [state.globalConfig],
  );

  const getErrorsByBoundary = useCallback(
    (boundaryId: string) => {
      return state.errors.filter((error) => error.boundaryId === boundaryId);
    },
    [state.errors],
  );

  const getErrorsByLevel = useCallback(
    (level: ErrorLevel) => {
      return state.errors.filter((error) => error.level === level);
    },
    [state.errors],
  );

  const contextValue: ErrorBoundaryContextValue = {
    state,
    dispatch,
    reportError,
    clearError,
    clearAllErrors,
    updateConfig,
    getErrorsByBoundary,
    getErrorsByLevel,
    isNested,
    parentContext,
    propagateToParent,
  };

  return <ErrorBoundaryContext.Provider value={contextValue}>{children}</ErrorBoundaryContext.Provider>;
}

/**
 * Hook to access error boundary context
 */
export function useErrorBoundaryContext() {
  const context = useContext(ErrorBoundaryContext);
  if (!context) {
    throw new Error('useErrorBoundaryContext must be used within ErrorBoundaryProvider');
  }
  return context;
}

/**
 * Hook for dispatching error boundary actions
 */
export function useErrorBoundaryActions() {
  const { dispatch, reportError, clearError, clearAllErrors, updateConfig } = useErrorBoundaryContext();
  return { dispatch, reportError, clearError, clearAllErrors, updateConfig };
}

/**
 * Hook for accessing error boundary state
 */
export function useErrorBoundaryState() {
  const { state, getErrorsByBoundary, getErrorsByLevel } = useErrorBoundaryContext();
  return { state, getErrorsByBoundary, getErrorsByLevel };
}
