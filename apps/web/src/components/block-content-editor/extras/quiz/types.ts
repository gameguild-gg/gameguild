/**
 * Quiz Assessment Types
 * Clean, type-safe definitions for quiz components
 * Based on base-quiz.ts with improvements for efficiency and clarity
 */

import type { SerializedEditorState } from "lexical"

// ============================================================================
// Enums
// ============================================================================

export enum QuizEntryType {
  SingleChoice = "SINGLE_CHOICE",
  MultipleChoice = "MULTIPLE_CHOICE",
  TrueFalse = "TRUE_FALSE",
  FillInTheBlank = "FILL_IN_THE_BLANK",
  ShortAnswer = "SHORT_ANSWER",
  Essay = "ESSAY",
  Matching = "MATCHING",
  Ordering = "ORDERING",
  Categorization = "CATEGORIZATION",
  Rating = "RATING",
  Numeric = "NUMERIC",
  Formula = "FORMULA",
  Hotspot = "HOTSPOT",
  Highlight = "HIGHLIGHT",
}

export enum FillBlankInputType {
  Text = "TEXT", // Free text input with regex validation
  Number = "NUMBER", // Numeric input with tolerance
  Dropdown = "DROPDOWN", // Select from options
  WordBank = "WORDBANK", // Drag from word bank
}

// ============================================================================
// Base Interfaces
// ============================================================================

export interface QuizFeedback {
  correct?: string
  incorrect?: string
  general?: string
}

export interface QuizSettings {
  allowRetry: boolean
  shuffleOptions?: boolean
  showFeedback?: boolean // Whether to show correct/incorrect feedback after submission
  showCorrectAnswer?: boolean // Whether to reveal the correct answer after submission
}

// ============================================================================
// Question Entry Base
// ============================================================================

interface QuizEntryBase {
  stem: string // The question text
  points?: number
  feedback?: QuizFeedback
  settings: QuizSettings
}

// ============================================================================
// Single Choice Entry
// ============================================================================

export interface SingleChoiceOption {
  id: string
  text: string
}

export interface SingleChoiceEntry extends QuizEntryBase {
  type: QuizEntryType.SingleChoice
  options: SingleChoiceOption[]
  correctOptionId: string
}

// ============================================================================
// Multiple Choice Entry
// ============================================================================

export interface MultipleChoiceOption {
  id: string
  text: string
}

export interface MultipleChoiceEntry extends QuizEntryBase {
  type: QuizEntryType.MultipleChoice
  options: MultipleChoiceOption[]
  correctOptionIds: string[]
}

// ============================================================================
// True/False Entry
// ============================================================================

export interface TrueFalseEntry extends QuizEntryBase {
  type: QuizEntryType.TrueFalse
  correctAnswer: boolean
}

// ============================================================================
// Fill in the Blank Entry
// ============================================================================

export interface FillBlankTextInput {
  type: FillBlankInputType.Text
  acceptedAnswers: string[] // List of acceptable answers
  caseSensitive?: boolean
}

export interface FillBlankNumberInput {
  type: FillBlankInputType.Number
  correctValue: number
  tolerance?: number // Accepted margin of error (±)
  requiredPrecision?: number // Required number of decimal places
  unit?: string // Expected unit suffix (e.g. "kg", "%", "m/s")
  requireUnit?: boolean // Whether the unit must be included in the answer
  allowNegative?: boolean // Whether negative answers are accepted (default: true)
}

export interface FillBlankDropdownInput {
  type: FillBlankInputType.Dropdown
  options: string[] // First option is correct, rest are distractors
}

export interface FillBlankWordBankInput {
  type: FillBlankInputType.WordBank
  words: string[] // First word is correct, rest are distractors
}

export type FillBlankInput =
  | FillBlankTextInput
  | FillBlankNumberInput
  | FillBlankDropdownInput
  | FillBlankWordBankInput

export interface FillBlankField {
  id: string
  position: number // Position of the blank in the stem (0-indexed)
  input: FillBlankInput
}

export interface FillInTheBlankEntry extends QuizEntryBase {
  type: QuizEntryType.FillInTheBlank
  // Stem contains ___ or _word_ markers for blanks
  blanks: FillBlankField[]
}

// ============================================================================
// Short Answer Entry
// ============================================================================

export interface ShortAnswerEntry extends QuizEntryBase {
  type: QuizEntryType.ShortAnswer
  acceptedAnswers: string[]
  caseSensitive?: boolean
}

// ============================================================================
// Essay Entry
// ============================================================================

