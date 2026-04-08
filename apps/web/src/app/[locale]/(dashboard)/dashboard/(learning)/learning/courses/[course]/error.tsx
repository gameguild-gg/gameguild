'use client';

import React from 'react';

/**
 * Course Error Boundary
 *
 * Catches errors in all course subroutes and provides recovery options.
 * This is a client component as required by Next.js error boundaries.
 */
export default function Error({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}): React.JSX.Element {
  // TODO: Implement error UI with:
  // - User-friendly error message
  // - Error digest for support reference
  // - Retry button (calls reset())
  // - Link back to courses list
  // - Log error to monitoring service

  void error;
  void reset;

  return <></>;
}
