'use client';

import {
  cancelTestingEventRegistration,
  registerForTestingEventSlot,
  type TestingEventActionResult,
} from '@/lib/testing-lab/events-actions';
import { Alert, AlertDescription } from '@game-guild/ui/components/alert';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { buttonVariants } from '@game-guild/ui/components/button-variants';
import { AlertCircle, ArrowRight, CalendarDays, CheckCircle2, Loader2, MapPin } from 'lucide-react';
import { Link } from '@/i18n/navigation';
import { useState, useTransition } from 'react';
import type { TestingLabQuestionnaireOutput, TestingLabQuestionnaireSchema } from '@game-guild/client';
import { TestingEventDateRange } from './testing-event-date-range';
import { QuestionnaireFieldset } from './questionnaire-fieldset';

interface PublicSlot {
  id?: string | null;
  mode?: string | null;
  startsAt?: string | null;
  endsAt?: string | null;
  maxTesters?: number | null;
  maxProjects?: number | null;
  campusName?: string | null;
  roomName?: string | null;
  registeredTesterCount?: number | null;
  approvedProjectCount?: number | null;
  availableTesterCount?: number | null;
  availableProjectCount?: number | null;
}

interface CurrentRegistration {
  id?: string | null;
  status?: string | null;
  waitlistPosition?: number | null;
}

