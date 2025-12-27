/**
 * Fill in the Blank Question Editor
 * Configure expected answers for each blank
 */

"use client"

import { useFieldArray, useFormContext } from "react-hook-form"
import { Plus, Trash2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"

export function FillBlankEditor() {
  const { register, control, watch } = useFormContext()
  const { fields, append, remove } = useFieldArray({
    control,
    name: "fillBlankFields",
  })

  const question = watch("question")
  const blankCount = (question?.split("___").length || 1) - 1

  const addBlankField = () => {
    append({
      id: Math.random().toString(36).substring(7),
      position: fields.length,
      expectedWords: [""],
      alternatives: [],
    })
  }

  return (
    <div className="space-y-4">
      <div className="text-sm text-gray-600 dark:text-gray-400 bg-blue-50 dark:bg-blue-950/30 p-3 rounded-lg border border-blue-200 dark:border-blue-800">
        <p className="font-medium mb-2">💡 Tip for Fill-in-the-Blank:</p>
        <p>
          Use <code className="bg-gray-200 dark:bg-gray-700 px-1 rounded">___</code> to create blanks.
        </p>
        <p>Example: &quot;The capital of Brazil is ___ and it is in the state of ___.&quot;</p>
      </div>

      <div className="flex items-center justify-between">
        <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
          Blank Fields Configuration ({blankCount} blank{blankCount !== 1 ? "s" : ""} detected)
        </Label>
        <Button type="button" variant="outline" size="sm" onClick={addBlankField} className="bg-transparent">
          <Plus className="h-4 w-4 mr-2" />
          Add Blank
        </Button>
      </div>

      {fields.length === 0 && (
        <div className="text-center py-8 text-gray-500 dark:text-gray-400 bg-gray-50 dark:bg-gray-800/50 rounded-lg border border-gray-200 dark:border-gray-700">
          <p>No blanks configured yet.</p>
          <p className="text-sm">
            Add <code className="bg-gray-200 dark:bg-gray-700 px-1 rounded">___</code> to your question to create
            blanks.
          </p>
        </div>
      )}

      {fields.map((field, index) => (
        <div
          key={field.id}
          className="border border-gray-200 dark:border-gray-700 rounded-lg p-4 space-y-3 bg-gray-50 dark:bg-gray-800/30"
        >
          <div className="flex items-center justify-between">
            <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">Blank #{index + 1}</Label>
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => remove(index)}
              className="text-red-600 dark:text-red-400 hover:text-red-700 dark:hover:text-red-300 hover:bg-red-50 dark:hover:bg-red-950/30"
            >
              <Trash2 className="h-4 w-4" />
            </Button>
          </div>

          <div className="space-y-2">
            <Label className="text-xs text-gray-600 dark:text-gray-400">Expected Words</Label>
            <div className="text-xs text-gray-500 dark:text-gray-400 mb-2 bg-blue-50 dark:bg-blue-950/30 p-2 rounded border border-blue-200 dark:border-blue-800">
              Enter acceptable answers separated by double commas (,,). Spaces and punctuation are allowed.
              <br />
              Example: <code className="bg-gray-200 dark:bg-gray-700 px-1 rounded">Brasília,, Brasilia,, BSB</code>
            </div>
            <Input
              placeholder="Enter expected words (separated by ,,)"
              {...register(`fillBlankFields.${index}.expectedWords.0`)}
              className="bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600"
            />
          </div>
        </div>
      ))}
    </div>
  )
}
