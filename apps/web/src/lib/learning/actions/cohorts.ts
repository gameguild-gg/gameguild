'use server';

import { getToken } from '@/auth';
import {
  createServerClient,
  GeneratedApi,
  type LearningCohortsApplyCohortScheduleInput,
  type LearningCohortsCohort,
  type LearningCohortsCohortSchedule,
  type LearningCohortsCohortSchedulePreview,
  type LearningCohortsCreateCohortInput,
  type LearningCohortsPreviewCohortScheduleInput,
  type LearningCohortsShiftCohortScheduleInput,
  type LearningCohortsUpdateCohortInput,
  type LearningCohortsUpdateCohortScheduleInput,
} from '@game-guild/client';
import { revalidatePath } from 'next/cache';

export type CohortActionResult<T> = { success: true; data: T } | { success: false; error: string };
export type CohortStatusAction = 'open' | 'close' | 'complete' | 'cancel';

export interface CreateCohortInput {
  courseId: string;
  name: string;
  description?: string;
  startDate: string;
  endDate: string;
  maxCapacity: number;
  instructorId?: string;
  meetingSchedule?: string;
}

export interface UpdateCohortInput {
  courseId: string;
  cohortId: string;
  name?: string;
  description?: string;
  startDate?: string;
  endDate?: string;
  maxCapacity?: number;
  instructorId?: string | null;
  meetingSchedule?: string | null;
}

function createCohortModules() {
  const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5295';
  const client = createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
  });

  return {
    cohorts: new GeneratedApi.LearningCohortsModule(client),
    schedules: new GeneratedApi.LearningCohortsSchedulesModule(client),
  };
}

async function resolveCourseMutationId(courseId: string): Promise<string> {
  const { resolveCourseId } = await import('@/lib/learning/queries/course');
  return resolveCourseId(courseId);
}

function errorMessage(error: unknown): string {
  const value = error as { detail?: string; message?: string } | undefined;
  return value?.detail || value?.message || 'The operation could not be completed.';
}

function validatePeriod(startDate: string | undefined, endDate: string | undefined): string | null {
  if (!startDate || !endDate) return 'Start and end date are required.';

  const startsAt = new Date(startDate).getTime();
  const endsAt = new Date(endDate).getTime();
  if (!Number.isFinite(startsAt) || !Number.isFinite(endsAt)) return 'Start and end date must be valid dates.';
  if (endsAt <= startsAt) return 'End date must be after start date.';
  return null;
}

function revalidateCohortPaths(courseIdentifier: string, cohortId?: string) {
  revalidatePath(`/dashboard/learning/courses/${courseIdentifier}/classes`);
  revalidatePath(`/dashboard/learning/courses/${courseIdentifier}/classes/calendar`);
  if (cohortId) {
    revalidatePath(`/dashboard/learning/courses/${courseIdentifier}/classes/${cohortId}`);
    revalidatePath(`/dashboard/learning/courses/${courseIdentifier}/classes/${cohortId}/schedule`);
  }
}

export async function createCohort(input: CreateCohortInput): Promise<CohortActionResult<{ id: string }>> {
  const name = input.name.trim();
  if (name.length < 3) return { success: false, error: 'Class name must be at least 3 characters.' };

  const periodError = validatePeriod(input.startDate, input.endDate);
  if (periodError) return { success: false, error: periodError };
  if (!Number.isInteger(input.maxCapacity) || input.maxCapacity < 1) {
    return { success: false, error: 'Capacity must be at least 1.' };
  }

  try {
    const courseId = await resolveCourseMutationId(input.courseId);
    const request: LearningCohortsCreateCohortInput = {
      courseId,
      name,
      description: input.description?.trim() || null,
      startDate: new Date(input.startDate).toISOString(),
      endDate: new Date(input.endDate).toISOString(),
      maxCapacity: input.maxCapacity,
      instructorId: input.instructorId?.trim() || null,
      meetingSchedule: input.meetingSchedule?.trim() || null,
    };
    const result = await createCohortModules().cohorts.postApiCohorts(request);
    if (!result.ok) return { success: false, error: errorMessage(result.error) };

    revalidateCohortPaths(input.courseId, result.data.id);
    return { success: true, data: { id: result.data.id ?? '' } };
  } catch (error) {
    return { success: false, error: errorMessage(error) };
  }
}

