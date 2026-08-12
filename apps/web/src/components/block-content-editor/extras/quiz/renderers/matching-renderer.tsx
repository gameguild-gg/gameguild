/**
 * Matching Renderer
 * Connect items from left column to right column
 */

"use client"

import { useState, useRef, useEffect, useMemo, useCallback } from "react"
import type { MatchingEntry, QuizAnswerState } from "../types"
import type { MatchingLearnerEntry } from "../contracts"

interface MatchingRendererProps {
  entry: MatchingEntry | MatchingLearnerEntry
  answerState: QuizAnswerState
  onAnswerChange: (updates: Partial<QuizAnswerState>) => void
  disabled?: boolean
  showFeedback?: boolean
}

interface Point {
  x: number
  y: number
}

const CONNECTION_COLORS = [
  { card: "border-blue-500 bg-blue-50 dark:bg-blue-950/30 dark:border-blue-500", line: "text-blue-500 dark:text-blue-400" },
  { card: "border-green-500 bg-green-50 dark:bg-green-950/30 dark:border-green-500", line: "text-green-500 dark:text-green-400" },
  { card: "border-purple-500 bg-purple-50 dark:bg-purple-950/30 dark:border-purple-500", line: "text-purple-500 dark:text-purple-400" },
  { card: "border-orange-500 bg-orange-50 dark:bg-orange-950/30 dark:border-orange-500", line: "text-orange-500 dark:text-orange-400" },
  { card: "border-pink-500 bg-pink-50 dark:bg-pink-950/30 dark:border-pink-500", line: "text-pink-500 dark:text-pink-400" },
  { card: "border-teal-500 bg-teal-50 dark:bg-teal-950/30 dark:border-teal-500", line: "text-teal-500 dark:text-teal-400" },
  { card: "border-indigo-500 bg-indigo-50 dark:bg-indigo-950/30 dark:border-indigo-500", line: "text-indigo-500 dark:text-indigo-400" },
  { card: "border-rose-500 bg-rose-50 dark:bg-rose-950/30 dark:border-rose-500", line: "text-rose-500 dark:text-rose-400" },
]

function stableShuffle<T>(items: readonly T[], getKey: (item: T, index: number) => string): T[] {
  return items
    .map((item, index) => ({ item, index, rank: hashString(`${getKey(item, index)}:${index}`) }))
    .sort((left, right) => left.rank - right.rank || left.index - right.index)
    .map(({ item }) => item)
}

function hashString(value: string): number {
  let hash = 0
  for (let index = 0; index < value.length; index++) {
    hash = (hash * 31 + value.charCodeAt(index)) >>> 0
  }
  return hash
}

