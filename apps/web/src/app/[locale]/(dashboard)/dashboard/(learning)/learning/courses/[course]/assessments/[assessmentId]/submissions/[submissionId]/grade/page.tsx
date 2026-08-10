import React from 'react';
import { notFound } from 'next/navigation';
import { getToken } from '@/auth';
import { createServerClient, type LearningAssessmentsAssessmentSubmission } from '@game-guild/client';
import { getCodingDefinitionFull } from '@/lib/emception/get-coding-definition-full';
import { codePayloadToFiles, type CodeFile } from '@/lib/emception/code-payload';
import { GradeClient } from './grade-client';

/**
 * Instructor grading view for a coding assessment submission.
 *
 * Route: /dashboard/learning/courses/[course]/assessments/[assessmentId]/submissions/[submissionId]/grade
 *
 * The route group (dashboard)/(learning) already enforces auth + tenant
 * middleware; the API additionally enforces CanReviewCourseAsync on both the
 * /coding-definition/full fetch and the eventual POST .../grade.
 */
export default async function GradeSubmissionPage({
  params,
}: {
  params: Promise<{ locale: string; course: string; assessmentId: string; submissionId: string }>;
}): Promise<React.JSX.Element> {
  const { course, assessmentId, submissionId } = await params;

  const [definition, submission] = await Promise.all([
    getCodingDefinitionFull(assessmentId),
    fetchSubmission(submissionId),
  ]);

  if (!definition || !submission) {
    notFound();
  }

  const codePayload = submission.codePayload ?? '';
  let initialFiles: CodeFile[] = [];
  if (codePayload) {
    try {
      initialFiles = codePayloadToFiles(codePayload);
    } catch (err) {
      console.error('Failed to parse submission codePayload:', err);
    }
  }

  return (
    <GradeClient
      courseSlug={course}
      assessmentId={assessmentId}
      submissionId={submissionId}
      initialFiles={initialFiles}
      workspaceConfig={definition.workspaceConfig}
      testPlan={definition.testPlan}
      maxScore={definition.maxScore}
      passingScore={definition.passingScore}
      manifestUrl="/cdn/manifest.json"
    />
  );
}

/** Fetch a single submission including its codePayload. */
async function fetchSubmission(
  submissionId: string,
): Promise<LearningAssessmentsAssessmentSubmission | null> {
  const apiUrl =
    process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080';
  const client = createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
  });
  try {
    const result = await client.request<LearningAssessmentsAssessmentSubmission>({
      method: 'GET',
      path: `/v1.0/assessments/submissions/${submissionId}`,
    });
    if (!result.ok) return null;
    return result.data;
  } catch (err) {
    console.error('Error fetching submission for grading:', err);
    return null;
  }
}
