'use client';

import { submitTestingEventFeedback, type TestingEventActionResult } from '@/lib/testing-lab/events-actions';
import { Alert, AlertDescription } from '@game-guild/ui/components/alert';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';
import { AlertCircle, CheckCircle2, Loader2, MessageSquareText } from 'lucide-react';
import Link from 'next/link';
import { useState, useTransition, type FormEvent } from 'react';

interface FeedbackObligation {
  id?: string | null;
  applicationId?: string | null;
  status?: string | null;
}

function FeedbackForm({ eventId, obligation }: { eventId: string; obligation: FeedbackObligation }) {
  const [pending, startTransition] = useTransition();
  const [result, setResult] = useState<TestingEventActionResult<unknown> | null>(null);

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = event.currentTarget;
    startTransition(async () => {
      const next = await submitTestingEventFeedback(new FormData(form));
      setResult(next);
    });
  }

  return (
    <form className="space-y-4 rounded-md border bg-card p-4" onSubmit={submit}>
      <input type="hidden" name="eventId" value={eventId} />
      <input type="hidden" name="obligationId" value={obligation.id ?? ''} />
      <div className="flex flex-wrap items-center justify-between gap-2">
        <p className="font-medium">Assigned project feedback</p>
        <Badge variant="secondary">Required</Badge>
      </div>
      <div className="space-y-2">
        <Label htmlFor={`feedback-${obligation.id}`}>Structured feedback</Label>
        <Textarea
          id={`feedback-${obligation.id}`}
          name="feedbackData"
          rows={5}
          required
          placeholder="Describe what worked, what blocked you, and the most important improvement."
        />
      </div>
      <div className="grid gap-4 sm:grid-cols-2">
        <div className="space-y-2">
          <Label htmlFor={`rating-${obligation.id}`}>Overall rating (1-10)</Label>
          <Input id={`rating-${obligation.id}`} name="overallRating" type="number" min={1} max={10} required />
        </div>
        <div className="flex items-end gap-3 pb-2">
          <input id={`recommend-${obligation.id}`} name="wouldRecommend" type="checkbox" className="size-4 rounded border-input accent-primary" />
          <Label htmlFor={`recommend-${obligation.id}`}>I would recommend this project</Label>
        </div>
      </div>
      <div className="space-y-2">
        <Label htmlFor={`notes-${obligation.id}`}>Additional notes</Label>
        <Textarea id={`notes-${obligation.id}`} name="additionalNotes" rows={2} />
      </div>
      <Button type="submit" disabled={pending}>
        {pending ? <Loader2 className="mr-2 size-4 animate-spin" /> : null}
        Submit required feedback
      </Button>
      {result ? (
        <Alert variant={result.success ? 'default' : 'destructive'} aria-live="polite">
          {result.success ? <CheckCircle2 className="size-4" /> : <AlertCircle className="size-4" />}
          <AlertDescription>{result.success ? result.message : result.error}</AlertDescription>
        </Alert>
      ) : null}
    </form>
  );
}

export function TestingFeedbackSubmission({
  eventId,
  isAuthenticated,
  obligations,
}: {
  eventId: string;
  isAuthenticated: boolean;
  obligations: FeedbackObligation[];
}) {
  const pending = obligations.filter((obligation) => obligation.status === 'Pending');

  if (!isAuthenticated && pending.length > 0) {
    return (
      <Button asChild>
        <Link href="/sign-in">Sign in to submit feedback</Link>
      </Button>
    );
  }

  if (obligations.length === 0) {
    return <p className="text-sm text-muted-foreground">No project feedback is assigned to you for this event.</p>;
  }

  if (pending.length === 0) {
    return (
      <Alert>
        <CheckCircle2 className="size-4" />
        <AlertDescription>All assigned feedback is complete.</AlertDescription>
      </Alert>
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-2 text-sm text-muted-foreground">
        <MessageSquareText className="size-4" />
        {pending.length} required feedback {pending.length === 1 ? 'submission remains' : 'submissions remain'}
      </div>
      {pending.map((obligation) => (
        <FeedbackForm key={obligation.id ?? obligation.applicationId} eventId={eventId} obligation={obligation} />
      ))}
    </div>
  );
}
