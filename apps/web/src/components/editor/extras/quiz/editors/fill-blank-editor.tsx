/**
 * Fill in the Blank Editor
 * Configure expected answers for each blank with three input modes:
 * 1. Text - Type the answer
 * 2. Dropdown - Select from options (first is correct)
 * 3. Word Bank - Drag from shared pool (first is correct)
 */

"use client"

import { useEffect } from "react"
import { useFieldArray, useFormContext } from "react-hook-form"
import { Plus, X, Type, ChevronDown, LayoutGrid } from "lucide-react"
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
import type { FillInTheBlankEntry, FillBlankInput } from "../types"
import { FillBlankInputType } from "../types"

const INPUT_TYPE_OPTIONS = [
  { value: FillBlankInputType.Text, label: "Text Input", icon: Type, description: "Type the answer" },
  { value: FillBlankInputType.Dropdown, label: "Dropdown", icon: ChevronDown, description: "Select from options" },
  { value: FillBlankInputType.WordBank, label: "Word Bank", icon: LayoutGrid, description: "Drag from word pool" },
]

function createDefaultInput(type: FillBlankInputType): FillBlankInput {
  switch (type) {
    case FillBlankInputType.Text:
      return { type: FillBlankInputType.Text, acceptedAnswers: [""] }
    case FillBlankInputType.Dropdown:
      return { type: FillBlankInputType.Dropdown, options: ["", ""] }
    case FillBlankInputType.WordBank:
      return { type: FillBlankInputType.WordBank, words: ["", ""] }
  }
}

