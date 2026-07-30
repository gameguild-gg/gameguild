import { FloatingIcons } from "@/components/testing-lab/common/ui/floating-icons";
import { TestingLabHero } from "@/components/testing-lab/landing/testing-lab-hero";
import { TestingLabHowItWorks } from "@/components/testing-lab/landing/testing-lab-how-it-works";
import { TestingLabLearnMore } from "@/components/testing-lab/landing/testing-lab-learn-more";
import { TestingLabStats } from "@/components/testing-lab/landing/testing-lab-stats";
import { Link } from "@/i18n/navigation";
import { getPublicTestingEventsDirectory } from "@/lib/testing-lab/events-queries";
import { Badge } from "@game-guild/ui/components/badge";
import {
  ArrowRight,
  CalendarDays,
  ClipboardCheck,
  MapPin,
  UsersRound,
} from "lucide-react";
import type { ReactNode } from "react";

function formatDateTime(value?: string | null) {
  if (!value) return "Schedule pending";
  const date = new Date(value);
  if (Number.isNaN(date.valueOf())) return "Schedule pending";
  return new Intl.DateTimeFormat("en", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
}

function Metric({ icon, children }: { icon: ReactNode; children: ReactNode }) {
  return (
    <span className="inline-flex items-center gap-2 text-sm text-slate-300">
      {icon}
      {children}
    </span>
  );
}

export default async function TestingLabPage() {
  const directory = await getPublicTestingEventsDirectory({ take: 100 });
  const openEvents = directory.events.filter((event) =>
    /open|active|scheduled|published/i.test(String(event.status ?? "")),
  ).length;
  const upcomingEvents = directory.events.filter(
    (event) =>
      Boolean(event.startsAt) &&
      /open|scheduled|published/i.test(String(event.status ?? "")),
  ).length;
  const openTesterSeats = directory.events.reduce(
    (eventTotal, event) =>
      eventTotal +
      (event.slots ?? []).reduce(
        (slotTotal, slot) => slotTotal + (slot.availableTesterCount ?? 0),
        0,
      ),
    0,
  );

  return (
    <div className="relative flex min-h-screen flex-1 flex-col overflow-hidden bg-gradient-to-b from-slate-950 via-slate-900 to-slate-950 text-white">
      <FloatingIcons />
      <div className="relative z-10 flex flex-1 flex-col">
        <main className="mx-auto flex w-full max-w-7xl flex-1 flex-col px-4 sm:px-6 lg:px-8">
          <TestingLabHero />
          <TestingLabStats
            totalSessions={directory.events.length}
            openSessions={openEvents}
            upcomingSessions={upcomingEvents}
            openTesterSeats={openTesterSeats}
          />
          <TestingLabLearnMore />

          <section id="events" className="scroll-mt-24 py-16">
            <div className="mb-10 text-center">
              <div className="mb-5 flex justify-center">
                <Badge
                  variant="outline"
                  className="border-blue-400/30 bg-blue-500/5 text-blue-200"
                >
                  {directory.events.length}{" "}
                  {directory.events.length === 1
                    ? "public event"
                    : "public events"}
                </Badge>
              </div>
              <h2 className="text-3xl font-bold text-white sm:text-4xl">
                Test. Play. Improve.
              </h2>
              <p className="mx-auto mt-4 max-w-3xl text-base leading-7 text-slate-300">
                Choose a managed event, review its schedules, and participate as
                a tester or apply with a GameGuild project.
              </p>
            </div>

            {directory.accessIssues.length > 0 ? (
              <div className="mb-6 rounded-md border border-amber-400/30 bg-amber-400/5 p-4 text-sm text-amber-100">
                Live Testing Lab events are temporarily unavailable. Retry
                shortly.
              </div>
            ) : null}

            {directory.events.length === 0 ? (
              <div className="rounded-lg border border-dashed border-slate-700 bg-slate-900/40 p-12 text-center">
                <CalendarDays className="mx-auto size-8 text-slate-500" />
                <h3 className="mt-4 font-semibold text-white">
                  No public testing events
                </h3>
                <p className="mt-2 text-sm text-slate-400">
                  Events appear here when a manager opens applications or
                  publishes an approved testing schedule.
                </p>
              </div>
            ) : (
              <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
                {directory.events.map((event) => {
                  const slots = event.slots ?? [];
                  const testerAvailability = slots.reduce(
                    (total, slot) => total + (slot.availableTesterCount ?? 0),
                    0,
                  );
                  const locations = [
                    ...new Set(
                      slots.map((slot) => slot.campusName).filter(Boolean),
                    ),
                  ];
                  return (
                    <article
                      key={event.id}
                      className="flex h-full flex-col rounded-xl border border-slate-700 bg-gradient-to-br from-slate-900/80 to-slate-800/60 p-6 backdrop-blur-sm transition hover:border-blue-400/40 hover:shadow-lg hover:shadow-blue-950/20"
                    >
                      <div className="flex flex-wrap items-center justify-between gap-3">
                        <div className="flex flex-wrap gap-2">
                          <Badge className="bg-blue-500/10 text-blue-200">
                            {event.status}
                          </Badge>
                          <Badge
                            variant="outline"
                            className="border-slate-600 text-slate-300"
                          >
                            {event.mode === "InPerson"
                              ? "In person"
                              : event.mode}
                          </Badge>
                        </div>
                        <span className="text-xs text-slate-400">
                          {event.applicationCount ?? 0} applications
                        </span>
                      </div>
                      <h3 className="mt-5 text-xl font-semibold text-white">
                        {event.name}
                      </h3>
                      <p className="mt-2 line-clamp-3 text-sm leading-6 text-slate-400">
                        {event.description ??
                          "A managed GameGuild project testing event."}
                      </p>
                      <div className="my-5 grid gap-3 border-y border-slate-700 py-4 sm:grid-cols-2">
                        <Metric
                          icon={
                            <CalendarDays className="size-4 text-blue-300" />
                          }
                        >
                          {formatDateTime(event.startsAt)}
                        </Metric>
                        <Metric
                          icon={<UsersRound className="size-4 text-blue-300" />}
                        >
                          {testerAvailability} tester seats open
                        </Metric>
                        <Metric
                          icon={
                            <ClipboardCheck className="size-4 text-blue-300" />
                          }
                        >
                          {slots.length}{" "}
                          {slots.length === 1 ? "schedule" : "schedules"}
                        </Metric>
                        <Metric
                          icon={<MapPin className="size-4 text-blue-300" />}
                        >
                          {locations.length > 0
                            ? locations.join(", ")
                            : event.mode === "Online"
                              ? "Online"
                              : "Location pending"}
                        </Metric>
                      </div>
                      <Link
                        href={`/testing-lab/events/${event.id}`}
                        className="mt-auto inline-flex items-center font-semibold text-blue-200 transition hover:text-blue-100"
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
        </main>

        <aside id="learn-more" className="border-t border-slate-800">
          <TestingLabHowItWorks />
        </aside>
      </div>
    </div>
  );
}
