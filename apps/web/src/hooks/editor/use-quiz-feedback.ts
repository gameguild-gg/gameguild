"use client"

import { useState, useEffect } from "react"

interface UseQuizFeedbackProps {
  isCorrect: boolean
  correctFeedback: string
  incorrectFeedback: string
}

interface UseQuizFeedbackReturn {
  feedbackMessage: string
  feedbackClassName: string
}

export function useQuizFeedback({
  isCorrect,
  correctFeedback,
  incorrectFeedback,
}: UseQuizFeedbackProps): UseQuizFeedbackReturn {
  const [feedbackMessage, setFeedbackMessage] = useState("")
  const [feedbackClassName, setFeedbackClassName] = useState("")

  useEffect(() => {
    if (isCorrect) {
      setFeedbackMessage(correctFeedback || "Correct!")
      setFeedbackClassName("bg-green-500/10 text-green-600")
    } else {
      setFeedbackMessage(incorrectFeedback || "Try again")
      setFeedbackClassName("bg-red-500/10 text-red-600")
    }
  }, [isCorrect, correctFeedback, incorrectFeedback])

  return {
    feedbackMessage,
    feedbackClassName,
  }
}
