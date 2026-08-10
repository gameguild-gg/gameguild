import {
  CURRENT_GRADING_SCHEMA_VERSION,
  GradingConfigValidationError,
  type ContentGradingConfig,
  type FeedbackMode,
  type GradebookConfig,
  type GradedItemConfig,
  type GradingKind,
  type GradingPolicy,
  type GradingValidationMode,
  type PresentationMode,
} from './types';

const VALIDATION_MODES = new Set<GradingValidationMode>(['public', 'protected']);
const FEEDBACK_MODES = new Set<FeedbackMode>(['immediate', 'after-submit', 'after-close', 'manual']);
const PRESENTATION_MODES = new Set<PresentationMode>(['continuous', 'single-step']);
const GRADING_KINDS = new Set<GradingKind>(['deterministic', 'manual', 'external']);

export function createDisabledGradingConfig(): ContentGradingConfig {
  return {
    enabled: false,
    schemaVersion: CURRENT_GRADING_SCHEMA_VERSION,
    validationMode: 'public',
    gradebook: {
      maxScore: 0,
      official: false,
    },
    policy: {},
    items: {},
  };
}

export function normalizeGradingConfig(config: Partial<ContentGradingConfig> | null | undefined): ContentGradingConfig {
  if (!config || config.enabled === false) return createDisabledGradingConfig();

  const gradebook = normalizeGradebook(config.gradebook);
  const validationMode = normalizeValidationMode(config.validationMode);

  return {
    enabled: true,
    schemaVersion: normalizePositiveInteger(config.schemaVersion, CURRENT_GRADING_SCHEMA_VERSION),
    validationMode,
    gradebook: {
      ...gradebook,
      official: validationMode === 'public' ? false : (gradebook.official ?? false),
    },
    policy: normalizePolicy(config.policy),
    items: normalizeItems(config.items),
  };
}

export function validateGradingConfig(config: unknown): ContentGradingConfig {
  const normalized = normalizeGradingConfig(asPartialConfig(config));
  const issues = collectValidationIssues(normalized);
  if (issues.length > 0) throw new GradingConfigValidationError(issues);
  return normalized;
}

export function tryParseGradingConfig(config: unknown): ContentGradingConfig | null {
  try {
    const normalized = validateGradingConfig(config);
    return normalized.enabled ? normalized : null;
  } catch {
    return null;
  }
}

export function isOfficialGrade(config: ContentGradingConfig): boolean {
  return config.enabled && config.validationMode === 'protected' && config.gradebook.official === true;
}

export function assertPublicIsNotOfficial(config: ContentGradingConfig): void {
  if (config.validationMode === 'public' && config.gradebook.official === true) {
    throw new GradingConfigValidationError(['Public validation cannot produce an official grade.']);
  }
}

export function sumGradedItemPoints(config: ContentGradingConfig): number {
  return Object.values(config.items).reduce((sum, item) => sum + item.points, 0);
}

function collectValidationIssues(config: ContentGradingConfig): string[] {
  const issues: string[] = [];

  if (!Number.isInteger(config.schemaVersion) || config.schemaVersion < 1) {
    issues.push('schemaVersion must be a positive integer.');
  }

  if (!VALIDATION_MODES.has(config.validationMode)) {
    issues.push('validationMode must be public or protected.');
  }

  if (config.enabled) {
    if (!Number.isFinite(config.gradebook.maxScore) || config.gradebook.maxScore <= 0) {
      issues.push('gradebook.maxScore must be greater than zero when grading is enabled.');
    }

    if (config.gradebook.passingScore !== undefined &&
      (!Number.isFinite(config.gradebook.passingScore) || config.gradebook.passingScore < 0)) {
      issues.push('gradebook.passingScore must be zero or greater.');
    }

    if (config.gradebook.weight !== undefined &&
      (!Number.isFinite(config.gradebook.weight) || config.gradebook.weight < 0 || config.gradebook.weight > 100)) {
      issues.push('gradebook.weight must be between 0 and 100.');
    }

    for (const [itemId, item] of Object.entries(config.items)) {
      if (!item.contentBlockId.trim()) issues.push(`items.${itemId}.contentBlockId is required.`);
      if (!Number.isFinite(item.points) || item.points < 0) issues.push(`items.${itemId}.points must be zero or greater.`);
      if (!GRADING_KINDS.has(item.gradingKind)) issues.push(`items.${itemId}.gradingKind is invalid.`);
    }
  }

  if (config.validationMode === 'public' && config.gradebook.official === true) {
    issues.push('Public validation cannot produce an official grade.');
  }

  return issues;
}

function normalizeGradebook(gradebook: GradebookConfig | undefined): GradebookConfig {
  return {
    maxScore: normalizeFiniteNumber(gradebook?.maxScore, 1),
    passingScore: normalizeOptionalFiniteNumber(gradebook?.passingScore),
    weight: normalizeOptionalFiniteNumber(gradebook?.weight),
    groupId: gradebook?.groupId ?? null,
    required: gradebook?.required ?? true,
    official: gradebook?.official ?? false,
  };
}

function normalizePolicy(policy: GradingPolicy | undefined): GradingPolicy {
  const normalized: GradingPolicy = {};
  if (policy?.maxAttempts !== undefined) normalized.maxAttempts = normalizeOptionalPositiveInteger(policy.maxAttempts);
  if (policy?.timeLimitMinutes !== undefined) normalized.timeLimitMinutes = normalizeOptionalPositiveInteger(policy.timeLimitMinutes);
  if (policy?.availableFrom !== undefined) normalized.availableFrom = policy.availableFrom ?? null;
  if (policy?.availableUntil !== undefined) normalized.availableUntil = policy.availableUntil ?? null;
  if (policy?.feedbackMode && FEEDBACK_MODES.has(policy.feedbackMode)) normalized.feedbackMode = policy.feedbackMode;
  if (policy?.presentationMode && PRESENTATION_MODES.has(policy.presentationMode)) normalized.presentationMode = policy.presentationMode;
  return normalized;
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

function normalizeValidationMode(value: GradingValidationMode | undefined): GradingValidationMode {
  return value && VALIDATION_MODES.has(value) ? value : 'public';
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

function asPartialConfig(config: unknown): Partial<ContentGradingConfig> | null {
  if (!config || typeof config !== 'object') return null;
  return config as Partial<ContentGradingConfig>;
}
