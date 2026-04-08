'use server';

import {
  createServerClient,
  type LearningCoursesCreateProgram,
  type LearningCoursesCreateProgramContent,
  type LearningCoursesProgram,
  type LearningCoursesProgramContent,
  type LearningCoursesProgramContentType,
  type LearningCoursesVisibility,
  type LearningCoursesUpdateProgramContent,
  type LearningAssessmentsAssessment,
  type LearningAssessmentsCreateAssessmentInput,
  type LearningAssessmentsUpdateAssessmentInput,
  type LearningAssessmentsAssessmentType,
} from '@game-guild/client';
import { getToken } from '@/auth';
import { revalidatePath } from 'next/cache';

type ActionResult<T> = { success: true; data: T } | { success: false; error: string };

function getApiClient() {
  const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5295';
  return createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
  });
}

function extractError(err: unknown): string {
  const e = err as { status?: number; message?: string; detail?: string } | undefined;
  return e?.detail || e?.message || 'An unexpected error occurred.';
}

// ── Content actions ──

export interface AddContentInput {
  courseId: string;
  parentId?: string;
  title: string;
  description?: string;
  type: LearningCoursesProgramContentType;
  sortOrder?: number;
}

export async function addContent(input: AddContentInput): Promise<ActionResult<{ id: string }>> {
  const { courseId, parentId, title, type, description, sortOrder } = input;

  if (!title || title.trim().length < 1) {
    return { success: false, error: 'Title is required.' };
  }

  try {
    const client = getApiClient();
    const contentBody: LearningCoursesCreateProgramContent = {
      programId: courseId,
      title: title.trim(),
      description: (description ?? '').trim(),
      type,
      sortOrder: sortOrder ?? 0,
      isRequired: true,
      visibility: 'Public',
      ...(parentId ? { parentId } : {}),
    };

    const result = await client.request<LearningCoursesProgramContent>({
      method: 'POST',
      path: `/v1/courses/${courseId}/content`,
      body: contentBody,
      requiresAuth: true,
    });

    if (result.ok) {
      revalidatePath(`/dashboard/learning/courses/${courseId}`);
      return { success: true, data: { id: result.data.id! } };
    }

    return { success: false, error: extractError(result.error) };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

export async function deleteContent(courseId: string, contentId: string): Promise<ActionResult<null>> {
  try {
    const client = getApiClient();
    const result = await client.request<void>({
      method: 'DELETE',
      path: `/v1/courses/${courseId}/content/${contentId}`,
      requiresAuth: true,
    });

    if (result.ok) {
      revalidatePath(`/dashboard/learning/courses/${courseId}`);
      return { success: true, data: null };
    }

    return { success: false, error: extractError(result.error) };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

export interface UpdateContentInput {
  courseId: string;
  contentId: string;
  title?: string;
  description?: string;
  type?: LearningCoursesProgramContentType;
  body?: string;
  sortOrder?: number;
  isRequired?: boolean;
  estimatedMinutes?: number;
  visibility?: string;
}

export async function updateContent(input: UpdateContentInput): Promise<ActionResult<null>> {
  const { courseId, contentId, ...fields } = input;

  try {
    const client = getApiClient();
    const result = await client.request<LearningCoursesProgramContent>({
      method: 'PUT',
      path: `/v1/courses/${courseId}/content/${contentId}`,
      body: { id: contentId, ...fields },
      requiresAuth: true,
    });

    if (result.ok) {
      revalidatePath(`/dashboard/learning/courses/${courseId}`);
      return { success: true, data: null };
    }

    return { success: false, error: extractError(result.error) };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

export async function reorderContent(courseId: string, contentIds: string[]): Promise<ActionResult<null>> {
  try {
    const client = getApiClient();
    const result = await client.request<void>({
      method: 'POST',
      path: `/v1/courses/${courseId}/content:reorder`,
      body: { contentIds },
      requiresAuth: true,
    });

    if (result.ok) {
      revalidatePath(`/dashboard/learning/courses/${courseId}`);
      return { success: true, data: null };
    }

    return { success: false, error: extractError(result.error) };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

// ── Course CRUD actions ──

export interface CreateCourseInput {
  title: string;
  description: string;
  slug: string;
}

export async function createCourse(input: CreateCourseInput): Promise<ActionResult<{ id: string }>> {
  const { title, description, slug } = input;

  if (!title || title.trim().length < 3) {
    return { success: false, error: 'Title must be at least 3 characters.' };
  }
  if (!description || description.trim().length < 10) {
    return { success: false, error: 'Description must be at least 10 characters.' };
  }
  if (!slug || slug.trim().length < 1) {
    return { success: false, error: 'Slug is required.' };
  }

  try {
    const client = getApiClient();
    const result = await client.request<LearningCoursesProgram>({
      method: 'POST',
      path: '/v1/courses',
      body: {
        title: title.trim(),
        description: description.trim(),
        slug: slug.trim(),
      } satisfies LearningCoursesCreateProgram,
      requiresAuth: true,
    });

    if (result.ok) {
      revalidatePath('/dashboard/learning/courses');
      return { success: true, data: { id: result.data.id! } };
    }

    return { success: false, error: extractError(result.error) };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

export interface UpdateCourseInput {
  courseId: string;
  title?: string;
  description?: string;
  slug?: string;
  thumbnail?: string;
  videoShowcaseUrl?: string;
  estimatedHours?: number;
  visibility?: string;
  category?: string;
  difficulty?: string;
  skillsRequired?: string;
  skillsProvided?: string;
  enrollmentStatus?: string;
  maxEnrollments?: number;
  enrollmentDeadline?: string;
}

export async function updateCourse(input: UpdateCourseInput): Promise<ActionResult<null>> {
  const { courseId, ...fields } = input;

  try {
    const client = getApiClient();
    const result = await client.request<LearningCoursesProgram>({
      method: 'PUT',
      path: `/v1/courses/${courseId}`,
      body: fields,
      requiresAuth: true,
    });

    if (result.ok) {
      revalidatePath(`/dashboard/learning/courses/${courseId}`);
      revalidatePath('/dashboard/learning/courses');
      return { success: true, data: null };
    }

    return { success: false, error: extractError(result.error) };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

export async function publishCourse(courseId: string): Promise<ActionResult<null>> {
  try {
    const client = getApiClient();
    const result = await client.request<LearningCoursesProgram>({
      method: 'POST',
      path: `/v1/courses/${courseId}:publish`,
      requiresAuth: true,
    });

    if (result.ok) {
      revalidatePath(`/dashboard/learning/courses/${courseId}`);
      revalidatePath('/dashboard/learning/courses');
      return { success: true, data: null };
    }

    return { success: false, error: extractError(result.error) };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

export async function unpublishCourse(courseId: string): Promise<ActionResult<null>> {
  try {
    const client = getApiClient();
    const result = await client.request<LearningCoursesProgram>({
      method: 'POST',
      path: `/v1/courses/${courseId}:unpublish`,
      requiresAuth: true,
    });

    if (result.ok) {
      revalidatePath(`/dashboard/learning/courses/${courseId}`);
      revalidatePath('/dashboard/learning/courses');
      return { success: true, data: null };
    }

    return { success: false, error: extractError(result.error) };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

export async function archiveCourse(courseId: string): Promise<ActionResult<null>> {
  try {
    const client = getApiClient();
    const result = await client.request<LearningCoursesProgram>({
      method: 'POST',
      path: `/v1/courses/${courseId}:archive`,
      requiresAuth: true,
    });

    if (result.ok) {
      revalidatePath(`/dashboard/learning/courses/${courseId}`);
      revalidatePath('/dashboard/learning/courses');
      return { success: true, data: null };
    }

    return { success: false, error: extractError(result.error) };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

export async function deleteCourse(courseId: string): Promise<ActionResult<null>> {
  try {
    const client = getApiClient();
    const result = await client.request<void>({
      method: 'DELETE',
      path: `/v1/courses/${courseId}`,
      requiresAuth: true,
    });

    if (result.ok) {
      revalidatePath('/dashboard/learning/courses');
      return { success: true, data: null };
    }

    return { success: false, error: extractError(result.error) };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

export async function cloneCourse(courseId: string, newTitle: string): Promise<ActionResult<{ id: string }>> {
  try {
    const client = getApiClient();
    const result = await client.request<LearningCoursesProgram>({
      method: 'POST',
      path: `/v1/courses/${courseId}:clone`,
      body: { newTitle },
      requiresAuth: true,
    });

    if (result.ok) {
      revalidatePath('/dashboard/learning/courses');
      return { success: true, data: { id: result.data.id! } };
    }

    return { success: false, error: extractError(result.error) };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

// ── Assessment actions ──

export interface CreateAssessmentInput {
  courseId: string;
  title: string;
  description?: string;
  type: LearningAssessmentsAssessmentType;
  maxScore?: number;
  passingScore?: number;
  timeLimitMinutes?: number;
  maxAttempts?: number;
  isRequired?: boolean;
  availableFrom?: string;
  availableUntil?: string;
}

export async function createAssessment(input: CreateAssessmentInput): Promise<ActionResult<{ id: string }>> {
  const { courseId, title, ...rest } = input;

  if (!title || title.trim().length < 1) {
    return { success: false, error: 'Title is required.' };
  }

  try {
    const client = getApiClient();
    const body: LearningAssessmentsCreateAssessmentInput = {
      courseId,
      title: title.trim(),
      description: rest.description?.trim() ?? null,
      type: rest.type,
      maxScore: rest.maxScore ?? 100,
      passingScore: rest.passingScore ?? 70,
      timeLimitMinutes: rest.timeLimitMinutes ?? null,
      maxAttempts: rest.maxAttempts ?? null,
      isRequired: rest.isRequired ?? true,
      availableFrom: rest.availableFrom ?? null,
      availableUntil: rest.availableUntil ?? null,
    };

    const result = await client.request<LearningAssessmentsAssessment>({
      method: 'POST',
      path: '/v1/assessments',
      body,
      requiresAuth: true,
    });

    if (result.ok) {
      revalidatePath(`/dashboard/learning/courses/${courseId}`);
      return { success: true, data: { id: result.data.id! } };
    }

    return { success: false, error: extractError(result.error) };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

export interface UpdateAssessmentInput {
  courseId: string;
  assessmentId: string;
  title?: string;
  description?: string;
  maxScore?: number;
  passingScore?: number;
  timeLimitMinutes?: number | null;
  maxAttempts?: number | null;
  isRequired?: boolean;
  availableFrom?: string | null;
  availableUntil?: string | null;
  contentId?: string | null;
  clearContentId?: boolean;
}

export async function updateAssessment(input: UpdateAssessmentInput): Promise<ActionResult<null>> {
  const { courseId, assessmentId, ...fields } = input;

  try {
    const client = getApiClient();
    const body: LearningAssessmentsUpdateAssessmentInput = {
      title: fields.title?.trim() ?? null,
      description: fields.description?.trim() ?? null,
      maxScore: fields.maxScore ?? null,
      passingScore: fields.passingScore ?? null,
      timeLimitMinutes: fields.timeLimitMinutes ?? null,
      maxAttempts: fields.maxAttempts ?? null,
      isRequired: fields.isRequired ?? null,
      availableFrom: fields.availableFrom ?? null,
      availableUntil: fields.availableUntil ?? null,
      contentId: fields.contentId ?? null,
      clearContentId: fields.clearContentId ?? false,
    } as any;

    const result = await client.request<void>({
      method: 'PUT',
      path: `/v1/assessments/${assessmentId}`,
      body,
      requiresAuth: true,
    });

    if (result.ok) {
      revalidatePath(`/dashboard/learning/courses/${courseId}`);
      return { success: true, data: null };
    }

    return { success: false, error: extractError(result.error) };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

export async function deleteAssessment(courseId: string, assessmentId: string): Promise<ActionResult<null>> {
  try {
    const client = getApiClient();
    const result = await client.request<void>({
      method: 'DELETE',
      path: `/v1/assessments/${assessmentId}`,
      requiresAuth: true,
    });

    if (result.ok) {
      revalidatePath(`/dashboard/learning/courses/${courseId}`);
      return { success: true, data: null };
    }

    return { success: false, error: extractError(result.error) };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

// =============================================================================
// SERVER-SIDE DATA FETCHING ACTIONS
// =============================================================================
// These wrap query functions so 'use client' components can fetch data via RPC
// instead of dynamic-importing server-only modules (which breaks Turbopack).
// =============================================================================

import type { CourseDetails } from '@/lib/learning/types';

/**
 * Fetch course details. Safe to call from client components via server action RPC.
 */
export async function fetchCourse(courseId: string): Promise<CourseDetails | null> {
  const { getCourse } = await import('@/lib/learning/queries/course');
  return getCourse(courseId);
}
