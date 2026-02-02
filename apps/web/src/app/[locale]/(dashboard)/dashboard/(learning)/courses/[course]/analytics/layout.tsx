import React from 'react';
import { notFound } from 'next/navigation';
import { getCourse, getCourseEngagementAnalytics, getCourseCompletionAnalytics, getCourseRevenueAnalytics } from '@/lib/learning';

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
}: {
  children: React.ReactNode;
  params: Promise<{ locale: string; course: string }>;
}): Promise<React.JSX.Element> {
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

  void course;

  return <>{children}</>;
}
