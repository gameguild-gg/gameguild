'use server';

import { getToken } from '@/auth';
import { createServerClient, GeneratedApi } from '@game-guild/client';

type ActionResult =
  | { success: true }
  | { success: false; error: string };

function getApiClient() {
  const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080';

  return createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
  });
}

function createCourseModules() {
  const client = getApiClient();

  return {
    programs: new GeneratedApi.LearningCoursesProgramModule(client),
    interactions: new GeneratedApi.LearningCoursesContentInteractionModule(client),
  };
}

function extractError(error: unknown): string {
  const candidate = error as { message?: string; detail?: string } | undefined;
  return candidate?.detail || candidate?.message || 'Unable to update course progress.';
}

async function resolveEnrollmentId(
  programs: GeneratedApi.LearningCoursesProgramModule,
  courseId: string,
): Promise<string> {
  const progress = await programs.getCoursesMeProgress(courseId);
  if (!progress.ok || !progress.data.enrollmentId) {
    throw new Error(progress.ok ? 'Your course enrollment could not be resolved.' : extractError(progress.error));
  }

  return progress.data.enrollmentId;
}

export async function beginCourseContent(courseId: string, contentId: string): Promise<ActionResult> {
  try {
    const { programs, interactions } = createCourseModules();
    const enrollmentId = await resolveEnrollmentId(programs, courseId);
    const existing = await interactions.getCourseInteractionsUserContent(enrollmentId, contentId, { programId: courseId });
    const result = existing.ok && existing.data.id
      ? await interactions.putCourseInteractionsProgress(
          existing.data.id,
          { contentId, programUserId: enrollmentId, completionPercentage: Math.max(existing.data.completionPercentage ?? 0, 1) },
          { programId: courseId },
        )
      : await interactions.postCourseInteractions(
          { contentId, programUserId: enrollmentId },
          { programId: courseId },
        );

    if (!result.ok) {
      return { success: false, error: extractError(result.error) };
    }

    return { success: true };
  } catch (error) {
    return { success: false, error: extractError(error) };
  }
}

export async function completeCourseContent(courseId: string, contentId: string): Promise<ActionResult> {
  try {
    const { programs, interactions } = createCourseModules();
    const enrollmentId = await resolveEnrollmentId(programs, courseId);
    let interaction = await interactions.getCourseInteractionsUserContent(enrollmentId, contentId, { programId: courseId });
    if (!interaction.ok) {
      interaction = await interactions.postCourseInteractions(
        { contentId, programUserId: enrollmentId },
        { programId: courseId },
      );
    }

    if (!interaction.ok || !interaction.data.id) {
      return { success: false, error: interaction.ok ? 'The lesson interaction could not be resolved.' : extractError(interaction.error) };
    }

    const result = await interactions.postCourseInteractionsComplete(
      interaction.data.id,
      { contentId, programUserId: enrollmentId },
      { programId: courseId },
    );

    if (!result.ok) {
      return { success: false, error: extractError(result.error) };
    }

    return { success: true };
  } catch (error) {
    return { success: false, error: extractError(error) };
  }
}