import { act, renderHook } from "@testing-library/react"
import { describe, expect, it } from "vitest"
import { QuizEntryType, type SingleChoiceEntry } from "../types"
import { useQuizAnswers } from "./use-quiz-answers"

function createSingleChoiceEntry(): SingleChoiceEntry {
  return {
    type: QuizEntryType.SingleChoice,
    stem: "What is the answer?",
    options: [
      { id: "a", text: "A" },
      { id: "b", text: "B" },
    ],
    correctOptionId: "b",
    feedback: {
      correct: "Correct",
      incorrect: "Incorrect",
    },
    settings: {
      allowRetry: true,
      showFeedback: true,
      showCorrectAnswer: true,
    },
  }
}

describe("useQuizAnswers", () => {
  it("grades locally when quiz submission is practice-only", () => {
    const entry = createSingleChoiceEntry()
    const { result } = renderHook(() =>
      useQuizAnswers({ entry, submissionMode: "local-practice" }),
    )

    act(() => {
      result.current.updateAnswerState({ selectedOptionIds: ["b"] })
    })

    act(() => {
      result.current.checkAnswers()
    })

    expect(result.current.showFeedback).toBe(true)
    expect(result.current.isCorrect).toBe(true)
  })

  it("does not expose correctness when quiz submission is server-graded", () => {
    const entry = createSingleChoiceEntry()
    const { result } = renderHook(() =>
      useQuizAnswers({ entry, submissionMode: "server-graded" }),
    )

    act(() => {
      result.current.updateAnswerState({ selectedOptionIds: ["b"] })
    })

    act(() => {
      result.current.checkAnswers()
    })

    expect(result.current.showFeedback).toBe(true)
    expect(result.current.isCorrect).toBeNull()
  })
})
