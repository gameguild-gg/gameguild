import React from 'react';
import { notFound } from 'next/navigation';
import { getAssessment, getAssessmentDefinition, getCourseAssessmentGroups } from '@/lib/learning';
import { AssessmentEditor } from './assessment-editor';

/**
 * Assessment Detail/Editor Page
 *
 * Route: /courses/[course]/assessments/[assessmentId]
 */
export default async function AssessmentDetailPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/assessments/[assessmentId]'>): Promise<React.JSX.Element> {
  const { course: courseId, assessmentId } = await params;

  const [assessment, assessmentDefinition, assessmentGroups] = await Promise.all([
    getAssessment(assessmentId),
    getAssessmentDefinition(assessmentId),
    getCourseAssessmentGroups(courseId),
  ]);

  if (!assessment) {
    notFound();
  }

  return (
    <AssessmentEditor
      courseId={courseId}
      assessment={assessment}
      assessmentDefinition={assessmentDefinition}
      assessmentGroups={assessmentGroups}
    />
  );
}
