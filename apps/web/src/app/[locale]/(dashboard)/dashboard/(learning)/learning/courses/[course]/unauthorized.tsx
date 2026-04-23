import { Link } from '@/i18n/navigation';
import { Button } from '@game-guild/ui/components/button';
import { Lock } from 'lucide-react';
import React from 'react';

export default function Unauthorized(): React.JSX.Element {
  return (
    <div className="flex flex-col items-center justify-center gap-4 p-12 text-center">
      <Lock className="size-12 text-muted-foreground" />
      <div>
        <h2 className="text-xl font-semibold">Sign in required</h2>
        <p className="text-sm text-muted-foreground">You need to be signed in to view this course.</p>
      </div>
      <Button asChild>
        <Link href="/sign-in">Sign in</Link>
      </Button>
    </div>
  );
}
