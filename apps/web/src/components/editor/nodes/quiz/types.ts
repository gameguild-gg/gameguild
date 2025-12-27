/**
 * Quiz Node Type Definitions
 * Clean TypeScript types without Zod - using discriminated unions for type safety
 */

// ============================================================================
// Base Types
// ============================================================================

export type QuestionType =
  | "multiple-choice"
  | "true-false"
  | "fill-blank"
  | "short-answer"
  | "essay"
  | "matching"
  | "ordering"
  | "rating"

// ============================================================================
// Common Interfaces
// ============================================================================

export interface QuizSettings {
  correctFeedback?: string
  incorrectFeedback?: string
  allowRetry: boolean
  backgroundColor?: string
}

// ============================================================================
// Question-Specific Types (Discriminated Union)
// ============================================================================

export interface MultipleChoiceQuestion extends QuizSettings {
  type: "multiple-choice"
  question: string
  answers: Array<{
    id: string
    text: string
    isCorrect: boolean
  }>
}

export interface TrueFalseQuestion extends QuizSettings {
  type: "true-false"
  question: string
  correctAnswer: boolean
}

export interface FillBlankQuestion extends QuizSettings {
  type: "fill-blank"
  question: string
  blanks: Array<{
    id: string
    position: number
    acceptedAnswers: string[]
  }>
}

export interface ShortAnswerQuestion extends QuizSettings {
  type: "short-answer"
  question: string
  acceptedAnswers: string[]
  caseSensitive?: boolean
}

export interface EssayQuestion extends QuizSettings {
  type: "essay"
  question: string
  minWords?: number
  maxWords?: number
}

export interface MatchingQuestion extends QuizSettings {
  type: "matching"
  question: string
  pairs: Array<{
    id: string
    left: string
    right: string
  }>
}

export interface OrderingQuestion extends QuizSettings {
  type: "ordering"
  question: string
  items: Array<{
    id: string
    text: string
    correctOrder: number
  }>
}

export interface RatingQuestion extends QuizSettings {
  type: "rating"
  question: string
  scale: {
    min: number
    max: number
    step: number
  }
  correctRating?: number
}

// ============================================================================
// Union Type for All Question Types
// ============================================================================

export type QuizQuestion =
  | MultipleChoiceQuestion
  | TrueFalseQuestion
  | FillBlankQuestion
  | ShortAnswerQuestion
  | EssayQuestion
  | MatchingQuestion
  | OrderingQuestion
  | RatingQuestion

// ============================================================================
// Type Guards
// ============================================================================

export function isMultipleChoice(quiz: QuizQuestion): quiz is MultipleChoiceQuestion {
  return quiz.type === "multiple-choice"
}

export function isTrueFalse(quiz: QuizQuestion): quiz is TrueFalseQuestion {
  return quiz.type === "true-false"
}

export function isFillBlank(quiz: QuizQuestion): quiz is FillBlankQuestion {
  return quiz.type === "fill-blank"
}

export function isShortAnswer(quiz: QuizQuestion): quiz is ShortAnswerQuestion {
  return quiz.type === "short-answer"
}

export function isEssay(quiz: QuizQuestion): quiz is EssayQuestion {
  return quiz.type === "essay"
}

export function isMatching(quiz: QuizQuestion): quiz is MatchingQuestion {
  return quiz.type === "matching"
}

export function isOrdering(quiz: QuizQuestion): quiz is OrderingQuestion {
  return quiz.type === "ordering"
}

export function isRating(quiz: QuizQuestion): quiz is RatingQuestion {
  return quiz.type === "rating"
}
