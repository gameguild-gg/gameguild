import "@testing-library/jest-dom/vitest"
import { fireEvent, render, screen } from "@testing-library/react"
import { describe, expect, it } from "vitest"
import { QuizEntryType, type SingleChoiceEntry } from "./types"
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
})
