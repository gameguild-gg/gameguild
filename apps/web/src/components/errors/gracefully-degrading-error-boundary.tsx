'use client';

import React, { Component, ErrorInfo, ReactNode, startTransition } from 'react';
import { logger } from '@/lib/logger'; // Assuming you have a logger utility

export interface ErrorBoundaryProps {
  children: ReactNode;
  fallback?: React.ComponentType<ErrorFallbackProps>;
  onError?: (error: Error, errorInfo: ErrorInfo) => void;
  resetOnPropsChange?: boolean;
  resetKeys?: Array<string | number>;
  isolate?: boolean;
  level?: 'page' | 'component' | 'critical';
  enableRecovery?: boolean;
  maxRetries?: number;
  retryDelay?: number;
  reportToAnalytics?: boolean;
}

export interface ErrorFallbackProps {
  error: Error;
  resetError: () => void;
  errorInfo?: ErrorInfo;
}

export interface ErrorBoundaryState {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
  eventId: string | null;
  retryCount: number;
  isRecovering: boolean;
}

// Default fallback component with improved accessibility and design
const DefaultErrorFallback: React.FC<ErrorFallbackProps> = ({ error, resetError }) => {
  // SSR-safe check for window availability
  const isClient = typeof window !== 'undefined';

  return (
    <div
      className="min-h-screen flex items-center justify-center bg-gradient-to-br from-red-50 to-red-100 dark:from-red-950 dark:to-red-900 p-4"
      role="alert"
      aria-labelledby="error-title"
      aria-describedby="error-description"
    >
      <div className="max-w-md w-full bg-white dark:bg-gray-900 rounded-xl shadow-2xl border border-red-200 dark:border-red-800 p-8">
        <div className="flex items-center justify-center w-16 h-16 mx-auto mb-6 bg-red-100 dark:bg-red-900 rounded-full">
          <svg className="w-8 h-8 text-red-600 dark:text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.732 16.5c-.77.833.192 2.5 1.732 2.5z"
            />
          </svg>
        </div>

        <h2 id="error-title" className="text-xl font-bold text-center text-gray-900 dark:text-white mb-4">
          Something went wrong
        </h2>

        <p id="error-description" className="text-gray-600 dark:text-gray-300 text-center mb-6 leading-relaxed">
          We encountered an unexpected error. Please try refreshing the page or contact support if the problem persists.
        </p>

        {process.env.NODE_ENV === 'development' && (
          <details className="mb-6 p-4 bg-gray-50 dark:bg-gray-800 rounded-lg border">
            <summary className="cursor-pointer font-medium text-gray-700 dark:text-gray-300 mb-2">Error Details (Development)</summary>
            <div className="text-sm text-gray-600 dark:text-gray-400 space-y-2">
              <div>
                <strong>Error:</strong> {error.message}
              </div>
              {error.stack && (
                <div>
                  <strong>Stack:</strong>
                  <pre className="mt-1 whitespace-pre-wrap text-xs bg-gray-100 dark:bg-gray-900 p-2 rounded">{error.stack}</pre>
                </div>
              )}
            </div>
          </details>
        )}

        <div className="flex flex-col sm:flex-row gap-3">
          <button
            onClick={resetError}
            className="flex-1 bg-red-600 hover:bg-red-700 focus:ring-4 focus:ring-red-200 dark:focus:ring-red-800 text-white font-medium rounded-lg px-6 py-3 transition-all duration-200 focus:outline-none"
            aria-label="Try again"
          >
            Try Again
          </button>
          {isClient && (
            <button
              onClick={() => window.location.reload()}
              className="flex-1 bg-gray-200 hover:bg-gray-300 dark:bg-gray-700 dark:hover:bg-gray-600 text-gray-900 dark:text-white font-medium rounded-lg px-6 py-3 transition-all duration-200 focus:outline-none focus:ring-4 focus:ring-gray-200 dark:focus:ring-gray-600"
              aria-label="Reload page"
            >
              Reload Page
            </button>
          )}
        </div>
      </div>
    </div>
  );
};

export class GracefullyDegradingErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  private resetTimeoutId: number | null = null;

  constructor(props: ErrorBoundaryProps) {
    super(props);
    this.state = {
      hasError: false,
      error: null,
      errorInfo: null,
      eventId: null,
      retryCount: 0,
      isRecovering: false,
    };
  }

  static getDerivedStateFromError(error: Error): Partial<ErrorBoundaryState> {
    return {
      hasError: true,
      error,
      eventId: `error-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
    };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    this.setState({ errorInfo });

    // Enhanced error reporting with SSR safety
    const errorReport = {
      error: {
        name: error.name,
        message: error.message,
        stack: error.stack,
      },
      errorInfo,
      userAgent: typeof navigator !== 'undefined' ? navigator.userAgent : 'server',
      timestamp: new Date().toISOString(),
      url: typeof window !== 'undefined' ? window.location.href : 'server',
      eventId: this.state.eventId,
      level: this.props.level || 'component',
    };

    // Log error with proper severity
    logger?.error('React Error Boundary caught an error', errorReport);

    // Call custom error handler
    if (this.props.onError) {
      this.props.onError(error, errorInfo);
    }

    // Report to error monitoring service (e.g., Sentry) only on client
    if (typeof window !== 'undefined' && this.props.reportToAnalytics !== false) {
      if (window.Sentry) {
        window.Sentry.captureException(error, {
          contexts: { react: { componentStack: errorInfo.componentStack } },
          tags: { errorBoundary: true, level: this.props.level },
        });
      }
    }
  }

  componentDidUpdate(prevProps: ErrorBoundaryProps) {
    const { resetKeys, resetOnPropsChange } = this.props;
    const { hasError } = this.state;

    if (hasError && prevProps.resetKeys !== resetKeys) {
      if (resetKeys?.some((resetKey, idx) => prevProps.resetKeys?.[idx] !== resetKey)) {
        this.resetErrorBoundary();
      }
    }

    if (hasError && resetOnPropsChange && prevProps.children !== this.props.children) {
      this.resetErrorBoundary();
    }
  }

  componentWillUnmount() {
    if (this.resetTimeoutId) {
      clearTimeout(this.resetTimeoutId);
    }
  }

  resetErrorBoundary = () => {
    if (this.resetTimeoutId) {
      clearTimeout(this.resetTimeoutId);
    }

    startTransition(() => {
      this.setState({
        hasError: false,
        error: null,
        errorInfo: null,
        eventId: null,
        retryCount: 0,
        isRecovering: false,
      });
    });
  };

  render() {
    const { hasError, error, errorInfo } = this.state;
    const { children, fallback: Fallback = DefaultErrorFallback, isolate } = this.props;

    if (hasError && error) {
      const errorFallback = <Fallback error={error} resetError={this.resetErrorBoundary} errorInfo={errorInfo || undefined} />;

      // Isolate error boundary to prevent error propagation
      if (isolate) {
        return <div className="error-boundary-isolation">{errorFallback}</div>;
      }

      return errorFallback;
    }

    return children;
  }
}

export default GracefullyDegradingErrorBoundary;
