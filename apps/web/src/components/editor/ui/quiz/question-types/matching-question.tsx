"use client"

import { useState } from "react"
import { Button } from "@/components/editor/ui/button"

interface MatchingPair {
  left: string
  right: string
}

interface MatchingQuestionProps {
  question: string
  pairs: MatchingPair[]
  userMatches: Record<string, string>
  onMatchChange: (leftItem: string, rightItem: string) => void
  showFeedback: boolean
  isCorrect: boolean
  disabled?: boolean
}

export function MatchingQuestion({
  question,
  pairs,
  userMatches,
  onMatchChange,
  disabled = false,
}: MatchingQuestionProps) {
  const [selectedLeft, setSelectedLeft] = useState<string | null>(null)

  const leftItems = pairs.map((p) => p.left)
  const rightItems = [...pairs.map((p) => p.right)].sort(() => Math.random() - 0.5) // Shuffle right items

  const handleLeftClick = (item: string) => {
    if (disabled) return
    setSelectedLeft(selectedLeft === item ? null : item)
  }

  const handleRightClick = (item: string) => {
    if (disabled || !selectedLeft) return
    onMatchChange(selectedLeft, item)
    setSelectedLeft(null)
  }

  return (
    <div className="space-y-4">
      <div className="font-semibold">{question}</div>
      <div className="grid grid-cols-2 gap-8">
        <div className="space-y-2">
          <div className="font-medium text-sm text-gray-600">Match these items:</div>
          {leftItems.map((item) => (
            <Button
              key={item}
              variant={selectedLeft === item ? "default" : "outline"}
              onClick={() => handleLeftClick(item)}
              disabled={disabled}
              className="w-full justify-start"
            >
              {item}
              {userMatches[item] && (
                <span className="ml-2 text-xs bg-blue-100 px-2 py-1 rounded">→ {userMatches[item]}</span>
              )}
            </Button>
          ))}
        </div>
        <div className="space-y-2">
          <div className="font-medium text-sm text-gray-600">With these options:</div>
          {rightItems.map((item) => (
            <Button
              key={item}
              variant="outline"
              onClick={() => handleRightClick(item)}
              disabled={disabled || !selectedLeft}
              className="w-full justify-start"
            >
              {item}
            </Button>
          ))}
        </div>
      </div>
    </div>
  )
}
