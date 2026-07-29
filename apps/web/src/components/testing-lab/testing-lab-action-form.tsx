'use client';

import type { TestingLabActionResult } from '@/lib/testing-lab/actions';
import { Alert, AlertDescription } from '@game-guild/ui/components/alert';
import { Button } from '@game-guild/ui/components/button';
import { AlertCircle, CheckCircle2, Loader2 } from 'lucide-react';
import { useRef, useState, useTransition, type FormEvent, type ReactNode } from 'react';

type Action = (formData: FormData) => Promise<TestingLabActionResult<unknown>>;

export function TestingLabActionMessage({ result }: { result: TestingLabActionResult<unknown> | null }) {
  if (!result) return null;
  return (
    <Alert variant={result.success ? 'default' : 'destructive'} aria-live="polite">
      {result.success ? <CheckCircle2 className="size-4" /> : <AlertCircle className="size-4" />}
      <AlertDescription>{result.success ? result.message : result.error}</AlertDescription>
    </Alert>
  );
}

export function TestingLabActionForm({
  action,
  children,
  submitLabel,
  pendingLabel = 'Saving...',
  secondaryAction,
  secondaryLabel,
  className,
  actionsClassName = 'flex flex-wrap justify-end gap-2',
  submitClassName,
  secondaryVariant = 'outline',
  resetOnSuccess = false,
}: {
  action: Action;
  children: ReactNode;
  submitLabel: string;
  pendingLabel?: string;
  secondaryAction?: Action;
  secondaryLabel?: string;
  className?: string;
  actionsClassName?: string;
  submitClassName?: string;
  secondaryVariant?: 'outline' | 'destructive' | 'secondary' | 'ghost';
  resetOnSuccess?: boolean;
}) {
  const formRef = useRef<HTMLFormElement>(null);
  const [pending, startTransition] = useTransition();
  const [result, setResult] = useState<TestingLabActionResult<unknown> | null>(null);

  function run(nextAction: Action) {
    const form = formRef.current;
    if (!form || !form.reportValidity()) return;
    const formData = new FormData(form);
    startTransition(async () => {
      const next = await nextAction(formData);
      setResult(next);
      if (next.success && resetOnSuccess) form.reset();
    });
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    run(action);
  }

  return (
    <form ref={formRef} onSubmit={handleSubmit} className={className} noValidate={false}>
      {children}
      <TestingLabActionMessage result={result} />
      <div className={actionsClassName}>
        {secondaryAction && secondaryLabel ? (
          <Button type="button" variant={secondaryVariant} disabled={pending} onClick={() => run(secondaryAction)}>
            {pending ? <Loader2 className="mr-2 size-4 animate-spin" /> : null}
            {secondaryLabel}
          </Button>
        ) : null}
        <Button type="submit" disabled={pending} className={submitClassName}>
          {pending ? <Loader2 className="mr-2 size-4 animate-spin" /> : null}
          {pending ? pendingLabel : submitLabel}
        </Button>
      </div>
    </form>
  );
}
