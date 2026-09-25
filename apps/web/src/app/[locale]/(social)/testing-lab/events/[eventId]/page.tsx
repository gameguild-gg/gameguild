import { TestingFeedbackSubmission } from '@/components/testing-lab/testing-feedback-submission';
import { TestingProjectApplication } from '@/components/testing-lab/testing-project-application';
import { TestingSlotRegistration } from '@/components/testing-lab/testing-slot-registration';
import { TestingEventGames } from '@/components/testing-lab/testing-event-games';
import { TestingEventDateRange } from '@/components/testing-lab/testing-event-date-range';
import { Link } from '@/i18n/navigation';
import { getPublicTestingEventExperience } from '@/lib/testing-lab/events-queries';
import { getTestingProjectVersionOptions } from '@/lib/testing-lab/queries';
import { getLocalizationPreference } from '@/lib/user-settings/queries';
import type {
  TestingLabPublicTestingEventSlotProjection,
} from '@game-guild/client';
import {
  ArrowDown,
  ArrowLeft,
  CalendarDays,
  Gamepad2,
  MessageSquare,
  Users,
} from 'lucide-react';
import { notFound } from 'next/navigation';

// The public event body can include account-specific participation data.
// Always request a fresh render so one visitor's private state cannot leak to another.
export const dynamic = 'force-dynamic';

function safeTimeZone(timeZone?: string | null) {
  if (!timeZone) return 'UTC';
  try {
    new Intl.DateTimeFormat('en-US', { timeZone });
    return timeZone;
  } catch {
    return 'UTC';
  }
}

function getTimestamp(value?: string | null) {
  if (!value) return null;
  const timestamp = new Date(value).valueOf();
  return Number.isFinite(timestamp) ? timestamp : null;
}

function eventStatusLabel({
  status,
  ended,
  beforeOpening,
  gameApplicationsOpen,
  testerRegistrationOpen,
}: {
  status?: string | null;
  ended: boolean;
  beforeOpening: boolean;
  gameApplicationsOpen: boolean;
  testerRegistrationOpen: boolean;
}) {
  if (ended) return 'Playtest ended';
  if (testerRegistrationOpen) return 'Tester registration open';
  if (gameApplicationsOpen) return 'Game submissions open';
  if (beforeOpening) return 'Game submissions open soon';
  if (status === 'Active') return 'Live now';
  if (status === 'Completed') return 'Completed';
  if (status === 'Cancelled') return 'Cancelled';
  if (status === 'ApplicationsClosed' || status === 'ApplicationsOpen') return 'Game submissions closed';
  if (status === 'Scheduled') return 'Scheduled';
  return status ?? 'Testing Lab event';
}

