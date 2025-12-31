/**
 * Unified Quiz Renderer
 * Routes to the appropriate question type renderer based on quiz type
 */

"use client"

import { MultipleChoiceRenderer } from "./multiple-choice-renderer"
import { TrueFalseRenderer } from "./true-false-renderer"
import { FillBlankRenderer } from "./fill-blank-renderer"
import { ShortAnswerRenderer } from "./short-answer-renderer"
import { EssayRenderer } from "./essay-renderer"
import { RatingRenderer } from "./rating-renderer"
import { CategorizationRenderer } from "./categorization-renderer"
import type { QuizData } from "../../quiz-node"

interface QuizRendererProps {
  data: QuizData
  selectedAnswers: string[]
  onAnswerChange: (answers: string[]) => void
  disabled?: boolean
  showFeedback?: boolean
}

export function QuizRenderer({ data, selectedAnswers, onAnswerChange, disabled, showFeedback }: QuizRendererProps) {
  const questionType = data.questionType

  const handleAnswerToggle = (answerId: string) => {
    if (selectedAnswers.includes(answerId)) {
      onAnswerChange(selectedAnswers.filter((id) => id !== answerId))
    } else {
      onAnswerChange([...selectedAnswers, answerId])
    }
  }

  const handleSingleAnswer = (answer: string) => {
    onAnswerChange([answer])
  }

  const handleFillBlankChange = (index: number, value: string) => {
    const newAnswers = [...selectedAnswers]
    newAnswers[index] = value
    onAnswerChange(newAnswers)
  }

  switch (questionType) {
    case "multiple-choice":
      return (
        <MultipleChoiceRenderer
          question={{ question: data.question, answers: data.answers }}
          selectedAnswers={selectedAnswers}
          onAnswerToggle={handleAnswerToggle}
          disabled={disabled}
          showFeedback={showFeedback}
        />
      )

    case "true-false": {
      const selectedAnswer = selectedAnswers[0] === "true" ? true : selectedAnswers[0] === "false" ? false : null
      return (
        <TrueFalseRenderer
          question={{
            question: data.question,
            correctAnswer: data.answers.find((a) => a.isCorrect)?.id === "true",
          }}
          selectedAnswer={selectedAnswer}
          onAnswerSelect={(answer) => handleSingleAnswer(answer.toString())}
          disabled={disabled}
          showFeedback={showFeedback}
        />
      )
    }

    case "fill-blank":
      return (
        <FillBlankRenderer
          question={{ question: data.question }}
          answers={selectedAnswers}
          onAnswerChange={handleFillBlankChange}
          disabled={disabled}
          showFeedback={showFeedback}
        />
      )

    case "short-answer":
      return (
        <ShortAnswerRenderer
          question={{ question: data.question }}
          answer={selectedAnswers[0] || ""}
          onAnswerChange={handleSingleAnswer}
          disabled={disabled}
          showFeedback={showFeedback}
        />
      )

    case "essay":
      return (
        <EssayRenderer
          question={{ question: data.question }}
          answer={selectedAnswers[0] || ""}
          onAnswerChange={handleSingleAnswer}
          disabled={disabled}
          showFeedback={showFeedback}
        />
      )

    case "rating": {
      const selectedRating = selectedAnswers[0] ? parseInt(selectedAnswers[0], 10) : null
      return (
        <RatingRenderer
          question={{
            question: data.question,
            scale: data.ratingScale || { min: 1, max: 5, step: 1 },
          }}
          selectedRating={selectedRating}
          onRatingSelect={(rating) => handleSingleAnswer(rating.toString())}
          disabled={disabled}
          showFeedback={showFeedback}
        />
      )
    }

    case "categorization":
      return (
        <CategorizationRenderer
          data={data}
          selectedAnswers={selectedAnswers}
          onAnswerChange={onAnswerChange}
          disabled={disabled}
          showFeedback={showFeedback}
        />
      )

    default:
      return <div className="text-red-600">Unsupported question type: {questionType}</div>
  }
}
