/**
 * Essay Renderer
 * Rich text editor for essay questions using an isolated Lexical instance
 */

"use client"

import { useMemo, useCallback, useRef, useState, useEffect } from "react"
import type { EssayEntry, QuizAnswerState } from "../types"
import { EssayLexicalEditor } from "./essay-lexical-editor"

interface EssayRendererProps {
  entry: EssayEntry
  answerState: QuizAnswerState
  onAnswerChange: (updates: Partial<QuizAnswerState>) => void
  disabled?: boolean
  showFeedback?: boolean
}

export function EssayRenderer({
  entry,
  answerState,
  onAnswerChange,
  disabled = false,
  showFeedback = false,
}: EssayRendererProps) {
  const serialized = answerState.textAnswers["main"] || ""
  const plainText = answerState.textAnswers["main_plain"] || ""

  // Force full Lexical remount when the quiz resets (disabled → enabled transition)
  const [resetKey, setResetKey] = useState(0)
  const prevEffectiveDisabled = useRef(disabled || showFeedback)

  useEffect(() => {
    const wasDisabled = prevEffectiveDisabled.current
    const isDisabled = disabled || showFeedback
    prevEffectiveDisabled.current = isDisabled
    if (wasDisabled && !isDisabled) {
      setResetKey((k) => k + 1)
    }
  }, [disabled, showFeedback])

  const wordCount = useMemo(() => {
    return plainText.trim().split(/\s+/).filter(Boolean).length
  }, [plainText])

  const answerStateRef = useRef(answerState)
  answerStateRef.current = answerState

  const handleChange = useCallback(
    (newSerialized: string, newPlainText: string) => {
      onAnswerChange({
        textAnswers: {
          ...answerStateRef.current.textAnswers,
          main: newSerialized,
          main_plain: newPlainText,
        },
      })
    },
    [onAnswerChange],
  )

  const isWordCountValid =
    (!entry.minWordCount || wordCount >= entry.minWordCount) &&
    (!entry.maxWordCount || wordCount <= entry.maxWordCount)

  return (
    <div className="space-y-3">
      <EssayLexicalEditor
        key={resetKey}
        initialState={serialized || undefined}
        onChange={handleChange}
        disabled={disabled || showFeedback}
        placeholder="Write your answer..."
      />

      {entry.showWordCount && (
        <div className="flex items-center justify-between text-sm text-gray-600">
          <span className={!isWordCountValid ? "text-red-600" : ""}>
            {wordCount} word{wordCount !== 1 ? "s" : ""}
          </span>
          {(entry.minWordCount || entry.maxWordCount) && (
            <span>
              {entry.minWordCount && `Min: ${entry.minWordCount}`}
              {entry.minWordCount && entry.maxWordCount && " • "}
              {entry.maxWordCount && `Max: ${entry.maxWordCount}`}
            </span>
          )}
        </div>
      )}
    </div>
  )
}
