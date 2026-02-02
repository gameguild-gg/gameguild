// =============================================================================
// COURSES LIST DATA QUERIES
// =============================================================================

/**
 * Course list item with data for KPI computation
 */
export interface CourseListItem {
  id: string;
  title: string;
  thumbnail: string | null;
  status: 'draft' | 'published' | 'archived';
  visibility: 'public' | 'private' | 'unlisted';
  enrollments: Array<{ id: string; completedAt: string | null }>;
  ratings: Array<{ score: number }>;
}

/**
 * Fetch courses list for the instructor.
 *
 * @returns Courses the user can manage, scoped by auth context
 *
 * Fetch Type: REST
 * Cache: revalidate 60s
 * Endpoint: GET /api/learning/courses
 *
 * Data returned:
 * - courses[] (id, title, thumbnail, status, visibility, enrollments[], ratings[])
 *
 * Computed client-side per course:
 * - enrolledCount: enrollments.length
 * - completionPercent: completions.length / enrollments.length * 100
 * - avgRating: avg(ratings[].score)
 */
export async function getCourses(): Promise<{
  courses: CourseListItem[];
}> {
  // TODO: Implement REST fetch
  // const response = await fetch(`${API_BASE_URL}/api/learning/courses`, {
  //   next: { revalidate: 60 },
  //   headers: { Authorization: `Bearer ${token}` },
  // });
  // if (!response.ok) throw new Error('Failed to fetch courses');
  // return response.json();

  return { courses: [] };
}
