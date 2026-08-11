export const CURRENT_GRADING_SCHEMA_VERSION = 1;

export type GradingResultUse = 'feedback' | 'gradebook';

export type FeedbackMode = 'immediate' | 'after-submit' | 'after-close' | 'manual';

export type PresentationMode = 'continuous' | 'single-step';

export type GradingKind = 'deterministic' | 'manual' | 'external';

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
  grading: ContentGradingDefinition;
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
