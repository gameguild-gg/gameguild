'use client';

import type { LearningCohortsCohortSchedule } from '@game-guild/client';
import { Badge } from '@game-guild/ui/components/badge';

import { formatScheduleDate, itemPrimaryDate, itemTypeLabel, scheduleItems } from './schedule-view-utils';

export function TimelineView({ schedule }: { schedule: LearningCohortsCohortSchedule }) {
  const items = scheduleItems(schedule)
    .map((item, index) => ({ item, index, date: itemPrimaryDate(item) }))
    .sort((left, right) => {
      const dateDifference = new Date(left.date ?? 0).getTime() - new Date(right.date ?? 0).getTime();
      return dateDifference || (left.item.sortOrder ?? left.index) - (right.item.sortOrder ?? right.index);
    });

  if (items.length === 0) {
    return <p className="border-y border-dashed py-14 text-center text-sm text-muted-foreground">No timeline items have been scheduled.</p>;
  }

  return (
    <ol className="relative ml-3 border-l">
      {items.map(({ item, index, date }) => (
        <li key={item.id ?? index} data-testid="timeline-entry" className="relative pb-6 pl-7 last:pb-0">
          <span className="absolute -left-1.5 top-2 size-3 rounded-full border-2 border-background bg-primary" aria-hidden="true" />
          <div className="flex flex-wrap items-start justify-between gap-2 rounded-md border bg-card px-4 py-3">
            <div className="min-w-0">
              <p className="text-xs text-muted-foreground">
                {date ? formatScheduleDate(date, schedule.timezoneId) : 'Date not set'}
              </p>
              <p className="mt-1 text-sm font-medium">{item.title?.trim() || 'Untitled schedule item'}</p>
              {item.dueAt && item.dueAt !== date ? (
                <p className="mt-1 text-xs text-muted-foreground">Due {formatScheduleDate(item.dueAt, schedule.timezoneId)}</p>
              ) : null}
            </div>
            <div className="flex items-center gap-2">
              <Badge variant="outline">Week {Math.max(1, item.instructionalWeek ?? 1)}</Badge>
              <Badge variant="secondary">{itemTypeLabel(item.type)}</Badge>
            </div>
          </div>
        </li>
      ))}
    </ol>
  );
}
