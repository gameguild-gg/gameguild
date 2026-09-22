import { Badge } from "@game-guild/ui/components/badge";
import { buttonVariants } from "@game-guild/ui/components/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Link } from "@/i18n/navigation";
import { Calendar, Clock, Gamepad2, MapPin, Users } from "lucide-react";
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
  return (
    <Card className="flex h-full flex-col transition hover:-translate-y-0.5 hover:border-primary/40 hover:shadow-lg">
      <CardHeader className="gap-3 pb-2">
        <div className="flex items-center justify-between gap-3">
          <Badge variant="secondary">{session.mode}</Badge>
          <Badge variant="outline" className={statusClasses(session.status)}>
            {session.statusLabel}
          </Badge>
        </div>
        <div>
          <CardTitle className="text-base">{session.title}</CardTitle>
          <CardDescription className="mt-1 text-xs">
            Testing Event
          </CardDescription>
        </div>
      </CardHeader>
      <CardContent className="flex flex-1 flex-col gap-4">
        <p className="line-clamp-3 text-sm leading-6 text-muted-foreground">
          {session.description}
        </p>
        <EventMeta session={session} />
        <div className="flex items-start gap-2 border-t border-border pt-3 text-xs text-muted-foreground">
          <MapPin className="mt-0.5 size-3.5 shrink-0" />
          <span>{session.location}</span>
        </div>
        {almostFull && session.status === "open" ? (
          <p className="rounded-md border border-destructive/40 bg-destructive/10 px-3 py-2 text-xs font-medium text-destructive">
            Only {session.availableTesterCount} tester{" "}
            {session.availableTesterCount === 1 ? "seat" : "seats"} left
          </p>
        ) : null}
        <Link
          href={eventHref(session.id, projectId)}
          className={buttonVariants({ size: "sm", className: "mt-auto w-full" })}
        >
          View event
        </Link>
      </CardContent>
    </Card>
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
