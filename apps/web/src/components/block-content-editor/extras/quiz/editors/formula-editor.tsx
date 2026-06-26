/**
 * Formula Editor
 * Configure formula questions with variables, expression, and tolerance
 */

"use client"

import { useFieldArray, useFormContext } from "react-hook-form"
import { Plus, X, AlertCircle, Check } from "lucide-react"
import { useState, useMemo } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import type { FormulaEntry } from "../types"
import { validateFormula, evaluateFormula, generateVariableValue } from "../utils/formula-evaluator"
import { MathInput } from "@/components/block-content-editor/extras/math/math-input"

export function FormulaEditor() {
  const { register, control, watch, setValue } = useFormContext<FormulaEntry>()
  const { fields, append, remove } = useFieldArray({
    control,
    name: "variables",
  })

  const formula = watch("formula") || ""
  const variables = watch("variables") || []
  const toleranceType = watch("toleranceType") || "absolute"
  const decimalPlaces = watch("decimalPlaces") ?? 2

  const [testResult, setTestResult] = useState<{
    values: Record<string, number>
    result: number
  } | null>(null)

  const variableNames = useMemo(() => variables.map((v) => v.name).filter(Boolean), [variables])

  const formulaError = useMemo(() => {
    if (!formula.trim()) return null
    return validateFormula(formula, variableNames)
  }, [formula, variableNames])

  const addVariable = () => {
    const usedNames = new Set(variables.map((v) => v.name))
    const letters = "xyzabcnmrst".split("")
    const nextName = letters.find((l) => !usedNames.has(l)) || `v${fields.length + 1}`
    append({
      id: Math.random().toString(36).substring(7),
      name: nextName,
      min: 1,
      max: 10,
      decimals: 0,
    })
  }

  const handleTestFormula = () => {
    if (formulaError || !formula.trim()) return
    const values: Record<string, number> = {}
    for (const v of variables) {
      if (v.name) {
        values[v.name] = generateVariableValue(v.min, v.max, v.decimals)
      }
    }
    try {
      const result = evaluateFormula(formula, values)
      setTestResult({ values, result: parseFloat(result.toFixed(decimalPlaces)) })
    } catch {
      setTestResult(null)
    }
  }

  return (
    <div className="space-y-5">
      {/* Variables Section */}
      <div className="space-y-3">
        <div className="flex items-center justify-between">
          <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
            Variables
          </Label>
          <Button type="button" variant="outline" size="sm" onClick={addVariable}>
            <Plus className="h-4 w-4 mr-1" />
            Add Variable
          </Button>
        </div>

        <div className="text-xs text-gray-500 dark:text-gray-400 bg-blue-50 dark:bg-blue-950/30 p-2 rounded border border-blue-200 dark:border-blue-800">
          Define variables with their range. Random values will be generated within these ranges for each quiz attempt.
        </div>

        {fields.length === 0 && (
          <div className="text-center py-4 text-gray-500 dark:text-gray-400 bg-gray-50 dark:bg-gray-800/50 rounded-lg border">
            <p className="text-sm">No variables yet. Add variables to use in the formula.</p>
          </div>
        )}

        <div className="space-y-2">
          {fields.map((field, index) => (
            <div
              key={field.id}
              className="flex items-center gap-2 px-3 py-1.5 bg-gray-50 dark:bg-gray-800/50 rounded-lg border"
            >
              <span className="text-xs text-gray-500 dark:text-gray-400 shrink-0">Name</span>
              <Input
                {...register(`variables.${index}.name`, { required: true })}
                autoComplete="off"
                placeholder="x"
                className="bg-white dark:bg-gray-800 font-mono text-center h-8 w-16"
              />
              <span className="text-xs text-gray-500 dark:text-gray-400 shrink-0">Min</span>
              <Input
                type="number"
                step="any"
                {...register(`variables.${index}.min`, { valueAsNumber: true })}
                autoComplete="off"
                className="bg-white dark:bg-gray-800 h-8 w-20"
              />
              <span className="text-xs text-gray-500 dark:text-gray-400 shrink-0">Max</span>
              <Input
                type="number"
                step="any"
                {...register(`variables.${index}.max`, { valueAsNumber: true })}
                autoComplete="off"
                className="bg-white dark:bg-gray-800 h-8 w-20"
              />
              <span className="text-xs text-gray-500 dark:text-gray-400 shrink-0">Dec</span>
              <Input
                type="number"
                min="0"
                max="6"
                step="1"
                {...register(`variables.${index}.decimals`, { valueAsNumber: true })}
                autoComplete="off"
                className="bg-white dark:bg-gray-800 h-8 w-16"
              />
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => remove(index)}
                className="hover:bg-red-50 dark:hover:bg-red-950/30 hover:text-red-600 h-8 w-8 p-0 shrink-0"
              >
                <X className="h-4 w-4" />
              </Button>
            </div>
          ))}
        </div>
      </div>

      {/* Formula Section */}
      <div className="space-y-2">
        <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
          Correct Formula (hidden from student)
        </Label>
        <div className="text-xs text-gray-500 dark:text-gray-400 bg-blue-50 dark:bg-blue-950/30 p-2 rounded border border-blue-200 dark:border-blue-800">
          Use o editor visual de fórmula (LaTeX) para escrever a expressão correta. Suporta frações, raízes,
          potências, funções (sqrt, sin, cos, tan, log, ln, exp, ceil, floor, round, min, max, pow), parênteses
          e constantes (pi, e). Use os nomes das variáveis definidas acima.
        </div>
        <div className="relative">
          <MathInput
            value={formula}
            onChange={(latex) => setValue("formula", latex, { shouldDirty: true })}
            placeholder="e.g.,\\ x^2 + 2y"
            className={
              formulaError
                ? "border-red-400 dark:border-red-600"
                : formula.trim() && !formulaError
                  ? "border-green-400 dark:border-green-600"
                  : ""
            }
          />
          {formula.trim() && (
            <span className="absolute right-3 top-1/2 -translate-y-1/2 z-10">
              {formulaError ? (
                <AlertCircle className="h-4 w-4 text-red-500" />
              ) : (
                <Check className="h-4 w-4 text-green-500" />
              )}
            </span>
          )}
        </div>
        {formulaError && (
          <p className="text-xs text-red-500 dark:text-red-400">{formulaError}</p>
        )}
      </div>

      {/* Test Formula */}
      {!formulaError && formula.trim() && variableNames.length > 0 && (
        <div className="space-y-2">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={handleTestFormula}
            className="w-full"
          >
            Test Formula with Random Values
          </Button>
          {testResult && (
            <div className="bg-green-50 dark:bg-green-950/30 border border-green-200 dark:border-green-800 rounded-lg p-3 text-sm">
              <div className="font-medium text-green-700 dark:text-green-400 mb-1">Test Result</div>
              <div className="space-y-1 text-green-600 dark:text-green-300 font-mono text-xs">
                {Object.entries(testResult.values).map(([name, val]) => (
                  <div key={name}>
                    {name} = {val}
                  </div>
                ))}
                <div className="border-t border-green-200 dark:border-green-700 pt-1 mt-1 font-semibold">
                  Result = {testResult.result}
                </div>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Tolerance Settings */}
      <div className="space-y-3">
        <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
          Answer Settings
        </Label>

        <div className="grid grid-cols-3 gap-3">
          <div className="space-y-2">
            <Label className="text-xs text-gray-500 dark:text-gray-400">Margin Type</Label>
            <Select
              value={toleranceType}
              onValueChange={(val) => setValue("toleranceType", val as "absolute" | "percentage")}
            >
              <SelectTrigger className="bg-white dark:bg-gray-800">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="absolute">Absolute</SelectItem>
                <SelectItem value="percentage">Percentage</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-2">
            <Label className="text-xs text-gray-500 dark:text-gray-400">
              ± Margin of Error
            </Label>
            <Input
              type="number"
              step="any"
              min="0"
              {...register("tolerance", { valueAsNumber: true })}
              autoComplete="off"
              placeholder="0"
              className="bg-white dark:bg-gray-800"
            />
          </div>
          <div className="space-y-2">
            <Label className="text-xs text-gray-500 dark:text-gray-400">
              Decimal Places
            </Label>
            <Input
              type="number"
              step="1"
              min="0"
              max="10"
              {...register("decimalPlaces", { valueAsNumber: true })}
              autoComplete="off"
              placeholder="2"
              className="bg-white dark:bg-gray-800"
            />
          </div>
        </div>

        {toleranceType === "percentage" && (
          <p className="text-xs text-gray-500 dark:text-gray-400">
            Student answer must be within ±{watch("tolerance") || 0}% of the correct value.
          </p>
        )}
        {toleranceType === "absolute" && (watch("tolerance") ?? 0) > 0 && (
          <p className="text-xs text-gray-500 dark:text-gray-400">
            Student answer must be within ±{watch("tolerance")} of the correct value.
          </p>
        )}
      </div>
    </div>
  )
}
