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

  const [{ assessments, total }, assessmentGroups, analytics, content] = await Promise.all([
    getCourseAssessments(courseId),
    getCourseAssessmentGroups(courseId),
    getCourseAssessmentAnalytics(courseId),
    getCourseContent(courseId),
  ]);

  const gradedContentItems = content.items.filter((item) => item.gradingConfig?.enabled);

  return (
    <AssessmentsList
      courseId={courseId}
      assessments={assessments}
      total={total}
      gradedContentItems={gradedContentItems}
      assessmentGroups={assessmentGroups}
      analytics={analytics}
    />
  );
}
