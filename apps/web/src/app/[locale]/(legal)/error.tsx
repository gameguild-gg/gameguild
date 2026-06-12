'use client';

import React from 'react';

export default function Error({ error, reset }: { error: Error & { digest?: string }; reset: () => void }): React.JSX.Element {
  return (
    <div className="container mx-auto px-4 py-12">
      <div className="max-w-2xl rounded-lg border border-border bg-card p-6 text-card-foreground shadow-sm">
        <h1 className="text-2xl font-bold mb-2">Something went wrong</h1>
        <p className="text-muted-foreground mb-4">{error.message}</p>
        {error.digest ? <p className="mb-4 text-xs text-muted-foreground">Error ID: {error.digest}</p> : null}
        <button
          type="button"
          onClick={reset}
          className="inline-flex items-center px-4 py-2 rounded-md border border-border bg-background hover:bg-accent"
        >
          Try again
        </button>
      </div>
    </div>
  );
}
