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
import { buttonVariants } from '@game-guild/ui/components/button-variants';
import { AlertCircle, Archive, CheckCircle2, Loader2, RotateCcw, Trash2 } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useEffect, useRef, useState, useTransition, type MouseEvent } from 'react';
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
  const closeTimerRef = useRef<number | null>(null);
  const Icon = intent === 'restore' ? RotateCcw : intent === 'delete' ? Trash2 : Archive;

  useEffect(() => {
    return () => {
      if (closeTimerRef.current !== null) {
        window.clearTimeout(closeTimerRef.current);
      }
    };
  }, []);

  function runAction(event: MouseEvent<HTMLButtonElement>) {
    event.preventDefault();
    const formData = new FormData();
    Object.entries(fields).forEach(([key, value]) => formData.set(key, value));
    startTransition(async () => {
      try {
        const next = await action(formData);
        setResult(next);
        if (next.success) {
          toast.success(next.message);
          if (successHref) router.push(successHref);
          if (closeTimerRef.current !== null) {
            window.clearTimeout(closeTimerRef.current);
          }
          closeTimerRef.current = window.setTimeout(() => {
            closeTimerRef.current = null;
            setOpen(false);
          }, 650);
        } else {
          toast.error(next.error);
        }
      } catch (error) {
        const message = error instanceof Error ? error.message : 'The Testing Lab operation failed.';
        setResult({ success: false, error: message });
        toast.error(message);
      }
    });
  }

  return (
    <AlertDialog
      open={open}
      onOpenChange={(next) => {
        if (!next && closeTimerRef.current !== null) {
          window.clearTimeout(closeTimerRef.current);
          closeTimerRef.current = null;
        }
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
          <AlertDialogAction
            type="button"
            className={buttonVariants({ variant: intent === 'delete' ? 'destructive' : 'default' })}
            disabled={pending}
            onClick={runAction}
          >
            {pending ? <Loader2 className="mr-2 size-4 animate-spin" /> : <Icon className="mr-2 size-4" />}
            {pending ? 'Working...' : confirmLabel}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