export interface EssayEntry extends QuizEntryBase {
  type: QuizEntryType.Essay
  minWordCount?: number
  maxWordCount?: number
  showWordCount?: boolean
  correctAnswer?: SerializedEditorState | null // Serialized Lexical editor state for the model/correct answer
  correctAnswerPlain?: string // Plain text of the correct answer for comparison
  requireFormatting?: boolean // Require formatting to match the model answer
}

// ============================================================================
// Matching Entry
// ============================================================================

export interface MatchingPair {
  id: string
  left: string
  right: string
}

export interface MatchingEntry extends QuizEntryBase {
  type: QuizEntryType.Matching
  pairs: MatchingPair[]
  distractors?: string[] // Extra items on the right side
  allowPartialCredit?: boolean
}

// ============================================================================
// Ordering Entry
// ============================================================================

export interface OrderingItem {
  id: string
  text: string
  correctPosition: number // 0-indexed position
}

export interface OrderingEntry extends QuizEntryBase {
  type: QuizEntryType.Ordering
  items: OrderingItem[]
  allowPartialCredit?: boolean
}

// ============================================================================
// Categorization Entry
// ============================================================================

export interface Category {
  id: string
  name: string
  description?: string
}

export interface CategorizationItem {
  id: string
  text: string
  correctCategoryIds: string[] // Can belong to multiple categories
}

export interface CategorizationEntry extends QuizEntryBase {
  type: QuizEntryType.Categorization
  categories: Category[]
  items: CategorizationItem[]
}

// ============================================================================
// Rating Entry
// ============================================================================

export interface RatingScale {
  min: number
  max: number
  step: number
  minLabel?: string
  maxLabel?: string
}

export interface RatingEntry extends QuizEntryBase {
  type: QuizEntryType.Rating
  scale: RatingScale
  correctRating?: number // If undefined, any rating is accepted
}

// ============================================================================
// Numeric Entry (compute numeric result from a formula)
// ============================================================================

export interface FormulaVariable {
  id: string
  name: string // e.g. "x", "y", "r"
  min: number
  max: number
  decimals: number // decimal places for generated values
}

export interface NumericEntry extends QuizEntryBase {
  type: QuizEntryType.Numeric
  variables: FormulaVariable[]
  formula: string // math expression using variable names, e.g. "x^2 + 2*y"
  toleranceType: "absolute" | "percentage"
  tolerance: number // margin of error
  decimalPlaces: number // answer decimal places
}

// ============================================================================
// Formula Entry (discover the formula from variables and expected result)
// ============================================================================

export interface FormulaEntry extends QuizEntryBase {
  type: QuizEntryType.Formula
  variables: FormulaVariable[]
  formula: string // the correct formula (hidden from student)
  toleranceType: "absolute" | "percentage"
  tolerance: number // margin of error
  decimalPlaces: number // answer decimal places
}

// ============================================================================
// Hotspot Entry (click on image to identify a point/area)
// ============================================================================

export interface HotspotZone {
  radius: number // percentage of image width (0-50)
  label: string  // e.g. "Exact", "Close", "Near"
}

export interface HotspotPoint {
  id: string
  x: number // 0-100, percentage of image width
  y: number // 0-100, percentage of image height
  zones: HotspotZone[] // from innermost to outermost
}

export interface HotspotEntry extends QuizEntryBase {
  type: QuizEntryType.Hotspot
  imageUrl: string       // base64 data URL or external URL
  imageWidth: number     // natural width in pixels
  imageHeight: number    // natural height in pixels
  hotspots: HotspotPoint[]
}

// ============================================================================
// Highlight Entry (select/highlight correct spans in a text)
// ============================================================================

export interface HighlightSpan {
  start: number // character offset in plain text
  end: number   // character offset in plain text (exclusive)
}

export interface HighlightEntry extends QuizEntryBase {
  type: QuizEntryType.Highlight
  /** Raw text with __marked__ syntax for correct highlights */
  sourceText: string
  /** Plain text (markers stripped) shown to student */
  plainText: string
  /** Correct highlight spans (character offsets in plainText) */
  highlights: HighlightSpan[]
}

// ============================================================================
// Union Type
// ============================================================================

export type QuizEntry =
  | SingleChoiceEntry
  | MultipleChoiceEntry
  | TrueFalseEntry
  | FillInTheBlankEntry
  | ShortAnswerEntry
  | EssayEntry
  | MatchingEntry
  | OrderingEntry
  | CategorizationEntry
  | RatingEntry
  | NumericEntry
  | FormulaEntry
  | HotspotEntry
  | HighlightEntry

