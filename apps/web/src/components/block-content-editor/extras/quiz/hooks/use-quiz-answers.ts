"use client"

import { useCallback, useState } from "react"
import {
  type QuizAnswerState,
  type QuizEntry,
  createEmptyAnswerState,
} from "../types"

interface UseQuizAnswersProps {
  entry: QuizEntry
}

interface UseQuizAnswersReturn {
  answerState: QuizAnswerState
  updateAnswerState: (updates: Partial<QuizAnswerState>) => void
  showFeedback: boolean
  isCorrect: boolean | null
  checkAnswers: () => void
  resetQuiz: () => void
}

export function useQuizAnswers(_props: UseQuizAnswersProps): UseQuizAnswersReturn {
  const [answerState, setAnswerState] = useState<QuizAnswerState>(createEmptyAnswerState())
  const [showFeedback, setShowFeedback] = useState(false)
  const [isCorrect, setIsCorrect] = useState<boolean | null>(null)

  const updateAnswerState = useCallback((updates: Partial<QuizAnswerState>) => {
    setAnswerState((prev) => ({ ...prev, ...updates }))
  }, [])

  const checkAnswers = useCallback(() => {
    // The browser may collect answers, but correctness is always produced by the server.
    setIsCorrect(null)
    setShowFeedback(true)
  }, [])

  const resetQuiz = useCallback(() => {
    setAnswerState(createEmptyAnswerState())
    setShowFeedback(false)
    setIsCorrect(null)
  }, [])

  return {
    answerState,
    updateAnswerState,
    showFeedback,
    isCorrect,
    checkAnswers,
    resetQuiz,
  }
}
