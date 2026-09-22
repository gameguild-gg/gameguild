import { Badge } from "@game-guild/ui/components/badge";
import { buttonVariants } from "@game-guild/ui/components/button";
import { Link } from "@/i18n/navigation";
import { Calendar, Clock, Gamepad2, MapPin, Users } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import type { ReactNode } from "react";
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
  return `${current}/${limit == null ? "Unlimited" : limit} ${noun}`;
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
      className="group relative isolate block h-full min-h-[26rem] overflow-hidden rounded-2xl border border-border bg-card text-card-foreground shadow-sm transition-all duration-300 hover:-translate-y-1 hover:border-primary/40 hover:shadow-xl hover:shadow-primary/10 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/50"
    >
      {/* Cover art: compact inset artwork that expands to a full-bleed hero on hover. */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute bottom-[calc(100%-240px)] left-3 right-3 top-3 z-0 overflow-hidden rounded-xl shadow-sm transition-all duration-500 ease-out group-hover:bottom-0 group-hover:left-0 group-hover:right-0 group-hover:top-0 group-hover:rounded-none"
      >
        <EventCoverArt seed={session.id} />
        {/* Mode and status share one segmented pill on the artwork. */}
        <div className="absolute left-3 top-3 flex overflow-hidden rounded-full border border-white/20 bg-black/55 text-xs font-medium text-white backdrop-blur-sm">
          <span className="px-2.5 py-1">{session.mode}</span>
          <span className="flex items-center gap-1.5 border-l border-white/20 px-2.5 py-1">
            <span
              aria-hidden="true"
              className={`size-1.5 rounded-full ${statusDotClasses(session.status)}`}
            />
            {session.statusLabel}
          </span>
        </div>
      </div>
      {/* Title rides on the cover art in both states; fixed offset keeps it over
          the artwork after the cover expands to full-bleed on hover. */}
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
      </div>
      {/* Card-colored scrim keeps overlaid text readable once the art fills the card. */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-0 z-[1] bg-gradient-to-t from-card via-card/90 to-card/5 opacity-0 transition-opacity duration-500 group-hover:opacity-100"
      />
      <div className="relative z-[2] flex flex-col gap-3 p-4 pt-[15.75rem]">
        <p className="line-clamp-2 text-sm leading-6 text-muted-foreground">
          {session.description}
        </p>
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
    <article className="grid gap-5 rounded-lg border border-border bg-card p-5 transition hover:border-primary/40 lg:grid-cols-[minmax(0,1fr)_18rem_10rem] lg:items-center">
      <div className="min-w-0">
        <div className="mb-2 flex flex-wrap items-center gap-2">
          <h2 className="text-lg font-bold">{session.title}</h2>
          <Badge variant="secondary">{session.mode}</Badge>
          <Badge variant="outline" className={statusClasses(session.status)}>
            {session.statusLabel}
          </Badge>
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
