import type { LearningAssessmentsGradingQueueItem } from '@game-guild/client';

export interface NavResolutionSource {
  submissionId: string;
  userId?: string;
  attemptNumber?: number;
}

/**
 * Resolve the grading-queue index for a submission row.
 *
 * 1. Exact id match (individual rows + canonical group rows).
 * 2. Group fan-out rows (member copies): the submission DTO carries no
 *    CourseGroupId, so match the group item by attempt — unambiguous whenever
 *    only one group submitted at that attempt.
 * 3. User + attempt fallback.
 * 4. nav=0.
 */
export function resolveNavIndex(items: LearningAssessmentsGradingQueueItem[], source: NavResolutionSource): number {
  const exact = items.findIndex((item) => item.submissionId === source.submissionId || item.canonicalSubmissionId === source.submissionId);
  if (exact >= 0) return exact;

  const groupAttempts = items.filter((item) => item.isGroup && item.attemptNumber === source.attemptNumber);
  if (groupAttempts.length === 1) {
    return items.indexOf(groupAttempts[0] as LearningAssessmentsGradingQueueItem);
  }

  const byUser = items.findIndex((item) => item.userId === source.userId && item.attemptNumber === source.attemptNumber);
  if (byUser >= 0) return byUser;

  return 0;
}
