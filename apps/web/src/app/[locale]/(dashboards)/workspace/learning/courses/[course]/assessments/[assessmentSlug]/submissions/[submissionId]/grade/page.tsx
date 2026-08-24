import { notFound } from 'next/navigation';
import { redirect } from '@/i18n/navigation';
import { getToken } from '@/auth';
import { createServerClient, GeneratedApi, type LearningAssessmentsAssessmentSubmission, type LearningAssessmentsGradingQueueItem } from '@game-guild/client';
import { resolveNavIndex } from './resolve-nav-index';

/**
 * Legacy grading route → SpeedGrader redirect shim.
 *
 * Route: `/dashboard/learning/courses/[course]/assessments/[assessmentId]/submissions/[submissionId]/grade`
 *
 * Server-side: load the submission row, load the grading queue, resolve the
 * queue index for this submission, then redirect to
 * `/speedgrader/assessments/{assessmentId}?course=<slug>&nav=<index>`.
 */
export default async function GradeSubmissionPage({
  params,
}: {
  params: Promise<{
    locale: string;
    course: string;
    assessmentSlug: string;
    submissionId: string;
  }>;
}): Promise<void> {
  const { locale, course, submissionId } = await params;

  const submission = await fetchSubmission(submissionId);
  if (!submission?.assessmentId || !submission.id) {
    notFound();
  }
  const assessmentId = submission.assessmentId;

  const items = await fetchQueueItems(assessmentId);
  const index = resolveNavIndex(items, {
    submissionId: submission.id,
    userId: submission.userId ?? undefined,
    attemptNumber: submission.attemptNumber,
  });

  redirect({
    href: `/speedgrader/assessments/${assessmentId}?course=${encodeURIComponent(course)}&nav=${index}`,
    locale,
  });
}

/** Fetch a single submission row via the raw request channel (same as the legacy grade page). */
async function fetchSubmission(submissionId: string): Promise<LearningAssessmentsAssessmentSubmission | null> {
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
    if (!result.ok) return null;
    return result.data;
  } catch (err) {
    console.error('Error fetching submission for redirect:', err);
    return null;
  }
}

async function fetchQueueItems(assessmentId: string): Promise<LearningAssessmentsGradingQueueItem[]> {
  const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080';
  const client = createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
  });
  try {
    const result = await new GeneratedApi.LearningAssessmentsModule(client).getAssessmentsGradingQueue(assessmentId);
    if (!result.ok) return [];
    return result.data.items ?? [];
  } catch {
    return [];
  }
}
