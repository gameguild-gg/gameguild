/**
 * Quiz Display
 * Renders a complete quiz with question, renderer, submit button, and feedback
 */

"use client"

import { QuizFeedback } from "./quiz-feedback"
import { QuizRenderer } from "./renderers/quiz-renderer"
import { useQuizAnswers } from "./hooks/use-quiz-answers"
import { type QuizEntry, QuizEntryType } from "./types"

interface QuizDisplayProps {
  entry: QuizEntry
}

export function QuizDisplay({ entry }: QuizDisplayProps) {
  const {
    answerState,
    updateAnswerState,
    showFeedback,
    isCorrect,
    checkAnswers,
    resetQuiz,
  } = useQuizAnswers({ entry })
  const hasTrustedFeedback = showFeedback && isCorrect !== null

  return (
    <div className="space-y-4">
      {/* Question text - hide for fill-blank as it's rendered inline */}
      {entry.type !== QuizEntryType.FillInTheBlank && (
        <div className="text-lg font-medium">{entry.stem}</div>
      )}

      {/* Render appropriate question type */}
      <QuizRenderer
        entry={entry}
        answerState={answerState}
        onAnswerChange={updateAnswerState}
        disabled={false}
        showFeedback={hasTrustedFeedback}
      />

      {/* Submit button - h-12 ensures Submit/Feedback/Submitted all share
          the same height so swapping them does not shift sibling blocks. */}
      {!showFeedback && (
        <button
          onClick={checkAnswers}
          className="w-full h-12 bg-blue-600 hover:bg-blue-700 text-white font-semibold px-6 rounded-lg transition-colors duration-200 shadow-sm hover:shadow-md"
        >
          Submit Answer
        </button>
      )}

      {/* Feedback */}
      {hasTrustedFeedback && (entry.settings.showFeedback ?? true) && (
        <QuizFeedback
          isCorrect={isCorrect === true}
          correctFeedback={entry.feedback?.correct || ""}
          incorrectFeedback={entry.feedback?.incorrect || ""}
          allowRetry={entry.settings.allowRetry}
          onRetry={resetQuiz}
          showRetryButton={entry.settings.allowRetry}
        />
      )}

      {/* Submitted without feedback - h-12 matches Submit button height */}
      {showFeedback && !hasTrustedFeedback && (
        <div className="flex items-center justify-between gap-3 rounded-lg px-4 h-12 py-0 text-sm border-l-4 bg-blue-50 dark:bg-blue-950/20 text-blue-700 dark:text-blue-400 border-blue-500">
          <span className="font-medium">Answer submitted.</span>
          {entry.settings.allowRetry && (
            <button
              onClick={resetQuiz}
              className="shrink-0 flex items-center gap-1.5 text-xs font-medium border border-blue-300 dark:border-blue-600 text-blue-700 dark:text-blue-400 hover:bg-blue-100 dark:hover:bg-blue-950/30 py-1.5 px-3 rounded-md transition-colors"
            >
              Try Again
            </button>
          )}
        </div>
      )}

      {hasTrustedFeedback && !(entry.settings.showFeedback ?? true) && (
        <div className="flex items-center justify-between gap-3 rounded-lg px-4 h-12 py-0 text-sm border-l-4 bg-blue-50 dark:bg-blue-950/20 text-blue-700 dark:text-blue-400 border-blue-500">
          <span className="font-medium">Answer submitted.</span>
          {entry.settings.allowRetry && (
            <button
              onClick={resetQuiz}
              className="shrink-0 flex items-center gap-1.5 text-xs font-medium border border-blue-300 dark:border-blue-600 text-blue-700 dark:text-blue-400 hover:bg-blue-100 dark:hover:bg-blue-950/30 py-1.5 px-3 rounded-md transition-colors"
            >
              Try Again
            </button>
          )}
        </div>
      )}
    </div>
  )
}
