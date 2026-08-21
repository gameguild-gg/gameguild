/**
 * Quiz Renderer
 * Routes to the appropriate question type renderer
 */

"use client"

import type { RendererAnswerState } from "../player/renderer-answer-adapter"

import {
  
  QuizEntryType,
} from "@game-guild/quiz"
import type { QuizRuntimeEntry } from "@game-guild/quiz"
import { SingleChoiceRenderer } from "../questions/single-choice/player"
import { MultipleChoiceRenderer } from "../questions/multiple-choice/player"
import { TrueFalseRenderer } from "../questions/true-false/player"
import { FillBlankRenderer } from "../questions/fill-blank/player"
import { ShortAnswerRenderer } from "../questions/short-answer/player"
import { EssayRenderer } from "../questions/essay/player"
import { MatchingRenderer } from "../questions/matching/player"
import { OrderingRenderer } from "../questions/ordering/player"
import { CategorizationRenderer } from "../questions/categorization/player"
import { RatingRenderer } from "../questions/rating/player"
import { NumericRenderer } from "../questions/numeric/player"
import { FormulaRenderer } from "../questions/formula/player"
import { HotspotRenderer } from "../questions/hotspot/player"
import { HighlightRenderer } from "../questions/highlight/player"

interface QuizRendererProps {
  entry: QuizRuntimeEntry
  answerState: RendererAnswerState
  onAnswerChange: (updates: Partial<RendererAnswerState>) => void
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
