// =============================================================================
// INSTRUCTOR DATA QUERIES
// =============================================================================

/**
 * Instructor course summary for KPI computation
 */
export interface InstructorCourseSummary {
  id: string;
  title: string;
  status: 'draft' | 'published' | 'archived';
  enrollments: Array<{ id: string; enrolledAt: string }>;
  completions: Array<{ id: string; completedAt: string }>;
  ratings: Array<{ score: number }>;
}

/**
 * Activity feed item
 */
export interface ActivityItem {
  type: 'enrollment' | 'completion' | 'review' | 'comment';
  studentName: string;
  courseName: string;
  timestamp: string;
}

/**
 * Fetch instructor stats for the learning dashboard overview.
 *
 * @returns Instructor statistics scoped by auth context (tenant + user permissions)
 *
 * Fetch Type: GraphQL
 * Cache: revalidate 120s
 * Endpoint: TBD - GraphQL query `instructorStats`
 *
 * Data returned:
 * - courses[] (id, title, status, enrollments[], completions[])
 *
 * Computed client-side:
 * - totalCourses: courses.length
 * - totalStudents: sum of unique enrollments across courses
 * - avgCompletionRate: avg(completions.length / enrollments.length) per course
 * - avgRating: avg of all course ratings
 */
export async function getInstructorStats(): Promise<{
  courses: InstructorCourseSummary[];
}> {
  // TODO: Implement GraphQL fetch
  // const query = gql`
  //   query InstructorStats {
  //     instructorStats {
  //       courses {
  //         id
  //         title
  //         status
  //         enrollments { id enrolledAt }
  //         completions { id completedAt }
  //         ratings { score }
  //       }
  //     }
  //   }
  // `;
  // return graphqlClient.request(query, {}, { next: { revalidate: 120 } });

  return { courses: [] };
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
  // TODO: Implement GraphQL fetch
  // const query = gql`
  //   query RecentActivity($limit: Int) {
  //     recentActivity(limit: $limit) {
  //       type
  //       studentName
  //       courseName
  //       timestamp
  //     }
  //   }
  // `;
  // return graphqlClient.request(query, { limit: 20 }, { next: { revalidate: 60 } });

  return { activities: [] };
}
