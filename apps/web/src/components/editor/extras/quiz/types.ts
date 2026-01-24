/**
 * Quiz Assessment Types
 * Clean, type-safe definitions for quiz components
 * Based on base-quiz.ts with improvements for efficiency and clarity
 */

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
}

export enum FillBlankInputType {
  Text = "TEXT", // Free text input with regex validation
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

// ============================================================================
// Factory Functions
// ============================================================================

export function createDefaultSettings(): QuizSettings {
  return {
    allowRetry: true,
  }
}

export function createSingleChoiceEntry(stem = ""): SingleChoiceEntry {
  return {
    type: QuizEntryType.SingleChoice,
    stem,
    options: [
      { id: "1", text: "" },
      { id: "2", text: "" },
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
      { id: "1", text: "" },
      { id: "2", text: "" },
    ],
    correctOptionIds: ["1"],
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
  return {
    type: QuizEntryType.FillInTheBlank,
    stem,
    blanks: [],
    settings: createDefaultSettings(),
  }
}

export function createShortAnswerEntry(stem = ""): ShortAnswerEntry {
  return {
    type: QuizEntryType.ShortAnswer,
    stem,
    acceptedAnswers: [],
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
    pairs: [],
    settings: createDefaultSettings(),
  }
}

export function createOrderingEntry(stem = ""): OrderingEntry {
  return {
    type: QuizEntryType.Ordering,
    stem,
    items: [],
    settings: createDefaultSettings(),
  }
}

export function createCategorizationEntry(stem = ""): CategorizationEntry {
  return {
    type: QuizEntryType.Categorization,
    stem,
    categories: [],
    items: [],
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
