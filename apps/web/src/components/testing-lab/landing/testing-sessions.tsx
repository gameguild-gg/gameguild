"use client";

import { Button } from "@game-guild/ui/components/button";
import { Input } from "@game-guild/ui/components/input";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from "@game-guild/ui/components/sheet";
import {
  CalendarDays,
  LayoutGrid,
  List,
  Search,
  SlidersHorizontal,
  Table2,
  X,
} from "lucide-react";
import { useMemo, useState } from "react";
import type { TestingEventViewModel } from "./testing-events-presentation";
import { TestingEventsEmptyState } from "./testing-events-empty-state";
import {
  TestingEventCard,
  TestingEventRow,
  TestingEventsCalendar,
  TestingEventsTable,
} from "./testing-event-views";
type ViewMode = "cards" | "row" | "table" | "calendar";
type StatusFilter = "all" | "open" | "in-progress" | "completed";
type ModeFilter = "all" | "Online" | "InPerson" | "Hybrid";
type PeriodFilter = "all" | "upcoming" | "month";

const STATUS_OPTIONS = [
  ["all", "All"],
  ["open", "Open"],
  ["in-progress", "In progress"],
  ["completed", "Completed"],
] as const;
const FORMAT_OPTIONS = [
  ["all", "All"],
  ["Online", "Online"],
  ["InPerson", "In person"],
  ["Hybrid", "Hybrid"],
] as const;
const SCHEDULE_OPTIONS = [
  ["all", "Any"],
  ["upcoming", "Upcoming"],
  ["month", "This month"],
] as const;

