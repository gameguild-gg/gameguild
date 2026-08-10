export const CURRENT_GRADING_SCHEMA_VERSION = 1;

export type GradingValidationMode = 'public' | 'protected';

export type FeedbackMode = 'immediate' | 'after-submit' | 'after-close' | 'manual';

export type PresentationMode = 'continuous' | 'single-step';

export type GradingKind = 'deterministic' | 'manual' | 'external';

export interface ContentGradingConfig {
  enabled: boolean;
  schemaVersion: number;
  validationMode: GradingValidationMode;
  gradebook: GradebookConfig;
  policy: GradingPolicy;
  items: Record<string, GradedItemConfig>;
}

export interface GradebookConfig {
  maxScore: number;
  passingScore?: number;
  weight?: number;
  groupId?: string | null;
  required?: boolean;
  official?: boolean;
}

export interface GradingPolicy {
  maxAttempts?: number | null;
  timeLimitMinutes?: number | null;
  availableFrom?: string | null;
  availableUntil?: string | null;
  feedbackMode?: FeedbackMode;
  presentationMode?: PresentationMode;
}

export interface GradedItemConfig {
  contentBlockId: string;
  points: number;
  gradingKind: GradingKind;
  answerKeyRef?: string;
  rubricRef?: string;
}

export interface StructuredAnswer {
  selectedOptionIds?: string[];
  textAnswers?: Record<string, string>;
  categorizations?: Record<string, string[]>;
  ordering?: string[];
  rating?: number;
}

export interface StructuredAnswerPayload {
  answers: Record<string, StructuredAnswer>;
}

export interface AnswerKey {
  items: Record<string, unknown>;
}

export interface GradeItemResult {
  contentBlockId: string;
  status: 'graded' | 'pending' | 'unsupported';
  score: number | null;
  maxScore: number;
  isCorrect?: boolean;
  feedback?: string;
}

export interface GradeResult {
  status: 'graded' | 'pending' | 'unsupported';
  score: number | null;
  maxScore: number;
  passed?: boolean;
  items?: GradeItemResult[];
  feedback?: string;
}

export interface GradeSubmissionArgs {
  grading: ContentGradingConfig;
  payload: StructuredAnswerPayload;
  answerKey?: AnswerKey;
  contentBody?: unknown;
}

export class GradingConfigValidationError extends Error {
  readonly issues: string[];

  constructor(issues: string[]) {
    super(issues.join('; '));
    this.name = 'GradingConfigValidationError';
    this.issues = issues;
  }
}
