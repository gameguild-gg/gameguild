import { TestingEventCommittee } from '@/components/testing-lab/testing-event-management';
import { TestingEventConfigurationEditor } from '@/components/testing-lab/testing-event-configuration-editor';
import { TestingLabPageHeader } from '@/components/testing-lab/testing-lab-page-header';
import { Link } from '@/i18n/navigation';
import { getMembers } from '@/lib/community/queries/members';
import {
  countLabel,
  formatEventDateRange,
  isTestingEventReadOnly,
} from '@/lib/testing-lab/event-workspace';
import { getTestingEventWorkspaceData } from '@/lib/testing-lab/events-queries';
import { Badge } from '@game-guild/ui/components/badge';
import { buttonVariants } from '@game-guild/ui/components/button-variants';
import {
  ArrowRight,
  CalendarClock,
  ClipboardList,
  UsersRound,
} from 'lucide-react';
import { notFound } from 'next/navigation';

export default async function TestingEventOverviewPage({
  params,
}: {
  params: Promise<{ eventId: string }>;
}) {
  const { eventId } = await params;
  const [detail, memberDirectory] = await Promise.all([
    getTestingEventWorkspaceData(eventId),
    getMembers({ page: 1, limit: 100 }),
  ]);

  if (!detail.event) notFound();
  const event = detail.event;
  const registrations = Object.values(detail.registrationsBySlot).flat().filter((registration) => registration.status !== 'Cancelled');
  const testerCount = new Set(registrations.map((registration) => registration.userId ?? registration.id)).size;
  const pendingApplications = detail.applications.filter((item) =>
    ['Pending', 'UnderReview', 'Waitlisted'].includes(item.status ?? 'Pending'),
  ).length;
  const readOnly = isTestingEventReadOnly(event);

  const metrics = [
    {
      label: countLabel(detail.applications.length, 'project application'),
      note: countLabel(pendingApplications, 'awaiting decision'),
      icon: ClipboardList,
      destination: 'applications',
    },
    {
      label: countLabel(detail.slots.length, 'testing slot'),
      note: detail.slots.length ? 'View dates and capacity' : 'Add dates and capacity',
      icon: CalendarClock,
      destination: 'schedule',
    },
    {
      label: countLabel(testerCount, 'registered tester'),
      note: 'View participants and attendance',
      icon: UsersRound,
      destination: 'participants',
    },
  ];

  return (
    <div className="space-y-6">
      <TestingLabPageHeader
        headingLevel={2}
        icon={CalendarClock}
        title="Event overview"
        description="Set up the playtest, review sign-ups, and track participation."
      />

      <section aria-label="Event metrics" className="grid gap-3 md:grid-cols-3">
        {metrics.map(({ label, note, icon: Icon, destination }) => (
          <Link href={`/workspace/testing-lab/events/${eventId}/${destination}`} key={label} className="rounded-md border p-4 transition-colors hover:bg-muted/30 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">
            <div className="flex items-center gap-2 text-muted-foreground">
              <Icon className="size-4" />
              <p className="text-sm">{label}</p>
            </div>
            <p className="mt-3 text-sm font-medium">{note}</p>
          </Link>
        ))}
      </section>

      <section className={event.approvalMode === 'Committee' ? "grid gap-4 xl:grid-cols-[minmax(0,1fr)_minmax(320px,0.7fr)]" : "grid gap-4"}>
        <article className="rounded-md border p-4">
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div>
              <h2 className="font-semibold">Dates & sign-ups</h2>
              <p className="mt-1 text-sm text-muted-foreground">
                All times use {event.timeZoneId ?? "UTC"} · 24-hour clock.
              </p>
            </div>
            <Badge variant="outline">{event.mode}</Badge>
          </div>
          <dl className="mt-4 grid gap-4 sm:grid-cols-2">
            <div>
              <dt className="text-sm font-medium text-muted-foreground">Sign-ups open → close</dt>
              <dd className="mt-1 text-sm">
                {formatEventDateRange(event.applicationsOpenAt, event.applicationsCloseAt, event.timeZoneId ?? "UTC")}
              </dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-muted-foreground">Session starts → ends</dt>
              <dd className="mt-1 text-sm">
                {formatEventDateRange(event.startsAt, event.endsAt, event.timeZoneId ?? "UTC")}
              </dd>
            </div>
          </dl>
          <div className="mt-4 flex flex-wrap gap-2 border-t pt-4">
            <Link
              href={`/workspace/testing-lab/events/${eventId}/applications`}
              className={buttonVariants({ size: 'sm', variant: 'outline' })}
            >
              Review applications <ArrowRight className="ml-2 size-4" />
            </Link>
            <Link
              href={`/workspace/testing-lab/events/${eventId}/schedule`}
              className={buttonVariants({ size: 'sm', variant: 'outline' })}
            >
              Manage schedule <ArrowRight className="ml-2 size-4" />
            </Link>
          </div>
        </article>

        {event.approvalMode === 'Committee' ? <article className="rounded-md border p-4">
          <TestingEventCommittee
            event={event}
            members={memberDirectory.members.map((member) => ({
              id: member.id,
              label: `${member.displayName} / ${member.email}`,
            }))}
            committee={detail.committee}
            readOnly={readOnly}
          />
        </article> : null}
      </section>

      <TestingEventConfigurationEditor
        eventId={eventId}
        status={event.status}
        configuration={event.configuration}
      />

      <details className="rounded-md border p-4">
        <summary className="cursor-pointer text-sm font-medium">Course integration {event.courseId ? '· connected' : '· optional'}</summary>
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h2 className="font-semibold">Learning evidence</h2>
            <p className="mt-1 text-sm text-muted-foreground">
              {event.courseId
                ? 'Attendance and feedback can publish evidence to the connected course activity.'
                : 'No course activity is connected. Testing Lab evidence remains available in this event.'}
            </p>
          </div>
          <Link
            href={`/workspace/testing-lab/events/${eventId}/learning`}
            className={buttonVariants({ size: 'sm', variant: 'outline' })}
          >
            Open learning setup <ArrowRight className="ml-2 size-4" />
          </Link>
        </div>
      </details>
    </div>
  );
}
