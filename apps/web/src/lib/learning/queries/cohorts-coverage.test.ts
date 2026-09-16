import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  createServerClient: vi.fn(), getToken: vi.fn(), resolveCourseId: vi.fn(),
  getCourseCohorts: vi.fn(), getCohort: vi.fn(), getEnrollments: vi.fn(), getSchedule: vi.fn(), getCalendar: vi.fn(),
}));
vi.mock('react', () => ({ cache: <T extends (...args: never[]) => unknown>(callback: T) => callback }));
vi.mock('@/auth', () => ({ getToken: mocks.getToken }));
vi.mock('./course', () => ({ resolveCourseId: mocks.resolveCourseId }));
vi.mock('@game-guild/client', () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    LearningCohortsModule: class {
      getApiCohortsCourse = mocks.getCourseCohorts;
      getApiCohorts = mocks.getCohort;
    },
    LearningCohortsSchedulesModule: class {
      getCoursesCohortsSchedule = mocks.getSchedule;
      getCoursesCohortsCalendar = mocks.getCalendar;
    },
    LearningEnrollmentsModule: class {
      getApiLearningEnrollmentsCourses = mocks.getEnrollments;
    },
  },
}));

import { getCohort, getCohortSchedule, getCourseCohortCalendar, getCourseCohorts, mapCohort } from './cohorts';

function cohort(overrides: Record<string, unknown> = {}) {
  return {
    id: 'cohort-1', courseId: 'course-1', name: ' Evening ', description: ' Description ',
    instructorId: 'instructor-1', startDate: '2026-08-01', endDate: '2026-12-01',
    meetingSchedule: ' Tue/Thu ', currentEnrollmentCount: 8, maxCapacity: 24,
    nextMeetingAt: '2026-09-16', conflictCount: 2, status: 'Scheduled', isOpen: true,
    schedule: {
      version: 2, timezoneId: ' America/Sao_Paulo ', meetingDays: ['Tuesday', 'Thursday'],
      meetingStartTime: '19:00', pacingMode: 'Weekly', releasePolicy: 'Timed', itemCount: 10,
    },
    createdAt: '2026-07-01', ...overrides,
  } as never;
}

