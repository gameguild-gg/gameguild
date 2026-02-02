import React from 'react';
import { getCourses } from '@/lib/learning';

/**
 * L2: Courses List Page
 *
 * Displays all courses the instructor can manage with:
 * - Course cards (thumbnail, title, status, visibility)
 * - Simplified KPIs per course (enrolled count, completion %, avg rating)
 *
 * Data fetching: Single REST call
 * - getCourses() -> full list with data for KPI computation
 *
 * KPIs are computed client-side from the enrollments[] and ratings[] arrays.
 */
export default async function Page({ params }: PageProps<'/[locale]/dashboard/learning/courses'>): Promise<React.JSX.Element> {
  const { locale } = await params;
  void locale; // Available for i18n if needed

  // Data fetching
  const { courses } = await getCourses();

  // Computed values per course (would be calculated in component/util)
  // courses.map(course => ({
  //   ...course,
  //   enrolledCount: course.enrollments.length,
  //   completionPercent: (course.enrollments.filter(e => e.completedAt).length / course.enrollments.length) * 100 || 0,
  //   avgRating: course.ratings.reduce((acc, r) => acc + r.score, 0) / course.ratings.length || 0,
  // }));

  void courses; // TODO: Pass to UI components

  return <></>;
}
