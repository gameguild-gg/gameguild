import type { ComponentType } from "react";
import { QuizEntryType } from "@game-guild/quiz";
import { CategorizationEditor } from "../questions/categorization/editor";
import { EssayEditor } from "../questions/essay/editor";
import { FillBlankEditor } from "../questions/fill-blank/editor";
import { FormulaEditor } from "../questions/formula/editor";
import { HighlightEditor } from "../questions/highlight/editor";
import { HotspotEditor } from "../questions/hotspot/editor";
import { MatchingEditor } from "../questions/matching/editor";
import { MultipleChoiceEditor } from "../questions/multiple-choice/editor";
import { NumericEditor } from "../questions/numeric/editor";
import { OrderingEditor } from "../questions/ordering/editor";
import { RatingEditor } from "../questions/rating/editor";
import { ShortAnswerEditor } from "../questions/short-answer/editor";
import { SingleChoiceEditor } from "../questions/single-choice/editor";
import { TrueFalseEditor } from "../questions/true-false/editor";

export const quizEditorRegistry: Record<QuizEntryType, ComponentType> = {
  [QuizEntryType.SingleChoice]: SingleChoiceEditor,
  [QuizEntryType.MultipleChoice]: MultipleChoiceEditor,
  [QuizEntryType.TrueFalse]: TrueFalseEditor,
  [QuizEntryType.FillInTheBlank]: FillBlankEditor,
  [QuizEntryType.ShortAnswer]: ShortAnswerEditor,
  [QuizEntryType.Essay]: EssayEditor,
  [QuizEntryType.Matching]: MatchingEditor,
  [QuizEntryType.Ordering]: OrderingEditor,
  [QuizEntryType.Categorization]: CategorizationEditor,
  [QuizEntryType.Rating]: RatingEditor,
  [QuizEntryType.Numeric]: NumericEditor,
  [QuizEntryType.Formula]: FormulaEditor,
  [QuizEntryType.Hotspot]: HotspotEditor,
  [QuizEntryType.Highlight]: HighlightEditor,
};

export const quizQuestionLabels: Record<QuizEntryType, string> = {
  [QuizEntryType.SingleChoice]: "Single choice",
  [QuizEntryType.MultipleChoice]: "Multiple choice",
  [QuizEntryType.TrueFalse]: "True / false",
  [QuizEntryType.FillInTheBlank]: "Fill in the blank",
  [QuizEntryType.ShortAnswer]: "Short answer",
  [QuizEntryType.Essay]: "Essay",
  [QuizEntryType.Matching]: "Matching",
  [QuizEntryType.Ordering]: "Ordering",
  [QuizEntryType.Categorization]: "Categorization",
  [QuizEntryType.Rating]: "Rating",
  [QuizEntryType.Numeric]: "Numeric",
  [QuizEntryType.Formula]: "Formula",
  [QuizEntryType.Hotspot]: "Hotspot",
  [QuizEntryType.Highlight]: "Highlight",
};
