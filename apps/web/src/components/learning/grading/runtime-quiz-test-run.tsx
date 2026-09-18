'use client';

import { useMemo, useRef, useState } from 'react';
import type {
  AssessmentTestRunRuntimeViewV1,
  InstructorReviewResolutionV1,
  ScoreValue,
} from '@game-guild/grading';
import {
  createQuizAnswerEnvelope,
  parseQuizAnswerEnvelope,
  QUIZ_ASSESSMENT_TYPE_ADAPTER,
  type QuizLearnerDeliveryItemV1,
} from '@game-guild/grading-adapter-quiz';
import { createEmptyQuizAnswer, type QuizAnswer } from '@game-guild/quiz';
import { QuizPlayer, type QuizSubmissionResult } from '@game-guild/quiz-surface/player';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@game-guild/ui/components/dialog';
import { Input } from '@game-guild/ui/components/input';
import { Textarea } from '@game-guild/ui/components/textarea';
import {
  AlertTriangle,
  CheckCircle2,
  Loader2,
  Play,
  RotateCcw,
} from 'lucide-react';
import {
  restartAssessmentTestRun,
  resolveTestRunInstructorReview,
  startAssessmentTestRun,
  submitAssessmentTestRun,
} from '@/lib/learning/grading-runtime-actions';
import {
  pointsToScoreUnits,
  scoreUnitsToPoints,
} from '@/lib/learning/academic-values';

export interface RuntimeQuizTestRunProps {
  assessmentId: string;
  revisionId: string;
  disabled?: boolean;
}

interface ScoreDraft {
  points: string;
  feedback: string;
}

