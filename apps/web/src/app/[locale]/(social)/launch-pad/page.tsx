import { Link } from '@/i18n/navigation';
import { getPublicLaunchPadEvents, type LaunchPadEvent } from '@/lib/launch-pad/queries';
import { ArrowRight, CalendarDays, Clock3, Rocket } from 'lucide-react';
import React from 'react';

const eventStatusNames = ['Draft', 'ApplicationsOpen', 'ApplicationsClosed', 'Scheduled', 'Active', 'Completed', 'Cancelled', 'Archived'];

function getStatusKey(status: LaunchPadEvent['status']): string {
  return typeof status === 'number' ? eventStatusNames[status] ?? 'Scheduled' : status;
}

function getStatusLabel(status: LaunchPadEvent['status']): string {
  switch (getStatusKey(status)) {
    case 'ApplicationsOpen': return 'Applications open';
    case 'ApplicationsClosed': return 'Applications closed';
    case 'Active': return 'In progress';
    case 'Completed': return 'Completed';
    default: return getStatusKey(status);
  }
}

function getDateParts(value: string): { month: string; day: string; year: string } {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return { month: 'Date', day: '—', year: '' };

  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone: 'UTC',
    month: 'short',
    day: '2-digit',
    year: 'numeric',
  }).formatToParts(date);

  return {
    month: parts.find((part) => part.type === 'month')?.value ?? '',
    day: parts.find((part) => part.type === 'day')?.value ?? '',
    year: parts.find((part) => part.type === 'year')?.value ?? '',
  };
}

function formatUTCDate(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return 'Date to be announced';

  return new Intl.DateTimeFormat('en-US', {
    timeZone: 'UTC',
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  }).format(date);
}

