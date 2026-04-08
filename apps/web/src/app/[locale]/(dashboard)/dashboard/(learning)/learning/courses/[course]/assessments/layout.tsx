import React from 'react';
import { notFound, forbidden } from 'next/navigation';
import { getCourse, getCourseAssessments } from '@/lib/learning';

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
}: {
  children: React.ReactNode;
  params: Promise<{ locale: string; course: string }>;
}): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  const course = await getCourse(courseId);
  
  if (!course) {
    notFound();
  }
  
  if (!course.features.hasAssessments) {
    forbidden();
  }

  // Preload assessments
  getCourseAssessments(courseId);

  void course;

  return <>{children}</>;
}
