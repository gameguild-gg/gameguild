/**
 * Single Choice Editor
 * Edit mode for creating/editing single choice questions
 */

"use client"

import { useFieldArray, useFormContext } from "react-hook-form"
import { Plus, X } from "lucide-react"
import { Button } from "@game-guild/ui/components/button"
import { Input } from "@game-guild/ui/components/input"
import { Label } from "@game-guild/ui/components/label"
import type { SingleChoiceEntry } from "@game-guild/quiz"

export function SingleChoiceEditor() {
  const { register, control, watch, setValue } = useFormContext<SingleChoiceEntry>()
  const { fields, append, remove } = useFieldArray({
    control,
    name: "options",
  })

  const correctOptionId = watch("correctOptionId")

  const addOption = () => {
    append({
      id: crypto.randomUUID(),
      text: "",
    })
  }

  const handleCorrectChange = (optionId: string) => {
    setValue("correctOptionId", optionId)
  }

  return (
    <div className="space-y-3">
      <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
        Answer Options
      </Label>

      {fields.map((field, index) => (
        <div key={field.id} className="flex items-center gap-2">
          <input
            type="radio"
            checked={correctOptionId === watch(`options.${index}.id`)}
            onChange={() => handleCorrectChange(watch(`options.${index}.id`))}
            className="w-4 h-4 accent-blue-600 dark:accent-blue-400"
          />
          <Input
            placeholder="Enter answer option"
            {...register(`options.${index}.text`, { required: true })}
            autoComplete="off"
            className="flex-1 bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600"
          />
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={() => remove(index)}
            disabled={fields.length <= 2}
            className="hover:bg-red-50 dark:hover:bg-red-950/30 hover:text-red-600"
          >
            <X className="h-4 w-4" />
          </Button>
        </div>
      ))}

      <Button type="button" variant="outline" size="sm" onClick={addOption} className="w-full">
        <Plus className="h-4 w-4 mr-1" />
        Add Option
      </Button>

      <p className="text-xs text-gray-500 dark:text-gray-400">
        Select the radio button next to the correct answer
      </p>
    </div>
  )
}
