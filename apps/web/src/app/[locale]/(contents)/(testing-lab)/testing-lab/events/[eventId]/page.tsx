import { TestingFeedbackSubmission } from '@/components/testing-lab/testing-feedback-submission';
import { TestingProjectApplication } from '@/components/testing-lab/testing-project-application';
import { TestingSlotRegistration } from '@/components/testing-lab/testing-slot-registration';
import { Link } from '@/i18n/navigation';
import { getPublicTestingEventExperience } from '@/lib/testing-lab/events-queries';
import { getTestingProjectOptions } from '@/lib/testing-lab/queries';
import { Badge } from '@game-guild/ui/components/badge';
import { ArrowLeft, CalendarDays, ClipboardCheck, FlaskConical, MessageSquareText } from 'lucide-react';
import { notFound } from 'next/navigation';

function formatDateTime(value?: string | null) {
  if (!value) return 'Not scheduled';
  const date = new Date(value);
  if (Number.isNaN(date.valueOf())) return 'Not scheduled';
  return new Intl.DateTimeFormat('en', { dateStyle: 'full', timeStyle: 'short' }).format(date);
}

export default async function PublicTestingEventDetailPage({
  params,
}: {
  params: Promise<{ eventId: string }>;
}) {
  const { eventId } = await params;
  const experience = await getPublicTestingEventExperience(eventId);
  if (!experience.event && experience.accessIssues.length === 0) notFound();

  if (!experience.event) {
    console.error(
      `[testing-lab] public event ${eventId} could not be loaded`,
      experience.accessIssues,
    );
    return (
      <main className="min-h-screen bg-slate-950 px-4 py-16 text-white">
        <div className="mx-auto max-w-4xl rounded-md border border-amber-400/30 bg-amber-400/5 p-6">
          <h1 className="text-2xl font-semibold text-amber-100">Event temporarily unavailable</h1>
          <p className="mt-2 text-sm leading-6 text-amber-100/80">
            The event details could not be loaded. Return to the directory and try again shortly.
          </p>
          <Link href="/testing-lab/events" className="mt-5 inline-flex text-sm font-medium text-sky-200 hover:text-sky-100">
            Back to Testing Lab events
          </Link>
        </div>
      </main>
    );
  }

  const event = experience.event;
  const projects = experience.isAuthenticated ? await getTestingProjectOptions() : [];
  const application = experience.applications[0];
  const acceptsApplications = event.status === 'ApplicationsOpen';

  return (
    <main className="min-h-screen bg-slate-950 text-white">
      <section className="border-b border-white/10">
        <div className="mx-auto w-full max-w-7xl px-4 py-12 sm:px-6 lg:px-8">
          <Link href="/testing-lab" className="inline-flex items-center text-sm text-slate-400 hover:text-white">
            <ArrowLeft className="mr-2 size-4" />
            Testing Lab events
          </Link>
          <div className="mt-8 grid gap-8 lg:grid-cols-[minmax(0,1fr)_360px] lg:items-end">
            <div>
              <div className="flex flex-wrap gap-2">
                <Badge className="bg-sky-300/10 text-sky-200">{event.status}</Badge>
                <Badge variant="outline" className="border-white/15 text-slate-300">
                  {event.mode === 'InPerson' ? 'In person' : event.mode}
                </Badge>
                <Badge variant="outline" className="border-white/15 text-slate-300">
                  {event.approvalMode === 'Committee' ? 'Committee review' : 'Manager decision'}
                </Badge>
              </div>
              <h1 className="mt-5 text-4xl font-semibold sm:text-5xl">{event.name}</h1>
              <p className="mt-4 max-w-3xl text-lg leading-8 text-slate-300">
                {event.description ?? 'A managed community project testing event.'}
              </p>
            </div>
            <dl className="grid gap-3 rounded-md border border-white/10 bg-white/[0.03] p-4 text-sm">
              <div>
                <dt className="text-slate-500">Applications</dt>
                <dd className="mt-1 text-slate-200">
                  {formatDateTime(event.applicationsOpenAt)} to {formatDateTime(event.applicationsCloseAt)}
                </dd>
              </div>
              <div>
                <dt className="text-slate-500">Event window</dt>
                <dd className="mt-1 text-slate-200">
                  {formatDateTime(event.startsAt)} to {formatDateTime(event.endsAt)}
                </dd>
              </div>
            </dl>
          </div>
        </div>
      </section>

      {experience.accessIssues.length > 0 ? (
        <div className="mx-auto w-full max-w-7xl px-4 pt-6 sm:px-6 lg:px-8">
          <div className="rounded-md border border-amber-400/30 bg-amber-400/5 p-4 text-sm text-amber-100">
            Some account-specific participation data could not be loaded. Public event details remain available.
          </div>
        </div>
      ) : null}

      <div className="mx-auto grid w-full max-w-7xl gap-10 px-4 py-12 sm:px-6 lg:grid-cols-[minmax(0,1fr)_minmax(340px,0.45fr)] lg:px-8">
        <div className="space-y-10">
          <section aria-labelledby="event-schedules">
            <div className="mb-4 flex items-center gap-3">
              <CalendarDays className="size-5 text-sky-200" />
              <div>
                <h2 id="event-schedules" className="text-2xl font-semibold">Schedules and tester capacity</h2>
                <p className="text-sm text-slate-400">Each schedule has independent tester and approved-project limits.</p>
              </div>
            </div>
            {(event.slots ?? []).length === 0 ? (
              <p className="rounded-md border border-dashed border-white/15 p-8 text-sm text-slate-400">
                The event schedule has not been published yet.
              </p>
            ) : (
              <div className="space-y-4">
                {(event.slots ?? []).map((slot) => (
                  <TestingSlotRegistration
                    key={slot.id}
                    eventId={eventId}
                    isAuthenticated={experience.isAuthenticated}
                    slot={slot}
                    registration={experience.registrations.find((registration) => registration.slotId === slot.id)}
                  />
                ))}
              </div>
            )}
          </section>

          {event.requiresFeedback || experience.feedbackObligations.length > 0 ? (
            <section aria-labelledby="required-feedback">
              <div className="mb-4 flex items-center gap-3">
                <MessageSquareText className="size-5 text-sky-200" />
                <div>
                  <h2 id="required-feedback" className="text-2xl font-semibold">Required feedback</h2>
                  <p className="text-sm text-slate-400">
                    Assigned testers must complete structured feedback for each project they test.
                  </p>
                </div>
              </div>
              <TestingFeedbackSubmission
                eventId={eventId}
                isAuthenticated={experience.isAuthenticated}
                obligations={experience.feedbackObligations}
              />
            </section>
          ) : null}
        </div>

        <aside className="space-y-6">
          <section className="rounded-md border border-white/10 bg-white/[0.03] p-5">
            <div className="flex items-center gap-3">
              <ClipboardCheck className="size-5 text-sky-200" />
              <div>
                <h2 className="font-semibold">Apply with a project</h2>
                <p className="text-sm text-slate-400">Candidate projects are reviewed before consuming capacity.</p>
              </div>
            </div>
            <div className="mt-5">
              <TestingProjectApplication
                eventId={eventId}
                isAuthenticated={experience.isAuthenticated}
                acceptsApplications={acceptsApplications}
                projects={projects}
                application={application?.id ? {
                  id: application.id,
                  status: application.status,
                  decisionRationale: application.decisionRationale,
                } : undefined}
              />
            </div>
          </section>

          <section className="rounded-md border border-white/10 bg-white/[0.03] p-5">
            <FlaskConical className="size-5 text-sky-200" />
            <h2 className="mt-3 font-semibold">How selection works</h2>
            <ol className="mt-4 space-y-3 text-sm leading-6 text-slate-400">
              <li>1. Submit an existing GameGuild project.</li>
              <li>2. The manager or committee reviews the candidacy.</li>
              <li>3. Approval assigns a schedule and reserves project capacity.</li>
              <li>4. Tester registrations and feedback are tracked separately.</li>
            </ol>
          </section>
        </aside>
      </div>
    </main>
  );
}
