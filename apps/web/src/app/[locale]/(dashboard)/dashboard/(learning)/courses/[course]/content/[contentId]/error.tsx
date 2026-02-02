'use client';

import React from 'react';

/**
 * Error boundary for Content Item Editor route.
 * Catches errors from getContentItem() or child components.
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
  return <div>Error loading content item. Please try again.</div>;
}