// ============================================================================
// Type Guards
// ============================================================================

export function isSingleChoice(entry: QuizEntry): entry is SingleChoiceEntry {
  return entry.type === QuizEntryType.SingleChoice
}

export function isMultipleChoice(entry: QuizEntry): entry is MultipleChoiceEntry {
  return entry.type === QuizEntryType.MultipleChoice
}

export function isTrueFalse(entry: QuizEntry): entry is TrueFalseEntry {
  return entry.type === QuizEntryType.TrueFalse
}

export function isFillInTheBlank(entry: QuizEntry): entry is FillInTheBlankEntry {
  return entry.type === QuizEntryType.FillInTheBlank
}

export function isShortAnswer(entry: QuizEntry): entry is ShortAnswerEntry {
  return entry.type === QuizEntryType.ShortAnswer
}

export function isEssay(entry: QuizEntry): entry is EssayEntry {
  return entry.type === QuizEntryType.Essay
}

export function isMatching(entry: QuizEntry): entry is MatchingEntry {
  return entry.type === QuizEntryType.Matching
}

export function isOrdering(entry: QuizEntry): entry is OrderingEntry {
  return entry.type === QuizEntryType.Ordering
}

export function isCategorization(entry: QuizEntry): entry is CategorizationEntry {
  return entry.type === QuizEntryType.Categorization
}

export function isRating(entry: QuizEntry): entry is RatingEntry {
  return entry.type === QuizEntryType.Rating
}

export function isNumeric(entry: QuizEntry): entry is NumericEntry {
  return entry.type === QuizEntryType.Numeric
}

export function isFormula(entry: QuizEntry): entry is FormulaEntry {
  return entry.type === QuizEntryType.Formula
}

export function isHotspot(entry: QuizEntry): entry is HotspotEntry {
  return entry.type === QuizEntryType.Hotspot
}

export function isHighlight(entry: QuizEntry): entry is HighlightEntry {
  return entry.type === QuizEntryType.Highlight
}

// ============================================================================
// Factory Functions
// ============================================================================

export function createDefaultSettings(): QuizSettings {
  return {
    allowRetry: true,
    showFeedback: true,
    showCorrectAnswer: true,
  }
}

export function createSingleChoiceEntry(stem = ""): SingleChoiceEntry {
  return {
    type: QuizEntryType.SingleChoice,
    stem,
    options: [
      { id: "1", text: "Paris" },
      { id: "2", text: "London" },
      { id: "3", text: "Berlin" },
      { id: "4", text: "Madrid" },
    ],
    correctOptionId: "1",
    settings: createDefaultSettings(),
  }
}

export function createMultipleChoiceEntry(stem = ""): MultipleChoiceEntry {
  return {
    type: QuizEntryType.MultipleChoice,
    stem,
    options: [
      { id: "1", text: "2" },
      { id: "2", text: "3" },
      { id: "3", text: "4" },
      { id: "4", text: "7" },
    ],
    correctOptionIds: ["1", "2", "4"],
    settings: createDefaultSettings(),
  }
}

export function createTrueFalseEntry(stem = ""): TrueFalseEntry {
  return {
    type: QuizEntryType.TrueFalse,
    stem,
    correctAnswer: true,
    settings: createDefaultSettings(),
  }
}

export function createFillInTheBlankEntry(stem = ""): FillInTheBlankEntry {
  // Pre-parse blanks from stem so the form starts with correct data
  const parsedBlanks = stem.match(/___|\b_[^_]+_\b/g) || []
  const blanks: FillBlankField[] = parsedBlanks.map((match, i) => {
    const extractedAnswer = match === "___" ? null : match.slice(1, -1)
    const acceptedAnswers = extractedAnswer ? [extractedAnswer] : [""]
    return {
      id: Math.random().toString(36).substring(7),
      position: i,
      input: { type: FillBlankInputType.Text, acceptedAnswers } as FillBlankTextInput,
    }
  })
  return {
    type: QuizEntryType.FillInTheBlank,
    stem,
    blanks,
    settings: createDefaultSettings(),
  }
}

export function createShortAnswerEntry(stem = ""): ShortAnswerEntry {
  return {
    type: QuizEntryType.ShortAnswer,
    stem,
    acceptedAnswers: ["Tokyo"],
    settings: createDefaultSettings(),
  }
}

export function createEssayEntry(stem = ""): EssayEntry {
  return {
    type: QuizEntryType.Essay,
    stem,
    settings: createDefaultSettings(),
  }
}

