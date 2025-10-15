'use client';

import React, { PropsWithChildren, useEffect, useState } from 'react';
import Link from 'next/link';
import { GitHubIssueModal } from '@/components/ui/github-issue-modal';
import { usePathname } from 'next/navigation';

// Define ErrorProps locally to avoid import issues
interface ErrorProps {
  error: Error & { digest?: string };
  reset: () => void;
}

// Define types locally
interface ErrorEntry {
  id: string;
  error: Error;
  timestamp: number;
  boundaryId: string;
  level: string;
  retryCount: number;
}

// Create a simple error boundary context hook that doesn't require external dependencies
function useErrorBoundaryContext() {
  return {
    reportError: (error: Error, errorInfo?: { componentStack?: string }, boundaryId?: string, level?: string) => {
      // Only log if error has content
      if (error && (error.message || error.stack || error.name)) {
        console.error('Error reported:', { error, errorInfo, boundaryId, level });
      } else {
        console.warn('Empty or invalid error object passed to reportError:', error);
      }
    },
    state: {
      globalConfig: {
        enableRetry: true,
        maxRetries: 3,
      },
      errors: [] as ErrorEntry[],
      errorStats: {
        totalErrors: 0,
        errorsByLevel: { page: 0, component: 0, critical: 0 },
      },
    },
    clearAllErrors: () => {
      console.log('Clearing all errors');
    },
  };
}

export const Error = ({ error, reset, children }: PropsWithChildren<ErrorProps>): React.JSX.Element => {
  const isDevelopment = process.env.NODE_ENV === 'development';
  const { reportError, state, clearAllErrors } = useErrorBoundaryContext();
  const [isGitHubModalOpen, setIsGitHubModalOpen] = useState(false);
  const pathname = usePathname();

  useEffect(() => {
    // Check if error is empty or invalid before reporting
    if (!error || (typeof error === 'object' && Object.keys(error).length === 0)) {
      console.error('Empty error object received in error boundary:', error);
      return;
    }

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

  const canRetry = state.globalConfig.enableRetry && state.errors.filter((e) => e.boundaryId === 'error-page').reduce((max, e) => Math.max(max, e.retryCount), 0) < (state.globalConfig.maxRetries || 3);

  return (
    <div className="flex flex-col flex-1 relative items-center justify-center">
      {children && <>{children}</>}
      <div className="max-w-md w-full bg-white dark:bg-gray-900 rounded-xl shadow-2xl border border-red-200 dark:border-red-800 p-8">
        <div className="flex items-center justify-center w-16 h-16 mx-auto mb-6 bg-red-100 dark:bg-red-900 rounded-full">
          <svg className="w-8 h-8 text-red-600 dark:text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.732 16.5c-.77.833.192 2.5 1.732 2.5z" />
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

          <button
            onClick={() => setIsGitHubModalOpen(true)}
            className="w-full bg-gradient-to-r from-orange-500 to-red-500 hover:from-orange-600 hover:to-red-600 focus:ring-4 focus:ring-orange-200 dark:focus:ring-orange-800 text-white font-bold rounded-lg px-6 py-4 transition-all duration-300 focus:outline-none flex items-center justify-center gap-3 transform hover:scale-105 hover:shadow-xl active:scale-95 animate-pulse hover:animate-none border-2 border-orange-400 hover:border-orange-300"
          >
            <svg className="w-6 h-6 animate-bounce" fill="currentColor" viewBox="0 0 24 24">
              <path d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z"/>
            </svg>
            <span className="text-lg">Report Issue on GitHub</span>
            <svg className="w-4 h-4 animate-ping" fill="currentColor" viewBox="0 0 20 20">
              <circle cx="10" cy="10" r="3"/>
            </svg>
          </button>
        </div>
      </div>
      
      <GitHubIssueModal
        isOpen={isGitHubModalOpen}
        onClose={() => setIsGitHubModalOpen(false)}
        route={pathname}
      />
    </div>
  );
};
