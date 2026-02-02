import React from 'react';
import { notFound } from 'next/navigation';
import { getCourse, getCourseAnalytics } from '@/lib/learning';

/**
 * L4a: Course Overview Page
 *
 * Course dashboard home showing:
 * - Course info (title, description, status, dates)
 * - Analytics: enrollment trend, completion funnel, rating distribution, revenue
 *
 * Data fetching: Parallel (layout already preloaded — hits cache or awaits in-flight)
 * - getCourse(courseId) -> course info (returns null if not found -> 404)
 * - getCourseAnalytics(courseId) -> raw data for analytics computation
 *
 * Analytics are computed client-side from the raw enrollments[], ratings[], revenue[] arrays.
 */
export default async function Page({ params }: PageProps<'/[locale]/dashboard/learning/courses/[course]/overview'>): Promise<React.JSX.Element> {
  const { locale, course: courseId } = await params;
  void locale; // Available for i18n if needed

  // ==========================================================================
  // PARALLEL FETCH: Layout already preloaded — hits cache or awaits in-flight promise
  // ==========================================================================
  const [course, analytics] = await Promise.all([
    getCourse(courseId),
    getCourseAnalytics(courseId),
  ]);

  // Handle course not found
  if (!course) {
    notFound();
  }

  // Computed values (would be calculated in component/util)
  // const enrollmentTrend = groupByDate(analytics.enrollments, 'enrolledAt');
  // const completionFunnel = {
  //   enrolled: analytics.enrollments.length,
  //   started: analytics.enrollments.filter(e => e.progress > 0).length,
  //   completed: analytics.enrollments.filter(e => e.completedAt).length,
  // };
  // const ratingDistribution = groupByScore(analytics.ratings);
  // const totalRevenue = analytics.revenue.reduce((acc, r) => acc + r.amount, 0);
  // const revenueOverTime = groupByDate(analytics.revenue, 'createdAt');

  void course; // TODO: Pass to UI components
  void analytics; // TODO: Pass to UI components

  return <></>;
}
