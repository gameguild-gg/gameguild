"use client"

import { useState, useCallback } from "react"

interface UseQuizLogicProps {
  answers: { id: string; text: string; isCorrect: boolean }[]
  correctFeedback?: string
  incorrectFeedback?: string
  allowRetry: boolean
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
}: UseQuizLogicProps): UseQuizLogicReturn {
  const [selectedAnswers, setSelectedAnswers] = useState<string[]>([])
  const [showFeedback, setShowFeedback] = useState(false)
  const [isCorrect, setIsCorrect] = useState(false)

  const checkAnswers = () => {
    const correctAnswers = answers.filter((a) => a.isCorrect).map((a) => a.id)
    const isAllCorrect =
      correctAnswers.length === selectedAnswers.length && correctAnswers.every((id) => selectedAnswers.includes(id))

    setIsCorrect(isAllCorrect)
    setShowFeedback(true)

    // Não limpar as seleções quando allowRetry é false para manter visível o que foi escolhido
    // if (!allowRetry) {
    //   setSelectedAnswers([])
    // }
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
  }, [setSelectedAnswers, setShowFeedback, setIsCorrect])

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