function formatUTCTime(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return 'Time to be announced';

  return new Intl.DateTimeFormat('en-US', {
    timeZone: 'UTC',
    hour: 'numeric',
    minute: '2-digit',
    timeZoneName: 'short',
  }).format(date);
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

function LaunchEventLink({ event, compact = false }: { event: LaunchPadEvent; compact?: boolean }): React.JSX.Element {
  const date = getDateParts(event.startsAt);

  return (
    <Link
      href={`/launch-pad/events/${event.id}`}
      className={`group flex min-w-0 gap-3 rounded-lg p-3 transition-colors hover:bg-muted/70 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring ${compact ? 'items-center' : 'items-start'}`}
    >
      <time dateTime={event.startsAt} className="flex w-14 shrink-0 flex-col items-center rounded-md border border-border bg-card px-2 py-2 text-center">
        <span className="text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">{date.month}</span>
        <span className="text-xl font-semibold tabular-nums">{date.day}</span>
        <span className="text-[10px] text-muted-foreground">{date.year}</span>
      </time>
      <span className="min-w-0 flex-1">
        <span className="block truncate text-sm font-semibold group-hover:text-primary">{event.name}</span>
        <span className="mt-1 block text-xs text-muted-foreground">
          {formatUTCTime(event.startsAt)} · {getStatusLabel(event.status)}
        </span>
        {!compact && event.description ? (
          <span className="mt-2 line-clamp-2 block text-sm leading-5 text-muted-foreground">{event.description}</span>
        ) : null}
      </span>
      <ArrowRight className="mt-1 size-4 shrink-0 text-muted-foreground transition-transform group-hover:translate-x-0.5 group-hover:text-primary" aria-hidden="true" />
    </Link>
  );
}

export default async function LaunchPadPage(): Promise<React.JSX.Element> {
  const events = await getPublicLaunchPadEvents();
  const sortedEvents = [...events].sort((left, right) => Date.parse(left.startsAt) - Date.parse(right.startsAt));
  const upcomingEvents = sortedEvents.filter((event) => getStatusKey(event.status) !== 'Completed' && getStatusKey(event.status) !== 'Cancelled');
  const completedEvents = sortedEvents.filter((event) => getStatusKey(event.status) === 'Completed').reverse();
  const featuredEvent = upcomingEvents[0];

  return (
    <main className="mx-auto flex min-h-full w-full max-w-[1560px] flex-col gap-8 px-4 py-7 sm:px-6 lg:px-8">
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="mb-2 text-sm font-medium text-muted-foreground">Community launch calendar</p>
          <h1 className="text-3xl font-semibold tracking-tight sm:text-4xl">Launch Pad</h1>
          <p className="mt-2 max-w-2xl text-sm leading-6 text-muted-foreground sm:text-base">
            Find upcoming showcases, then explore the ways to take part.
          </p>
        </div>
        <Link
          href="/launch-pad/participation"
          className="inline-flex min-h-10 items-center gap-2 rounded-md px-3 text-sm font-medium text-muted-foreground transition-colors hover:bg-muted hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
        >
          Your participation
          <ArrowRight className="size-4" aria-hidden="true" />
        </Link>
      </header>

      <section aria-label="Launch Pad schedule" className="grid items-start gap-5 xl:grid-cols-[minmax(17rem,0.72fr)_minmax(0,1.28fr)]">
        <div className="rounded-xl border border-border bg-card p-4 sm:p-5">
          <div className="mb-3 flex items-center gap-2 px-3">
            <CalendarDays className="size-4 text-primary" aria-hidden="true" />
            <h2 className="text-sm font-semibold">Upcoming events</h2>
          </div>
          {upcomingEvents.length > 0 ? (
            <ol className="relative space-y-1 before:absolute before:bottom-5 before:left-[1.68rem] before:top-5 before:w-px before:bg-border">
              {upcomingEvents.map((event) => (
                <li key={event.id} className="relative">
                  <LaunchEventLink event={event} compact />
                </li>
              ))}
            </ol>
          ) : (
            <p className="px-3 py-5 text-sm leading-6 text-muted-foreground">No upcoming Launch Pad events are scheduled.</p>
          )}
          <Link href="/launch-pad/events" className="mt-3 inline-flex min-h-10 items-center gap-2 px-3 text-sm font-semibold text-primary hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">
            Browse all events
            <ArrowRight className="size-4" aria-hidden="true" />
          </Link>
        </div>

        {featuredEvent ? (
          <article className="overflow-hidden rounded-xl border border-border bg-card">
            <div className="grid md:grid-cols-[180px_minmax(0,1fr)]">
              <div className="flex min-h-48 flex-col items-center justify-center border-b border-border bg-muted/50 p-5 text-center md:min-h-full md:border-b-0 md:border-r">
                <span className="mb-3 inline-flex size-11 items-center justify-center rounded-full bg-primary/10 text-primary">
                  <Rocket className="size-5" aria-hidden="true" />
                </span>
                <span className="text-xs font-semibold uppercase tracking-[0.14em] text-muted-foreground">Next up</span>
                <time dateTime={featuredEvent.startsAt} className="mt-2 flex flex-col">
                  <span className="text-sm font-semibold text-muted-foreground">{getDateParts(featuredEvent.startsAt).month}</span>
                  <span className="text-5xl font-semibold leading-none tracking-tight tabular-nums">{getDateParts(featuredEvent.startsAt).day}</span>
                  <span className="mt-1 text-sm text-muted-foreground">{getDateParts(featuredEvent.startsAt).year}</span>
                </time>
              </div>
              <div className="flex flex-col justify-center p-5 sm:p-7 lg:p-9">
                <span className="mb-3 inline-flex w-fit items-center rounded-full border border-border px-3 py-1 text-xs font-medium text-muted-foreground">
                  {getStatusLabel(featuredEvent.status)}
                </span>
                <h2 className="text-2xl font-semibold tracking-tight sm:text-3xl">{featuredEvent.name}</h2>
                <p className="mt-3 max-w-2xl text-sm leading-6 text-muted-foreground sm:text-base">
                  {featuredEvent.description || 'See the schedule and participation options for this community launch event.'}
                </p>
                <dl className="mt-6 grid gap-4 border-t border-border pt-5 sm:grid-cols-2">
                  <div className="flex items-start gap-3">
                    <CalendarDays className="mt-0.5 size-4 shrink-0 text-muted-foreground" aria-hidden="true" />
                    <div>
                      <dt className="text-xs text-muted-foreground">Date</dt>
                      <dd className="mt-1 text-sm font-medium">{formatUTCDate(featuredEvent.startsAt)}</dd>
                    </div>
                  </div>
                  <div className="flex items-start gap-3">
                    <Clock3 className="mt-0.5 size-4 shrink-0 text-muted-foreground" aria-hidden="true" />
                    <div>
                      <dt className="text-xs text-muted-foreground">Time</dt>
                      <dd className="mt-1 text-sm font-medium">
                        {formatUTCTimeRange(featuredEvent.startsAt, featuredEvent.endsAt)}
                      </dd>
                    </div>
                  </div>
                  {featuredEvent.applicationsCloseAt ? (
                    <div className="flex items-start gap-3 sm:col-span-2">
                      <CalendarDays className="mt-0.5 size-4 shrink-0 text-muted-foreground" aria-hidden="true" />
                      <div>
                        <dt className="text-xs text-muted-foreground">Applications close</dt>
                        <dd className="mt-1 text-sm font-medium">{formatUTCDate(featuredEvent.applicationsCloseAt)}</dd>
                      </div>
                    </div>
                  ) : null}
                </dl>
                <Link
                  href={`/launch-pad/events/${featuredEvent.id}`}
                  className="mt-6 inline-flex min-h-11 w-fit items-center justify-center gap-2 rounded-md bg-primary px-4 py-2 text-sm font-semibold text-primary-foreground transition-colors hover:bg-primary/90 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-card"
                >
                  {getStatusKey(featuredEvent.status) === 'ApplicationsOpen' ? 'See signup options' : 'View event'}
                  <ArrowRight className="size-4" aria-hidden="true" />
                </Link>
              </div>
            </div>
          </article>
        ) : (
          <div className="flex min-h-64 flex-col justify-center rounded-xl border border-dashed border-border bg-card px-6 py-10 text-center">
            <h2 className="text-xl font-semibold">No upcoming launch event</h2>
            <p className="mx-auto mt-2 max-w-md text-sm leading-6 text-muted-foreground">
              New community showcases will appear here when their schedule is published.
            </p>
            <Link href="/projects" className="mx-auto mt-4 inline-flex min-h-10 items-center gap-2 rounded-md px-3 text-sm font-semibold text-primary hover:bg-primary/10 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">
              Explore community projects
              <ArrowRight className="size-4" aria-hidden="true" />
            </Link>
          </div>
        )}
      </section>

      {completedEvents.length > 0 ? (
        <section aria-labelledby="past-launch-events-title" className="space-y-3">
          <div className="flex items-baseline justify-between gap-3">
            <div>
              <h2 id="past-launch-events-title" className="text-xl font-semibold tracking-tight">Past events</h2>
              <p className="mt-1 text-sm text-muted-foreground">Recent community showcases.</p>
            </div>
            <Link href="/launch-pad/events" className="inline-flex min-h-10 items-center gap-2 rounded-md px-3 text-sm font-medium text-muted-foreground hover:bg-muted hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">
              All events <ArrowRight className="size-4" aria-hidden="true" />
            </Link>
          </div>
          <div className="divide-y divide-border rounded-xl border border-border bg-card px-2">
            {completedEvents.slice(0, 3).map((event) => <LaunchEventLink key={event.id} event={event} />)}
          </div>
        </section>
      ) : null}
    </main>
  );
}
