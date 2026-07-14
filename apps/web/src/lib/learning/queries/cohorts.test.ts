import type { LearningCohortsCohort } from '@game-guild/client';
import { describe, expect, it } from 'vitest';

import { mapCohort } from './cohorts';

function cohort(overrides: Partial<LearningCohortsCohort> = {}): LearningCohortsCohort {
  return {
    id: 'cohort-1',
    courseId: 'course-1',
    name: '2026.2 - Morning',
    description: 'Monday and Wednesday morning cohort.',
    startDate: '2026-08-12T00:00:00Z',
    endDate: '2026-12-18T00:00:00Z',
    maxCapacity: 24,
    currentEnrollmentCount: 8,
    status: 'Scheduled',
    isOpen: true,
    instructorId: 'instructor-1',
    meetingSchedule: 'Mon/Wed - 09:00',
    createdAt: '2026-07-14T10:00:00Z',
    nextMeetingAt: '2026-08-12T12:00:00Z',
    conflictCount: 2,
    schedule: {
      version: 3,
      timezoneId: 'America/Sao_Paulo',
      meetingDays: ['Monday', 'Wednesday'],
      meetingStartTime: '09:00:00',
      pacingMode: 'OneModulePerWeek',
      releasePolicy: 'Weekly',
      itemCount: 18,
    },
    ...overrides,
  };
}

describe('cohort query model', () => {
  it('maps a cohort period without deriving a session duration', () => {
    const result = mapCohort(cohort());

    expect(result.period).toEqual({
      startsAt: '2026-08-12T00:00:00Z',
      endsAt: '2026-12-18T00:00:00Z',
    });
    expect(result).not.toHaveProperty('duration');
    expect(result).not.toHaveProperty('scheduledAt');
  });

  it('preserves operational cohort and schedule information', () => {
    const result = mapCohort(cohort());

    expect(result).toMatchObject({
      id: 'cohort-1',
      courseId: 'course-1',
      name: '2026.2 - Morning',
      meetingPattern: 'Mon/Wed - 09:00',
      enrollment: { current: 8, capacity: 24 },
      nextMeetingAt: '2026-08-12T12:00:00Z',
      conflictCount: 2,
      status: 'scheduled',
      isOpen: true,
      schedule: {
        version: 3,
        timezoneId: 'America/Sao_Paulo',
        meetingDays: ['Monday', 'Wednesday'],
        meetingStartTime: '09:00:00',
        pacingMode: 'OneModulePerWeek',
        releasePolicy: 'Weekly',
        itemCount: 18,
      },
    });
  });
});
