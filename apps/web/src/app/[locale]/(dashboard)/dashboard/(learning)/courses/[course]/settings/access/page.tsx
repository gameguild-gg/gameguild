import React from 'react';
import { getCourseAccessSettings } from '@/lib/learning';

/**
 * Access Settings Page
 *
 * Route: /courses/[course]/settings/access
 */
export default async function AccessSettingsPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/settings/access'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  const access = await getCourseAccessSettings(courseId);

  // ==========================================================================
  // DATA: CourseAccessSettings
  // visibility, password, enrollmentType, maxEnrollments
  // enrollmentStart/End, requiresVerification, allowedDomains
  // prerequisiteCourses, completionCriteria, completionThreshold
  // ==========================================================================
  void access;

  return <div>Access Settings Page - UI not implemented</div>;
}
