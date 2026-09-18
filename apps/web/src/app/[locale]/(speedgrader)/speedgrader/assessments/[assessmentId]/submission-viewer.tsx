'use client';

import { useEffect, useMemo, useState } from 'react';
import type { AssessmentSubmissionRuntimeViewV1 } from '@game-guild/grading';
import {
  parseQuizAnswerEnvelope,
  QUIZ_ASSESSMENT_TYPE_ADAPTER,
  type QuizLearnerDeliveryItemV1,
} from '@game-guild/grading-adapter-quiz';
import { createEmptyQuizAnswer, type QuizAnswer } from '@game-guild/quiz';
import { QuizPlayer, type QuizSubmissionResult } from '@game-guild/quiz-surface/player';
import { Badge } from '@game-guild/ui/components/badge';
import { getRuntimeSubmission } from '@/lib/learning/grading-runtime-actions';
import { scoreUnitsToPoints } from '@/lib/learning/academic-values';

export interface SubmissionViewerProps {
  submissionId: string;
}

/**
 * Instructor view of the exact delivery and response bound to the official
 * grading execution. The authored content is intentionally not consulted.
 */
export function SubmissionViewer({
  submissionId,
}: SubmissionViewerProps): React.JSX.Element {
  const [submission, setSubmission] =
    useState<AssessmentSubmissionRuntimeViewV1 | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setSubmission(null);
    setError(null);
    getRuntimeSubmission(submissionId).then((result) => {
      if (cancelled) return;
      if (result.success) {
        setSubmission(result.data);
      } else {
        setError(result.error);
      }
    });
    return () => {
      cancelled = true;
    };
  }, [submissionId]);

  if (error) {
    return (
      <div
        data-testid="viewer-error"
        role="alert"
        className="p-4 text-sm text-destructive"
      >
        {error}
      </div>
    );
  }
  if (!submission) {
    return (
      <div
        data-testid="viewer-loading"
        className="p-4 text-sm text-muted-foreground"
      >
        Loading submission...
      </div>
    );
  }

  return <RuntimeSubmission submission={submission} />;
}

function RuntimeSubmission({
  submission,
}: {
  submission: AssessmentSubmissionRuntimeViewV1;
}): React.JSX.Element {
  const quiz = useMemo(() => readQuizSubmission(submission), [submission]);

  return (
    <div
      data-testid="submission-viewer"
      className="h-full space-y-4 overflow-auto p-4"
    >
      <div className="flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
        <Badge variant="outline">Attempt {submission.attemptNumber}</Badge>
        <span>Delivery {submission.execution.deliveryHash.slice(0, 12)}</span>
        <span>Revision {submission.definitionRevisionId.slice(0, 8)}</span>
      </div>

      {quiz ? (
        <div className="space-y-4" data-testid="runtime-quiz-submission">
          {quiz.items.map((item, index) => (
            <section
              key={item.itemId}
              className="space-y-3 rounded-md border bg-card p-4"
            >
              <div className="flex items-center justify-between gap-3">
                <h2 className="text-sm font-semibold">Question {index + 1}</h2>
                <Badge variant="secondary">
                  {scoreUnitsToPoints(
                    submission.execution.itemMaxScores[item.itemId] ?? 0,
                  )}{' '}
                  pts
                </Badge>
              </div>
              <QuizPlayer
                entry={item.delivery.entry}
                answer={item.answer}
                onAnswerChange={() => undefined}
                onSubmit={() => undefined}
                submissionResult={item.result}
                disabled
              />
            </section>
          ))}
        </div>
      ) : (
        <GenericRuntimeSubmission submission={submission} />
      )}
    </div>
  );
}

function readQuizSubmission(submission: AssessmentSubmissionRuntimeViewV1): {
  items: Array<{
    itemId: string;
    delivery: QuizLearnerDeliveryItemV1;
    answer: QuizAnswer;
    result: QuizSubmissionResult;
  }>;
} | null {
  const delivery = submission.execution.delivery;
  if (
    delivery.itemOrder.some((itemId) => {
      const item = delivery.items[itemId];
      return (
        !item ||
        item.adapterKey !== QUIZ_ASSESSMENT_TYPE_ADAPTER.key ||
        item.adapterVersion !== QUIZ_ASSESSMENT_TYPE_ADAPTER.version
      );
    })
  ) {
    return null;
  }

  let answers: Record<string, QuizAnswer> = {};
  if (submission.execution.submittedResponse) {
    try {
      answers = parseQuizAnswerEnvelope(
        submission.execution.submittedResponse,
      ).payload.answers;
    } catch {
      return null;
    }
  }

  const resultByItem = new Map(
    (submission.execution.instructorVisibleResult?.items ?? []).map((item) => [
      item.itemId,
      item,
    ]),
  );
  return {
    items: delivery.itemOrder.map((itemId) => {
      const item = delivery.items[itemId]!;
      const learnerPayload = item.learnerPayload as QuizLearnerDeliveryItemV1;
      if (learnerPayload.itemId !== itemId) {
        throw new Error(`Quiz delivery item ${itemId} has a mismatched payload.`);
      }
      const answer =
        answers[itemId] ?? createEmptyQuizAnswer(learnerPayload.entry.type);
      const result = resultByItem.get(itemId);
      return {
        itemId,
        delivery: learnerPayload,
        answer,
        result: toQuizSubmissionResult(result),
      };
    }),
  };
}

function toQuizSubmissionResult(
  item:
    | NonNullable<
        AssessmentSubmissionRuntimeViewV1['execution']['instructorVisibleResult']
      >['items'][number]
    | undefined,
): QuizSubmissionResult {
  if (!item || item.state !== 'graded' || item.score == null) {
    return {
      status: 'pending',
      feedback: 'Awaiting grading.',
    };
  }
  return {
    status: item.score === item.maxScore ? 'correct' : 'incorrect',
    feedback: item.feedback ?? undefined,
  };
}

function GenericRuntimeSubmission({
  submission,
}: {
  submission: AssessmentSubmissionRuntimeViewV1;
}): React.JSX.Element {
  return (
    <div data-testid="runtime-generic-submission" className="space-y-4">
      <div className="rounded-md border bg-card p-4">
        <h2 className="text-sm font-semibold">Immutable delivery</h2>
        <pre className="mt-3 overflow-auto whitespace-pre-wrap text-xs text-muted-foreground">
          {JSON.stringify(submission.execution.delivery, null, 2)}
        </pre>
      </div>
      <div className="rounded-md border bg-card p-4">
        <h2 className="text-sm font-semibold">Submitted response</h2>
        <pre className="mt-3 overflow-auto whitespace-pre-wrap text-xs text-muted-foreground">
          {JSON.stringify(submission.execution.submittedResponse, null, 2)}
        </pre>
      </div>
    </div>
  );
}
