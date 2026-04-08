import React from 'react';

export default function Loading(): React.JSX.Element {
  return (
    <div className="flex w-full flex-col gap-6 animate-pulse">
      <div className="rounded-lg border bg-card p-6">
        <div className="space-y-4">
          <div className="h-6 w-40 rounded bg-muted mx-auto" />
          <div className="h-4 w-56 rounded bg-muted mx-auto" />
          <div className="space-y-3 pt-4">
            <div className="h-10 rounded bg-muted" />
            <div className="h-10 rounded bg-muted" />
            <div className="h-10 rounded bg-muted" />
          </div>
        </div>
      </div>
    </div>
  );
}
