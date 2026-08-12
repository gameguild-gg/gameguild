import type {
  TestingLabTestingEventProjection,
  TestingLabTestingEventStatus,
} from '@game-guild/client';

const readOnlyStatuses: TestingLabTestingEventStatus[] = ['Completed', 'Cancelled'];

export function isTestingEventReadOnly(event: TestingLabTestingEventProjection) {
  return readOnlyStatuses.includes(event.status ?? 'Draft');
}

export function formatEventDateTime(value?: string | null) {
  if (!value) return 'Not scheduled';
  const date = new Date(value);
  if (Number.isNaN(date.valueOf())) return 'Not scheduled';
  const formatted = new Intl.DateTimeFormat('en', {
    dateStyle: 'medium',
    timeStyle: 'short',
    timeZone: 'UTC',
  }).format(date);
  return `${formatted} UTC`;
}

export function formatCapacity(current?: number, maximum?: number | null) {
  return maximum ? `${current ?? 0}/${maximum}` : `${current ?? 0}/unlimited`;
}

export function countLabel(value: number, singular: string, plural = `${singular}s`) {
  return `${value} ${value === 1 ? singular : plural}`;
}
