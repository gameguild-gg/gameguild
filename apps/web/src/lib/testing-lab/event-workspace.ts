import type {
  TestingLabTestingEventProjection,
  TestingLabTestingEventStatus,
} from '@game-guild/client';
import { isSupportedTimeZone } from '@/lib/date-time-zone';

const readOnlyStatuses: TestingLabTestingEventStatus[] = ['Completed', 'Cancelled'];

export function isTestingEventReadOnly(event: TestingLabTestingEventProjection) {
  return readOnlyStatuses.includes(event.status ?? 'Draft');
}

export function formatEventDateTime(value?: string | null, timeZone = 'UTC') {
  if (!value) return 'Not scheduled';
  const date = new Date(value);
  if (Number.isNaN(date.valueOf())) return 'Not scheduled';
  timeZone = isSupportedTimeZone(timeZone) ? timeZone : 'UTC';
  const formatted = new Intl.DateTimeFormat('en', {
    dateStyle: 'medium',
    timeStyle: 'short',
    timeZone,
    hourCycle: 'h23',
  }).format(date);
  return `${formatted} ${timeZone}`;
}

export function formatCapacity(current?: number, maximum?: number | null) {
  return maximum ? `${current ?? 0}/${maximum}` : `${current ?? 0}/unlimited`;
}

export function formatEventDateRange(start?: string | null, end?: string | null, timeZone = 'UTC') {
  timeZone = isSupportedTimeZone(timeZone) ? timeZone : 'UTC';
  const startText = formatEventDateTime(start, timeZone);
  const endText = formatEventDateTime(end, timeZone);
  if (startText === 'Not scheduled' || endText === 'Not scheduled') return 'Not scheduled';
  return `${startText.slice(0, -(timeZone.length + 1))} → ${endText.slice(0, -(timeZone.length + 1))} · ${timeZone}`;
}

export function countLabel(value: number, singular: string, plural = `${singular}s`) {
  return `${value} ${value === 1 ? singular : plural}`;
}
