import type {
  LearningCohortsCohortSchedule,
  LearningCohortsCohortScheduleItem,
  LearningCohortsCohortSchedulePreview,
} from '@game-guild/client';

import type { CourseCohortSummary } from '@/lib/learning/queries/cohorts';

export const cohortFixture: CourseCohortSummary = {
  id: 'cohort-1',
  courseId: 'course-1',
  name: '2026.2 - Evening',
  description: 'Evening delivery for the advanced track.',
  instructor: null,
  period: { startsAt: '2026-08-12T00:00:00Z', endsAt: '2026-12-18T23:59:59Z' },
  meetingPattern: 'Tue/Thu - 19:00',
  enrollment: { current: 12, capacity: 24 },
  nextMeetingAt: '2026-08-12T22:00:00Z',
  conflictCount: 0,
  status: 'active',
  isOpen: true,
  schedule: null,
  createdAt: '2026-07-14T00:00:00Z',
};

export const scheduleItemsFixture: LearningCohortsCohortScheduleItem[] = [
  {
    id: 'release-1',
    programContentId: 'content-1',
    type: 'ContentRelease',
    instructionalWeek: 1,
    sortOrder: 0,
    availableFrom: '2026-08-12T11:00:00Z',
    title: 'Foundations',
    status: 'Published',
  },
  {
    id: 'meeting-1',
    type: 'LiveSession',
    instructionalWeek: 1,
    sortOrder: 1,
    startsAt: '2026-08-12T22:00:00Z',
    endsAt: '2026-08-12T23:30:00Z',
    title: 'Foundations studio',
    location: 'Discord classroom',
    status: 'Scheduled',
  },
  {
    id: 'assessment-1',
    assessmentId: 'quiz-1',
    type: 'AssessmentWindow',
    instructionalWeek: 1,
    sortOrder: 2,
    availableFrom: '2026-08-12T11:00:00Z',
    dueAt: '2026-08-18T23:59:00Z',
    title: 'Foundations quiz',
    status: 'Scheduled',
  },
  {
    id: 'release-2',
    programContentId: 'content-2',
    type: 'ContentRelease',
    instructionalWeek: 2,
    sortOrder: 3,
    availableFrom: '2026-08-19T11:00:00Z',
    title: 'Decision systems',
    status: 'Scheduled',
  },
];

export const scheduleFixture: LearningCohortsCohortSchedule = {
  id: 'schedule-1',
  cohortId: 'cohort-1',
  version: 3,
  timezoneId: 'America/Sao_Paulo',
  meetingDays: ['Tuesday', 'Thursday'],
  meetingStartTime: '19:00:00',
  meetingDurationMinutes: 90,
  pacingMode: 'OneModulePerWeek',
  unitsPerPeriod: 1,
  releasePolicy: 'Weekly',
  items: scheduleItemsFixture,
  unscheduledContentIds: [],
};

export function previewFixture(
  overrides: Partial<LearningCohortsCohortSchedulePreview> = {},
): LearningCohortsCohortSchedulePreview {
  return {
    items: scheduleItemsFixture.map(({ id: _id, status: _status, visibilityOverride: _visibility, ...item }) => item),
    conflicts: [],
    calculatedEndDate: '2026-08-19',
    hasBlockingConflicts: false,
    ...overrides,
  };
}
