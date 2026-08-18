import {
  CURRENT_GRADING_SCHEMA_VERSION,
  GradingConfigValidationError,
  type AttemptPolicy,
  type ContentGradingDefinition,
  type FeedbackMode,
  type FeedbackPolicy,
  type GradedItemConfig,
  type GradingKind,
  type PresentationMode,
  type PresentationPolicy,
  type ScorePolicy,
} from './types';

const FEEDBACK_MODES = new Set<FeedbackMode>(['immediate', 'after-submit', 'after-close', 'manual']);
const PRESENTATION_MODES = new Set<PresentationMode>(['continuous', 'single-step']);
const GRADING_KINDS = new Set<GradingKind>(['deterministic', 'manual', 'external', 'unsupported']);

export function createDisabledGradingDefinition(): ContentGradingDefinition {
  return {
    enabled: false,
    schemaVersion: CURRENT_GRADING_SCHEMA_VERSION,
    score: {
      maxScore: 0,
    },
    attempts: {},
    feedback: {},
    presentation: {},
    items: {},
  };
}

export function normalizeGradingDefinition(
  definition: Partial<ContentGradingDefinition> | null | undefined,
): ContentGradingDefinition {
  if (!definition || definition.enabled === false) return createDisabledGradingDefinition();

  return {
    enabled: true,
    schemaVersion: normalizePositiveInteger(definition.schemaVersion, CURRENT_GRADING_SCHEMA_VERSION),
    score: normalizeScore(definition.score),
    attempts: normalizeAttempts(definition.attempts),
    feedback: normalizeFeedback(definition.feedback),
    presentation: normalizePresentation(definition.presentation),
    items: normalizeItems(definition.items),
  };
}

export function validateGradingDefinition(definition: unknown): ContentGradingDefinition {
  const partial = asPartialDefinition(definition);
  const normalized = normalizeGradingDefinition(partial);
  const issues = collectDefinitionValidationIssues(normalized);

  if (issues.length > 0) throw new GradingConfigValidationError(issues);
  return normalized;
}

export function tryParseGradingDefinition(definition: unknown): ContentGradingDefinition | null {
  try {
    const normalized = validateGradingDefinition(definition);
    return normalized.enabled ? normalized : null;
  } catch {
    return null;
  }
}

export function sumGradedItemPoints(definition: ContentGradingDefinition): number {
  return Object.values(definition.items).reduce((sum, item) => sum + item.points, 0);
}

function collectDefinitionValidationIssues(definition: ContentGradingDefinition): string[] {
  const issues: string[] = [];

  if (!Number.isInteger(definition.schemaVersion) || definition.schemaVersion < 1) {
    issues.push('schemaVersion must be a positive integer.');
  }

  if (definition.enabled) {
    if (!Number.isFinite(definition.score.maxScore) || definition.score.maxScore <= 0) {
      issues.push('score.maxScore must be greater than zero when grading is enabled.');
    }

    if (
      definition.score.passingScore !== undefined &&
      (!Number.isFinite(definition.score.passingScore) || definition.score.passingScore < 0)
    ) {
      issues.push('score.passingScore must be zero or greater.');
    }

    if (
      definition.score.passingScore !== undefined &&
      definition.score.passingScore > definition.score.maxScore
    ) {
      issues.push('score.passingScore must be less than or equal to score.maxScore.');
    }

    for (const [itemId, item] of Object.entries(definition.items)) {
      if (!item.contentBlockId.trim()) issues.push(`items.${itemId}.contentBlockId is required.`);
      if (!Number.isFinite(item.points) || item.points < 0) issues.push(`items.${itemId}.points must be zero or greater.`);
      if (!GRADING_KINDS.has(item.gradingKind)) issues.push(`items.${itemId}.gradingKind is invalid.`);
    }
  }

  return issues;
}

function normalizeScore(score: ScorePolicy | undefined): ScorePolicy {
  return {
    maxScore: normalizeFiniteNumber(score?.maxScore, 1),
    passingScore: normalizeOptionalFiniteNumber(score?.passingScore),
  };
}

function normalizeAttempts(attempts: AttemptPolicy | undefined): AttemptPolicy {
  const normalized: AttemptPolicy = {};
  if (attempts?.maxAttempts !== undefined) normalized.maxAttempts = normalizeOptionalPositiveInteger(attempts.maxAttempts);
  if (attempts?.timeLimitMinutes !== undefined) normalized.timeLimitMinutes = normalizeOptionalPositiveInteger(attempts.timeLimitMinutes);
  if (attempts?.availableFrom !== undefined) normalized.availableFrom = attempts.availableFrom ?? null;
  if (attempts?.availableUntil !== undefined) normalized.availableUntil = attempts.availableUntil ?? null;
  if (attempts?.dueAt !== undefined) normalized.dueAt = attempts.dueAt ?? null;
  if (attempts?.allowLateSubmissions !== undefined) normalized.allowLateSubmissions = attempts.allowLateSubmissions;
  if (attempts?.lateSubmissionDeadline !== undefined) normalized.lateSubmissionDeadline = attempts.lateSubmissionDeadline ?? null;
  return normalized;
}

function normalizeFeedback(feedback: FeedbackPolicy | undefined): FeedbackPolicy {
  return feedback?.mode && FEEDBACK_MODES.has(feedback.mode)
    ? { mode: feedback.mode }
    : {};
}

function normalizePresentation(presentation: PresentationPolicy | undefined): PresentationPolicy {
  return presentation?.mode && PRESENTATION_MODES.has(presentation.mode)
    ? { mode: presentation.mode }
    : {};
}

function normalizeItems(items: Record<string, GradedItemConfig> | undefined): Record<string, GradedItemConfig> {
  if (!items) return {};
  return Object.fromEntries(
    Object.entries(items).map(([key, item]) => [
      key,
      {
        contentBlockId: String(item.contentBlockId ?? key),
        points: normalizeFiniteNumber(item.points, 0),
        gradingKind: GRADING_KINDS.has(item.gradingKind) ? item.gradingKind : 'manual',
        ...(item.answerKeyRef ? { answerKeyRef: item.answerKeyRef } : {}),
        ...(item.rubricRef ? { rubricRef: item.rubricRef } : {}),
      } satisfies GradedItemConfig,
    ]),
  );
}

function normalizePositiveInteger(value: number | undefined, fallback: number): number {
  return typeof value === 'number' && Number.isInteger(value) && value > 0 ? value : fallback;
}

function normalizeOptionalPositiveInteger(value: number | null | undefined): number | null {
  if (value == null) return null;
  return typeof value === 'number' && Number.isInteger(value) && value > 0 ? value : null;
}

function normalizeFiniteNumber(value: number | undefined, fallback: number): number {
  return typeof value === 'number' && Number.isFinite(value) ? value : fallback;
}

function normalizeOptionalFiniteNumber(value: number | undefined): number | undefined {
  return typeof value === 'number' && Number.isFinite(value) ? value : undefined;
}

function asPartialDefinition(definition: unknown): Partial<ContentGradingDefinition> | null {
  if (!definition || typeof definition !== 'object') return null;
  return definition as Partial<ContentGradingDefinition>;
}
