"use client";

import { useMemo, useState } from "react";
import {
  QuizEntryType,
  createEmptyQuizAnswer,
  evaluateQuizAnswer,
  type QuizAnswer,
  type QuizEvaluationResult,
  type QuizPracticeEntry,
} from "@game-guild/quiz";
import { QuizRenderer } from "../registry/player-registry";
import { QuizAssetsBoundary } from "../shared/quiz-assets-boundary";
import { QuizFeedback, QuizSubmittedStatus } from "./quiz-feedback";
import {
  applyRendererAnswerUpdate,
  toRendererAnswerState,
} from "./renderer-answer-adapter";

export interface QuizPracticePlayerProps {
  entry: QuizPracticeEntry;
  answer: QuizAnswer;
  onAnswerChange: (answer: QuizAnswer) => void;
  onEvaluation?: (result: QuizEvaluationResult) => void;
  disabled?: boolean;
}

export function QuizPracticePlayer({
  entry,
  answer,
  onAnswerChange,
  onEvaluation,
  disabled = false,
}: QuizPracticePlayerProps) {
  const [result, setResult] = useState<QuizEvaluationResult | null>(null);
  const [promptVariables, setPromptVariables] = useState<Record<string, number> | undefined>();
  const activeAnswer = answer.type === entry.type ? answer : createEmptyQuizAnswer(entry.type);
  const rendererState = useMemo(
    () => toRendererAnswerState(activeAnswer, promptVariables),
    [activeAnswer, promptVariables],
  );

  const updateAnswer = (update: Parameters<typeof applyRendererAnswerUpdate>[1]) => {
    const next = applyRendererAnswerUpdate(activeAnswer, update);
    if (next.promptVariables) setPromptVariables(next.promptVariables);
    onAnswerChange(next.answer);
  };

  const submit = () => {
    const evaluation = evaluateQuizAnswer(entry, activeAnswer, {
      formulaPrompts: promptVariables ? [promptVariables] : undefined,
    });
    setResult(evaluation);
    onEvaluation?.(evaluation);
  };

  const reset = () => {
    setResult(null);
    setPromptVariables(undefined);
    onAnswerChange(createEmptyQuizAnswer(entry.type));
  };

  const isCorrect = result?.status === "correct";
  const hasFeedback = result?.status === "correct" || result?.status === "incorrect";

  return (
    <QuizAssetsBoundary>
      <div className="space-y-4">
        {entry.type !== QuizEntryType.FillInTheBlank && (
          <div className="text-lg font-medium">{entry.stem}</div>
        )}
        <QuizRenderer
          entry={entry}
          answerState={rendererState}
          onAnswerChange={updateAnswer}
          disabled={disabled}
          showFeedback={hasFeedback}
        />
        {!result && (
          <button
            type="button"
            className="h-12 w-full rounded-lg bg-blue-600 px-6 font-semibold text-white shadow-sm transition-colors duration-200 hover:bg-blue-700 hover:shadow-md disabled:cursor-not-allowed disabled:opacity-50"
            disabled={disabled}
            onClick={submit}
          >
            Check answer
          </button>
        )}
        {hasFeedback && (entry.settings.showFeedback ?? true) && (
          <QuizFeedback
            isCorrect={isCorrect}
            correctFeedback={entry.feedback?.correct ?? ""}
            incorrectFeedback={entry.feedback?.incorrect ?? ""}
            allowRetry={entry.settings.allowRetry}
            onRetry={reset}
            showRetryButton={entry.settings.allowRetry}
          />
        )}
        {hasFeedback && !(entry.settings.showFeedback ?? true) && (
          <QuizSubmittedStatus />
        )}
        {result && !hasFeedback && (
          <div className="min-h-12 rounded-md border px-4 py-3 text-sm" aria-live="polite">
            {result.reason ?? "This answer requires review."}
          </div>
        )}
      </div>
    </QuizAssetsBoundary>
  );
}
