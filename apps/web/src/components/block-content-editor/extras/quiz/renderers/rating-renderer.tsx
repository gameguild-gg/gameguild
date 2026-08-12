/**
 * Rating Renderer
 * Displays a scale of rating buttons
 */

"use client"

import type { QuizAnswerState, RatingEntry } from "../types"
import type { RatingLearnerEntry } from "../contracts"

interface RatingRendererProps {
  entry: RatingEntry | RatingLearnerEntry
  answerState: QuizAnswerState
  onAnswerChange: (updates: Partial<QuizAnswerState>) => void
  disabled?: boolean
  showFeedback?: boolean
}

export function RatingRenderer({
  entry,
  answerState,
  onAnswerChange,
  disabled = false,
  showFeedback = false,
}: RatingRendererProps) {
  const { scale } = entry
  const selectedRating = answerState.rating

  // Generate rating options based on scale
  const ratingOptions: number[] = []
  for (let i = scale.min; i <= scale.max; i += scale.step) {
    ratingOptions.push(i)
  }

  const handleSelect = (value: number) => {
    if (disabled || showFeedback) return
    onAnswerChange({ rating: value })
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <span className="text-sm text-gray-600 dark:text-gray-400 font-medium">
          {scale.minLabel || `${scale.min} (Lowest)`}
        </span>
        <span className="text-sm text-gray-600 dark:text-gray-400 font-medium">
          {scale.maxLabel || `${scale.max} (Highest)`}
        </span>
      </div>

      <div className="flex items-center justify-center space-x-3">
        {ratingOptions.map((value) => {
          const isSelected = selectedRating === value

          return (
            <button
              key={value}
              type="button"
              className={`
                w-12 h-12 rounded-lg border-2 font-bold text-lg transition-all duration-200
                ${isSelected
                  ? "border-blue-500 bg-blue-500 text-white shadow-lg scale-110"
                  : "border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:border-blue-300 hover:bg-blue-50 dark:hover:bg-blue-950/30"
                }
                ${disabled || showFeedback ? "cursor-not-allowed opacity-75" : "hover:shadow-md cursor-pointer"}
              `}
              onClick={() => handleSelect(value)}
              disabled={disabled || showFeedback}
            >
              {value}
            </button>
          )
        })}
      </div>
    </div>
  )
}
