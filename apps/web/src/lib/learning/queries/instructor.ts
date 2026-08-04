// =============================================================================
// INSTRUCTOR DATA QUERIES
// =============================================================================

import { getToken } from '@/auth';
import { createServerClient, GeneratedApi } from '@game-guild/client';

/**
 * Instructor course summary for KPI computation
 */
export interface InstructorCourseSummary {
  id: string;
  title: string;
  status: 'draft' | 'published' | 'archived';
  enrolledCount: number;
  completionPercent: number | null;
  averageRating: number | null;
  totalRatings: number;
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

function mapStatus(status: unknown): 'draft' | 'published' | 'archived' {
  if (status === 2 || status === '2') return 'published';
  if (status === 3 || status === '3') return 'archived';

  const normalizedStatus = String(status ?? '').toLowerCase();
  if (normalizedStatus === 'published') return 'published';
  if (normalizedStatus === 'archived') return 'archived';

  return 'draft';
}

async function getCourseMetrics(
  programs: GeneratedApi.LearningCoursesProgramModule,
  program: ProgramDto,
): Promise<Pick<InstructorCourseSummary, 'enrolledCount' | 'completionPercent' | 'averageRating' | 'totalRatings'>> {
  const enrolledCount = Math.max(0, program.currentEnrollments ?? 0);
  const totalRatings = Math.max(0, program.totalRatings ?? 0);
  const averageRating = totalRatings > 0 ? program.averageRating ?? 0 : null;

  if (!program.id) {
    return {
      enrolledCount,
      completionPercent: null,
      averageRating,
      totalRatings,
    };
  }

  try {
    const analyticsResult = await programs.getCoursesAnalytics(String(program.id));
    if (!analyticsResult.ok) {
      return {
        enrolledCount,
        completionPercent: null,
        averageRating,
        totalRatings,
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
      averageRating,
      totalRatings,
    };
  } catch {
    return {
      enrolledCount,
      completionPercent: null,
      averageRating,
      totalRatings,
    };
  }
}

/**
 * Activity feed item
 */
export interface ActivityItem {
  type: 'enrollment' | 'completion' | 'review' | 'comment' | 'activity';
  studentName: string;
  courseName: string;
  timestamp: string;
}

type StudentProgressDto = GeneratedApi.LearningCoursesUserProgress & {
  userId?: string;
  userName?: string;
  userEmail?: string;
};

/**
 * Fetch instructor stats for the learning dashboard overview.
 *
 * @returns Instructor statistics built from the authenticated course list and
 * per-course analytics endpoints.
 */
export async function getInstructorStats(): Promise<{
  courses: InstructorCourseSummary[];
}> {
  try {
    const programs = createCourseProgramsModule();
    const result = await programs.getCourses({ take: 50 });

    if (!result.ok || !Array.isArray(result.data)) {
      return { courses: [] };
    }

    const courses = await Promise.all(
      result.data.map(async (program) => {
        const metrics = await getCourseMetrics(programs, program);

        return {
          id: String(program.id ?? ''),
          title: program.title ?? 'Untitled course',
          status: mapStatus(program.status),
          enrolledCount: metrics.enrolledCount,
          completionPercent: metrics.completionPercent,
          averageRating: metrics.averageRating,
          totalRatings: metrics.totalRatings,
        } satisfies InstructorCourseSummary;
      }),
    );

    return { courses };
  } catch {
    return { courses: [] };
  }
}

/**
 * Fetch recent activity feed for the instructor.
 *
 * @returns Recent activity items scoped by auth context
 *
 * Fetch Type: GraphQL
 * Cache: revalidate 60s
 * Endpoint: TBD - GraphQL query `recentActivity`
 *
 * Data returned:
 * - activities[] (type, studentName, courseName, timestamp)
 */
export async function getRecentActivity(): Promise<{
  activities: ActivityItem[];
}> {
  try {
    const programs = createCourseProgramsModule();
    const coursesResult = await programs.getCourses({ take: 20 });

    if (!coursesResult.ok || !Array.isArray(coursesResult.data) || coursesResult.data.length === 0) {
      return { activities: [] };
    }

    const activityResults = await Promise.all(
      coursesResult.data
        .filter((course): course is ProgramDto & { id: string } => Boolean(course.id))
        .map(async (course) => {
          try {
            const studentsResult = await programs.getCoursesUsers(String(course.id), { take: 50 });
            if (!studentsResult.ok || !Array.isArray(studentsResult.data)) {
              return [] as ActivityItem[];
            }

            return (studentsResult.data as StudentProgressDto[])
              .flatMap((student, index) => {
                const studentName = student.userName?.trim() || student.userEmail?.trim() || `Student ${index + 1}`;
                const courseName = course.title ?? 'Untitled course';
                const activities: ActivityItem[] = [];

                if (student.startedAt) {
                  activities.push({
                    type: 'enrollment',
                    studentName,
                    courseName,
                    timestamp: student.startedAt,
                  });
                }

                if (student.completedAt) {
                  activities.push({
                    type: 'completion',
                    studentName,
                    courseName,
                    timestamp: student.completedAt,
                  });
                }

                const hasDistinctLastActivity =
                  Boolean(student.lastAccessedAt) &&
                  student.lastAccessedAt !== student.startedAt &&
                  student.lastAccessedAt !== student.completedAt;

                if (hasDistinctLastActivity && student.lastAccessedAt) {
                  activities.push({
                    type: 'activity',
                    studentName,
                    courseName,
                    timestamp: student.lastAccessedAt,
                  });
                }

                return activities;
              })
              .filter((activity) => !Number.isNaN(Date.parse(activity.timestamp)));
          } catch {
            return [] as ActivityItem[];
          }
        }),
    );

    const activities = activityResults
      .flat()
      .sort((left, right) => Date.parse(right.timestamp) - Date.parse(left.timestamp))
      .slice(0, 20);

    return { activities };
  } catch {
    return { activities: [] };
  }
}
