import React from 'react';
import { getCourseAssessmentAnalytics, getCourseAssessmentGroups, getCourseAssessments, getCourseContent } from '@/lib/learning';
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

  const [{ assessments, total }, content, assessmentGroups, analytics] = await Promise.all([
    getCourseAssessments(courseId),
    getCourseContent(courseId),
    getCourseAssessmentGroups(courseId),
    getCourseAssessmentAnalytics(courseId),
  ]);

  return (
    <AssessmentsList
      courseId={courseId}
      assessments={assessments}
      total={total}
      contentItems={content.items}
      assessmentGroups={assessmentGroups}
      analytics={analytics}
    />
  );
}
