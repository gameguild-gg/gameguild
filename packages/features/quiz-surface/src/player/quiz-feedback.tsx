"use client"

import { Check, X, RotateCcw } from "lucide-react"

export function QuizSubmittedStatus({
  message = "Answer submitted.",
}: {
  message?: string;
}) {
  return (
    <div
      role="status"
      aria-live="polite"
      className="flex h-12 items-center justify-between gap-3 rounded-lg border-l-4 border-blue-500 bg-blue-50 px-4 py-0 text-sm text-blue-700 dark:bg-blue-950/20 dark:text-blue-400"
    >
      <span className="font-medium">{message}</span>
    </div>
  );
}

interface QuizFeedbackProps {
  isCorrect: boolean
  correctFeedback: string
  incorrectFeedback: string
  allowRetry?: boolean
  onRetry?: () => void
  showRetryButton?: boolean
}

export function QuizFeedback({
  isCorrect,
  correctFeedback,
  incorrectFeedback,
  allowRetry,
  onRetry,
  showRetryButton,
}: QuizFeedbackProps) {
  return (
    // h-12 + py-0 matches the original Submit button height (py-3 + text-base
    // ~ 48px) so swapping Submit -> Feedback does not push surrounding
    // blocks down. Internal flex items-center vertically centres the row.
    <div
      className={`flex items-center justify-between gap-3 rounded-lg px-4 h-12 py-0 text-sm border-l-4 ${
        isCorrect
          ? "bg-green-50 dark:bg-green-950/20 text-green-700 dark:text-green-400 border-green-500"
          : "bg-red-50 dark:bg-red-950/20 text-red-700 dark:text-red-400 border-red-500"
      }`}
    >
      <div className="flex items-center gap-2 flex-1 min-w-0">
        {isCorrect ? (
          <div className="shrink-0 w-5 h-5 rounded-full bg-green-500 flex items-center justify-center">
            <Check className="h-3 w-3 text-white" />
          </div>
        ) : (
          <div className="shrink-0 w-5 h-5 rounded-full bg-red-500 flex items-center justify-center">
            <X className="h-3 w-3 text-white" />
          </div>
        )}
        <span className="font-medium truncate">
          {isCorrect
            ? correctFeedback || "Excellent! That's correct!"
            : incorrectFeedback || "Not quite right. Try again!"}
        </span>
      </div>

      {showRetryButton && allowRetry && onRetry && (
        <button
          type="button"
          onClick={onRetry}
          className={`shrink-0 flex items-center gap-1.5 text-xs font-medium border py-1.5 px-3 rounded-md transition-colors ${
            isCorrect
              ? "border-green-300 text-green-700 hover:bg-green-100 dark:border-green-600 dark:text-green-400 dark:hover:bg-green-950/30"
              : "border-red-300 text-red-700 hover:bg-red-100 dark:border-red-600 dark:text-red-400 dark:hover:bg-red-950/30"
          }`}
        >
          <RotateCcw className="h-3 w-3" />
          Try Again
        </button>
      )}

      {showRetryButton && !allowRetry && onRetry && (
        <button
          type="button"
          onClick={onRetry}
          className="shrink-0 bg-gray-600 hover:bg-gray-700 text-white font-medium py-1.5 px-3 rounded-md transition-colors duration-200 text-xs"
        >
          Reset Quiz
        </button>
      )}
    </div>
  )
}
