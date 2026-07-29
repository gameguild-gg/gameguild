'use client';

import {
  cancelTestingEventRegistration,
  registerForTestingEventSlot,
  type TestingEventActionResult,
} from '@/lib/testing-lab/events-actions';
import { Alert, AlertDescription } from '@game-guild/ui/components/alert';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { AlertCircle, CalendarDays, CheckCircle2, Loader2, MapPin, UsersRound } from 'lucide-react';
import Link from 'next/link';
import { useState, useTransition } from 'react';

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

function formatSchedule(value?: string | null) {
  if (!value) return 'Schedule pending';
  const date = new Date(value);
  if (Number.isNaN(date.valueOf())) return 'Schedule pending';
  return new Intl.DateTimeFormat('en', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(date);
}

export function TestingSlotRegistration({
  eventId,
  isAuthenticated,
  slot,
  registration,
}: {
  eventId: string;
  isAuthenticated: boolean;
  slot: PublicSlot;
  registration?: CurrentRegistration;
}) {
  const [pending, startTransition] = useTransition();
  const [result, setResult] = useState<TestingEventActionResult<unknown> | null>(null);
  const isFull = (slot.availableTesterCount ?? 0) <= 0;
  const location = [slot.campusName, slot.roomName].filter(Boolean).join(' · ');

  function register() {
    const formData = new FormData();
    formData.set('eventId', eventId);
    formData.set('slotId', slot.id ?? '');
    startTransition(async () => {
      const next = await registerForTestingEventSlot(formData);
      setResult(next);
    });
  }

  function cancel() {
    const formData = new FormData();
    formData.set('eventId', eventId);
    formData.set('registrationId', registration?.id ?? '');
    startTransition(async () => {
      const next = await cancelTestingEventRegistration(formData);
      setResult(next);
    });
  }

  return (
    <article className="space-y-4 rounded-md border bg-card p-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="space-y-2">
          <div className="flex items-center gap-2">
            <Badge variant="outline">{slot.mode === 'InPerson' ? 'In person' : slot.mode ?? 'Online'}</Badge>
            {isFull ? <Badge variant="secondary">Waitlist available</Badge> : null}
          </div>
          <p className="flex items-center gap-2 text-sm">
            <CalendarDays className="size-4 text-muted-foreground" />
            {formatSchedule(slot.startsAt)}
          </p>
          {location ? (
            <p className="flex items-center gap-2 text-sm text-muted-foreground">
              <MapPin className="size-4" />
              {location}
            </p>
          ) : null}
        </div>
        <div className="text-right text-sm text-muted-foreground">
          <p className="flex items-center justify-end gap-2">
            <UsersRound className="size-4" />
            {slot.registeredTesterCount ?? 0} of {slot.maxTesters ?? 'unlimited'} testers
          </p>
          <p>
            Approved projects use {slot.approvedProjectCount ?? 0} of {slot.maxProjects ?? 'unlimited'} slots
          </p>
        </div>
      </div>

      {!isAuthenticated ? (
        <Button asChild className="w-full">
          <Link href="/sign-in">Sign in to register</Link>
        </Button>
      ) : registration ? (
        <div className="flex flex-wrap items-center justify-between gap-3 border-t pt-4">
          <div>
            <p className="font-medium">{registration.status ?? 'Registered'}</p>
            {registration.status === 'Waitlisted' && registration.waitlistPosition ? (
              <p className="text-sm text-muted-foreground">Waitlist position {registration.waitlistPosition}</p>
            ) : null}
          </div>
          {!['Cancelled', 'Completed', 'NoShow'].includes(registration.status ?? '') ? (
            <Button type="button" variant="outline" disabled={pending} onClick={cancel}>
              {pending ? <Loader2 className="mr-2 size-4 animate-spin" /> : null}
              Cancel registration
            </Button>
          ) : null}
        </div>
      ) : (
        <Button type="button" className="w-full" disabled={pending || !slot.id} onClick={register}>
          {pending ? <Loader2 className="mr-2 size-4 animate-spin" /> : null}
          {isFull ? 'Join waitlist' : 'Reserve tester seat'}
        </Button>
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
