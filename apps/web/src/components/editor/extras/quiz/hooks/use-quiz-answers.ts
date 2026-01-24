/**
 * Quiz Answer Validation Hook
 * Manages quiz state and answer validation for all question types
 */

"use client"

import { useState, useCallback, useMemo } from "react"
import {
  type QuizEntry,
  type QuizAnswerState,
  QuizEntryType,
  createEmptyAnswerState,
  type FillBlankTextInput,
  FillBlankInputType,
} from "../types"

interface UseQuizAnswersProps {
  entry: QuizEntry
}

interface UseQuizAnswersReturn {
  answerState: QuizAnswerState
  updateAnswerState: (updates: Partial<QuizAnswerState>) => void
  showFeedback: boolean
  isCorrect: boolean
  checkAnswers: () => void
  resetQuiz: () => void
}

export function useQuizAnswers({ entry }: UseQuizAnswersProps): UseQuizAnswersReturn {
  const [answerState, setAnswerState] = useState<QuizAnswerState>(createEmptyAnswerState())
  const [showFeedback, setShowFeedback] = useState(false)
  const [isCorrect, setIsCorrect] = useState(false)

  const updateAnswerState = useCallback((updates: Partial<QuizAnswerState>) => {
    setAnswerState((prev) => ({ ...prev, ...updates }))
  }, [])

  const checkAnswers = useCallback(() => {
    let correct = false

    switch (entry.type) {
      case QuizEntryType.SingleChoice: {
        const selectedId = answerState.selectedOptionIds[0]
        correct = selectedId === entry.correctOptionId
        break
      }

      case QuizEntryType.MultipleChoice: {
        const selected = new Set(answerState.selectedOptionIds)
        const correctIds = new Set(entry.correctOptionIds)
        correct =
          selected.size === correctIds.size &&
          [...selected].every((id) => correctIds.has(id))
        break
      }

      case QuizEntryType.TrueFalse: {
        const selectedId = answerState.selectedOptionIds[0]
        correct = selectedId === (entry.correctAnswer ? "true" : "false")
        break
      }

      case QuizEntryType.FillInTheBlank: {
        correct = entry.blanks.every((blank) => {
          const rawAnswer = (answerState.textAnswers[blank.id] || "").trim()
          if (!rawAnswer) return false

          switch (blank.input.type) {
            case FillBlankInputType.Text: {
              const textInput = blank.input as FillBlankTextInput
              const caseSensitive = textInput.caseSensitive ?? false
              return textInput.acceptedAnswers.some((accepted) =>
                caseSensitive
                  ? rawAnswer === accepted
                  : rawAnswer.toLowerCase() === accepted.toLowerCase()
              )
            }
            case FillBlankInputType.Dropdown:
              // First option is the correct answer
              return rawAnswer === blank.input.options[0]
            case FillBlankInputType.WordBank:
              // Extract just the word part (format is "word|uniqueIndex")
              const userWord = rawAnswer.includes("|") ? rawAnswer.split("|")[0] : rawAnswer
              // First word is the correct answer
              return userWord === blank.input.words[0]
            default:
              return false
          }
        })
        break
      }

      case QuizEntryType.ShortAnswer: {
        const userAnswer = (answerState.textAnswers["main"] || "").trim()
        const caseSensitive = entry.caseSensitive ?? false
        correct = entry.acceptedAnswers.some((accepted) =>
          caseSensitive
            ? userAnswer === accepted
            : userAnswer.toLowerCase() === accepted.toLowerCase()
        )
        break
      }

      case QuizEntryType.Essay: {
        // Essays are manually graded, always show as "submitted"
        correct = true
        break
      }

      case QuizEntryType.Matching: {
        // TODO: Implement matching validation
        correct = false
        break
      }

      case QuizEntryType.Ordering: {
        const userOrder = answerState.ordering
        const correctOrder = [...entry.items]
          .sort((a, b) => a.correctPosition - b.correctPosition)
          .map((item) => item.id)
        correct =
          userOrder.length === correctOrder.length &&
          userOrder.every((id, index) => id === correctOrder[index])
        break
      }

      case QuizEntryType.Categorization: {
        correct = entry.items.every((item) => {
          const assignedCategories = new Set(answerState.categorizations[item.id] || [])
          const correctCategories = new Set(item.correctCategoryIds)
          return (
            assignedCategories.size === correctCategories.size &&
            [...assignedCategories].every((id) => correctCategories.has(id))
          )
        })
        break
      }

      case QuizEntryType.Rating: {
        if (entry.correctRating !== undefined) {
          correct = answerState.rating === entry.correctRating
        } else {
          // Any rating is accepted
          correct = answerState.rating !== undefined
        }
        break
      }
    }

    setIsCorrect(correct)
    setShowFeedback(true)
  }, [entry, answerState])

  const resetQuiz = useCallback(() => {
    setAnswerState(createEmptyAnswerState())
    setShowFeedback(false)
    setIsCorrect(false)
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