describe('cohort query coverage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubEnv('API_URL', 'https://api.gameguild.test');
    vi.stubEnv('NEXT_PUBLIC_API_URL', 'https://public-api.gameguild.test');
    mocks.createServerClient.mockReturnValue({});
    mocks.getToken.mockResolvedValue('access-token');
    mocks.resolveCourseId.mockResolvedValue('course-1');
  });

  afterEach(() => vi.unstubAllEnvs());

  it('maps operational values and every cohort status', () => {
    expect(mapCohort(cohort())).toMatchObject({
      name: 'Evening', description: 'Description', instructor: { id: 'instructor-1', name: null },
      meetingPattern: 'Tue/Thu', status: 'scheduled', schedule: { timezoneId: 'America/Sao_Paulo' },
    });
    expect(mapCohort(cohort({ status: 'Active' })).status).toBe('active');
    expect(mapCohort(cohort({ status: 'Completed' })).status).toBe('completed');
    expect(mapCohort(cohort({ status: 'Cancelled' })).status).toBe('cancelled');
  });

  it('maps all missing cohort and schedule values to stable defaults', () => {
    expect(mapCohort(cohort({
      id: null, courseId: null, name: '   ', description: ' ', instructorId: null,
      startDate: null, endDate: null, meetingSchedule: ' ', currentEnrollmentCount: null,
      maxCapacity: null, nextMeetingAt: null, conflictCount: null, status: null, isOpen: null,
      schedule: {
        version: null, timezoneId: ' ', meetingDays: null, meetingStartTime: null,
        pacingMode: null, releasePolicy: null, itemCount: null,
      }, createdAt: null,
    }))).toEqual({
      id: '', courseId: '', name: 'Untitled class', description: '', instructor: null,
      period: { startsAt: '', endsAt: '' }, meetingPattern: null,
      enrollment: { current: 0, capacity: null }, nextMeetingAt: null, conflictCount: 0,
      status: 'scheduled', isOpen: false,
      schedule: {
        version: 0, timezoneId: 'UTC', meetingDays: [], meetingStartTime: null,
        pacingMode: null, releasePolicy: null, itemCount: 0,
      }, createdAt: '',
    });
    expect(mapCohort(cohort({ schedule: null })).schedule).toBeNull();
  });

  it('collects and counts course cohorts', async () => {
    mocks.getCourseCohorts.mockResolvedValueOnce({ ok: true, data: [
      cohort(), cohort({ id: 'active', status: 'Active' }), cohort({ id: 'done', status: 'Completed' }), cohort({ id: 'cancelled', status: 'Cancelled' }),
    ] });
    await expect(getCourseCohorts('course-slug')).resolves.toMatchObject({
      total: 4, scheduledCount: 1, activeCount: 1, completedCount: 1,
    });
    expect(mocks.getCourseCohorts).toHaveBeenCalledWith('course-1');

    mocks.getCourseCohorts.mockResolvedValueOnce({ ok: false, error: {} });
    await expect(getCourseCohorts('course-slug')).resolves.toEqual({
      cohorts: [], total: 0, scheduledCount: 0, activeCount: 0, completedCount: 0,
    });
  });

  it('loads a cohort and maps only its attendees', async () => {
    mocks.getCohort.mockResolvedValue({ ok: true, data: cohort() });
    mocks.getEnrollments.mockResolvedValue({ ok: true, data: [
      { id: 'e-1', userId: 'u-1', cohortId: 'cohort-1', status: 'Paused', progress: -5, enrolledAt: '2026-01-01', completedAt: null, lastActivityAt: null },
      { id: 'e-2', userId: 'u-2', cohortId: 'cohort-1', status: 'Completed', progress: 49.6 },
      { id: 'e-3', userId: 'u-3', cohortId: 'cohort-1', status: 'Dropped', progress: 110 },
      { id: 'e-4', userId: 'u-4', cohortId: 'cohort-1', status: 'Expired', progress: null },
      { id: null, userId: null, cohortId: 'cohort-1', status: 'Active', progress: 1.2 },
      { id: 'other', userId: 'u-x', cohortId: 'other-cohort', status: 'Active', progress: 10 },
    ] });

    const result = await getCohort('cohort-1');
    expect(result?.attendees).toEqual([
      { id: 'e-1', userId: 'u-1', status: 'paused', progress: 0, enrolledAt: '2026-01-01', completedAt: null, lastActivityAt: null },
      { id: 'e-2', userId: 'u-2', status: 'completed', progress: 50, enrolledAt: '', completedAt: null, lastActivityAt: null },
      { id: 'e-3', userId: 'u-3', status: 'dropped', progress: 100, enrolledAt: '', completedAt: null, lastActivityAt: null },
      { id: 'e-4', userId: 'u-4', status: 'expired', progress: 0, enrolledAt: '', completedAt: null, lastActivityAt: null },
      { id: 'enrollment-4', userId: 'unknown-user', status: 'active', progress: 1, enrolledAt: '', completedAt: null, lastActivityAt: null },
    ]);
  });

  it('handles missing cohorts and failed or absent enrollments', async () => {
    mocks.getCohort.mockResolvedValueOnce({ ok: false, error: {} });
    await expect(getCohort('missing')).resolves.toBeNull();

    mocks.getCohort.mockResolvedValue({ ok: true, data: cohort() });
    mocks.getEnrollments.mockResolvedValueOnce({ ok: false, error: {} });
    await expect(getCohort('cohort-1')).resolves.toMatchObject({ attendees: [] });
    mocks.getEnrollments.mockResolvedValueOnce({ ok: true, data: null });
    await expect(getCohort('cohort-1')).resolves.toMatchObject({ attendees: [] });
  });

  it('loads schedules and calendars with success and failure semantics', async () => {
    const schedule = { version: 1 };
    mocks.getSchedule.mockResolvedValueOnce({ ok: true, data: schedule }).mockResolvedValueOnce({ ok: false, error: {} });
    await expect(getCohortSchedule('course-slug', 'cohort-1')).resolves.toEqual(schedule);
    await expect(getCohortSchedule('course-slug', 'cohort-1')).resolves.toBeNull();
    expect(mocks.getSchedule).toHaveBeenCalledWith('course-1', 'cohort-1');

    const calendar = { items: [] };
    const query = { cohortId: 'cohort-1', from: '2026-01-01', to: '2026-02-01' };
    mocks.getCalendar.mockResolvedValueOnce({ ok: true, data: calendar }).mockResolvedValueOnce({ ok: false, error: {} });
    await expect(getCourseCohortCalendar('course-slug', query)).resolves.toEqual(calendar);
    await expect(getCourseCohortCalendar('course-slug')).resolves.toBeNull();
    expect(mocks.getCalendar).toHaveBeenNthCalledWith(1, 'course-1', query);
    expect(mocks.getCalendar).toHaveBeenNthCalledWith(2, 'course-1', undefined);
  });

  it('uses authenticated API URL fallbacks', async () => {
    mocks.getCourseCohorts.mockResolvedValue({ ok: true, data: [] });
    await getCourseCohorts('course-1');
    expect(mocks.createServerClient).toHaveBeenCalledWith({
      baseUrl: 'https://api.gameguild.test', auth: { getAccessToken: expect.any(Function) },
    });
    await expect(mocks.createServerClient.mock.calls[0]![0].auth.getAccessToken()).resolves.toBe('access-token');

    vi.stubEnv('API_URL', '');
    await getCourseCohorts('course-1');
    expect(mocks.createServerClient).toHaveBeenLastCalledWith(expect.objectContaining({ baseUrl: 'https://public-api.gameguild.test' }));
    vi.stubEnv('NEXT_PUBLIC_API_URL', '');
    await getCourseCohorts('course-1');
    expect(mocks.createServerClient).toHaveBeenLastCalledWith(expect.objectContaining({ baseUrl: 'http://localhost:8080' }));
  });
});