export async function updateCohort(input: UpdateCohortInput): Promise<CohortActionResult<null>> {
  const periodError = input.startDate || input.endDate ? validatePeriod(input.startDate, input.endDate) : null;
  if (periodError) return { success: false, error: periodError };
  if (input.maxCapacity != null && (!Number.isInteger(input.maxCapacity) || input.maxCapacity < 1)) {
    return { success: false, error: 'Capacity must be at least 1.' };
  }

  try {
    const request: LearningCohortsUpdateCohortInput = {
      name: input.name?.trim() || null,
      description: input.description?.trim() || null,
      startDate: input.startDate ? new Date(input.startDate).toISOString() : null,
      endDate: input.endDate ? new Date(input.endDate).toISOString() : null,
      maxCapacity: input.maxCapacity ?? null,
      instructorId: input.instructorId?.trim() || null,
      meetingSchedule: input.meetingSchedule?.trim() || null,
    };
    const result = await createCohortModules().cohorts.putApiCohorts(input.cohortId, request);
    if (!result.ok) return { success: false, error: errorMessage(result.error) };

    revalidateCohortPaths(input.courseId, input.cohortId);
    return { success: true, data: null };
  } catch (error) {
    return { success: false, error: errorMessage(error) };
  }
}

export async function updateCohortStatus(
  courseId: string,
  cohortId: string,
  action: CohortStatusAction,
): Promise<CohortActionResult<null>> {
  try {
    const cohorts = createCohortModules().cohorts;
    let result: Awaited<ReturnType<typeof cohorts.postApiCohortsOpen>>;
    if (action === 'close') result = await cohorts.postApiCohortsClose(cohortId);
    else if (action === 'complete') result = await cohorts.postApiCohortsComplete(cohortId);
    else if (action === 'cancel') result = await cohorts.postApiCohortsCancel(cohortId);
    else result = await cohorts.postApiCohortsOpen(cohortId);

    if (!result.ok) return { success: false, error: errorMessage(result.error) };
    revalidateCohortPaths(courseId, cohortId);
    return { success: true, data: null };
  } catch (error) {
    return { success: false, error: errorMessage(error) };
  }
}

export async function deleteCohort(courseId: string, cohortId: string): Promise<CohortActionResult<null>> {
  try {
    const result = await createCohortModules().cohorts.deleteApiCohorts(cohortId);
    if (!result.ok) return { success: false, error: errorMessage(result.error) };
    revalidateCohortPaths(courseId);
    return { success: true, data: null };
  } catch (error) {
    return { success: false, error: errorMessage(error) };
  }
}

export async function previewCohortSchedule(
  courseIdentifier: string,
  cohortId: string,
  rules: LearningCohortsPreviewCohortScheduleInput,
): Promise<CohortActionResult<LearningCohortsCohortSchedulePreview>> {
  try {
    const courseId = await resolveCourseMutationId(courseIdentifier);
    const result = await createCohortModules().schedules.postCoursesCohortsSchedulePreview(courseId, cohortId, rules);
    return result.ok
      ? { success: true, data: result.data }
      : { success: false, error: errorMessage(result.error) };
  } catch (error) {
    return { success: false, error: errorMessage(error) };
  }
}

export async function applyCohortSchedule(
  courseIdentifier: string,
  cohortId: string,
  input: LearningCohortsApplyCohortScheduleInput,
): Promise<CohortActionResult<LearningCohortsCohortSchedule>> {
  try {
    const courseId = await resolveCourseMutationId(courseIdentifier);
    const result = await createCohortModules().schedules.putCoursesCohortsSchedule(courseId, cohortId, input);
    if (!result.ok) return { success: false, error: errorMessage(result.error) };
    revalidateCohortPaths(courseIdentifier, cohortId);
    return { success: true, data: result.data };
  } catch (error) {
    return { success: false, error: errorMessage(error) };
  }
}

export async function updateCohortScheduleItem(
  courseIdentifier: string,
  cohortId: string,
  itemId: string,
  input: LearningCohortsUpdateCohortScheduleInput,
): Promise<CohortActionResult<LearningCohortsCohortSchedule>> {
  try {
    const courseId = await resolveCourseMutationId(courseIdentifier);
    const result = await createCohortModules().schedules.patchCoursesCohortsScheduleItems(courseId, cohortId, itemId, input);
    if (!result.ok) return { success: false, error: errorMessage(result.error) };
    revalidateCohortPaths(courseIdentifier, cohortId);
    return { success: true, data: result.data };
  } catch (error) {
    return { success: false, error: errorMessage(error) };
  }
}

export async function shiftCohortScheduleItem(
  courseIdentifier: string,
  cohortId: string,
  itemId: string,
  input: LearningCohortsShiftCohortScheduleInput,
): Promise<CohortActionResult<LearningCohortsCohortSchedule>> {
  try {
    const courseId = await resolveCourseMutationId(courseIdentifier);
    const result = await createCohortModules().schedules.postCoursesCohortsScheduleItemsShift(courseId, cohortId, itemId, input);
    if (!result.ok) return { success: false, error: errorMessage(result.error) };
    revalidateCohortPaths(courseIdentifier, cohortId);
    return { success: true, data: result.data };
  } catch (error) {
    return { success: false, error: errorMessage(error) };
  }
}
