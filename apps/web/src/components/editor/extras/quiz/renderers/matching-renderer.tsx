/**
 * Matching Renderer
 * Connect items from left column to right column
 */

"use client"

import { useState } from "react"
import type { MatchingEntry, QuizAnswerState } from "../types"

interface MatchingRendererProps {
  entry: MatchingEntry
  answerState: QuizAnswerState
  onAnswerChange: (updates: Partial<QuizAnswerState>) => void
  disabled?: boolean
  showFeedback?: boolean
}

export function MatchingRenderer({
  entry,
  answerState,
  onAnswerChange,
  disabled = false,
  showFeedback = false,
}: MatchingRendererProps) {
  const [selectedLeft, setSelectedLeft] = useState<string | null>(null)

  // Build a map of left -> right assignments from selectedOptionIds
  // Format: "leftId:rightValue"
  const assignments = new Map<string, string>()
  answerState.selectedOptionIds.forEach((sel) => {
    const [leftId, rightValue] = sel.split(":")
    if (leftId && rightValue) {
      assignments.set(leftId, rightValue)
    }
  })

  const rightItems = entry.pairs.map((p) => p.right)
  const distractors = entry.distractors || []
  const allRightItems = [...rightItems, ...distractors]
  const usedRightItems = new Set(assignments.values())

  const handleLeftClick = (leftId: string) => {
    if (disabled || showFeedback) return
    setSelectedLeft(leftId === selectedLeft ? null : leftId)
  }

  const handleRightClick = (rightValue: string) => {
    if (disabled || showFeedback || !selectedLeft) return

    const newAssignments = new Map(assignments)
    
    // If this right item is already used, remove its previous assignment
    for (const [leftId, right] of newAssignments.entries()) {
      if (right === rightValue) {
        newAssignments.delete(leftId)
        break
      }
    }

    // Assign the right item to the selected left
    newAssignments.set(selectedLeft, rightValue)

    // Convert back to selectedOptionIds format
    const newSelectedOptionIds = Array.from(newAssignments.entries()).map(
      ([leftId, right]) => `${leftId}:${right}`
    )

    onAnswerChange({ selectedOptionIds: newSelectedOptionIds })
    setSelectedLeft(null)
  }

  const handleRemoveAssignment = (leftId: string) => {
    if (disabled || showFeedback) return

    const newAssignments = new Map(assignments)
    newAssignments.delete(leftId)

    const newSelectedOptionIds = Array.from(newAssignments.entries()).map(
      ([lId, right]) => `${lId}:${right}`
    )

    onAnswerChange({ selectedOptionIds: newSelectedOptionIds })
  }

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-2 gap-8">
        {/* Left Column */}
        <div className="space-y-3">
          <h4 className="text-sm font-medium text-gray-600 mb-2">Items</h4>
          {entry.pairs.map((pair) => {
            const isSelected = selectedLeft === pair.id
            const isMatched = assignments.has(pair.id)
            const matchedValue = assignments.get(pair.id)

            return (
              <div
                key={pair.id}
                className={`
                  p-4 rounded-lg border-2 transition-all cursor-pointer
                  ${isSelected ? "border-blue-500 bg-blue-50" : "border-gray-200"}
                  ${isMatched ? "bg-green-50 border-green-300" : ""}
                  ${disabled || showFeedback ? "cursor-not-allowed opacity-75" : "hover:border-gray-300"}
                `}
                onClick={() => handleLeftClick(pair.id)}
              >
                <div className="font-medium">{pair.left}</div>
                {isMatched && (
                  <div className="mt-2 flex items-center justify-between text-sm text-green-700">
                    <span>→ {matchedValue}</span>
                    {!disabled && !showFeedback && (
                      <button
                        className="text-red-600 hover:text-red-700"
                        onClick={(e) => {
                          e.stopPropagation()
                          handleRemoveAssignment(pair.id)
                        }}
                      >
                        ×
                      </button>
                    )}
                  </div>
                )}
              </div>
            )
          })}
        </div>

        {/* Right Column */}
        <div className="space-y-3">
          <h4 className="text-sm font-medium text-gray-600 mb-2">Options</h4>
          {allRightItems.map((right, index) => {
            const isUsed = usedRightItems.has(right)

            return (
              <div
                key={index}
                className={`
                  p-4 rounded-lg border-2 transition-all
                  ${isUsed ? "bg-gray-100 border-gray-200 opacity-50" : "border-gray-200"}
                  ${selectedLeft && !isUsed ? "cursor-pointer hover:border-blue-400 hover:bg-blue-50" : ""}
                  ${disabled || showFeedback ? "cursor-not-allowed" : ""}
                `}
                onClick={() => !isUsed && handleRightClick(right)}
              >
                <span className="font-medium">{right}</span>
              </div>
            )
          })}
        </div>
      </div>

      {selectedLeft && (
        <div className="text-sm text-blue-600 text-center">
          Select an option from the right column to match
        </div>
      )}
    </div>
  )
}
