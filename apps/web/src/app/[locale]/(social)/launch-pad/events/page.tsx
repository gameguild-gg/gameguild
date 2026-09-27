import { Link } from '@/i18n/navigation';
import { getPublicLaunchPadEvents } from '@/lib/launch-pad/queries';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { CalendarDays, ArrowLeft, Rocket } from 'lucide-react';

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

export default async function LaunchPadEventsPage({ searchParams }: { searchParams?: Promise<{ projectId?: string }> }) {
  const { projectId } = searchParams ? await searchParams : {};
  const events = await getPublicLaunchPadEvents();

  return (
    <main className="mx-auto w-full max-w-[1560px] space-y-7 px-4 py-8 sm:px-6 lg:px-8">
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <Link href="/launch-pad" className="mb-4 inline-flex min-h-9 items-center gap-2 text-sm text-muted-foreground hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">
            <ArrowLeft className="size-4" aria-hidden="true" />
            Launch Pad
          </Link>
          <p className="text-sm font-medium text-muted-foreground">Community calendar</p>
          <h1 className="mt-1 text-3xl font-semibold tracking-tight">Launch Pad events</h1>
          <p className="mt-2 max-w-2xl text-sm leading-6 text-muted-foreground">
            Browse upcoming showcases and see how to take part as a project team or participant.
          </p>
        </div>
        <Button nativeButton={false} variant="outline" render={<Link href="/launch-pad/participation" />}>Your participation</Button>
      </header>

      {events.length === 0 ? (
        <div className="rounded-xl border border-dashed border-border px-6 py-12 text-center">
          <Rocket className="mx-auto size-6 text-muted-foreground" aria-hidden="true" />
          <h2 className="mt-3 text-lg font-semibold">No public event is scheduled</h2>
          <p className="mt-2 text-sm text-muted-foreground">New Launch Pad events will appear here when their schedule is published.</p>
        </div>
      ) : (
        <section aria-label="Public Launch Pad events" className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {[...events].sort((a, b) => Date.parse(a.startsAt) - Date.parse(b.startsAt)).map((event) => (
            <Card key={event.id} className="border-border bg-card text-card-foreground">
              <CardHeader className="space-y-3">
                <div className="flex items-start justify-between gap-3">
                  <CardTitle className="flex items-start gap-2 text-lg leading-6">
                    <Rocket className="mt-0.5 size-4 shrink-0 text-primary" aria-hidden="true" />
                    {event.name}
                  </CardTitle>
                  <Badge variant="outline" className="shrink-0">{String(event.status).replace(/([a-z])([A-Z])/g, '$1 $2')}</Badge>
                </div>
                {event.description ? <p className="text-sm leading-5 text-muted-foreground">{event.description}</p> : null}
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex items-start gap-3 border-t border-border pt-4 text-sm">
                  <CalendarDays className="mt-0.5 size-4 shrink-0 text-muted-foreground" aria-hidden="true" />
                  <div>
                    <p className="font-medium">{formatUTCDate(event.startsAt)}</p>
                    <p className="mt-1 text-muted-foreground">{formatUTCTimeRange(event.startsAt, event.endsAt)}</p>
                  </div>
                </div>
                <Button nativeButton={false} className="w-full" render={
                  <Link href={projectId ? `/launch-pad/events/${event.id}?projectId=${encodeURIComponent(projectId)}` : `/launch-pad/events/${event.id}`} />
                }>
                  {event.status === 'ApplicationsOpen' ? 'See signup options' : 'View event'}
                </Button>
              </CardContent>
            </Card>
          ))}
        </section>
      )}
    </main>
  );
}
