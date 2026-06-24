'use server';

import { getToken } from '@/auth';
import type { LearningCoursesProgramContentType } from '@/lib/learning/types';
import {
  createServerClient,
  GeneratedApi,
  type LearningAssessmentsAssessmentType,
  type LearningAssessmentsCreateAssessmentInput,
  type LearningAssessmentsUpdateAssessmentInput,
  type LearningCoursesMonetization,
  type LearningCoursesCloneProgram,
  type LearningCoursesCreateProgram,
  type LearningCoursesCreateProgramContent,
  type LearningCoursesUpdateProgram,
  type LearningCoursesUpdateProgramContent
} from '@game-guild/client';
import { revalidatePath } from 'next/cache';

type ActionResult<T> = { success: true; data: T } | { success: false; error: string };

function getApiClient() {
  const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5295';
  return createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
  });
}

function createCourseModules() {
  const client = getApiClient();

  return {
    programs: new GeneratedApi.LearningCoursesProgramModule(client),
    content: new GeneratedApi.LearningCoursesProgramcontentModule(client),
    lifecycle: new GeneratedApi.LearningCoursesProgramlifecycleModule(client),
    assessments: new GeneratedApi.LearningAssessmentsModule(client),
  };
}

function extractError(err: unknown): string {
  const e = err as { status?: number; message?: string; detail?: string } | undefined;
  return e?.detail || e?.message || 'An unexpected error occurred.';
}

