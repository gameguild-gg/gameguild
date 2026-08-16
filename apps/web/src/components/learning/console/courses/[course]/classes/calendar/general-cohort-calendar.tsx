'use client';

import { Link } from '@/i18n/navigation';
import type { CourseCohortSummary } from '@/lib/learning/queries/cohorts';
import type { LearningCohortsCohortCalendarEntry } from '@game-guild/client';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { CalendarDays, Clock3 } from 'lucide-react';
import { useState } from 'react';

interface GeneralCohortCalendarProps {
  courseId: string;
  cohorts: CourseCohortSummary[];
  entries: LearningCohortsCohortCalendarEntry[];
}

const laneColors = [
  'border-l-emerald-500 bg-emerald-500/5',
  'border-l-sky-500 bg-sky-500/5',
  'border-l-amber-500 bg-amber-500/5',
  'border-l-fuchsia-500 bg-fuchsia-500/5',
];
const dateTimeFormatter = new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit', timeZone: 'UTC' });

export function GeneralCohortCalendar({ courseId, cohorts, entries }: GeneralCohortCalendarProps) {
  const [mode, setMode] = useState<'week' | 'month'>('week');

  return (
    <div className="space-y-5">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h2 className="text-xl font-semibold">General class calendar</h2>
          <p className="mt-1 text-sm text-muted-foreground">Compare overlapping cohorts without merging their schedules.</p>
        </div>
        <div className="inline-flex w-fit rounded-md border p-1" aria-label="Calendar view">
          <Button type="button" size="sm" variant={mode === 'week' ? 'secondary' : 'ghost'} onClick={() => setMode('week')}>Week</Button>
          <Button type="button" size="sm" variant={mode === 'month' ? 'secondary' : 'ghost'} onClick={() => setMode('month')}>Month</Button>
        </div>
      </div>

      <div className="space-y-3" data-calendar-mode={mode}>
        {cohorts.map((cohort, index) => {
          const cohortEntries = entries
            .filter((entry) => entry.cohortId === cohort.id)
            .sort((left, right) => String(left.startsAt ?? left.availableFrom ?? '').localeCompare(String(right.startsAt ?? right.availableFrom ?? '')));

          return (
            <section
              key={cohort.id}
              aria-label={`${cohort.name} calendar lane`}
              className={`rounded-lg border border-l-4 ${laneColors[index % laneColors.length]}`}
            >
              <header className="flex flex-col gap-2 border-b px-4 py-3 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <Link href={`/workspace/learning/courses/${courseId}/classes/${cohort.id}/schedule`} className="font-medium hover:underline">{cohort.name}</Link>
                  <p className="mt-0.5 text-xs text-muted-foreground">{cohort.meetingPattern ?? 'Meeting pattern not configured'}</p>
                </div>
                <Badge variant="outline">{cohortEntries.length} scheduled items</Badge>
              </header>

              <div className={mode === 'week' ? 'grid gap-2 p-3 sm:grid-cols-2 xl:grid-cols-4' : 'grid gap-2 p-3 sm:grid-cols-2 lg:grid-cols-3'}>
                {cohortEntries.length === 0 ? (
                  <div className="col-span-full flex items-center gap-2 px-2 py-6 text-sm text-muted-foreground">
                    <CalendarDays className="size-4" /> No schedule has been applied to this class.
                  </div>
                ) : cohortEntries.map((entry, itemIndex) => {
                  const instant = entry.startsAt ?? entry.availableFrom ?? entry.dueAt;
                  return (
                    <Link
                      key={entry.itemId ?? `${cohort.id}-${itemIndex}`}
                      href={`/workspace/learning/courses/${courseId}/classes/${cohort.id}/schedule#item-${entry.itemId ?? itemIndex}`}
                      className="min-w-0 rounded-md border bg-background p-3 transition-colors hover:bg-muted/50"
                    >
                      <p className="truncate text-sm font-medium">{entry.title || 'Untitled schedule item'}</p>
                      <div className="mt-2 flex items-center gap-1.5 text-xs text-muted-foreground">
                        <Clock3 className="size-3.5" />
                        {instant ? dateTimeFormatter.format(new Date(instant)) : 'Date not set'}
                      </div>
                      <p className="mt-1 text-xs text-muted-foreground">{entry.type ?? 'Schedule item'}</p>
                    </Link>
                  );
                })}
              </div>
            </section>
          );
        })}
      </div>
    </div>
  );
}
