import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  createServerClient: vi.fn(),
  postApiCohorts: vi.fn(),
  putApiCohorts: vi.fn(),
  openCohort: vi.fn(),
  closeCohort: vi.fn(),
  completeCohort: vi.fn(),
  cancelCohort: vi.fn(),
  deleteCohort: vi.fn(),
  postSchedulePreview: vi.fn(),
  putSchedule: vi.fn(),
  patchScheduleItem: vi.fn(),
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
      putApiCohorts = mocks.putApiCohorts;
      postApiCohortsOpen = mocks.openCohort;
      postApiCohortsClose = mocks.closeCohort;
      postApiCohortsComplete = mocks.completeCohort;
      postApiCohortsCancel = mocks.cancelCohort;
      deleteApiCohorts = mocks.deleteCohort;
    },
    LearningCohortsSchedulesModule: class {
      postCoursesCohortsSchedulePreview = mocks.postSchedulePreview;
      putCoursesCohortsSchedule = mocks.putSchedule;
      patchCoursesCohortsScheduleItems = mocks.patchScheduleItem;
      postCoursesCohortsScheduleItemsShift = mocks.shiftScheduleItem;
    },
  },
}));

import {
  applyCohortSchedule,
  createCohort,
  deleteCohort,
  previewCohortSchedule,
  shiftCohortScheduleItem,
  updateCohort,
  updateCohortScheduleItem,
  updateCohortStatus,
} from './cohorts';

const validCreateInput = {
  courseId: 'course-slug',
  name: '2026.2 - Evening',
  startDate: '2026-08-12T00:00:00Z',
  endDate: '2026-12-18T00:00:00Z',
  maxCapacity: 24,
};

