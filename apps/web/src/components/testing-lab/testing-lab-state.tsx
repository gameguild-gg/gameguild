import { Alert, AlertDescription, AlertTitle } from '@game-guild/ui/components/alert';
import { AlertCircle, Inbox } from 'lucide-react';
import type { ReactNode } from 'react';

export function TestingLabAccessIssues({ issues }: { issues: string[] }) {
  if (issues.length === 0) return null;
  return (
    <Alert variant="destructive">
      <AlertCircle className="size-4" />
      <AlertTitle>Some data could not be loaded</AlertTitle>
      <AlertDescription>{issues.join(' ')}</AlertDescription>
    </Alert>
  );
}

export function TestingLabEmptyState({ title, description, action }: { title: string; description: string; action?: ReactNode }) {
  return (
    <div className="flex min-h-52 flex-col items-center justify-center rounded-md border border-dashed px-6 py-10 text-center">
      <Inbox className="mb-4 size-8 text-muted-foreground" />
      <h2 className="font-semibold">{title}</h2>
      <p className="mt-1 max-w-md text-sm text-muted-foreground">{description}</p>
      {action ? <div className="mt-4">{action}</div> : null}
    </div>
  );
}
