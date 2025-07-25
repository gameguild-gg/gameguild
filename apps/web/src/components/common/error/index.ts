export { Error } from './error';
export { NotFound } from './not-found';

// Error Boundary Components for Next.js 15+ and React 19
// Optimized for SSR and modern React patterns

export {
  GracefullyDegradingErrorBoundary,
  type ErrorBoundaryProps,
  type ErrorFallbackProps,
  type ErrorBoundaryState,
} from './gracefully-degrading-error-boundary';

export { RetryableErrorBoundary, type RetryableErrorBoundaryProps, type RetryableErrorFallbackProps } from './retryable-error-boundary';

export { ErrorBoundaryProvider, useErrorBoundaryContext, useErrorBoundaryActions, useErrorBoundaryState } from './error-boundary-provider';

export {
  type ErrorBoundaryConfig,
  type ErrorBoundaryProviderProps,
  type ErrorBoundaryContextValue,
  type ErrorEntry,
  type ErrorLevel,
  type ErrorState,
  type ErrorBoundaryAction,
  type ErrorBoundaryActionType,
} from './types';

// Re-export the hook for convenience (using relative path)
export { useErrorBoundary } from '../../../lib/useErrorBoundary';

// Re-export error reporting utilities (using relative path)
export { errorReporter, ErrorReporter, type ErrorReport } from '../../../lib/errorReporting';

// Default export for convenience - use ErrorBoundaryProvider for most cases
export { ErrorBoundaryProvider as default } from './error-boundary-provider';

// Legacy exports for backward compatibility
export { GracefullyDegradingErrorBoundary as ErrorBoundary } from './gracefully-degrading-error-boundary';
