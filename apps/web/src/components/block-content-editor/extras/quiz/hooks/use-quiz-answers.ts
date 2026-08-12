/**
 * Quiz Answer Validation Hook
 * Manages quiz state and answer validation for all question types.
 */

"use client"

import { useCallback, useState } from "react"
import { gradeQuizAnswer } from "@game-guild/grading"
import type { QuizPracticeEntry, QuizRuntimeEntry } from "../contracts"
import {
  type QuizAnswerState,
  QuizEntryType,
  createEmptyAnswerState,
  type FillBlankNumberInput,
  FillBlankInputType,
  type FillBlankTextInput,
} from "../types"
import { evaluateFormula, generateVariableValue, validateFormula } from "../utils/formula-evaluator"

export type QuizSubmissionMode = "local-practice" | "server-graded"

export type UseQuizAnswersProps =
  | {
    entry: QuizPracticeEntry
    submissionMode?: "local-practice"
  }
  | {
    entry: QuizRuntimeEntry
    submissionMode: "server-graded"
  }

/**
 * Extracts a normalized formatting structure from a Lexical editor state.
 * Accepts either the serialized JSON string or a parsed `SerializedEditorState`
 * object. Returns an array of `{ type, format, children }` per node, stripping
 * text content so only structure and formatting flags are compared.
 */
function extractFormattingStructure(input: string | object): unknown[] | null {
  try {
    const state = typeof input === "string" ? JSON.parse(input) : input
    const root = (state as { root?: { children?: unknown[] } })?.root
    if (!root?.children) return null

    function normalizeNode(node: Record<string, unknown>): Record<string, unknown> {
      const result: Record<string, unknown> = { type: node.type }

      if (node.format !== undefined) result.format = node.format
      if (node.tag !== undefined) result.tag = node.tag
      if (node.listType !== undefined) result.listType = node.listType

      if (Array.isArray(node.children)) {
        result.children = (node.children as Record<string, unknown>[]).map(normalizeNode)
      }

      return result
    }

    return (root.children as Record<string, unknown>[]).map(normalizeNode)
  } catch {
    return null
  }
}

function compareFormattingStructure(expected: string | object, userSerialized: string): boolean {
  const expectedStructure = extractFormattingStructure(expected)
  const user = extractFormattingStructure(userSerialized)
  if (!expectedStructure || !user) return false

  return JSON.stringify(expectedStructure) === JSON.stringify(user)
}

interface UseQuizAnswersReturn {
  answerState: QuizAnswerState
  updateAnswerState: (updates: Partial<QuizAnswerState>) => void
  showFeedback: boolean
  isCorrect: boolean | null
  checkAnswers: () => void
  resetQuiz: () => void
}

