"use client"

import { useState } from "react"
import type { QuizData } from "../../../nodes/quiz-node"

interface CategorizationAnswerState {
  answerId: string
  categoryId: string
}

interface CategorizationRendererProps {
  data: QuizData
  selectedAnswers: string[]
  onAnswerChange: (answers: string[]) => void
  disabled?: boolean
  showFeedback?: boolean
}

export function CategorizationRenderer({
  data,
  selectedAnswers,
  onAnswerChange,
  disabled = false,
  showFeedback = false,
}: CategorizationRendererProps) {
  const [draggedAnswer, setDraggedAnswer] = useState<string | null>(null)

  // Build state of answers assigned to each category
  const categorizedAnswers: Record<string, string[]> = {}
  
  // Initialize categories
  const categories = (data as any).categories || []
  categories.forEach((cat: any) => {
    if (cat.id) {
      categorizedAnswers[cat.id] = []
    }
  })

  // Parse selected answers format: "answerId:categoryId"
  selectedAnswers.forEach((sel) => {
    const parts = sel.split(":")
    const answerId = parts[0]
    const categoryId = parts[1]
    if (answerId && categoryId && categorizedAnswers[categoryId] !== undefined) {
      categorizedAnswers[categoryId]?.push(answerId)
    }
  })

  const availableAnswers = (data as any).answers || []
  const assignedAnswerIds = new Set(selectedAnswers.map((s) => s.split(":")[0]))
  const unassignedAnswers = availableAnswers.filter(
    (a: any) => !assignedAnswerIds.has(a.id)
  )

  const handleAnswerDragStart = (answerId: string) => {
    setDraggedAnswer(answerId)
  }

  const handleCategoryDragOver = (e: React.DragEvent) => {
    e.preventDefault()
    e.currentTarget.classList.add("bg-blue-100", "border-blue-500")
  }

  const handleCategoryDragLeave = (e: React.DragEvent) => {
    e.currentTarget.classList.remove("bg-blue-100", "border-blue-500")
  }

  const handleCategoryDrop = (e: React.DragEvent, categoryId: string) => {
    e.preventDefault()
    e.currentTarget.classList.remove("bg-blue-100", "border-blue-500")

    if (!draggedAnswer || disabled) return

    // Add answer to category
    const newSelection = [...selectedAnswers]
    const key = `${draggedAnswer}:${categoryId}`

    // Check if already assigned
    if (!newSelection.includes(key)) {
      // Remove from any other category
      const filtered = newSelection.filter((s) => !s.startsWith(draggedAnswer))
      filtered.push(key)
      onAnswerChange(filtered)
    }

    setDraggedAnswer(null)
  }

  const handleRemoveAnswer = (answerId: string, categoryId: string) => {
    if (disabled) return
    const key = `${answerId}:${categoryId}`
    const filtered = selectedAnswers.filter((s) => s !== key)
    onAnswerChange(filtered)
  }

  return (
    <div className="space-y-6">
      {/* Unassigned Answers Pool */}
      {unassignedAnswers.length > 0 && (
        <div className="border rounded-lg p-4 bg-gray-50 dark:bg-gray-800/50">
          <h4 className="text-sm font-medium mb-3 text-gray-700 dark:text-gray-300">
            Possible Answers
          </h4>
          <div className="flex flex-wrap gap-2">
            {unassignedAnswers.map((answer: any) => (
              <div
                key={answer.id}
                draggable={!disabled}
                onDragStart={() => handleAnswerDragStart(answer.id)}
                className={`px-4 py-2 rounded border text-sm font-medium transition-all cursor-move ${
                  draggedAnswer === answer.id
                    ? "bg-blue-200 border-blue-400 opacity-50"
                    : "bg-white dark:bg-gray-900 border-gray-300 dark:border-gray-700 hover:border-blue-400 dark:hover:border-blue-500"
                } ${disabled ? "cursor-not-allowed opacity-50" : ""}`}
              >
                {answer.text}
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Categories */}
      <div className="grid gap-4">
        {categories.map((category: any) => (
          <div
            key={category.id}
            className="border rounded-lg overflow-hidden bg-white dark:bg-gray-900"
          >
            {/* Category Header */}
            <div className="bg-gray-100 dark:bg-gray-800 p-4 border-b">
              <h4 className="font-medium text-sm text-gray-900 dark:text-gray-100">
                {category.name}
              </h4>
              {category.description && (
                <p className="text-xs text-gray-600 dark:text-gray-400 mt-1">
                  {category.description}
                </p>
              )}
            </div>

            {/* Drop Zone */}
            <div
              className="min-h-24 p-4 border-2 border-dashed border-gray-300 dark:border-gray-600 transition-colors"
              onDragOver={handleCategoryDragOver}
              onDragLeave={handleCategoryDragLeave}
              onDrop={(e) => handleCategoryDrop(e, category.id)}
            >
              {(categorizedAnswers[category.id]?.length || 0) === 0 ? (
                <div className="text-sm text-gray-400 dark:text-gray-500 text-center py-6">
                  {disabled ? "No Answers Chosen" : "Drag answers here"}
                </div>
              ) : (
                <div className="space-y-2">
                  {(categorizedAnswers[category.id] || []).map((answerId) => {
                    const answer = availableAnswers.find(
                      (a: any) => a.id === answerId
                    )
                    return (
                      <div
                        key={answerId}
                        className="flex items-center justify-between gap-2 px-4 py-2 bg-blue-50 dark:bg-blue-900/20 rounded border border-blue-300 dark:border-blue-700 text-sm"
                      >
                        <span className="font-medium text-gray-900 dark:text-gray-100">
                          {answer?.text}
                        </span>
                        {!disabled && (
                          <button
                            onClick={() =>
                              handleRemoveAnswer(answerId, category.id)
                            }
                            className="text-red-600 hover:text-red-700 dark:text-red-400 dark:hover:text-red-300"
                            title="Remove"
                          >
                            <svg
                              className="h-4 w-4"
                              fill="none"
                              stroke="currentColor"
                              viewBox="0 0 24 24"
                            >
                              <path
                                strokeLinecap="round"
                                strokeLinejoin="round"
                                strokeWidth={2}
                                d="M6 18L18 6M6 6l12 12"
                              />
                            </svg>
                          </button>
                        )}
                      </div>
                    )
                  })}
                </div>
              )}
            </div>
          </div>
        ))}

        {categories.length === 0 && (
          <div className="text-sm text-gray-500 dark:text-gray-400 text-center py-8">
            No categories defined yet. Create categories in the editor.
          </div>
        )}
      </div>
    </div>
  )
}
