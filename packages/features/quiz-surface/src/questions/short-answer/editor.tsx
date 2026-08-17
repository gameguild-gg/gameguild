/**
 * Short Answer Editor
 * Configure accepted answers for short answer questions
 */

"use client"

import { useFormContext } from "react-hook-form"
import { Label } from "@game-guild/ui/components/label"
import { Input } from "@game-guild/ui/components/input"
import { Switch } from "@game-guild/ui/components/switch"
import type { ShortAnswerEntry } from "@game-guild/quiz"

export function ShortAnswerEditor() {
  const { register, watch, setValue } = useFormContext<ShortAnswerEntry>()
  const caseSensitive = watch("caseSensitive")

  return (
    <div className="space-y-4">
      <div className="space-y-2">
        <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
          Accepted Answers
        </Label>
        <div className="text-xs text-gray-500 dark:text-gray-400 mb-2 bg-blue-50 dark:bg-blue-950/30 p-2 rounded border border-blue-200 dark:border-blue-800">
          Enter acceptable answers separated by commas. Any of these will be marked as correct.
        </div>
        <Input
          placeholder="e.g., Paris, paris, PARIS"
          {...register("acceptedAnswers.0")}
          autoComplete="off"
          className="bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600"
        />
      </div>

      <div className="flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-800/50 rounded-lg border">
        <Label className="text-sm">Case Sensitive</Label>
        <Switch
          checked={caseSensitive}
          onCheckedChange={(checked) => setValue("caseSensitive", checked)}
        />
      </div>
    </div>
  )
}
