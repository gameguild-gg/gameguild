'use client';

import ErrorBoundary from '@/components/error-boundary.tsx';

export default function AuthError({ error, reset }: { error: Error; reset: () => void }) {
  return (
    <ErrorBoundary
      error={error}
      reset={reset}
      title="Authentication Error"
      description="An error occurred during authentication. Please try signing in again."
      showDetails
    />
  );
}
