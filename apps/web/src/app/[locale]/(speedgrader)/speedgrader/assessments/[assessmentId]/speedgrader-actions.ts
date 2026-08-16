'use server';

import { getToken } from '@/auth';
import {
  createServerClient,
  GeneratedApi,
  type LearningAssessmentsAssessmentSubmission,
  type LearningAssessmentsInstructorPeerReview,
} from '@game-guild/client';

export type SubmissionResult = { ok: true; submission: LearningAssessmentsAssessmentSubmission } | { ok: false; error: string };

export type PeerReviewsResult = { ok: true; reviews: LearningAssessmentsInstructorPeerReview[] } | { ok: false; error: string };

/**
 * Fetch one submission (with payloads) for the SpeedGrader viewer.
 *
 * `getAssessmentsSubmissionsBySubmissionId` is typed `Result<void>` in the
 * generated client (response schema not described in OpenAPI) — same raw
 * request channel as the legacy grade page.
 */
export async function fetchSubmissionAction(submissionId: string): Promise<SubmissionResult> {
  const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080';
  const client = createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
  });
  try {
    const result = await client.request<LearningAssessmentsAssessmentSubmission>({
      method: 'GET',
      path: `/v1.0/assessments/submissions/${submissionId}`,
      requiresAuth: true,
    });
    if (!result.ok) {
      return { ok: false, error: 'Failed to load the submission.' };
    }
    return { ok: true, submission: result.data };
  } catch {
    return { ok: false, error: 'Failed to load the submission.' };
  }
}

/** Instructor-named peer reviews for a submission (todo 8d endpoint). */
export async function fetchPeerReviewsAction(submissionId: string): Promise<PeerReviewsResult> {
  const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080';
  const client = createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
  });
  try {
    const reviews = new GeneratedApi.LearningAssessmentsPeerReviewsModule(client);
    const result = await reviews.getAssessmentsSubmissionsPeerReviews(submissionId);
    if (!result.ok) {
      return { ok: false, error: 'Failed to load peer reviews.' };
    }
    return { ok: true, reviews: result.data ?? [] };
  } catch {
    return { ok: false, error: 'Failed to load peer reviews.' };
  }
}
