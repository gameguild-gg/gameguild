import { Link } from '@/i18n/navigation';
import { getPublicTestingEventsDirectory } from '@/lib/testing-lab/events-queries';
import { Badge } from '@game-guild/ui/components/badge';
import { ArrowRight, CalendarDays, ClipboardCheck, FlaskConical, MapPin, UsersRound } from 'lucide-react';
import type { ReactNode } from 'react';

const testingSteps = [
  ['Connect a project', 'Apply with a project already created on GameGuild.'],
  ['Manager review', 'A manager or review committee decides which projects join each slot.'],
  ['Reserve a tester seat', 'Register for an available schedule or enter its waitlist.'],
  ['Deliver feedback', 'Complete structured feedback for the projects assigned to you.'],
] as const;

function formatDateTime(value?: string | null) {
  if (!value) return 'Schedule pending';
  const date = new Date(value);
  if (Number.isNaN(date.valueOf())) return 'Schedule pending';
  return new Intl.DateTimeFormat('en', { dateStyle: 'medium', timeStyle: 'short' }).format(date);
}

function Metric({ icon, children }: { icon: ReactNode; children: ReactNode }) {
  return <span className="inline-flex items-center gap-2 text-sm text-slate-300">{icon}{children}</span>;
}

export default async function TestingLabPage() {
  const directory = await getPublicTestingEventsDirectory({ take: 100 });

  return (
    <main className="min-h-screen bg-slate-950 text-white">
      <section className="border-b border-white/10">
        <div className="mx-auto grid w-full max-w-7xl gap-10 px-4 py-16 sm:px-6 lg:grid-cols-[minmax(0,1fr)_minmax(420px,0.8fr)] lg:px-8 lg:py-20">
          <div className="max-w-3xl space-y-6">
            <Badge variant="outline" className="border-sky-300/30 text-sky-200">Community playtesting</Badge>
            <h1 className="text-4xl font-semibold sm:text-5xl">Testing Lab</h1>
            <p className="text-lg leading-8 text-slate-300">
              Put member projects in front of real testers through managed online and campus events, structured review,
              attendance, and actionable feedback.
            </p>
            <div className="flex flex-wrap gap-3">
              <Link
                href="#events"
                className="inline-flex items-center rounded-md bg-sky-300 px-5 py-3 text-sm font-semibold text-slate-950 transition hover:bg-sky-200"
              >
                Find an event
                <ArrowRight className="ml-2 size-4" />
              </Link>
              <Link
                href="/dashboard/projects"
                className="inline-flex items-center rounded-md border border-white/15 px-5 py-3 text-sm font-semibold transition hover:bg-white/10"
              >
                Manage projects
              </Link>
            </div>
          </div>
          <ol className="grid gap-px overflow-hidden rounded-md border border-white/10 bg-white/10 sm:grid-cols-2">
            {testingSteps.map(([title, description], index) => (
              <li key={title} className="bg-slate-950 p-5">
                <span className="text-xs font-semibold text-sky-300">0{index + 1}</span>
                <h2 className="mt-3 font-semibold">{title}</h2>
                <p className="mt-2 text-sm leading-6 text-slate-400">{description}</p>
              </li>
            ))}
          </ol>
        </div>
      </section>

      <section id="events" className="mx-auto w-full max-w-7xl px-4 py-14 sm:px-6 lg:px-8">
        <div className="mb-8 flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <h2 className="text-3xl font-semibold">Open and upcoming events</h2>
            <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-400">
              Applications never consume project capacity until a manager approves them. Tester capacity is managed
              independently for each schedule.
            </p>
          </div>
          <Badge variant="outline" className="w-fit border-white/15 text-slate-200">
            {directory.events.length} available
          </Badge>
        </div>

        {directory.accessIssues.length > 0 ? (
          <div className="mb-6 rounded-md border border-amber-400/30 bg-amber-400/5 p-4 text-sm text-amber-100">
            Live Testing Lab events are temporarily unavailable. Retry shortly.
          </div>
        ) : null}

        {directory.events.length === 0 ? (
          <div className="rounded-md border border-dashed border-white/15 p-10 text-center">
            <CalendarDays className="mx-auto size-8 text-slate-500" />
            <h3 className="mt-4 font-semibold">No public testing events</h3>
            <p className="mt-2 text-sm text-slate-400">
              Events appear here when their application window opens or their approved schedule is published.
            </p>
          </div>
        ) : (
          <div className="grid gap-4 lg:grid-cols-2">
            {directory.events.map((event) => {
              const slots = event.slots ?? [];
              const testerAvailability = slots.reduce((total, slot) => total + (slot.availableTesterCount ?? 0), 0);
              const locations = [...new Set(slots.map((slot) => slot.campusName).filter(Boolean))];
              return (
                <article key={event.id} className="flex flex-col rounded-md border border-white/10 bg-slate-900/70 p-6">
                  <div className="flex flex-wrap items-center justify-between gap-3">
                    <div className="flex flex-wrap gap-2">
                      <Badge className="bg-sky-300/10 text-sky-200">{event.status}</Badge>
                      <Badge variant="outline" className="border-white/15 text-slate-300">
                        {event.mode === 'InPerson' ? 'In person' : event.mode}
                      </Badge>
                    </div>
                    <span className="text-xs text-slate-400">{event.applicationCount ?? 0} applications</span>
                  </div>
                  <h3 className="mt-5 text-xl font-semibold">{event.name}</h3>
                  <p className="mt-2 line-clamp-3 text-sm leading-6 text-slate-400">
                    {event.description ?? 'A managed GameGuild project testing event.'}
                  </p>
                  <div className="my-5 grid gap-3 border-y border-white/10 py-4 sm:grid-cols-2">
                    <Metric icon={<CalendarDays className="size-4 text-slate-500" />}>
                      {formatDateTime(event.startsAt)}
                    </Metric>
                    <Metric icon={<UsersRound className="size-4 text-slate-500" />}>
                      {testerAvailability} tester seats open
                    </Metric>
                    <Metric icon={<ClipboardCheck className="size-4 text-slate-500" />}>
                      {slots.length} {slots.length === 1 ? 'schedule' : 'schedules'}
                    </Metric>
                    <Metric icon={<MapPin className="size-4 text-slate-500" />}>
                      {locations.length > 0 ? locations.join(', ') : event.mode === 'Online' ? 'Online' : 'Location pending'}
                    </Metric>
                  </div>
                  <Link
                    href={`/testing-lab/events/${event.id}`}
                    className="mt-auto inline-flex items-center text-sm font-semibold text-sky-200 hover:text-sky-100"
                  >
                    View event and participate
                    <ArrowRight className="ml-2 size-4" />
                  </Link>
                </article>
              );
            })}
          </div>
        )}
      </section>

      <section className="border-t border-white/10 bg-white/[0.03]">
        <div className="mx-auto flex w-full max-w-7xl flex-col gap-4 px-4 py-12 sm:px-6 md:flex-row md:items-center md:justify-between lg:px-8">
          <div>
            <FlaskConical className="size-6 text-sky-200" />
            <h2 className="mt-3 text-xl font-semibold">Testing Lab managers</h2>
            <p className="mt-1 text-sm text-slate-400">Create events, review candidates, manage capacity, and monitor feedback.</p>
          </div>
          <Link
            href="/dashboard/testing-lab/events"
            className="inline-flex w-fit items-center rounded-md border border-white/15 px-4 py-2 text-sm font-semibold hover:bg-white/10"
          >
            Open event management
            <ArrowRight className="ml-2 size-4" />
          </Link>
        </div>
      </section>
    </main>
  );
}
