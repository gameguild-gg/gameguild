'use client';

import React, { Component, ErrorInfo, ReactNode, startTransition } from 'react';

// Simple logger implementation
const logger = {
  error: (message: string, data?: unknown) => {
    console.error(message, data);
  },
};

// Simple error report interfaces and implementation
interface ErrorReport {
  id: string;
  error: Error;
  timestamp: number;
  level: string;
  componentStack?: string;
}

const errorReporter = {
  createErrorReport: (error: Error, context?: { componentStack?: string }, level = 'component') => ({
    id: `error-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
    error,
    timestamp: Date.now(),
    level,
    componentStack: context?.componentStack,
  }),
  reportError: async (report: ErrorReport) => {
    console.error('Error Report:', report);
  },
};

export interface RetryableErrorBoundaryProps {
  children: ReactNode;
  fallback?: React.ComponentType<RetryableErrorFallbackProps>;
  onError?: (error: Error, errorInfo: ErrorInfo) => void;
  maxRetries?: number;
  retryDelay?: number;
  level?: 'page' | 'component' | 'critical';
  resetKeys?: Array<string | number>;
  reportToAnalytics?: boolean;
}

export interface RetryableErrorFallbackProps {
  error: Error;
  resetError: () => void;
  retry: () => void;
  retryCount: number;
  maxRetries: number;
  isRetrying: boolean;
  canRetry: boolean;
}

interface RetryableErrorBoundaryState {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
  retryCount: number;
  isRetrying: boolean;
  errorReport: ErrorReport | null;
}

const DefaultRetryableFallback: React.FC<RetryableErrorFallbackProps> = ({ error, resetError, retry, retryCount, maxRetries, isRetrying, canRetry }) => {
  const isClient = typeof window !== 'undefined';

  return (
    <div
      className="min-h-[400px] flex items-center justify-center bg-gradient-to-br from-yellow-50 to-orange-100 dark:from-yellow-950 dark:to-orange-900 p-4 rounded-lg border border-yellow-200 dark:border-yellow-800"
      role="alert"
      aria-labelledby="retry-error-title"
      aria-describedby="retry-error-description"
    >
      <div className="max-w-md w-full bg-white dark:bg-gray-900 rounded-xl shadow-xl border border-yellow-200 dark:border-yellow-800 p-6">
        <div className="flex items-center justify-center w-12 h-12 mx-auto mb-4 bg-yellow-100 dark:bg-yellow-900 rounded-full">
          <svg className="w-6 h-6 text-yellow-600 dark:text-yellow-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
        </div>

        <h3 id="retry-error-title" className="text-lg font-semibold text-center text-gray-900 dark:text-white mb-3">
          Component Error
        </h3>

        <p id="retry-error-description" className="text-gray-600 dark:text-gray-300 text-center mb-4 text-sm">
          This component encountered an error. {canRetry ? 'You can try again.' : 'Maximum retries reached.'}
        </p>

        {process.env.NODE_ENV === 'development' && (
          <details className="mb-4 p-3 bg-gray-50 dark:bg-gray-800 rounded-lg text-xs">
            <summary className="cursor-pointer font-medium text-gray-700 dark:text-gray-300">Error Details</summary>
            <div className="mt-2 text-gray-600 dark:text-gray-400">
              <div>
                <strong>Error:</strong> {error.message}
              </div>
              <div>
                <strong>Retry Count:</strong> {retryCount}/{maxRetries}
              </div>
            </div>
          </details>
        )}

        <div className="flex flex-col gap-2">
          {canRetry && (
            <button
              onClick={retry}
              disabled={isRetrying}
              className="w-full bg-yellow-600 hover:bg-yellow-700 disabled:bg-yellow-400 disabled:cursor-not-allowed text-white font-medium rounded-lg px-4 py-2 transition-all duration-200 focus:outline-none focus:ring-4 focus:ring-yellow-200 dark:focus:ring-yellow-800"
            >
              {isRetrying ? 'Retrying...' : `Retry (${maxRetries - retryCount} left)`}
            </button>
          )}

          <div className="flex gap-2">
            <button
              onClick={resetError}
              className="flex-1 bg-gray-200 hover:bg-gray-300 dark:bg-gray-700 dark:hover:bg-gray-600 text-gray-900 dark:text-white font-medium rounded-lg px-4 py-2 transition-all duration-200 focus:outline-none focus:ring-4 focus:ring-gray-200 dark:focus:ring-gray-600"
            >
              Reset
            </button>
            {isClient && (
              <button
                onClick={() => window.location.reload()}
                className="flex-1 bg-gray-500 hover:bg-gray-600 text-white font-medium rounded-lg px-4 py-2 transition-all duration-200 focus:outline-none focus:ring-4 focus:ring-gray-200"
              >
                Reload
              </button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export class RetryableErrorBoundary extends Component<RetryableErrorBoundaryProps, RetryableErrorBoundaryState> {
  private retryTimeoutId: number | null = null;

  constructor(props: RetryableErrorBoundaryProps) {
    super(props);
    this.state = {
      hasError: false,
      error: null,
      errorInfo: null,
      retryCount: 0,
      isRetrying: false,
      errorReport: null,
    };
  }

  static getDerivedStateFromError(error: Error): Partial<RetryableErrorBoundaryState> {
    return {
      hasError: true,
      error,
    };
  }

  async componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    const { level = 'component', onError, reportToAnalytics = true } = this.props;

    // Create error report
    const errorReport = errorReporter.createErrorReport(error, { componentStack: errorInfo.componentStack || undefined }, level);

    this.setState({
      errorInfo,
      errorReport,
    });

    // Log error
    logger?.error('Retryable Error Boundary caught an error', JSON.stringify(errorReport));

    // Call custom error handler
    if (onError) {
      onError(error, errorInfo);
    }

    // Report to analytics
    if (reportToAnalytics) {
      await errorReporter.reportError(errorReport);
    }
  }

  componentDidUpdate(prevProps: RetryableErrorBoundaryProps) {
    const { resetKeys } = this.props;
    const { hasError } = this.state;

    if (hasError && prevProps.resetKeys !== resetKeys) {
      if (resetKeys?.some((resetKey, idx) => prevProps.resetKeys?.[idx] !== resetKey)) {
        this.resetErrorBoundary();
      }
    }
  }

  componentWillUnmount() {
    if (this.retryTimeoutId) {
      clearTimeout(this.retryTimeoutId);
    }
  }

  resetErrorBoundary = () => {
    if (this.retryTimeoutId) {
      clearTimeout(this.retryTimeoutId);
    }

    startTransition(() => {
      this.setState({
        hasError: false,
        error: null,
        errorInfo: null,
        retryCount: 0,
        isRetrying: false,
        errorReport: null,
      });
    });
  };

  retry = () => {
    const { maxRetries = 3, retryDelay = 1000 } = this.props;
    const { retryCount } = this.state;

    if (retryCount >= maxRetries) {
      return;
    }

    this.setState({ isRetrying: true });

    this.retryTimeoutId = window.setTimeout(() => {
      startTransition(() => {
        this.setState({
          hasError: false,
          error: null,
          errorInfo: null,
          retryCount: retryCount + 1,
          isRetrying: false,
        });
      });
    }, retryDelay);
  };

  render() {
    const { hasError, error, retryCount, isRetrying } = this.state;
    const { children, fallback: Fallback = DefaultRetryableFallback, maxRetries = 3 } = this.props;

    if (hasError && error) {
      const canRetry = retryCount < maxRetries;

      return (
        <Fallback
          error={error}
          resetError={this.resetErrorBoundary}
          retry={this.retry}
          retryCount={retryCount}
          maxRetries={maxRetries}
          isRetrying={isRetrying}
          canRetry={canRetry}
        />
      );
    }

    return children;
  }
}

export default RetryableErrorBoundary;
