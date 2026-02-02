import React from 'react';
import { getCourseAssessments } from '@/lib/learning';

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

  const assessments = await getCourseAssessments(courseId);

  // ==========================================================================
  // DATA: CourseAssessments
  // assessments: [{ id, title, type, status, passingScore, questionCount, submissionCount, avgScore }]
  // Types: quiz, exam, assignment, project, peer-review
  // ==========================================================================
  void assessments;

  return <div>Assessments Page - UI not implemented</div>;
}
