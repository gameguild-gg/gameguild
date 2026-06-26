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
  type FillBlankNumberInput,
  FillBlankInputType,
} from "../types"
import { evaluateFormula, validateFormula, generateVariableValue } from "../utils/formula-evaluator"

interface UseQuizAnswersProps {
  entry: QuizEntry
}

/**
 * Extracts a normalized formatting structure from a Lexical editor state.
 * Accepts either the serialized JSON string or a parsed `SerializedEditorState`
 * object. Returns an array of `{ type, format, children }` per node, stripping
 * text content so only structure + formatting flags are compared.
 */
function extractFormattingStructure(input: string | object): unknown[] | null {
  try {
    const state = typeof input === "string" ? JSON.parse(input) : input
    const root = (state as { root?: { children?: unknown[] } })?.root
    if (!root?.children) return null

    function normalizeNode(node: Record<string, unknown>): Record<string, unknown> {
      const result: Record<string, unknown> = { type: node.type }

      // Keep formatting flags (bold, italic, etc.) encoded in the format bitmask
      if (node.format !== undefined) result.format = node.format
      // Keep node-specific structural info
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
            case FillBlankInputType.Number: {
              const numberInput = blank.input as FillBlankNumberInput
              // Strip unit suffix if present
              let numericStr = rawAnswer
              if (numberInput.unit) {
                numericStr = numericStr.replace(new RegExp(`\\s*${numberInput.unit.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}\\s*$`), '').trim()
                // If unit is required, ensure it was present
                if (numberInput.requireUnit && numericStr === rawAnswer) return false
              }
              const userNumber = parseFloat(numericStr)
              if (isNaN(userNumber)) return false
              // Check negative constraint
              if (!(numberInput.allowNegative ?? true) && userNumber < 0) return false
              // Check required precision (decimal places)
              if (numberInput.requiredPrecision !== undefined) {
                const decimalPart = numericStr.includes('.') ? numericStr.split('.')[1] || '' : ''
                if (decimalPart.length !== numberInput.requiredPrecision) return false
              }
              // Check tolerance
              const tolerance = numberInput.tolerance ?? 0
              return Math.abs(userNumber - numberInput.correctValue) <= tolerance
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
        const expectedPlain = (entry.correctAnswerPlain || "").trim()
        if (!expectedPlain) {
          // No correct answer configured — treat as manually graded
          correct = true
          break
        }

        const userPlain = (answerState.textAnswers["main_plain"] || "").trim()
        const textMatch = userPlain.toLowerCase() === expectedPlain.toLowerCase()

        if (!textMatch) {
          correct = false
          break
        }

        if (entry.requireFormatting && entry.correctAnswer) {
          const userSerialized = answerState.textAnswers["main"] || ""
          correct = compareFormattingStructure(entry.correctAnswer, userSerialized)
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

        if (userAssignments.size !== entry.pairs.length) {
          correct = false
        } else {
          correct = entry.pairs.every(
            (pair) => userAssignments.get(pair.id) === pair.right
          )
        }
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

      case QuizEntryType.Numeric: {
        const userStr = (answerState.textAnswers["main"] || "").trim()
        if (!userStr) {
          correct = false
          break
        }
        const storedValsNumeric = answerState.textAnswers["formula_values"]
        if (!storedValsNumeric) {
          correct = false
          break
        }
        try {
          const values = JSON.parse(storedValsNumeric) as Record<string, number>
          const expected = evaluateFormula(entry.formula, values)
          const userNum = parseFloat(userStr)
          if (isNaN(userNum)) {
            correct = false
            break
          }
          const diff = Math.abs(userNum - expected)
          if (entry.toleranceType === "percentage") {
            const threshold = Math.abs(expected) * (entry.tolerance / 100)
            correct = diff <= threshold
          } else {
            correct = diff <= entry.tolerance
          }
        } catch {
          correct = false
        }
        break
      }

      case QuizEntryType.Formula: {
        const userStr = (answerState.textAnswers["main"] || "").trim()
        if (!userStr) {
          correct = false
          break
        }
        const storedVals = answerState.textAnswers["formula_values"]
        if (!storedVals) {
          correct = false
          break
        }
        try {
          const values = JSON.parse(storedVals) as Record<string, number>

          // Formula mode: user submits a formula expression
          // Run 5 unit tests with random variable values
          const varNames = entry.variables.map((v) => v.name).filter(Boolean)
          const validationError = validateFormula(userStr, varNames)
          if (validationError) {
            correct = false
            break
          }

          const NUM_TESTS = 5
          const testResults: Array<{
            values: Record<string, number>
            userResult: number
            expected: number
            passed: boolean
          }> = []

          // First test uses the displayed values
          const userResult0 = evaluateFormula(userStr, values)
          const expected0 = evaluateFormula(entry.formula, values)
          const diff0 = Math.abs(userResult0 - expected0)
          const threshold0 = entry.toleranceType === "percentage"
            ? Math.abs(expected0) * (entry.tolerance / 100)
            : entry.tolerance
          testResults.push({
            values: { ...values },
            userResult: userResult0,
            expected: expected0,
            passed: diff0 <= threshold0,
          })

          // Run 4 more tests with random values
          for (let i = 1; i < NUM_TESTS; i++) {
            const testVals: Record<string, number> = {}
            for (const v of entry.variables) {
              if (v.name) {
                testVals[v.name] = generateVariableValue(v.min, v.max, v.decimals)
              }
            }
            const userRes = evaluateFormula(userStr, testVals)
            const expectedRes = evaluateFormula(entry.formula, testVals)
            const diff = Math.abs(userRes - expectedRes)
            const threshold = entry.toleranceType === "percentage"
              ? Math.abs(expectedRes) * (entry.tolerance / 100)
              : entry.tolerance
            testResults.push({
              values: testVals,
              userResult: userRes,
              expected: expectedRes,
              passed: diff <= threshold,
            })
          }

          // Store test results for the renderer to display
          setAnswerState((prev) => ({
            ...prev,
            textAnswers: {
              ...prev.textAnswers,
              formula_test_results: JSON.stringify(testResults),
            },
          }))

          correct = testResults.every((t) => t.passed)
        } catch {
          correct = false
        }
        break
      }

      case QuizEntryType.Hotspot: {
        const hx = parseFloat(answerState.textAnswers["hotspot_x"] || "")
        const hy = parseFloat(answerState.textAnswers["hotspot_y"] || "")
        if (isNaN(hx) || isNaN(hy)) {
          correct = false
          break
        }
        let withinAny = false
        for (const hp of entry.hotspots) {
          if (hp.zones.length === 0) continue
          const outermostRadius = Math.max(...hp.zones.map(z => z.radius))
          const dx = (hx - hp.x) / 100 * entry.imageWidth
          const dy = (hy - hp.y) / 100 * entry.imageHeight
          const distance = Math.sqrt(dx * dx + dy * dy)
          const threshold = outermostRadius / 100 * entry.imageWidth
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
          const studentSpans = JSON.parse(answerState.textAnswers["highlight_spans"] || "[]") as { start: number; end: number }[]
          if (studentSpans.length === 0 && entry.highlights.length > 0) {
            correct = false
            break
          }
          // Every correct span must be covered by at least one student span, and vice-versa
          const allCorrectCovered = entry.highlights.every((h) =>
            studentSpans.some((s) => s.start < h.end && s.end > h.start)
          )
          const noFalsePositives = studentSpans.every((s) =>
            entry.highlights.some((h) => s.start < h.end && s.end > h.start)
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
