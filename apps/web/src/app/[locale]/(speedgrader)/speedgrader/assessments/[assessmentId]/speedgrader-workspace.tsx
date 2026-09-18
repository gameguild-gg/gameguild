'use client';

import type { LearningAssessmentsGradingQueue, LearningAssessmentsGradingQueueItem } from '@game-guild/client';
import { SpeedgraderShell } from './speedgrader-shell';
import { SubmissionViewer } from './submission-viewer';
import { GradingPanel } from './grading-panel';

export interface SpeedgraderWorkspaceProps {
  queue: LearningAssessmentsGradingQueue;
  assessmentId: string;
  courseSlug: string;
  initialIndex: number;
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

  const renderViewer = (item: LearningAssessmentsGradingQueueItem) => (
    <SubmissionViewer submissionId={item.submissionId ?? ''} />
  );

  const renderGrading = (item: LearningAssessmentsGradingQueueItem) => (
    <GradingPanel item={item} assessment={assessment} />
  );

  return (
    <SpeedgraderShell
      assessmentTitle={assessment.title ?? 'Assessment'}
      assessmentId={assessmentId}
      courseSlug={courseSlug}
      items={queue.items ?? []}
      needsGrading={queue.needsGrading ?? 0}
      initialIndex={initialIndex}
      renderViewer={renderViewer}
      renderGrading={renderGrading}
    />
  );
}
