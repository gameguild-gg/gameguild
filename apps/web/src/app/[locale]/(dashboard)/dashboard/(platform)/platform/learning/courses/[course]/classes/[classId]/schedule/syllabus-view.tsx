'use client';

import type { LearningCohortsCohortSchedule, LearningCohortsCohortScheduleItem } from '@game-guild/client';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { CalendarClock, FileClock, MoveRight, Pencil, Radio, Target } from 'lucide-react';

import { formatScheduleDate, itemTypeLabel, scheduleItems } from './schedule-view-utils';

interface SyllabusViewProps {
  schedule: LearningCohortsCohortSchedule;
  readOnly?: boolean;
  onShift?: (item: LearningCohortsCohortScheduleItem) => void;
  onEdit?: (item: LearningCohortsCohortScheduleItem) => void;
}

function typeIcon(item: LearningCohortsCohortScheduleItem) {
  if (item.type === 'LiveSession') return Radio;
  if (item.type === 'AssessmentWindow') return Target;
  if (item.type === 'Milestone') return CalendarClock;
  return FileClock;
}

function itemMeta(item: LearningCohortsCohortScheduleItem, timezoneId: string | null | undefined) {
  const values: string[] = [];
  const available = formatScheduleDate(item.availableFrom, timezoneId);
  const starts = formatScheduleDate(item.startsAt, timezoneId);
  const due = formatScheduleDate(item.dueAt, timezoneId);
  if (available) values.push(`Available ${available}`);
  if (starts) values.push(`Starts ${starts}`);
  if (due) values.push(`Due ${due}`);
  if (item.location) values.push(item.location);
  return values;
}

export function SyllabusView({ schedule, readOnly = false, onShift, onEdit }: SyllabusViewProps) {
  const weeks = new Map<number, LearningCohortsCohortScheduleItem[]>();
  for (const item of scheduleItems(schedule)) {
    const week = Math.max(1, item.instructionalWeek ?? 1);
    weeks.set(week, [...(weeks.get(week) ?? []), item]);
  }

  if (weeks.size === 0) {
    return (
      <div className="border-y border-dashed py-14 text-center">
        <h3 className="font-medium">No scheduled content yet</h3>
        <p className="mt-1 text-sm text-muted-foreground">Build a schedule to turn the course curriculum into a class syllabus.</p>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {[...weeks.entries()].map(([week, items]) => {
        const anchor = items.find((item) => item.type === 'ContentRelease') ?? items[0];
        const heading = `Week ${week} - ${anchor?.title?.trim() || 'Instructional plan'}`;

        return (
          <section key={week} className="overflow-hidden rounded-md border bg-card" aria-labelledby={`week-${week}`}>
            <div className="flex min-h-12 items-center justify-between gap-3 border-b bg-muted/35 px-4 py-2.5">
              <h3 id={`week-${week}`} className="text-sm font-semibold">{heading}</h3>
              <Badge variant="outline">{items.length} {items.length === 1 ? 'item' : 'items'}</Badge>
            </div>
            <div className="divide-y">
              {items.map((item, index) => {
                const Icon = typeIcon(item);
                const title = item.title?.trim() || 'Untitled schedule item';
                return (
                  <div key={item.id ?? `${week}-${index}`} className="flex min-h-16 items-center gap-3 px-4 py-3">
                    <span className="flex size-8 shrink-0 items-center justify-center rounded-md bg-muted text-muted-foreground">
                      <Icon className="size-4" aria-hidden="true" />
                    </span>
                    <div className="min-w-0 flex-1">
                      <div className="flex flex-wrap items-center gap-2">
                        <p className="truncate text-sm font-medium">{title}</p>
                        <Badge variant="secondary" className="font-normal">{itemTypeLabel(item.type)}</Badge>
                      </div>
                      <p className="mt-1 truncate text-xs text-muted-foreground">
                        {itemMeta(item, schedule.timezoneId).join(' · ') || 'Date not set'}
                      </p>
                    </div>
                    {!readOnly && item.id ? (
                      <div className="flex shrink-0 items-center gap-1">
                        {onEdit ? (
                          <Button type="button" variant="ghost" size="icon-sm" aria-label={`Edit ${title}`} onClick={() => onEdit(item)}>
                            <Pencil className="size-4" />
                          </Button>
                        ) : null}
                        {onShift ? (
                          <Button type="button" variant="ghost" size="icon-sm" aria-label={`Shift ${title}`} onClick={() => onShift(item)}>
                            <MoveRight className="size-4" />
                          </Button>
                        ) : null}
                      </div>
                    ) : null}
                  </div>
                );
              })}
            </div>
          </section>
        );
      })}
    </div>
  );
}
