'use client';

import type { TestingLabActionResult } from '@/lib/testing-lab/actions';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/components/ui/alert-dialog';
import { Alert, AlertDescription } from '@game-guild/ui/components/alert';
import { Button } from '@game-guild/ui/components/button';
import { AlertCircle, Archive, CheckCircle2, Loader2, RotateCcw, Trash2 } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useState, useTransition } from 'react';
import { toast } from 'sonner';

type Action = (formData: FormData) => Promise<TestingLabActionResult<unknown>>;

export function TestingLabConfirmAction({
  action,
  fields,
  label,
  title,
  description,
  confirmLabel,
  intent = 'archive',
  successHref,
}: {
  action: Action;
  fields: Record<string, string>;
  label: string;
  title: string;
  description: string;
  confirmLabel: string;
  intent?: 'archive' | 'delete' | 'restore';
  successHref?: string;
}) {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [pending, startTransition] = useTransition();
  const [result, setResult] = useState<TestingLabActionResult<unknown> | null>(null);
  const Icon = intent === 'restore' ? RotateCcw : intent === 'delete' ? Trash2 : Archive;

  function runAction() {
    const formData = new FormData();
    Object.entries(fields).forEach(([key, value]) => formData.set(key, value));
    startTransition(async () => {
      const next = await action(formData);
      setResult(next);
      if (next.success) {
        toast.success(next.message);
        if (successHref) router.push(successHref);
        window.setTimeout(() => setOpen(false), 650);
      } else {
        toast.error(next.error);
      }
    });
  }

  return (
    <AlertDialog
      open={open}
      onOpenChange={(next) => {
        setOpen(next);
        if (!next) setResult(null);
      }}
    >
      <AlertDialogTrigger asChild>
        <Button type="button" size="sm" variant={intent === 'delete' ? 'destructive' : 'outline'}>
          <Icon className="mr-2 size-4" />
          {label}
        </Button>
      </AlertDialogTrigger>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{title}</AlertDialogTitle>
          <AlertDialogDescription>{description}</AlertDialogDescription>
        </AlertDialogHeader>
        {result ? (
          <Alert variant={result.success ? 'default' : 'destructive'}>
            {result.success ? <CheckCircle2 className="size-4" /> : <AlertCircle className="size-4" />}
            <AlertDescription>{result.success ? result.message : result.error}</AlertDescription>
          </Alert>
        ) : null}
        <AlertDialogFooter>
          <AlertDialogCancel disabled={pending}>Cancel</AlertDialogCancel>
          <AlertDialogAction asChild>
            <Button type="button" variant={intent === 'delete' ? 'destructive' : 'default'} disabled={pending} onClick={runAction}>
              {pending ? <Loader2 className="mr-2 size-4 animate-spin" /> : <Icon className="mr-2 size-4" />}
              {pending ? 'Working...' : confirmLabel}
            </Button>
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
