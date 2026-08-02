'use client';

import { Button } from '@game-guild/ui/components/button';
import { LogIn } from 'lucide-react';
import { useEffect } from 'react';

export function LearningAuthRedirect({ href }: { href: string }) {
  useEffect(() => {
    window.location.replace(href);
  }, [href]);

  return (
    <main className="grid min-h-dvh place-items-center bg-background p-6">
      <div className="max-w-sm text-center">
        <LogIn className="mx-auto size-8 text-primary" aria-hidden="true" />
        <h1 className="mt-4 text-xl font-semibold text-foreground">Sign in to continue</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          Your learning destination will be restored after authentication.
        </p>
        <Button asChild className="mt-6">
          <a href={href}>Continue to sign in</a>
        </Button>
      </div>
    </main>
  );
}
