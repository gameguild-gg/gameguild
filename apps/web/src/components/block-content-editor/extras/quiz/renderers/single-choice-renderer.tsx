/**
 * Single Choice Renderer
 * Displays single choice questions with radio-style selection
 */

"use client"

import type { SingleChoiceEntry, QuizAnswerState } from "../types"

interface SingleChoiceRendererProps {
  entry: SingleChoiceEntry
  answerState: QuizAnswerState
  onAnswerChange: (updates: Partial<QuizAnswerState>) => void
  disabled?: boolean
  showFeedback?: boolean
}

export function SingleChoiceRenderer({
  entry,
  answerState,
  onAnswerChange,
  disabled = false,
  showFeedback = false,
}: SingleChoiceRendererProps) {
  const selectedId = answerState.selectedOptionIds[0]

  const handleSelect = (optionId: string) => {
    if (disabled || showFeedback) return
    onAnswerChange({ selectedOptionIds: [optionId] })
  }

  return (
    <div className="space-y-3">
      {entry.options.map((option) => {
        const isSelected = selectedId === option.id

        return (
          <button
            key={option.id}
            type="button"
            className={`
              relative flex items-center w-full p-4 rounded-lg border-2 transition-all duration-200
              ${isSelected ? "border-blue-500 bg-blue-50 dark:bg-blue-950/30 shadow-sm" : "border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600"}
              ${!disabled && !isSelected ? "hover:bg-gray-50 dark:hover:bg-gray-800" : ""}
              ${disabled || showFeedback ? "cursor-not-allowed opacity-75" : "hover:shadow-sm"}
            `}
            onClick={() => handleSelect(option.id)}
            disabled={disabled || showFeedback}
          >
            <div
              className={`
                flex items-center justify-center w-5 h-5 rounded-full border-2 mr-3 transition-colors
                ${isSelected ? "border-blue-500 bg-blue-500" : "border-gray-300 dark:border-gray-600"}
              `}
            >
              {isSelected && (
                <div className="w-2 h-2 rounded-full bg-white" />
              )}
            </div>
            <span className="text-base font-medium text-gray-800 dark:text-gray-200">{option.text}</span>
          </button>
        )
      })}
    </div>
  )
}
