import "@testing-library/jest-dom/vitest"
import { fireEvent, render, screen } from "@testing-library/react"
import { describe, expect, it } from "vitest"
import {
  QuizEntryType,
  type SingleChoiceEntry,
} from "./types"
import type {
  FormulaLearnerEntry,
  HighlightLearnerEntry,
  MatchingLearnerEntry,
  MultipleChoiceLearnerEntry,
} from "./contracts"
import { QuizDisplay } from "./quiz-display"

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

function createRedactedMultipleChoiceEntry(): MultipleChoiceLearnerEntry {
  return {
    type: QuizEntryType.MultipleChoice,
    stem: "Pick applicable engines.",
    options: [
      { id: "godot", text: "Godot" },
      { id: "unity", text: "Unity" },
    ],
    settings: {
      allowRetry: false,
      showFeedback: false,
      showCorrectAnswer: false,
    },
  }
}

function createRedactedMatchingEntry(): MatchingLearnerEntry {
  return {
    type: QuizEntryType.Matching,
    stem: "Match each country to its capital.",
    pairs: [{ id: "fr", left: "France" }],
    rightOptions: ["Paris", "Rome"],
    settings: {
      allowRetry: false,
      showFeedback: false,
      showCorrectAnswer: false,
    },
  }
}

function createRedactedHighlightEntry(): HighlightLearnerEntry {
  return {
    type: QuizEntryType.Highlight,
    stem: "Highlight the relevant words.",
    plainText: "Prototype before production",
    settings: {
      allowRetry: false,
      showFeedback: false,
      showCorrectAnswer: false,
    },
  }
}

function createFormulaLearnerEntryWithPrompt(): FormulaLearnerEntry {
  return {
    type: QuizEntryType.Formula,
    stem: "Find the formula.",
    variables: [
      { id: "x", name: "x", min: 1, max: 10, decimals: 0 },
      { id: "y", name: "y", min: 1, max: 10, decimals: 0 },
    ],
    decimalPlaces: 0,
    prompt: {
      variables: { x: 2, y: 3 },
      expectedResult: 7,
      decimalPlaces: 0,
    },
    settings: {
      allowRetry: false,
      showFeedback: false,
      showCorrectAnswer: false,
    },
  }
}

function createFormulaLearnerEntryWithoutPrompt(): FormulaLearnerEntry {
  return {
    type: QuizEntryType.Formula,
    stem: "Find the formula.",
    variables: [
      { id: "x", name: "x", min: 1, max: 10, decimals: 0 },
    ],
    decimalPlaces: 0,
    settings: {
      allowRetry: false,
      showFeedback: false,
      showCorrectAnswer: false,
    },
  }
}

describe("QuizDisplay", () => {
  it("shows correct feedback for practice quizzes", () => {
    render(<QuizDisplay entry={createSingleChoiceEntry()} />)

    fireEvent.click(screen.getByRole("button", { name: "B" }))
    fireEvent.click(screen.getByRole("button", { name: /submit answer/i }))

    expect(screen.getByText("Correct")).toBeInTheDocument()
  })

  it("only shows submitted feedback for server-graded quizzes", () => {
    render(
      <QuizDisplay
        entry={createSingleChoiceEntry()}
        submissionMode="server-graded"
      />,
    )

    fireEvent.click(screen.getByRole("button", { name: "B" }))
    fireEvent.click(screen.getByRole("button", { name: /submit answer/i }))

    expect(screen.getByText("Answer submitted.")).toBeInTheDocument()
    expect(screen.queryByText("Correct")).not.toBeInTheDocument()
  })

  it("renders redacted multiple choice entries without answer keys", () => {
    render(
      <QuizDisplay
        entry={createRedactedMultipleChoiceEntry()}
        submissionMode="server-graded"
      />,
    )

    fireEvent.click(screen.getByRole("button", { name: "Godot" }))
    fireEvent.click(screen.getByRole("button", { name: /submit answer/i }))

    expect(screen.getByText("Answer submitted.")).toBeInTheDocument()
  })

  it("renders redacted matching entries with separated right options", () => {
    render(
      <QuizDisplay
        entry={createRedactedMatchingEntry()}
        submissionMode="server-graded"
      />,
    )

    expect(screen.getByText("France")).toBeInTheDocument()
    expect(screen.getByText("Paris")).toBeInTheDocument()
  })

  it("renders redacted highlight entries without correct spans", () => {
    render(
      <QuizDisplay
        entry={createRedactedHighlightEntry()}
        submissionMode="server-graded"
      />,
    )

    expect(screen.getByText("Prototype")).toBeInTheDocument()
  })

  it("renders grading-enabled formula learner prompts without exposing the formula", () => {
    render(
      <QuizDisplay
        entry={createFormulaLearnerEntryWithPrompt()}
        submissionMode="server-graded"
      />,
    )

    expect(screen.getByText("x=2")).toBeInTheDocument()
    expect(screen.getByText("y=3")).toBeInTheDocument()
    expect(screen.getByText("? = 7")).toBeInTheDocument()
    expect(screen.getByRole("button", { name: /server check/i })).toBeDisabled()
    expect(screen.queryByText(/test your formula/i)).not.toBeInTheDocument()
  })

  it("renders grading-enabled formula learner entries without a prompt in a neutral state", () => {
    render(
      <QuizDisplay
        entry={createFormulaLearnerEntryWithoutPrompt()}
        submissionMode="server-graded"
      />,
    )

    expect(screen.getByText("x=?")).toBeInTheDocument()
    expect(screen.getByText("? = ?")).toBeInTheDocument()
    expect(screen.getByRole("button", { name: /server check/i })).toBeDisabled()
  })
})
