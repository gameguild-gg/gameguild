'use client';

import React, { createContext, useContext, ReactNode, ErrorInfo } from 'react';
import { GracefullyDegradingErrorBoundary, ErrorFallbackProps } from './GracefullyDegradingErrorBoundary';
import { RetryableErrorBoundary, RetryableErrorFallbackProps } from './RetryableErrorBoundary';

export interface ErrorBoundaryConfig {
  enableRetry?: boolean;
  maxRetries?: number;
  retryDelay?: number;
  level?: 'page' | 'component' | 'critical';
  reportToAnalytics?: boolean;
  isolate?: boolean;
}

interface ErrorBoundaryContextValue {
  config: ErrorBoundaryConfig;
  reportError: (error: Error) => void;
}

const ErrorBoundaryContext = createContext<ErrorBoundaryContextValue | undefined>(undefined);

export interface ErrorBoundaryProviderProps {
  children: ReactNode;
  config?: ErrorBoundaryConfig;
}

/**
 * Provider that sets up global error boundary configuration
 */
export function ErrorBoundaryProvider({ children, config = {} }: ErrorBoundaryProviderProps) {
  const defaultConfig: ErrorBoundaryConfig = {
    enableRetry: false,
    maxRetries: 3,
    retryDelay: 1000,
    level: 'component',
    reportToAnalytics: true,
    isolate: false,
    ...config,
  };

  const reportError = (error: Error) => {
    // This could be enhanced to use the error reporting utility
    console.error('Manually reported error:', error);
  };

  const contextValue: ErrorBoundaryContextValue = {
    config: defaultConfig,
    reportError,
  };

  return (
    <ErrorBoundaryContext.Provider value={contextValue}>
      {children}
    </ErrorBoundaryContext.Provider>
  );
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
export function SmartErrorBoundary({
  children,
  enableRetry,
  gracefulFallback,
  retryableFallback,
  ...props
}: SmartErrorBoundaryProps) {
  const context = useContext(ErrorBoundaryContext);
  const shouldEnableRetry = enableRetry ?? context?.config.enableRetry ?? false;

  if (shouldEnableRetry) {
    return (
      <RetryableErrorBoundary
        fallback={retryableFallback}
        maxRetries={context?.config.maxRetries ?? 3}
        retryDelay={context?.config.retryDelay ?? 1000}
        level={context?.config.level ?? 'component'}
        reportToAnalytics={context?.config.reportToAnalytics ?? true}
        {...props}
      >
        {children}
      </RetryableErrorBoundary>
    );
  }

  return (
    <GracefullyDegradingErrorBoundary
      fallback={gracefulFallback}
      level={context?.config.level ?? 'component'}
      reportToAnalytics={context?.config.reportToAnalytics ?? true}
      isolate={context?.config.isolate ?? false}
      {...props}
    >
      {children}
    </GracefullyDegradingErrorBoundary>
  );
}

export default SmartErrorBoundary;