export function RuntimeQuizTestRun({
  assessmentId,
  revisionId,
  disabled = false,
}: RuntimeQuizTestRunProps): React.JSX.Element {
  const [open, setOpen] = useState(false);
  const [run, setRun] =
    useState<AssessmentTestRunRuntimeViewV1<QuizLearnerDeliveryItemV1> | null>(
      null,
    );
  const [answers, setAnswers] = useState<Record<string, QuizAnswer>>({});
  const [recorded, setRecorded] = useState<ReadonlySet<string>>(new Set());
  const [scores, setScores] = useState<Record<string, ScoreDraft>>({});
  const [overallFeedback, setOverallFeedback] = useState('');
  const [overrideReason, setOverrideReason] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const startKey = useRef(createIdempotencyKey());
  const submitCommand = useRef<{ hash: string; key: string } | null>(null);

  const items = useMemo(() => (run ? readQuizItems(run) : []), [run]);
  const scoreRows = items.map((item) => {
    const raw = scores[item.itemId]?.points ?? '';
    let score: ScoreValue | null = null;
    try {
      if (raw.trim()) score = pointsToScoreUnits(raw);
    } catch {
      score = null;
    }
    const maxScore = run?.execution.itemMaxScores[item.itemId] ?? 0;
    return {
      ...item,
      raw,
      score,
      maxScore,
      valid: score != null && score >= 0 && score <= maxScore,
    };
  });
  const canResolve =
    run?.execution.requiresInstructorReview === true &&
    scoreRows.length > 0 &&
    scoreRows.every((row) => row.valid) &&
    !busy;

  async function begin() {
    setBusy(true);
    setError(null);
    const result = await startAssessmentTestRun(
      assessmentId,
      revisionId,
      startKey.current,
      'instructor-test-learner',
      'Instructor test learner',
    );
    setBusy(false);
    if (!result.success) {
      setError(result.error);
      return;
    }
    adoptRun(
      result.data as AssessmentTestRunRuntimeViewV1<QuizLearnerDeliveryItemV1>,
    );
  }

  async function submit() {
    if (!run || run.execution.submittedResponse) return;
    const response = createQuizAnswerEnvelope(
      Object.fromEntries(
        items.map((item) => [
          item.itemId,
          answers[item.itemId] ?? createEmptyQuizAnswer(item.delivery.entry.type),
        ]),
      ),
    );
    const hash = JSON.stringify(response);
    if (submitCommand.current?.hash !== hash) {
      submitCommand.current = { hash, key: createIdempotencyKey() };
    }
    setBusy(true);
    setError(null);
    const result = await submitAssessmentTestRun(
      run.testRunId,
      response,
      submitCommand.current.key,
    );
    setBusy(false);
    if (!result.success) {
      setError(result.error);
      return;
    }
    adoptRun(
      result.data as AssessmentTestRunRuntimeViewV1<QuizLearnerDeliveryItemV1>,
    );
  }

  async function resolveInstructorReview() {
    if (!run || !canResolve) return;
    const resolution: InstructorReviewResolutionV1 = {
      schemaVersion: 1,
      items: scoreRows.map((row) => ({
        itemId: row.itemId,
        score: row.score!,
        feedback: scores[row.itemId]?.feedback.trim() || null,
      })),
      feedback: overallFeedback.trim() || null,
      overrideReason: overrideReason.trim() || null,
    };
    setBusy(true);
    setError(null);
    const result = await resolveTestRunInstructorReview(
      run.testRunId,
      resolution,
      createIdempotencyKey(),
    );
    setBusy(false);
    if (!result.success) {
      setError(result.error);
      return;
    }
    adoptRun(
      result.data as AssessmentTestRunRuntimeViewV1<QuizLearnerDeliveryItemV1>,
    );
  }

  async function restart() {
    if (!run) return;
    setBusy(true);
    setError(null);
    const result = await restartAssessmentTestRun(
      run.testRunId,
      createIdempotencyKey(),
    );
    setBusy(false);
    if (!result.success) {
      setError(result.error);
      return;
    }
    submitCommand.current = null;
    adoptRun(
      result.data as AssessmentTestRunRuntimeViewV1<QuizLearnerDeliveryItemV1>,
    );
  }

  function adoptRun(
    next: AssessmentTestRunRuntimeViewV1<QuizLearnerDeliveryItemV1>,
  ) {
    setRun(next);
    const nextItems = readQuizItems(next);
    let nextAnswers: Record<string, QuizAnswer> = {};
    if (next.execution.submittedResponse) {
      nextAnswers = parseQuizAnswerEnvelope(next.execution.submittedResponse)
        .payload.answers;
    } else {
      nextAnswers = Object.fromEntries(
        nextItems.map((item) => [
          item.itemId,
          createEmptyQuizAnswer(item.delivery.entry.type),
        ]),
      );
    }
    setAnswers(nextAnswers);
    setRecorded(
      next.execution.submittedResponse
        ? new Set(nextItems.map((item) => item.itemId))
        : new Set(),
    );

    const resultByItem = new Map(
      (next.execution.instructorVisibleResult?.items ?? []).map((item) => [
        item.itemId,
        item,
      ]),
    );
    setScores(
      Object.fromEntries(
        nextItems.map((item) => {
          const result = resultByItem.get(item.itemId);
          return [
            item.itemId,
            {
              points:
                result?.score == null
                  ? ''
                  : String(scoreUnitsToPoints(result.score)),
              feedback: result?.feedback ?? '',
            },
          ];
        }),
      ),
    );
    setOverallFeedback(next.execution.instructorVisibleResult?.feedback ?? '');
    setOverrideReason('');
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button type="button" size="sm" variant="outline" disabled={disabled}>
          <Play className="mr-2 size-4" />
          Test assessment
        </Button>
      </DialogTrigger>
      <DialogContent className="flex h-[min(90vh,900px)] w-[calc(100vw-2rem)] max-w-6xl flex-col gap-0 overflow-hidden p-0">
        <DialogHeader className="border-b px-6 py-4">
          <div className="flex flex-wrap items-center gap-2">
            <DialogTitle>Assessment test run</DialogTitle>
            {run && <RunStatus run={run} />}
          </div>
          <DialogDescription>
            Exercises the candidate revision as a synthetic learner without
            creating academic results.
          </DialogDescription>
        </DialogHeader>

        <div className="min-h-0 flex-1 overflow-auto p-6">
          {!run && (
            <div className="grid min-h-64 place-items-center">
              <Button type="button" onClick={() => void begin()} disabled={busy}>
                {busy ? (
                  <Loader2 className="mr-2 size-4 animate-spin" />
                ) : (
                  <Play className="mr-2 size-4" />
                )}
                Start test run
              </Button>
            </div>
          )}

          {run && (
            <div className="space-y-6">
              <div className="flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
                <Badge variant="outline">
                  Revision {run.definitionRevisionId.slice(0, 8)}
                </Badge>
                <span>Delivery {run.execution.deliveryHash.slice(0, 12)}</span>
                <span>{run.personaDisplayName}</span>
              </div>

              <div className="space-y-4">
                {items.map((item, index) => (
                  <section
                    key={item.itemId}
                    className="space-y-3 rounded-md border bg-card p-4"
                  >
                    <div className="flex items-center justify-between gap-3">
                      <h3 className="text-sm font-semibold">
                        Question {index + 1}
                      </h3>
                      <Badge variant="secondary">
                        {scoreUnitsToPoints(
                          run.execution.itemMaxScores[item.itemId] ?? 0,
                        )}{' '}
                        pts
                      </Badge>
                    </div>
                    <QuizPlayer
                      entry={item.delivery.entry}
                      answer={
                        answers[item.itemId] ??
                        createEmptyQuizAnswer(item.delivery.entry.type)
                      }
                      onAnswerChange={(answer) =>
                        setAnswers((current) => ({
                          ...current,
                          [item.itemId]: answer,
                        }))
                      }
                      onSubmit={(answer) => {
                        setAnswers((current) => ({
                          ...current,
                          [item.itemId]: answer,
                        }));
                        setRecorded((current) =>
                          new Set([...current, item.itemId]),
                        );
                      }}
                      submissionResult={readPlayerResult(run, item.itemId, recorded)}
                      disabled={Boolean(run.execution.submittedResponse)}
                    />
                  </section>
                ))}
              </div>

              {!run.execution.submittedResponse && (
                <Button type="button" onClick={() => void submit()} disabled={busy}>
                  {busy && <Loader2 className="mr-2 size-4 animate-spin" />}
                  Run grading workflow
                </Button>
              )}

              {run.execution.requiresInstructorReview && (
                <section className="space-y-4 border-t pt-6">
                  <div>
                    <h3 className="font-semibold">Instructor review</h3>
                    <p className="text-sm text-muted-foreground">
                      Resolve every item. The server calculates the total.
                    </p>
                  </div>
                  {scoreRows.map((row) => (
                    <div
                      key={row.itemId}
                      className="grid gap-3 rounded-md border p-3 md:grid-cols-[minmax(0,1fr)_8rem]"
                    >
                      <div className="space-y-2">
                        <p className="text-sm font-medium">
                          {row.delivery.entry.stem || row.itemId}
                        </p>
                        <Textarea
                          aria-label={`Feedback for ${row.itemId}`}
                          value={scores[row.itemId]?.feedback ?? ''}
                          onChange={(event) =>
                            setScores((current) => ({
                              ...current,
                              [row.itemId]: {
                                ...(current[row.itemId] ?? { points: '' }),
                                feedback: event.target.value,
                              },
                            }))
                          }
                          rows={2}
                        />
                      </div>
                      <div className="space-y-2">
                        <label
                          htmlFor={`test-score-${row.itemId}`}
                          className="text-sm font-medium"
                        >
                          Score / {scoreUnitsToPoints(row.maxScore)}
                        </label>
                        <Input
                          id={`test-score-${row.itemId}`}
                          type="number"
                          min={0}
                          max={scoreUnitsToPoints(row.maxScore)}
                          step="0.01"
                          value={row.raw}
                          onChange={(event) =>
                            setScores((current) => ({
                              ...current,
                              [row.itemId]: {
                                ...(current[row.itemId] ?? { feedback: '' }),
                                points: event.target.value,
                              },
                            }))
                          }
                        />
                      </div>
                    </div>
                  ))}
                  <Textarea
                    aria-label="Overall test feedback"
                    value={overallFeedback}
                    onChange={(event) => setOverallFeedback(event.target.value)}
                    placeholder="Overall feedback"
                    rows={3}
                  />
                  <Input
                    aria-label="Override reason"
                    value={overrideReason}
                    onChange={(event) => setOverrideReason(event.target.value)}
                    placeholder="Reason when changing an automated result"
                  />
                  <Button
                    type="button"
                    onClick={() => void resolveInstructorReview()}
                    disabled={!canResolve}
                  >
                    {busy && <Loader2 className="mr-2 size-4 animate-spin" />}
                    Complete instructor review
                  </Button>
                </section>
              )}

              {run.execution.instructorVisibleResult?.state === 'final' && (
                <section className="space-y-3 rounded-md border bg-muted/30 p-4">
                  <div className="flex flex-wrap items-center justify-between gap-3">
                    <div>
                      <p className="font-semibold">Diagnostic result</p>
                      <p className="text-sm text-muted-foreground">
                        {scoreUnitsToPoints(
                          run.execution.instructorVisibleResult.score ?? 0,
                        )}{' '}
                        /{' '}
                        {scoreUnitsToPoints(
                          run.execution.instructorVisibleResult.maxScore,
                        )}
                      </p>
                    </div>
                    {run.readyForPublication ? (
                      <Badge>
                        <CheckCircle2 className="mr-1 size-3" />
                        Ready to publish
                      </Badge>
                    ) : (
                      <Badge variant="outline">
                        <AlertTriangle className="mr-1 size-3" />
                        Not ready to publish
                      </Badge>
                    )}
                  </div>
                  {run.diagnostics.length > 0 && (
                    <ul className="list-disc space-y-1 pl-5 text-sm text-muted-foreground">
                      {run.diagnostics.map((diagnostic) => (
                        <li key={diagnostic}>{diagnostic}</li>
                      ))}
                    </ul>
                  )}
                </section>
              )}
            </div>
          )}

          {error && (
            <div
              role="alert"
              className="mt-4 rounded-md border border-destructive bg-destructive/10 p-3 text-sm text-destructive"
            >
              {error}
            </div>
          )}
        </div>

        {run && (
          <div className="flex justify-end border-t px-6 py-3">
            <Button
              type="button"
              variant="outline"
              onClick={() => void restart()}
              disabled={busy}
            >
              <RotateCcw className="mr-2 size-4" />
              Restart test run
            </Button>
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
}

function readQuizItems(
  run: AssessmentTestRunRuntimeViewV1<QuizLearnerDeliveryItemV1>,
): Array<{ itemId: string; delivery: QuizLearnerDeliveryItemV1 }> {
  return run.execution.delivery.itemOrder.map((itemId) => {
    const item = run.execution.delivery.items[itemId];
    if (
      !item ||
      item.adapterKey !== QUIZ_ASSESSMENT_TYPE_ADAPTER.key ||
      item.adapterVersion !== QUIZ_ASSESSMENT_TYPE_ADAPTER.version
    ) {
      throw new Error(`Unsupported test-run delivery for item ${itemId}.`);
    }
    if (item.learnerPayload.itemId !== itemId) {
      throw new Error(`Test-run delivery item ${itemId} is mismatched.`);
    }
    return { itemId, delivery: item.learnerPayload };
  });
}

function readPlayerResult(
  run: AssessmentTestRunRuntimeViewV1<QuizLearnerDeliveryItemV1>,
  itemId: string,
  recorded: ReadonlySet<string>,
): QuizSubmissionResult {
  const result = run.execution.instructorVisibleResult?.items.find(
    (item) => item.itemId === itemId,
  );
  if (result?.state === 'graded' && result.score != null) {
    return {
      status: result.score === result.maxScore ? 'correct' : 'incorrect',
      feedback: result.feedback ?? undefined,
    };
  }
  if (run.execution.submittedResponse || recorded.has(itemId)) {
    return {
      status: 'pending',
      feedback: run.execution.submittedResponse
        ? 'Submitted to the grading workflow.'
        : 'Answer recorded for this test run.',
    };
  }
  return { status: 'idle' };
}

function RunStatus({
  run,
}: {
  run: AssessmentTestRunRuntimeViewV1;
}): React.JSX.Element {
  return <Badge variant="secondary">{run.status}</Badge>;
}

function createIdempotencyKey(): string {
  return (
    globalThis.crypto?.randomUUID?.() ??
    `assessment-test-${Date.now()}-${Math.random()}`
  );
}
