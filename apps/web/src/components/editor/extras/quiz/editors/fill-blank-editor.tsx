/**
 * Fill in the Blank Editor
 * Configure expected answers for each blank
 */

"use client"

import { useEffect } from "react"
import { useFieldArray, useFormContext } from "react-hook-form"
import { Plus, Trash2, X } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import type { FillInTheBlankEntry } from "../types"
import { FillBlankInputType } from "../types"

export function FillBlankEditor() {
  const { control, watch, setValue, getValues } = useFormContext<FillInTheBlankEntry>()
  const { fields, append, remove, replace } = useFieldArray({
    control,
    name: "blanks",
  })

  const stem = watch("stem")
  const blankCount = (stem?.split("___").length || 1) - 1

  // Auto-sync blanks with ___ markers in stem
  useEffect(() => {
    const currentBlanks = getValues("blanks") || []
    
    if (blankCount > currentBlanks.length) {
      // Add missing blanks
      const newBlanks = [...currentBlanks]
      for (let i = currentBlanks.length; i < blankCount; i++) {
        newBlanks.push({
          id: Math.random().toString(36).substring(7),
          position: i,
          input: {
            type: FillBlankInputType.Text,
            acceptedAnswers: [""],
          },
        })
      }
      replace(newBlanks)
    } else if (blankCount < currentBlanks.length && blankCount >= 0) {
      // Remove extra blanks
      replace(currentBlanks.slice(0, blankCount))
    }
  }, [blankCount, getValues, replace])

  const addAcceptedAnswer = (blankIndex: number) => {
    const currentAnswers = getValues(`blanks.${blankIndex}.input.acceptedAnswers`) || []
    setValue(`blanks.${blankIndex}.input.acceptedAnswers`, [...currentAnswers, ""])
  }

  const removeAcceptedAnswer = (blankIndex: number, answerIndex: number) => {
    const currentAnswers = getValues(`blanks.${blankIndex}.input.acceptedAnswers`) || []
    if (currentAnswers.length > 1) {
      setValue(
        `blanks.${blankIndex}.input.acceptedAnswers`,
        currentAnswers.filter((_: string, i: number) => i !== answerIndex)
      )
    }
  }

  const updateAcceptedAnswer = (blankIndex: number, answerIndex: number, value: string) => {
    const currentAnswers = [...(getValues(`blanks.${blankIndex}.input.acceptedAnswers`) || [])]
    currentAnswers[answerIndex] = value
    setValue(`blanks.${blankIndex}.input.acceptedAnswers`, currentAnswers)
  }

  return (
    <div className="space-y-4">
      <div className="text-sm text-gray-600 dark:text-gray-400 bg-blue-50 dark:bg-blue-950/30 p-3 rounded-lg border border-blue-200 dark:border-blue-800">
        <p className="font-medium mb-2">💡 Tip for Fill-in-the-Blank:</p>
        <p>
          Use <code className="bg-gray-200 dark:bg-gray-700 px-1 rounded">___</code> to create blanks in your question.
        </p>
        <p className="mt-1">Example: &quot;The capital of Brazil is ___ and it is in the state of ___.&quot;</p>
      </div>

      <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
        Blank Configuration ({blankCount} blank{blankCount !== 1 ? "s" : ""} detected)
      </Label>

      {fields.length === 0 && (
        <div className="text-center py-8 text-gray-500 dark:text-gray-400 bg-gray-50 dark:bg-gray-800/50 rounded-lg border border-gray-200 dark:border-gray-700">
          <p>No blanks detected yet.</p>
          <p className="text-sm mt-1">
            Add <code className="bg-gray-200 dark:bg-gray-700 px-1 rounded">___</code> in your question above to create blanks.
          </p>
        </div>
      )}

      {fields.map((field, blankIndex) => {
        const acceptedAnswers = watch(`blanks.${blankIndex}.input.acceptedAnswers`) || [""]
        
        return (
          <div
            key={field.id}
            className="border border-gray-200 dark:border-gray-700 rounded-lg p-4 space-y-3 bg-gray-50 dark:bg-gray-800/30"
          >
            <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
              Blank #{blankIndex + 1}
            </Label>

            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <Label className="text-xs text-gray-600 dark:text-gray-400">
                  Accepted Answers ({acceptedAnswers.length})
                </Label>
              </div>

              <div className="space-y-2">
                {acceptedAnswers.map((answer: string, answerIndex: number) => (
                  <div key={answerIndex} className="flex items-center gap-2">
                    <Input
                      placeholder={`Answer option ${answerIndex + 1}`}
                      value={answer}
                      onChange={(e) => updateAcceptedAnswer(blankIndex, answerIndex, e.target.value)}
                      className="bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 flex-1"
                    />
                    {acceptedAnswers.length > 1 && (
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        onClick={() => removeAcceptedAnswer(blankIndex, answerIndex)}
                        className="text-gray-500 hover:text-red-600 dark:text-gray-400 dark:hover:text-red-400 h-9 w-9 shrink-0"
                      >
                        <X className="h-4 w-4" />
                      </Button>
                    )}
                  </div>
                ))}
              </div>

              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => addAcceptedAnswer(blankIndex)}
                className="w-full bg-transparent border-dashed border-gray-300 dark:border-gray-600 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800"
              >
                <Plus className="h-4 w-4 mr-2" />
                Add Alternative Answer
              </Button>
            </div>
          </div>
        )
      })}
    </div>
  )
}
