import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  createServerClient: vi.fn(),
  postApiCohorts: vi.fn(),
  postSchedulePreview: vi.fn(),
  putSchedule: vi.fn(),
  shiftScheduleItem: vi.fn(),
  revalidatePath: vi.fn(),
  resolveCourseId: vi.fn(),
  getToken: vi.fn(),
}));

vi.mock('@/auth', () => ({ getToken: mocks.getToken }));
vi.mock('next/cache', () => ({ revalidatePath: mocks.revalidatePath }));
vi.mock('@/lib/learning/queries/course', () => ({ resolveCourseId: mocks.resolveCourseId }));

vi.mock('@game-guild/client', () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    LearningCohortsModule: class {
      postApiCohorts = mocks.postApiCohorts;
    },
    LearningCohortsSchedulesModule: class {
      postCoursesCohortsSchedulePreview = mocks.postSchedulePreview;
      putCoursesCohortsSchedule = mocks.putSchedule;
      postCoursesCohortsScheduleItemsShift = mocks.shiftScheduleItem;
    },
  },
}));

import {
  applyCohortSchedule,
  createCohort,
  previewCohortSchedule,
  shiftCohortScheduleItem,
} from './cohorts';

describe('cohort actions', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.createServerClient.mockReturnValue({});
    mocks.getToken.mockResolvedValue('access-token');
    mocks.resolveCourseId.mockImplementation(async (value: string) => value);
  });

  it('creates a cohort as a course period instead of a single meeting', async () => {
    mocks.postApiCohorts.mockResolvedValue({
      ok: true,
      data: { id: 'cohort-1', courseId: 'course-1' },
    });

    const result = await createCohort({
      courseId: 'course-1',
      name: '2026.2 - Evening',
      startDate: '2026-08-12T00:00:00Z',
      endDate: '2026-12-18T00:00:00Z',
      maxCapacity: 24,
      meetingSchedule: 'Tue/Thu - 19:00',
    });

    expect(result).toEqual({ success: true, data: { id: 'cohort-1' } });
    expect(mocks.postApiCohorts).toHaveBeenCalledWith({
      courseId: 'course-1',
      name: '2026.2 - Evening',
      description: null,
      startDate: '2026-08-12T00:00:00.000Z',
      endDate: '2026-12-18T00:00:00.000Z',
      maxCapacity: 24,
      instructorId: null,
      meetingSchedule: 'Tue/Thu - 19:00',
    });
  });

  it('previews a generated schedule without revalidating persisted UI', async () => {
    const preview = { items: [], conflicts: [], calculatedEndDate: '2026-12-18', hasBlockingConflicts: false };
    mocks.postSchedulePreview.mockResolvedValue({ ok: true, data: preview });

    const rules = {
      firstInstructionalDate: '2026-08-12',
      cohortEndDate: '2026-12-18',
      timezoneId: 'America/Sao_Paulo',
      meetingDays: ['Monday', 'Wednesday'] as const,
      meetingStartTime: '09:00:00',
      meetingDurationMinutes: 120,
      pacingMode: 'OneModulePerWeek' as const,
      unitsPerPeriod: 1,
      releasePolicy: 'Weekly' as const,
      skippedDates: [],
      assessmentDueOffsetDays: 2,
    };

    const result = await previewCohortSchedule('course-1', 'cohort-1', rules);

    expect(result).toEqual({ success: true, data: preview });
    expect(mocks.postSchedulePreview).toHaveBeenCalledWith('course-1', 'cohort-1', rules);
    expect(mocks.revalidatePath).not.toHaveBeenCalled();
  });

  it('applies and shifts a versioned cohort schedule', async () => {
    mocks.putSchedule.mockResolvedValue({ ok: true, data: { id: 'schedule-1', version: 4 } });
    mocks.shiftScheduleItem.mockResolvedValue({ ok: true, data: { id: 'schedule-1', version: 5 } });

    const applyResult = await applyCohortSchedule('course-1', 'cohort-1', {
      expectedVersion: 3,
      rules: {
        firstInstructionalDate: '2026-08-12',
        cohortEndDate: '2026-12-18',
      },
      confirmAdvisories: true,
    });
    const shiftResult = await shiftCohortScheduleItem('course-1', 'cohort-1', 'item-1', {
      expectedVersion: 4,
      days: 7,
      scope: 'Following',
    });

    expect(applyResult).toEqual({ success: true, data: { id: 'schedule-1', version: 4 } });
    expect(shiftResult).toEqual({ success: true, data: { id: 'schedule-1', version: 5 } });
    expect(mocks.putSchedule).toHaveBeenCalledWith('course-1', 'cohort-1', expect.objectContaining({ expectedVersion: 3 }));
    expect(mocks.shiftScheduleItem).toHaveBeenCalledWith('course-1', 'cohort-1', 'item-1', {
      expectedVersion: 4,
      days: 7,
      scope: 'Following',
    });
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/workspace/learning/courses/course-1/classes/cohort-1/schedule');
  });
});
