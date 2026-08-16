'use client';

import type { LearningCohortsCohortSchedule } from '@game-guild/client';
import { Badge } from '@game-guild/ui/components/badge';

import { calendarDayKey, formatCalendarDay, formatScheduleDate, itemTypeLabel, scheduleItems } from './schedule-view-utils';

interface CalendarEvent {
  key: string;
  date: string;
  title: string;
  type: string;
}

function scheduleEvents(schedule: LearningCohortsCohortSchedule): CalendarEvent[] {
  const events: CalendarEvent[] = [];
  scheduleItems(schedule).forEach((item, index) => {
    const title = item.title?.trim() || 'Untitled schedule item';
    const baseKey = item.id ?? `item-${index}`;
    if (item.availableFrom) {
      events.push({ key: `${baseKey}-available`, date: item.availableFrom, title, type: itemTypeLabel(item.type) });
    } else if (item.startsAt) {
      events.push({ key: `${baseKey}-starts`, date: item.startsAt, title, type: itemTypeLabel(item.type) });
    }
    if (item.dueAt) {
      events.push({ key: `${baseKey}-due`, date: item.dueAt, title: `${title} due`, type: 'Due date' });
    }
  });
  return events.sort((left, right) => new Date(left.date).getTime() - new Date(right.date).getTime());
}

export function CalendarView({ schedule }: { schedule: LearningCohortsCohortSchedule }) {
  const days = new Map<string, CalendarEvent[]>();
  for (const event of scheduleEvents(schedule)) {
    const key = calendarDayKey(event.date, schedule.timezoneId);
    days.set(key, [...(days.get(key) ?? []), event]);
  }

  if (days.size === 0) {
    return <p className="border-y border-dashed py-14 text-center text-sm text-muted-foreground">No calendar dates have been scheduled.</p>;
  }

  return (
    <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
      {[...days.entries()].map(([key, events]) => (
        <section key={key} role="group" aria-label={formatCalendarDay(events[0].date, schedule.timezoneId)} className="min-h-36 rounded-md border bg-card p-4">
          <div className="flex items-start justify-between gap-3">
            <div>
              <p className="text-xs font-medium uppercase text-muted-foreground">{formatCalendarDay(events[0].date, schedule.timezoneId)}</p>
              <p className="mt-1 text-2xl font-semibold tabular-nums">{key.slice(-2)}</p>
            </div>
            <Badge variant="outline">{events.length}</Badge>
          </div>
          <div className="mt-4 space-y-2">
            {events.map((event) => (
              <div key={event.key} className="border-l-2 border-primary/50 pl-2.5">
                <p className="text-sm font-medium leading-tight">{event.title}</p>
                <p className="mt-0.5 text-xs text-muted-foreground">
                  {formatScheduleDate(event.date, schedule.timezoneId, { hour: '2-digit', minute: '2-digit' })} · {event.type}
                </p>
              </div>
            ))}
          </div>
        </section>
      ))}
    </div>
  );
}