export function TestingSlotRegistration({
  eventId,
  isAuthenticated,
  registrationOpen = true,
  showClosedMessage = true,
  timeZoneId = 'UTC',
  locale = 'en-US',
  hour12 = true,
  slot,
  registration,
  registrationSchema,
  generalRules,
  testerInstructions,
}: {
  eventId: string;
  isAuthenticated: boolean;
  registrationOpen?: boolean;
  showClosedMessage?: boolean;
  timeZoneId?: string;
  locale?: string;
  hour12?: boolean;
  slot: PublicSlot;
  registration?: CurrentRegistration;
  registrationSchema?: TestingLabQuestionnaireSchema | null;
  generalRules?: string | null;
  testerInstructions?: string | null;
}) {
  const [pending, startTransition] = useTransition();
  const [result, setResult] = useState<TestingEventActionResult<unknown> | null>(null);
  const [responses, setResponses] = useState<TestingLabQuestionnaireOutput>({ answers: [] });
  const [questionnaireComplete, setQuestionnaireComplete] = useState((registrationSchema?.questions?.length ?? 0) === 0);
  const [acceptedRules, setAcceptedRules] = useState(false);
  const [joining, setJoining] = useState(false);
  const isFull = slot.availableTesterCount !== null && slot.availableTesterCount !== undefined && slot.availableTesterCount <= 0;
  const location = [slot.campusName, slot.roomName].filter(Boolean).join(' · ');
  const registrationId = registration?.id ?? null;
  const redirectTo = `/testing-lab/events/${eventId}?slotId=${encodeURIComponent(slot.id ?? '')}#session-${slot.id ?? ''}`;
  const signInHref = `/sign-in?redirectTo=${encodeURIComponent(redirectTo)}`;

  function register() {
    const formData = new FormData();
    formData.set('eventId', eventId);
    formData.set('slotId', slot.id!);
    formData.set('registrationResponseJson', JSON.stringify(responses));
    formData.set('acceptedRules', String(acceptedRules));
    startTransition(async () => {
      try {
        const next = await registerForTestingEventSlot(formData);
        setResult(next);
      } catch (error) {
        setResult({
          success: false,
          error: error instanceof Error ? error.message : 'The Testing Lab operation failed.',
        });
      }
    });
  }

  function cancel() {
    const formData = new FormData();
    formData.set('eventId', eventId);
    formData.set('registrationId', registrationId!);
    startTransition(async () => {
      try {
        const next = await cancelTestingEventRegistration(formData);
        setResult(next);
      } catch (error) {
        setResult({
          success: false,
          error: error instanceof Error ? error.message : 'The Testing Lab operation failed.',
        });
      }
    });
  }

  return (
    <article id={slot.id ? `session-${slot.id}` : undefined} className="scroll-mt-24 space-y-3 rounded-lg border bg-card p-4">
      <div className="space-y-2">
        <div className="flex flex-wrap items-center gap-2">
          <Badge variant="outline">{slot.mode === 'InPerson' ? 'In person' : slot.mode ?? 'Online'}</Badge>
          {!registrationOpen ? <Badge variant="secondary">Registration closed</Badge> : isFull ? <Badge variant="secondary">Waitlist available</Badge> : slot.availableTesterCount != null ? <Badge variant="secondary">{slot.availableTesterCount} {slot.availableTesterCount === 1 ? 'seat' : 'seats'} left</Badge> : null}
        </div>
        <div className="flex items-start gap-2 text-sm">
          <CalendarDays className="size-4 shrink-0 text-muted-foreground" />
          <TestingEventDateRange startsAt={slot.startsAt} endsAt={slot.endsAt} timeZone={timeZoneId} locale={locale} hour12={hour12} />
        </div>
        {location ? (
          <p className="flex items-center gap-2 text-sm text-muted-foreground">
            <MapPin className="size-4 shrink-0" />
            {location}
          </p>
        ) : null}
      </div>

      {registration && registration.status !== 'Cancelled' ? (
        <div className="flex flex-wrap items-center justify-between gap-3 border-t pt-4">
          <div>
            <p className="font-medium">{registration.status ?? 'Registered'}</p>
            {registration.status === 'Waitlisted' && registration.waitlistPosition ? (
              <p className="text-sm text-muted-foreground">Waitlist position {registration.waitlistPosition}</p>
            ) : null}
          </div>
          {registration.id && !['Cancelled', 'Completed', 'NoShow'].includes(registration.status ?? '') ? (
            <Button type="button" variant="outline" disabled={pending} onClick={cancel}>
              {pending ? <Loader2 className="mr-2 size-4 animate-spin" /> : null}
              Cancel registration
            </Button>
          ) : null}
        </div>
      ) : !registrationOpen && showClosedMessage ? (
        <div className="border-t pt-4">
          <p className="text-sm text-muted-foreground">This session is not accepting new registrations. Browse other playtests for an open seat.</p>
        </div>
      ) : !registrationOpen ? null : !isAuthenticated ? (
        <div className="border-t pt-3">
          <Link href={signInHref} className={buttonVariants({ className: 'w-full sm:w-auto' })}>
            Sign in or create a free account <ArrowRight className="ml-2 size-4" aria-hidden="true" />
          </Link>
        </div>
      ) : !joining ? (
        <div className="border-t pt-3">
          <Button type="button" onClick={() => setJoining(true)}>
            {isFull ? 'Join the waitlist' : 'Join this playtest'} <ArrowRight className="ml-2 size-4" aria-hidden="true" />
          </Button>
        </div>
      ) : (
        <div className="space-y-3 border-t pt-3">
          {testerInstructions?.trim() ? (
            <Alert><AlertDescription><span className="font-medium">Host’s note: </span>{testerInstructions}</AlertDescription></Alert>
          ) : null}
          <QuestionnaireFieldset
            schema={registrationSchema}
            value={responses}
            onChange={(value) => { setResponses(value); setQuestionnaireComplete(false); }}
            onComplete={() => setQuestionnaireComplete(true)}
            submitLabel="Confirm registration answers"
          />
          <div className="max-h-48 overflow-y-auto rounded-md border bg-muted/20 p-3 text-sm leading-6 whitespace-pre-wrap">
            <p className="mb-1 font-medium">Playtest rules</p>
            {generalRules?.trim() || 'No event-specific rules have been published.'}
          </div>
          <label className="flex items-start gap-3 text-sm">
            <input className="mt-1 size-4" type="checkbox" checked={acceptedRules} onChange={(event) => setAcceptedRules(event.currentTarget.checked)} />
            <span>I’ve read and agree to follow the rules for this playtest.</span>
          </label>
          <Button type="button" className="w-full" disabled={pending || !slot.id || !questionnaireComplete || !acceptedRules} onClick={register}>
            {pending ? <Loader2 className="mr-2 size-4 animate-spin" /> : null}
            {isFull ? 'Confirm waitlist request' : 'Join playtest'}
          </Button>
          <Button type="button" variant="ghost" className="w-full" disabled={pending} onClick={() => setJoining(false)}>Back to session details</Button>
        </div>
      )}

      {result ? (
        <Alert variant={result.success ? 'default' : 'destructive'} aria-live="polite">
          {result.success ? <CheckCircle2 className="size-4" /> : <AlertCircle className="size-4" />}
          <AlertDescription>{result.success ? result.message : result.error}</AlertDescription>
        </Alert>
      ) : null}
    </article>
  );
}
