import { Link } from '@/i18n/navigation';
import { Button } from '@game-guild/ui/components/button';
import { ShieldOff } from 'lucide-react';
import React from 'react';

export default function Forbidden(): React.JSX.Element {
  return (
    <div className="flex flex-col items-center justify-center gap-4 p-12 text-center">
      <ShieldOff className="size-12 text-muted-foreground" />
      <div>
        <h2 className="text-xl font-semibold">Access denied</h2>
        <p className="text-sm text-muted-foreground">You don't have permission to view this resource.</p>
      </div>
      <div className="flex gap-2">
        <Button asChild variant="outline">
          <Link href="/dashboard/platform/learning/courses">Back to courses</Link>
        </Button>
      </div>
    </div>
  );
}
