/**
 * Quiz Answer Logic Hook
 * Manages quiz state and answer validation
 */

"use client"

import { useState, useCallback } from "react"
import type { QuizData } from "../../quiz-node"

interface UseQuizAnswersProps {
  data: QuizData
}

interface UseQuizAnswersReturn {
  selectedAnswers: string[]
  setSelectedAnswers: (answers: string[]) => void
  showFeedback: boolean
  isCorrect: boolean
  checkAnswers: () => void
  resetQuiz: () => void
}

export function useQuizAnswers({ data }: UseQuizAnswersProps): UseQuizAnswersReturn {
  const [selectedAnswers, setSelectedAnswers] = useState<string[]>([])
  const [showFeedback, setShowFeedback] = useState(false)
  const [isCorrect, setIsCorrect] = useState(false)

  const checkAnswers = useCallback(() => {
    let correct = false

    switch (data.questionType) {
      case "multiple-choice": {
        const correctIds = data.answers.filter((a: any) => a.isCorrect).map((a: any) => a.id)
        correct =
          correctIds.length === selectedAnswers.length &&
          correctIds.every((id: string) => selectedAnswers.includes(id))
        break
      }

      case "true-false": {
        const correctAnswer = data.answers.find((a: any) => a.isCorrect)?.id
        correct = selectedAnswers[0] === correctAnswer
        break
      }

      case "fill-blank": {
        if (data.fillBlankFields) {
          correct = data.fillBlankFields.every((field: any, index: number) => {
            const userAnswer = (selectedAnswers[index] || "").toLowerCase().trim()
            if (!userAnswer) return false

            // Check expected words
            const matchesExpected = field.expectedWords.some(
              (word: string) => word.toLowerCase().trim() === userAnswer
            )

            // Check alternatives
            const matchesAlternatives = field.alternatives.some((alt: any) =>
              alt.words.some((word: string) => word.toLowerCase().trim() === userAnswer)
            )

            return matchesExpected || matchesAlternatives
          })
        }
        break
      }

      case "short-answer": {
        const userAnswer = (selectedAnswers[0] || "").toLowerCase().trim()
        const acceptedAnswers = data.answers
          .filter((a: any) => a.isCorrect)
          .map((a: any) => a.text.toLowerCase().trim())
        correct = acceptedAnswers.some((answer: string) => answer === userAnswer)
        break
      }

      case "rating": {
        const userRating = selectedAnswers[0] ? parseInt(selectedAnswers[0], 10) : null
        correct = userRating === data.correctRating
        break
      }

      case "essay":
      case "matching":
      case "ordering":
        // These require manual grading or more complex logic
        correct = false
        break
    }

    setIsCorrect(correct)
    setShowFeedback(true)
  }, [data, selectedAnswers])

  const resetQuiz = useCallback(() => {
    setSelectedAnswers([])
    setShowFeedback(false)
    setIsCorrect(false)
  }, [])

  return {
    selectedAnswers,
    setSelectedAnswers,
    showFeedback,
    isCorrect,
    checkAnswers,
    resetQuiz,
  }
}
