'use client';

import type { LearningAssessmentsGradingQueue, LearningAssessmentsGradingQueueItem } from '@game-guild/client';
import type { AssessmentSubmissionRuntimeViewV1 } from '@game-guild/grading';
import { useCallback, useState } from 'react';
import { SpeedgraderShell } from './speedgrader-shell';
import { SubmissionViewer } from './submission-viewer';
import { GradingPanel } from './grading-panel';

export interface SpeedgraderWorkspaceProps {
  queue: LearningAssessmentsGradingQueue;
  assessmentId: string;
  courseSlug: string;
  initialIndex: number;
}

interface ConfirmedReview {
  previousRoundId: string | null;
  roundId: string | null;
  supersededRoundIds: (string | null)[];
  requiresStaffReview: boolean;
  finalized: boolean;
  acknowledged: boolean;
  version: number;
}

function requiresStaffReview(item: LearningAssessmentsGradingQueueItem): boolean {
  return item.activeGradeRoundId != null
    ? item.requiresStaffReview === true
    : item.status === 'Submitted' || item.status === 'Late';
}

function reconcileConfirmations(
  queue: LearningAssessmentsGradingQueue,
  confirmed: Record<string, ConfirmedReview>,
): Record<string, ConfirmedReview> {
  const items = new Map((queue.items ?? []).map((item) => [item.submissionId, item]));
  return Object.fromEntries(Object.entries(confirmed).flatMap<[string, ConfirmedReview]>(([id, review]) => {
    const item = items.get(id);
    if (!item) return [];
    const roundId = item.activeGradeRoundId ?? null;
    if (review.supersededRoundIds.includes(roundId)) return [[id, review]];
    if (roundId === review.roundId) {
      // A finalized round cannot acquire more work. Retain its confirmation
      // after acknowledgement too, so a late refresh cannot resurrect it.
      if (review.finalized) return [[id, review]];
      if (!review.acknowledged && review.previousRoundId === roundId
        && requiresStaffReview(item) !== review.requiresStaffReview
        && item.activeGradeRoundStatus !== 'finalized') return [[id, review]];
    }
    // A new canonical round can introduce work, including an automated stage
    // advancing to instructor review. Remember the old round for late refreshes.
    return [[id, {
      ...review,
      roundId,
      supersededRoundIds: roundId === review.roundId
        ? review.supersededRoundIds
        : [...review.supersededRoundIds, review.roundId],
      requiresStaffReview: requiresStaffReview(item),
      finalized: item.activeGradeRoundStatus === 'finalized',
      acknowledged: true,
    }]];
  }));
}

/**
 * Client wrapper that fills the shell's renderViewer/renderGrading slots.
 * The run-tests computed score flows from the (left) code viewer into the
 * (right) grading panel through here, keyed by the submission it belongs to.
 */
export function SpeedgraderWorkspace({
  queue,
  assessmentId,
  courseSlug,
  initialIndex,
}: SpeedgraderWorkspaceProps): React.JSX.Element {
  const assessment = queue.assessment ?? {};
  const [reviewState, setReviewState] = useState<{
    queue: LearningAssessmentsGradingQueue;
    confirmed: Record<string, ConfirmedReview>;
  }>({
    queue,
    confirmed: {},
  });
  let confirmed = reviewState.confirmed;
  if (reviewState.queue !== queue) {
    confirmed = reconcileConfirmations(queue, confirmed);
    // Reconcile changed server props before rendering children, rather than
    // briefly painting an acknowledged confirmation over a newer round.
    setReviewState({ queue, confirmed });
  }

  const confirmReview = useCallback((submission: AssessmentSubmissionRuntimeViewV1) => {
    if (submission.assessmentId !== assessmentId) return;
    setReviewState((current) => {
      // An action can finish after newer queue props arrive. Read the current
      // queue here instead of retaining the queue captured before its await.
      const item = current.queue.items?.find((entry) => entry.submissionId === submission.submissionId);
      if (!item) return current;
      const prior = current.confirmed[submission.submissionId];
      if (prior && prior.version > submission.version) return current;
      const roundId = submission.execution.activeRoundId ?? null;
      if (prior?.supersededRoundIds.includes(roundId)) return current;
      const currentRoundId = item.activeGradeRoundId ?? null;
      // A late mutation result cannot reopen a round already confirmed final
      // by a newer queue snapshot, including before any local confirmation.
      if ((prior?.roundId === roundId && prior.finalized)
        || (currentRoundId === roundId && item.activeGradeRoundStatus === 'finalized')) return current;
      // Without a recorded confirmation, a response for another round may
      // predate fresh queue props. Its history must establish the ordering.
      if (currentRoundId !== null && currentRoundId !== roundId
        && !submission.execution.history.some((round) => round.roundId === currentRoundId)
        && !(prior?.roundId === roundId && prior.supersededRoundIds.includes(currentRoundId))) return current;
      const supersededRoundIds = new Set(prior?.supersededRoundIds ?? []);
      supersededRoundIds.add(currentRoundId);
      if (prior) supersededRoundIds.add(prior.roundId);
      for (const round of submission.execution.history) supersededRoundIds.add(round.roundId);
      supersededRoundIds.delete(roundId);
      return {
        ...current,
        confirmed: {
          ...current.confirmed,
          [submission.submissionId]: {
            previousRoundId: item.activeGradeRoundId ?? null,
            roundId,
            supersededRoundIds: [...supersededRoundIds],
            requiresStaffReview: submission.execution.requiresInstructorReview,
            finalized: submission.execution.history.some((round) => round.roundId === roundId && round.status === 'finalized'),
            acknowledged: false,
            version: submission.version,
          },
        },
      };
    });
  }, [assessmentId]);

  const needsGrading = Math.max(0, (queue.needsGrading ?? 0) + (queue.items ?? []).reduce((delta, item) => {
    const review = item.submissionId ? confirmed[item.submissionId] : undefined;
    if (!review) return delta;
    return delta + Number(review.requiresStaffReview) - Number(requiresStaffReview(item));
  }, 0));

  const renderViewer = (item: LearningAssessmentsGradingQueueItem) => (
    <SubmissionViewer submissionId={item.submissionId ?? ''} />
  );

  const renderGrading = (item: LearningAssessmentsGradingQueueItem) => (
    <GradingPanel item={item} assessment={assessment} onReviewStateChanged={confirmReview} />
  );

  return (
    <SpeedgraderShell
      assessmentTitle={assessment.title ?? 'Assessment'}
      assessmentId={assessmentId}
      courseSlug={courseSlug}
      items={queue.items ?? []}
      needsGrading={needsGrading}
      initialIndex={initialIndex}
      renderViewer={renderViewer}
      renderGrading={renderGrading}
    />
  );
}
