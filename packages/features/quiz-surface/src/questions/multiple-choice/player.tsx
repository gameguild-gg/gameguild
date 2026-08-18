/**
 * Multiple Choice Renderer
 * Displays multiple choice questions with checkbox-style multi-select
 */

"use client"

import type { RendererAnswerState } from "../../player/renderer-answer-adapter"

import type { MultipleChoiceEntry } from "@game-guild/quiz"
import type { MultipleChoiceLearnerEntry } from "@game-guild/quiz"

interface MultipleChoiceRendererProps {
  entry: MultipleChoiceEntry | MultipleChoiceLearnerEntry
  answerState: RendererAnswerState
  onAnswerChange: (updates: Partial<RendererAnswerState>) => void
  disabled?: boolean
  showFeedback?: boolean
}

export function MultipleChoiceRenderer({
  entry,
  answerState,
  onAnswerChange,
  disabled = false,
  showFeedback = false,
}: MultipleChoiceRendererProps) {
  const selectedIds = answerState.selectedOptionIds
  const answerKeyCount = "correctOptionIds" in entry ? entry.correctOptionIds.length : undefined
  const configuredLimit =
    typeof entry.selectionLimit === "number" && Number.isFinite(entry.selectionLimit) && entry.selectionLimit > 0
      ? entry.selectionLimit
      : undefined
  const maxSelections = Math.min(
    entry.options.length,
    Math.max(1, configuredLimit ?? answerKeyCount ?? entry.options.length),
  )
  const canSelectMore = selectedIds.length < maxSelections

  const handleToggle = (optionId: string) => {
    if (disabled || showFeedback) return

    const isSelected = selectedIds.includes(optionId)
    let newSelection: string[]

    if (isSelected) {
      newSelection = selectedIds.filter((id) => id !== optionId)
    } else if (canSelectMore) {
      newSelection = [...selectedIds, optionId]
    } else {
      return
    }

    onAnswerChange({ selectedOptionIds: newSelection })
  }

  return (
    <div className="space-y-3">
      {maxSelections > 1 && (
        <div className="text-sm text-gray-600 bg-blue-50 border border-blue-200 rounded-lg p-3">
          <span className="font-medium">
            Select {maxSelections} answer{maxSelections > 1 ? "s" : ""} ({selectedIds.length}/{maxSelections} selected)
          </span>
        </div>
      )}

      {entry.options.map((option) => {
        const isSelected = selectedIds.includes(option.id)
        const canClick = isSelected || canSelectMore

        return (
          <button
            key={option.id}
            type="button"
            className={`
              relative flex items-center w-full p-4 rounded-lg border-2 transition-all duration-200
              ${isSelected ? "border-blue-500 bg-blue-50 dark:bg-blue-950/30 shadow-sm" : "border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600"}
              ${!disabled && !isSelected ? "hover:bg-gray-50 dark:hover:bg-gray-800" : ""}
              ${disabled || showFeedback ? "cursor-not-allowed opacity-75" : "hover:shadow-sm"}
              ${!canClick ? "cursor-not-allowed opacity-50" : ""}
            `}
            onClick={() => canClick && handleToggle(option.id)}
            disabled={disabled || showFeedback}
          >
            <div
              className={`
                flex items-center justify-center w-5 h-5 rounded border-2 mr-3 transition-colors
                ${isSelected ? "border-blue-500 bg-blue-500" : "border-gray-300 dark:border-gray-600"}
              `}
            >
              {isSelected && (
                <svg className="w-3 h-3 text-white" fill="currentColor" viewBox="0 0 20 20">
                  <path
                    fillRule="evenodd"
                    d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
                    clipRule="evenodd"
                  />
                </svg>
              )}
            </div>
            <span className="text-base font-medium text-gray-800 dark:text-gray-200">{option.text}</span>
          </button>
        )
      })}
    </div>
  )
}
