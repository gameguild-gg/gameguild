import {
  CURRENT_GRADING_SCHEMA_VERSION,
  GradingConfigValidationError,
  type AttemptPolicy,
  type ContentGradingDefinition,
  type FeedbackMode,
  type FeedbackPolicy,
  type GradebookPlacement,
  type GradedItemConfig,
  type GradingKind,
  type GradingOutcomePolicy,
  type GradingResultUse,
  type PresentationMode,
  type PresentationPolicy,
  type ScorePolicy,
} from './types';

const RESULT_USES = new Set<GradingResultUse>(['feedback', 'gradebook']);
const FEEDBACK_MODES = new Set<FeedbackMode>(['immediate', 'after-submit', 'after-close', 'manual']);
const PRESENTATION_MODES = new Set<PresentationMode>(['continuous', 'single-step']);
const GRADING_KINDS = new Set<GradingKind>(['deterministic', 'manual', 'external', 'unsupported']);

export function createDisabledGradingDefinition(): ContentGradingDefinition {
  return {
    enabled: false,
    schemaVersion: CURRENT_GRADING_SCHEMA_VERSION,
    outcome: {
      uses: ['feedback'],
      gradebook: null,
    },
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

  const outcome = normalizeOutcome(definition.outcome);

  return {
    enabled: true,
    schemaVersion: normalizePositiveInteger(definition.schemaVersion, CURRENT_GRADING_SCHEMA_VERSION),
    outcome,
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
  const issues = [
    ...collectInputValidationIssues(partial),
    ...collectDefinitionValidationIssues(normalized),
  ];

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

export function getResultUses(definition: ContentGradingDefinition): readonly GradingResultUse[] {
  return definition.outcome.uses;
}

export function isGradebookBound(definition: ContentGradingDefinition): boolean {
  return definition.enabled && definition.outcome.uses.includes('gradebook');
}

export function sumGradedItemPoints(definition: ContentGradingDefinition): number {
  return Object.values(definition.items).reduce((sum, item) => sum + item.points, 0);
}

function collectInputValidationIssues(definition: Partial<ContentGradingDefinition> | null): string[] {
  const issues: string[] = [];
  const rawUses = asRecord(definition?.outcome)?.uses;

  if (Array.isArray(rawUses)) {
    for (const use of rawUses) {
      if (!RESULT_USES.has(use as GradingResultUse)) {
        issues.push(`outcome.uses contains unsupported result use "${String(use)}".`);
      }
    }
  }

  return issues;
}

function collectDefinitionValidationIssues(definition: ContentGradingDefinition): string[] {
  const issues: string[] = [];

  if (!Number.isInteger(definition.schemaVersion) || definition.schemaVersion < 1) {
    issues.push('schemaVersion must be a positive integer.');
  }

  if (definition.enabled) {
    if (definition.outcome.uses.length === 0) {
      issues.push('outcome.uses must include at least one result use.');
    }

    if (definition.outcome.uses.includes('gradebook') && !definition.outcome.gradebook) {
      issues.push('outcome.gradebook is required when outcome.uses includes gradebook.');
    }

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

    if (
      definition.outcome.gradebook?.weight !== undefined &&
      (!Number.isFinite(definition.outcome.gradebook.weight) ||
        definition.outcome.gradebook.weight < 0 ||
        definition.outcome.gradebook.weight > 100)
    ) {
      issues.push('outcome.gradebook.weight must be between 0 and 100.');
    }

    for (const [itemId, item] of Object.entries(definition.items)) {
      if (!item.contentBlockId.trim()) issues.push(`items.${itemId}.contentBlockId is required.`);
      if (!Number.isFinite(item.points) || item.points < 0) issues.push(`items.${itemId}.points must be zero or greater.`);
      if (!GRADING_KINDS.has(item.gradingKind)) issues.push(`items.${itemId}.gradingKind is invalid.`);
    }
  }

  return issues;
}

function normalizeOutcome(outcome: GradingOutcomePolicy | undefined): GradingOutcomePolicy {
  const rawUses = Array.isArray(outcome?.uses) ? outcome.uses : ['feedback'];
  const uses = uniqueResultUses(rawUses.filter((use): use is GradingResultUse => RESULT_USES.has(use as GradingResultUse)));
  const normalizedUses: GradingResultUse[] = uses.length > 0 ? uses : ['feedback'];
  const gradebook = normalizedUses.includes('gradebook')
    ? normalizeGradebookPlacement(outcome?.gradebook ?? {})
    : null;

  return {
    uses: normalizedUses,
    gradebook,
  };
}

function normalizeGradebookPlacement(gradebook: GradebookPlacement | null | undefined): GradebookPlacement {
  return {
    groupId: gradebook?.groupId ?? null,
    weight: normalizeOptionalFiniteNumber(gradebook?.weight),
    required: gradebook?.required ?? true,
    includeInFinalGrade: gradebook?.includeInFinalGrade ?? true,
  };
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

function uniqueResultUses(uses: readonly GradingResultUse[]): GradingResultUse[] {
  const normalized: GradingResultUse[] = [];
  for (const use of uses) {
    if (!normalized.includes(use)) normalized.push(use);
  }
  return normalized;
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

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === 'object' && !Array.isArray(value) ? value as Record<string, unknown> : null;
}
