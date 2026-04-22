/**
 * Quiz Renderer
 * Routes to the appropriate question type renderer
 */

"use client"

import {
  type QuizEntry,
  type QuizAnswerState,
  QuizEntryType,
} from "../types"
import { SingleChoiceRenderer } from "./single-choice-renderer"
import { MultipleChoiceRenderer } from "./multiple-choice-renderer"
import { TrueFalseRenderer } from "./true-false-renderer"
import { FillBlankRenderer } from "./fill-blank-renderer"
import { ShortAnswerRenderer } from "./short-answer-renderer"
import { EssayRenderer } from "./essay-renderer"
import { MatchingRenderer } from "./matching-renderer"
import { OrderingRenderer } from "./ordering-renderer"
import { CategorizationRenderer } from "./categorization-renderer"
import { RatingRenderer } from "./rating-renderer"
import { NumericRenderer } from "./numeric-renderer"
import { FormulaRenderer } from "./formula-renderer"
import { HotspotRenderer } from "./hotspot-renderer"
import { HighlightRenderer } from "./highlight-renderer"

interface QuizRendererProps {
  entry: QuizEntry
  answerState: QuizAnswerState
  onAnswerChange: (updates: Partial<QuizAnswerState>) => void
  disabled?: boolean
  showFeedback?: boolean
}

export function QuizRenderer({
  entry,
  answerState,
  onAnswerChange,
  disabled = false,
  showFeedback = false,
}: QuizRendererProps) {
  const commonProps = {
    answerState,
    onAnswerChange,
    disabled,
    showFeedback,
  }

  switch (entry.type) {
    case QuizEntryType.SingleChoice:
      return <SingleChoiceRenderer entry={entry} {...commonProps} />

    case QuizEntryType.MultipleChoice:
      return <MultipleChoiceRenderer entry={entry} {...commonProps} />

    case QuizEntryType.TrueFalse:
      return <TrueFalseRenderer entry={entry} {...commonProps} />

    case QuizEntryType.FillInTheBlank:
      return <FillBlankRenderer entry={entry} {...commonProps} />

    case QuizEntryType.ShortAnswer:
      return <ShortAnswerRenderer entry={entry} {...commonProps} />

    case QuizEntryType.Essay:
      return <EssayRenderer entry={entry} {...commonProps} />

    case QuizEntryType.Matching:
      return <MatchingRenderer entry={entry} {...commonProps} />

    case QuizEntryType.Ordering:
      return <OrderingRenderer entry={entry} {...commonProps} />

    case QuizEntryType.Categorization:
      return <CategorizationRenderer entry={entry} {...commonProps} />

    case QuizEntryType.Rating:
      return <RatingRenderer entry={entry} {...commonProps} />

    case QuizEntryType.Numeric:
      return <NumericRenderer entry={entry} {...commonProps} />

    case QuizEntryType.Formula:
      return <FormulaRenderer entry={entry} {...commonProps} />

    case QuizEntryType.Hotspot:
      return <HotspotRenderer entry={entry} {...commonProps} />

    case QuizEntryType.Highlight:
      return <HighlightRenderer entry={entry} {...commonProps} />

    default:
      return (
        <div className="text-red-600 p-4 border border-red-200 rounded-lg bg-red-50">
          Unsupported question type
        </div>
      )
  }
}
