/**
 * Formula Renderer
 * Displays a formula discovery quiz: student sees variable values and expected result,
 * then writes a formula expression that matches.
 */

"use client"

import { useMemo, useState, useEffect, useCallback } from "react"
import { Search, PenLine, CheckCircle, XCircle, FlaskConical } from "lucide-react"
import { Button } from "@/components/ui/button"
import type { FormulaEntry, QuizAnswerState } from "../types"
import { generateVariableValue, evaluateFormula, validateFormula } from "../utils/formula-evaluator"
import { MathInput } from "@/components/block-content-editor/extras/math/math-input"

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
  const [seed, setSeed] = useState(0)
  const storedValues = answerState.textAnswers["formula_values"]

  useEffect(() => {
    if (!storedValues) {
      setSeed((s) => s + 1)
    }
  }, [storedValues])

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

  type TestResult = {
    values: Record<string, number>
    userResult: number
    expected: number
    passed: boolean
  }

  const [localTestResults, setLocalTestResults] = useState<TestResult[] | null>(null)

  const submittedTestResults = useMemo(() => {
    if (!showFeedback) return null
    const raw = answerState.textAnswers["formula_test_results"]
    if (!raw) return null
    try {
      return JSON.parse(raw) as TestResult[]
    } catch {
      return null
    }
  }, [showFeedback, answerState.textAnswers])

  const testResults = submittedTestResults ?? localTestResults

  const passedCount = testResults?.filter((t) => t.passed).length ?? 0
  const totalTests = testResults?.length ?? 0

  const runTests = useCallback(() => {
    if (!userAnswer.trim()) return
    const varNames = entry.variables.map((v) => v.name).filter(Boolean)
    const validationError = validateFormula(userAnswer, varNames)
    if (validationError) {
      setLocalTestResults([])
      return
    }
    const NUM_TESTS = 5
    const results: TestResult[] = []
    try {
      const userRes0 = evaluateFormula(userAnswer, activeValues)
      const expected0 = evaluateFormula(entry.formula, activeValues)
      const diff0 = Math.abs(userRes0 - expected0)
      const threshold0 = entry.toleranceType === "percentage"
        ? Math.abs(expected0) * (entry.tolerance / 100)
        : entry.tolerance
      results.push({ values: { ...activeValues }, userResult: userRes0, expected: expected0, passed: diff0 <= threshold0 })
    } catch {
      setLocalTestResults([])
      return
    }
    for (let i = 1; i < NUM_TESTS; i++) {
      const testVals: Record<string, number> = {}
      for (const v of entry.variables) {
        if (v.name) testVals[v.name] = generateVariableValue(v.min, v.max, v.decimals)
      }
      try {
        const userRes = evaluateFormula(userAnswer, testVals)
        const expectedRes = evaluateFormula(entry.formula, testVals)
        const diff = Math.abs(userRes - expectedRes)
        const threshold = entry.toleranceType === "percentage"
          ? Math.abs(expectedRes) * (entry.tolerance / 100)
          : entry.tolerance
        results.push({ values: testVals, userResult: userRes, expected: expectedRes, passed: diff <= threshold })
      } catch {
        results.push({ values: testVals, userResult: NaN, expected: 0, passed: false })
      }
    }
    setLocalTestResults(results)
  }, [userAnswer, entry, activeValues])

  useEffect(() => {
    setLocalTestResults(null)
  }, [userAnswer])

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
      {/* Expected Result */}
      <div className="bg-amber-50 dark:bg-amber-950/20 rounded-xl border border-amber-200 dark:border-amber-800 p-5">
        <div className="flex items-center gap-2 mb-4">
          <Search className="h-5 w-5 text-amber-600 dark:text-amber-400" />
          <h4 className="text-base font-semibold text-amber-700 dark:text-amber-300">
            Expected Result
          </h4>
        </div>
        <div className="bg-white dark:bg-gray-900 rounded-lg px-5 py-4 border border-amber-100 dark:border-amber-900 text-center shadow-sm">
          <div>
            <div className="flex flex-wrap justify-center gap-2 mb-3">
              {entry.variables.map((v) => (
                <span key={v.id} className="font-mono text-sm bg-blue-50 dark:bg-blue-950/30 text-blue-700 dark:text-blue-300 px-2 py-0.5 rounded border border-blue-200 dark:border-blue-800">
                  {v.name}={activeValues[v.name] ?? "?"}
                </span>
              ))}
            </div>
            <span className="font-mono text-2xl font-bold text-amber-600 dark:text-amber-400">
              ? = {correctAnswer !== null ? parseFloat(correctAnswer.toFixed(entry.decimalPlaces)) : "?"}
            </span>
          </div>
        </div>

        {/* Always reserve space for 5 test rows */}
        <div className="mt-3 space-y-1">
          <div className="flex items-center justify-between h-4">
            {testResults && testResults.length > 0 ? (
              <>
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
              </>
            ) : (
              <span className="text-xs text-amber-400 dark:text-amber-600 flex items-center gap-1">
                <FlaskConical className="h-3 w-3" /> Test your formula
              </span>
            )}
          </div>
          <div className="space-y-0.5">
            {Array.from({ length: 5 }).map((_, i) => {
              const test = testResults?.[i]
              if (test) {
                return (
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
                )
              }
              return (
                <div
                  key={i}
                  className="flex items-center gap-1.5 px-2 py-0.5 rounded border border-dashed border-amber-200/50 dark:border-amber-800/50 text-xs font-mono h-[22px]"
                >
                  <span className="text-amber-300 dark:text-amber-700 shrink-0">#{i + 1}</span>
                </div>
              )
            })}
          </div>
        </div>
        <p className="text-sm text-amber-600 dark:text-amber-400 mt-3 text-center">
          Find a formula using these variables that produces this result.
        </p>
      </div>

      {/* Write Your Formula */}
      <div className="bg-green-50 dark:bg-green-950/20 rounded-xl border border-green-200 dark:border-green-800 p-5">
        <div className="flex items-center gap-2 mb-4">
          <PenLine className="h-5 w-5 text-green-600 dark:text-green-400" />
          <h4 className="text-base font-semibold text-green-700 dark:text-green-300">
            Write Your Formula
          </h4>
        </div>
        <div className="flex gap-2">
          <div className="flex-1">
            <MathInput
              value={userAnswer}
              onChange={(latex) => handleChange(latex)}
              readOnly={disabled || showFeedback}
              placeholder="e.g.,\\ x^2 + y"
              className="border-green-300 dark:border-green-700 text-lg min-h-[3rem]"
            />
          </div>
          <Button
            type="button"
            variant="outline"
            size="default"
            onClick={runTests}
            disabled={disabled || showFeedback || !userAnswer.trim()}
            className="h-12 px-4 border-green-300 dark:border-green-700 text-green-700 dark:text-green-300 hover:bg-green-100 dark:hover:bg-green-900/30"
          >
            <FlaskConical className="h-4 w-4 mr-1.5" />
            Test
          </Button>
        </div>
        <p className="text-xs text-green-600 dark:text-green-400 mt-2">
          Use variables: {entry.variables.map((v) => v.name).join(", ")} · Operators: +, -, *, /, ^ · Functions: sqrt, abs, sin, cos, etc.
        </p>
      </div>
    </div>
  )
}
