'use client';

import { submitTestingEventFeedback, type TestingEventActionResult } from '@/lib/testing-lab/events-actions';
import type {
  TestingLabQuestionnaireOutput,
  TestingLabTestingApplicationReviewPackageProjection,
  TestingLabTestingProjectBrief,
} from '@game-guild/client';
import { Alert, AlertDescription } from '@game-guild/ui/components/alert';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';
import { AlertCircle, CheckCircle2, Loader2, MessageSquareText } from 'lucide-react';
import Link from 'next/link';
import { useState, useTransition } from 'react';
import { QuestionnaireFieldset } from './questionnaire-fieldset';

interface FeedbackObligation {
  id?: string | null;
  applicationId?: string | null;
  questionnaireRevisionId?: string | null;
  status?: string | null;
  reviewPackage?: TestingLabTestingApplicationReviewPackageProjection | null;
}
function BriefSummary({ brief }: { brief?: TestingLabTestingProjectBrief }) {
  if (!brief) return null;
  return (
    <div className="space-y-3 rounded-md border bg-muted/20 p-4 text-sm">
      <div><p className="font-medium">Test objective</p><p className="mt-1 text-muted-foreground whitespace-pre-wrap">{brief.testObjective}</p></div>
      <div><p className="font-medium">Installation and access</p><p className="mt-1 text-muted-foreground whitespace-pre-wrap">{brief.installationAndAccess}</p></div>
      <div><p className="font-medium">Tasks</p><ol className="mt-1 list-decimal space-y-1 pl-5 text-muted-foreground">{(brief.testTasks ?? []).map((task, index) => <li key={`${index}-${task}`}>{task}</li>)}</ol></div>
      <div className="grid gap-3 sm:grid-cols-2"><div><p className="font-medium">Controls</p><p className="mt-1 text-muted-foreground whitespace-pre-wrap">{brief.controls}</p></div><div><p className="font-medium">Known limitations</p><p className="mt-1 text-muted-foreground whitespace-pre-wrap">{brief.knownLimitations}</p></div></div>
    </div>
  );
}

function FeedbackForm({ eventId, obligation }: { eventId: string; obligation: FeedbackObligation }) {
  const [pending, startTransition] = useTransition();
  const [result, setResult] = useState<TestingEventActionResult<unknown> | null>(null);
  const [responses, setResponses] = useState<TestingLabQuestionnaireOutput>({ answers: [] });
  const [questionnaireComplete, setQuestionnaireComplete] = useState(false);
  const [rating, setRating] = useState('');
  const [recommendation, setRecommendation] = useState('');
  const [notes, setNotes] = useState('');
  const reviewPackage = obligation.reviewPackage;

  function submit() {
    const formData = new FormData();
    formData.set('eventId', eventId);
    formData.set('obligationId', obligation.id ?? '');
    formData.set('questionnaireRevisionId', obligation.questionnaireRevisionId ?? '');
    formData.set('responsesJson', JSON.stringify(responses));
    formData.set('overallRating', rating);
    formData.set('wouldRecommend', String(recommendation === 'yes'));
    formData.set('additionalNotes', notes);
    startTransition(async () => setResult(await submitTestingEventFeedback(formData)));
  }

  return (
    <section className="space-y-4 rounded-md border bg-card p-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div><p className="font-medium">{reviewPackage?.versionNumber ? `Version ${reviewPackage.versionNumber}` : 'Assigned project feedback'}</p><p className="text-xs text-muted-foreground">Developer questionnaire revision {obligation.questionnaireRevisionId?.slice(0, 8)}</p></div>
        <Badge variant="secondary">Required</Badge>
      </div>
      <BriefSummary brief={reviewPackage?.brief} />
      {(reviewPackage?.assets?.length ?? 0) > 0 ? (
        <div className="space-y-2"><p className="text-sm font-medium">Test assets</p><div className="flex flex-wrap gap-2">{reviewPackage?.assets?.map((asset) => asset.accessUrl ? <Button key={asset.assetReferenceId} asChild size="sm" variant="outline"><a href={asset.accessUrl} target="_blank" rel="noreferrer">{asset.displayName || 'Open asset'}</a></Button> : null)}</div></div>
      ) : null}
      <QuestionnaireFieldset
        schema={reviewPackage?.feedbackQuestionnaire}
        value={responses}
        onChange={(value) => { setResponses(value); setQuestionnaireComplete(false); }}
        onComplete={() => setQuestionnaireComplete(true)}
        submitLabel="Confirm questionnaire answers"
      />
      <div className="grid gap-4 sm:grid-cols-2">
        <div className="space-y-2"><Label htmlFor={`rating-${obligation.id}`}>Overall rating (1–10)</Label><Input id={`rating-${obligation.id}`} value={rating} onChange={(event) => setRating(event.currentTarget.value)} type="number" min={1} max={10} required /></div>
        <div className="space-y-2"><Label htmlFor={`recommend-${obligation.id}`}>Would you recommend it?</Label><select id={`recommend-${obligation.id}`} value={recommendation} onChange={(event) => setRecommendation(event.currentTarget.value)} className="flex h-9 w-full rounded-md border border-input bg-background px-3 text-sm"><option value="">Choose</option><option value="yes">Yes</option><option value="no">No</option></select></div>
      </div>
      <div className="space-y-2"><Label htmlFor={`notes-${obligation.id}`}>Additional observations (optional)</Label><Textarea id={`notes-${obligation.id}`} value={notes} onChange={(event) => setNotes(event.currentTarget.value)} rows={3} /></div>
      <Button type="button" disabled={pending || !questionnaireComplete || !rating || !recommendation} onClick={submit}>
        {pending ? <Loader2 className="mr-2 size-4 animate-spin" /> : null}Submit required feedback
      </Button>
      {result ? <Alert variant={result.success ? 'default' : 'destructive'} aria-live="polite">{result.success ? <CheckCircle2 className="size-4" /> : <AlertCircle className="size-4" />}<AlertDescription>{result.success ? result.message : result.error}</AlertDescription></Alert> : null}
    </section>
  );
}

export function TestingFeedbackSubmission({ eventId, isAuthenticated, obligations }: { eventId: string; isAuthenticated: boolean; obligations: FeedbackObligation[] }) {
  const pending = obligations.filter((obligation) => obligation.status === 'Pending');
  if (!isAuthenticated && pending.length > 0) return <Button asChild><Link href="/sign-in">Sign in to submit feedback</Link></Button>;
  if (obligations.length === 0) return <p className="text-sm text-muted-foreground">No project feedback is assigned to you for this event.</p>;
  if (pending.length === 0) return <Alert><CheckCircle2 className="size-4" /><AlertDescription>All assigned feedback is complete.</AlertDescription></Alert>;
  return (
    <div className="space-y-4">
      <div className="flex items-center gap-2 text-sm text-muted-foreground"><MessageSquareText className="size-4" />{pending.length} required feedback {pending.length === 1 ? 'submission remains' : 'submissions remain'}</div>
      {pending.map((obligation) => <FeedbackForm key={obligation.id ?? obligation.applicationId} eventId={eventId} obligation={obligation} />)}
    </div>
  );
}
