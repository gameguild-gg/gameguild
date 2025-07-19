"use client"

import { Button } from "@/components/editor/ui/button"

interface RatingQuestionProps {
  question: string
  maxScore: number
  userRating: number | null
  onRatingChange: (rating: number) => void
  showFeedback: boolean
  isCorrect: boolean
  disabled?: boolean
}

export function RatingQuestion({
  question,
  maxScore = 5,
  userRating,
  onRatingChange,
  disabled = false,
}: RatingQuestionProps) {
  return (
    <div className="space-y-4">
      <div className="font-semibold">{question}</div>
      <div className="flex gap-2">
        {Array.from({ length: maxScore }, (_, i) => i + 1).map((rating) => (
          <Button
            key={rating}
            variant={userRating === rating ? "default" : "outline"}
            onClick={() => onRatingChange(rating)}
            disabled={disabled}
            className="w-12 h-12"
          >
            {rating}
          </Button>
        ))}
      </div>
      <div className="text-sm text-gray-600">Scale: 1 (lowest) to {maxScore} (highest)</div>
    </div>
  )
}
