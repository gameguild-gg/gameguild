import { getCourse, getCourseCompletionAnalytics, getCourseEngagementAnalytics, getCourseRevenueAnalytics } from '@/lib/learning';
import { notFound } from 'next/navigation';
import React from 'react';

/**
 * Analytics Group Layout
 *
 * Shared layout for detailed analytics subroutes.
 *
 * Routes:
 * - /analytics (redirect → /analytics/engagement)
 * - /analytics/engagement - Activity, views, session duration
 * - /analytics/completion - Completion rates, drop-off points
 * - /analytics/revenue - Revenue metrics (conditional: hasPricing)
 */
export default async function AnalyticsLayout({
  children,
  params,
}: LayoutProps<'/[locale]/workspace/learning/courses/[course]/analytics'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  const course = await getCourse(courseId);
  if (!course) {
    notFound();
  }

  // Preload analytics data
  getCourseEngagementAnalytics(courseId);
  getCourseCompletionAnalytics(courseId);

  if (course.features.hasPricing) {
    getCourseRevenueAnalytics(courseId);
  }

  return <>{children}</>;
}
