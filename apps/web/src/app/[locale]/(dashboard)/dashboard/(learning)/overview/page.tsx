import React from 'react';
import { getInstructorStats, getRecentActivity } from '@/lib/learning';

/**
 * L1a: Learning Overview Page
 *
 * Instructor dashboard home showing:
 * - KPIs: total courses, total students, avg completion rate, avg rating
 * - Recent activity feed (enrollments, completions, reviews)
 *
 * Data fetching: Sequential (stats first, then activity)
 * - getInstructorStats() -> courses summary for KPI computation
 * - getRecentActivity() -> activity feed
 */
export default async function Page({ params }: PageProps<'/[locale]/dashboard/learning/overview'>): Promise<React.JSX.Element> {
  const { locale } = await params;
  void locale; // Available for i18n if needed

  // Sequential data fetching
  const stats = await getInstructorStats();
  const activity = await getRecentActivity();

  // Computed values (would be calculated from stats.courses)
  // const totalCourses = stats.courses.length;
  // const totalStudents = new Set(stats.courses.flatMap(c => c.enrollments.map(e => e.id))).size;
  // const avgCompletionRate = stats.courses.reduce((acc, c) => acc + (c.completions.length / c.enrollments.length || 0), 0) / stats.courses.length;
  // const avgRating = stats.courses.flatMap(c => c.ratings).reduce((acc, r) => acc + r.score, 0) / stats.courses.flatMap(c => c.ratings).length;

  void stats; // TODO: Pass to UI components
  void activity; // TODO: Pass to UI components

  return <></>;
}
