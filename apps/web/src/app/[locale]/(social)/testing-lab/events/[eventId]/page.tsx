import { TestingFeedbackSubmission } from '@/components/testing-lab/testing-feedback-submission';
import { TestingProjectApplication } from '@/components/testing-lab/testing-project-application';
import { TestingSlotRegistration } from '@/components/testing-lab/testing-slot-registration';
import { EventHeroMedia } from '@/components/testing-lab/landing/event-hero-media';
import { Link } from '@/i18n/navigation';
import {
  getApprovedTestingEventGames,
  getPublicTestingEventExperience,
} from '@/lib/testing-lab/events-queries';
import { getTestingProjectVersionOptions } from '@/lib/testing-lab/queries';
import type { TestingLabPublicTestingEventSlotProjection } from '@game-guild/client';
import {
  CalendarDays,
  ClipboardCheck,
  FlaskConical,
  Gamepad2,
  MapPin,
  MessageSquareText,
  Users,
} from 'lucide-react';
import type { ReactNode } from 'react';
import { notFound } from 'next/navigation';

// The public event body includes account-specific applications, registrations,
// and feedback obligations. Never reuse an anonymous render for an authenticated
// visitor, even when the public event projection itself is cacheable.
export const dynamic = 'force-dynamic';

function formatDateTime(value?: string | null) {
  if (!value) return 'Not scheduled';
  const date = new Date(value);
  if (Number.isNaN(date.valueOf())) return 'Not scheduled';
  const formatted = new Intl.DateTimeFormat('en', {
    dateStyle: 'full',
    timeStyle: 'short',
    timeZone: 'UTC',
  }).format(date);
  return `${formatted} UTC`;
}

function formatDay(value?: string | null) {
  if (!value) return 'Not scheduled';
  const date = new Date(value);
  if (Number.isNaN(date.valueOf())) return 'Not scheduled';
  return new Intl.DateTimeFormat('en', { month: 'short', day: 'numeric', timeZone: 'UTC' }).format(date);
}

function statusLabel(status?: string | null) {
  if (status === 'ApplicationsOpen') return 'Open';
  if (status === 'Scheduled') return 'Scheduled';
  if (status === 'Active') return 'Live now';
  if (status === 'Completed') return 'Completed';
  return status ?? 'Event';
}


function HeroChip({ children }: { children: ReactNode }) {
  return (
    <span className="inline-flex items-center gap-1.5 rounded-full border border-white/25 bg-black/55 px-3 py-1 text-xs font-medium text-white backdrop-blur-sm">
      {children}
    </span>
  );
}

