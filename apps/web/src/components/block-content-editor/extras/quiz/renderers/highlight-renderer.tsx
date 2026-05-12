/**
 * Highlight Renderer
 * Student selects (highlights) spans of text by clicking on words.
 * After submission, shows which selections were correct/incorrect/missed.
 */

"use client"

import { useCallback, useMemo } from "react"
import type { HighlightEntry, HighlightSpan, QuizAnswerState } from "../types"

interface HighlightRendererProps {
  entry: HighlightEntry
  answerState: QuizAnswerState
  onAnswerChange: (updates: Partial<QuizAnswerState>) => void
  disabled?: boolean
  showFeedback?: boolean
}

/** Split plain text into word tokens preserving whitespace as separate tokens */
function tokenize(text: string): { text: string; start: number; end: number; isWord: boolean }[] {
  const tokens: { text: string; start: number; end: number; isWord: boolean }[] = []
  const regex = /(\S+|\s+)/g
  let match
  while ((match = regex.exec(text)) !== null) {
    tokens.push({
      text: match[0],
      start: match.index,
      end: match.index + match[0].length,
      isWord: match[0].trim().length > 0,
    })
  }
  return tokens
}

/** Check if a token overlaps with any span in a list */
function tokenOverlaps(token: { start: number; end: number }, spans: HighlightSpan[]): boolean {
  return spans.some((s) => token.start < s.end && token.end > s.start)
}

export function HighlightRenderer({
  entry,
  answerState,
  onAnswerChange,
  disabled = false,
  showFeedback = false,
}: HighlightRendererProps) {
  const tokens = useMemo(() => tokenize(entry.plainText), [entry.plainText])

  // Student selections stored as JSON array of HighlightSpan in textAnswers["highlight_spans"]
  const studentSpans: HighlightSpan[] = useMemo(() => {
    try {
      return JSON.parse(answerState.textAnswers["highlight_spans"] || "[]")
    } catch {
      return []
    }
  }, [answerState.textAnswers])

  const revealFeedback = showFeedback && (entry.settings?.showFeedback ?? true)
  const revealCorrect = showFeedback && (entry.settings?.showCorrectAnswer ?? true)

  const toggleToken = useCallback(
    (token: { start: number; end: number }) => {
      if (disabled || showFeedback) return

      const already = studentSpans.findIndex(
        (s) => s.start === token.start && s.end === token.end
      )
      let next: HighlightSpan[]
      if (already >= 0) {
        next = studentSpans.filter((_, i) => i !== already)
      } else {
        next = [...studentSpans, { start: token.start, end: token.end }]
      }

      onAnswerChange({
        textAnswers: {
          ...answerState.textAnswers,
          highlight_spans: JSON.stringify(next),
        },
      })
    },
    [disabled, showFeedback, studentSpans, answerState.textAnswers, onAnswerChange]
  )

  const getTokenStyle = (token: { start: number; end: number; isWord: boolean }) => {
    if (!token.isWord) return undefined

    const isSelected = tokenOverlaps(token, studentSpans)
    const isCorrect = tokenOverlaps(token, entry.highlights)

    if (revealFeedback) {
      if (isSelected && isCorrect) {
        // Correct hit
        return "bg-green-200 dark:bg-green-800/50 text-green-900 dark:text-green-100 rounded-sm"
      }
      if (isSelected && !isCorrect) {
        // False positive
        return "bg-red-200 dark:bg-red-800/50 text-red-900 dark:text-red-100 rounded-sm line-through"
      }
      if (!isSelected && isCorrect && revealCorrect) {
        // Missed
        return "bg-yellow-100 dark:bg-yellow-900/30 text-yellow-800 dark:text-yellow-200 rounded-sm border-b-2 border-dashed border-yellow-400 dark:border-yellow-600"
      }
      return undefined
    }

    if (isSelected) {
      return "bg-blue-200 dark:bg-blue-700/50 text-blue-900 dark:text-blue-100 rounded-sm"
    }

    return undefined
  }

  return (
    <div className="space-y-3">
      {/* Text with clickable words */}
      <div
        className={`p-4 rounded-lg border text-base leading-loose select-none ${
          showFeedback
            ? "bg-gray-50 dark:bg-gray-800/50 border-gray-200 dark:border-gray-700"
            : "bg-white dark:bg-gray-800 border-gray-200 dark:border-gray-700"
        }`}
      >
        {tokens.map((token, i) => {
          if (!token.isWord) {
            return <span key={i}>{token.text}</span>
          }
          const style = getTokenStyle(token)
          return (
            <span
              key={i}
              className={`${
                !disabled && !showFeedback ? "cursor-pointer hover:bg-blue-100 dark:hover:bg-blue-800/30" : ""
              } ${style || ""} px-0.5 py-0.5 transition-colors`}
              onClick={() => toggleToken(token)}
            >
              {token.text}
            </span>
          )
        })}
      </div>

      {/* Instructions */}
      {!showFeedback && studentSpans.length === 0 && (
        <div className="text-sm text-gray-500 dark:text-gray-400 text-center">
          Click on words to highlight them
        </div>
      )}
      {!showFeedback && studentSpans.length > 0 && (
        <div className="text-sm text-blue-600 dark:text-blue-400 text-center">
          {studentSpans.length} word{studentSpans.length !== 1 ? "s" : ""} selected — click to toggle, then submit
        </div>
      )}
    </div>
  )
}
