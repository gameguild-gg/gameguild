// =============================================================================
// COURSES LIST DATA QUERIES
// =============================================================================

import { getToken } from '@/auth';
import { getCourseRouteParam, slugifyRoutePart } from '@/lib/learning/course-route';
import { createServerClient, GeneratedApi } from '@game-guild/client';

/**
 * Course list item with data for KPI computation
 */
export interface CourseListItem {
  id: string;
  slug: string;
  routeParam: string;
  creatorId: string | null;
  creatorHandle: string | null;
  title: string;
  thumbnail: string | null;
  status: 'draft' | 'published' | 'archived';
  visibility: 'public' | 'private' | 'unlisted';
  enrolledCount: number;
  completionPercent: number | null;
  avgRating: string | null;
}

type ProgramDto = GeneratedApi.LearningCoursesProgram;

function getApiClient() {
  const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080';
  return createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
  });
}

function createCourseProgramsModule() {
  return new GeneratedApi.LearningCoursesProgramModule(getApiClient());
}

type UserLookupResult =
  | { ok: true; data: { name?: string | null; email?: string | null } }
  | { ok: false; error?: unknown };

type UserLookupModule = {
  getUsersByUserId(userId: string): Promise<UserLookupResult>;
};

function createUsersModule(): UserLookupModule | null {
  const UsersModule = (GeneratedApi as unknown as {
    UsersModule?: new (client: ReturnType<typeof getApiClient>) => UserLookupModule;
  }).UsersModule;

  return UsersModule ? new UsersModule(getApiClient()) : null;
}

async function getCreatorHandles(programs: ProgramDto[]): Promise<Map<string, string>> {
  const users = createUsersModule();
  const creatorIds = [...new Set(programs.map((program) => program.creatorId).filter((id): id is string => Boolean(id)))];
  const handles = new Map<string, string>();

  await Promise.all(
    creatorIds.map(async (creatorId) => {
      const fallback = slugifyRoutePart(creatorId).slice(0, 12) || 'gameguild';

      if (!users) {
        handles.set(creatorId, fallback);
        return;
      }

      try {
        const result = await users.getUsersByUserId(creatorId);
        if (!result.ok) {
          handles.set(creatorId, fallback);
          return;
        }

        handles.set(
          creatorId,
          slugifyRoutePart(result.data.name ?? '') ||
          slugifyRoutePart(result.data.email?.split('@')[0] ?? '') ||
          fallback,
        );
      } catch {
        handles.set(creatorId, fallback);
      }
    }),
  );

  return handles;
}

function mapAverageRating(program: ProgramDto): string | null {
  const totalRatings = Math.max(0, program.totalRatings ?? 0);
  if (totalRatings === 0) {
    return null;
  }

  return (program.averageRating ?? 0).toFixed(1);
}

async function getCourseMetrics(
  programs: GeneratedApi.LearningCoursesProgramModule,
  program: ProgramDto,
): Promise<Pick<CourseListItem, 'enrolledCount' | 'completionPercent' | 'avgRating'>> {
  const enrolledCount = Math.max(0, program.currentEnrollments ?? 0);
  const avgRating = mapAverageRating(program);

  if (!program.id) {
    return {
      enrolledCount,
      completionPercent: null,
      avgRating,
    };
  }

  try {
    const analyticsResult = await programs.getCoursesAnalytics(String(program.id));
    if (!analyticsResult.ok) {
      return {
        enrolledCount,
        completionPercent: null,
        avgRating,
      };
    }

    const totalUsers = Math.max(0, analyticsResult.data.totalUsers ?? enrolledCount);
    const completionPercent =
      totalUsers > 0
        ? Math.min(
          100,
          Math.max(
            0,
            Math.round(
              analyticsResult.data.completionRate ??
              (((analyticsResult.data.completedUsers ?? 0) / totalUsers) * 100),
            ),
          ),
        )
        : null;

    return {
      enrolledCount: totalUsers,
      completionPercent,
      avgRating,
    };
  } catch {
    return {
      enrolledCount,
      completionPercent: null,
      avgRating,
    };
  }
}

// ContentStatus enum: Draft=0, Review=1, Published=2, Archived=3, Deleted=4
function mapStatus(status: unknown): 'draft' | 'published' | 'archived' {
  if (status === 2 || status === '2') return 'published';
  if (status === 3 || status === '3') return 'archived';
  const s = String(status ?? '').toLowerCase();
  if (s === 'published') return 'published';
  if (s === 'archived') return 'archived';
  return 'draft';
}

// ContentVisibility enum: Private=0, Internal=1, Friends=2, Protected=3, Public=4
function mapVisibility(visibility: unknown): 'public' | 'private' | 'unlisted' {
  if (visibility === 4 || visibility === '4') return 'public';
  const v = String(visibility ?? '').toLowerCase();
  if (v === 'public') return 'public';
  if (v === 'unlisted') return 'unlisted';
  return 'private';
}

/**
 * Fetch courses list for the instructor.
 *
 * Endpoint: GET /v1/courses
 */
export async function getCourses(): Promise<{
  courses: CourseListItem[];
  error: string | null;
}> {
  try {
    const programs = createCourseProgramsModule();
    const result = await programs.getCourses({ take: 50 });

    if (result.ok && Array.isArray(result.data)) {
      const creatorHandles = await getCreatorHandles(result.data);
      const courses = await Promise.all(
        result.data.map(async (program) => {
          const metrics = await getCourseMetrics(programs, program);
          const id = String(program.id ?? '');
          const slug = typeof program.slug === 'string' ? program.slug.trim() : '';
          const creatorId = program.creatorId ?? null;
          const creatorHandle = creatorId ? creatorHandles.get(creatorId) ?? null : null;

          return {
            id,
            slug,
            creatorId,
            creatorHandle,
            routeParam: getCourseRouteParam({ id, slug, creatorId, creatorHandle }),
            title: program.title ?? 'Untitled course',
            thumbnail: typeof program.thumbnail === 'string' ? program.thumbnail : null,
            status: mapStatus(program.status),
            visibility: mapVisibility(program.visibility),
            enrolledCount: metrics.enrolledCount,
            completionPercent: metrics.completionPercent,
            avgRating: metrics.avgRating,
          } satisfies CourseListItem;
        }),
      );

      return {
        courses,
        error: null,
      };
    }

    const err = result.error as { status?: number; code?: string; message?: string; detail?: string } | undefined;
    return {
      courses: [],
      error: `[${err?.status ?? 'unknown'}${err?.code ? ' ' + err.code : ''}] ${err?.detail || err?.message || 'Failed to load courses'}`,
    };
  } catch (e) {
    return {
      courses: [],
      error: `Unexpected: ${e instanceof Error ? e.message : String(e)}`,
    };
  }
}
