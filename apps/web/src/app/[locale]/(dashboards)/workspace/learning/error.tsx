'use client';

import { Button } from '@game-guild/ui/components/button';
import { AlertTriangle } from 'lucide-react';
import React from 'react';

export default function Error({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}): React.JSX.Element {
  React.useEffect(() => {
    console.error('Learning error:', error);
  }, [error]);

  return (
    <div className="flex flex-col items-center justify-center gap-4 p-12 text-center">
      <AlertTriangle className="size-12 text-destructive" />
      <div>
        <h2 className="text-xl font-semibold">Something went wrong</h2>
        <p className="text-sm text-muted-foreground">{error.message || 'An unexpected error occurred while loading this section.'}</p>
        {error.digest && <p className="mt-1 text-xs text-muted-foreground">Reference: {error.digest}</p>}
      </div>
      <Button onClick={reset}>Try again</Button>
    </div>
  );
}
