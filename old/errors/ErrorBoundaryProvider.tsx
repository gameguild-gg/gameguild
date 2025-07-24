'use client';

import React, { createContext, ErrorInfo, ReactNode, useCallback, useContext, useReducer } from 'react';
import { ErrorFallbackProps, GracefullyDegradingErrorBoundary } from './GracefullyDegradingErrorBoundary';
import { RetryableErrorBoundary, RetryableErrorFallbackProps } from './RetryableErrorBoundary';

export interface ErrorBoundaryConfig {
  enableRetry?: boolean;
  maxRetries?: number;
  retryDelay?: number;
  level?: 'page' | 'component' | 'critical';
  reportToAnalytics?: boolean;
  isolate?: boolean;
}

export interface ErrorState {
  errors: Array<{
    id: string;
    error: Error;
    errorInfo: ErrorInfo;
    timestamp: number;
    boundaryId?: string;
    level: 'page' | 'component' | 'critical';
    retryCount: number;
  }>;
  globalConfig: ErrorBoundaryConfig;
  isGloballyDisabled: boolean;
  errorStats: {
    totalErrors: number;
    errorsByLevel: Record<string, number>;
    errorsByBoundary: Record<string, number>;
  };
}

export type ErrorBoundaryAction =
  | {
      type: 'REPORT_ERROR';
      payload: { error: Error; errorInfo: ErrorInfo; boundaryId?: string; level: 'page' | 'component' | 'critical' };
    }
  | { type: 'CLEAR_ERROR'; payload: { errorId: string } }
  | { type: 'CLEAR_ALL_ERRORS' }
  | { type: 'INCREMENT_RETRY'; payload: { errorId: string } }
  | { type: 'UPDATE_CONFIG'; payload: Partial<ErrorBoundaryConfig> }
  | { type: 'DISABLE_GLOBALLY' }
  | { type: 'ENABLE_GLOBALLY' }
  | { type: 'RESET_STATS' };

/**
 * Default error boundary state
 */
const defaultErrorBoundaryState: ErrorState = {
  errors: [],
  globalConfig: {
    enableRetry: false,
    maxRetries: 3,
    retryDelay: 1000,
    level: 'component',
    reportToAnalytics: true,
    isolate: false,
  },
  isGloballyDisabled: false,
  errorStats: {
    totalErrors: 0,
    errorsByLevel: {},
    errorsByBoundary: {},
  },
};

/**
 * Error boundary reducer function to handle state updates
 */
