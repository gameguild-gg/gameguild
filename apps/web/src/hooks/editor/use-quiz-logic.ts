"use client"

import { useState, useCallback } from "react"
import type { FillBlankField } from "@/components/editor/nodes/quiz-node"

interface UseQuizLogicProps {
  answers: { id: string; text: string; isCorrect: boolean }[]
  correctFeedback?: string
  incorrectFeedback?: string
  allowRetry: boolean
  questionType?: string
  fillBlankFields?: FillBlankField[]
}

interface UseQuizLogicReturn {
  selectedAnswers: string[]
  setSelectedAnswers: (answers: string[]) => void
  showFeedback: boolean
  setShowFeedback: (show: boolean) => void
  isCorrect: boolean
  setIsCorrect: (correct: boolean) => void
  resetQuiz: () => void
  checkAnswers: () => void
  toggleAnswer: (id: string) => void
}

export function useQuizLogic({
  answers,
  allowRetry,
  correctFeedback,
  incorrectFeedback,
  questionType,
  fillBlankFields,
}: UseQuizLogicProps): UseQuizLogicReturn {
  const [selectedAnswers, setSelectedAnswers] = useState<string[]>([])
  const [showFeedback, setShowFeedback] = useState(false)
  const [isCorrect, setIsCorrect] = useState(false)

  const checkAnswers = () => {
    let isAllCorrect = false

    if (questionType === "fill-blank" && fillBlankFields) {
      // Check fill-blank answers
      isAllCorrect = fillBlankFields.every((field, index) => {
        const userAnswer = selectedAnswers[index] || ""
        if (!userAnswer.trim()) return false
        
        const allAcceptableWords = [
          ...field.expectedWords,
          ...field.alternatives.flatMap(alt => alt.words)
        ]
        
        return allAcceptableWords.some(word => 
          word.toLowerCase().trim() === userAnswer.toLowerCase().trim()
        )
      })
    } else {
      // Check other question types
      const correctAnswers = answers.filter((a) => a.isCorrect).map((a) => a.id)
      isAllCorrect =
        correctAnswers.length === selectedAnswers.length && 
        correctAnswers.every((id) => selectedAnswers.includes(id))
    }

    setIsCorrect(isAllCorrect)
    setShowFeedback(true)
  }

  const toggleAnswer = (id: string) => {
    setSelectedAnswers((prevSelected) => {
      if (prevSelected.includes(id)) {
        return prevSelected.filter((answerId) => answerId !== id)
      } else {
        return [...prevSelected, id]
      }
    })
  }

  const resetQuiz = useCallback(() => {
    setSelectedAnswers([])
    setShowFeedback(false)
    setIsCorrect(false)
  }, [])

  return {
    selectedAnswers,
    setSelectedAnswers,
    showFeedback,
    setShowFeedback,
    isCorrect,
    setIsCorrect,
    resetQuiz,
    checkAnswers,
    toggleAnswer,
  }
}