/** Segmented toggle shared by the desktop toolbar and the mobile filter sheet. */
function SegmentedFilter<T extends string>({
  label,
  value,
  options,
  onChange,
  className = "flex h-9 items-stretch overflow-hidden rounded-md border border-input text-sm",
}: {
  label: string;
  value: T;
  options: ReadonlyArray<readonly [T, string]>;
  onChange: (value: T) => void;
  className?: string;
}) {
  return (
    <div role="group" aria-label={`Filter by ${label}`} className={className}>
      {options.map(([optionValue, optionLabel]) => (
        <button
          key={optionValue}
          type="button"
          aria-pressed={value === optionValue}
          onClick={() => onChange(optionValue)}
          className={`border-l border-input px-2.5 transition first:border-l-0 ${
            value === optionValue
              ? "bg-primary/15 font-medium text-primary"
              : "text-muted-foreground hover:bg-accent hover:text-foreground"
          }`}
        >
          {optionLabel}
        </button>
      ))}
    </div>
  );
}

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
  const [sheetOpen, setSheetOpen] = useState(false);
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

  const activeFilterCount = [
    Boolean(search.trim()),
    status !== "all",
    mode !== "all",
    period !== "all",
  ].filter(Boolean).length;
  const hasFilters = activeFilterCount > 0;
  const clearFilters = () => {
    setSearch("");
    setStatus("all");
    setMode("all");
    setPeriod("all");
  };

  return (
    <div className="px-4 py-12 sm:px-6 lg:px-8">
      <div className="mx-auto w-full max-w-7xl">
        <header className="mb-8 text-center">
          <h1 className="text-3xl font-bold text-foreground md:text-4xl">
            Test. Play. Earn.
          </h1>
          <p className="mx-auto mt-2 max-w-2xl text-sm leading-6 text-muted-foreground sm:text-base">
            Join community game testing events, play upcoming projects, and
            provide feedback creators can use.
          </p>
        </header>

        {accessIssues.length > 0 ? (
          <div
            role="alert"
            className="mb-6 rounded-lg border border-destructive/40 bg-destructive/10 p-4 text-sm text-destructive"
          >
            Live testing events could not be refreshed. Retry shortly.
          </div>
        ) : null}

        {presentedEvents.length > 0 ? <section
          aria-label="Event filters"
          className="mb-6 flex flex-wrap items-center gap-2"
        >
          {/* Small screens: Filters button opens a labeled side sheet. Styled
              to match the segmented toggles' height and quiet colors. */}
          <button
            type="button"
            className="flex h-9 items-center gap-1.5 rounded-md border border-input px-2.5 text-sm text-muted-foreground transition hover:bg-accent hover:text-foreground lg:hidden"
            onClick={() => setSheetOpen(true)}
          >
            <SlidersHorizontal className="size-4" aria-hidden="true" />
            Filters
            {activeFilterCount > 0 ? (
              <span className="rounded-full bg-primary px-1.5 text-xs font-semibold text-primary-foreground">
                {activeFilterCount}
              </span>
            ) : null}
          </button>

          <Sheet open={sheetOpen} onOpenChange={setSheetOpen}>
            <SheetContent side="left" className="w-72 overflow-y-auto p-0">
              <SheetHeader className="px-4 pt-4">
                <SheetTitle>Filters</SheetTitle>
              </SheetHeader>
              <div className="space-y-5 px-4 pb-6">
                <div>
                  <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                    Search
                  </p>
                  <Input
                    suppressHydrationWarning
                    value={search}
                    onChange={(event) => setSearch(event.target.value)}
                    placeholder="Search events..."
                  />
                </div>
                {(
                  [
                    ["Status", status, setStatus, STATUS_OPTIONS],
                    ["Format", mode, setMode, FORMAT_OPTIONS],
                    ["Schedule", period, setPeriod, SCHEDULE_OPTIONS],
                  ] as const
                ).map(([label, value, setValue, options]) => (
                  <div key={label}>
                    <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                      {label}
                    </p>
                    <SegmentedFilter
                      label={label}
                      value={value}
                      options={options}
                      onChange={setValue}
                      className="flex flex-wrap rounded-md border border-input text-sm"
                    />
                  </div>
                ))}
                {hasFilters ? (
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    className="w-full"
                    onClick={clearFilters}
                  >
                    <X className="mr-1 size-3.5" />
                    Clear filters
                  </Button>
                ) : null}
              </div>
            </SheetContent>
          </Sheet>

          <label className="relative hidden min-w-44 flex-1 sm:max-w-xs lg:block">
            <span className="sr-only">Search events</span>
            <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              suppressHydrationWarning
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Search events..."
              className="pl-9"
            />
          </label>
          <SegmentedFilter
            label="status"
            value={status}
            options={STATUS_OPTIONS}
            onChange={setStatus}
            className="hidden h-9 items-stretch overflow-hidden rounded-md border border-input text-sm lg:flex"
          />
          <SegmentedFilter
            label="format"
            value={mode}
            options={FORMAT_OPTIONS}
            onChange={setMode}
            className="hidden h-9 items-stretch overflow-hidden rounded-md border border-input text-sm lg:flex"
          />
          <SegmentedFilter
            label="schedule"
            value={period}
            options={SCHEDULE_OPTIONS}
            onChange={setPeriod}
            className="hidden h-9 items-stretch overflow-hidden rounded-md border border-input text-sm lg:flex"
          />
          {hasFilters ? (
            <Button
              type="button"
              variant="ghost"
              size="sm"
              className="hidden lg:inline-flex"
              onClick={clearFilters}
            >
              <X className="mr-1 size-3.5" />
              Clear filters
            </Button>
          ) : null}

          {/* View switcher pinned to the right edge as one joined segmented
              control — rounded only on the outer corners. */}
          <div
            role="group"
            aria-label="Event view"
            className="ml-auto flex h-9 items-stretch overflow-hidden rounded-md border border-input"
          >
            {(
              [
                ["cards", "Switch to cards view", LayoutGrid],
                ["row", "Switch to rows view", List],
                ["table", "Switch to table view", Table2],
                ["calendar", "Switch to calendar view", CalendarDays],
              ] as const
            ).map(([value, label, Icon]) => (
              <button
                key={value}
                type="button"
                aria-label={label}
                aria-pressed={viewMode === value}
                onClick={() => setViewMode(value)}
                className={`flex w-9 items-center justify-center border-l border-input transition first:border-l-0 ${
                  viewMode === value
                    ? "bg-primary/15 text-primary"
                    : "text-muted-foreground hover:bg-accent hover:text-foreground"
                }`}
              >
                <Icon className="size-4" aria-hidden="true" />
              </button>
            ))}
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
        ) : viewMode === "table" ? (
          <TestingEventsTable sessions={filteredEvents} projectId={projectId} />
        ) : (
          <section aria-label="Testing events">
            <TestingEventsCalendar sessions={filteredEvents} projectId={projectId} />
          </section>
        )}
      </div>
    </div>
  );
}
