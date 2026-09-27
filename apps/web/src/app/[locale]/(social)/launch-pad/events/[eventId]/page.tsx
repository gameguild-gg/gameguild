import { LaunchPadApplicationForm } from '@/components/launch-pad/launch-pad-application-form';
import { Link } from '@/i18n/navigation';
import { registerLaunchPadSlotForm } from '@/lib/launch-pad/actions';
import { getMyLaunchPadApplications, getMyLaunchPadRegistrations, getPublicLaunchPadEvent } from '@/lib/launch-pad/queries';
import { getTestingProjectVersionOptions } from '@/lib/testing-lab/queries';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { ArrowLeft, CalendarDays, Clock3, Users } from 'lucide-react';
import { notFound } from 'next/navigation';

function formatUTCDate(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return 'Date to be announced';
  return new Intl.DateTimeFormat('en-US', { timeZone: 'UTC', dateStyle: 'medium' }).format(date);
}

function formatUTCTime(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return 'Time to be announced';
  return new Intl.DateTimeFormat('en-US', { timeZone: 'UTC', hour: 'numeric', minute: '2-digit', timeZoneName: 'short' }).format(date);
}

function formatUTCTimeRange(startsAt: string, endsAt: string): string {
  const start = new Date(startsAt);
  const end = new Date(endsAt);
  if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime())) return 'Time to be announced';
  const sameUTCDate = start.toISOString().slice(0, 10) === end.toISOString().slice(0, 10);
  return sameUTCDate
    ? `${formatUTCTime(startsAt)} – ${formatUTCTime(endsAt)}`
    : `${formatUTCDate(startsAt)} ${formatUTCTime(startsAt)} – ${formatUTCDate(endsAt)} ${formatUTCTime(endsAt)}`;
}

export default async function LaunchPadEventDetailPage({ params, searchParams }: { params: Promise<{ eventId: string }>; searchParams?: Promise<{ projectId?: string }> }) {
  const { eventId } = await params;
  const { projectId } = searchParams ? await searchParams : {};
  const detail = await getPublicLaunchPadEvent(eventId);
  if (!detail) notFound();

  const [versions, applications, registrations] = await Promise.all([
    getTestingProjectVersionOptions(),
    getMyLaunchPadApplications(),
    getMyLaunchPadRegistrations(),
  ]);
  const currentApplication = applications.find((application) => application.eventId === eventId);
  const registrationIds = new Set(
    registrations
      .filter((registration) => registration.status !== 'Cancelled' && registration.status !== 5)
      .map((registration) => registration.slotId),
  );
  const eventStatusKey = typeof detail.event.status === 'number'
    ? ['Draft', 'ApplicationsOpen', 'ApplicationsClosed', 'Scheduled', 'Active', 'Completed', 'Cancelled', 'Archived'][detail.event.status]
    : detail.event.status;
  const canRegister = !['Draft', 'Completed', 'Cancelled', 'Archived'].includes(eventStatusKey ?? '');
  const canApply = eventStatusKey === 'ApplicationsOpen';

  return (
    <main className="mx-auto w-full max-w-[1280px] space-y-7 px-4 py-8 sm:px-6 lg:px-8">
      <div>
        <Link href="/launch-pad/events" className="inline-flex min-h-9 items-center gap-2 text-sm text-muted-foreground hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">
          <ArrowLeft className="size-4" aria-hidden="true" />
          All Launch Pad events
        </Link>
      </div>

      <header className="space-y-3">
        <div className="flex flex-wrap items-center gap-3">
          <h1 className="text-3xl font-semibold tracking-tight sm:text-4xl">{detail.event.name}</h1>
          <Badge variant="outline">{String(detail.event.status).replace(/([a-z])([A-Z])/g, '$1 $2')}</Badge>
        </div>
        {detail.event.description ? <p className="max-w-3xl text-base leading-7 text-muted-foreground">{detail.event.description}</p> : null}
        <div className="flex flex-wrap gap-x-6 gap-y-2 rounded-lg border border-border bg-card px-4 py-3 text-sm">
          <span className="inline-flex items-center gap-2"><CalendarDays className="size-4 text-muted-foreground" aria-hidden="true" />{formatUTCDate(detail.event.startsAt)}</span>
          <span className="inline-flex items-center gap-2"><Clock3 className="size-4 text-muted-foreground" aria-hidden="true" />{formatUTCTimeRange(detail.event.startsAt, detail.event.endsAt)}</span>
        </div>
      </header>

      <section aria-label="Ways to participate" className="grid items-start gap-5 lg:grid-cols-2">
        <Card className="border-border bg-card text-card-foreground">
          <CardHeader>
            <CardTitle className="text-xl">Submit a project</CardTitle>
            <p className="text-sm font-normal text-muted-foreground">Choose a release from a project you can represent.</p>
          </CardHeader>
          <CardContent>
            {currentApplication ? (
              <div className="space-y-2 rounded-lg border border-border bg-muted/40 p-4">
                <Badge variant="outline">{String(currentApplication.status).replace(/([a-z])([A-Z])/g, '$1 $2')}</Badge>
                <p className="text-sm text-muted-foreground">This application belongs to Project {currentApplication.projectId}.</p>
              </div>
            ) : canApply ? (
              <LaunchPadApplicationForm eventId={eventId} versions={versions} initialProjectId={projectId} />
            ) : (
              <p className="rounded-lg bg-muted/50 p-4 text-sm text-muted-foreground">Project applications are closed for this event.</p>
            )}
          </CardContent>
        </Card>

        <Card className="border-border bg-card text-card-foreground">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-xl"><Users className="size-5 text-primary" aria-hidden="true" />Join as a participant</CardTitle>
            <p className="text-sm font-normal text-muted-foreground">Help present and support community releases.</p>
          </CardHeader>
          <CardContent className="space-y-3">
            {detail.slots.length === 0 ? (
              <p className="rounded-lg bg-muted/50 p-4 text-sm text-muted-foreground">There are no participant roles configured for this event.</p>
            ) : detail.slots.map((slot) => (
              <div key={slot.id} className="flex flex-wrap items-center justify-between gap-4 rounded-lg border border-border p-4">
                <div className="min-w-0">
                  <p className="font-medium">{slot.name}</p>
                  <p className="mt-1 text-sm text-muted-foreground">{slot.reservedCount} of {slot.capacity} spots filled · {String(slot.role)}</p>
                </div>
                {registrationIds.has(slot.id) ? (
                  <Badge variant="secondary">Registered</Badge>
                ) : canRegister ? (
                  <form action={registerLaunchPadSlotForm}>
                    <input type="hidden" name="eventId" value={eventId} />
                    <input type="hidden" name="slotId" value={slot.id} />
                    <Button size="sm" type="submit">{slot.reservedCount >= slot.capacity ? 'Join waitlist' : 'Register'}</Button>
                  </form>
                ) : (
                  <Badge variant="outline">Closed</Badge>
                )}
              </div>
            ))}
          </CardContent>
        </Card>
      </section>
    </main>
  );
}
