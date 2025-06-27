"use client"

import { Input } from "@/components/editor/ui/input"

interface FillBlankQuestionProps {
  question: string
  userAnswer: string
  onAnswerChange: (answer: string) => void
  showFeedback: boolean
  isCorrect: boolean
  disabled?: boolean
}

export function FillBlankQuestion({ question, userAnswer, onAnswerChange, disabled = false }: FillBlankQuestionProps) {
  // Replace blanks in question with input fields
  const renderQuestionWithBlanks = () => {
    const parts = question.split("___")
    if (parts.length === 1) {
      return (
        <div className="space-y-4">
          <div className="font-semibold">{question}</div>
          <Input
            value={userAnswer}
            onChange={(e) => onAnswerChange(e.target.value)}
            disabled={disabled}
            placeholder="Enter your answer"
          />
        </div>
      )
    }

    return (
      <div className="font-semibold">
        {parts.map((part, index) => (
          <span key={index}>
            {part}
            {index < parts.length - 1 && (
              <Input
                className="inline-block w-32 mx-2"
                value={userAnswer}
                onChange={(e) => onAnswerChange(e.target.value)}
                disabled={disabled}
                placeholder="___"
              />
            )}
          </span>
        ))}
      </div>
    )
  }

  return <div className="space-y-4">{renderQuestionWithBlanks()}</div>
}