describe('cohort actions', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubEnv('API_URL', '');
    vi.stubEnv('NEXT_PUBLIC_API_URL', '');
    mocks.createServerClient.mockReturnValue({});
    mocks.getToken.mockResolvedValue('access-token');
    mocks.resolveCourseId.mockImplementation(async (value: string) => value);
  });

  it('creates a cohort with normalized optional values and revalidates its pages', async () => {
    mocks.resolveCourseId.mockResolvedValue('course-id');
    mocks.postApiCohorts.mockResolvedValue({ ok: true, data: { id: 'cohort-1' } });

    const result = await createCohort({
      ...validCreateInput,
      description: '  Evening class  ',
      instructorId: ' instructor-1 ',
      meetingSchedule: ' Tue/Thu - 19:00 ',
    });

    expect(result).toEqual({ success: true, data: { id: 'cohort-1' } });
    expect(mocks.postApiCohorts).toHaveBeenCalledWith({
      courseId: 'course-id',
      name: '2026.2 - Evening',
      description: 'Evening class',
      startDate: '2026-08-12T00:00:00.000Z',
      endDate: '2026-12-18T00:00:00.000Z',
      maxCapacity: 24,
      instructorId: 'instructor-1',
      meetingSchedule: 'Tue/Thu - 19:00',
    });
    expect(mocks.revalidatePath).toHaveBeenCalledTimes(4);
  });

  it('uses the configured API URL and authenticated token provider', async () => {
    vi.stubEnv('API_URL', 'https://internal.example');
    vi.stubEnv('NEXT_PUBLIC_API_URL', 'https://public.example');
    mocks.postApiCohorts.mockResolvedValue({ ok: true, data: { id: null } });

    const result = await createCohort({ ...validCreateInput, description: ' ', instructorId: ' ', meetingSchedule: ' ' });

    expect(result).toEqual({ success: true, data: { id: '' } });
    const options = mocks.createServerClient.mock.calls[0]?.[0];
    expect(options.baseUrl).toBe('https://internal.example');
    await expect(options.auth.getAccessToken()).resolves.toBe('access-token');
    expect(mocks.postApiCohorts).toHaveBeenCalledWith(expect.objectContaining({
      description: null,
      instructorId: null,
      meetingSchedule: null,
    }));
  });

  it('falls back to the public API URL when the server URL is absent', async () => {
    vi.stubEnv('NEXT_PUBLIC_API_URL', 'https://public.example');
    mocks.postApiCohorts.mockResolvedValue({ ok: true, data: { id: 'cohort-1' } });
    await createCohort(validCreateInput);
    expect(mocks.createServerClient).toHaveBeenCalledWith(expect.objectContaining({ baseUrl: 'https://public.example' }));
  });

  it.each([
    [{ ...validCreateInput, name: ' x ' }, 'Class name must be at least 3 characters.'],
    [{ ...validCreateInput, startDate: '' }, 'Start and end date are required.'],
    [{ ...validCreateInput, endDate: '' }, 'Start and end date are required.'],
    [{ ...validCreateInput, startDate: 'invalid' }, 'Start and end date must be valid dates.'],
    [{ ...validCreateInput, endDate: 'invalid' }, 'Start and end date must be valid dates.'],
    [{ ...validCreateInput, endDate: validCreateInput.startDate }, 'End date must be after start date.'],
    [{ ...validCreateInput, maxCapacity: 1.5 }, 'Capacity must be at least 1.'],
    [{ ...validCreateInput, maxCapacity: 0 }, 'Capacity must be at least 1.'],
  ])('rejects invalid cohort creation input', async (input, error) => {
    await expect(createCohort(input)).resolves.toEqual({ success: false, error });
    expect(mocks.postApiCohorts).not.toHaveBeenCalled();
  });

  it.each([
    [{ ok: false, error: { detail: 'Detailed failure', message: 'Ignored' } }, 'Detailed failure'],
    [{ ok: false, error: { message: 'Message failure' } }, 'Message failure'],
    [{ ok: false, error: {} }, 'The operation could not be completed.'],
  ])('returns normalized API errors while creating a cohort', async (response, error) => {
    mocks.postApiCohorts.mockResolvedValue(response);
    await expect(createCohort(validCreateInput)).resolves.toEqual({ success: false, error });
  });

  it('returns thrown creation errors', async () => {
    mocks.resolveCourseId.mockRejectedValue(new Error('Resolution failed'));
    await expect(createCohort(validCreateInput)).resolves.toEqual({ success: false, error: 'Resolution failed' });
  });

  it('updates every supported cohort field and revalidates the cohort', async () => {
    mocks.putApiCohorts.mockResolvedValue({ ok: true, data: {} });
    const result = await updateCohort({
      courseId: 'course-slug',
      cohortId: 'cohort-1',
      name: ' Updated ',
      description: ' Description ',
      startDate: '2026-08-12T00:00:00Z',
      endDate: '2026-12-18T00:00:00Z',
      maxCapacity: 30,
      instructorId: ' instructor-2 ',
      meetingSchedule: ' Friday ',
    });

    expect(result).toEqual({ success: true, data: null });
    expect(mocks.putApiCohorts).toHaveBeenCalledWith('cohort-1', {
      name: 'Updated',
      description: 'Description',
      startDate: '2026-08-12T00:00:00.000Z',
      endDate: '2026-12-18T00:00:00.000Z',
      maxCapacity: 30,
      instructorId: 'instructor-2',
      meetingSchedule: 'Friday',
    });
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/workspace/learning/courses/course-slug/classes/cohort-1');
  });

  it('uses null values for omitted cohort updates', async () => {
    mocks.putApiCohorts.mockResolvedValue({ ok: true, data: {} });
    await updateCohort({ courseId: 'course-slug', cohortId: 'cohort-1' });
    expect(mocks.putApiCohorts).toHaveBeenCalledWith('cohort-1', {
      name: null,
      description: null,
      startDate: null,
      endDate: null,
      maxCapacity: null,
      instructorId: null,
      meetingSchedule: null,
    });
  });

  it.each([
    [{ startDate: validCreateInput.startDate }, 'Start and end date are required.'],
    [{ endDate: validCreateInput.endDate }, 'Start and end date are required.'],
    [{ maxCapacity: 1.5 }, 'Capacity must be at least 1.'],
    [{ maxCapacity: 0 }, 'Capacity must be at least 1.'],
  ])('rejects invalid cohort updates', async (changes, error) => {
    const result = await updateCohort({ courseId: 'course-slug', cohortId: 'cohort-1', ...changes });
    expect(result).toEqual({ success: false, error });
    expect(mocks.putApiCohorts).not.toHaveBeenCalled();
  });

  it('returns update API failures and thrown errors', async () => {
    mocks.putApiCohorts.mockResolvedValueOnce({ ok: false, error: { message: 'Update failed' } });
    await expect(updateCohort({ courseId: 'course-slug', cohortId: 'cohort-1' })).resolves.toEqual({ success: false, error: 'Update failed' });
    mocks.putApiCohorts.mockRejectedValueOnce(new Error('Network failed'));
    await expect(updateCohort({ courseId: 'course-slug', cohortId: 'cohort-1' })).resolves.toEqual({ success: false, error: 'Network failed' });
  });

  it.each([
    ['open', 'openCohort'],
    ['close', 'closeCohort'],
    ['complete', 'completeCohort'],
    ['cancel', 'cancelCohort'],
  ] as const)('updates cohort status with the %s endpoint', async (action, mockName) => {
    mocks[mockName].mockResolvedValue({ ok: true, data: {} });
    await expect(updateCohortStatus('course-slug', 'cohort-1', action)).resolves.toEqual({ success: true, data: null });
    expect(mocks[mockName]).toHaveBeenCalledWith('cohort-1');
  });

  it('returns status API failures and thrown errors', async () => {
    mocks.openCohort.mockResolvedValueOnce({ ok: false, error: { detail: 'Cannot open' } });
    await expect(updateCohortStatus('course-slug', 'cohort-1', 'open')).resolves.toEqual({ success: false, error: 'Cannot open' });
    mocks.closeCohort.mockRejectedValueOnce(new Error('Status network failed'));
    await expect(updateCohortStatus('course-slug', 'cohort-1', 'close')).resolves.toEqual({ success: false, error: 'Status network failed' });
  });

  it('deletes a cohort and revalidates only collection pages', async () => {
    mocks.deleteCohort.mockResolvedValue({ ok: true, data: {} });
    await expect(deleteCohort('course-slug', 'cohort-1')).resolves.toEqual({ success: true, data: null });
    expect(mocks.revalidatePath).toHaveBeenCalledTimes(2);
    expect(mocks.revalidatePath).not.toHaveBeenCalledWith(expect.stringContaining('cohort-1'));
  });

  it('returns delete API failures and thrown errors', async () => {
    mocks.deleteCohort.mockResolvedValueOnce({ ok: false, error: { message: 'Delete denied' } });
    await expect(deleteCohort('course-slug', 'cohort-1')).resolves.toEqual({ success: false, error: 'Delete denied' });
    mocks.deleteCohort.mockRejectedValueOnce(new Error('Delete failed'));
    await expect(deleteCohort('course-slug', 'cohort-1')).resolves.toEqual({ success: false, error: 'Delete failed' });
  });

  it('previews a generated schedule without revalidating persisted UI', async () => {
    const preview = { items: [], conflicts: [], calculatedEndDate: '2026-12-18', hasBlockingConflicts: false };
    mocks.postSchedulePreview.mockResolvedValue({ ok: true, data: preview });
    const rules = { firstInstructionalDate: '2026-08-12', cohortEndDate: '2026-12-18' };
    await expect(previewCohortSchedule('course-slug', 'cohort-1', rules)).resolves.toEqual({ success: true, data: preview });
    expect(mocks.postSchedulePreview).toHaveBeenCalledWith('course-slug', 'cohort-1', rules);
    expect(mocks.revalidatePath).not.toHaveBeenCalled();
  });

  it('returns preview API failures and thrown errors', async () => {
    mocks.postSchedulePreview.mockResolvedValueOnce({ ok: false, error: { message: 'Preview failed' } });
    await expect(previewCohortSchedule('course-slug', 'cohort-1', {})).resolves.toEqual({ success: false, error: 'Preview failed' });
    mocks.resolveCourseId.mockRejectedValueOnce(new Error('Preview resolution failed'));
    await expect(previewCohortSchedule('course-slug', 'cohort-1', {})).resolves.toEqual({ success: false, error: 'Preview resolution failed' });
  });

  it.each([
    ['apply', applyCohortSchedule, 'putSchedule', ['course-slug', 'cohort-1', { expectedVersion: 3 }]],
    ['update', updateCohortScheduleItem, 'patchScheduleItem', ['course-slug', 'cohort-1', 'item-1', { expectedVersion: 3 }]],
    ['shift', shiftCohortScheduleItem, 'shiftScheduleItem', ['course-slug', 'cohort-1', 'item-1', { expectedVersion: 3, days: 7 }]],
  ] as const)('%s schedule mutations handle success, API failure, and exceptions', async (_name, action, mockName, args) => {
    const apiMock = mocks[mockName];
    apiMock.mockResolvedValueOnce({ ok: true, data: { id: 'schedule-1', version: 4 } });
    await expect((action as (...values: readonly unknown[]) => Promise<unknown>)(...args)).resolves.toEqual({ success: true, data: { id: 'schedule-1', version: 4 } });
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/workspace/learning/courses/course-slug/classes/cohort-1/schedule');
    apiMock.mockResolvedValueOnce({ ok: false, error: { message: 'Mutation failed' } });
    await expect((action as (...values: readonly unknown[]) => Promise<unknown>)(...args)).resolves.toEqual({ success: false, error: 'Mutation failed' });
    apiMock.mockRejectedValueOnce(new Error('Mutation exception'));
    await expect((action as (...values: readonly unknown[]) => Promise<unknown>)(...args)).resolves.toEqual({ success: false, error: 'Mutation exception' });
  });
});
