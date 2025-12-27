/**
 * Multiple Choice Question Editor
 * Edit mode for creating/editing multiple choice questions
 */

"use client"

import { useFieldArray, useFormContext } from "react-hook-form"
import { Plus, X } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"

export function MultipleChoiceEditor() {
  const { register, control, watch } = useFormContext()
  const { fields, append, remove } = useFieldArray({
    control,
    name: "answers",
  })

  const addAnswer = () => {
    append({
      id: Math.random().toString(36).substring(7),
      text: "",
      isCorrect: false,
    })
  }

  return (
    <div className="space-y-3">
      <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">Answer Options</Label>

      {fields.map((field, index) => (
        <div key={field.id} className="flex items-center gap-2">
          <input
            type="checkbox"
            {...register(`answers.${index}.isCorrect`)}
            className="w-4 h-4 rounded accent-blue-600 dark:accent-blue-400"
          />
          <Input
            placeholder="Enter answer"
            {...register(`answers.${index}.text`, { required: true })}
            className="flex-1 bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 focus:border-blue-500 dark:focus:border-blue-400"
          />
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={() => remove(index)}
            disabled={fields.length <= 2}
            className="hover:bg-red-50 dark:hover:bg-red-950/30 hover:text-red-600 dark:hover:text-red-400"
          >
            <X className="h-4 w-4" />
          </Button>
        </div>
      ))}

      <Button type="button" variant="outline" size="sm" onClick={addAnswer} className="w-full">
        <Plus className="h-4 w-4 mr-1" />
        Add Answer
      </Button>
    </div>
  )
}
