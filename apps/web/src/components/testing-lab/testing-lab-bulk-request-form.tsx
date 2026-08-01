'use client';

import { bulkUpdateTestingRequests, type TestingLabActionResult } from '@/lib/testing-lab/actions';
import { Alert, AlertDescription } from '@game-guild/ui/components/alert';
import { Button } from '@game-guild/ui/components/button';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@game-guild/ui/components/alert-dialog';
import { AlertCircle, Archive, CheckCircle2, Loader2, RotateCcw } from 'lucide-react';
import { useRef, useState, useTransition, type ReactNode } from 'react';

export function TestingLabBulkRequestForm({ children, matchingCount }: { children: ReactNode; matchingCount: number }) {
  const formRef = useRef<HTMLFormElement>(null);
  const [operation, setOperation] = useState<'archive' | 'restore' | null>(null);
  const [selectedCount, setSelectedCount] = useState(0);
  const [pending, startTransition] = useTransition();
  const [result, setResult] = useState<TestingLabActionResult<unknown> | null>(null);

  function prepare(next: 'archive' | 'restore') {
    const count = formRef.current?.querySelectorAll<HTMLInputElement>('input[name="requestIds"]:checked').length ?? 0;
    if (count === 0) {
      setResult({ success: false, error: 'Select at least one testing request.' });
      return;
    }
    setSelectedCount(count);
    setOperation(next);
  }

  function execute() {
    const form = formRef.current;
    if (!form || !operation) return;
    const data = new FormData(form);
    data.set('operation', operation);
    startTransition(async () => {
      const next = await bulkUpdateTestingRequests(data);
      setResult(next);
      if (next.success)
        form.querySelectorAll<HTMLInputElement>('input[name="requestIds"]').forEach((input) => {
          input.checked = false;
        });
      setOperation(null);
    });
  }

  return (
    <form ref={formRef} className="overflow-hidden rounded-md border" onSubmit={(event) => event.preventDefault()}>
      <div className="flex flex-wrap items-center justify-between gap-3 border-b px-4 py-3">
        <div>
          <h2 className="font-semibold">Request directory</h2>
          <p className="text-sm text-muted-foreground">{matchingCount} matching requests</p>
        </div>
        <div className="flex gap-2">
          <Button type="button" size="sm" variant="outline" onClick={() => prepare('restore')}>
            <RotateCcw className="mr-2 size-4" />
            Restore selected
          </Button>
          <Button type="button" size="sm" variant="destructive" onClick={() => prepare('archive')}>
            <Archive className="mr-2 size-4" />
            Archive selected
          </Button>
        </div>
      </div>
      {result ? (
        <div className="border-b p-3">
          <Alert variant={result.success ? 'default' : 'destructive'} aria-live="polite">
            {result.success ? <CheckCircle2 className="size-4" /> : <AlertCircle className="size-4" />}
            <AlertDescription>{result.success ? result.message : result.error}</AlertDescription>
          </Alert>
        </div>
      ) : null}
      {children}
      <AlertDialog
        open={operation !== null}
        onOpenChange={(open) => {
          if (!open && !pending) setOperation(null);
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{operation === 'archive' ? 'Archive selected requests?' : 'Restore selected requests?'}</AlertDialogTitle>
            <AlertDialogDescription>
              {selectedCount} request{selectedCount === 1 ? '' : 's'} will be{' '}
              {operation === 'archive' ? 'removed from active operations' : 'returned to active operations'}. The records remain auditable.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={pending}>Cancel</AlertDialogCancel>
            <AlertDialogAction asChild>
              <Button type="button" variant={operation === 'archive' ? 'destructive' : 'default'} disabled={pending} onClick={execute}>
                {pending ? <Loader2 className="mr-2 size-4 animate-spin" /> : null}
                {pending ? 'Working...' : operation === 'archive' ? 'Archive requests' : 'Restore requests'}
              </Button>
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </form>
  );
}