export function MatchingRenderer({
  entry,
  answerState,
  onAnswerChange,
  disabled = false,
  showFeedback = false,
}: MatchingRendererProps) {
  const [selectedLeft, setSelectedLeft] = useState<string | null>(null)
  const containerRef = useRef<HTMLDivElement>(null)
  const leftRefs = useRef<Map<string, HTMLDivElement>>(new Map())
  const rightRefs = useRef<Map<string, HTMLDivElement>>(new Map())
  const [lines, setLines] = useState<Array<{ id: string; start: Point; end: Point; colorClass: string }>>([])

  // Build a map of left -> right assignments from selectedOptionIds
  // Format: "leftId:rightValue"
  const assignments = useMemo(() => {
    const next = new Map<string, string>()
    answerState.selectedOptionIds.forEach((sel) => {
      const idx = sel.indexOf(":")
      if (idx > 0) {
        next.set(sel.substring(0, idx), sel.substring(idx + 1))
      }
    })
    return next
  }, [answerState.selectedOptionIds])

  const allRightItems = useMemo(() => {
    const rightItems = entry.rightOptions ?? entry.pairs.flatMap((pair) => ("right" in pair && pair.right ? [pair.right] : []))
    const distractors = "distractors" in entry ? entry.distractors ?? [] : []
    return [...rightItems, ...distractors]
  }, [entry])
  const usedRightItems = useMemo(() => new Set(assignments.values()), [assignments])

  // Shuffle both columns once on mount
  const shuffledPairs = useMemo(() => {
    return stableShuffle(entry.pairs, (pair) => `${pair.id}:${pair.left}`)
  }, [entry.pairs])

  const shuffledRightItems = useMemo(() => {
    return stableShuffle(allRightItems, (right) => right)
  }, [allRightItems])

  const updateLines = useCallback(() => {
    if (!containerRef.current) return

    const containerRect = containerRef.current.getBoundingClientRect()
    const newLines: Array<{ id: string; start: Point; end: Point; colorClass: string }> = []

    assignments.forEach((rightValue, leftId) => {
      const leftEl = leftRefs.current.get(leftId)
      // Find the right element by rightValue
      // We need to find its key. We use rightValue directly as a key or store by rightValue.
      const rightEl = rightRefs.current.get(rightValue)

      const pairIndex = entry.pairs.findIndex((p) => p.id === leftId)
      const colorIndex = pairIndex !== -1 ? pairIndex % CONNECTION_COLORS.length : 0
      const color = CONNECTION_COLORS[colorIndex]

      if (leftEl && rightEl) {
        const leftRect = leftEl.getBoundingClientRect()
        const rightRect = rightEl.getBoundingClientRect()

        newLines.push({
          id: `${leftId}-${rightValue}`,
          start: {
            x: leftRect.right - containerRect.left,
            y: leftRect.top - containerRect.top + leftRect.height / 2,
          },
          end: {
            x: rightRect.left - containerRect.left,
            y: rightRect.top - containerRect.top + rightRect.height / 2,
          },
          colorClass: color!.line,
        })
      }
    })

    setLines(newLines)
  }, [assignments, entry.pairs])

  // Update lines when assignments or window size changes
  useEffect(() => {
    updateLines()
    window.addEventListener("resize", updateLines)
    return () => window.removeEventListener("resize", updateLines)
  }, [updateLines])


  const handleLeftClick = (leftId: string) => {
    if (disabled || showFeedback) return
    // If already matched, clicking removes the connection
    if (assignments.has(leftId)) {
      handleRemoveAssignment(leftId)
      return
    }
    setSelectedLeft(leftId === selectedLeft ? null : leftId)
  }

  const handleRightClick = (rightValue: string) => {
    if (disabled || showFeedback) return

    // If already used and no left selected, clicking removes the connection
    if (usedRightItems.has(rightValue) && !selectedLeft) {
      const matchedLeftId = Array.from(assignments.entries()).find(([, r]) => r === rightValue)?.[0]
      if (matchedLeftId) handleRemoveAssignment(matchedLeftId)
      return
    }

    if (!selectedLeft) return

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
    <div className="space-y-6 relative" ref={containerRef}>
      {/* SVG Container for the lines */}
      <svg
        className="absolute inset-0 pointer-events-none w-full h-full z-10"
        style={{ minHeight: "100%" }}
      >
        {lines.map((l) => (
          <path
            key={l.id}
            d={`M ${l.start.x} ${l.start.y} C ${l.start.x + 50} ${l.start.y}, ${l.end.x - 50} ${l.end.y}, ${l.end.x} ${l.end.y}`}
            fill="none"
            stroke="currentColor"
            strokeWidth="3"
            className={`${l.colorClass} opacity-60`}
            strokeLinecap="round"
          />
        ))}
      </svg>
      <div className="grid grid-cols-2 gap-8 z-20 relative">
        {/* Left Column */}
        <div className="space-y-3">
          <h4 className="text-sm font-medium text-gray-600 dark:text-gray-400 mb-2">Items</h4>
          {shuffledPairs.map((pair) => {
            const isSelected = selectedLeft === pair.id
            const isMatched = assignments.has(pair.id)
            const originalIndex = entry.pairs.findIndex((p) => p.id === pair.id)
            const colorClass = CONNECTION_COLORS[originalIndex % CONNECTION_COLORS.length]!.card

            return (
              <div
                key={pair.id}
                ref={(el) => {
                  if (el) leftRefs.current.set(pair.id, el)
                  else leftRefs.current.delete(pair.id)
                }}
                className={`
                  p-4 rounded-lg border-2 transition-all cursor-pointer relative z-20
                  ${isSelected ? "border-blue-500 bg-blue-50 dark:bg-blue-950/30 ring-2 ring-blue-200 dark:ring-blue-800" : isMatched ? colorClass : "border-gray-200 bg-white dark:bg-gray-900 dark:border-gray-700"}
                  ${disabled || showFeedback ? "cursor-not-allowed opacity-75" : isSelected ? "" : isMatched ? "hover:brightness-95" : "hover:border-blue-300 dark:hover:border-blue-600"}
                `}
                onClick={() => handleLeftClick(pair.id)}
              >
                <div className="font-medium">{pair.left}</div>
              </div>
            )
          })}
        </div>

        {/* Right Column */}
        <div className="space-y-3">
          <h4 className="text-sm font-medium text-gray-600 dark:text-gray-400 mb-2">Options</h4>
          {shuffledRightItems.map((right, index) => {
            const isUsed = usedRightItems.has(right)
            
            // Find which left item connects to this right item to get its color
            let colorClass = "border-gray-200 bg-white dark:bg-gray-900 dark:border-gray-700"
            if (isUsed) {
              const matchedLeftId = Array.from(assignments.entries()).find(([, r]) => r === right)?.[0]
              if (matchedLeftId) {
                const pairIndex = entry.pairs.findIndex((p) => p.id === matchedLeftId)
                if (pairIndex !== -1) {
                  colorClass = CONNECTION_COLORS[pairIndex % CONNECTION_COLORS.length]!.card
                }
              }
            }

            return (
              <div
                key={index}
                ref={(el) => {
                  if (el) rightRefs.current.set(right, el)
                  else rightRefs.current.delete(right)
                }}
                className={`
                  p-4 rounded-lg border-2 transition-all relative z-20
                  ${colorClass}
                  ${selectedLeft && !isUsed ? "cursor-pointer hover:border-blue-400 hover:bg-blue-50 dark:hover:bg-blue-950/30" : ""}
                  ${isUsed && !disabled && !showFeedback ? "cursor-pointer hover:brightness-95" : ""}
                  ${disabled || showFeedback ? "cursor-not-allowed" : ""}
                `}
                onClick={() => handleRightClick(right)}
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
