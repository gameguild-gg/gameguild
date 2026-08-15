import { describe, expect, it } from "vitest"
import {
  isCompleteQuizAuthoringEntry,
  toQuizLearnerEntry,
  validateQuizAuthoringEntry,
} from "./contracts"
import {
  FillBlankInputType,
  QuizEntryType,
  type FillInTheBlankEntry,
  type FormulaEntry,
  type HighlightEntry,
  type MatchingEntry,
  type MultipleChoiceEntry,
  type SingleChoiceEntry,
} from "./types"
import { createLocalAssetUri } from "@game-guild/assets"

describe("quiz contracts", () => {
  it("keeps learner-visible attachments and redacts author-only attachments", () => {
    const source: SingleChoiceEntry = {
      type: QuizEntryType.SingleChoice,
      stem: "Inspect the diagram.",
      options: [{ id: "a", text: "A" }],
      correctOptionId: "a",
      attachments: {
        learnerVisible: [{
          assetUri: createLocalAssetUri("7776453f-1123-4f56-8abc-1234567890ab"),
          name: "diagram.png",
          mimeType: "image/png",
          size: 10,
          role: "question",
        }],
        authorOnly: [{
          assetUri: createLocalAssetUri("8776453f-1123-4f56-8abc-1234567890ab"),
          name: "answer-key.pdf",
          mimeType: "application/pdf",
          size: 20,
          role: "answer",
        }],
      },
      settings: { allowRetry: false },
    }

    const learnerEntry = toQuizLearnerEntry(source)
    expect(learnerEntry.attachments).toEqual({
      learnerVisible: [source.attachments!.learnerVisible![0]],
    })
    expect("authorOnly" in learnerEntry.attachments!).toBe(false)
  })

  it("redacts answer-key fields from authoring entries", () => {
    const source: MatchingEntry = {
      type: QuizEntryType.Matching,
      stem: "Match each country to its capital.",
      pairs: [
        { id: "fr", left: "France", right: "Paris" },
        { id: "it", left: "Italy", right: "Rome" },
      ],
      distractors: ["Madrid"],
      feedback: {
        correct: "Correct",
        incorrect: "Try again",
        general: "Review capitals.",
      },
      settings: {
        allowRetry: false,
        showFeedback: false,
        showCorrectAnswer: false,
      },
    }

    const learnerEntry = toQuizLearnerEntry(source)

    expect(learnerEntry).toEqual({
      type: QuizEntryType.Matching,
      stem: "Match each country to its capital.",
      pairs: [
        { id: "fr", left: "France" },
        { id: "it", left: "Italy" },
      ],
      rightOptions: ["Rome", "Madrid", "Paris"],
      feedback: {
        general: "Review capitals.",
      },
      settings: {
        allowRetry: false,
        showFeedback: false,
        showCorrectAnswer: false,
      },
    })
    expect("correct" in learnerEntry.feedback!).toBe(false)
    expect("right" in learnerEntry.pairs[0]!).toBe(false)
  })

  it("redacts per-type answer keys that commonly leak through generic omission", () => {
    const fill: FillInTheBlankEntry = {
      type: QuizEntryType.FillInTheBlank,
      stem: "Capital: ___",
      blanks: [
        {
          id: "blank-1",
          position: 0,
          input: {
            type: FillBlankInputType.Text,
            acceptedAnswers: ["Paris"],
          },
        },
      ],
      settings: {
        allowRetry: false,
        showFeedback: false,
        showCorrectAnswer: false,
      },
    }
    const highlight: HighlightEntry = {
      type: QuizEntryType.Highlight,
      stem: "Highlight the target.",
      sourceText: "The __target__",
      plainText: "The target",
      highlights: [{ start: 4, end: 10 }],
      settings: {
        allowRetry: false,
        showFeedback: false,
        showCorrectAnswer: false,
      },
    }

    expect(toQuizLearnerEntry(fill)).toMatchObject({
      blanks: [{ input: { type: "TEXT" } }],
    })
    expect(toQuizLearnerEntry(highlight)).toEqual({
      type: QuizEntryType.Highlight,
      stem: "Highlight the target.",
      plainText: "The target",
      settings: {
        allowRetry: false,
        showFeedback: false,
        showCorrectAnswer: false,
      },
    })
  })

  it("redacts formula answer material without inventing server-generated prompts", () => {
    const source: FormulaEntry = {
      type: QuizEntryType.Formula,
      stem: "Find the formula.",
      variables: [{ id: "x", name: "x", min: 1, max: 10, decimals: 0 }],
      formula: "x * 2",
      toleranceType: "absolute",
      tolerance: 0,
      decimalPlaces: 0,
      settings: {
        allowRetry: false,
        showFeedback: false,
        showCorrectAnswer: false,
      },
    }

    const learnerEntry = toQuizLearnerEntry(source)

    expect(learnerEntry).toEqual({
      type: QuizEntryType.Formula,
      stem: "Find the formula.",
      variables: [{ id: "x", name: "x", min: 1, max: 10, decimals: 0 }],
      decimalPlaces: 0,
      settings: {
        allowRetry: false,
        showFeedback: false,
        showCorrectAnswer: false,
      },
    })
    expect("formula" in learnerEntry).toBe(false)
    expect("tolerance" in learnerEntry).toBe(false)
    expect("prompt" in learnerEntry).toBe(false)
  })

  it("validates source answer keys before grading contracts consume them", () => {
    const incompleteSingle: SingleChoiceEntry = {
      type: QuizEntryType.SingleChoice,
      stem: "Pick one.",
      options: [{ id: "a", text: "A" }],
      correctOptionId: "missing",
      settings: {
        allowRetry: true,
        showFeedback: true,
        showCorrectAnswer: true,
      },
    }
    const incompleteMultiple: MultipleChoiceEntry = {
      type: QuizEntryType.MultipleChoice,
      stem: "Pick many.",
      options: [{ id: "a", text: "A" }],
      correctOptionIds: [],
      settings: {
        allowRetry: true,
        showFeedback: true,
        showCorrectAnswer: true,
      },
    }

    expect(validateQuizAuthoringEntry(incompleteSingle)).toEqual([
      expect.objectContaining({
        code: "invalid-answer-key",
        path: "correctOptionId",
      }),
    ])
    expect(validateQuizAuthoringEntry(incompleteMultiple)).toEqual([
      expect.objectContaining({
        code: "missing-answer-key",
        path: "correctOptionIds",
      }),
    ])
    expect(isCompleteQuizAuthoringEntry(incompleteSingle)).toBe(false)
  })
})