export function createMatchingEntry(stem = ""): MatchingEntry {
  return {
    type: QuizEntryType.Matching,
    stem,
    pairs: [
      { id: "1", left: "France", right: "Paris" },
      { id: "2", left: "Japan", right: "Tokyo" },
      { id: "3", left: "Brazil", right: "Brasília" },
    ],
    settings: createDefaultSettings(),
  }
}

export function createOrderingEntry(stem = ""): OrderingEntry {
  return {
    type: QuizEntryType.Ordering,
    stem,
    items: [
      { id: "1", text: "World War I", correctPosition: 0 },
      { id: "2", text: "World War II", correctPosition: 1 },
      { id: "3", text: "Moon Landing", correctPosition: 2 },
    ],
    settings: createDefaultSettings(),
  }
}

export function createCategorizationEntry(stem = ""): CategorizationEntry {
  return {
    type: QuizEntryType.Categorization,
    stem,
    categories: [
      { id: "1", name: "Fruits" },
      { id: "2", name: "Vegetables" },
    ],
    items: [
      { id: "1", text: "Apple", correctCategoryIds: ["1"] },
      { id: "2", text: "Carrot", correctCategoryIds: ["2"] },
      { id: "3", text: "Banana", correctCategoryIds: ["1"] },
    ],
    settings: createDefaultSettings(),
  }
}

export function createRatingEntry(stem = ""): RatingEntry {
  return {
    type: QuizEntryType.Rating,
    stem,
    scale: { min: 1, max: 5, step: 1 },
    settings: createDefaultSettings(),
  }
}

export function createNumericEntry(stem = ""): NumericEntry {
  return {
    type: QuizEntryType.Numeric,
    stem,
    variables: [
      { id: "1", name: "x", min: 1, max: 10, decimals: 0 },
      { id: "2", name: "y", min: 1, max: 10, decimals: 0 },
    ],
    formula: "x^2 + y",
    toleranceType: "absolute",
    tolerance: 0,
    decimalPlaces: 2,
    settings: createDefaultSettings(),
  }
}

export function createFormulaEntry(stem = ""): FormulaEntry {
  return {
    type: QuizEntryType.Formula,
    stem,
    variables: [
      { id: "1", name: "x", min: 1, max: 10, decimals: 0 },
      { id: "2", name: "y", min: 1, max: 10, decimals: 0 },
    ],
    formula: "x^2 + y",
    toleranceType: "absolute",
    tolerance: 0,
    decimalPlaces: 2,
    settings: createDefaultSettings(),
  }
}

export function createHotspotEntry(stem = ""): HotspotEntry {
  return {
    type: QuizEntryType.Hotspot,
    stem,
    imageUrl: "",
    imageWidth: 0,
    imageHeight: 0,
    hotspots: [],
    settings: createDefaultSettings(),
  }
}

/**
 * Parse __marked__ text into plainText + highlight spans.
 */
export function parseHighlightSource(source: string): { plainText: string; highlights: HighlightSpan[] } {
  const highlights: HighlightSpan[] = []
  let plain = ""
  let i = 0
  while (i < source.length) {
    if (source[i] === "_" && source[i + 1] === "_") {
      const close = source.indexOf("__", i + 2)
      if (close !== -1) {
        const word = source.substring(i + 2, close)
        highlights.push({ start: plain.length, end: plain.length + word.length })
        plain += word
        i = close + 2
        continue
      }
    }
    plain += source[i]
    i++
  }
  return { plainText: plain, highlights }
}

export function createHighlightEntry(stem = ""): HighlightEntry {
  const defaultSource = "The __mitochondria__ is the powerhouse of the __cell__."
  const { plainText, highlights } = parseHighlightSource(defaultSource)
  return {
    type: QuizEntryType.Highlight,
    stem,
    sourceText: defaultSource,
    plainText,
    highlights,
    settings: createDefaultSettings(),
  }
}

// ============================================================================
// Utility Types for Components
// ============================================================================

export interface QuizAnswerState {
  selectedOptionIds: string[] // For choice-based questions
  textAnswers: Record<string, string> // For text input (key = blank position or "main")
  categorizations: Record<string, string[]> // itemId -> categoryIds
  ordering: string[] // Ordered item IDs
  rating?: number
}

export function createEmptyAnswerState(): QuizAnswerState {
  return {
    selectedOptionIds: [],
    textAnswers: {},
    categorizations: {},
    ordering: [],
  }
}
