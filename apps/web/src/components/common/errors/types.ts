'use client';

import { ErrorInfo, ReactNode } from 'react';

/**
 * Error boundary action types using const assertion for type safety
 */
export const ErrorBoundaryActionTypes = {
  REPORT_ERROR: 'REPORT_ERROR',
  CLEAR_ERROR: 'CLEAR_ERROR',
  CLEAR_ALL_ERRORS: 'CLEAR_ALL_ERRORS',
  INCREMENT_RETRY: 'INCREMENT_RETRY',
  UPDATE_CONFIG: 'UPDATE_CONFIG',
  DISABLE_GLOBALLY: 'DISABLE_GLOBALLY',
  ENABLE_GLOBALLY: 'ENABLE_GLOBALLY',
  RESET_STATS: 'RESET_STATS',
} as const;

/**
 * Error levels using const assertion for type safety
 */
export const ErrorLevels = {
  PAGE: 'page',
  COMPONENT: 'component',
  CRITICAL: 'critical',
} as const;

/**
 * Extract type from const object
 */
export type ErrorBoundaryActionType = (typeof ErrorBoundaryActionTypes)[keyof typeof ErrorBoundaryActionTypes];
export type ErrorLevel = (typeof ErrorLevels)[keyof typeof ErrorLevels];

/**
 * Configuration options for error boundary behavior
 */
export interface ErrorBoundaryConfig {
  /** Enable retry functionality for errors */
  enableRetry?: boolean;
  /** Maximum number of retry attempts */
  maxRetries?: number;
  /** Delay between retry attempts in milliseconds */
  retryDelay?: number;
  /** Error level classification */
  level?: ErrorLevel;
  /** Enable error reporting to analytics services */
  reportToAnalytics?: boolean;
  /** Isolate errors to prevent propagation to parent boundaries */
  isolate?: boolean;
}

/**
 * Individual error entry in the error state
 */
export interface ErrorEntry {
  /** Unique identifier for the error */
  id: string;
  /** The actual error object */
  error: Error;
  /** React error info containing component stack */
  errorInfo: ErrorInfo;
  /** Timestamp when error occurred */
  timestamp: number;
  /** Optional boundary identifier where error occurred */
  boundaryId?: string;
  /** Error level classification */
  level: ErrorLevel;
  /** Number of retry attempts for this error */
  retryCount: number;
}

/**
 * Error statistics tracking
 */
export interface ErrorStats {
  /** Total number of errors recorded */
  totalErrors: number;
  /** Count of errors by level */
  errorsByLevel: Record<ErrorLevel, number>;
  /** Count of errors by boundary ID */
  errorsByBoundary: Record<string, number>;
}

/**
 * Complete error boundary state
 */
export interface ErrorState {
  /** Array of all recorded errors */
  errors: ErrorEntry[];
  /** Global configuration for error handling */
  globalConfig: ErrorBoundaryConfig;
  /** Whether error handling is globally disabled */
  isGloballyDisabled: boolean;
  /** Statistics about errors */
  errorStats: ErrorStats;
}

/**
 * Action to report a new error
 */
export interface ReportErrorAction {
  type: typeof ErrorBoundaryActionTypes.REPORT_ERROR;
  payload: {
    error: Error;
    errorInfo: ErrorInfo;
    boundaryId?: string;
    level: ErrorLevel;
  };
}

/**
 * Action to clear a specific error
 */
export interface ClearErrorAction {
  type: typeof ErrorBoundaryActionTypes.CLEAR_ERROR;
  payload: {
    errorId: string;
  };
}

/**
 * Action to clear all errors
 */
export interface ClearAllErrorsAction {
  type: typeof ErrorBoundaryActionTypes.CLEAR_ALL_ERRORS;
}

/**
 * Action to increment retry count for an error
 */
export interface IncrementRetryAction {
  type: typeof ErrorBoundaryActionTypes.INCREMENT_RETRY;
  payload: {
    errorId: string;
  };
}

