import type {
  FeedbackMode,
  GradingKind,
  GradingResultUse,
  PresentationMode,
  StructuredAnswer,
} from '../../types';

export type QuizQuestionType =
  | 'SINGLE_CHOICE'
  | 'MULTIPLE_CHOICE'
  | 'TRUE_FALSE'
  | 'FILL_IN_THE_BLANK'
  | 'SHORT_ANSWER'
  | 'ESSAY'
  | 'MATCHING'
  | 'ORDERING'
  | 'CATEGORIZATION'
  | 'RATING'
  | 'NUMERIC'
  | 'FORMULA'
  | 'HOTSPOT'
  | 'HIGHLIGHT';

export type QuizGradingSupportStatus = GradingKind;

export interface QuizBlockLike {
  id: string;
  type: string;
  data?: unknown;
}

export interface QuizBlockStorageLike {
  order?: readonly (readonly [string, string])[];
  blocks?: Record<string, unknown>;
  [key: string]: unknown;
}

export interface QuizQuestionLike {
  type?: string;
  points?: number;
  [key: string]: unknown;
}

export interface QuizGradingOptions {
  uses?: readonly GradingResultUse[];
  maxScore?: number;
  passingScore?: number;
  required?: boolean;
  groupId?: string | null;
  weight?: number;
  includeInFinalGrade?: boolean;
  feedbackMode?: FeedbackMode;
  presentationMode?: PresentationMode;
}

export interface QuizAnswerKeyInventoryItem {
  type: QuizQuestionType;
  learnerSafeFields: readonly string[];
  answerKeyFields: readonly string[];
  structuredAnswerFields: readonly (keyof StructuredAnswer)[];
  gradingSupport: QuizGradingSupportStatus;
  notes?: string;
}
