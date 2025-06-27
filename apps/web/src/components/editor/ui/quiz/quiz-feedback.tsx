"use client"

import { Check, X, RotateCcw } from "lucide-react"
import { Button } from "@/components/editor/ui/button"

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
    <div
      className={`flex items-center justify-between gap-3 rounded-lg mt-3 px-4 py-3 text-sm border-l-4 ${
        isCorrect
          ? "bg-green-50 dark:bg-green-950/20 text-green-700 dark:text-green-400 border-green-500"
          : "bg-red-50 dark:bg-red-950/20 text-red-700 dark:text-red-400 border-red-500"
      }`}
    >
      <div className="flex items-center gap-2 flex-1">
        {isCorrect ? (
          <div className="flex-shrink-0 w-5 h-5 rounded-full bg-green-500 flex items-center justify-center">
            <Check className="h-3 w-3 text-white" />
          </div>
        ) : (
          <div className="flex-shrink-0 w-5 h-5 rounded-full bg-red-500 flex items-center justify-center">
            <X className="h-3 w-3 text-white" />
          </div>
        )}
        <span className="font-medium">
          {isCorrect
            ? correctFeedback || "Excellent! That's correct!"
            : incorrectFeedback || "Not quite right. Try again!"}
        </span>
      </div>

      {showRetryButton && !allowRetry && onRetry && (
        <Button
          variant="outline"
          size="sm"
          onClick={onRetry}
          className={`flex items-center gap-1.5 text-xs font-medium transition-colors ${
            isCorrect
              ? "border-green-300 text-green-700 hover:bg-green-100 dark:border-green-600 dark:text-green-400 dark:hover:bg-green-950/30"
              : "border-red-300 text-red-700 hover:bg-red-100 dark:border-red-600 dark:text-red-400 dark:hover:bg-red-950/30"
          }`}
        >
          <RotateCcw className="h-3 w-3" />
          Try Again
        </Button>
      )}
    </div>
  )
}
