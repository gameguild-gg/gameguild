/**
 * Short Answer Renderer
 * Single text input for short answer questions
 */

"use client"

import type { QuizAnswerState, ShortAnswerEntry } from "../types"
import type { ShortAnswerLearnerEntry } from "../contracts"

interface ShortAnswerRendererProps {
  entry: ShortAnswerEntry | ShortAnswerLearnerEntry
  answerState: QuizAnswerState
  onAnswerChange: (updates: Partial<QuizAnswerState>) => void
  disabled?: boolean
  showFeedback?: boolean
}

export function ShortAnswerRenderer({
  answerState,
  onAnswerChange,
  disabled = false,
  showFeedback = false,
}: ShortAnswerRendererProps) {
  const answer = answerState.textAnswers["main"] || ""

  const handleChange = (value: string) => {
    if (disabled || showFeedback) return
    onAnswerChange({
      textAnswers: {
        ...answerState.textAnswers,
        main: value,
      },
    })
  }

  return (
    <div className="space-y-2">
      <input
        type="text"
        className="w-full px-4 py-3 border-2 border-gray-300 dark:border-gray-600 rounded-lg focus:border-blue-500 focus:outline-none transition-colors text-base bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100"
        placeholder="Type your answer..."
        value={answer}
        onChange={(e) => handleChange(e.target.value)}
        disabled={disabled || showFeedback}
      />
    </div>
  )
}
