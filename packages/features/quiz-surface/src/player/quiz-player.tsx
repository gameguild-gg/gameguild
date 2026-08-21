"use client";

import { useMemo } from "react";
import {
  QuizEntryType,
  createEmptyQuizAnswer,
  type QuizAnswer,
  type QuizLearnerEntry,
} from "@game-guild/quiz";
import { QuizRenderer } from "../registry/player-registry";
import { QuizAssetsBoundary } from "../shared/quiz-assets-boundary";
import { QuizFeedback, QuizSubmittedStatus } from "./quiz-feedback";
import {
  applyRendererAnswerUpdate,
  toRendererAnswerState,
} from "./renderer-answer-adapter";

export interface QuizSubmissionResult {
  status: "idle" | "pending" | "correct" | "incorrect";
  feedback?: string;
}

export interface QuizPlayerProps {
  entry: QuizLearnerEntry;
  answer: QuizAnswer;
  onAnswerChange: (answer: QuizAnswer) => void;
  onSubmit: (answer: QuizAnswer) => void | Promise<void>;
  submissionResult?: QuizSubmissionResult;
  disabled?: boolean;
}

export function QuizPlayer({
  entry,
  answer,
  onAnswerChange,
  onSubmit,
  submissionResult = { status: "idle" },
  disabled = false,
}: QuizPlayerProps) {
  const activeAnswer = answer.type === entry.type ? answer : createEmptyQuizAnswer(entry.type);
  const rendererState = useMemo(() => toRendererAnswerState(activeAnswer), [activeAnswer]);
  const submitted = submissionResult.status !== "idle";

  return (
    <QuizAssetsBoundary>
      <div className="space-y-4">
        {entry.type !== QuizEntryType.FillInTheBlank && (
          <div className="text-lg font-medium">{entry.stem}</div>
        )}
        <QuizRenderer
          entry={entry}
          answerState={rendererState}
          onAnswerChange={(update) => onAnswerChange(applyRendererAnswerUpdate(activeAnswer, update).answer)}
          disabled={disabled || submissionResult.status === "pending"}
          showFeedback={submissionResult.status === "correct" || submissionResult.status === "incorrect"}
        />
        {!submitted && (
          <button
            type="button"
            className="h-12 w-full rounded-lg bg-blue-600 px-6 font-semibold text-white shadow-sm transition-colors duration-200 hover:bg-blue-700 hover:shadow-md disabled:cursor-not-allowed disabled:opacity-50"
            disabled={disabled}
            onClick={() => void onSubmit(activeAnswer)}
          >
            Submit answer
          </button>
        )}
        {submitted && <QuizSubmissionStatus result={submissionResult} />}
      </div>
    </QuizAssetsBoundary>
  );
}

function QuizSubmissionStatus({ result }: { result: QuizSubmissionResult }) {
  if (result.status === "idle") return null;

  if (result.status === "correct" || result.status === "incorrect") {
    return (
      <QuizFeedback
        isCorrect={result.status === "correct"}
        correctFeedback={result.status === "correct" ? result.feedback ?? "" : ""}
        incorrectFeedback={result.status === "incorrect" ? result.feedback ?? "" : ""}
      />
    );
  }

  return <QuizSubmittedStatus message={result.feedback} />;
}
