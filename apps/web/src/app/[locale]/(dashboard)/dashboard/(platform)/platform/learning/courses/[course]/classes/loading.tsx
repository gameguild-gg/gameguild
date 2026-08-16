import React from 'react';

/**
 * Loading UI for Course Classes route.
 * Shown while getCourseCohorts() resolves.
 */
export default function Loading(): React.JSX.Element {
  return (
    <div className="space-y-5" aria-label="Loading classes">
      <div className="flex items-center justify-between gap-4">
        <div className="space-y-2"><div className="h-6 w-32 animate-pulse rounded bg-muted" /><div className="h-4 w-72 max-w-full animate-pulse rounded bg-muted" /></div>
        <div className="h-9 w-28 animate-pulse rounded bg-muted" />
      </div>
      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        {Array.from({ length: 4 }, (_, index) => <div key={index} className="h-20 animate-pulse rounded-lg border bg-muted/40" />)}
      </div>
      <div className="h-14 animate-pulse rounded-lg border bg-muted/40" />
      <div className="h-64 animate-pulse rounded-lg border bg-muted/30" />
    </div>
  );
}