export function useQuizAnswers(props: UseQuizAnswersProps): UseQuizAnswersReturn {
  const practiceEntry = getPracticeEntry(props)
  const [answerState, setAnswerState] = useState<QuizAnswerState>(createEmptyAnswerState())
  const [showFeedback, setShowFeedback] = useState(false)
  const [isCorrect, setIsCorrect] = useState<boolean | null>(null)

  const updateAnswerState = useCallback((updates: Partial<QuizAnswerState>) => {
    setAnswerState((prev) => ({ ...prev, ...updates }))
  }, [])

  const checkAnswers = useCallback(() => {
    if (!practiceEntry) {
      setIsCorrect(null)
      setShowFeedback(true)
      return
    }

    const packageResult = gradeQuizAnswer(practiceEntry, answerState)
    if (packageResult.status === "graded") {
      setIsCorrect(packageResult.isCorrect === true)
      setShowFeedback(true)
      return
    }

    if (packageResult.status === "pending") {
      setIsCorrect(true)
      setShowFeedback(true)
      return
    }

    let correct = false

    switch (practiceEntry.type) {
      case QuizEntryType.SingleChoice: {
        const selectedId = answerState.selectedOptionIds[0]
        correct = selectedId === practiceEntry.correctOptionId
        break
      }

      case QuizEntryType.MultipleChoice: {
        const selected = new Set(answerState.selectedOptionIds)
        const correctIds = new Set(practiceEntry.correctOptionIds)
        correct =
          correctIds.size > 0 &&
          selected.size === correctIds.size &&
          [...selected].every((id) => correctIds.has(id))
        break
      }

      case QuizEntryType.TrueFalse: {
        const selectedId = answerState.selectedOptionIds[0]
        correct = selectedId === (practiceEntry.correctAnswer ? "true" : "false")
        break
      }

      case QuizEntryType.FillInTheBlank: {
        correct = practiceEntry.blanks.every((blank) => {
          const rawAnswer = (answerState.textAnswers[blank.id] || "").trim()
          if (!rawAnswer) return false

          switch (blank.input.type) {
            case FillBlankInputType.Text: {
              const textInput = blank.input as FillBlankTextInput
              const caseSensitive = textInput.caseSensitive ?? false
              const acceptedAnswers = textInput.acceptedAnswers ?? []
              return acceptedAnswers.length > 0 && acceptedAnswers.some((accepted) =>
                caseSensitive
                  ? rawAnswer === accepted
                  : rawAnswer.toLowerCase() === accepted.toLowerCase(),
              )
            }

            case FillBlankInputType.Number: {
              const numberInput = blank.input as FillBlankNumberInput
              let numericStr = rawAnswer

              if (numberInput.unit) {
                numericStr = numericStr
                  .replace(new RegExp(`\\s*${numberInput.unit.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}\\s*$`), "")
                  .trim()

                if (numberInput.requireUnit && numericStr === rawAnswer) return false
              }

              const userNumber = Number.parseFloat(numericStr)
              const correctValue = numberInput.correctValue
              if (Number.isNaN(userNumber)) return false
              if (typeof correctValue !== "number") return false
              if (!(numberInput.allowNegative ?? true) && userNumber < 0) return false

              if (numberInput.requiredPrecision !== undefined) {
                const decimalPart = numericStr.includes(".") ? numericStr.split(".")[1] || "" : ""
                if (decimalPart.length !== numberInput.requiredPrecision) return false
              }

              const tolerance = numberInput.tolerance ?? 0
              return Math.abs(userNumber - correctValue) <= tolerance
            }

            case FillBlankInputType.Dropdown:
              return rawAnswer === blank.input.options[0]

            case FillBlankInputType.WordBank: {
              const userWord = rawAnswer.includes("|") ? rawAnswer.split("|")[0] : rawAnswer
              return userWord === blank.input.words[0]
            }

            default:
              return false
          }
        })
        break
      }

      case QuizEntryType.ShortAnswer: {
        const userAnswer = (answerState.textAnswers.main || "").trim()
        const caseSensitive = practiceEntry.caseSensitive ?? false
        const acceptedAnswers = practiceEntry.acceptedAnswers
        correct = acceptedAnswers.length > 0 && acceptedAnswers.some((accepted) =>
          caseSensitive
            ? userAnswer === accepted
            : userAnswer.toLowerCase() === accepted.toLowerCase(),
        )
        break
      }

      case QuizEntryType.Essay: {
        const expectedPlain = (practiceEntry.correctAnswerPlain || "").trim()
        if (!expectedPlain) {
          correct = true
          break
        }

        const userPlain = (answerState.textAnswers.main_plain || "").trim()
        const textMatch = userPlain.toLowerCase() === expectedPlain.toLowerCase()

        if (!textMatch) {
          correct = false
          break
        }

        if (practiceEntry.requireFormatting && practiceEntry.correctAnswer) {
          const userSerialized = answerState.textAnswers.main || ""
          correct = compareFormattingStructure(practiceEntry.correctAnswer, userSerialized)
        } else {
          correct = true
        }
        break
      }

      case QuizEntryType.Matching: {
        const userAssignments = new Map<string, string>()
        answerState.selectedOptionIds.forEach((sel) => {
          const idx = sel.indexOf(":")
          if (idx > 0) {
            userAssignments.set(sel.substring(0, idx), sel.substring(idx + 1))
          }
        })

        if (userAssignments.size !== practiceEntry.pairs.length) {
          correct = false
        } else {
          correct = practiceEntry.pairs.every((pair) => userAssignments.get(pair.id) === pair.right)
        }
        break
      }

      case QuizEntryType.Ordering: {
        const userOrder = answerState.ordering
        const correctOrder = [...practiceEntry.items]
          .sort((a, b) => a.correctPosition - b.correctPosition)
          .map((item) => item.id)
        correct =
          userOrder.length === correctOrder.length &&
          userOrder.every((id, index) => id === correctOrder[index])
        break
      }

      case QuizEntryType.Categorization: {
        correct = practiceEntry.items.every((item) => {
          const correctCategoryIds = item.correctCategoryIds
          if (correctCategoryIds.length === 0) return false
          const assignedCategories = new Set(answerState.categorizations[item.id] || [])
          const correctCategories = new Set(correctCategoryIds)
          return (
            assignedCategories.size === correctCategories.size &&
            [...assignedCategories].every((id) => correctCategories.has(id))
          )
        })
        break
      }

      case QuizEntryType.Rating: {
        correct = practiceEntry.correctRating !== undefined
          ? answerState.rating === practiceEntry.correctRating
          : answerState.rating !== undefined
        break
      }

      case QuizEntryType.Numeric: {
        const userStr = (answerState.textAnswers.main || "").trim()
        if (!userStr) {
          correct = false
          break
        }

        const storedValsNumeric = answerState.textAnswers.formula_values
        if (!storedValsNumeric) {
          correct = false
          break
        }

        try {
          const values = JSON.parse(storedValsNumeric) as Record<string, number>
          const expected = evaluateFormula(practiceEntry.formula, values)
          const userNum = Number.parseFloat(userStr)

          if (Number.isNaN(userNum)) {
            correct = false
            break
          }

          const diff = Math.abs(userNum - expected)
          if (practiceEntry.toleranceType === "percentage") {
            const threshold = Math.abs(expected) * (practiceEntry.tolerance / 100)
            correct = diff <= threshold
          } else {
            correct = diff <= practiceEntry.tolerance
          }
        } catch {
          correct = false
        }
        break
      }

      case QuizEntryType.Formula: {
        const userStr = (answerState.textAnswers.main || "").trim()
        if (!userStr) {
          correct = false
          break
        }

        const storedVals = answerState.textAnswers.formula_values
        if (!storedVals) {
          correct = false
          break
        }

        try {
          const values = JSON.parse(storedVals) as Record<string, number>
          const varNames = practiceEntry.variables.map((v) => v.name).filter(Boolean)
          const validationError = validateFormula(userStr, varNames)

          if (validationError) {
            correct = false
            break
          }

          const numTests = 5
          const testResults: Array<{
            values: Record<string, number>
            userResult: number
            expected: number
            passed: boolean
          }> = []

          const userResult0 = evaluateFormula(userStr, values)
          const expected0 = evaluateFormula(practiceEntry.formula, values)
          const diff0 = Math.abs(userResult0 - expected0)
          const threshold0 = practiceEntry.toleranceType === "percentage"
            ? Math.abs(expected0) * (practiceEntry.tolerance / 100)
            : practiceEntry.tolerance

          testResults.push({
            values: { ...values },
            userResult: userResult0,
            expected: expected0,
            passed: diff0 <= threshold0,
          })

          for (let i = 1; i < numTests; i++) {
            const testVals: Record<string, number> = {}
            for (const variable of practiceEntry.variables) {
              if (variable.name) {
                testVals[variable.name] = generateVariableValue(variable.min, variable.max, variable.decimals)
              }
            }

            const userResult = evaluateFormula(userStr, testVals)
            const expected = evaluateFormula(practiceEntry.formula, testVals)
            const diff = Math.abs(userResult - expected)
            const threshold = practiceEntry.toleranceType === "percentage"
              ? Math.abs(expected) * (practiceEntry.tolerance / 100)
              : practiceEntry.tolerance

            testResults.push({
              values: testVals,
              userResult,
              expected,
              passed: diff <= threshold,
            })
          }

          setAnswerState((prev) => ({
            ...prev,
            textAnswers: {
              ...prev.textAnswers,
              formula_test_results: JSON.stringify(testResults),
            },
          }))

          correct = testResults.every((result) => result.passed)
        } catch {
          correct = false
        }
        break
      }

      case QuizEntryType.Hotspot: {
        const hx = Number.parseFloat(answerState.textAnswers.hotspot_x || "")
        const hy = Number.parseFloat(answerState.textAnswers.hotspot_y || "")

        if (Number.isNaN(hx) || Number.isNaN(hy)) {
          correct = false
          break
        }

        let withinAny = false
        for (const hotspot of practiceEntry.hotspots) {
          if (hotspot.zones.length === 0) continue

          const outermostRadius = Math.max(...hotspot.zones.map((zone) => zone.radius))
          const dx = (hx - hotspot.x) / 100 * practiceEntry.imageWidth
          const dy = (hy - hotspot.y) / 100 * practiceEntry.imageHeight
          const distance = Math.sqrt(dx * dx + dy * dy)
          const threshold = outermostRadius / 100 * practiceEntry.imageWidth

          if (distance <= threshold) {
            withinAny = true
            break
          }
        }

        correct = withinAny
        break
      }

      case QuizEntryType.Highlight: {
        try {
          const studentSpans = JSON.parse(answerState.textAnswers.highlight_spans || "[]") as Array<{
            start: number
            end: number
          }>

          const highlights = practiceEntry.highlights
          if (studentSpans.length === 0 && highlights.length > 0) {
            correct = false
            break
          }

          const allCorrectCovered = highlights.length > 0 && highlights.every((highlight) =>
            studentSpans.some((span) => span.start < highlight.end && span.end > highlight.start),
          )
          const noFalsePositives = studentSpans.every((span) =>
            highlights.some((highlight) => span.start < highlight.end && span.end > highlight.start),
          )

          correct = allCorrectCovered && noFalsePositives
        } catch {
          correct = false
        }
        break
      }
    }

    setIsCorrect(correct)
    setShowFeedback(true)
  }, [answerState, practiceEntry])

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

function getPracticeEntry(props: UseQuizAnswersProps): QuizPracticeEntry | null {
  if (props.submissionMode === "server-graded") return null
  return props.entry
}
