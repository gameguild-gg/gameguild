'use client';

import { Link } from '@/i18n/navigation';
import {
  calendarEventSegments,
  calendarRange,
  calendarRangeLabel,
  calendarViews,
  shiftCalendarAnchor,
  type CalendarView,
} from '@/lib/testing-lab/calendar';
import { formatTestingEventStatus } from '@/lib/testing-lab/format';
import type { TestingLabTestingEventProjection } from '@game-guild/client';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { format, isSameMonth, startOfMonth } from 'date-fns';
import { BarChart3, CalendarDays, ChevronLeft, ChevronRight, FolderKanban, Settings, Users } from 'lucide-react';
import { useMemo, useState } from 'react';

import { CreateTestingEventDialog } from './testing-event-management';

const operations = [
  { label: 'Events', href: '/dashboard/testing-lab/events', icon: CalendarDays },
  { label: 'Projects', href: '/dashboard/testing-lab/projects', icon: FolderKanban },
  { label: 'Participants', href: '/dashboard/testing-lab/participants', icon: Users },
  { label: 'Analytics', href: '/dashboard/testing-lab/analytics', icon: BarChart3 },
  { label: 'Settings', href: '/dashboard/testing-lab/settings', icon: Settings },
];

const viewLabels: Record<CalendarView, string> = {
  day: 'Day',
  week: 'Week',
  month: 'Month',
  year: 'Year',
  schedule: 'Schedule',
  '3days': '3 days',
};

function eventStatusClass(status?: string | null) {
  switch (status) {
    case 'Active':
      return 'border-primary/35 bg-primary/10 text-foreground';
    case 'Completed':
      return 'border-border bg-muted text-muted-foreground';
    case 'Cancelled':
      return 'border-destructive/30 bg-destructive/10 text-destructive';
    default:
      return 'border-border bg-muted/60 text-foreground';
  }
}

function eventStart(event: TestingLabTestingEventProjection) {
  const date = event.startsAt ? new Date(event.startsAt) : null;
  return date && !Number.isNaN(date.valueOf()) ? date : null;
}

function EventLink({ event, compact = false }: { event: TestingLabTestingEventProjection; compact?: boolean }) {
  if (!event.id) return null;
  const startsAt = eventStart(event);

  return (
    <Link
      href={`/dashboard/testing-lab/events/${event.id}`}
      className={`block rounded border px-2 py-1 text-left text-xs transition-colors hover:brightness-95 ${eventStatusClass(event.status)}`}
      aria-label={`${event.name ?? 'Untitled event'}${startsAt ? `, ${format(startsAt, 'PPp')}` : ''}`}
    >
      <span className="block truncate font-medium">{event.name ?? 'Untitled event'}</span>
      {compact ? null : <span className="block truncate opacity-75">{formatTestingEventStatus(event.status)}</span>}
    </Link>
  );
}

function ScheduleView({ events, anchor }: { events: TestingLabTestingEventProjection[]; anchor: Date }) {
  const range = calendarRange(anchor, 'schedule', true);
  const scheduled = events
    .filter((event) => {
      const startsAt = eventStart(event);
      return startsAt && startsAt >= range.start && startsAt <= range.end;
    })
    .sort((left, right) => (eventStart(left)?.valueOf() ?? 0) - (eventStart(right)?.valueOf() ?? 0));

  return (
    <section aria-label="Testing Lab schedule" className="rounded-md border">
      <div className="border-b px-4 py-3">
        <h2 className="text-lg font-semibold">Schedule</h2>
        <p className="text-sm text-muted-foreground">The next 90 days of Testing Lab events.</p>
      </div>
      {scheduled.length === 0 ? (
        <p className="p-4 text-sm text-muted-foreground">No Testing Lab events are scheduled in this period.</p>
      ) : (
        <ol className="divide-y">
          {scheduled.map((event) => {
            const startsAt = eventStart(event);
            return (
              <li key={event.id} className="flex items-center gap-4 p-4">
                <time className="w-28 shrink-0 text-sm text-muted-foreground" dateTime={startsAt?.toISOString()}>
                  {startsAt ? format(startsAt, 'EEE, MMM d · p') : 'Unscheduled'}
                </time>
                <div className="min-w-0 flex-1"><EventLink event={event} compact /></div>
                <Badge variant="outline">{formatTestingEventStatus(event.status)}</Badge>
              </li>
            );
          })}
        </ol>
      )}
    </section>
  );
}

function YearView({ events, anchor }: { events: TestingLabTestingEventProjection[]; anchor: Date }) {
  const months = Array.from({ length: 12 }, (_, index) => new Date(anchor.getFullYear(), index, 1));

  return (
    <section aria-label="Testing Lab year" className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
      {months.map((month) => {
        const monthEvents = events.filter((event) => {
          const startsAt = eventStart(event);
          return startsAt && isSameMonth(startsAt, month);
        });
        return (
          <div key={month.toISOString()} className="rounded-md border p-3">
            <h2 className="text-sm font-semibold">{format(month, 'MMMM')}</h2>
            {monthEvents.length === 0 ? (
              <p className="mt-3 text-sm text-muted-foreground">No events</p>
            ) : (
              <div className="mt-3 space-y-2">
                {monthEvents.slice(0, 4).map((event) => <EventLink key={event.id} event={event} />)}
                {monthEvents.length > 4 ? <p className="text-xs text-muted-foreground">+{monthEvents.length - 4} more events</p> : null}
              </div>
            )}
          </div>
        );
      })}
    </section>
  );
}

