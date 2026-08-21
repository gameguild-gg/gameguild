'use client';

import { useState } from 'react';
import type { CodingAssignmentContent } from '@/lib/coding-assignment/client';
import type { LearningAssessmentsGradingQueue, LearningAssessmentsGradingQueueItem } from '@game-guild/client';
import { SpeedgraderShell } from './speedgrader-shell';
import { SubmissionViewer } from './submission-viewer';
import { GradingPanel } from './grading-panel';
import type { ComputedScore } from './code-grader-panel';

export interface SpeedgraderWorkspaceProps {
  queue: LearningAssessmentsGradingQueue;
  assessmentId: string;
  courseSlug: string;
  initialIndex: number;
  codingAssignment: CodingAssignmentContent | null;
  manifestUrl: string;
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
  codingAssignment,
  manifestUrl,
}: SpeedgraderWorkspaceProps): React.JSX.Element {
  const [computed, setComputed] = useState<(ComputedScore & { submissionId: string }) | null>(null);

  const assessment = queue.assessment ?? {};

  const renderViewer = (item: LearningAssessmentsGradingQueueItem) => (
    <SubmissionViewer
      submissionId={item.submissionId ?? ''}
      codingAssignment={codingAssignment}
      manifestUrl={manifestUrl}
      onComputedScore={
        item.submissionId
          ? (result) =>
              setComputed({
                ...result,
                submissionId: item.submissionId as string,
              })
          : undefined
      }
    />
  );

  const renderGrading = (item: LearningAssessmentsGradingQueueItem) => (
    <GradingPanel
      item={item}
      assessment={assessment}
      computedScore={computed && computed.submissionId === item.submissionId ? { score: computed.score, autoFeedback: computed.autoFeedback } : null}
    />
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
