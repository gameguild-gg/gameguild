import { act, renderHook } from "@testing-library/react"
import { describe, expect, it } from "vitest"
import { FillBlankInputType, QuizEntryType, type SingleChoiceEntry } from "../types"
import { type FillInTheBlankLearnerEntry, toQuizLearnerEntry } from "../contracts"
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
    const entry = toQuizLearnerEntry(createSingleChoiceEntry())
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

  it("does not infer local grading from learner-safe fill-blank dropdown options", () => {
    const entry: FillInTheBlankLearnerEntry = {
      type: QuizEntryType.FillInTheBlank,
      stem: "Capital: ___",
      blanks: [
        {
          id: "capital",
          position: 0,
          input: {
            type: FillBlankInputType.Dropdown,
            options: ["Rome", "Paris"],
          },
        },
      ],
      settings: {
        allowRetry: true,
        showFeedback: true,
        showCorrectAnswer: false,
      },
    }
    const { result } = renderHook(() =>
      useQuizAnswers({ entry, submissionMode: "server-graded" }),
    )

    act(() => {
      result.current.updateAnswerState({ textAnswers: { capital: "Paris" } })
    })

    act(() => {
      result.current.checkAnswers()
    })

    expect(result.current.showFeedback).toBe(true)
    expect(result.current.isCorrect).toBeNull()
  })

  it("does not infer local grading from learner-safe fill-blank word-bank words", () => {
    const entry: FillInTheBlankLearnerEntry = {
      type: QuizEntryType.FillInTheBlank,
      stem: "Engine: ___",
      blanks: [
        {
          id: "engine",
          position: 0,
          input: {
            type: FillBlankInputType.WordBank,
            words: ["Unity", "Godot"],
          },
        },
      ],
      settings: {
        allowRetry: true,
        showFeedback: true,
        showCorrectAnswer: false,
      },
    }
    const { result } = renderHook(() =>
      useQuizAnswers({ entry, submissionMode: "server-graded" }),
    )

    act(() => {
      result.current.updateAnswerState({ textAnswers: { engine: "Godot" } })
    })

    act(() => {
      result.current.checkAnswers()
    })

    expect(result.current.showFeedback).toBe(true)
    expect(result.current.isCorrect).toBeNull()
  })
})
