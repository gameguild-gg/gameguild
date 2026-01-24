/**
 * Fill in the Blank Renderer
 * Renders questions with inline input fields for blanks
 * Supports three modes: Text, Dropdown, and Word Bank (drag-drop)
 */

"use client"

import { useMemo, useState, useCallback } from "react"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import type { FillInTheBlankEntry, QuizAnswerState, FillBlankField } from "../types"
import { FillBlankInputType } from "../types"

interface FillBlankRendererProps {
  entry: FillInTheBlankEntry
  answerState: QuizAnswerState
  onAnswerChange: (updates: Partial<QuizAnswerState>) => void
  disabled?: boolean
  showFeedback?: boolean
}

// Utility to shuffle array
function shuffleArray<T>(array: T[]): T[] {
  const shuffled = [...array]
  for (let i = shuffled.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1))
    const temp = shuffled[i]
    shuffled[i] = shuffled[j]!
    shuffled[j] = temp!
  }
  return shuffled
}

export function FillBlankRenderer({
  entry,
  answerState,
  onAnswerChange,
  disabled = false,
  showFeedback = false,
}: FillBlankRendererProps) {
  // Split stem by blank markers (___)
  const parts = entry.stem.split("___")

  // Track which word is being dragged
  const [draggedWord, setDraggedWord] = useState<string | null>(null)

  // Serialize blanks for dependency tracking (detects deep changes)
  const blanksKey = JSON.stringify(entry.blanks)

  // Collect all words from Word Bank blanks (no shuffle during editing for better UX)
  const wordBankWords = useMemo(() => {
    const words: { word: string; blankId: string }[] = []
    entry.blanks.forEach((blank) => {
      if (blank.input.type === FillBlankInputType.WordBank) {
        blank.input.words.filter(w => w.trim()).forEach((word) => {
          words.push({ word, blankId: blank.id })
        })
      }
    })
    return words
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [blanksKey])

  // Get used words from answer state
  const usedWords = useMemo(() => {
    const used = new Set<string>()
    entry.blanks.forEach((blank) => {
      if (blank.input.type === FillBlankInputType.WordBank) {
        const answer = answerState.textAnswers[blank.id]
        if (answer) used.add(answer)
      }
    })
    return used
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [blanksKey, answerState.textAnswers])

  // Available words (not yet used)
  const availableWords = wordBankWords.filter(({ word }) => !usedWords.has(word))

  const handleInputChange = useCallback((blankId: string, value: string) => {
    if (disabled || showFeedback) return
    onAnswerChange({
      textAnswers: {
        ...answerState.textAnswers,
        [blankId]: value,
      },
    })
  }, [disabled, showFeedback, answerState.textAnswers, onAnswerChange])

  const handleDragStart = useCallback((word: string) => {
    if (disabled || showFeedback) return
    setDraggedWord(word)
  }, [disabled, showFeedback])

  const handleDragEnd = useCallback(() => {
    setDraggedWord(null)
  }, [])

  const handleDrop = useCallback((blankId: string) => {
    if (draggedWord && !disabled && !showFeedback) {
      // If the blank already has a value, return it to the pool
      handleInputChange(blankId, draggedWord)
      setDraggedWord(null)
    }
  }, [draggedWord, disabled, showFeedback, handleInputChange])

  const handleRemoveFromBlank = useCallback((blankId: string) => {
    if (disabled || showFeedback) return
    handleInputChange(blankId, "")
  }, [disabled, showFeedback, handleInputChange])

  // Pre-shuffle dropdown options (memoized per blank)
  const shuffledDropdownOptions = useMemo(() => {
    const map: Record<string, string[]> = {}
    entry.blanks.forEach((blank) => {
      if (blank.input.type === FillBlankInputType.Dropdown) {
        map[blank.id] = shuffleArray(blank.input.options.filter(o => o.trim()))
      }
    })
    return map
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [blanksKey])

  const renderBlankInput = (blank: FillBlankField) => {
    const { input } = blank
    const currentValue = answerState.textAnswers[blank.id] || ""

    switch (input.type) {
      case FillBlankInputType.Text:
        return (
          <input
            type="text"
            className="inline-block w-40 mx-2 px-3 py-2 border-2 border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:border-blue-500 focus:outline-none transition-colors"
            placeholder="..."
            value={currentValue}
            onChange={(e) => handleInputChange(blank.id, e.target.value)}
            disabled={disabled || showFeedback}
          />
        )

      case FillBlankInputType.Dropdown:
        const options = shuffledDropdownOptions[blank.id] || []
        return (
          <Select
            value={currentValue}
            onValueChange={(value) => handleInputChange(blank.id, value)}
            disabled={disabled || showFeedback}
          >
            <SelectTrigger className="inline-flex w-40 mx-2 bg-white dark:bg-gray-800 border-2 border-gray-300 dark:border-gray-600">
              <SelectValue placeholder="Select..." />
            </SelectTrigger>
            <SelectContent>
              {options.map((option, idx) => (
                <SelectItem key={idx} value={option}>
                  {option}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        )

      case FillBlankInputType.WordBank:
        return (
          <span
            className={`inline-flex items-center justify-center min-w-[8rem] h-10 mx-2 px-3 py-2 border-2 border-dashed rounded-lg transition-all ${
              currentValue
                ? "border-purple-500 bg-purple-50 dark:bg-purple-950/50"
                : "border-gray-400 dark:border-gray-500 bg-gray-50 dark:bg-gray-800"
            } ${!disabled && !showFeedback ? "cursor-pointer" : ""}`}
            onDragOver={(e) => {
              e.preventDefault()
              e.currentTarget.classList.add("border-purple-500", "bg-purple-100", "dark:bg-purple-900/50")
            }}
            onDragLeave={(e) => {
              e.currentTarget.classList.remove("border-purple-500", "bg-purple-100", "dark:bg-purple-900/50")
            }}
            onDrop={(e) => {
              e.preventDefault()
              e.currentTarget.classList.remove("border-purple-500", "bg-purple-100", "dark:bg-purple-900/50")
              handleDrop(blank.id)
            }}
            onClick={() => currentValue && handleRemoveFromBlank(blank.id)}
            title={currentValue ? "Click to remove" : "Drag a word here"}
          >
            {currentValue ? (
              <span className="text-purple-700 dark:text-purple-300 font-medium">{currentValue}</span>
            ) : (
              <span className="text-gray-400 dark:text-gray-500 text-sm">Drop here</span>
            )}
          </span>
        )
    }
  }

  const hasWordBankBlanks = entry.blanks.some(b => b.input.type === FillBlankInputType.WordBank)

  return (
    <div className="space-y-6">
      {/* Question with inline blanks */}
      <div className="text-lg leading-relaxed flex flex-wrap items-center">
        {parts.map((part, index) => (
          <span key={index} className="flex items-center">
            <span>{part}</span>
            {index < parts.length - 1 && entry.blanks[index] && renderBlankInput(entry.blanks[index])}
          </span>
        ))}
      </div>

      {/* Word Bank Pool */}
      {hasWordBankBlanks && (
        <div className="border-2 border-dashed border-purple-300 dark:border-purple-700 rounded-xl p-4 bg-purple-50/50 dark:bg-purple-950/20">
          <p className="text-sm font-medium text-purple-700 dark:text-purple-300 mb-3">
            📦 Word Bank - Drag words to the blanks
          </p>
          {availableWords.length > 0 ? (
            <div className="flex flex-wrap gap-2">
              {availableWords.map(({ word }, index) => (
                <span
                  key={index}
                  draggable={!disabled && !showFeedback}
                  onDragStart={() => handleDragStart(word)}
                  onDragEnd={handleDragEnd}
                  className={`px-4 py-2 bg-white dark:bg-gray-800 border-2 border-purple-300 dark:border-purple-600 rounded-lg text-gray-700 dark:text-gray-300 shadow-sm transition-all ${
                    !disabled && !showFeedback
                      ? "cursor-grab hover:border-purple-500 hover:shadow-md active:cursor-grabbing"
                      : "opacity-70"
                  } ${draggedWord === word ? "opacity-50 scale-95" : ""}`}
                >
                  {word}
                </span>
              ))}
            </div>
          ) : (
            <p className="text-sm text-gray-500 dark:text-gray-400 text-center py-2">
              {usedWords.size > 0
                ? "All words have been placed. Click a filled blank to remove."
                : "Add words in the configuration to see them here."}
            </p>
          )}
        </div>
      )}
    </div>
  )
}
