/**
 * Rating Editor
 * Configure rating scale settings
 */

"use client"

import { useFormContext } from "react-hook-form"
import { Label } from "@/components/ui/label"
import { Input } from "@/components/ui/input"
import type { RatingEntry } from "../types"

export function RatingEditor() {
  const { register, watch, setValue } = useFormContext<RatingEntry>()
  const scale = watch("scale") || { min: 1, max: 5, step: 1 }
  const correctRating = watch("correctRating")

  // Generate rating options for preview
  const ratingOptions: number[] = []
  for (let i = scale.min; i <= scale.max; i += scale.step) {
    ratingOptions.push(i)
  }

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-3 gap-4">
        <div className="space-y-2">
          <Label className="text-sm font-medium">Minimum</Label>
          <Input
            type="number"
            {...register("scale.min", { valueAsNumber: true })}
            autoComplete="off"
            className="bg-white dark:bg-gray-800"
          />
        </div>
        <div className="space-y-2">
          <Label className="text-sm font-medium">Maximum</Label>
          <Input
            type="number"
            {...register("scale.max", { valueAsNumber: true })}
            autoComplete="off"
            className="bg-white dark:bg-gray-800"
          />
        </div>
        <div className="space-y-2">
          <Label className="text-sm font-medium">Step</Label>
          <Input
            type="number"
            {...register("scale.step", { valueAsNumber: true })}
            autoComplete="off"
            className="bg-white dark:bg-gray-800"
          />
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-2">
          <Label className="text-sm font-medium">Min Label (optional)</Label>
          <Input
            placeholder="e.g., Strongly Disagree"
            {...register("scale.minLabel")}
            autoComplete="off"
            className="bg-white dark:bg-gray-800"
          />
        </div>
        <div className="space-y-2">
          <Label className="text-sm font-medium">Max Label (optional)</Label>
          <Input
            placeholder="e.g., Strongly Agree"
            {...register("scale.maxLabel")}
            autoComplete="off"
            className="bg-white dark:bg-gray-800"
          />
        </div>
      </div>

      <div className="space-y-2">
        <Label className="text-sm font-medium">Correct Rating (optional)</Label>
        <div className="text-xs text-gray-500 dark:text-gray-400 dark:text-gray-400 mb-2">
          Leave empty if any rating is acceptable, or select the correct answer.
        </div>
        <div className="flex items-center gap-2 flex-wrap">
          <button
            type="button"
            onClick={() => setValue("correctRating", undefined)}
            className={`
              px-3 py-2 rounded-lg border-2 text-sm font-medium transition-all
              ${correctRating === undefined
                ? "border-blue-500 bg-blue-50 dark:bg-blue-950/30 text-blue-700 dark:text-blue-400"
                : "border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600"
              }
            `}
          >
            Any
          </button>
          {ratingOptions.map((value) => (
            <button
              key={value}
              type="button"
              onClick={() => setValue("correctRating", value)}
              className={`
                w-10 h-10 rounded-lg border-2 font-bold text-sm transition-all
                ${correctRating === value
                  ? "border-green-500 bg-green-50 dark:bg-green-950/30 text-green-700 dark:text-green-400"
                  : "border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600"
                }
              `}
            >
              {value}
            </button>
          ))}
        </div>
      </div>
    </div>
  )
}