export function FillBlankEditor() {
  const { control, watch, setValue, getValues } = useFormContext<FillInTheBlankEntry>()
  const { fields, replace } = useFieldArray({
    control,
    name: "blanks",
  })

  const stem = watch("stem")
  const blanks = watch("blanks") || []
  const blankCount = (stem?.split("___").length || 1) - 1

  // Auto-sync blanks with ___ markers in stem
  useEffect(() => {
    const currentBlanks = getValues("blanks") || []

    if (blankCount > currentBlanks.length) {
      const newBlanks = [...currentBlanks]
      for (let i = currentBlanks.length; i < blankCount; i++) {
        newBlanks.push({
          id: Math.random().toString(36).substring(7),
          position: i,
          input: createDefaultInput(FillBlankInputType.Text),
        })
      }
      replace(newBlanks)
    } else if (blankCount < currentBlanks.length && blankCount >= 0) {
      replace(currentBlanks.slice(0, blankCount))
    }
  }, [blankCount, getValues, replace])

  const changeInputType = (blankIndex: number, newType: FillBlankInputType) => {
    setValue(`blanks.${blankIndex}.input`, createDefaultInput(newType))
  }

  // ============================================================================
  // Text Input Handlers
  // ============================================================================

  const addTextAnswer = (blankIndex: number) => {
    const input = getValues(`blanks.${blankIndex}.input`)
    if (input.type === FillBlankInputType.Text) {
      setValue(`blanks.${blankIndex}.input.acceptedAnswers`, [...input.acceptedAnswers, ""])
    }
  }

  const removeTextAnswer = (blankIndex: number, answerIndex: number) => {
    const input = getValues(`blanks.${blankIndex}.input`)
    if (input.type === FillBlankInputType.Text && input.acceptedAnswers.length > 1) {
      setValue(
        `blanks.${blankIndex}.input.acceptedAnswers`,
        input.acceptedAnswers.filter((_: string, i: number) => i !== answerIndex)
      )
    }
  }

  const updateTextAnswer = (blankIndex: number, answerIndex: number, value: string) => {
    const input = getValues(`blanks.${blankIndex}.input`)
    if (input.type === FillBlankInputType.Text) {
      const newAnswers = [...input.acceptedAnswers]
      newAnswers[answerIndex] = value
      setValue(`blanks.${blankIndex}.input.acceptedAnswers`, newAnswers)
    }
  }

  // ============================================================================
  // Dropdown Handlers
  // ============================================================================

  const addDropdownOption = (blankIndex: number) => {
    const input = getValues(`blanks.${blankIndex}.input`)
    if (input.type === FillBlankInputType.Dropdown) {
      setValue(`blanks.${blankIndex}.input.options`, [...input.options, ""])
    }
  }

  const removeDropdownOption = (blankIndex: number, optionIndex: number) => {
    const input = getValues(`blanks.${blankIndex}.input`)
    if (input.type === FillBlankInputType.Dropdown && input.options.length > 2) {
      setValue(
        `blanks.${blankIndex}.input.options`,
        input.options.filter((_: string, i: number) => i !== optionIndex)
      )
    }
  }

  const updateDropdownOption = (blankIndex: number, optionIndex: number, value: string) => {
    const input = getValues(`blanks.${blankIndex}.input`)
    if (input.type === FillBlankInputType.Dropdown) {
      const newOptions = [...input.options]
      newOptions[optionIndex] = value
      setValue(`blanks.${blankIndex}.input.options`, newOptions)
    }
  }

  // ============================================================================
  // Word Bank Handlers
  // ============================================================================

  const addWordBankWord = (blankIndex: number) => {
    const input = getValues(`blanks.${blankIndex}.input`)
    if (input.type === FillBlankInputType.WordBank) {
      setValue(`blanks.${blankIndex}.input.words`, [...input.words, ""])
    }
  }

  const removeWordBankWord = (blankIndex: number, wordIndex: number) => {
    const input = getValues(`blanks.${blankIndex}.input`)
    if (input.type === FillBlankInputType.WordBank && input.words.length > 2) {
      setValue(
        `blanks.${blankIndex}.input.words`,
        input.words.filter((_: string, i: number) => i !== wordIndex)
      )
    }
  }

  const updateWordBankWord = (blankIndex: number, wordIndex: number, value: string) => {
    const input = getValues(`blanks.${blankIndex}.input`)
    if (input.type === FillBlankInputType.WordBank) {
      const newWords = [...input.words]
      newWords[wordIndex] = value
      setValue(`blanks.${blankIndex}.input.words`, newWords)
    }
  }

  // Check if any blank uses Word Bank
  const hasWordBankBlanks = blanks.some(b => b.input.type === FillBlankInputType.WordBank)

  // Collect all Word Bank words for preview
  const allWordBankWords = blanks
    .filter(b => b.input.type === FillBlankInputType.WordBank)
    .flatMap(b => (b.input as { words: string[] }).words)
    .filter(w => w.trim() !== "")

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
        const input = blanks[blankIndex]?.input
        if (!input) return null

        return (
          <div
            key={field.id}
            className="border border-gray-200 dark:border-gray-700 rounded-lg p-4 space-y-4 bg-gray-50 dark:bg-gray-800/30"
          >
            <div className="flex items-center justify-between">
              <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                Blank #{blankIndex + 1}
              </Label>
              <Select
                value={input.type}
                onValueChange={(value) => changeInputType(blankIndex, value as FillBlankInputType)}
              >
                <SelectTrigger className="w-40 h-8 text-xs bg-white dark:bg-gray-800">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {INPUT_TYPE_OPTIONS.map((option) => (
                    <SelectItem key={option.value} value={option.value}>
                      <div className="flex items-center gap-2">
                        <option.icon className="h-3 w-3" />
                        <span>{option.label}</span>
                      </div>
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {/* Text Input Mode */}
            {input.type === FillBlankInputType.Text && (
              <div className="space-y-2">
                <Label className="text-xs text-gray-600 dark:text-gray-400">
                  Accepted Answers (any of these will be correct)
                </Label>
                <div className="space-y-2">
                  {input.acceptedAnswers.map((answer: string, answerIndex: number) => (
                    <div key={answerIndex} className="flex items-center gap-2">
                      <Input
                        placeholder={`Answer ${answerIndex + 1}`}
                        value={answer}
                        onChange={(e) => updateTextAnswer(blankIndex, answerIndex, e.target.value)}
                        className="bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 flex-1"
                      />
                      {input.acceptedAnswers.length > 1 && (
                        <Button
                          type="button"
                          variant="ghost"
                          size="icon"
                          onClick={() => removeTextAnswer(blankIndex, answerIndex)}
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
                  onClick={() => addTextAnswer(blankIndex)}
                  className="w-full bg-transparent border-dashed border-gray-300 dark:border-gray-600 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800"
                >
                  <Plus className="h-4 w-4 mr-2" />
                  Add Alternative Answer
                </Button>
              </div>
            )}

            {/* Dropdown Mode */}
            {input.type === FillBlankInputType.Dropdown && (
              <div className="space-y-2">
                <Label className="text-xs text-gray-600 dark:text-gray-400">
                  Options (first is the correct answer)
                </Label>
                <div className="space-y-2">
                  {input.options.map((option: string, optionIndex: number) => (
                    <div key={optionIndex} className="flex items-center gap-2">
                      <div className="relative flex-1">
                        <Input
                          placeholder={optionIndex === 0 ? "✓ Correct answer" : `Distractor ${optionIndex}`}
                          value={option}
                          onChange={(e) => updateDropdownOption(blankIndex, optionIndex, e.target.value)}
                          className={`bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 ${
                            optionIndex === 0 ? "border-green-500 dark:border-green-600 ring-1 ring-green-500/20" : ""
                          }`}
                        />
                        {optionIndex === 0 && (
                          <span className="absolute right-3 top-1/2 -translate-y-1/2 text-xs text-green-600 dark:text-green-400 font-medium">
                            Correct
                          </span>
                        )}
                      </div>
                      {input.options.length > 2 && (
                        <Button
                          type="button"
                          variant="ghost"
                          size="icon"
                          onClick={() => removeDropdownOption(blankIndex, optionIndex)}
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
                  onClick={() => addDropdownOption(blankIndex)}
                  className="w-full bg-transparent border-dashed border-gray-300 dark:border-gray-600 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800"
                >
                  <Plus className="h-4 w-4 mr-2" />
                  Add Distractor
                </Button>
              </div>
            )}

            {/* Word Bank Mode */}
            {input.type === FillBlankInputType.WordBank && (
              <div className="space-y-2">
                <Label className="text-xs text-gray-600 dark:text-gray-400">
                  Words (first is the correct answer)
                </Label>
                <div className="space-y-2">
                  {input.words.map((word: string, wordIndex: number) => (
                    <div key={wordIndex} className="flex items-center gap-2">
                      <div className="relative flex-1">
                        <Input
                          placeholder={wordIndex === 0 ? "✓ Correct word" : `Distractor ${wordIndex}`}
                          value={word}
                          onChange={(e) => updateWordBankWord(blankIndex, wordIndex, e.target.value)}
                          className={`bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 ${
                            wordIndex === 0 ? "border-green-500 dark:border-green-600 ring-1 ring-green-500/20" : ""
                          }`}
                        />
                        {wordIndex === 0 && (
                          <span className="absolute right-3 top-1/2 -translate-y-1/2 text-xs text-green-600 dark:text-green-400 font-medium">
                            Correct
                          </span>
                        )}
                      </div>
                      {input.words.length > 2 && (
                        <Button
                          type="button"
                          variant="ghost"
                          size="icon"
                          onClick={() => removeWordBankWord(blankIndex, wordIndex)}
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
                  onClick={() => addWordBankWord(blankIndex)}
                  className="w-full bg-transparent border-dashed border-gray-300 dark:border-gray-600 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800"
                >
                  <Plus className="h-4 w-4 mr-2" />
                  Add Distractor
                </Button>
              </div>
            )}
          </div>
        )
      })}

      {/* Word Bank Preview */}
      {hasWordBankBlanks && allWordBankWords.length > 0 && (
        <div className="border border-purple-200 dark:border-purple-800 rounded-lg p-4 bg-purple-50 dark:bg-purple-950/30">
          <Label className="text-sm font-medium text-purple-700 dark:text-purple-300 mb-3 block">
            📦 Word Bank Preview (shuffled in quiz)
          </Label>
          <div className="flex flex-wrap gap-2">
            {allWordBankWords.map((word, index) => (
              <span
                key={index}
                className="px-3 py-1.5 bg-white dark:bg-gray-800 border border-purple-300 dark:border-purple-700 rounded-lg text-sm text-gray-700 dark:text-gray-300 shadow-sm"
              >
                {word}
              </span>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}
