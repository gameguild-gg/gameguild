import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
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
    return "border-green-700 bg-green-900/20 text-green-300";
  if (status === "in-progress")
    return "border-blue-700 bg-blue-900/20 text-blue-300";
  if (status === "completed")
    return "border-slate-600 bg-slate-800/50 text-slate-300";
  return "border-red-800 bg-red-950/30 text-red-300";
}

function eventHref(eventId: string, projectId?: string) {
  const path = `/testing-lab/events/${eventId}`;
  return projectId
    ? `${path}?projectId=${encodeURIComponent(projectId)}`
    : path;
}

function EventMeta({ session }: { session: TestingEventViewModel }) {
  return (
    <div className="grid grid-cols-2 gap-3 text-xs text-slate-400">
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
    session.availableTesterCount != null && session.availableTesterCount <= 2;
  return (
    <Card className="flex h-full flex-col border-slate-700 bg-gradient-to-br from-slate-900/60 to-slate-800/50 backdrop-blur-sm transition hover:-translate-y-0.5 hover:border-slate-500 hover:shadow-lg hover:shadow-blue-950/30">
      <CardHeader className="gap-3 pb-2">
        <div className="flex items-center justify-between gap-3">
          <Badge variant="secondary" className="bg-slate-800 text-slate-200">
            {session.mode}
          </Badge>
          <Badge variant="outline" className={statusClasses(session.status)}>
            {session.statusLabel}
          </Badge>
        </div>
        <div>
          <CardTitle className="text-base text-white">
            {session.title}
          </CardTitle>
          <CardDescription className="mt-1 text-xs text-slate-400">
            Testing Event
          </CardDescription>
        </div>
      </CardHeader>
      <CardContent className="flex flex-1 flex-col gap-4">
        <p className="line-clamp-3 text-sm leading-6 text-slate-300">
          {session.description}
        </p>
        <EventMeta session={session} />
        <div className="flex items-start gap-2 border-t border-slate-700/70 pt-3 text-xs text-slate-400">
          <MapPin className="mt-0.5 size-3.5 shrink-0" />
          <span>{session.location}</span>
        </div>
        {almostFull && session.status === "open" ? (
          <p className="rounded-md border border-orange-700/60 bg-orange-950/30 px-3 py-2 text-xs font-medium text-orange-300">
            Only {session.availableTesterCount} tester{" "}
            {session.availableTesterCount === 1 ? "seat" : "seats"} left
          </p>
        ) : null}
        <Button
          asChild
          size="sm"
          className="mt-auto w-full border border-blue-400/40 bg-gradient-to-r from-blue-600/40 to-blue-500/30 text-white hover:from-blue-600/90 hover:to-blue-500/90"
        >
          <Link href={eventHref(session.id, projectId)}>View event</Link>
        </Button>
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
    <article className="grid gap-5 rounded-lg border border-slate-700 bg-gradient-to-br from-slate-900/60 to-slate-800/50 p-5 transition hover:border-slate-500 lg:grid-cols-[minmax(0,1fr)_18rem_10rem] lg:items-center">
      <div className="min-w-0">
        <div className="mb-2 flex flex-wrap items-center gap-2">
          <h2 className="text-lg font-bold text-white">{session.title}</h2>
          <Badge variant="secondary" className="bg-slate-800 text-slate-200">
            {session.mode}
          </Badge>
          <Badge variant="outline" className={statusClasses(session.status)}>
            {session.statusLabel}
          </Badge>
        </div>
        <p className="line-clamp-2 text-sm leading-6 text-slate-300">
          {session.description}
        </p>
        <p className="mt-3 flex items-center gap-2 text-xs text-slate-400">
          <MapPin className="size-3.5" />
          {session.location}
        </p>
      </div>
      <EventMeta session={session} />
      <Button
        asChild
        size="sm"
        className="w-full border border-blue-400/40 bg-blue-600/40 text-white hover:bg-blue-600/80"
      >
        <Link href={eventHref(session.id, projectId)}>View event</Link>
      </Button>
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
    <div className="overflow-x-auto rounded-lg border border-slate-700 bg-gradient-to-br from-slate-900/60 to-slate-800/50">
      <table className="w-full min-w-[980px] text-sm">
        <thead className="border-b border-slate-700 bg-slate-800/60 text-left text-slate-300">
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
              className="border-b border-slate-700/60 text-slate-300 last:border-0 hover:bg-slate-800/40"
            >
              <td className="p-4">
                <p className="font-medium text-white">{session.title}</p>
                <p className="mt-1 max-w-xs truncate text-xs text-slate-400">
                  {session.description}
                </p>
              </td>
              <td className="p-4">{session.location}</td>
              <td className="p-4">
                <p>{formatDate(session.startsAt)}</p>
                <p className="text-xs text-slate-400">
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
                <p className="text-xs text-slate-400">
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
                <Button asChild size="sm" variant="outline">
                  <Link href={eventHref(session.id, projectId)}>
                    View event
                  </Link>
                </Button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
