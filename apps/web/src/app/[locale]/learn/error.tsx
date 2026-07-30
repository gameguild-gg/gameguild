'use client';

import { Button } from '@game-guild/ui/components/button';
import { AlertTriangle, RotateCcw } from 'lucide-react';
import { useEffect } from 'react';

export default function LearningError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    console.error('[learning] route error', error);
  }, [error]);

  return (
    <section className="flex min-h-[28rem] flex-col items-center justify-center border-y text-center">
      <AlertTriangle className="size-9 text-destructive" />
      <h1 className="mt-4 text-xl font-semibold">Learning could not be loaded</h1>
      <p className="mt-2 max-w-md text-sm text-muted-foreground">
        The request failed without losing your place. Retry the current view.
      </p>
      <Button className="mt-6" onClick={reset}>
        <RotateCcw className="size-4" />
        Retry
      </Button>
    </section>
  );
}