export default async function Page({
  params,
  searchParams,
}: {
  params: Promise<{ eventId: string }>;
  searchParams?: Promise<{ projectId?: string }>;
}) {
  const { eventId } = await params;
  const { projectId } = searchParams ? await searchParams : {};
  const experience = await getPublicTestingEventExperience(eventId);
  if (!experience.event && experience.accessIssues.length === 0) notFound();

  if (!experience.event) {
    console.error(
      `[testing-lab] public event ${eventId} could not be loaded`,
      experience.accessIssues,
    );
    return (
      <main className="px-4 py-16">
        <div className="mx-auto max-w-4xl rounded-2xl border border-destructive/30 bg-destructive/10 p-6">
          <h1 className="text-2xl font-semibold text-foreground">Event temporarily unavailable</h1>
          <p className="mt-2 text-sm leading-6 text-muted-foreground">
            The event details could not be loaded. Return to the directory and try again shortly.
          </p>
          <Link href="/testing-lab" className="mt-5 inline-flex text-sm font-medium text-primary hover:text-primary/80">
            Back to Testing Lab events
          </Link>
        </div>
      </main>
    );
  }

  const event = experience.event;
  const [projectVersions, games] = await Promise.all([
    experience.isAuthenticated ? getTestingProjectVersionOptions() : Promise.resolve([]),
    getApprovedTestingEventGames(eventId),
  ]);
  const acceptsApplications = event.status === 'ApplicationsOpen';
  const slots = event.slots ?? [];

  const modeLabel = event.mode === 'InPerson' ? 'In person' : (event.mode ?? 'Online');

  return (
    <main>
      {/* Landing hero: 16:9 media — a crossfading carousel of the submitted
          games' covers, falling back to the seeded event artwork — with the
          event as a product and dual CTAs on top. */}
      <section className="relative isolate flex aspect-[16/9] min-h-[22rem] flex-col overflow-hidden border-b border-border">
        <EventHeroMedia
          seed={eventId}
          images={games
            .map((game) => game.imageUrl)
            .filter((url): url is string => Boolean(url))}
        />
        <div className="relative mx-auto flex w-full max-w-7xl flex-1 flex-col justify-end px-4 pb-8 pt-6 sm:px-6 lg:px-8">
          <div className="flex flex-wrap items-center gap-2">
            <HeroChip>
              {statusLabel(event.status)} · {event.approvalMode === 'Committee' ? 'Committee review' : 'Manager review'}
            </HeroChip>
            <HeroChip>
              <MapPin className="size-3.5" aria-hidden="true" />
              {modeLabel}
            </HeroChip>
          </div>
          <h1 className="mt-4 max-w-4xl text-3xl font-bold leading-tight tracking-tight text-white drop-shadow-lg sm:text-5xl">
            {event.name}
          </h1>
          {event.description ? (
            <p className="mt-2 line-clamp-1 max-w-2xl text-sm leading-6 text-white/85">
              {event.description}
            </p>
          ) : null}
          <div className="mt-5 flex flex-wrap items-center gap-3">
            <a
              href="#join"
              className="inline-flex h-10 items-center justify-center gap-2 rounded-full bg-white px-5 text-sm font-semibold text-slate-950 transition hover:bg-white/90"
            >
              <Users className="size-4" aria-hidden="true" />
              Join the playtest
            </a>
            <a
              href="#apply"
              className="inline-flex h-10 items-center justify-center gap-2 rounded-full border border-white/30 px-5 text-sm font-semibold text-white transition hover:bg-white/10"
            >
              <Gamepad2 className="size-4" aria-hidden="true" />
              Submit a game
            </a>
          </div>
        </div>
      </section>

      
      {experience.accessIssues.length > 0 ? (
        <div className="mx-auto w-full max-w-7xl px-4 pt-6 sm:px-6 lg:px-8">
          <div className="rounded-2xl border border-destructive/30 bg-destructive/10 p-4 text-sm text-destructive">
            Some account-specific participation data could not be loaded. Public event details remain available.
          </div>
        </div>
      ) : null}

      {/* Participation body: no cards — plain sections separated by hairlines. */}
      <div className="mx-auto w-full max-w-3xl divide-y divide-border px-4 py-12 sm:px-6">
        <section id="join" aria-labelledby="event-schedules" className="scroll-mt-6 pb-8">
          <h2 id="event-schedules" className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Sessions</h2>
          <div className="mt-3 flex flex-wrap gap-x-6 gap-y-1 text-sm text-muted-foreground">
            <span className="inline-flex items-center gap-1.5">
              <ClipboardCheck className="size-3.5 shrink-0" aria-hidden="true" />
              Applications {formatDay(event.applicationsOpenAt)} → {formatDay(event.applicationsCloseAt)}
            </span>
            <span className="inline-flex items-center gap-1.5">
              <CalendarDays className="size-3.5 shrink-0" aria-hidden="true" />
              Event {formatDay(event.startsAt)} → {formatDay(event.endsAt)}
            </span>
          </div>
          {slots.length === 0 ? (
            <p className="mt-3 text-sm text-muted-foreground">
              Coming soon.
            </p>
          ) : (
            <div className="mt-4 space-y-4">
              {slots.map((slot: TestingLabPublicTestingEventSlotProjection) => (
                <TestingSlotRegistration
                  key={slot.id}
                  eventId={eventId}
                  isAuthenticated={experience.isAuthenticated}
                  slot={slot}
                  registration={experience.registrations.find((registration) => registration.slotId === slot.id)}
                  registrationSchema={event.configuration?.testerRegistrationSchema}
                  generalRules={event.configuration?.generalRules}
                  testerInstructions={event.configuration?.testerInstructions}
                />
              ))}
            </div>
          )}
        </section>

        <section id="apply" className="scroll-mt-6 py-8">
          <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Submit a game</h2>
          <div className="mt-4">
            <TestingProjectApplication
              eventId={eventId}
              isAuthenticated={experience.isAuthenticated}
              acceptsApplications={acceptsApplications}
              projectVersions={projectVersions}
              initialProjectId={projectId}
              applicationSchema={event.configuration?.projectApplicationSchema}
              generalRules={event.configuration?.generalRules}
              candidateInstructions={event.configuration?.candidateInstructions}
              requiresFeedback={event.requiresFeedback ?? false}
              applications={experience.applications.flatMap((application) => application.id ? [{
                id: application.id,
                projectId: application.projectId,
                projectVersionId: application.projectVersionId,
                preferredAvailability: application.preferredAvailability,
                status: application.status,
                decisionRationale: application.decisionRationale,
                brief: application.brief,
                eventApplicationResponse: application.eventApplicationResponse,
                feedbackQuestionnaire: application.feedbackQuestionnaire,
                rulesAcceptedAt: application.rulesAcceptedAt,
                submittedAssetReferenceIds: application.submittedAssetReferenceIds,
              }] : [])}
            />
          </div>
          <p className="mt-4 text-xs text-muted-foreground">
            Every game is reviewed by the {event.approvalMode === 'Committee' ? 'committee' : 'manager'} before it joins the lineup.
          </p>
        </section>

        {event.requiresFeedback || experience.feedbackObligations.length > 0 ? (
          <section aria-labelledby="required-feedback" className="pt-8">
            <h2 id="required-feedback" className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Feedback</h2>
            <div className="mt-4">
              <TestingFeedbackSubmission
                eventId={eventId}
                isAuthenticated={experience.isAuthenticated}
                obligations={experience.feedbackObligations}
              />
            </div>
          </section>
        ) : null}
      </div>
    </main>
  );
}
