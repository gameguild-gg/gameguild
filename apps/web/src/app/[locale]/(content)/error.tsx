'use client';

import ErrorBoundary from '@/components/error-boundary.tsx';

export default function MainError({ error, reset }: { error: Error; reset: () => void }) {
  return <ErrorBoundary error={error} reset={reset} title="Main App Error" description="An error occurred in the main application. Please try again." />;
}