export default async function Page({
  params,
  searchParams,
}: {
  params: Promise<{ eventId: string }>;
  searchParams?: Promise<{ projectId?: string; submitGame?: string; applicationId?: string }>;
}) {
  const { eventId } = await params;
  const { projectId, submitGame, applicationId } = searchParams ? await searchParams : {};
  const experience = await getPublicTestingEventExperience(eventId);
  if (!experience.event && experience.accessIssues.length === 0) notFound();

  if (!experience.event) {
    const eventNotFound = experience.accessIssues.some((issue) =>
      /\b404\b|PublicEventNotFound/i.test(issue),
    );
    console.error(
      `[testing-lab] public event ${eventId} could not be loaded`,
      experience.accessIssues,
    );
    return (
      <main className="mx-auto w-full max-w-4xl px-4 py-16 sm:px-6">
        <div className="rounded-lg border border-destructive/30 bg-destructive/10 p-6">
          <h1 className="text-2xl font-semibold text-foreground">
            {eventNotFound ? 'Playtest not found' : 'Event temporarily unavailable'}
          </h1>
          <p className="mt-2 text-sm leading-6 text-muted-foreground">
            {eventNotFound
              ? 'We could not find a public playtest at this link. It may have ended or the URL may be incorrect.'
              : 'We could not load this playtest right now. Your place in the community is still here—try again shortly.'}
          </p>
          <Link href="/testing-lab" className="mt-5 inline-flex items-center gap-2 text-sm font-medium text-primary hover:text-primary/80">
            <ArrowLeft className="size-4" aria-hidden="true" /> Browse other playtests
          </Link>
        </div>
      </main>
    );
  }

  const event = experience.event;
  // This route is force-dynamic; registration eligibility must use request-time state.
  // eslint-disable-next-line react-hooks/purity
  const now = Date.now();
  const openingAt = getTimestamp(event.applicationsOpenAt);
  const closingAt = getTimestamp(event.applicationsCloseAt);
  const eventEndsAt = getTimestamp(event.endsAt);
  const eventEnded = event.status === 'Completed' || event.status === 'Cancelled' || (eventEndsAt !== null && eventEndsAt < now);
  const beforeOpening = openingAt !== null && openingAt > now;
  const withinApplicationWindow = (openingAt === null || openingAt <= now) && (closingAt === null || closingAt >= now);
  const acceptsApplications = !eventEnded && event.status === 'ApplicationsOpen' && withinApplicationWindow;
  const slots = event.slots ?? [];
  const testerRegistrationStatus = ['ApplicationsClosed', 'Scheduled', 'Active'].includes(event.status ?? '');
  const testerConfigurationReady = Boolean(
    event.configuration?.frozenAt && event.configuration.testerRegistrationSchema,
  );
  const canRegisterForSlot = (slot: TestingLabPublicTestingEventSlotProjection) => {
    const slotEnd = getTimestamp(slot.endsAt);
    return !eventEnded && testerRegistrationStatus && testerConfigurationReady && (slotEnd === null || slotEnd > now);
  };
  const testerRegistrationOpen = slots.some(canRegisterForSlot);
  const games = event.games ?? [];
  const availableCapacity = (field: 'availableProjectCount' | 'availableTesterCount') => {
    if (slots.length === 0 || slots.some((slot) => slot[field] == null)) return null;
    return slots.reduce((total, slot) => total + (slot[field] ?? 0), 0);
  };
  const projectSpotsLeft = availableCapacity('availableProjectCount');
  const testerSpotsLeft = availableCapacity('availableTesterCount');
  const projectCapacityLabel = acceptsApplications
    ? projectSpotsLeft === null
      ? 'No limit'
      : projectSpotsLeft === 0
        ? 'Full'
        : String(projectSpotsLeft) + ' left'
    : beforeOpening
      ? 'Opens soon'
      : 'Closed';
  const testerCapacityLabel = testerRegistrationOpen
    ? testerSpotsLeft === null
      ? 'No limit'
      : testerSpotsLeft === 0
        ? 'Full'
        : String(testerSpotsLeft) + ' left'
    : 'Closed';
  const showGameSubmission = acceptsApplications || submitGame === '1' || Boolean(projectId) || Boolean(applicationId);
  const modeLabel = event.mode === 'InPerson' ? 'In person' : (event.mode ?? 'Online');
  const statusLabel = eventStatusLabel({
    status: event.status,
    ended: eventEnded,
    beforeOpening,
    gameApplicationsOpen: acceptsApplications,
    testerRegistrationOpen,
  });
  const [projectVersions, localization] = await Promise.all([
    experience.isAuthenticated && showGameSubmission ? getTestingProjectVersionOptions() : Promise.resolve([]),
    experience.isAuthenticated ? getLocalizationPreference().catch(() => null) : Promise.resolve(null),
  ]);
  const applicationSummaries = experience.applications.map((application, index) => ({
    id: application.id ?? application.projectId ?? `application-${index}`,
    projectId: application.projectId,
    title: games.find((game) => game.projectId === application.projectId)?.title?.trim()
      || projectVersions.find((version) => version.projectId === application.projectId)?.projectTitle.trim()
      || `Game submission ${index + 1}`,
    status: application.status?.trim() || 'Under review',
  }));
  const timeZone = safeTimeZone(localization?.timezone ?? event.timeZoneId);
  const dateLocale = localization?.language ?? 'en-US';
  const hour12 = localization?.timeFormat !== '24h';

  return (
    <main className="grid w-full grid-cols-1 lg:grid-cols-[19rem_minmax(0,1fr)]">
      <aside
        id="event-info"
        aria-label="Playtest details"
        className="min-w-0 space-y-5 border-y border-border bg-muted/20 px-4 pb-5 pt-0 sm:px-6 lg:sticky lg:top-16 lg:col-start-1 lg:row-start-1 lg:h-[calc(100svh-4rem)] lg:self-start lg:overflow-y-auto lg:border-y-0 lg:border-r lg:px-5 lg:pb-5 lg:pt-0"
      >
        <div>
          <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Testing Lab</p>
          <header className="mt-2 space-y-3">
            <h1 id="event-title" className="text-xl font-semibold tracking-tight">
              {event.name}
            </h1>
            {event.description?.trim() ? (
              <p className="text-sm leading-6 text-muted-foreground">
                {event.description}
              </p>
            ) : null}
            <div className="flex flex-wrap items-center gap-2">
              <div className="inline-flex max-w-full overflow-hidden rounded-full border border-border bg-background text-xs font-medium">
                <span className="whitespace-nowrap px-2.5 py-1 text-muted-foreground">{modeLabel}</span>
                <span className="border-l border-border px-2.5 py-1">{statusLabel}</span>
              </div>
            </div>
          </header>
        </div>

        <section id="schedule" aria-labelledby="schedule-heading" className="scroll-mt-8 border-t border-border pt-4">
          <h2 id="schedule-heading" className="text-sm font-semibold">Tester - Sign up</h2>
          {slots.length === 0 && event.startsAt ? (
            <div className="mt-3 flex items-start gap-2">
              <CalendarDays className="mt-0.5 size-4 shrink-0 text-muted-foreground" aria-hidden="true" />
              <TestingEventDateRange
                startsAt={event.startsAt}
                endsAt={event.endsAt}
                timeZone={timeZone}
                locale={dateLocale}
                hour12={hour12}
              />
            </div>
          ) : null}
          {!testerRegistrationOpen ? (
            <p className="mt-2 text-sm leading-5 text-muted-foreground">
              This session is not accepting new registrations. Browse other playtests for an open seat.
            </p>
          ) : null}
          {slots.length > 0 ? (
            <div className="mt-3 space-y-2">
              {slots.map((slot: TestingLabPublicTestingEventSlotProjection) => (
                <TestingSlotRegistration
                  key={slot.id}
                  eventId={eventId}
                  isAuthenticated={experience.isAuthenticated}
                  registrationOpen={canRegisterForSlot(slot)}
                  showClosedMessage={false}
                  timeZoneId={timeZone}
                  locale={dateLocale}
                  hour12={hour12}
                  slot={slot}
                  registration={experience.registrations.find((registration) => registration.slotId === slot.id)}
                  registrationSchema={event.configuration?.testerRegistrationSchema}
                  generalRules={event.configuration?.generalRules}
                  testerInstructions={event.configuration?.testerInstructions}
                />
              ))}
            </div>
          ) : null}
        </section>

        <section className="border-t border-border pt-5" aria-labelledby="capacity-heading">
          <h2 id="capacity-heading" className="text-sm font-semibold">At a glance</h2>
          <dl className="mt-2 space-y-1 text-sm">
            <div className="flex items-center justify-between gap-3 py-1">
              <dt className="flex items-center gap-2 text-muted-foreground">
                <Gamepad2 className="size-4" aria-hidden="true" />
                Games / game spots
              </dt>
              <dd className="whitespace-nowrap text-right font-semibold tabular-nums">{games.length} / {projectCapacityLabel}</dd>
            </div>
            <div className="flex items-center justify-between gap-3 py-1">
              <dt className="flex items-center gap-2 text-muted-foreground">
                <Users className="size-4" aria-hidden="true" />
                Tester spots
              </dt>
              <dd className="whitespace-nowrap text-right font-semibold">{testerCapacityLabel}</dd>
            </div>
            {event.requiresFeedback ? (
              <div className="flex items-center justify-between gap-3 py-1">
                <dt className="flex items-center gap-2 text-muted-foreground">
                  <MessageSquare className="size-4" aria-hidden="true" />
                  Feedback
                </dt>
                <dd className="whitespace-nowrap text-right font-semibold">Required</dd>
              </div>
            ) : null}
          </dl>
        </section>

        <section className="border-t border-border pt-5">
          <h3 className="text-sm font-semibold">Rules for testers</h3>
          {event.configuration?.generalRules?.trim() || event.configuration?.testerInstructions?.trim() ? (
            <div className="mt-3 space-y-3 text-sm leading-6 text-muted-foreground">
              {event.configuration?.generalRules?.trim() ? (
                <p className="whitespace-pre-wrap">{event.configuration.generalRules}</p>
              ) : null}
              {event.configuration?.testerInstructions?.trim() ? (
                <p className="whitespace-pre-wrap">{event.configuration.testerInstructions}</p>
              ) : null}
            </div>
          ) : (
            <p className="mt-2 text-sm leading-6 text-muted-foreground">No tester rules have been posted yet.</p>
          )}
        </section>

        {experience.accessIssues.length > 0 ? (
          <p className="border-t border-border pt-4 text-sm text-destructive" role="status">
            Some account-specific participation details could not be loaded. The public event information is still available.
          </p>
        ) : null}

        {experience.feedbackObligations.length > 0 ? (
          <section className="border-t border-border pt-5" aria-labelledby="feedback-heading">
            <h2 id="feedback-heading" className="text-sm font-semibold">Your feedback</h2>
            <div className="mt-2">
              <TestingFeedbackSubmission
                eventId={eventId}
                isAuthenticated={experience.isAuthenticated}
                obligations={experience.feedbackObligations}
              />
            </div>
          </section>
        ) : null}

        <div className="grid gap-2 border-t border-border pt-5">
          {!testerRegistrationOpen ? (
            <>
              <button
                type="button"
                disabled
                className="inline-flex h-10 cursor-not-allowed items-center justify-center rounded-md bg-primary px-4 text-sm font-medium text-primary-foreground opacity-60"
              >
                Registration closed
              </button>
              <Link
                href="/testing-lab"
                className="inline-flex items-center gap-2 text-sm font-medium text-primary underline-offset-4 hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              >
                <ArrowLeft className="size-4" aria-hidden="true" />
                Browse other playtests
              </Link>
            </>
          ) : (
            <>
              <a
                href="#schedule"
                className="inline-flex h-10 items-center justify-center gap-2 rounded-md bg-primary px-4 text-sm font-medium text-primary-foreground hover:bg-primary/90 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              >
                Sign up to test
                <ArrowDown className="size-4" aria-hidden="true" />
              </a>
              <Link
                href="/testing-lab"
                className="inline-flex items-center gap-2 text-sm font-medium text-primary underline-offset-4 hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              >
                <ArrowLeft className="size-4" aria-hidden="true" />
                Browse other playtests
              </Link>
            </>
          )}
        </div>
      </aside>

      <TestingEventGames
        games={games}
        eventId={eventId}
        applications={applicationSummaries}
        emptyState={
          <div className="max-w-md">
            <p className="font-medium">
              {eventEnded ? 'No games were published for this playtest.' : 'No games have been announced yet.'}
            </p>
            <p className="mt-2 text-sm leading-6 text-muted-foreground">
              {acceptsApplications
                ? 'Bring a project to the lab and give testers something new to play.'
                : 'Check back when the playtest lineup is published.'}
            </p>
          </div>
        }
      >
        {showGameSubmission ? (
          <details
            id="submit-game"
            open={submitGame === '1' || Boolean(projectId) || Boolean(applicationId)}
            className="mt-6 border-t border-border pt-4"
          >
            <summary className="cursor-pointer text-sm font-medium">
              {acceptsApplications ? 'Submit a game' : 'Review this game submission'}
            </summary>
            <div className="pt-4">
              <TestingProjectApplication
                eventId={eventId}
                isAuthenticated={experience.isAuthenticated}
                acceptsApplications={acceptsApplications}
                projectVersions={projectVersions}
                initialProjectId={projectId}
                selectedApplicationId={applicationId}
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
          </details>
        ) : null}
      </TestingEventGames>
    </main>
  );
}
