'use client';

import React from 'react';

/**
 * Error boundary for Class Detail route.
 * Catches errors from the cohort workspace or child routes.
 */
export default function Error({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}): React.JSX.Element {
  void error;
  void reset;
  return <div>Error loading class details. Please try again.</div>;
}
