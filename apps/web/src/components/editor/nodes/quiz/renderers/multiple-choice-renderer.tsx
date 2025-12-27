/**
 * Multiple Choice Question Renderer
 * Displays multiple choice questions with single or multi-select capability
 */

import type { QuizAnswer } from "../../quiz-node"

interface MultipleChoiceRendererProps {
  question: {
    question: string
    answers: QuizAnswer[]
  }
  selectedAnswers: string[]
  onAnswerToggle: (answerId: string) => void
  disabled?: boolean
  showFeedback?: boolean
}

export function MultipleChoiceRenderer({
  question,
  selectedAnswers,
  onAnswerToggle,
  disabled = false,
  showFeedback = false,
}: MultipleChoiceRendererProps) {
  const correctAnswers = question.answers.filter((a) => a.isCorrect)
  const maxSelections = correctAnswers.length
  const canSelectMore = selectedAnswers.length < maxSelections

  return (
    <div className="space-y-3">
      {maxSelections > 1 && (
        <div className="text-sm text-gray-600 bg-blue-50 border border-blue-200 rounded-lg p-3">
          <span className="font-medium">
            Select {maxSelections} answer{maxSelections > 1 ? "s" : ""} ({selectedAnswers.length}/{maxSelections}{" "}
            selected)
          </span>
        </div>
      )}

      {question.answers.map((answer) => {
        const isSelected = selectedAnswers.includes(answer.id)
        const canClick = !disabled && (isSelected || canSelectMore)

        return (
          <button
            key={answer.id}
            type="button"
            className={`
              relative flex items-center w-full p-4 rounded-lg border-2 transition-all duration-200
              ${isSelected ? "border-blue-500 bg-blue-50 shadow-sm" : "border-gray-200 hover:border-gray-300"}
              ${!disabled && !isSelected ? "hover:bg-gray-50" : ""}
              ${disabled || showFeedback ? "cursor-not-allowed opacity-75" : "hover:shadow-sm"}
              ${!canClick ? "cursor-not-allowed opacity-50" : ""}
            `}
            onClick={() => canClick && onAnswerToggle(answer.id)}
            disabled={disabled || showFeedback}
          >
            <div
              className={`
                flex items-center justify-center w-5 h-5 rounded border-2 mr-3 transition-colors
                ${isSelected ? "border-blue-500 bg-blue-500" : "border-gray-300"}
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
            <span className="text-base font-medium text-gray-800">{answer.text}</span>
          </button>
        )
      })}
    </div>
  )
}
