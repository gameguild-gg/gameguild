/**
 * Formula Renderer
 * Displays a formula question with generated variable values
 * and accepts a numeric answer from the student.
 */

"use client"

import { useMemo, useState, useEffect } from "react"
import { Calculator, Braces, PenLine, Search, CheckCircle, XCircle } from "lucide-react"
import { Input } from "@/components/ui/input"
import type { FormulaEntry, QuizAnswerState } from "../types"
import { generateVariableValue, evaluateFormula } from "../utils/formula-evaluator"

interface FormulaRendererProps {
  entry: FormulaEntry
  answerState: QuizAnswerState
  onAnswerChange: (updates: Partial<QuizAnswerState>) => void
  disabled?: boolean
  showFeedback?: boolean
}

export function FormulaRenderer({
  entry,
  answerState,
  onAnswerChange,
  disabled = false,
  showFeedback = false,
}: FormulaRendererProps) {
  // Track a seed that increments on retry (when stored values are cleared)
  const [seed, setSeed] = useState(0)
  const storedValues = answerState.textAnswers["formula_values"]

  // When stored values disappear (retry clears answerState), bump the seed
  useEffect(() => {
    if (!storedValues) {
      setSeed((s) => s + 1)
    }
  }, [storedValues])

  // Generate variable values — re-randomizes when seed changes
  const generatedValues = useMemo(() => {
    const values: Record<string, number> = {}
    for (const v of entry.variables) {
      if (v.name) {
        values[v.name] = generateVariableValue(v.min, v.max, v.decimals)
      }
    }
    return values
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [entry.variables, seed])

  // Store generated values in answer state so validation can use them
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

  // Use stored values if available, otherwise use generated
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
  const isDiscoverMode = entry.formulaMode === "discover"

  // In discover mode, parse the test results stored by validation
  const testResults = useMemo(() => {
    if (!isDiscoverMode || !showFeedback) return null
    const raw = answerState.textAnswers["formula_test_results"]
    if (!raw) return null
    try {
      return JSON.parse(raw) as Array<{
        values: Record<string, number>
        userResult: number
        expected: number
        passed: boolean
      }>
    } catch {
      return null
    }
  }, [isDiscoverMode, showFeedback, answerState.textAnswers])

  const passedCount = testResults?.filter((t) => t.passed).length ?? 0
  const totalTests = testResults?.length ?? 0

  const handleChange = (value: string) => {
    onAnswerChange({
      textAnswers: {
        ...answerState.textAnswers,
        main: value,
        formula_values: JSON.stringify(activeValues),
      },
    })
  }

  const showingTests = isDiscoverMode && showFeedback && !!testResults

  return (
    <div className="space-y-6">
      {/* Step 1: Given variable values (hidden when discover mode shows test results) */}
      {!showingTests && (
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
      )}

      {/* Step 2: Formula / Expected Result */}
      <div className="bg-amber-50 dark:bg-amber-950/20 rounded-xl border border-amber-200 dark:border-amber-800 p-5">
        <div className="flex items-center gap-2 mb-4">
          {!showingTests && (
          <div className="flex items-center justify-center w-7 h-7 rounded-full bg-amber-600 text-white text-sm font-bold">
            2
          </div>
          )}
          {isDiscoverMode ? (
            <Search className="h-5 w-5 text-amber-600 dark:text-amber-400" />
          ) : (
            <Calculator className="h-5 w-5 text-amber-600 dark:text-amber-400" />
          )}
          <h4 className="text-base font-semibold text-amber-700 dark:text-amber-300">
            {isDiscoverMode ? "Expected Result" : "Apply the Formula"}
          </h4>
        </div>
        <div className="bg-white dark:bg-gray-900 rounded-lg px-5 py-4 border border-amber-100 dark:border-amber-900 text-center shadow-sm">
          {isDiscoverMode ? (
            <span className="font-mono text-2xl font-bold text-amber-600 dark:text-amber-400">
              ? = {correctAnswer !== null ? parseFloat(correctAnswer.toFixed(entry.decimalPlaces)) : "?"}
            </span>
          ) : (
            <span className="font-mono text-2xl font-bold text-gray-900 dark:text-gray-100">
              {entry.formula}
              {showFeedback && correctAnswer !== null && (entry.settings?.showCorrectAnswer ?? true) && (
                <span className="text-amber-600 dark:text-amber-400">
                  {" "}= {parseFloat(correctAnswer.toFixed(entry.decimalPlaces))}
                </span>
              )}
            </span>
          )}
        </div>

        {/* Discover mode: show test results */}
        {isDiscoverMode && showFeedback && testResults && (
          <div className="mt-3 space-y-1">
            <div className="flex items-center justify-between">
              <span className="text-xs font-medium text-amber-700 dark:text-amber-300">
                Tests: {passedCount}/{totalTests} passed
              </span>
              {passedCount === totalTests ? (
                <span className="text-xs font-medium text-green-600 dark:text-green-400 flex items-center gap-1">
                  <CheckCircle className="h-3 w-3" /> All passed
                </span>
              ) : (
                <span className="text-xs font-medium text-red-500 dark:text-red-400 flex items-center gap-1">
                  <XCircle className="h-3 w-3" /> {totalTests - passedCount} failed
                </span>
              )}
            </div>
            <div className="space-y-0.5">
              {testResults.map((test, i) => (
                <div
                  key={i}
                  className={`flex items-center gap-1.5 px-2 py-0.5 rounded border text-xs font-mono ${
                    test.passed
                      ? "bg-green-50 dark:bg-green-950/20 border-green-200 dark:border-green-800"
                      : "bg-red-50 dark:bg-red-950/20 border-red-200 dark:border-red-800"
                  }`}
                >
                  {test.passed ? (
                    <CheckCircle className="h-3 w-3 text-green-500 shrink-0" />
                  ) : (
                    <XCircle className="h-3 w-3 text-red-500 shrink-0" />
                  )}
                  <span className="text-gray-500 dark:text-gray-400 shrink-0">#{i + 1}</span>
                  <span className="text-gray-600 dark:text-gray-300 truncate">
                    {Object.entries(test.values).map(([k, v]) => `${k}=${v}`).join(", ")}
                  </span>
                  <span className="text-gray-400 ml-auto shrink-0">→</span>
                  <span className={test.passed ? "text-green-600 dark:text-green-400" : "text-red-500 dark:text-red-400"}>
                    {parseFloat(test.userResult.toFixed(entry.decimalPlaces))}
                  </span>
                  <span className="text-gray-400">
                    {test.passed ? "=" : "≠"}
                  </span>
                  <span className="text-amber-600 dark:text-amber-400">
                    {parseFloat(test.expected.toFixed(entry.decimalPlaces))}
                  </span>
                </div>
              ))}
            </div>
          </div>
        )}
        <p className="text-sm text-amber-600 dark:text-amber-400 mt-3 text-center">
          {isDiscoverMode
            ? "Find a formula using the variables above that produces this result."
            : "Substitute the variable values above into this formula and compute the result."}
        </p>
      </div>

      {/* Step 3: Answer input */}
      <div className="bg-green-50 dark:bg-green-950/20 rounded-xl border border-green-200 dark:border-green-800 p-5">
        <div className="flex items-center gap-2 mb-4">
          {!showingTests && (
          <div className="flex items-center justify-center w-7 h-7 rounded-full bg-green-600 text-white text-sm font-bold">
            3
          </div>
          )}
          <PenLine className="h-5 w-5 text-green-600 dark:text-green-400" />
          <h4 className="text-base font-semibold text-green-700 dark:text-green-300">
            {isDiscoverMode ? "Write Your Formula" : "Enter Your Answer"}
          </h4>
        </div>
        {isDiscoverMode ? (
          <Input
            type="text"
            value={userAnswer}
            onChange={(e) => handleChange(e.target.value)}
            disabled={disabled || showFeedback}
            autoComplete="off"
            placeholder="e.g., x^2 + y"
            className="bg-white dark:bg-gray-900 border-green-300 dark:border-green-700 font-mono text-lg h-12"
          />
        ) : (
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
        )}
        {!isDiscoverMode && entry.tolerance > 0 && (
          <p className="text-xs text-green-600 dark:text-green-400 mt-2">
            Tolerance: ± {entry.tolerance}{entry.toleranceType === "percentage" ? "%" : ""}
            {entry.decimalPlaces > 0 && ` · Round to ${entry.decimalPlaces} decimal place${entry.decimalPlaces > 1 ? "s" : ""}`}
          </p>
        )}
        {isDiscoverMode && (
          <p className="text-xs text-green-600 dark:text-green-400 mt-2">
            Use variables: {entry.variables.map((v) => v.name).join(", ")} · Operators: +, -, *, /, ^ · Functions: sqrt, abs, sin, cos, etc.
          </p>
        )}
      </div>
    </div>
  )
}
