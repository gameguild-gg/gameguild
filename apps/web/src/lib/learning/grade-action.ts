'use server';

import { getToken } from '@/auth';
import {
  createServerClient,
  GeneratedApi,
} from '@game-guild/client';

type ActionResult<T> =
  | { success: true; data: T }
  | { success: false; error: string };

function getApiClient() {
  const apiUrl =
    process.env.API_URL ||
    process.env.NEXT_PUBLIC_API_URL ||
    'http://localhost:8080';
  return createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
  });
}

/**
 * Post an instructor grade for an assessment submission.
 *
 * Backend `POST /v1.0/assessments/submissions/{id}/grade` enforces
 * `CanReviewCourseAsync`; the actor's `gradedBy` is taken from auth context,
 * not the request body, so the body's `gradedBy` is omitted.
 */
export async function gradeSubmission(input: {
  submissionId: string;
  score: number;
  feedback: string;
}): Promise<ActionResult<{ submissionId: string }>> {
  const client = getApiClient();
  const assessments = new GeneratedApi.LearningAssessmentsModule(client);
  const result = await assessments.postAssessmentsSubmissionsGrade(
    input.submissionId,
    {
      score: input.score,
      feedback: input.feedback,
    },
  );
  if (!result.ok) {
    return {
      success: false,
      error: result.error?.message ?? 'Failed to post grade.',
    };
  }
  return {
    success: true,
    data: { submissionId: input.submissionId },
  };
}
