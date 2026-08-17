import React from 'react';

/**
 * Loading UI for Class Detail route.
 * Shown while the cohort workspace resolves.
 */
export default function Loading(): React.JSX.Element {
  return <div className="h-80 animate-pulse rounded-lg border bg-muted/30" aria-label="Loading class workspace" />;
}
