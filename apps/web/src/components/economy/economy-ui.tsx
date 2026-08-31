import type { EconomyActionResult } from '@/lib/economy/actions';
import { Alert, AlertDescription, AlertTitle } from '@game-guild/ui/components/alert';
import { Badge } from '@game-guild/ui/components/badge';
import { AlertTriangle, CheckCircle2 } from 'lucide-react';
import type { ReactNode } from 'react';

export function EconomyPageHeader({ title, description, badge }: { title: string; description: string; badge?: string }) {
  return (
    <header className="flex flex-col gap-2">
      <div className="flex flex-wrap items-center gap-2">
        <h1 className="text-2xl font-semibold tracking-tight">{title}</h1>
        {badge ? <Badge variant="secondary">{badge}</Badge> : null}
      </div>
      <p className="max-w-3xl text-sm text-muted-foreground">{description}</p>
    </header>
  );
}

export function EconomyIssue({ issue }: { issue?: string | null }) {
  if (!issue) return null;
  return (
    <Alert variant="destructive">
      <AlertTriangle className="size-4" aria-hidden="true" />
      <AlertTitle>Unavailable</AlertTitle>
      <AlertDescription>{issue}</AlertDescription>
    </Alert>
  );
}

export function EconomyActionNotice({ result }: { result?: EconomyActionResult<unknown> | null }) {
  if (!result) return null;
  return (
    <Alert variant={result.success ? 'default' : 'destructive'}>
      {result.success
        ? <CheckCircle2 className="size-4" aria-hidden="true" />
        : <AlertTriangle className="size-4" aria-hidden="true" />}
      <AlertTitle>{result.success ? 'Recorded safely' : 'Action not completed'}</AlertTitle>
      <AlertDescription>{result.message}</AlertDescription>
    </Alert>
  );
}

export function EconomyWorkspace({ children }: { children: ReactNode }) {
  return <main className="flex min-h-0 flex-1 flex-col gap-6 p-4 sm:p-6">{children}</main>;
}

export function formatEconomyDate(value?: string | null) {
  if (!value) return 'Not available';
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
}

export function formatEconomyUnits(value?: number | null) {
  return new Intl.NumberFormat(undefined, { maximumFractionDigits: 0 }).format(value ?? 0);
}
