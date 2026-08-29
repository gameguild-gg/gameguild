"use client";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { LayoutGrid, List, Search, Table2, X } from "lucide-react";
import { useMemo, useState } from "react";
import type { TestingEventViewModel } from "./testing-events-presentation";
import { TestingEventsEmptyState } from "./testing-events-empty-state";
import {
  TestingEventCard,
  TestingEventRow,
  TestingEventsTable,
} from "./testing-event-views";
type ViewMode = "cards" | "row" | "table";
type StatusFilter = "all" | "open" | "in-progress" | "completed";
type ModeFilter = "all" | "Online" | "InPerson" | "Hybrid";
type PeriodFilter = "all" | "upcoming" | "month";

interface TestingEventsBrowserProps {
  events: TestingEventViewModel[];
  accessIssues: string[];
  projectId?: string;
}
export function TestingEventsBrowser({
  events,
  accessIssues,
  projectId,
}: TestingEventsBrowserProps) {
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState<StatusFilter>("all");
  const [mode, setMode] = useState<ModeFilter>("all");
  const [period, setPeriod] = useState<PeriodFilter>("all");
  const [viewMode, setViewMode] = useState<ViewMode>("cards");
  const presentedEvents = events;
  const filteredEvents = useMemo(() => {
    const term = search.trim().toLowerCase();
    return presentedEvents
      .filter(
        (session) =>
          !term ||
          [
            session.title,
            session.description,
            session.location,
            session.mode,
          ].some((value) => value.toLowerCase().includes(term)),
      )
      .filter((session) => status === "all" || session.status === status)
      .filter(
        (session) =>
          mode === "all" ||
          session.mode === (mode === "InPerson" ? "In person" : mode),
      )
      .filter((session) => {
        if (period === "all") return true;
        if (!session.startsAt) return false;
        const start = new Date(session.startsAt);
        const now = new Date();
        if (period === "upcoming") return start >= now;
        return (
          start.getFullYear() === now.getFullYear() &&
          start.getMonth() === now.getMonth()
        );
      })
      .sort((left, right) =>
        (left.startsAt ?? "").localeCompare(right.startsAt ?? ""),
      );
  }, [mode, period, search, presentedEvents, status]);

  const hasFilters =
    Boolean(search.trim()) ||
    status !== "all" ||
    mode !== "all" ||
    period !== "all";
  const openEvents = presentedEvents.filter(
    (session) => session.status === "open" || session.status === "in-progress",
  ).length;
  const clearFilters = () => {
    setSearch("");
    setStatus("all");
    setMode("all");
    setPeriod("all");
  };

  return (
    <div className="min-h-screen bg-gradient-to-b from-slate-950 via-slate-900 to-slate-950 px-4 py-12 text-white sm:px-6 lg:px-8">
      <div className="mx-auto w-full max-w-7xl">
        <header className="mb-12 text-center">
          {presentedEvents.length > 0 ? (
            <div className="mb-6 flex justify-center">
              <div className="flex items-center gap-2 rounded-full border border-blue-400/30 bg-gradient-to-r from-blue-600/20 to-purple-600/20 px-4 py-2 backdrop-blur-sm">
                <span className="size-2 animate-pulse rounded-full bg-blue-400" />
                <span className="text-sm font-semibold text-blue-300">
                  {openEvents} Open {openEvents === 1 ? "Event" : "Events"} -
                  Join Now!
                </span>
              </div>
            </div>
          ) : null}
          <h1
            className="my-8 text-5xl font-bold text-white md:text-6xl"
            style={{
              textShadow:
                "0 0 8px rgba(59, 130, 246, 0.25), 0 0 16px rgba(147, 51, 234, 0.2)",
            }}
          >
            Test. Play. Earn.
          </h1>
          <p className="mx-auto max-w-3xl text-lg leading-8 text-slate-300 sm:text-xl">
            Join community game testing events, play upcoming projects, and
            provide feedback creators can use.
          </p>
        </header>

        {accessIssues.length > 0 ? (
          <div
            role="alert"
            className="mb-6 rounded-lg border border-amber-500/40 bg-amber-950/30 p-4 text-sm text-amber-100"
          >
            Live testing events could not be refreshed. Retry shortly.
          </div>
        ) : null}

        {presentedEvents.length > 0 ? <section
          aria-label="Event filters"
          className="mb-8 rounded-xl border border-slate-700/70 bg-slate-900/50 p-4 backdrop-blur-sm"
        >
          <div className="grid gap-3 xl:grid-cols-[minmax(16rem,1fr)_12rem_12rem_12rem_auto] xl:items-center">
            <label className="relative block">
              <span className="sr-only">Search events</span>
              <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-slate-400" />
              <Input
                suppressHydrationWarning
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Search events..."
                className="border-slate-700 bg-slate-950/60 pl-9"
              />
            </label>
            <Select
              value={status}
              onValueChange={(value) => setStatus(value as StatusFilter)}
            >
              <SelectTrigger aria-label="Filter by status">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All statuses</SelectItem>
                <SelectItem value="open">Open</SelectItem>
                <SelectItem value="in-progress">In progress</SelectItem>
                <SelectItem value="completed">Completed</SelectItem>
              </SelectContent>
            </Select>
            <Select
              value={mode}
              onValueChange={(value) => setMode(value as ModeFilter)}
            >
              <SelectTrigger aria-label="Filter by format">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All formats</SelectItem>
                <SelectItem value="Online">Online</SelectItem>
                <SelectItem value="InPerson">In person</SelectItem>
                <SelectItem value="Hybrid">Hybrid</SelectItem>
              </SelectContent>
            </Select>
            <Select
              value={period}
              onValueChange={(value) => setPeriod(value as PeriodFilter)}
            >
              <SelectTrigger aria-label="Filter by schedule">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Any schedule</SelectItem>
                <SelectItem value="upcoming">Upcoming</SelectItem>
                <SelectItem value="month">This month</SelectItem>
              </SelectContent>
            </Select>
            <div className="hidden items-center justify-end gap-2 lg:flex">
              {(
                [
                  ["cards", "Switch to cards view", LayoutGrid],
                  ["row", "Switch to rows view", List],
                  ["table", "Switch to table view", Table2],
                ] as const
              ).map(([value, label, Icon]) => (
                <Button
                  key={value}
                  type="button"
                  size="icon"
                  variant={viewMode === value ? "default" : "outline"}
                  aria-label={label}
                  aria-pressed={viewMode === value}
                  onClick={() => setViewMode(value)}
                >
                  <Icon className="size-4" />
                </Button>
              ))}
            </div>
          </div>
          <div className="mt-3 flex items-center justify-between gap-4 text-sm text-slate-400">
            <span>
              {filteredEvents.length} of {presentedEvents.length} events
            </span>
            {hasFilters ? (
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={clearFilters}
              >
                <X className="mr-2 size-4" />
                Clear filters
              </Button>
            ) : null}
          </div>
        </section> : null}

        {filteredEvents.length === 0 ? (
          <TestingEventsEmptyState
            filtered={hasFilters}
            hasEvents={presentedEvents.length > 0}
            clearFilters={clearFilters}
          />
        ) : viewMode === "cards" ? (
          <section
            aria-label="Testing events"
            className="grid gap-5 md:grid-cols-2 xl:grid-cols-3"
          >
            {filteredEvents.map((session) => (
              <TestingEventCard key={session.id} session={session} projectId={projectId} />
            ))}
          </section>
        ) : viewMode === "row" ? (
          <section aria-label="Testing events" className="space-y-3">
            {filteredEvents.map((session) => (
              <TestingEventRow key={session.id} session={session} projectId={projectId} />
            ))}
          </section>
        ) : (
          <TestingEventsTable sessions={filteredEvents} projectId={projectId} />
        )}
      </div>
    </div>
  );
}
