import { Badge } from "@game-guild/ui/components/badge";
import { buttonVariants } from "@game-guild/ui/components/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@game-guild/ui/components/dropdown-menu";
import {
  HoverCard,
  HoverCardContent,
  HoverCardTrigger,
} from "@game-guild/ui/components/hover-card";
import { Link } from "@/i18n/navigation";
import {
  ArrowRight,
  Calendar,
  CalendarDays,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  Clock,
  Gamepad2,
  MapPin,
  Users,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { getISOWeek } from "date-fns";
import { type ReactNode, useState } from "react";
import { EventCoverArt } from "./event-cover-art";
import type {
  TestingEventStatus,
  TestingEventViewModel,
} from "./testing-events-presentation";
function formatDate(value?: string) {
  if (!value) return "Schedule pending";
  const date = new Date(value);
  if (Number.isNaN(date.valueOf())) return "Schedule pending";
  return new Intl.DateTimeFormat("en-US", {
    month: "short",
    day: "2-digit",
    year: "numeric",
    timeZone: "UTC",
  }).format(date);
}
function formatTime(value?: string) {
  if (!value) return "Time pending";
  const date = new Date(value);
  if (Number.isNaN(date.valueOf())) return "Time pending";
  const formatted = new Intl.DateTimeFormat("en-US", {
    hour: "numeric",
    minute: "2-digit",
    timeZone: "UTC",
  }).format(date);
  return `${formatted} UTC`;
}
function formatDuration(startsAt?: string, endsAt?: string) {
  if (!startsAt || !endsAt) return "TBD";
  const duration = new Date(endsAt).valueOf() - new Date(startsAt).valueOf();
  if (!Number.isFinite(duration) || duration <= 0) return "TBD";
  const minutes = Math.round(duration / 60_000);
  if (minutes < 60) return `${minutes} min`;
  const hours = Math.floor(minutes / 60);
  const remainder = minutes % 60;
  return remainder === 0 ? `${hours}h` : `${hours}h ${remainder}m`;
}

function capacityLabel(current: number, limit: number | null, noun: string) {
  if (limit == null) {
    return `${current} ${current === 1 ? noun.replace(/s$/, "") : noun}`;
  }
  return `${current}/${limit} ${noun}`;
}

function statusClasses(status: TestingEventStatus) {
  if (status === "open")
    return "border-success/30 bg-success/10 text-success";
  if (status === "in-progress")
    return "border-highlight/30 bg-highlight/10 text-highlight";
  if (status === "completed")
    return "border-border bg-muted text-muted-foreground";
  return "border-destructive/30 bg-destructive/10 text-destructive";
}

/** Status dot for the high-contrast cover chip; keeps color coding without
 * relying on translucent tints that wash out over the artwork. */
function statusDotClasses(status: TestingEventStatus) {
  if (status === "open") return "bg-success";
  if (status === "in-progress") return "bg-highlight";
  if (status === "completed") return "bg-muted-foreground";
  return "bg-destructive";
}

function eventHref(eventId: string, projectId?: string) {
  const path = `/testing-lab/events/${eventId}`;
  return projectId
    ? `${path}?projectId=${encodeURIComponent(projectId)}`
    : path;
}

function EventMeta({ session }: { session: TestingEventViewModel }) {
  return (
    <div className="grid grid-cols-2 gap-3 text-xs text-muted-foreground">
      <span className="flex items-center gap-1.5">
        <Users className="size-3.5" />
        {capacityLabel(session.testerCount, session.testerLimit, "testers")}
      </span>
      <span className="flex items-center gap-1.5">
        <Gamepad2 className="size-3.5" />
        {capacityLabel(session.projectCount, session.projectLimit, "projects")}
      </span>
      <span className="flex items-center gap-1.5">
        <Calendar className="size-3.5" />
        {formatDate(session.startsAt)}
      </span>
      <span className="flex items-center gap-1.5">
        <Clock className="size-3.5" />
        {formatTime(session.startsAt)}
      </span>
    </div>
  );
}

function MetaChip({
  icon: Icon,
  children,
}: {
  icon: LucideIcon;
  children: ReactNode;
}) {
  return (
    <span className="inline-flex items-center gap-1.5 text-xs text-muted-foreground">
      <Icon className="size-3.5 shrink-0" aria-hidden="true" />
      {children}
    </span>
  );
}

export function TestingEventCard({
  session,
  projectId,
}: {
  session: TestingEventViewModel;
  projectId?: string;
}) {
  const almostFull =
    session.availableTesterCount != null &&
    session.availableTesterCount > 0 &&
    session.availableTesterCount <= 2;
  const href = eventHref(session.id, projectId);
  return (
    <Link
      href={href}
      aria-label="View event"
      className="group relative isolate block h-full min-h-[24rem] overflow-hidden rounded-2xl border border-border bg-card text-card-foreground shadow-sm transition-all duration-300 hover:-translate-y-1 hover:border-primary/40 hover:shadow-xl hover:shadow-primary/10 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/50"
    >
      {/* Cover art fills the upper portion edge-to-edge (the card clips the
          top corners); on hover only the bottom edge expands to full-bleed. */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute bottom-[calc(100%-264px)] left-0 right-0 top-0 z-0 overflow-hidden transition-all duration-500 ease-out group-hover:bottom-0"
      >
        <EventCoverArt seed={session.id} />
        {/* Mode and status share one segmented pill on the artwork. */}
        <div className="absolute right-3 top-3 flex overflow-hidden rounded-full border border-white/20 bg-black/55 text-xs font-medium text-white backdrop-blur-sm">
          <span className="px-2.5 py-1">{session.mode}</span>
          <span className="border-l border-white/20 px-2.5 py-1">
            {session.statusLabel}
          </span>
        </div>
      </div>
      {/* Title, location, and description ride on the cover art in both
          states; a fixed offset keeps them over the artwork after the cover
          expands to full-bleed on hover. */}
      <div className="pointer-events-none absolute inset-x-4 top-[9.75rem] z-[2]">
        <h3 className="text-lg font-semibold leading-snug text-white drop-shadow-lg">
          {session.title}
        </h3>
        {session.location !== "Online" ? (
          <p className="mt-1 flex items-center gap-1.5 text-xs font-medium text-white/85 drop-shadow-md">
            <MapPin className="size-3.5 shrink-0" aria-hidden="true" />
            {session.location}
          </p>
        ) : null}
        <p className="mt-2 line-clamp-2 text-sm leading-6 text-white/80 drop-shadow-md">
          {session.description}
        </p>
      </div>
      {/* Card-colored scrim keeps overlaid text readable once the art fills the card. */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-0 z-[1] bg-gradient-to-t from-card via-card/90 to-card/5 opacity-0 transition-opacity duration-500 group-hover:opacity-100"
      />
      <div className="relative z-[2] flex h-full flex-col gap-3 p-4 pt-[17.25rem]">
        {/* Two columns: schedule on the left, capacity on the right. */}
        <div className="grid grid-cols-2 gap-1.5">
          <MetaChip icon={Calendar}>{formatDate(session.startsAt)}</MetaChip>
          <MetaChip icon={Users}>
            {capacityLabel(session.testerCount, session.testerLimit, "testers")}
          </MetaChip>
          <MetaChip icon={Clock}>{formatTime(session.startsAt)}</MetaChip>
          <MetaChip icon={Gamepad2}>
            {capacityLabel(
              session.projectCount,
              session.projectLimit,
              "projects",
            )}
          </MetaChip>
        </div>
        {almostFull && session.status === "open" ? (
          <p className="rounded-md border border-destructive/40 bg-destructive/10 px-3 py-2 text-xs font-medium text-destructive">
            Only {session.availableTesterCount} tester{" "}
            {session.availableTesterCount === 1 ? "seat" : "seats"} left
          </p>
        ) : null}
        {/* Footer anchors the bottom: capacity hint on the left, arrow
            affordance that fills on hover on the right. */}
        <div className="mt-auto flex items-center justify-between gap-3 border-t border-border/60 pt-3">
          <span className="text-xs font-medium text-muted-foreground">
            {session.availableTesterCount != null && session.availableTesterCount > 0
              ? `${session.availableTesterCount} ${
                  session.availableTesterCount === 1 ? "spot" : "spots"
                } left`
              : session.status === "completed"
                ? "Session completed"
                : session.status === "closed"
                  ? "Registration closed"
                  : session.testerLimit == null
                    ? "No seat limit"
                    : "Registration open"}
          </span>
          <span className="flex size-8 items-center justify-center rounded-full border border-border text-muted-foreground transition group-hover:border-primary group-hover:bg-primary group-hover:text-primary-foreground">
            <ArrowRight className="size-4" aria-hidden="true" />
          </span>
        </div>
      </div>
    </Link>
  );
}

export function TestingEventRow({
  session,
  projectId,
}: {
  session: TestingEventViewModel;
  projectId?: string;
}) {
  return (
    <article className="grid gap-5 rounded-lg border border-border bg-card p-5 transition hover:border-primary/40 lg:grid-cols-[auto_minmax(0,1fr)_18rem_10rem] lg:items-center">
      {/* 2:1 cover thumbnail leading the row, same seeded artwork family. */}
      <div className="relative h-20 w-40 shrink-0 overflow-hidden rounded-lg lg:h-24 lg:w-48">
        <EventCoverArt seed={session.id} />
      </div>
      <div className="min-w-0">
        <div className="mb-2 flex flex-wrap items-center gap-2">
          <h2 className="text-lg font-bold">{session.title}</h2>
          {/* Same joined mode+status chip as the directory cards. */}
          <div className="flex overflow-hidden rounded-full border border-border text-xs font-medium">
            <span className="bg-muted/50 px-2.5 py-0.5 text-muted-foreground">
              {session.mode}
            </span>
            <span className="border-l border-border px-2.5 py-0.5 text-foreground">
              {session.statusLabel}
            </span>
          </div>
        </div>
        <p className="line-clamp-2 text-sm leading-6 text-muted-foreground">
          {session.description}
        </p>
        <p className="mt-3 flex items-center gap-2 text-xs text-muted-foreground">
          <MapPin className="size-3.5" />
          {session.location}
        </p>
      </div>
      <EventMeta session={session} />
      <Link
        href={eventHref(session.id, projectId)}
        className={buttonVariants({ size: "sm", className: "w-full" })}
      >
        View event
      </Link>
    </article>
  );
}

export function TestingEventsTable({
  sessions,
  projectId,
}: {
  sessions: TestingEventViewModel[];
  projectId?: string;
}) {
  return (
    <div className="overflow-x-auto rounded-lg border border-border bg-card">
      <table className="w-full min-w-[980px] text-sm">
        <thead className="border-b border-border bg-muted text-left text-muted-foreground">
          <tr>
            <th className="p-4 font-medium">Session</th>
            <th className="p-4 font-medium">Location</th>
            <th className="p-4 font-medium">Date & Time</th>
            <th className="p-4 font-medium">Duration</th>
            <th className="p-4 font-medium">Capacity</th>
            <th className="p-4 font-medium">Status</th>
            <th className="p-4 font-medium">Action</th>
          </tr>
        </thead>
        <tbody>
          {sessions.map((session) => (
            <tr
              key={session.id}
              className="border-b border-border last:border-0 hover:bg-muted/50"
            >
              <td className="p-4">
                <p className="font-medium">{session.title}</p>
                <p className="mt-1 max-w-xs truncate text-xs text-muted-foreground">
                  {session.description}
                </p>
              </td>
              <td className="p-4">{session.location}</td>
              <td className="p-4">
                <p>{formatDate(session.startsAt)}</p>
                <p className="text-xs text-muted-foreground">
                  {formatTime(session.startsAt)}
                </p>
              </td>
              <td className="p-4">
                {formatDuration(session.startsAt, session.endsAt)}
              </td>
              <td className="p-4">
                <p>
                  {capacityLabel(
                    session.testerCount,
                    session.testerLimit,
                    "testers",
                  )}
                </p>
                <p className="text-xs text-muted-foreground">
                  {capacityLabel(
                    session.projectCount,
                    session.projectLimit,
                    "projects",
                  )}
                </p>
              </td>
              <td className="p-4">
                <Badge
                  variant="outline"
                  className={statusClasses(session.status)}
                >
                  {session.statusLabel}
                </Badge>
              </td>
              <td className="p-4">
                <Link
                  href={eventHref(session.id, projectId)}
                  className={buttonVariants({ size: "sm", variant: "outline" })}
                >
                  View event
                </Link>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

const WEEKDAY_LABELS = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

function utcDayKey(date: Date) {
  return `${date.getUTCFullYear()}-${date.getUTCMonth()}-${date.getUTCDate()}`;
}

/** Month-grid calendar over the filtered sessions; dates are UTC to match the
 *  rest of the directory. Rows are ISO weeks with the week number on the left;
 *  each event pill links to the event page. */
export function TestingEventsCalendar({
  sessions,
  projectId,
}: {
  sessions: TestingEventViewModel[];
  projectId?: string;
}) {
  const scheduled = sessions.filter(
    (session) => session.startsAt && !Number.isNaN(new Date(session.startsAt).valueOf()),
  );
  const [anchor, setAnchor] = useState<Date>(() =>
    scheduled.length > 0 ? new Date(scheduled[0]!.startsAt!) : new Date(),
  );

  const eventsByDay = new Map<string, TestingEventViewModel[]>();
  for (const session of scheduled) {
    const key = utcDayKey(new Date(session.startsAt!));
    const bucket = eventsByDay.get(key) ?? [];
    bucket.push(session);
    eventsByDay.set(key, bucket);
  }

  const anchorYear = anchor.getUTCFullYear();
  const anchorMonth = anchor.getUTCMonth();
  const monthLabel = new Intl.DateTimeFormat("en-US", {
    month: "long",
    year: "numeric",
    timeZone: "UTC",
  }).format(anchor);
  const firstWeekday = new Date(Date.UTC(anchorYear, anchorMonth, 1)).getUTCDay();
  const daysInMonth = new Date(Date.UTC(anchorYear, anchorMonth + 1, 0)).getUTCDate();
  const todayKey = utcDayKey(new Date());
  const monthEventCount = scheduled.filter((session) => {
    const date = new Date(session.startsAt!);
    return date.getUTCFullYear() === anchorYear && date.getUTCMonth() === anchorMonth;
  }).length;

  const shiftMonth = (delta: number) =>
    setAnchor(new Date(Date.UTC(anchorYear, anchorMonth + delta, 1)));

  // Fill the grid with real dates: previous-month tail, this month, and the
  // next-month head, so the rectangle is complete and adjacent events bleed in.
  const leading = Array.from(
    { length: firstWeekday },
    (_, index) =>
      new Date(Date.UTC(anchorYear, anchorMonth, index - firstWeekday + 1)),
  );
  const monthDays = Array.from(
    { length: daysInMonth },
    (_, index) => new Date(Date.UTC(anchorYear, anchorMonth, index + 1)),
  );
  const trailingCount =
    (7 - ((leading.length + monthDays.length) % 7)) % 7;
  const trailing = Array.from(
    { length: trailingCount },
    (_, index) => new Date(Date.UTC(anchorYear, anchorMonth + 1, index + 1)),
  );
  const allDays = [...leading, ...monthDays, ...trailing];
  const weeks: Array<Array<Date>> = [];
  for (let index = 0; index < allDays.length; index += 7) {
    weeks.push(allDays.slice(index, index + 7));
  }

  const monthOptions = Array.from({ length: 25 }, (_, index) => {
    const base = new Date();
    return new Date(
      Date.UTC(base.getUTCFullYear(), base.getUTCMonth() - 12 + index, 1),
    );
  });
  const shortMonthLabel = new Intl.DateTimeFormat("en-US", {
    month: "short",
    year: "numeric",
    timeZone: "UTC",
  }).format(anchor);

  return (
    <div>
      <div className="mb-5 flex flex-wrap items-end justify-between gap-3">
        <div>
          <h3 className="text-2xl font-semibold tracking-tight text-foreground">
            {monthLabel}
          </h3>
          <p className="mt-1 text-xs text-muted-foreground">
            {monthEventCount} {monthEventCount === 1 ? "event" : "events"} this month
          </p>
        </div>
        <div className="flex items-center gap-1">
          <DropdownMenu>
            <DropdownMenuTrigger
              render={
                <button
                  type="button"
                  aria-label="Select month"
                  className="flex h-8 items-center gap-1.5 rounded-full px-3 text-xs font-medium text-muted-foreground transition hover:bg-muted hover:text-foreground"
                />
              }
            >
              {shortMonthLabel}
              <ChevronDown className="size-3.5" aria-hidden="true" />
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="max-h-72 overflow-y-auto">
              {monthOptions.map((option) => {
                const isActive =
                  option.getUTCFullYear() === anchorYear &&
                  option.getUTCMonth() === anchorMonth;
                return (
                  <DropdownMenuItem
                    key={option.valueOf()}
                    onClick={() => setAnchor(option)}
                    className={isActive ? "font-medium text-primary" : ""}
                  >
                    {new Intl.DateTimeFormat("en-US", {
                      month: "short",
                      year: "numeric",
                      timeZone: "UTC",
                    }).format(option)}
                  </DropdownMenuItem>
                );
              })}
            </DropdownMenuContent>
          </DropdownMenu>
          <span aria-hidden="true" className="mx-1 h-4 w-px bg-border" />
          <button
            type="button"
            aria-label="Previous month"
            onClick={() => shiftMonth(-1)}
            className="flex size-8 items-center justify-center rounded-full text-muted-foreground transition hover:bg-muted hover:text-foreground"
          >
            <ChevronLeft className="size-4" aria-hidden="true" />
          </button>
          <button
            type="button"
            aria-label="Next month"
            onClick={() => shiftMonth(1)}
            className="flex size-8 items-center justify-center rounded-full text-muted-foreground transition hover:bg-muted hover:text-foreground"
          >
            <ChevronRight className="size-4" aria-hidden="true" />
          </button>
        </div>
      </div>
      {/* Borderless grid with real adjacent-month days as quiet ghosts; day
          numbers lead from the left, events stack underneath, hover lifts a
          day into a soft surface. */}
      <div role="grid" aria-label={`${monthLabel} event calendar`}>
        <div className="mb-2 grid grid-cols-[2rem_repeat(7,minmax(0,1fr))]">
          <div />
          {WEEKDAY_LABELS.map((label, index) => (
            <div
              key={label}
              className={`px-2 text-[11px] font-medium uppercase tracking-widest ${
                index === 0 || index === 6
                  ? "text-muted-foreground/50"
                  : "text-muted-foreground/70"
              }`}
            >
              {label}
            </div>
          ))}
        </div>
        <div className="space-y-1">
          {weeks.map((week, weekIndex) => {
            const weekNumber = getISOWeek(week[0] ?? anchor);
            return (
              <div
                key={`week-${weekIndex}`}
                role="row"
                className="grid grid-cols-[2rem_repeat(7,minmax(0,1fr))]"
              >
                <div className="pt-2 text-center text-[10px] font-medium text-muted-foreground/35">
                  {weekNumber}
                </div>
                {week.map((day, dayIndex) => {
                  const key = utcDayKey(day);
                  const dayEvents = eventsByDay.get(key) ?? [];
                  const isToday = key === todayKey;
                  const isOutside =
                    day.getUTCMonth() !== anchorMonth ||
                    day.getUTCFullYear() !== anchorYear;
                  const isWeekend = dayIndex === 0 || dayIndex === 6;
                  const visibleEvents = dayEvents.slice(0, 2);
                  const hiddenCount = dayEvents.length - visibleEvents.length;
                  return (
                    <div
                      key={key}
                      role={isOutside ? undefined : "gridcell"}
                      aria-label={
                        isOutside
                          ? undefined
                          : new Intl.DateTimeFormat("en-US", {
                              month: "short",
                              day: "numeric",
                              timeZone: "UTC",
                            }).format(day)
                      }
                      className={`group flex min-h-32 flex-col rounded-lg p-2 ${
                        isOutside ? "" : "transition hover:bg-muted/50"
                      }`}
                    >
                      <div className="mb-1.5">
                        {isToday ? (
                          <span className="flex size-6 items-center justify-center rounded-full bg-primary text-xs font-semibold text-primary-foreground">
                            {day.getUTCDate()}
                          </span>
                        ) : (
                          <span
                            className={`text-xs tabular-nums ${
                              isOutside
                                ? "text-muted-foreground/25"
                                : isWeekend
                                  ? "text-muted-foreground/50"
                                  : "text-muted-foreground"
                            }`}
                          >
                            {day.getUTCDate()}
                          </span>
                        )}
                      </div>
                      <div className={`space-y-0.5 ${isOutside ? "opacity-40" : ""}`}>
                        {visibleEvents.map((session) => (
                          <HoverCard key={session.id} openDelay={200}>
                            <HoverCardTrigger
                              render={
                                <Link
                                  href={eventHref(session.id, projectId)}
                                  title={session.title}
                                  className="flex items-center gap-1.5 rounded-md px-1.5 py-1 text-[11px] font-medium leading-tight text-foreground/80 transition hover:bg-primary/10 hover:text-primary"
                                />
                              }
                            >
                              <span
                                aria-hidden="true"
                                className={`size-1.5 shrink-0 rounded-full ${statusDotClasses(session.status)}`}
                              />
                              <span className="truncate">{session.title}</span>
                            </HoverCardTrigger>
                            <HoverCardContent side="top" className="w-72">
                              <p className="text-sm font-semibold text-foreground">
                                {session.title}
                              </p>
                              <p className="mt-1 line-clamp-3 text-xs leading-5 text-muted-foreground">
                                {session.description}
                              </p>
                              <div className="mt-3 space-y-1.5 text-xs text-muted-foreground">
                                <p className="flex items-center gap-1.5">
                                  <Calendar className="size-3.5 shrink-0" aria-hidden="true" />
                                  {formatDate(session.startsAt)} · {formatTime(session.startsAt)}
                                </p>
                                <p className="flex items-center gap-1.5">
                                  <MapPin className="size-3.5 shrink-0" aria-hidden="true" />
                                  {session.location}
                                </p>
                                <p className="flex items-center gap-1.5">
                                  <Users className="size-3.5 shrink-0" aria-hidden="true" />
                                  {capacityLabel(session.testerCount, session.testerLimit, "testers")}
                                </p>
                              </div>
                              <p className="mt-3 text-xs font-medium text-primary">
                                View event →
                              </p>
                            </HoverCardContent>
                          </HoverCard>
                        ))}
                        {hiddenCount > 0 ? (
                          <p className="px-1.5 text-[10px] font-medium text-muted-foreground/60">
                            +{hiddenCount} more
                          </p>
                        ) : null}
                      </div>
                    </div>
                  );
                })}
              </div>
            );
          })}
        </div>
      </div>
      {sessions.length > scheduled.length ? (
        <p className="mt-4 text-xs text-muted-foreground">
          {sessions.length - scheduled.length} unscheduled{" "}
          {sessions.length - scheduled.length === 1 ? "event is" : "events are"} not
          shown on the calendar.
        </p>
      ) : null}
    </div>
  );
}
