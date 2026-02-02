import React from 'react';
import { notFound } from 'next/navigation';
import { getAssessment } from '@/lib/learning';

/**
 * Assessment Detail/Editor Page
 *
 * Route: /courses/[course]/assessments/[assessmentId]
 */
export default async function AssessmentDetailPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/assessments/[assessmentId]'>): Promise<React.JSX.Element> {
  const { assessmentId } = await params;

  const assessment = await getAssessment(assessmentId);

  if (!assessment) {
    notFound();
  }

  // ==========================================================================
  // DATA: AssessmentDetail
  // title, description, type, passingScore, maxScore, timeLimit, attempts
  // questions: [{ type, question, points, options, correctAnswer, rubric }]
  // settings: { shuffleQuestions, shuffleOptions, showResults, allowReview, proctored }
  // ==========================================================================
  void assessment;

  return <div>Assessment Editor Page - UI not implemented</div>;
}
