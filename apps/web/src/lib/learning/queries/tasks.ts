// =============================================================================
// CROSS-COURSE TASKS QUERIES (/me/tasks)
// =============================================================================

import { getToken } from '@/auth';
import { createServerClient, GeneratedApi } from '@game-guild/client';

export type LearningTaskType = 'grade' | 'do' | 'review';

export interface LearningTask {
  type: LearningTaskType;
  courseId: string;
  courseTitle: string;
  /** Resolved from the course list; undefined when the course is not in the actor's list. */
  courseSlug?: string;
  assessmentId: string;
  assessmentTitle: string;
  dueAt: string | null;
  countSubmitted: number | null;
  reviewsCompleted: number | null;
  reviewsRequired: number | null;
}

export type MyTasksResult =
  | { ok: true; tasks: LearningTask[] }
  | { ok: false; error: string };

function getApiClient() {
  const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080';

  return createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
  });
}

/**
 * Resolve program id → slug so task cards can link to slug-based routes.
 * Non-fatal: unresolved courses render their title without a link.
 */
async function getCourseSlugById(): Promise<Map<string, string>> {
  const slugs = new Map<string, string>();

  try {
    const programs = new GeneratedApi.LearningCoursesProgramModule(getApiClient());
    const result = await programs.getCoursesForGetCourses({ take: 100 });

    if (result.ok && Array.isArray(result.data)) {
      for (const program of result.data) {
        if (program.id && program.slug) {
          slugs.set(program.id, program.slug);
        }
      }
    }
  } catch {
    // Slug resolution is best-effort; tasks still render without links.
  }

  return slugs;
}

/**
 * Fetch the actor's cross-course task list (grade / do / review).
 * All counts come from /me/tasks — never aggregated client-side.
 */
export async function getMyTasks(): Promise<MyTasksResult> {
  try {
    const tasks = new GeneratedApi.LearningAssessmentsTasksModule(getApiClient());
    const [tasksResult, slugs] = await Promise.all([
      tasks.getMeTasks(),
      getCourseSlugById(),
    ]);

    if (!tasksResult.ok) {
      return { ok: false, error: 'Failed to load tasks. Please try again.' };
    }

    const items = (tasksResult.data.items ?? [])
      .filter(
        (item): item is typeof item & { type: LearningTaskType } =>
          item.type === 'grade' || item.type === 'do' || item.type === 'review',
      )
      .map((item) => ({
        type: item.type,
        courseId: item.courseId ?? '',
        courseTitle: item.courseTitle ?? 'Untitled course',
        courseSlug: (item.courseId && slugs.get(item.courseId)) || undefined,
        assessmentId: item.assessmentId ?? '',
        assessmentTitle: item.assessmentTitle ?? 'Untitled assessment',
        dueAt: item.dueAt ?? null,
        countSubmitted: item.countSubmitted ?? null,
        reviewsCompleted: item.reviewsCompleted ?? null,
        reviewsRequired: item.reviewsRequired ?? null,
      }));

    return { ok: true, tasks: items satisfies LearningTask[] };
  } catch {
    return { ok: false, error: 'Failed to load tasks. Please try again.' };
  }
}

/** Sum of submissions awaiting grading across the actor's grade tasks (instructor widget). */
export function sumAwaitingGrading(tasks: readonly LearningTask[]): number | null {
  const gradeTasks = tasks.filter((task) => task.type === 'grade');
  if (gradeTasks.length === 0) return null;

  return gradeTasks.reduce((acc, task) => acc + (task.countSubmitted ?? 0), 0);
}
