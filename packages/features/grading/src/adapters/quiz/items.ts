import {
  normalizeGradingDefinition,
  sumGradedItemPoints,
  validateGradingDefinition,
} from '../../config';
import type {
  ContentGradingDefinition,
  GradedItemConfig,
  GradingKind,
} from '../../types';
import type { QuizBlockLike, QuizGradingOptions } from './types';
import {
  asQuizQuestion,
  asRecord,
  asRecordArray,
  asStringArray,
  getQuizQuestionType,
  normalizeQuestionPoints,
} from './utils';

export function buildQuizGradingItemsFromBlocks(blocks: readonly QuizBlockLike[]): Record<string, GradedItemConfig> {
  const items: Record<string, GradedItemConfig> = {};

  for (const block of blocks) {
    if (block.type !== 'quiz') continue;
    const question = asQuizQuestion(block.data);
    const points = normalizeQuestionPoints(question?.points);
    items[block.id] = {
      contentBlockId: block.id,
      points,
      gradingKind: getQuizQuestionGradingKind(question),
    };
  }

  return items;
}

export function createQuizGradingDefinition(
  blocks: readonly QuizBlockLike[],
  options: QuizGradingOptions = {},
): ContentGradingDefinition {
  const items = buildQuizGradingItemsFromBlocks(blocks);
  const itemTotal = Object.values(items).reduce((sum, item) => sum + item.points, 0);
  const maxScore = options.maxScore ?? itemTotal;

  return validateGradingDefinition({
    enabled: true,
    schemaVersion: 1,
    score: {
      maxScore: Math.max(1, maxScore),
      passingScore: options.passingScore,
    },
    attempts: {},
    feedback: {
      mode: options.feedbackMode ?? 'immediate',
    },
    presentation: {
      mode: options.presentationMode ?? 'continuous',
    },
    items,
  });
}

export function syncQuizGradingDefinition(
  blocks: readonly QuizBlockLike[],
  definition: ContentGradingDefinition,
): ContentGradingDefinition {
  if (!definition.enabled) return normalizeGradingDefinition(definition);
  const items = buildQuizGradingItemsFromBlocks(blocks);
  const next = normalizeGradingDefinition({ ...definition, items });
  if (next.score.maxScore <= 0) {
    const itemTotal = sumGradedItemPoints(next);
    next.score.maxScore = Math.max(1, itemTotal);
  }
  return next;
}

export function isDeterministicQuizQuestionType(value: unknown): boolean {
  return getQuizQuestionGradingKind(value) === 'deterministic';
}

export function getQuizQuestionGradingKind(value: unknown): GradingKind {
  const question = asQuizQuestion(value);
  const type = getQuizQuestionType(value);

  // Classification is intentionally conservative: a question is deterministic
  // only when its server-owned answer key is complete enough to grade.
  switch (type) {
    case 'SINGLE_CHOICE':
      return hasSingleChoiceAnswerKey(question) ? 'deterministic' : 'unsupported';

    case 'MULTIPLE_CHOICE':
      return hasMultipleChoiceAnswerKey(question) ? 'deterministic' : 'unsupported';

    case 'TRUE_FALSE':
      return question && typeof question.correctAnswer === 'boolean' ? 'deterministic' : 'unsupported';

    case 'SHORT_ANSWER':
      return hasNonEmptyStringArray(question?.acceptedAnswers) ? 'deterministic' : 'unsupported';

    case 'MATCHING':
      return hasMatchingAnswerKey(question) ? 'deterministic' : 'unsupported';

    case 'ORDERING':
      return hasOrderingAnswerKey(question) ? 'deterministic' : 'unsupported';

    case 'CATEGORIZATION':
      return hasCategorizationAnswerKey(question) ? 'deterministic' : 'unsupported';

    case 'HOTSPOT':
      return hasHotspotAnswerKey(question) ? 'deterministic' : 'unsupported';

    case 'HIGHLIGHT':
      return hasHighlightAnswerKey(question) ? 'deterministic' : 'unsupported';

    case 'FILL_IN_THE_BLANK':
      return hasFillBlankAnswerKey(question) ? 'deterministic' : 'unsupported';

    case 'ESSAY':
      // Essay submissions enter the trusted path, but wait for manual grading.
      return 'manual';

    case 'RATING':
      return hasRatingAnswerKey(question) ? 'deterministic' : 'unsupported';

    case 'NUMERIC':
    case 'FORMULA':
      // Numeric/formula need dedicated server evaluators before they can produce
      // official deterministic scores.
    default:
      return 'unsupported';
  }
}

function hasSingleChoiceAnswerKey(question: unknown): boolean {
  const value = asQuizQuestion(question);
  if (!value || !isNonEmptyString(value.correctOptionId)) return false;
  const optionIds = getOptionIds(value.options);
  return optionIds.size === 0 || optionIds.has(value.correctOptionId);
}

