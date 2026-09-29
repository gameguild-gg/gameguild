'use client';

import { Link } from '@/i18n/navigation';
import { Button, buttonVariants } from '@game-guild/ui/components/button';
import { Card, CardContent } from '@game-guild/ui/components/card';
import { TriangleAlert } from 'lucide-react';

interface ProjectsErrorProps {
  error: Error & { digest?: string };
  reset: () => void;
}

export default function ProjectsError({ reset }: ProjectsErrorProps) {
  return (
    <main className="mx-auto w-full max-w-2xl px-4 py-12 sm:px-6 sm:py-16">
      <Card role="alert">
        <CardContent className="flex flex-col items-center px-6 py-10 text-center">
          <TriangleAlert className="size-8 text-destructive" aria-hidden="true" />
          <h1 className="mt-4 text-xl font-semibold">Projects couldn’t load</h1>
          <p className="mt-2 max-w-md text-sm leading-6 text-muted-foreground">
            Something went wrong while opening Projects. Try again, or return to the project directory.
          </p>
          <div className="mt-5 flex w-full flex-col justify-center gap-2 sm:w-auto sm:flex-row">
            <Button type="button" onClick={reset}>Try again</Button>
            <Link href="/projects" className={buttonVariants({ variant: 'outline' })}>
              Back to projects
            </Link>
          </div>
        </CardContent>
      </Card>
    </main>
  );
}
