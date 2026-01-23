/**
 * Essay Renderer
 * Multi-line text area for essay questions
 */

"use client"

import { useMemo } from "react"
import type { EssayEntry, QuizAnswerState } from "../types"

interface EssayRendererProps {
  entry: EssayEntry
  answerState: QuizAnswerState
  onAnswerChange: (updates: Partial<QuizAnswerState>) => void
  disabled?: boolean
  showFeedback?: boolean
}

export function EssayRenderer({
  entry,
  answerState,
  onAnswerChange,
  disabled = false,
  showFeedback = false,
}: EssayRendererProps) {
  const answer = answerState.textAnswers["main"] || ""

  const wordCount = useMemo(() => {
    return answer.trim().split(/\s+/).filter(Boolean).length
  }, [answer])

  const handleChange = (value: string) => {
    if (disabled || showFeedback) return
    onAnswerChange({
      textAnswers: {
        ...answerState.textAnswers,
        main: value,
      },
    })
  }

  const isWordCountValid =
    (!entry.minWordCount || wordCount >= entry.minWordCount) &&
    (!entry.maxWordCount || wordCount <= entry.maxWordCount)

  return (
    <div className="space-y-3">
      <textarea
        className="w-full px-4 py-3 border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:outline-none transition-colors text-base min-h-[150px] resize-y"
        placeholder="Write your answer..."
        value={answer}
        onChange={(e) => handleChange(e.target.value)}
        disabled={disabled || showFeedback}
      />

      {entry.showWordCount && (
        <div className="flex items-center justify-between text-sm text-gray-600">
          <span className={!isWordCountValid ? "text-red-600" : ""}>
            {wordCount} word{wordCount !== 1 ? "s" : ""}
          </span>
          {(entry.minWordCount || entry.maxWordCount) && (
            <span>
              {entry.minWordCount && `Min: ${entry.minWordCount}`}
              {entry.minWordCount && entry.maxWordCount && " • "}
              {entry.maxWordCount && `Max: ${entry.maxWordCount}`}
            </span>
          )}
        </div>
      )}
    </div>
  )
}