async function learningApiRequest<T>(path: string, init: RequestInit): Promise<ActionResult<T>> {
  const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5295';
  const token = await getToken();
  const response = await fetch(`${apiUrl}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(init.headers ?? {}),
    },
  });

  if (!response.ok) {
    let error = response.statusText || 'API request failed.';
    try {
      const body = await response.json() as { detail?: string; message?: string; description?: string; code?: string };
      error = body.detail || body.message || body.description || body.code || error;
    } catch {
      // Keep the HTTP status text when the API returns an empty or non-JSON error body.
    }

    return { success: false, error };
  }

  if (response.status === 204) {
    return { success: true, data: null as T };
  }

  const data = await response.json() as T;
  return { success: true, data };
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

    const { content } = createCourseModules();
    const result = await content.postCoursesContent(courseId, contentBody);

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
    const { content } = createCourseModules();
    const result = await content.deleteCoursesContent(courseId, contentId);

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
    const { content } = createCourseModules();
    const body = { id: contentId, ...fields } as LearningCoursesUpdateProgramContent;
    const result = await content.putCoursesContent(courseId, contentId, body);

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
    const { programs } = createCourseModules();
    const result = await programs.postCoursesContentReorder1(courseId, { contentIds });

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

export async function createCourse(input: CreateCourseInput): Promise<ActionResult<{ id: string; slug: string }>> {
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
    const { programs } = createCourseModules();
    const result = await programs.postCourses({
      title: title.trim(),
      description: description.trim(),
      slug: slug.trim(),
    } satisfies LearningCoursesCreateProgram);

    if (result.ok) {
      revalidatePath('/dashboard/learning/courses');
      return { success: true, data: { id: result.data.id!, slug: result.data.slug?.trim() || slug.trim() } };
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
  metadata?: string | null;
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
  maxEnrollments?: number | null;
  enrollmentDeadline?: string | null;
}

export async function updateCourse(input: UpdateCourseInput): Promise<ActionResult<null>> {
  const { courseId, ...fields } = input;

  try {
    const { programs } = createCourseModules();
    const result = await programs.putCourses(courseId, fields as LearningCoursesUpdateProgram);

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
    const { lifecycle } = createCourseModules();
    const result = await lifecycle.postCoursesPublish(courseId);

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
    const { lifecycle } = createCourseModules();
    const result = await lifecycle.postCoursesUnpublish(courseId);

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
    const { lifecycle } = createCourseModules();
    const result = await lifecycle.postCoursesArchive(courseId);

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
    const { programs } = createCourseModules();
    const result = await programs.deleteCourses(courseId);

    if (result.ok) {
      revalidatePath('/dashboard/learning/courses');
      return { success: true, data: null };
    }

    return { success: false, error: extractError(result.error) };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

interface LandingFaqInput {
  question: string;
  answer: string;
  category?: string;
}

interface LandingProjectInput {
  title: string;
  summary: string;
  image?: string;
  skills?: string | string[];
  deliverable: string;
  moduleLabel?: string;
}

function parseMetadata(raw: string | null | undefined): Record<string, unknown> {
  if (!raw) return {};

  try {
    const parsed = JSON.parse(raw) as unknown;
    return parsed && typeof parsed === 'object' && !Array.isArray(parsed) ? parsed as Record<string, unknown> : {};
  } catch {
    return {};
  }
}

function normalizeStringList(value: string | string[] | undefined): string[] {
  if (Array.isArray(value)) {
    return value.map((item) => item.trim()).filter(Boolean);
  }

  if (typeof value === 'string') {
    return value
      .split(/[,;\n]/)
      .map((item) => item.trim())
      .filter(Boolean);
  }

  return [];
}

export async function updateCourseFaq(courseId: string, items: LandingFaqInput[]): Promise<ActionResult<null>> {
  const sanitizedItems = items
    .map((item) => ({
      question: item.question.trim(),
      answer: item.answer.trim(),
      category: item.category?.trim() || 'Course details',
    }))
    .filter((item) => item.question.length > 0 && item.answer.length > 0)
    .slice(0, 12);

  try {
    const course = await fetchCourse(courseId);
    if (!course) return { success: false, error: 'Course not found.' };

    const metadata = parseMetadata(course.metadata);
    metadata.landingFaq = sanitizedItems;

    return updateCourse({
      courseId,
      metadata: JSON.stringify(metadata),
    });
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

export async function updateCourseLandingProjects(courseId: string, items: LandingProjectInput[]): Promise<ActionResult<null>> {
  const sanitizedItems = items
    .map((item, index) => ({
      title: item.title.trim(),
      summary: item.summary.trim(),
      image: item.image?.trim() || null,
      skills: normalizeStringList(item.skills),
      deliverable: item.deliverable.trim(),
      moduleLabel: item.moduleLabel?.trim() || `Project ${String(index + 1).padStart(2, '0')}`,
    }))
    .filter((item) => item.title.length > 0 && item.summary.length > 0 && item.deliverable.length > 0)
    .slice(0, 6);

  try {
    const course = await fetchCourse(courseId);
    if (!course) return { success: false, error: 'Course not found.' };

    const metadata = parseMetadata(course.metadata);
    metadata.landingProjects = sanitizedItems;

    return updateCourse({
      courseId,
      metadata: JSON.stringify(metadata),
    });
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

export interface UpdateCoursePricingInput {
  courseId: string;
  isMonetizationEnabled: boolean;
  price: number;
  currency: string;
  isSubscription: boolean;
  subscriptionDurationDays?: number | null;
}

export async function updateCoursePricing(input: UpdateCoursePricingInput): Promise<ActionResult<null>> {
  try {
    const { programs } = createCourseModules();

    if (!input.isMonetizationEnabled) {
      const result = await programs.postCoursesDisableMonetization(input.courseId);

      if (result.ok) {
        revalidatePath(`/dashboard/learning/courses/${input.courseId}/listing/pricing`);
        revalidatePath(`/dashboard/learning/courses/${input.courseId}/listing`);
        return { success: true, data: null };
      }

      return { success: false, error: extractError(result.error) };
    }

    if (!Number.isFinite(input.price) || input.price < 0) {
      return { success: false, error: 'Price must be zero or greater.' };
    }

    const result = await programs.postCoursesMonetize(input.courseId, {
      price: input.price,
      currency: input.currency.trim().toUpperCase() || 'USD',
      isSubscription: input.isSubscription,
      subscriptionDurationDays: input.isSubscription ? input.subscriptionDurationDays ?? null : null,
    } satisfies LearningCoursesMonetization);

    if (result.ok) {
      revalidatePath(`/dashboard/learning/courses/${input.courseId}/listing/pricing`);
      revalidatePath(`/dashboard/learning/courses/${input.courseId}/listing`);
      return { success: true, data: null };
    }

    return { success: false, error: extractError(result.error) };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

export async function cloneCourse(courseId: string, newTitle: string): Promise<ActionResult<{ id: string }>> {
  try {
    const { programs } = createCourseModules();
    const result = await programs.postCoursesClone(courseId, { newTitle } satisfies LearningCoursesCloneProgram);

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

    const { assessments } = createCourseModules();
    const result = await assessments.postAssessments(body);

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
    };

    const { assessments } = createCourseModules();
    const result = await assessments.putAssessments(assessmentId, body);

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
    const { assessments } = createCourseModules();
    const result = await assessments.deleteAssessments(assessmentId);

    if (result.ok) {
      revalidatePath(`/dashboard/learning/courses/${courseId}`);
      return { success: true, data: null };
    }

    return { success: false, error: extractError(result.error) };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

// ── Certificate template actions ──

export interface CreateCertificateTemplateInput {
  courseId: string;
  name: string;
  templateHtml?: string;
}

interface CertificateTemplateActionDto {
  id: string;
  courseId: string;
  name: string;
}

const defaultCertificateTemplateHtml = `
<section style="font-family: Inter, Arial, sans-serif; padding: 48px; border: 12px solid #111827; text-align: center;">
  <p style="letter-spacing: 0.18em; text-transform: uppercase; color: #6b7280;">Certificate of Completion</p>
  <h1 style="font-size: 42px; margin: 24px 0;">{{recipientName}}</h1>
  <p style="font-size: 18px; color: #374151;">has successfully completed</p>
  <h2 style="font-size: 30px; margin: 18px 0;">{{courseName}}</h2>
  <p style="color: #6b7280;">Issued on {{issuedAt}} - Certificate {{certificateNumber}}</p>
</section>
`.trim();

export async function createCertificateTemplate(input: CreateCertificateTemplateInput): Promise<ActionResult<{ id: string }>> {
  const name = input.name.trim();
  const templateHtml = input.templateHtml?.trim() || defaultCertificateTemplateHtml;

  if (name.length < 3) {
    return { success: false, error: 'Template name must be at least 3 characters.' };
  }

  if (templateHtml.length < 20) {
    return { success: false, error: 'Template HTML must be at least 20 characters.' };
  }

  try {
    const result = await learningApiRequest<CertificateTemplateActionDto>('/api/certificates/templates', {
      method: 'POST',
      body: JSON.stringify({
        courseId: input.courseId,
        name,
        templateHtml,
      }),
    });

    if (!result.success) return result;

    revalidatePath(`/dashboard/learning/courses/${input.courseId}/certificates`);
    return { success: true, data: { id: result.data.id } };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

export async function deleteCertificateTemplate(courseId: string, templateId: string): Promise<ActionResult<null>> {
  try {
    const result = await learningApiRequest<null>(`/api/certificates/templates/${templateId}`, {
      method: 'DELETE',
    });

    if (result.success) {
      revalidatePath(`/dashboard/learning/courses/${courseId}/certificates`);
    }

    return result;
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

// ── Cohort/class actions ──

export interface CreateCourseClassInput {
  courseId: string;
  name: string;
  description?: string;
  startDate: string;
  endDate: string;
  maxCapacity: number;
  instructorId?: string;
  meetingSchedule?: string;
}

export interface UpdateCourseClassInput {
  courseId: string;
  classId: string;
  name?: string;
  description?: string;
  startDate?: string;
  endDate?: string;
  maxCapacity?: number;
  instructorId?: string | null;
  meetingSchedule?: string | null;
}

export type CourseClassStatusAction = 'open' | 'close' | 'complete' | 'cancel';

interface CourseClassActionDto {
  id: string;
  courseId: string;
}

function validateClassWindow(startDate: string | undefined, endDate: string | undefined): string | null {
  if (!startDate || !endDate) return 'Start and end date are required.';

  const startsAt = new Date(startDate).getTime();
  const endsAt = new Date(endDate).getTime();

  if (!Number.isFinite(startsAt) || !Number.isFinite(endsAt)) {
    return 'Start and end date must be valid dates.';
  }

  if (endsAt <= startsAt) {
    return 'End date must be after start date.';
  }

  return null;
}

export async function createCourseClass(input: CreateCourseClassInput): Promise<ActionResult<{ id: string }>> {
  const name = input.name.trim();
  if (name.length < 3) {
    return { success: false, error: 'Class name must be at least 3 characters.' };
  }

  const windowError = validateClassWindow(input.startDate, input.endDate);
  if (windowError) return { success: false, error: windowError };

  if (!Number.isInteger(input.maxCapacity) || input.maxCapacity < 1) {
    return { success: false, error: 'Capacity must be at least 1.' };
  }

  try {
    const result = await learningApiRequest<CourseClassActionDto>('/api/cohorts', {
      method: 'POST',
      body: JSON.stringify({
        courseId: input.courseId,
        name,
        description: input.description?.trim() || null,
        startDate: new Date(input.startDate).toISOString(),
        endDate: new Date(input.endDate).toISOString(),
        maxCapacity: input.maxCapacity,
        instructorId: input.instructorId?.trim() || null,
        meetingSchedule: input.meetingSchedule?.trim() || null,
      }),
    });

    if (!result.success) return result;

    revalidatePath(`/dashboard/learning/courses/${input.courseId}/classes`);
    return { success: true, data: { id: result.data.id } };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

export async function updateCourseClass(input: UpdateCourseClassInput): Promise<ActionResult<null>> {
  const windowError = input.startDate || input.endDate ? validateClassWindow(input.startDate, input.endDate) : null;
  if (windowError) return { success: false, error: windowError };

  if (input.maxCapacity != null && (!Number.isInteger(input.maxCapacity) || input.maxCapacity < 1)) {
    return { success: false, error: 'Capacity must be at least 1.' };
  }

  try {
    const result = await learningApiRequest<CourseClassActionDto>(`/api/cohorts/${input.classId}`, {
      method: 'PUT',
      body: JSON.stringify({
        name: input.name?.trim() || null,
        description: input.description?.trim() || null,
        startDate: input.startDate ? new Date(input.startDate).toISOString() : null,
        endDate: input.endDate ? new Date(input.endDate).toISOString() : null,
        maxCapacity: input.maxCapacity ?? null,
        instructorId: input.instructorId?.trim() || null,
        meetingSchedule: input.meetingSchedule?.trim() || null,
      }),
    });

    if (!result.success) return { success: false, error: result.error };

    revalidatePath(`/dashboard/learning/courses/${input.courseId}/classes`);
    revalidatePath(`/dashboard/learning/courses/${input.courseId}/classes/${input.classId}`);
    return { success: true, data: null };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

export async function updateCourseClassStatus(courseId: string, classId: string, statusAction: CourseClassStatusAction): Promise<ActionResult<null>> {
  try {
    const result = await learningApiRequest<CourseClassActionDto>(`/api/cohorts/${classId}/${statusAction}`, {
      method: 'POST',
      body: JSON.stringify({}),
    });

    if (!result.success) return { success: false, error: result.error };

    revalidatePath(`/dashboard/learning/courses/${courseId}/classes`);
    revalidatePath(`/dashboard/learning/courses/${courseId}/classes/${classId}`);
    return { success: true, data: null };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

export async function deleteCourseClass(courseId: string, classId: string): Promise<ActionResult<null>> {
  try {
    const result = await learningApiRequest<null>(`/api/cohorts/${classId}`, {
      method: 'DELETE',
    });

    if (result.success) {
      revalidatePath(`/dashboard/learning/courses/${courseId}/classes`);
    }

    return result;
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

// ── Course support/discussion actions ──

export interface CreateCourseDiscussionInput {
  courseId: string;
  title: string;
  content: string;
  contentId?: string | null;
}

export interface CreateDiscussionReplyInput {
  courseId: string;
  discussionId: string;
  content: string;
  parentReplyId?: string | null;
}

interface DiscussionActionDto {
  id: string;
  courseId: string;
  isPinned?: boolean;
  isResolved?: boolean;
}

interface DiscussionReplyActionDto {
  id: string;
  discussionId: string;
  isAcceptedAnswer?: boolean;
  upvoteCount?: number;
}

function revalidateCourseSupport(courseId: string, discussionId?: string) {
  revalidatePath(`/dashboard/learning/courses/${courseId}/support`);
  revalidatePath(`/dashboard/learning/courses/${courseId}/support/tickets`);
  revalidatePath(`/dashboard/learning/courses/${courseId}/support/discussions`);

  if (discussionId) {
    revalidatePath(`/dashboard/learning/courses/${courseId}/support/tickets/${discussionId}`);
    revalidatePath(`/dashboard/learning/courses/${courseId}/support/discussions/${discussionId}`);
  }
}

export async function createCourseDiscussion(input: CreateCourseDiscussionInput): Promise<ActionResult<{ id: string }>> {
  const title = input.title.trim();
  const content = input.content.trim();

  if (title.length < 5) {
    return { success: false, error: 'Discussion title must be at least 5 characters.' };
  }

  if (content.length < 10) {
    return { success: false, error: 'Discussion content must be at least 10 characters.' };
  }

  try {
    const result = await learningApiRequest<DiscussionActionDto>('/api/social/discussions', {
      method: 'POST',
      body: JSON.stringify({
        courseId: input.courseId,
        title,
        content,
        contentId: input.contentId?.trim() || null,
      }),
    });

    if (!result.success) return result;

    revalidateCourseSupport(input.courseId, result.data.id);
    return { success: true, data: { id: result.data.id } };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

export async function createDiscussionReply(input: CreateDiscussionReplyInput): Promise<ActionResult<{ id: string }>> {
  const content = input.content.trim();

  if (content.length < 2) {
    return { success: false, error: 'Reply content is required.' };
  }

  try {
    const result = await learningApiRequest<DiscussionReplyActionDto>(`/api/social/discussions/${input.discussionId}/replies`, {
      method: 'POST',
      body: JSON.stringify({
        discussionId: input.discussionId,
        content,
        parentReplyId: input.parentReplyId?.trim() || null,
      }),
    });

    if (!result.success) return result;

    revalidateCourseSupport(input.courseId, input.discussionId);
    return { success: true, data: { id: result.data.id } };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

export async function updateDiscussionPin(courseId: string, discussionId: string, pinned: boolean): Promise<ActionResult<null>> {
  try {
    const result = await learningApiRequest<DiscussionActionDto>(`/api/social/discussions/${discussionId}/${pinned ? 'pin' : 'unpin'}`, {
      method: 'POST',
      body: JSON.stringify({}),
    });

    if (!result.success) return { success: false, error: result.error };

    revalidateCourseSupport(courseId, discussionId);
    return { success: true, data: null };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

export async function resolveDiscussion(courseId: string, discussionId: string): Promise<ActionResult<null>> {
  try {
    const result = await learningApiRequest<DiscussionActionDto>(`/api/social/discussions/${discussionId}/resolve`, {
      method: 'POST',
      body: JSON.stringify({}),
    });

    if (!result.success) return { success: false, error: result.error };

    revalidateCourseSupport(courseId, discussionId);
    return { success: true, data: null };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

export async function deleteDiscussion(courseId: string, discussionId: string): Promise<ActionResult<null>> {
  try {
    const result = await learningApiRequest<null>(`/api/social/discussions/${discussionId}`, {
      method: 'DELETE',
    });

    if (result.success) {
      revalidateCourseSupport(courseId, discussionId);
    }

    return result;
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

export async function acceptDiscussionReply(courseId: string, discussionId: string, replyId: string): Promise<ActionResult<null>> {
  try {
    const result = await learningApiRequest<DiscussionReplyActionDto>(`/api/social/replies/${replyId}/accept`, {
      method: 'POST',
      body: JSON.stringify({}),
    });

    if (!result.success) return { success: false, error: result.error };

    revalidateCourseSupport(courseId, discussionId);
    return { success: true, data: null };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

export async function upvoteDiscussionReply(courseId: string, discussionId: string, replyId: string): Promise<ActionResult<null>> {
  try {
    const result = await learningApiRequest<DiscussionReplyActionDto>(`/api/social/replies/${replyId}/upvote`, {
      method: 'POST',
      body: JSON.stringify({}),
    });

    if (!result.success) return { success: false, error: result.error };

    revalidateCourseSupport(courseId, discussionId);
    return { success: true, data: null };
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
