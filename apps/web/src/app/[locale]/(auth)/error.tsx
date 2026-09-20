'use client';

import { Link } from '@/i18n/navigation';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import React from 'react';

export default function Error({ error, reset }: { error: Error & { digest?: string }; reset: () => void }): React.JSX.Element {
  return (
    <Card className="w-full">
      <CardHeader className="text-center">
        <CardTitle className="text-xl">Something went wrong</CardTitle>
        <CardDescription>{error.message || 'An unexpected error occurred. Please try again.'}</CardDescription>
      </CardHeader>
      <CardContent className="flex flex-col gap-3">
        <Button onClick={reset} variant="default">
          Try again
        </Button>
        <Button variant="outline" render={<Link href="/sign-in" />}>
          Back to Sign In
        </Button>
      </CardContent>
    </Card>
  );
}
