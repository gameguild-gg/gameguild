// Error Boundary Components for Next.js 15+ and React 19
// Optimized for SSR and modern React patterns

export {
  GracefullyDegradingErrorBoundary,
  type ErrorBoundaryProps,
  type ErrorFallbackProps,
  type ErrorBoundaryState,
} from './GracefullyDegradingErrorBoundary';

export {
  RetryableErrorBoundary,
  type RetryableErrorBoundaryProps,
  type RetryableErrorFallbackProps,
} from './RetryableErrorBoundary';

export {
  ErrorBoundaryProvider,
  SmartErrorBoundary,
  useErrorBoundaryContext,
  type ErrorBoundaryConfig,
  type ErrorBoundaryProviderProps,
  type SmartErrorBoundaryProps,
} from './ErrorBoundaryProvider';

// Re-export the hook for convenience
export { useErrorBoundary } from '../../hooks/useErrorBoundary';

// Re-export error reporting utilities
export { errorReporter, ErrorReporter, type ErrorReport } from '../../lib/errorReporting';

// Default export for convenience - use SmartErrorBoundary for most cases
export { SmartErrorBoundary as default } from './ErrorBoundaryProvider';

// Legacy exports for backward compatibility
export { GracefullyDegradingErrorBoundary as ErrorBoundary } from './GracefullyDegradingErrorBoundary';
