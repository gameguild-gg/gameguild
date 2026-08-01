'use client';

import { Button } from '@game-guild/ui/components/button';
import { Skeleton } from '@game-guild/ui/components/skeleton';
import { AlertTriangle, BookX, RotateCcw } from 'lucide-react';
import Link from 'next/link';
import { useEffect } from 'react';

type RouteScope = 'workspace' | 'course';

const copy = {
  workspace: {
    loading: 'Loading learning workspace',
    errorTitle: 'Learning could not be loaded',
    errorDescription: 'The request failed without losing your place. Retry the current view.',
    notFoundTitle: 'Learning resource not found',
    notFoundDescription:
      'The course, lesson, or activity may have moved or is not available to your enrollment.',
    returnHref: '/courses',
    returnLabel: 'Return to my courses',
  },
  course: {
    loading: 'Loading course workspace',
    errorTitle: 'Course workspace could not be loaded',
    errorDescription: 'Your place in the course is preserved. Retry to load this course view again.',
    notFoundTitle: 'Course resource not found',
    notFoundDescription:
      'This course item may have moved, been removed, or is unavailable to your enrollment.',
    returnHref: '/courses',
    returnLabel: 'Return to my courses',
  },
} satisfies Record<
  RouteScope,
  {
    loading: string;
    errorTitle: string;
    errorDescription: string;
    notFoundTitle: string;
    notFoundDescription: string;
    returnHref: string;
    returnLabel: string;
  }
>;

export function LearnerRouteLoading({ scope = 'workspace' }: { scope?: RouteScope }) {
  return (
    <div role="status" aria-live="polite" aria-busy="true" className="space-y-6">
      <span className="sr-only">{copy[scope].loading}</span>
      <Skeleton className="h-5 w-32" />
      <Skeleton className="h-10 w-full max-w-xl" />
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        <Skeleton className="h-44" />
        <Skeleton className="h-44" />
        <Skeleton className="hidden h-44 xl:block" />
      </div>
    </div>
  );
}

export function LearnerRouteError({
  error,
  reset,
  scope = 'workspace',
}: {
  error: Error & { digest?: string };
  reset: () => void;
  scope?: RouteScope;
}) {
  useEffect(() => {
    console.error(`[learning:${scope}] route error`, error);
  }, [error, scope]);

  return (
    <section
      role="alert"
      aria-labelledby={`learning-${scope}-error-title`}
      className="flex min-h-[28rem] flex-col items-center justify-center border-y px-4 text-center"
    >
      <AlertTriangle aria-hidden="true" className="size-9 text-destructive" />
      <h1 id={`learning-${scope}-error-title`} className="mt-4 text-xl font-semibold">
        {copy[scope].errorTitle}
      </h1>
      <p className="mt-2 max-w-md text-sm text-muted-foreground">
        {copy[scope].errorDescription}
      </p>
      <Button className="mt-6" onClick={reset}>
        <RotateCcw aria-hidden="true" className="size-4" />
        Retry
      </Button>
    </section>
  );
}

export function LearnerRouteNotFound({ scope = 'workspace' }: { scope?: RouteScope }) {
  return (
    <section
      aria-labelledby={`learning-${scope}-not-found-title`}
      className="flex min-h-[28rem] flex-col items-center justify-center border-y px-4 text-center"
    >
      <BookX aria-hidden="true" className="size-9 text-muted-foreground" />
      <h1 id={`learning-${scope}-not-found-title`} className="mt-4 text-xl font-semibold">
        {copy[scope].notFoundTitle}
      </h1>
      <p className="mt-2 max-w-md text-sm text-muted-foreground">
        {copy[scope].notFoundDescription}
      </p>
      <Button asChild className="mt-6">
        <Link href={copy[scope].returnHref}>{copy[scope].returnLabel}</Link>
      </Button>
    </section>
  );
}
