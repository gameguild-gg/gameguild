import React from 'react';
import { notFound } from 'next/navigation';
import { getToken } from '@/auth';
import {
  createServerClient,
  type LearningAssessmentsAssessmentSubmission,
} from '@game-guild/client';
import { getAssessment } from '@/lib/learning';
import { getCodingAssignmentFull } from '@/lib/coding-assignment/client';
import {
  codePayloadToFiles,
  type CodeFile,
} from '@/lib/coding-assignment/code-payload';
import { GradeClient } from './grade-client';

/**
 * Instructor grading IDE for a coding-assessment submission.
 *
 * Route: `/dashboard/platform/learning/courses/[course]/assessments/[assessmentId]/submissions/[submissionId]/grade`
 *
 * Server Component — fetches:
 *  1. The v1 `CodingAssignmentContent` via the Task 4 wrapper
 *     `getCodingAssignmentFull(programId, contentId)` (instructor view → has
 *     both Public + Private tests + all files).
 *  2. The submission's raw `CodePayload` JSON via the existing
 *     `GET /v1.0/assessments/submissions/{id}` endpoint, parsed by Task 9
 *     `codePayloadToFiles`.
 *
 * Both are passed to {@link GradeClient} which merges them with the
 * Private-collision guard (Metis #30) before seeding the IDE.
 */
export default async function GradeSubmissionPage({
  params,
}: {
  params: Promise<{
    locale: string;
    course: string;
    assessmentId: string;
    submissionId: string;
  }>;
}): Promise<React.JSX.Element> {
  const { course, assessmentId, submissionId } = await params;

  // Translate the Next.js route param into the v1 ProgramContent address.
  // `courseId` IS `programId` in this stack (per Task 8 learnings).
  const assessment = await getAssessment(assessmentId);
  if (!assessment || !assessment.contentId) {
    notFound();
  }

  const [assignment, submission] = await Promise.all([
    getCodingAssignmentFull(assessment.courseId, assessment.contentId),
    fetchSubmission(submissionId),
  ]);

  if (!assignment || !submission) {
    notFound();
  }

  const submittedFiles = parseSubmittedFiles(submission);

  return (
    <GradeClient
      courseSlug={course}
      assessmentId={assessmentId}
      submissionId={submissionId}
      assignment={assignment}
      submittedFiles={submittedFiles}
      maxScore={assignment.Grading.MaxScore}
      manifestUrl="/cdn/manifest.json"
    />
  );
}

/** Parse `AssessmentSubmission.codePayload` into `{path, content}[]`. Tolerant of legacy v0 shape. */
function parseSubmittedFiles(
  submission: LearningAssessmentsAssessmentSubmission,
): CodeFile[] {
  const codePayload = submission.codePayload ?? '';
  if (!codePayload) return [];
  try {
    return codePayloadToFiles(codePayload);
  } catch (err) {
    console.error('Failed to parse submission codePayload:', err);
    return [];
  }
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
    // `getAssessmentsSubmissionsBySubmissionId` is typed `Result<void>` in the
    // generated client (response schema not described in OpenAPI). Use the raw
    // `request<unknown>` channel and cast to the submission shape.
    const result = await client.request<LearningAssessmentsAssessmentSubmission>({
      method: 'GET',
      path: `/v1.0/assessments/submissions/${submissionId}`,
      requiresAuth: true,
    });
    if (!result.ok) return null;
    return result.data;
  } catch (err) {
    console.error('Error fetching submission for grading:', err);
    return null;
  }
}
