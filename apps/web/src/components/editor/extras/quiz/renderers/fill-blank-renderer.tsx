/**
 * Fill in the Blank Renderer
 * Renders questions with inline input fields for blanks
 */

"use client"

import type { FillInTheBlankEntry, QuizAnswerState } from "../types"

interface FillBlankRendererProps {
  entry: FillInTheBlankEntry
  answerState: QuizAnswerState
  onAnswerChange: (updates: Partial<QuizAnswerState>) => void
  disabled?: boolean
  showFeedback?: boolean
}

export function FillBlankRenderer({
  entry,
  answerState,
  onAnswerChange,
  disabled = false,
  showFeedback = false,
}: FillBlankRendererProps) {
  // Split stem by blank markers (___)
  const parts = entry.stem.split("___")

  const handleInputChange = (blankId: string, value: string) => {
    if (disabled || showFeedback) return
    onAnswerChange({
      textAnswers: {
        ...answerState.textAnswers,
        [blankId]: value,
      },
    })
  }

  return (
    <div className="space-y-4">
      <div className="text-lg leading-relaxed">
        {parts.map((part, index) => (
          <span key={index}>
            {part}
            {index < parts.length - 1 && entry.blanks[index] && (
              <input
                type="text"
                className="inline-block w-40 mx-2 px-3 py-2 border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:outline-none transition-colors"
                placeholder="..."
                value={answerState.textAnswers[entry.blanks[index].id] || ""}
                onChange={(e) => handleInputChange(entry.blanks[index]!.id, e.target.value)}
                disabled={disabled || showFeedback}
              />
            )}
          </span>
        ))}
      </div>
    </div>
  )
}