function hasMultipleChoiceAnswerKey(question: unknown): boolean {
  const value = asQuizQuestion(question);
  const correctOptionIds = asStringArray(value?.correctOptionIds).filter(isNonEmptyString);
  if (!value || correctOptionIds.length === 0) return false;
  const optionIds = getOptionIds(value.options);
  return optionIds.size === 0 || correctOptionIds.every((optionId) => optionIds.has(optionId));
}

function hasFillBlankAnswerKey(question: unknown): boolean {
  const blanks = asRecordArray(asQuizQuestion(question)?.blanks);
  return blanks.length > 0 && blanks.every((blank) => {
    const input = asRecord(blank.input);
    switch (input?.type) {
      case 'TEXT':
        return hasNonEmptyStringArray(input.acceptedAnswers);
      case 'NUMBER':
        return Number.isFinite(input.correctValue);
      case 'DROPDOWN':
        return isNonEmptyString(asStringArray(input.options)[0]);
      case 'WORDBANK':
        return isNonEmptyString(asStringArray(input.words)[0]);
      default:
        return false;
    }
  });
}

function hasMatchingAnswerKey(question: unknown): boolean {
  const pairs = asRecordArray(asQuizQuestion(question)?.pairs);
  return pairs.length > 0 && pairs.every((pair) => (
    isNonEmptyString(pair.id) &&
    isNonEmptyString(pair.left) &&
    isNonEmptyString(pair.right)
  ));
}

function hasOrderingAnswerKey(question: unknown): boolean {
  const items = asRecordArray(asQuizQuestion(question)?.items);
  if (items.length === 0) return false;
  const positions = new Set<number>();
  for (const item of items) {
    if (!isNonEmptyString(item.id) || !Number.isInteger(item.correctPosition)) return false;
    const position = Number(item.correctPosition);
    if (position < 0 || position >= items.length) return false;
    positions.add(position);
  }
  return positions.size === items.length;
}

function hasCategorizationAnswerKey(question: unknown): boolean {
  const value = asQuizQuestion(question);
  const categoryIds = new Set(asRecordArray(value?.categories).map((category) => category.id).filter(isNonEmptyString));
  const items = asRecordArray(value?.items);
  return categoryIds.size > 0 && items.length > 0 && items.every((item) => {
    const correctCategoryIds = asStringArray(item.correctCategoryIds).filter(isNonEmptyString);
    return correctCategoryIds.length > 0 && correctCategoryIds.every((categoryId) => categoryIds.has(categoryId));
  });
}

function hasRatingAnswerKey(question: unknown): boolean {
  const value = asQuizQuestion(question);
  if (!value || !Number.isFinite(value.correctRating)) return false;
  const scale = asRecord(value.scale);
  if (!scale || !Number.isFinite(scale.min) || !Number.isFinite(scale.max)) return true;
  return Number(value.correctRating) >= Number(scale.min) && Number(value.correctRating) <= Number(scale.max);
}

function hasHotspotAnswerKey(question: unknown): boolean {
  const value = asQuizQuestion(question);
  const hotspots = asRecordArray(value?.hotspots);
  return Boolean(
    value &&
      isPositiveNumber(value.imageWidth) &&
      isPositiveNumber(value.imageHeight) &&
      hotspots.length > 0 &&
      hotspots.every((hotspot) => (
        isFiniteInRange(hotspot.x, 0, 100) &&
        isFiniteInRange(hotspot.y, 0, 100) &&
        asRecordArray(hotspot.zones).some((zone) => isPositiveNumber(zone.radius))
      )),
  );
}

function hasHighlightAnswerKey(question: unknown): boolean {
  const value = asQuizQuestion(question);
  const plainText = typeof value?.plainText === 'string' ? value.plainText : '';
  const highlights = asRecordArray(value?.highlights);
  return plainText.trim().length > 0 && highlights.length > 0 && highlights.every((highlight) => (
    Number.isInteger(highlight.start) &&
    Number.isInteger(highlight.end) &&
    Number(highlight.start) >= 0 &&
    Number(highlight.end) > Number(highlight.start) &&
    Number(highlight.end) <= plainText.length
  ));
}

function getOptionIds(value: unknown): Set<string> {
  return new Set(asRecordArray(value).map((option) => option.id).filter(isNonEmptyString));
}

function hasNonEmptyStringArray(value: unknown): boolean {
  return asStringArray(value).some(isNonEmptyString);
}

function isNonEmptyString(value: unknown): value is string {
  return typeof value === 'string' && value.trim().length > 0;
}

function isPositiveNumber(value: unknown): value is number {
  return Number.isFinite(value) && Number(value) > 0;
}

function isFiniteInRange(value: unknown, min: number, max: number): value is number {
  return Number.isFinite(value) && Number(value) >= min && Number(value) <= max;
}
