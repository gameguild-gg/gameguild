import { getCourse, getCourseAssessments } from '@/lib/learning';
import { notFound } from 'next/navigation';
import React from 'react';

/**
 * Assessments Layout
 *
 * Routes:
 * - /assessments - Assessment list
 * - /assessments/[assessmentId] - Assessment editor
 *
 * Condition: course.features.hasAssessments = true
 */
export default async function AssessmentsLayout({
  children,
  params,
}: LayoutProps<'/[locale]/console/learning/courses/[course]/assessments'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  const course = await getCourse(courseId);

  if (!course || !course.features.hasAssessments) {
    notFound();
  }

  // Preload assessments
  getCourseAssessments(courseId);

  return <>{children}</>;
}