/**
 * Action to update error boundary configuration
 */
export interface UpdateConfigAction {
  type: typeof ErrorBoundaryActionTypes.UPDATE_CONFIG;
  payload: Partial<ErrorBoundaryConfig>;
}

/**
 * Action to globally disable error handling
 */
export interface DisableGloballyAction {
  type: typeof ErrorBoundaryActionTypes.DISABLE_GLOBALLY;
}

/**
 * Action to globally enable error handling
 */
export interface EnableGloballyAction {
  type: typeof ErrorBoundaryActionTypes.ENABLE_GLOBALLY;
}

/**
 * Action to reset error statistics
 */
export interface ResetStatsAction {
  type: typeof ErrorBoundaryActionTypes.RESET_STATS;
}

/**
 * Union type of all possible error boundary actions
 */
export type ErrorBoundaryAction = ReportErrorAction | ClearErrorAction | ClearAllErrorsAction | IncrementRetryAction | UpdateConfigAction | DisableGloballyAction | EnableGloballyAction | ResetStatsAction;

/**
 * Error boundary reducer function type
 */
export type ErrorBoundaryReducer = (state: ErrorState, action: ErrorBoundaryAction) => ErrorState;

/**
 * Context value provided by ErrorBoundaryProvider
 */
export interface ErrorBoundaryContextValue {
  /** Current error state */
  state: ErrorState;
  /** Dispatch function for actions */
  dispatch: React.Dispatch<ErrorBoundaryAction>;
  /** Report a new error */
  reportError: (error: Error, errorInfo?: ErrorInfo, boundaryId?: string, level?: ErrorLevel) => void;
  /** Clear a specific error by ID */
  clearError: (errorId: string) => void;
  /** Clear all recorded errors */
  clearAllErrors: () => void;
  /** Update error boundary configuration */
  updateConfig: (config: Partial<ErrorBoundaryConfig>) => void;
  /** Get errors for a specific boundary */
  getErrorsByBoundary: (boundaryId: string) => ErrorEntry[];
  /** Get errors by level */
  getErrorsByLevel: (level: ErrorLevel) => ErrorEntry[];
  /** Whether this provider is nested inside another */
  isNested: boolean;
  /** Parent context if nested */
  parentContext?: ErrorBoundaryContextValue;
  /** Propagate error to parent boundary */
  propagateToParent: (error: Error, errorInfo: ErrorInfo) => void;
}

/**
 * Props for ErrorBoundaryProvider component
 */
export interface ErrorBoundaryProviderProps {
  /** Child components to wrap with error boundary */
  children: ReactNode;
  /** Configuration options for error handling */
  config?: ErrorBoundaryConfig;
  /** Initial state for the error boundary */
  initialState?: ErrorState;
  /** Whether to propagate errors to parent boundary (auto-detects if not provided) */
  propagateUp?: boolean;
  /** Unique identifier for this boundary scope (auto-generates if not provided) */
  scopeId?: string;
  /** Force this provider to be treated as nested even if no parent is detected */
  forceNested?: boolean;
}

/**
 * Default error boundary configuration
 */
export const defaultErrorBoundaryConfig: ErrorBoundaryConfig = {
  enableRetry: false,
  maxRetries: 3,
  retryDelay: 1000,
  level: ErrorLevels.COMPONENT,
  reportToAnalytics: true,
  isolate: false,
};

/**
 * Default error statistics
 */
export const defaultErrorStats: ErrorStats = {
  totalErrors: 0,
  errorsByLevel: {
    [ErrorLevels.PAGE]: 0,
    [ErrorLevels.COMPONENT]: 0,
    [ErrorLevels.CRITICAL]: 0,
  },
  errorsByBoundary: {},
};

/**
 * Default error boundary state
 */
export const defaultErrorBoundaryState: ErrorState = {
  errors: [],
  globalConfig: defaultErrorBoundaryConfig,
  isGloballyDisabled: false,
  errorStats: defaultErrorStats,
};
