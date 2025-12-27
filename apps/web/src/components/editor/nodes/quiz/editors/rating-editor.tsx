/**
 * Rating Question Editor
 * Configure rating scale settings
 */

"use client"

import { useFormContext } from "react-hook-form"
import { Label } from "@/components/ui/label"
import { Input } from "@/components/ui/input"

export function RatingEditor() {
  const { register, watch } = useFormContext()
  const ratingScale = watch("ratingScale")

  return (
    <div className="space-y-4">
      <div className="text-sm text-gray-600 dark:text-gray-400 bg-blue-50 dark:bg-blue-950/30 p-3 rounded-lg border border-blue-200 dark:border-blue-800">
        Configure the rating scale for this question.
      </div>

      <div className="grid grid-cols-3 gap-4">
        <div className="space-y-2">
          <Label className="text-sm">Minimum</Label>
          <Input
            type="number"
            placeholder="Min value"
            {...register("ratingScale.min", { valueAsNumber: true })}
            className="bg-white dark:bg-gray-800"
          />
        </div>

        <div className="space-y-2">
          <Label className="text-sm">Maximum</Label>
          <Input
            type="number"
            placeholder="Max value"
            {...register("ratingScale.max", { valueAsNumber: true })}
            className="bg-white dark:bg-gray-800"
          />
        </div>

        <div className="space-y-2">
          <Label className="text-sm">Step</Label>
          <Input
            type="number"
            placeholder="Step"
            {...register("ratingScale.step", { valueAsNumber: true })}
            className="bg-white dark:bg-gray-800"
          />
        </div>
      </div>

      <div className="space-y-2">
        <Label className="text-sm">Correct Rating (optional)</Label>
        <Input
          type="number"
          placeholder="Expected rating"
          {...register("correctRating", { valueAsNumber: true })}
          className="bg-white dark:bg-gray-800"
        />
        <p className="text-xs text-gray-500">Leave empty if there&apos;s no specific correct answer</p>
      </div>
    </div>
  )
}
