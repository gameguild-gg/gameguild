'use server';

import { getToken } from '@/auth';
import { getCourseRouteParam } from '@/lib/learning/course-route';
import type { AssessmentType } from '@/lib/learning/queries/assessments';
import type { CourseIntegrationSettings, CourseNotificationSettings } from '@/lib/learning/queries/settings';
import type { LearningCoursesProgramContentType } from '@/lib/learning/types';
import {
  createServerClient,
  GeneratedApi,
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

async function resolveCourseMutationId(courseId: string): Promise<string> {
  const { resolveCourseId } = await import('@/lib/learning/queries/course');
  return resolveCourseId(courseId);
}

function revalidateCoursePath(courseId: string, resolvedCourseId: string, segment = '') {
  const suffix = segment ? `/${segment.replace(/^\/+/, '')}` : '';

  revalidatePath(`/dashboard/learning/courses/${courseId}${suffix}`);
  if (resolvedCourseId !== courseId) {
    revalidatePath(`/dashboard/learning/courses/${resolvedCourseId}${suffix}`);
  }
}

function createCourseModules() {
  const client = getApiClient();

  return {
    client,
    programs: new GeneratedApi.LearningCoursesProgramModule(client),
    content: new GeneratedApi.LearningCoursesProgramcontentModule(client),
    lifecycle: new GeneratedApi.LearningCoursesProgramlifecycleModule(client),
    assessments: new GeneratedApi.LearningAssessmentsModule(client),
    enrollments: new GeneratedApi.LearningEnrollmentsModule(client),
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
    const resolvedCourseId = await resolveCourseMutationId(courseId);
    const contentBody: LearningCoursesCreateProgramContent = {
      programId: resolvedCourseId,
      title: title.trim(),
      description: (description ?? '').trim(),
      type,
      sortOrder: sortOrder ?? 0,
      isRequired: true,
      visibility: 'Public',
      ...(parentId ? { parentId } : {}),
    };

    const { content } = createCourseModules();
    const result = await content.postCoursesContent(resolvedCourseId, contentBody);

    if (result.ok) {
      revalidateCoursePath(courseId, resolvedCourseId);
      return { success: true, data: { id: result.data.id! } };
    }

    return { success: false, error: extractError(result.error) };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

export async function deleteContent(courseId: string, contentId: string): Promise<ActionResult<null>> {
  try {
    const resolvedCourseId = await resolveCourseMutationId(courseId);
    const { content } = createCourseModules();
    const result = await content.deleteCoursesContent(resolvedCourseId, contentId);

    if (result.ok) {
      revalidateCoursePath(courseId, resolvedCourseId);
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
    const resolvedCourseId = await resolveCourseMutationId(courseId);
    const { content } = createCourseModules();
    const body = { id: contentId, ...fields } as LearningCoursesUpdateProgramContent;
    const result = await content.putCoursesContent(resolvedCourseId, contentId, body);

    if (result.ok) {
      revalidateCoursePath(courseId, resolvedCourseId);
      return { success: true, data: null };
    }

    return { success: false, error: extractError(result.error) };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

export async function reorderContent(courseId: string, contentIds: string[]): Promise<ActionResult<null>> {
  try {
    const resolvedCourseId = await resolveCourseMutationId(courseId);
    const { programs } = createCourseModules();
    const result = await programs.postCoursesContentReorder1(resolvedCourseId, { contentIds });

    if (result.ok) {
      revalidateCoursePath(courseId, resolvedCourseId);
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

export async function createCourse(input: CreateCourseInput): Promise<ActionResult<{ id: string; slug: string; routeParam: string }>> {
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
      const id = result.data.id!;
      const createdSlug = result.data.slug?.trim() || slug.trim();

      revalidatePath('/dashboard/learning/courses');
      return {
        success: true,
        data: {
          id,
          slug: createdSlug,
          routeParam: getCourseRouteParam({
            id,
            slug: createdSlug,
            creatorId: result.data.creatorId ?? null,
          }),
        },
      };
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
  const updateFields: LearningCoursesUpdateProgram & {
    clearMaxEnrollments?: boolean;
    clearEnrollmentDeadline?: boolean;
  } = { ...fields } as LearningCoursesUpdateProgram;

  if (input.maxEnrollments === null) {
    delete updateFields.maxEnrollments;
    updateFields.clearMaxEnrollments = true;
  }
  if (input.enrollmentDeadline === null) {
    delete updateFields.enrollmentDeadline;
    updateFields.clearEnrollmentDeadline = true;
  }

  try {
    const resolvedCourseId = await resolveCourseMutationId(courseId);
    const { programs } = createCourseModules();
    const result = await programs.putCourses(resolvedCourseId, updateFields);

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

export interface ManualEnrollStudentInput {
  courseId: string;
  userId: string;
  cohortId?: string | null;
}

interface UserSearchResponse {
  items?: Array<{
    id?: string | null;
    email?: string | null;
    username?: string | null;
    name?: string | null;
  }> | null;
}

function isGuid(value: string): boolean {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value.trim());
}

async function resolveEnrollmentUserId(reference: string): Promise<ActionResult<{ userId: string }>> {
  const value = reference.trim();
  if (isGuid(value)) {
    return { success: true, data: { userId: value } };
  }

  const { client } = createCourseModules();
  const query = value.includes('@')
    ? { email: value, limit: 5 }
    : { q: value, limit: 5 };

  const result = await client.request({
    method: 'GET',
    path: '/v1/users',
    params: query,
    requiresAuth: true,
  }) as { ok: true; data: UserSearchResponse } | { ok: false; error?: { detail?: string; message?: string } };

  if (!result.ok) {
    return { success: false, error: result.error?.detail || result.error?.message || 'Could not search users.' };
  }

  const normalized = value.toLowerCase();
  const matches = result.data.items ?? [];
  const match = matches.find((user) =>
    user.email?.toLowerCase() === normalized ||
    user.username?.toLowerCase() === normalized ||
    user.name?.toLowerCase() === normalized,
  ) ?? matches[0];

  if (!match?.id) {
    return { success: false, error: 'No user matched that email, username, or user ID.' };
  }

  return { success: true, data: { userId: match.id } };
}

export async function manualEnrollStudent(input: ManualEnrollStudentInput): Promise<ActionResult<{ id: string | null }>> {
  const courseId = input.courseId.trim();
  const userReference = input.userId.trim();
  const cohortId = input.cohortId?.trim() || null;

  if (!courseId) {
    return { success: false, error: 'Course is required.' };
  }

  if (!userReference) {
    return { success: false, error: 'Student email, username, or user ID is required.' };
  }

  try {
    const resolvedCourseId = await resolveCourseMutationId(courseId);
    const resolvedUser = await resolveEnrollmentUserId(userReference);
    if (!resolvedUser.success) {
      return resolvedUser;
    }

    const { programs, enrollments } = createCourseModules();
    const rosterResult = await programs.postCoursesUsers(resolvedCourseId, resolvedUser.data.userId);
    if (!rosterResult.ok) {
      return { success: false, error: extractError(rosterResult.error) };
    }

    if (cohortId) {
      const cohortResult = await enrollments.postApiLearningEnrollments({
        courseId: resolvedCourseId,
        userId: resolvedUser.data.userId,
        cohortId,
      });

      if (!cohortResult.ok) {
        await programs.deleteCoursesUsers(resolvedCourseId, resolvedUser.data.userId);
        return { success: false, error: extractError(cohortResult.error) };
      }
    }

    revalidateCoursePath(courseId, resolvedCourseId, 'students');
    revalidateCoursePath(courseId, resolvedCourseId);
    return { success: true, data: { id: rosterResult.data.enrollmentId ?? null } };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

export async function removeCourseStudents(courseId: string, userIds: string[]): Promise<ActionResult<{ removed: number }>> {
  const uniqueUserIds = [...new Set(userIds.map((userId) => userId.trim()).filter(Boolean))];
  if (uniqueUserIds.length === 0) {
    return { success: false, error: 'Select at least one student to remove.' };
  }

  try {
    const resolvedCourseId = await resolveCourseMutationId(courseId);
    const { programs } = createCourseModules();
    const results = await Promise.all(
      uniqueUserIds.map((userId) => programs.deleteCoursesUsers(resolvedCourseId, userId)),
    );
    const failed = results.find((result) => !result.ok);
    if (failed && !failed.ok) return { success: false, error: extractError(failed.error) };

    revalidateCoursePath(courseId, resolvedCourseId, 'students');
    revalidateCoursePath(courseId, resolvedCourseId);
    return { success: true, data: { removed: uniqueUserIds.length } };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

export interface SendCourseStudentMessageInput {
  courseId: string;
  userIds: string[];
  subject: string;
  message: string;
}

export async function sendCourseStudentMessage(input: SendCourseStudentMessageInput): Promise<ActionResult<{ sent: number }>> {
  const subject = input.subject.trim();
  const message = input.message.trim();
  const userIds = [...new Set(input.userIds.map((userId) => userId.trim()).filter(Boolean))];

  if (userIds.length === 0) return { success: false, error: 'Select at least one student.' };
  if (subject.length < 3) return { success: false, error: 'Subject must be at least 3 characters.' };
  if (message.length < 2) return { success: false, error: 'Message is required.' };

  try {
    const resolvedCourseId = await resolveCourseMutationId(input.courseId);
    return await learningApiRequest<{ sent: number }>(`/v1/courses/${resolvedCourseId}/students/message`, {
      method: 'POST',
      body: JSON.stringify({ userIds, subject, message }),
    });
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

type NotificationSettingsInput = Omit<CourseNotificationSettings, 'courseId' | 'updatedAt'>;
type IntegrationSettingsInput = Omit<CourseIntegrationSettings, 'courseId' | 'updatedAt'>;

async function updateCourseMetadataSection(
  courseId: string,
  key: 'notificationSettings' | 'integrationSettings',
  value: unknown,
): Promise<ActionResult<null>> {
  try {
    const resolvedCourseId = await resolveCourseMutationId(courseId);
    const { programs } = createCourseModules();
    const courseResult = await programs.getCourses1(resolvedCourseId);
    if (!courseResult.ok) return { success: false, error: extractError(courseResult.error) };

    const metadata = parseMetadata(courseResult.data.metadata);
    metadata[key] = value;
    const result = await programs.putCourses(resolvedCourseId, {
      metadata: JSON.stringify(metadata),
    } satisfies LearningCoursesUpdateProgram);

    if (!result.ok) return { success: false, error: extractError(result.error) };

    revalidateCoursePath(courseId, resolvedCourseId, `settings/${key === 'notificationSettings' ? 'notifications' : 'integrations'}`);
    return { success: true, data: null };
  } catch (error) {
    return { success: false, error: `Unexpected error: ${error instanceof Error ? error.message : String(error)}` };
  }
}

export async function updateCourseNotificationSettings(
  courseId: string,
  input: NotificationSettingsInput,
): Promise<ActionResult<null>> {
  const classReminders = [...new Set(input.studentNotifications.classReminders)]
    .filter((minutes) => Number.isFinite(minutes) && minutes >= 0 && minutes <= 10080)
    .sort((a, b) => b - a);
  const lowRatingThreshold = Math.min(5, Math.max(1, Math.round(input.instructorNotifications.lowRatingThreshold)));
  const templates = input.templates
    .map((template) => ({
      id: template.id.trim(),
      type: template.type.trim(),
      subject: template.subject.trim(),
      enabled: Boolean(template.enabled),
    }))
    .filter((template) => template.id && template.type && template.subject)
    .slice(0, 20);

  return updateCourseMetadataSection(courseId, 'notificationSettings', {
    studentNotifications: {
      ...input.studentNotifications,
      classReminders,
    },
    instructorNotifications: {
      ...input.instructorNotifications,
      lowRatingThreshold,
    },
    templates,
  });
}

export async function updateCourseIntegrationSettings(
  courseId: string,
  input: IntegrationSettingsInput,
): Promise<ActionResult<null>> {
  for (const webhook of input.webhooks) {
    try {
      const url = new URL(webhook.url);
      if (!['http:', 'https:'].includes(url.protocol)) throw new Error('invalid protocol');
    } catch {
      return { success: false, error: 'Webhook URLs must use http or https.' };
    }
  }

  const integrations = input.integrations.slice(0, 20).map((integration) => ({
    ...integration,
    id: integration.id.trim(),
    name: integration.name.trim(),
    enabled: Boolean(integration.enabled),
    status: integration.enabled ? integration.status : 'disconnected' as const,
  })).filter((integration) => integration.id && integration.name);
  const webhooks = input.webhooks.slice(0, 20).map((webhook) => ({
    id: webhook.id.trim(),
    url: webhook.url.trim(),
    events: [...new Set(webhook.events.map((event) => event.trim()).filter(Boolean))],
    enabled: Boolean(webhook.enabled),
  }));

  return updateCourseMetadataSection(courseId, 'integrationSettings', { integrations, webhooks });
}

export async function updateCourseReviewModeration(
  courseId: string,
  reviewId: string,
  isApproved: boolean,
  isFeatured: boolean,
): Promise<ActionResult<null>> {
  const result = await learningApiRequest<unknown>(`/api/social/reviews/${reviewId}/moderation`, {
    method: 'PATCH',
    body: JSON.stringify({ isApproved, isFeatured }),
  });

  if (!result.success) return { success: false, error: result.error };

  revalidatePath(`/dashboard/learning/courses/${courseId}/listing/testimonials`);
  revalidatePath(`/courses/${courseId}`);
  return { success: true, data: null };
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
    const resolvedCourseId = await resolveCourseMutationId(input.courseId);
    const { programs } = createCourseModules();

    if (!input.isMonetizationEnabled) {
      const result = await programs.postCoursesDisableMonetization(resolvedCourseId);

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

    const result = await programs.postCoursesMonetize(resolvedCourseId, {
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
  type: AssessmentType;
  assessmentGroupId?: string | null;
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
    const resolvedCourseId = await resolveCourseMutationId(courseId);
    const body: LearningAssessmentsCreateAssessmentInput & { assessmentGroupId?: string | null } = {
      courseId: resolvedCourseId,
      title: title.trim(),
      description: rest.description?.trim() ?? null,
      type: rest.type,
      assessmentGroupId: rest.assessmentGroupId ?? null,
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

export interface CreateAssessmentGroupInput {
  courseId: string;
  name: string;
  weightPercent: number;
  order?: number;
  description?: string;
}

export interface UpdateAssessmentGroupInput {
  courseId: string;
  groupId: string;
  name: string;
  weightPercent: number;
  order?: number;
  description?: string | null;
}

interface AssessmentGroupActionDto {
  id: string;
  courseId: string;
}

function validateAssessmentGroup(name: string, weightPercent: number): string | null {
  if (name.trim().length < 1) {
    return 'Group name is required.';
  }

  if (!Number.isFinite(weightPercent) || weightPercent < 0 || weightPercent > 100) {
    return 'Weight must be between 0 and 100.';
  }

  return null;
}

export async function createAssessmentGroup(input: CreateAssessmentGroupInput): Promise<ActionResult<{ id: string }>> {
  const name = input.name.trim();

  const validationError = validateAssessmentGroup(name, input.weightPercent);
  if (validationError) {
    return { success: false, error: validationError };
  }

  try {
    const resolvedCourseId = await resolveCourseMutationId(input.courseId);
    const result = await learningApiRequest<AssessmentGroupActionDto>('/v1/assessments/groups', {
      method: 'POST',
      body: JSON.stringify({
        courseId: resolvedCourseId,
        name,
        weightPercent: input.weightPercent,
        order: input.order ?? 0,
        description: input.description?.trim() || null,
      }),
    });

    if (!result.success) return result;

    revalidatePath(`/dashboard/learning/courses/${input.courseId}`);
    revalidatePath(`/dashboard/learning/courses/${input.courseId}/assessments`);
    return { success: true, data: { id: result.data.id } };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

export async function updateAssessmentGroup(input: UpdateAssessmentGroupInput): Promise<ActionResult<{ id: string }>> {
  const name = input.name.trim();

  const validationError = validateAssessmentGroup(name, input.weightPercent);
  if (validationError) {
    return { success: false, error: validationError };
  }

  if (!input.groupId.trim()) {
    return { success: false, error: 'Group id is required.' };
  }

  try {
    const result = await learningApiRequest<AssessmentGroupActionDto>(`/v1/assessments/groups/${input.groupId}`, {
      method: 'PUT',
      body: JSON.stringify({
        name,
        description: input.description?.trim() || null,
        weightPercent: input.weightPercent,
        order: input.order ?? 0,
      }),
    });

    if (!result.success) return result;

    revalidatePath(`/dashboard/learning/courses/${input.courseId}`);
    revalidatePath(`/dashboard/learning/courses/${input.courseId}/assessments`);
    return { success: true, data: { id: result.data.id } };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

export async function deleteAssessmentGroup(courseId: string, groupId: string): Promise<ActionResult<null>> {
  if (!groupId.trim()) {
    return { success: false, error: 'Group id is required.' };
  }

  try {
    const result = await learningApiRequest<null>(`/v1/assessments/groups/${groupId}`, {
      method: 'DELETE',
    });

    if (!result.success) return result;

    revalidatePath(`/dashboard/learning/courses/${courseId}`);
    revalidatePath(`/dashboard/learning/courses/${courseId}/assessments`);
    return { success: true, data: null };
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
  assessmentGroupId?: string | null;
  clearAssessmentGroupId?: boolean;
}

export async function updateAssessment(input: UpdateAssessmentInput): Promise<ActionResult<null>> {
  const { courseId, assessmentId, ...fields } = input;

  try {
    const body: LearningAssessmentsUpdateAssessmentInput & {
      assessmentGroupId?: string | null;
      clearAssessmentGroupId?: boolean;
    } = {
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
      assessmentGroupId: fields.assessmentGroupId ?? null,
      clearAssessmentGroupId: fields.clearAssessmentGroupId ?? false,
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
    const resolvedCourseId = await resolveCourseMutationId(input.courseId);
    const result = await learningApiRequest<CertificateTemplateActionDto>('/api/certificates/templates', {
      method: 'POST',
      body: JSON.stringify({
        courseId: resolvedCourseId,
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

export interface UpdateCertificateTemplateInput {
  courseId: string;
  templateId: string;
  name: string;
  description?: string | null;
  templateHtml: string;
  templateStyles?: string | null;
  isDefault: boolean;
  isActive: boolean;
}

export async function updateCertificateTemplate(input: UpdateCertificateTemplateInput): Promise<ActionResult<null>> {
  const name = input.name.trim();
  const templateHtml = input.templateHtml.trim();

  if (name.length < 3) {
    return { success: false, error: 'Template name must be at least 3 characters.' };
  }

  if (templateHtml.length < 20) {
    return { success: false, error: 'Template HTML must be at least 20 characters.' };
  }

  try {
    const result = await learningApiRequest<CertificateTemplateActionDto>(`/api/certificates/templates/${input.templateId}`, {
      method: 'PUT',
      body: JSON.stringify({
        name,
        description: input.description?.trim() || null,
        templateHtml,
        templateStyles: input.templateStyles?.trim() || null,
        isDefault: input.isDefault,
        isActive: input.isActive,
      }),
    });

    if (!result.success) return result;

    revalidatePath(`/dashboard/learning/courses/${input.courseId}/certificates`);
    revalidatePath(`/dashboard/learning/courses/${input.courseId}/certificates/${input.templateId}`);
    return { success: true, data: null };
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
    const resolvedCourseId = await resolveCourseMutationId(input.courseId);
    const result = await learningApiRequest<CourseClassActionDto>('/api/cohorts', {
      method: 'POST',
      body: JSON.stringify({
        courseId: resolvedCourseId,
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

export interface AddCourseSupportTicketMessageInput {
  courseId: string;
  ticketId: string;
  message: string;
}

export async function addCourseSupportTicketMessage(input: AddCourseSupportTicketMessageInput): Promise<ActionResult<null>> {
  const message = input.message.trim();
  if (message.length < 2) return { success: false, error: 'Reply is required.' };

  try {
    const resolvedCourseId = await resolveCourseMutationId(input.courseId);
    const result = await learningApiRequest<unknown>(
      `/v1/courses/${resolvedCourseId}/support/tickets/${input.ticketId}/messages`,
      { method: 'POST', body: JSON.stringify({ message }) },
    );
    if (!result.success) return { success: false, error: result.error };

    revalidateCourseSupport(input.courseId, input.ticketId);
    return { success: true, data: null };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
  }
}

export interface ResolveCourseSupportTicketInput {
  courseId: string;
  ticketId: string;
  summary: string;
}

export async function resolveCourseSupportTicket(input: ResolveCourseSupportTicketInput): Promise<ActionResult<null>> {
  const summary = input.summary.trim();
  if (summary.length < 3) return { success: false, error: 'Resolution summary must be at least 3 characters.' };

  try {
    const resolvedCourseId = await resolveCourseMutationId(input.courseId);
    const result = await learningApiRequest<unknown>(
      `/v1/courses/${resolvedCourseId}/support/tickets/${input.ticketId}:resolve`,
      { method: 'POST', body: JSON.stringify({ summary }) },
    );
    if (!result.success) return { success: false, error: result.error };

    revalidateCourseSupport(input.courseId, input.ticketId);
    return { success: true, data: null };
  } catch (e) {
    return { success: false, error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}` };
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
    const resolvedCourseId = await resolveCourseMutationId(input.courseId);
    const result = await learningApiRequest<DiscussionActionDto>('/api/social/discussions', {
      method: 'POST',
      body: JSON.stringify({
        courseId: resolvedCourseId,
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
