export const CURRENT_GRADING_SCHEMA_VERSION = 1;

// Result use answers "where should a trusted server result be applied?"
// It is separate from runtime trust: grading-enabled work is always server-graded.
export type GradingResultUse = 'feedback' | 'gradebook';

export type FeedbackMode = 'immediate' | 'after-submit' | 'after-close' | 'manual';

export type PresentationMode = 'continuous' | 'single-step';

export type GradingKind = 'deterministic' | 'manual' | 'external' | 'unsupported';

// Content owns this definition. Assessments and submissions consume it, but do
// not become the authoring source for the activity.
export interface ContentGradingDefinition {
  enabled: boolean;
  schemaVersion: number;
  outcome: GradingOutcomePolicy;
  score: ScorePolicy;
  attempts: AttemptPolicy;
  feedback: FeedbackPolicy;
  presentation: PresentationPolicy;
  items: Record<string, GradedItemConfig>;
}

export interface GradingOutcomePolicy {
  uses: GradingResultUse[];
  gradebook?: GradebookPlacement | null;
}

export interface GradebookPlacement {
  groupId?: string | null;
  weight?: number;
  required?: boolean;
  includeInFinalGrade?: boolean;
}

export interface ScorePolicy {
  maxScore: number;
  passingScore?: number;
}

export interface AttemptPolicy {
  maxAttempts?: number | null;
  timeLimitMinutes?: number | null;
  availableFrom?: string | null;
  availableUntil?: string | null;
  dueAt?: string | null;
  allowLateSubmissions?: boolean;
  lateSubmissionDeadline?: string | null;
}

export interface FeedbackPolicy {
  mode?: FeedbackMode;
}

export interface PresentationPolicy {
  mode?: PresentationMode;
}

export interface GradedItemConfig {
  contentBlockId: string;
  points: number;
  // Describes how this item can be resolved on the server.
  gradingKind: GradingKind;
  answerKeyRef?: string;
  rubricRef?: string;
}

// Learner submissions are normalized into this small answer vocabulary before
// server grading. Answer keys, score fields, and feedback do not belong here.
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
  // Server-owned material keyed by graded item id. Do not send this to learners.
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
  grading: ContentGradingDefinition;
  payload: StructuredAnswerPayload;
  // Required for deterministic server grading; absent keys produce pending or
  // unsupported results instead of trusting client-provided correctness.
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
