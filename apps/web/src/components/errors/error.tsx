'use client';

import React, { useEffect } from 'react';
import { ErrorProps } from '@/types';
import Link from 'next/link';
import { useErrorBoundaryContext } from './error-boundary-provider';

export const Error = ({ error, reset }: ErrorProps): React.JSX.Element => {
  const { reportError, state, clearAllErrors } = useErrorBoundaryContext();
  const isDevelopment = process.env.NODE_ENV === 'development';

  useEffect(() => {
    // Report the error to the error boundary context
    reportError(error, { componentStack: error.stack || '' }, 'error-page', 'page');

    // Log the error to an error reporting service.
    console.error('Error caught by error.tsx:', error);
  }, [error, reportError]);

  const handleRetry = () => {
    // Clear any tracked errors before resetting
    clearAllErrors();
    reset();
  };

  const canRetry =
    state.globalConfig.enableRetry &&
    state.errors.filter((e) => e.boundaryId === 'error-page').reduce((max, e) => Math.max(max, e.retryCount), 0) < (state.globalConfig.maxRetries || 3);

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-red-50 to-red-100 dark:from-red-950 dark:to-red-900 p-4">
      <div className="max-w-md w-full bg-white dark:bg-gray-900 rounded-xl shadow-2xl border border-red-200 dark:border-red-800 p-8">
        <div className="flex items-center justify-center w-16 h-16 mx-auto mb-6 bg-red-100 dark:bg-red-900 rounded-full">
          <svg className="w-8 h-8 text-red-600 dark:text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.732 16.5c-.77.833.192 2.5 1.732 2.5z"
            />
          </svg>
        </div>

        <h2 className="text-2xl font-bold text-gray-900 dark:text-white text-center mb-4">Something went wrong!</h2>
        <p className="text-gray-600 dark:text-gray-300 text-center mb-6">We apologize for the inconvenience. An unexpected error has occurred.</p>

        {isDevelopment && (
          <details className="mb-6 p-4 bg-gray-50 dark:bg-gray-800 rounded-lg">
            <summary className="cursor-pointer font-medium text-gray-900 dark:text-white mb-2">Error Details (Development)</summary>
            <div className="space-y-2">
              <div>
                <strong>Message:</strong>
                <pre className="mt-1 whitespace-pre-wrap text-xs bg-gray-100 dark:bg-gray-900 p-2 rounded">{error.message}</pre>
              </div>
              {error.stack && (
                <div>
                  <strong>Stack:</strong>
                  <pre className="mt-1 whitespace-pre-wrap text-xs bg-gray-100 dark:bg-gray-900 p-2 rounded">{error.stack}</pre>
                </div>
              )}
              <div>
                <strong>Error Count:</strong> {state.errorStats.totalErrors}
              </div>
              <div>
                <strong>Page Errors:</strong> {state.errorStats.errorsByLevel.page || 0}
              </div>
            </div>
          </details>
        )}

        <div className="space-y-3">
          {canRetry && (
            <button
              onClick={handleRetry}
              className="w-full bg-red-600 hover:bg-red-700 focus:ring-4 focus:ring-red-200 dark:focus:ring-red-800 text-white font-medium rounded-lg px-6 py-3 transition-all duration-200 focus:outline-none"
            >
              Try Again
            </button>
          )}

          <button
            onClick={() => window.location.reload()}
            className="w-full bg-gray-200 hover:bg-gray-300 dark:bg-gray-700 dark:hover:bg-gray-600 text-gray-900 dark:text-white font-medium rounded-lg px-6 py-3 transition-all duration-200 focus:outline-none focus:ring-4 focus:ring-gray-200 dark:focus:ring-gray-600"
          >
            Reload Page
          </button>

          <Link
            href="/"
            className="block w-full bg-blue-600 hover:bg-blue-700 focus:ring-4 focus:ring-blue-200 dark:focus:ring-blue-800 text-white font-medium rounded-lg px-6 py-3 text-center transition-all duration-200 focus:outline-none"
          >
            Go Home
          </Link>

          <button
            onClick={() => window.history.back()}
            className="w-full bg-gray-100 hover:bg-gray-200 dark:bg-gray-800 dark:hover:bg-gray-700 text-gray-900 dark:text-white font-medium rounded-lg px-6 py-3 transition-all duration-200 focus:outline-none focus:ring-4 focus:ring-gray-200 dark:focus:ring-gray-600"
          >
            Go Back
          </button>
        </div>

        <div className="mt-6 pt-6 border-t border-gray-200 dark:border-gray-700 text-center">
          <p className="text-sm text-gray-500 dark:text-gray-400 mb-2">If the problem persists, please contact support.</p>
          <Link href="/contact" className="text-blue-600 dark:text-blue-400 hover:underline text-sm font-medium">
            Contact Support
          </Link>
        </div>
      </div>
    </div>
  );
};
