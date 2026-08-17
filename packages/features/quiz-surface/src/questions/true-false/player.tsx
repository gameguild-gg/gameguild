/**
 * True/False Renderer
 * Displays a simple true/false question with two buttons
 */

"use client"

import type { RendererAnswerState } from "../../player/renderer-answer-adapter"

import type { TrueFalseEntry } from "@game-guild/quiz"
import type { TrueFalseLearnerEntry } from "@game-guild/quiz"

interface TrueFalseRendererProps {
  entry: TrueFalseEntry | TrueFalseLearnerEntry
  answerState: RendererAnswerState
  onAnswerChange: (updates: Partial<RendererAnswerState>) => void
  disabled?: boolean
  showFeedback?: boolean
}

export function TrueFalseRenderer({
  answerState,
  onAnswerChange,
  disabled = false,
  showFeedback = false,
}: TrueFalseRendererProps) {
  const selectedId = answerState.selectedOptionIds[0]
  const selectedAnswer = selectedId === "true" ? true : selectedId === "false" ? false : null

  const handleSelect = (answer: boolean) => {
    if (disabled || showFeedback) return
    onAnswerChange({ selectedOptionIds: [answer ? "true" : "false"] })
  }

  return (
    <div className="space-y-3">
      <button
        type="button"
        className={`
          w-full p-4 rounded-lg border-2 font-medium text-lg transition-all duration-200
          ${
            selectedAnswer === true
              ? "border-blue-500 bg-blue-50 dark:bg-blue-950/30 text-blue-700 dark:text-blue-400 shadow-sm"
              : "border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800"
          }
          ${disabled || showFeedback ? "cursor-not-allowed opacity-75" : "hover:shadow-sm"}
        `}
        onClick={() => handleSelect(true)}
        disabled={disabled || showFeedback}
      >
        <svg
          className={`w-5 h-5 inline mr-3 transition-colors ${
            selectedAnswer === true ? "text-blue-500" : "text-gray-400"
          }`}
          fill="currentColor"
          viewBox="0 0 20 20"
        >
          <path
            fillRule="evenodd"
            d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
            clipRule="evenodd"
          />
        </svg>
        True
      </button>

      <button
        type="button"
        className={`
          w-full p-4 rounded-lg border-2 font-medium text-lg transition-all duration-200
          ${
            selectedAnswer === false
              ? "border-blue-500 bg-blue-50 dark:bg-blue-950/30 text-blue-700 dark:text-blue-400 shadow-sm"
              : "border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800"
          }
          ${disabled || showFeedback ? "cursor-not-allowed opacity-75" : "hover:shadow-sm"}
        `}
        onClick={() => handleSelect(false)}
        disabled={disabled || showFeedback}
      >
        <svg
          className={`w-5 h-5 inline mr-3 transition-colors ${
            selectedAnswer === false ? "text-blue-500" : "text-gray-400"
          }`}
          fill="currentColor"
          viewBox="0 0 20 20"
        >
          <path
            fillRule="evenodd"
            d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z"
            clipRule="evenodd"
          />
        </svg>
        False
      </button>
    </div>
  )
}
