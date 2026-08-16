import { Link } from '@/i18n/navigation';
import { Button } from '@game-guild/ui/components/button';
import { SearchX } from 'lucide-react';
import React from 'react';

export default function NotFound(): React.JSX.Element {
  return (
    <div className="flex flex-col items-center justify-center gap-4 p-12 text-center">
      <SearchX className="size-12 text-muted-foreground" />
      <div>
        <h2 className="text-xl font-semibold">Course not found</h2>
        <p className="text-sm text-muted-foreground">This course doesn't exist or you don't have access to it.</p>
      </div>
      <Button asChild variant="outline">
        <Link href="/workspace/learning/courses">Back to courses</Link>
      </Button>
    </div>
  );
}
