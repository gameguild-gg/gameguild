import { getToken } from '@/auth';
import { createServerClient, GeneratedApi, type LearningCoursesProgramContent } from '@game-guild/client';
import { cache } from 'react';

export interface LearningContentLibraryItem {
  id: string;
  courseId: string;
  courseTitle: string;
  courseSlug: string;
  title: string;
  description: string | null;
  type: string;
  visibility: 'public' | 'private';
  status: 'draft' | 'published';
  durationMinutes: number | null;
  isRequired: boolean;
  updatedAt: string;
}

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
    content: new GeneratedApi.LearningCoursesProgramcontentModule(client),
  };
}

function flattenContent(items: LearningCoursesProgramContent[]): LearningCoursesProgramContent[] {
  const flattened: LearningCoursesProgramContent[] = [];

  const visit = (item: LearningCoursesProgramContent) => {
    flattened.push(item);

    for (const child of item.children ?? []) {
      visit(child);
    }
  };

  for (const item of items) {
    visit(item);
  }

  return flattened.sort((left, right) => (left.sortOrder ?? 0) - (right.sortOrder ?? 0));
}

function mapVisibility(visibility: unknown): 'public' | 'private' {
  return String(visibility ?? '').toLowerCase() === 'public' ? 'public' : 'private';
}

function mapStatus(item: LearningCoursesProgramContent): 'draft' | 'published' {
  return mapVisibility(item.visibility) === 'public' ? 'published' : 'draft';
}

function normalizeContentType(type: string | null | undefined): string {
  if (type === 'Page') return 'Lesson';
  if (type === 'Challenge') return 'Assignment';
  return type ?? 'Lesson';
}

export const getLearningContentLibrary = cache(async (): Promise<{
  items: LearningContentLibraryItem[];
  error: string | null;
}> => {
  try {
    const { programs, content } = createCourseModules();
    const coursesResult = await programs.getCourses({ take: 100 });

    if (!coursesResult.ok) {
      const err = coursesResult.error as { status?: number; code?: string; message?: string; detail?: string } | undefined;
      return {
        items: [],
        error: `[${err?.status ?? 'unknown'}${err?.code ? ' ' + err.code : ''}] ${err?.detail || err?.message || 'Failed to load courses'}`,
      };
    }

    const courseContent = await Promise.all(
      coursesResult.data
        .filter((course) => Boolean(course.id))
        .map(async (course) => {
          const courseId = String(course.id);
          const contentResult = await content.getCoursesByProgramIdContent(courseId);

          if (!contentResult.ok) {
            return [];
          }

          return flattenContent(contentResult.data)
            .filter((item) => Boolean(item.id))
            .map((item) => ({
              id: String(item.id),
              courseId,
              courseTitle: course.title ?? 'Untitled course',
              courseSlug: course.slug ?? '',
              title: item.title ?? 'Untitled content',
              description: item.description ?? null,
              type: normalizeContentType(item.type),
              visibility: mapVisibility(item.visibility),
              status: mapStatus(item),
              durationMinutes: item.estimatedMinutes ?? null,
              isRequired: item.isRequired ?? false,
              updatedAt: item.updatedAt ?? item.createdAt ?? course.updatedAt ?? new Date().toISOString(),
            }) satisfies LearningContentLibraryItem);
        }),
    );

    return {
      items: courseContent
        .flat()
        .sort((left, right) => new Date(right.updatedAt).getTime() - new Date(left.updatedAt).getTime()),
      error: null,
    };
  } catch (error) {
    return {
      items: [],
      error: `Unexpected: ${error instanceof Error ? error.message : String(error)}`,
    };
  }
});