function errorBoundaryReducer(state: ErrorState, action: ErrorBoundaryAction): ErrorState {
  switch (action.type) {
    case 'REPORT_ERROR': {
      const { error, errorInfo, boundaryId, level } = action.payload;
      const errorId = `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;

      const newError = {
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

    case 'CLEAR_ERROR':
      return {
        ...state,
        errors: state.errors.filter((error) => error.id !== action.payload.errorId),
      };

    case 'CLEAR_ALL_ERRORS':
      return {
        ...state,
        errors: [],
      };

    case 'INCREMENT_RETRY':
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

    case 'UPDATE_CONFIG':
      return {
        ...state,
        globalConfig: { ...state.globalConfig, ...action.payload },
      };

    case 'DISABLE_GLOBALLY':
      return {
        ...state,
        isGloballyDisabled: true,
      };

    case 'ENABLE_GLOBALLY':
      return {
        ...state,
        isGloballyDisabled: false,
      };

    case 'RESET_STATS':
      return {
        ...state,
        errorStats: {
          totalErrors: 0,
          errorsByLevel: {},
          errorsByBoundary: {},
        },
      };

    default: {
      console.error(`Unhandled action type: ${(action as any).type}`);
      return state;
    }
  }
}

interface ErrorBoundaryContextValue {
  state: ErrorState;
  dispatch: React.Dispatch<ErrorBoundaryAction>;
  // Convenience methods
  reportError: (error: Error, errorInfo?: ErrorInfo, boundaryId?: string, level?: 'page' | 'component' | 'critical') => void;
  clearError: (errorId: string) => void;
  clearAllErrors: () => void;
  updateConfig: (config: Partial<ErrorBoundaryConfig>) => void;
  getErrorsByBoundary: (boundaryId: string) => ErrorState['errors'];
  getErrorsByLevel: (level: 'page' | 'component' | 'critical') => ErrorState['errors'];
}

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
};

const ErrorBoundaryContext = createContext<ErrorBoundaryContextValue>(defaultContextValue);

export interface ErrorBoundaryProviderProps {
  children: ReactNode;
  config?: ErrorBoundaryConfig;
  initialState?: ErrorState;
}

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
 * Provider that manages global error boundary state using a reducer
 */
export function ErrorBoundaryProvider({ children, config = {}, initialState = defaultErrorBoundaryState }: ErrorBoundaryProviderProps) {
  // Merge provided config with initial state
  const stateWithConfig: ErrorState = {
    ...initialState,
    globalConfig: {
      ...initialState.globalConfig,
      ...config,
    },
  };

  const [state, dispatch] = useReducer(errorBoundaryReducer, stateWithConfig, createInitialErrorBoundaryState);

  const reportError = useCallback(
    (error: Error, errorInfo: ErrorInfo = { componentStack: '' }, boundaryId?: string, level: 'page' | 'component' | 'critical' = 'component') => {
      dispatch({
        type: 'REPORT_ERROR',
        payload: { error, errorInfo, boundaryId, level },
      });

      // Enhanced error reporting
      if (state.globalConfig.reportToAnalytics) {
        console.error('Error reported to analytics:', {
          error: error.message,
          stack: error.stack,
          componentStack: errorInfo.componentStack,
          boundaryId,
          level,
          timestamp: new Date().toISOString(),
        });
      }
    },
    [state.globalConfig.reportToAnalytics],
  );

  const clearError = useCallback((errorId: string) => {
    dispatch({ type: 'CLEAR_ERROR', payload: { errorId } });
  }, []);

  const clearAllErrors = useCallback(() => {
    dispatch({ type: 'CLEAR_ALL_ERRORS' });
  }, []);

  const updateConfig = useCallback(
    (newConfig: Partial<ErrorBoundaryConfig>) => {
      dispatch({ type: 'UPDATE_CONFIG', payload: newConfig });

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
    (level: 'page' | 'component' | 'critical') => {
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

export interface SmartErrorBoundaryProps {
  children: ReactNode;
  gracefulFallback?: React.ComponentType<ErrorFallbackProps>;
  retryableFallback?: React.ComponentType<RetryableErrorFallbackProps>;
  onError?: (error: Error, errorInfo: ErrorInfo) => void;
  resetKeys?: Array<string | number>;
  level?: 'page' | 'component' | 'critical';
  enableRetry?: boolean;
  maxRetries?: number;
  retryDelay?: number;
  isolate?: boolean;
}

/**
 * Smart error boundary that chooses between retryable and graceful error handling
 */
export function SmartErrorBoundary({ children, enableRetry, gracefulFallback, retryableFallback, ...props }: SmartErrorBoundaryProps) {
  const context = useContext(ErrorBoundaryContext);
  const shouldEnableRetry = enableRetry ?? context?.state.globalConfig.enableRetry ?? false;

  if (shouldEnableRetry) {
    return (
      <RetryableErrorBoundary
        fallback={retryableFallback}
        maxRetries={context?.state.globalConfig.maxRetries ?? 3}
        retryDelay={context?.state.globalConfig.retryDelay ?? 1000}
        level={context?.state.globalConfig.level ?? 'component'}
        reportToAnalytics={context?.state.globalConfig.reportToAnalytics ?? true}
        {...props}
      >
        {children}
      </RetryableErrorBoundary>
    );
  }

  return (
    <GracefullyDegradingErrorBoundary
      fallback={gracefulFallback}
      level={context?.state.globalConfig.level ?? 'component'}
      reportToAnalytics={context?.state.globalConfig.reportToAnalytics ?? true}
      isolate={context?.state.globalConfig.isolate ?? false}
      {...props}
    >
      {children}
    </GracefullyDegradingErrorBoundary>
  );
}

export default SmartErrorBoundary;
