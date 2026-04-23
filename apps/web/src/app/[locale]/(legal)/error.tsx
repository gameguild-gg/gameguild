'use client';

import React from 'react';

// TODO: Replace with shared <Error> component once @/components/common/errors is ported.
export default function Error({ error, reset }: { error: Error & { digest?: string }; reset: () => void }): React.JSX.Element {
  return (
    <div className="container mx-auto px-4 py-12">
      <h1 className="text-2xl font-bold mb-2">Something went wrong</h1>
      <p className="text-muted-foreground mb-4">{error.message}</p>
      <button
        type="button"
        onClick={reset}
        className="inline-flex items-center px-4 py-2 rounded-md border border-border bg-background hover:bg-accent"
      >
        Try again
      </button>
    </div>
  );
}
