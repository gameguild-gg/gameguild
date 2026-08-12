import React from 'react';
import { getCourseAssessmentAnalytics, getCourseAssessmentGroups, getCourseAssessments } from '@/lib/learning';
import { AssessmentsList } from './assessments-list';

/**
 * Assessments List Page
 *
 * Route: /courses/[course]/assessments
 * Condition: course.features.hasAssessments = true (checked in layout)
 */
export default async function AssessmentsPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/assessments'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  const [{ assessments, total }, assessmentGroups, analytics] = await Promise.all([
    getCourseAssessments(courseId),
    getCourseAssessmentGroups(courseId),
    getCourseAssessmentAnalytics(courseId),
  ]);

  return (
    <AssessmentsList
      courseId={courseId}
      assessments={assessments}
      total={total}
      assessmentGroups={assessmentGroups}
      analytics={analytics}
    />
  );
}