function GridView({ events, anchor, view, showWeekends }: {
  events: TestingLabTestingEventProjection[];
  anchor: Date;
  view: Exclude<CalendarView, 'year' | 'schedule'>;
  showWeekends: boolean;
}) {
  const range = calendarRange(anchor, view, showWeekends);
  const segmentsByDay = useMemo(() => {
    const segments = calendarEventSegments(events, range);
    return segments.reduce<Map<string, typeof segments>>((byDay, segment) => {
      const values = byDay.get(segment.dayKey) ?? [];
      values.push(segment);
      byDay.set(segment.dayKey, values);
      return byDay;
    }, new Map());
  }, [events, range]);
  const weekdays = showWeekends
    ? ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat']
    : ['Mon', 'Tue', 'Wed', 'Thu', 'Fri'];
  const monthStart = startOfMonth(anchor);

  return (
    <section aria-label={`${viewLabels[view]} Testing Lab calendar`} className="overflow-hidden rounded-md border">
      <div className="grid border-b text-center text-xs font-medium text-muted-foreground" style={{ gridTemplateColumns: `repeat(${weekdays.length}, minmax(0, 1fr))` }}>
        {weekdays.map((weekday) => <div key={weekday} className="p-2">{weekday}</div>)}
      </div>
      <div className="grid" style={{ gridTemplateColumns: `repeat(${weekdays.length}, minmax(0, 1fr))` }}>
        {range.days.map((day) => {
          const key = format(day, 'yyyy-MM-dd');
          const segments = segmentsByDay.get(key) ?? [];
          const outsideMonth = view === 'month' && !isSameMonth(day, monthStart);
          return (
            <div key={key} className={`min-h-32 border-b border-r p-2 last:border-r-0 ${outsideMonth ? 'bg-muted/25 text-muted-foreground' : ''}`}>
              <time dateTime={day.toISOString()} className="mb-2 block text-xs font-medium">{format(day, 'd')}</time>
              <div className="space-y-1">
                {segments.slice(0, 3).map((segment) => <EventLink key={`${segment.event.id}-${segment.dayKey}`} event={segment.event} compact />)}
                {segments.length > 3 ? <p className="text-xs text-muted-foreground">+{segments.length - 3} more</p> : null}
              </div>
            </div>
          );
        })}
      </div>
    </section>
  );
}

export function TestingLabCalendar({ events, initialDate = new Date() }: {
  events: TestingLabTestingEventProjection[];
  initialDate?: Date;
}) {
  const [view, setView] = useState<CalendarView>('month');
  const [anchor, setAnchor] = useState(() => initialDate);
  const [showWeekends, setShowWeekends] = useState(true);
  const range = calendarRange(anchor, view, showWeekends);

  return (
    <section aria-label="Testing Lab calendar" className="space-y-4">
      <nav aria-label="Testing Lab operations" className="flex flex-wrap gap-2 rounded-md border p-2">
        {operations.map(({ label, href, icon: Icon }) => (
          <Link
            key={href}
            href={href}
            aria-label={`${label} workspace`}
            className="inline-flex size-10 items-center justify-center rounded-sm text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
          >
            <Icon className="size-4" aria-hidden="true" />
            <span className="sr-only">{label} workspace</span>
          </Link>
        ))}
      </nav>

      <div className="flex flex-wrap items-center gap-2">
        <Button type="button" variant="outline" size="icon" aria-label="Previous period" onClick={() => setAnchor((date) => shiftCalendarAnchor(date, view, -1))}>
          <ChevronLeft className="size-4" />
        </Button>
        <Button type="button" variant="outline" onClick={() => setAnchor(new Date())}>Today</Button>
        <Button type="button" variant="outline" size="icon" aria-label="Next period" onClick={() => setAnchor((date) => shiftCalendarAnchor(date, view, 1))}>
          <ChevronRight className="size-4" />
        </Button>
        <h2 className="min-w-44 text-lg font-semibold">{calendarRangeLabel(anchor, view, range)}</h2>
        <label className="ml-auto inline-flex items-center gap-2 rounded-md border px-3 py-2 text-sm font-medium">
          <span className="sr-only">Calendar view</span>
          <select aria-label="Calendar view" value={view} onChange={(event) => setView(event.target.value as CalendarView)} className="bg-transparent outline-none">
            {calendarViews.map((value) => <option key={value} value={value}>{viewLabels[value]}</option>)}
          </select>
        </label>
        <Button type="button" variant="outline" aria-pressed={showWeekends} onClick={() => setShowWeekends((visible) => !visible)}>
          {showWeekends ? 'Hide weekends' : 'Show weekends'}
        </Button>
        <CreateTestingEventDialog />
      </div>

      {view === 'schedule' ? <ScheduleView events={events} anchor={anchor} /> : null}
      {view === 'year' ? <YearView events={events} anchor={anchor} /> : null}
      {view !== 'schedule' && view !== 'year' ? <GridView events={events} anchor={anchor} view={view} showWeekends={showWeekends} /> : null}
    </section>
  );
}
