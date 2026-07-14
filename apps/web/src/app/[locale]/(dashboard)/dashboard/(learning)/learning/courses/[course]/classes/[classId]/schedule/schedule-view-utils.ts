import type {
  LearningCohortsCohortSchedule,
  LearningCohortsCohortScheduleItem,
  LearningCohortsCohortScheduleItemType,
} from '@game-guild/client';

export function scheduleItems(schedule: LearningCohortsCohortSchedule): LearningCohortsCohortScheduleItem[] {
  return [...(schedule.items ?? [])].sort((left, right) => {
    const weekDifference = (left.instructionalWeek ?? 0) - (right.instructionalWeek ?? 0);
    return weekDifference || (left.sortOrder ?? 0) - (right.sortOrder ?? 0);
  });
}

export function itemPrimaryDate(item: LearningCohortsCohortScheduleItem): string | null {
  return item.availableFrom ?? item.startsAt ?? item.dueAt ?? item.availableUntil ?? null;
}

export function formatScheduleDate(
  value: string | null | undefined,
  timezoneId: string | null | undefined,
  options: Intl.DateTimeFormatOptions = {},
): string | null {
  if (!value) return null;
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return null;

  return new Intl.DateTimeFormat('en-US', {
    timeZone: timezoneId || 'UTC',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
    ...options,
  }).format(date);
}

export function formatCalendarDay(value: string, timezoneId: string | null | undefined): string {
  return new Intl.DateTimeFormat('en-US', {
    timeZone: timezoneId || 'UTC',
    weekday: 'long',
    month: 'long',
    day: 'numeric',
    year: 'numeric',
  }).format(new Date(value));
}

export function calendarDayKey(value: string, timezoneId: string | null | undefined): string {
  const parts = new Intl.DateTimeFormat('en-CA', {
    timeZone: timezoneId || 'UTC',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).formatToParts(new Date(value));
  const part = (type: Intl.DateTimeFormatPartTypes) => parts.find((candidate) => candidate.type === type)?.value ?? '';
  return `${part('year')}-${part('month')}-${part('day')}`;
}

export function itemTypeLabel(type: LearningCohortsCohortScheduleItemType | undefined): string {
  if (type === 'ContentRelease') return 'Content release';
  if (type === 'LiveSession') return 'Live class';
  if (type === 'AssessmentWindow') return 'Assessment';
  return 'Milestone';
}
