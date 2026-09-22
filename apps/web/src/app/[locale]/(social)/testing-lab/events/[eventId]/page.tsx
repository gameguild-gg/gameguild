import { TestingFeedbackSubmission } from '@/components/testing-lab/testing-feedback-submission';
import { TestingProjectApplication } from '@/components/testing-lab/testing-project-application';
import { TestingSlotRegistration } from '@/components/testing-lab/testing-slot-registration';
import { EventCoverArt } from '@/components/testing-lab/landing/event-cover-art';
import { Link } from '@/i18n/navigation';
import {
  getApprovedTestingEventGames,
  getPublicTestingEventExperience,
} from '@/lib/testing-lab/events-queries';
import { getTestingProjectVersionOptions } from '@/lib/testing-lab/queries';
import type { TestingLabPublicTestingEventSlotProjection } from '@game-guild/client';
import {
  ArrowLeft,
  CalendarDays,
  ClipboardCheck,
  FlaskConical,
  Gamepad2,
  MapPin,
  MessageSquareText,
} from 'lucide-react';
import type { ReactNode } from 'react';
import Image from 'next/image';
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

function statusDot(status?: string | null) {
  if (status === 'ApplicationsOpen') return 'bg-success';
  if (status === 'Active') return 'bg-highlight';
  if (status === 'Completed') return 'bg-muted-foreground';
  return 'bg-destructive';
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
      {/* Hero: seeded artwork with the event identity, matching the directory and feed cards. */}
      <section className="relative isolate overflow-hidden border-b border-border">
        <div className="absolute inset-0" aria-hidden="true">
          <EventCoverArt seed={eventId} />
        </div>
        <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/35 to-black/25" aria-hidden="true" />
        <div className="relative mx-auto w-full max-w-7xl px-4 pb-10 pt-8 sm:px-6 lg:px-8">
          <Link
            href="/testing-lab"
            className="inline-flex items-center text-sm font-medium text-white/80 transition hover:text-white"
          >
            <ArrowLeft className="mr-2 size-4" aria-hidden="true" />
            Testing Lab events
          </Link>
          <div className="mt-8 flex flex-wrap items-center gap-2">
            <HeroChip>
              <span aria-hidden="true" className={`size-1.5 rounded-full ${statusDot(event.status)}`} />
              {statusLabel(event.status)}
            </HeroChip>
            <HeroChip>
              <MapPin className="size-3.5" aria-hidden="true" />
              {modeLabel}
            </HeroChip>
            <HeroChip>
              <ClipboardCheck className="size-3.5" aria-hidden="true" />
              {event.approvalMode === 'Committee' ? 'Committee review' : 'Manager decision'}
            </HeroChip>
          </div>
          <h1 className="mt-5 max-w-4xl text-4xl font-bold leading-tight text-white drop-shadow-lg sm:text-5xl">
            {event.name}
          </h1>
          <p className="mt-2 text-xs font-semibold uppercase tracking-wider text-white/85 drop-shadow-md">
            Playtest · creators &amp; testers
          </p>
          {event.description ? (
            <p className="mt-4 line-clamp-2 max-w-2xl text-sm leading-6 text-white/85">
              {event.description}
            </p>
          ) : null}
          <div className="mt-8 flex flex-wrap gap-x-8 gap-y-2 text-sm text-white/90">
            <span className="inline-flex items-center gap-2">
              <ClipboardCheck className="size-4 shrink-0" aria-hidden="true" />
              Applications {formatDay(event.applicationsOpenAt)} → {formatDateTime(event.applicationsCloseAt)}
            </span>
            <span className="inline-flex items-center gap-2">
              <CalendarDays className="size-4 shrink-0" aria-hidden="true" />
              Event {formatDay(event.startsAt)} → {formatDateTime(event.endsAt)}
            </span>
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

      {/* The games are the product: full-width showcase right under the hero. */}
      <section aria-labelledby="featured-games" className="mx-auto w-full max-w-7xl px-4 pt-12 sm:px-6 lg:px-8">
        <div className="mb-5 flex flex-wrap items-end justify-between gap-3">
          <div>
            <h2 id="featured-games" className="text-2xl font-semibold text-foreground">
              Featured games
            </h2>
            <p className="mt-1 text-sm text-muted-foreground">
              {games.length > 0
                ? `${games.length} approved ${games.length === 1 ? 'project' : 'projects'} testing in this session.`
                : 'The lineup fills as candidate projects pass review — bring yours.'}
            </p>
          </div>
        </div>
        {games.length > 0 ? (
          <div className="-mx-1 flex snap-x snap-mandatory gap-4 overflow-x-auto px-1 pb-2 [scrollbar-width:none] [&::-webkit-scrollbar]:hidden">
            {games.map((game) => (
              <article
                key={game.applicationId}
                className="group w-80 shrink-0 snap-start overflow-hidden rounded-2xl border border-border bg-card transition hover:-translate-y-1 hover:border-primary/40 hover:shadow-xl hover:shadow-primary/10"
              >
                <div className="relative aspect-[4/3] w-full bg-muted">
                  {game.imageUrl ? (
                    <Image
                      src={game.imageUrl}
                      alt={game.name}
                      fill
                      unoptimized
                      className="object-cover"
                      sizes="320px"
                    />
                  ) : (
                    <div className="flex h-full w-full items-center justify-center">
                      <Gamepad2 className="size-12 text-muted-foreground/40" aria-hidden="true" />
                    </div>
                  )}
                  <div className="absolute inset-x-0 bottom-0 bg-gradient-to-t from-black/70 to-transparent px-4 pb-3 pt-10">
                    <p className="truncate text-base font-semibold text-white drop-shadow-md">{game.name}</p>
                  </div>
                </div>
                <div className="flex items-center justify-between gap-3 p-4">
                  <p className="min-w-0 line-clamp-2 text-xs leading-5 text-muted-foreground">
                    {game.shortDescription ?? 'Testing in this session.'}
                  </p>
                  {game.slug ? (
                    <Link
                      href={`/projects/${game.slug}`}
                      className="shrink-0 text-xs font-semibold text-primary transition hover:text-primary/80"
                    >
                      View
                    </Link>
                  ) : null}
                </div>
              </article>
            ))}
          </div>
        ) : slots.length === 0 ? (
          <p className="rounded-2xl border border-dashed border-border p-8 text-sm text-muted-foreground">
            The event schedule has not been published yet.
          </p>
        ) : (
          <div className="-mx-1 flex snap-x snap-mandatory gap-3 overflow-x-auto px-1 pb-2 [scrollbar-width:none] [&::-webkit-scrollbar]:hidden">
            {slots.map((slot: TestingLabPublicTestingEventSlotProjection, index: number) => {
              const slotApproved = slot.approvedProjectCount ?? 0;
              const slotMax = slot.maxProjects ?? null;
              const fill = slotMax ? Math.min(100, Math.round((slotApproved / slotMax) * 100)) : slotApproved > 0 ? 100 : 0;
              const venue = [slot.campusName, slot.roomName].filter(Boolean).join(' - ') || `${modeLabel} schedule`;
              return (
                <article key={slot.id ?? index} className="w-[18rem] shrink-0 snap-start rounded-2xl border border-border bg-card p-4">
                  <div className="flex items-start justify-between gap-3">
                    <div className="min-w-0">
                      <p className="truncate text-sm font-semibold text-foreground">{venue}</p>
                      <p className="mt-0.5 text-xs text-muted-foreground">
                        {formatDay(slot.startsAt)} · {formatDateTime(slot.startsAt)}
                      </p>
                    </div>
                    <span className="shrink-0 rounded-full bg-muted/60 px-2.5 py-1 text-xs font-medium text-muted-foreground">
                      {slotApproved}/{slotMax ?? 'Unlimited'}
                    </span>
                  </div>
                  <div className="mt-3 h-2 overflow-hidden rounded-full bg-primary/20" aria-hidden="true">
                    <div className="h-full rounded-full bg-primary transition-all" style={{ width: `${fill}%` }} />
                  </div>
                  <p className="mt-2 text-xs text-muted-foreground">
                    {slot.availableProjectCount == null
                      ? 'Open lineup — projects approved as they pass review'
                      : slot.availableProjectCount > 0
                        ? `${slot.availableProjectCount} project ${slot.availableProjectCount === 1 ? 'slot' : 'slots'} open`
                        : 'Project slots full'}
                  </p>
                </article>
              );
            })}
          </div>
        )}
      </section>

      <div className="mx-auto grid w-full max-w-7xl gap-10 px-4 py-12 sm:px-6 lg:grid-cols-[minmax(0,1fr)_minmax(340px,0.45fr)] lg:px-8">
        <div className="space-y-10">
          <section aria-labelledby="event-schedules">
            <div className="mb-4 flex items-center gap-3">
              <CalendarDays className="size-5 text-primary" aria-hidden="true" />
              <div>
                <h2 id="event-schedules" className="text-2xl font-semibold text-foreground">Schedules and tester capacity</h2>
                <p className="text-sm text-muted-foreground">Each schedule has independent tester and approved-project limits.</p>
              </div>
            </div>
            {slots.length === 0 ? (
              <p className="rounded-2xl border border-dashed border-border p-8 text-sm text-muted-foreground">
                The event schedule has not been published yet.
              </p>
            ) : (
              <div className="space-y-4">
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

          {event.requiresFeedback || experience.feedbackObligations.length > 0 ? (
            <section aria-labelledby="required-feedback">
              <div className="mb-4 flex items-center gap-3">
                <MessageSquareText className="size-5 text-primary" aria-hidden="true" />
                <div>
                  <h2 id="required-feedback" className="text-2xl font-semibold text-foreground">Required feedback</h2>
                  <p className="text-sm text-muted-foreground">
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

        <aside className="space-y-6 lg:sticky lg:top-20 lg:h-fit">
          <section className="rounded-2xl border border-border bg-card p-5">
            <div className="flex items-center gap-3">
              <ClipboardCheck className="size-5 text-primary" aria-hidden="true" />
              <div>
                <h2 className="font-semibold text-foreground">Apply with a project</h2>
                <p className="text-sm text-muted-foreground">Candidate projects are reviewed before consuming capacity.</p>
              </div>
            </div>
            <div className="mt-5">
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
          </section>

          <section className="rounded-2xl border border-border bg-card p-5">
            <FlaskConical className="size-5 text-primary" aria-hidden="true" />
            <h2 className="mt-3 font-semibold text-foreground">How selection works</h2>
            <p className="mt-3 text-sm leading-6 text-muted-foreground">
              Submit a project, the {event.approvalMode === 'Committee' ? 'committee' : 'manager'} reviews it, and approval
              reserves a schedule slot. Tester registration and feedback run separately.
            </p>
          </section>
        </aside>
      </div>
    </main>
  );
}
