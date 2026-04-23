import React from 'react';

export default function Loading(): React.JSX.Element {
  return (
    <div className="flex flex-col gap-4">
      {Array.from({ length: 5 }).map((_, i) => (
        <div key={i} className="flex flex-col gap-2">
          <div className="h-4 w-32 animate-pulse rounded bg-muted" />
          <div className="h-10 w-full animate-pulse rounded-md bg-muted/60" />
        </div>
      ))}
      <div className="h-10 w-32 animate-pulse rounded-md bg-muted" />
    </div>
  );
}
