/**
 * Numeric Renderer
 * Displays a numeric quiz: generated variable values, the formula to evaluate,
 * and a numeric input for the student's computed result.
 */

"use client"

import { useMemo, useEffect } from "react"
import { Calculator, Braces, PenLine } from "lucide-react"
import { Input } from "@/components/ui/input"
import type { NumericEntry, QuizAnswerState } from "../types"
import type { NumericLearnerEntry } from "../contracts"
import { generateVariableValue, evaluateFormula } from "../utils/formula-evaluator"

interface NumericRendererProps {
  entry: NumericEntry | NumericLearnerEntry
  answerState: QuizAnswerState
  onAnswerChange: (updates: Partial<QuizAnswerState>) => void
  disabled?: boolean
  showFeedback?: boolean
}

export function NumericRenderer({
  entry,
  answerState,
  onAnswerChange,
  disabled = false,
  showFeedback = false,
}: NumericRendererProps) {
  const storedValues = answerState.textAnswers["formula_values"]

  const generatedValues = useMemo(() => {
    const values: Record<string, number> = {}
    for (const v of entry.variables) {
      if (v.name) {
        values[v.name] = generateVariableValue(v.min, v.max, v.decimals)
      }
    }
    return values
  }, [entry.variables])

  useEffect(() => {
    if (!storedValues) {
      onAnswerChange({
        textAnswers: {
          ...answerState.textAnswers,
          formula_values: JSON.stringify(generatedValues),
        },
      })
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [generatedValues])

  const activeValues: Record<string, number> = useMemo(() => {
    if (storedValues) {
      try {
        return JSON.parse(storedValues)
      } catch {
        return generatedValues
      }
    }
    return generatedValues
  }, [storedValues, generatedValues])

  const correctAnswer = useMemo(() => {
    try {
      return evaluateFormula(entry.formula, activeValues)
    } catch {
      return null
    }
  }, [entry.formula, activeValues])

  const userAnswer = answerState.textAnswers["main"] || ""
  const tolerance = "tolerance" in entry && Number.isFinite(entry.tolerance) ? entry.tolerance : 0
  const toleranceType = "toleranceType" in entry ? entry.toleranceType : "absolute"

  const handleChange = (value: string) => {
    onAnswerChange({
      textAnswers: {
        ...answerState.textAnswers,
        main: value,
        formula_values: JSON.stringify(activeValues),
      },
    })
  }

  return (
    <div className="space-y-6">
      {/* Step 1: Given variable values */}
      <div className="bg-blue-50 dark:bg-blue-950/30 rounded-xl border border-blue-200 dark:border-blue-800 p-5">
        <div className="flex items-center gap-2 mb-4">
          <div className="flex items-center justify-center w-7 h-7 rounded-full bg-blue-600 text-white text-sm font-bold">
            1
          </div>
          <Braces className="h-5 w-5 text-blue-600 dark:text-blue-400" />
          <h4 className="text-base font-semibold text-blue-700 dark:text-blue-300">
            Given Variable Values
          </h4>
        </div>
        <div className="flex flex-wrap gap-3">
          {entry.variables.map((v) => (
            <div
              key={v.id}
              className="flex items-center gap-3 bg-white dark:bg-gray-900 rounded-lg px-4 py-3 border border-blue-100 dark:border-blue-900 shadow-sm"
            >
              <span className="font-mono text-lg font-bold text-blue-600 dark:text-blue-400">
                {v.name}
              </span>
              <span className="text-gray-400 text-lg">=</span>
              <span className="font-mono text-lg font-bold text-gray-900 dark:text-gray-100">
                {activeValues[v.name] ?? "?"}
              </span>
            </div>
          ))}
        </div>
      </div>

      {/* Step 2: Formula */}
      <div className="bg-amber-50 dark:bg-amber-950/20 rounded-xl border border-amber-200 dark:border-amber-800 p-5">
        <div className="flex items-center gap-2 mb-4">
          <div className="flex items-center justify-center w-7 h-7 rounded-full bg-amber-600 text-white text-sm font-bold">
            2
          </div>
          <Calculator className="h-5 w-5 text-amber-600 dark:text-amber-400" />
          <h4 className="text-base font-semibold text-amber-700 dark:text-amber-300">
            Apply the Formula
          </h4>
        </div>
        <div className="bg-white dark:bg-gray-900 rounded-lg px-5 py-4 border border-amber-100 dark:border-amber-900 text-center shadow-sm">
          <span className="font-mono text-2xl font-bold text-gray-900 dark:text-gray-100">
            {entry.formula}
            {showFeedback && correctAnswer !== null && (entry.settings?.showCorrectAnswer ?? true) && (
              <span className="text-amber-600 dark:text-amber-400">
                {" "}= {parseFloat(correctAnswer.toFixed(entry.decimalPlaces))}
              </span>
            )}
          </span>
        </div>
        <p className="text-sm text-amber-600 dark:text-amber-400 mt-3 text-center">
          Substitute the variable values above into this formula and compute the result.
        </p>
      </div>

      {/* Step 3: Answer input */}
      <div className="bg-green-50 dark:bg-green-950/20 rounded-xl border border-green-200 dark:border-green-800 p-5">
        <div className="flex items-center gap-2 mb-4">
          <div className="flex items-center justify-center w-7 h-7 rounded-full bg-green-600 text-white text-sm font-bold">
            3
          </div>
          <PenLine className="h-5 w-5 text-green-600 dark:text-green-400" />
          <h4 className="text-base font-semibold text-green-700 dark:text-green-300">
            Enter Your Answer
          </h4>
        </div>
        <Input
          type="number"
          step="any"
          value={userAnswer}
          onChange={(e) => handleChange(e.target.value)}
          disabled={disabled || showFeedback}
          autoComplete="off"
          placeholder="Type your computed result here..."
          className="bg-white dark:bg-gray-900 border-green-300 dark:border-green-700 font-mono text-lg h-12"
        />
        {tolerance > 0 && (
          <p className="text-xs text-green-600 dark:text-green-400 mt-2">
            Tolerance: +/- {tolerance}{toleranceType === "percentage" ? "%" : ""}
            {entry.decimalPlaces > 0 && ` · Round to ${entry.decimalPlaces} decimal place${entry.decimalPlaces > 1 ? "s" : ""}`}
          </p>
        )}
      </div>
    </div>
  )
}
