import React from 'react';
import {
  getCourse,
  getCourseAnalytics,
  getCourseContent,
  getCourseStudents,
  getCourseClasses,
} from '@/lib/learning';

/**
 * Course Detail Layout
 *
 * Shared layout for all course subroutes using Parallel Data Preload Pattern:
 * 1. Fires ALL course data fetches in parallel immediately (preload)
 * 2. Awaits getCourse() for navigation UI (tabs, breadcrumb) — same cached data pages use
 * 3. Child pages call same cached functions — get instant data or await in-flight promise
 * 4. Each subroute has loading.tsx → streams independently as data resolves
 *
 * Conditional Preloading:
 * - After getCourse() resolves, check course.features to preload feature-specific data
 * - e.g., getCourseClasses() only preloaded if course.features.hasClasses = true
 *
 * Result: No duplicate fetches, fast perceived load, incremental streaming.
 */
export default async function Layout({
  children,
  params,
}: LayoutProps<'/[locale]/dashboard/learning/courses/[course]'>): Promise<React.JSX.Element> {
  const { locale, course: courseId } = await params;
  void locale; // Available for i18n if needed

  // ==========================================================================
  // PARALLEL PRELOAD: Fire core fetches immediately (cache warms for child pages)
  // ==========================================================================
  const coursePromise = getCourse(courseId); // Layout will await this one
  getCourseAnalytics(courseId);              // Fire-and-forget preload
  getCourseContent(courseId);                // Fire-and-forget preload
  getCourseStudents(courseId);               // Fire-and-forget preload

  // ==========================================================================
  // LAYOUT DATA: Await course for navigation UI (same cached data pages will use)
  // ==========================================================================
  const course = await coursePromise;

  // ==========================================================================
  // CONDITIONAL PRELOAD: Based on course features (delivery mode / pricing)
  // ==========================================================================
  if (course?.features.hasClasses) {
    getCourseClasses(courseId);              // Fire-and-forget preload
  }
  // Future: if (course?.features.hasPricing) getCoursePricing(courseId);
  // Future: if (course?.features.hasCertificate) getCourseCertificate(courseId);

  // If course doesn't exist, let the page handle 404
  // The layout still renders to allow not-found.tsx to work
  void course; // TODO: Pass to navigation UI components (tabs, breadcrumb)
  // Navigation should conditionally render tabs based on course.features:
  //   - Always: Overview, Content, Students, Settings
  //   - If hasClasses: Classes/Schedule
  //   - If hasPricing: Pricing
  //   - If hasAssessments: Assessments
  //   - If hasCertificate: Certificates

  return <>{children}</>;
}
