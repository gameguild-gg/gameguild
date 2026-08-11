import {
  normalizeGradingDefinition,
  sumGradedItemPoints,
  validateGradingDefinition,
} from '../config';
import type {
  AnswerKey,
  ContentGradingDefinition,
  FeedbackMode,
  GradeItemResult,
  GradeResult,
  GradeSubmissionArgs,
  GradedItemConfig,
  GradingResultUse,
  PresentationMode,
  StructuredAnswer,
  StructuredAnswerPayload,
} from '../types';
import type { GradingAdapter } from './types';

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

export const quizGradingAdapter: GradingAdapter<readonly QuizBlockLike[] | QuizBlockStorageLike> = {
  contentType: 'quiz',
  extractItems(payload) {
    return buildQuizGradingItemsFromBlocks(toQuizBlocks(payload));
  },
  extractAnswerKey(payload, grading) {
    return extractQuizAnswerKeyFromBlocks(toQuizBlocks(payload), grading);
  },
  redactLearnerPayload(payload, grading) {
    if (Array.isArray(payload)) return redactQuizBlocks(payload, grading);
    return redactQuizBlockStorage(payload as QuizBlockStorageLike, grading);
  },
  buildStructuredAnswerPayload(input) {
    return buildQuizStructuredAnswerPayload(asAnswerRecord(input));
  },
};

export function buildQuizGradingItemsFromBlocks(blocks: readonly QuizBlockLike[]): Record<string, GradedItemConfig> {
  const items: Record<string, GradedItemConfig> = {};

  for (const block of blocks) {
    if (block.type !== 'quiz') continue;
    const question = asQuizQuestion(block.data);
    const points = normalizeQuestionPoints(question?.points);
    items[block.id] = {
      contentBlockId: block.id,
      points,
      gradingKind: isDeterministicQuizQuestionType(question?.type) ? 'deterministic' : 'manual',
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
  const uses = normalizeQuizResultUses(options.uses);

  return validateGradingDefinition({
    enabled: true,
    schemaVersion: 1,
    outcome: {
      uses,
      gradebook: uses.includes('gradebook')
        ? {
          groupId: options.groupId ?? null,
          weight: options.weight,
          required: options.required ?? true,
          includeInFinalGrade: options.includeInFinalGrade ?? true,
        }
        : null,
    },
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

export function extractQuizAnswerKeyFromBlocks(
  blocks: readonly QuizBlockLike[],
  _grading: ContentGradingDefinition,
): AnswerKey {
  const items: Record<string, unknown> = {};
  for (const block of blocks) {
    if (block.type === 'quiz') items[block.id] = cloneValue(block.data);
  }
  return { items };
}

export function redactQuizBlockStorage(
  contentBody: QuizBlockStorageLike,
  grading: ContentGradingDefinition,
): QuizBlockStorageLike {
  const blocks = isBlockStorage(contentBody)
    ? Object.fromEntries(
      Object.entries(contentBody.blocks).map(([id, data]) => {
        const type = contentBody.order?.find(([blockId]) => blockId === id)?.[1];
        return [id, type === 'quiz' ? redactQuizQuestion(data) : cloneValue(data)];
      }),
    )
    : {};

  return {
    ...contentBody,
    blocks,
    grading: grading.enabled ? validateGradingDefinition(grading) : undefined,
  };
}

export function redactQuizBlocks(
  blocks: readonly QuizBlockLike[],
  _grading: ContentGradingDefinition,
): QuizBlockLike[] {
  return blocks.map((block) => ({
    ...block,
    data: block.type === 'quiz' ? redactQuizQuestion(block.data) : cloneValue(block.data),
  }));
}

export function buildQuizStructuredAnswerPayload(
  answers: Record<string, StructuredAnswer>,
): StructuredAnswerPayload {
  return {
    answers: Object.fromEntries(
      Object.entries(answers).map(([blockId, answer]) => [blockId, cloneStructuredAnswer(answer)]),
    ),
  };
}

export function isDeterministicQuizQuestionType(type: unknown): boolean {
  return type !== 'ESSAY' && type !== undefined;
}

export function gradeQuizAnswer(entry: unknown, answer: StructuredAnswer, points?: number): GradeItemResult {
  const question = asQuizQuestion(entry);
  const maxScore = points ?? normalizeQuestionPoints(question?.points);
  if (!question?.type) return unsupported(maxScore);

  switch (question.type) {
    case 'SINGLE_CHOICE':
      return graded((answer.selectedOptionIds?.[0] ?? null) === question.correctOptionId, maxScore);

    case 'MULTIPLE_CHOICE':
      return graded(sameStringSet(answer.selectedOptionIds ?? [], asStringArray(question.correctOptionIds)), maxScore);

    case 'TRUE_FALSE':
      return graded((answer.selectedOptionIds?.[0] ?? null) === (question.correctAnswer === true ? 'true' : 'false'), maxScore);

    case 'FILL_IN_THE_BLANK':
      return graded(gradeFillInTheBlank(question, answer), maxScore);

    case 'SHORT_ANSWER':
      return graded(matchesAcceptedAnswer(
        answer.textAnswers?.main,
        asStringArray(question.acceptedAnswers),
        question.caseSensitive === true,
      ), maxScore);

    case 'ESSAY':
      return gradeEssay(question, answer, maxScore);

    case 'MATCHING':
      return graded(gradeMatching(question, answer), maxScore);

    case 'ORDERING':
      return graded(sameStringArray(answer.ordering ?? [], correctOrdering(question)), maxScore);

    case 'CATEGORIZATION':
      return graded(gradeCategorization(question, answer), maxScore);

    case 'RATING':
      return question.correctRating === undefined
        ? graded(answer.rating !== undefined, maxScore)
        : graded(answer.rating === question.correctRating, maxScore);

    case 'HOTSPOT':
      return graded(gradeHotspot(question, answer), maxScore);

    case 'HIGHLIGHT':
      return graded(gradeHighlight(question, answer), maxScore);

    case 'NUMERIC':
    case 'FORMULA':
      return unsupported(maxScore);

    default:
      return unsupported(maxScore);
  }
}

export function gradeDeterministicQuizSubmission(args: GradeSubmissionArgs): GradeResult {
  const { grading, payload, answerKey } = args;
  if (!grading.enabled || !answerKey) {
    return {
      status: 'unsupported',
      score: null,
      maxScore: grading.score.maxScore,
    };
  }

  const itemResults = Object.entries(grading.items).map(([itemId, item]) => {
    if (item.gradingKind !== 'deterministic') {
      return {
        contentBlockId: item.contentBlockId,
        status: item.gradingKind === 'manual' ? 'pending' : 'unsupported',
        score: null,
        maxScore: item.points,
      } satisfies GradeItemResult;
    }

    const result = gradeQuizAnswer(answerKey.items[itemId], payload.answers[item.contentBlockId] ?? {}, item.points);
    return {
      ...result,
      contentBlockId: item.contentBlockId,
    };
  });
  const gradedItems = itemResults.filter((item) => item.status === 'graded');
  const pendingItems = itemResults.filter((item) => item.status === 'pending');
  const unsupportedItems = itemResults.filter((item) => item.status === 'unsupported');
  const score = gradedItems.reduce((sum, item) => sum + (item.score ?? 0), 0);
  const maxScore = sumGradedItemPoints(grading);

  if (pendingItems.length > 0) {
    return {
      status: 'pending',
      score: null,
      maxScore,
      items: itemResults,
    };
  }

  if (unsupportedItems.length > 0) {
    return {
      status: 'unsupported',
      score: null,
      maxScore,
      items: itemResults,
    };
  }

  return {
    status: 'graded',
    score,
    maxScore,
    passed: grading.score.passingScore === undefined ? undefined : score >= grading.score.passingScore,
    items: itemResults,
  };
}

function normalizeQuizResultUses(uses: readonly GradingResultUse[] | undefined): GradingResultUse[] {
  const normalized: GradingResultUse[] = [];
  for (const use of uses ?? ['feedback']) {
    if ((use === 'feedback' || use === 'gradebook') && !normalized.includes(use)) normalized.push(use);
  }
  return normalized.length > 0 ? normalized : ['feedback'];
}

function toQuizBlocks(payload: readonly QuizBlockLike[] | QuizBlockStorageLike): QuizBlockLike[] {
  if (Array.isArray(payload)) return [...payload];
  if (!isBlockStorage(payload)) return [];
  return payload.order
    .map(([id, type]) => ({ id, type, data: payload.blocks[id] }))
    .filter((block) => block.data !== undefined);
}

function isBlockStorage(value: unknown): value is Required<Pick<QuizBlockStorageLike, 'order' | 'blocks'>> & QuizBlockStorageLike {
  const candidate = asRecord(value);
  return Boolean(
    candidate &&
      Array.isArray(candidate.order) &&
      candidate.blocks &&
      typeof candidate.blocks === 'object' &&
      !Array.isArray(candidate.blocks),
  );
}

function redactQuizQuestion(value: unknown): unknown {
  const question = asRecord(value);
  if (!question) return cloneValue(value);
  const next: Record<string, unknown> = {};
  for (const [key, field] of Object.entries(question)) {
    if (isAnswerKeyField(key)) continue;
    if (key === 'blanks' && Array.isArray(field)) {
      next[key] = field.map(redactBlankField);
      continue;
    }
    if (key === 'items' && Array.isArray(field)) {
      next[key] = field.map(redactCategorizationOrOrderingItem);
      continue;
    }
    next[key] = cloneValue(field);
  }
  return next;
}

function redactBlankField(value: unknown): unknown {
  const blank = asRecord(value);
  const input = asRecord(blank?.input);
  if (!blank || !input) return cloneValue(value);
  const redactedInput = Object.fromEntries(
    Object.entries(input).filter(([key]) => !isAnswerKeyField(key)),
  );
  return {
    ...blank,
    input: redactedInput,
  };
}

function redactCategorizationOrOrderingItem(value: unknown): unknown {
  const item = asRecord(value);
  if (!item) return cloneValue(value);
  return Object.fromEntries(
    Object.entries(item).filter(([key]) => !isAnswerKeyField(key)),
  );
}

function isAnswerKeyField(key: string): boolean {
  return [
    'acceptedAnswers',
    'caseSensitive',
    'correctAnswer',
    'correctAnswerPlain',
    'correctCategoryIds',
    'correctOptionId',
    'correctOptionIds',
    'correctPosition',
    'correctRating',
    'correctValue',
    'formula',
    'highlights',
    'tolerance',
    'toleranceType',
  ].includes(key);
}

function cloneStructuredAnswer(answer: StructuredAnswer): StructuredAnswer {
  return {
    selectedOptionIds: answer.selectedOptionIds ? [...answer.selectedOptionIds] : undefined,
    textAnswers: answer.textAnswers ? { ...answer.textAnswers } : undefined,
    categorizations: answer.categorizations
      ? Object.fromEntries(Object.entries(answer.categorizations).map(([key, values]) => [key, [...values]]))
      : undefined,
    ordering: answer.ordering ? [...answer.ordering] : undefined,
    rating: answer.rating,
  };
}

function gradeFillInTheBlank(question: QuizQuestionLike, answer: StructuredAnswer): boolean {
  const blanks = asRecordArray(question.blanks);
  if (blanks.length === 0) return false;

  return blanks.every((blank) => {
    const blankId = String(blank.id ?? '');
    const rawAnswer = (answer.textAnswers?.[blankId] ?? '').trim();
    if (!rawAnswer) return false;

    const input = asRecord(blank.input);
    switch (input?.type) {
      case 'TEXT':
        return matchesAcceptedAnswer(rawAnswer, asStringArray(input.acceptedAnswers), input.caseSensitive === true);
      case 'NUMBER':
        return matchesNumberAnswer(rawAnswer, input);
      case 'DROPDOWN':
        return rawAnswer === asStringArray(input.options)[0];
      case 'WORDBANK':
        return (rawAnswer.includes('|') ? rawAnswer.split('|')[0] : rawAnswer) === asStringArray(input.words)[0];
      default:
        return false;
    }
  });
}

function gradeEssay(question: QuizQuestionLike, answer: StructuredAnswer, maxScore: number): GradeItemResult {
  const expectedPlain = typeof question.correctAnswerPlain === 'string' ? question.correctAnswerPlain.trim() : '';
  if (!expectedPlain) {
    return {
      contentBlockId: '',
      status: 'pending',
      score: null,
      maxScore,
    };
  }

  if (question.requireFormatting === true) return unsupported(maxScore);
  return graded((answer.textAnswers?.main_plain ?? '').trim().toLowerCase() === expectedPlain.toLowerCase(), maxScore);
}

function gradeMatching(question: QuizQuestionLike, answer: StructuredAnswer): boolean {
  const pairs = asRecordArray(question.pairs);
  const assignments = new Map<string, string>();
  for (const selected of answer.selectedOptionIds ?? []) {
    const separator = selected.indexOf(':');
    if (separator > 0) assignments.set(selected.slice(0, separator), selected.slice(separator + 1));
  }

  return pairs.length > 0 &&
    assignments.size === pairs.length &&
    pairs.every((pair) => assignments.get(String(pair.id)) === pair.right);
}

function correctOrdering(question: QuizQuestionLike): string[] {
  return asRecordArray(question.items)
    .sort((a, b) => Number(a.correctPosition ?? 0) - Number(b.correctPosition ?? 0))
    .map((item) => String(item.id));
}

function gradeCategorization(question: QuizQuestionLike, answer: StructuredAnswer): boolean {
  const items = asRecordArray(question.items);
  return items.length > 0 && items.every((item) => {
    const itemId = String(item.id);
    return sameStringSet(answer.categorizations?.[itemId] ?? [], asStringArray(item.correctCategoryIds));
  });
}

function gradeHotspot(question: QuizQuestionLike, answer: StructuredAnswer): boolean {
  const x = Number.parseFloat(answer.textAnswers?.hotspot_x ?? '');
  const y = Number.parseFloat(answer.textAnswers?.hotspot_y ?? '');
  const imageWidth = Number(question.imageWidth ?? 0);
  if (!Number.isFinite(x) || !Number.isFinite(y) || imageWidth <= 0) return false;

  return asRecordArray(question.hotspots).some((point) => {
    const outerRadius = Math.max(0, ...asRecordArray(point.zones).map((zone) => Number(zone.radius ?? 0)));
    const dx = ((x - Number(point.x ?? 0)) / 100) * imageWidth;
    const dy = ((y - Number(point.y ?? 0)) / 100) * Number(question.imageHeight ?? 0);
    return Math.sqrt(dx * dx + dy * dy) <= (outerRadius / 100) * imageWidth;
  });
}

function gradeHighlight(question: QuizQuestionLike, answer: StructuredAnswer): boolean {
  const correct = asRecordArray(question.highlights);
  let student: Array<{ start: number; end: number }>;
  try {
    student = JSON.parse(answer.textAnswers?.highlight_spans ?? '[]') as Array<{ start: number; end: number }>;
  } catch {
    return false;
  }

  if (student.length === 0 && correct.length > 0) return false;
  return correct.every((expected) => student.some((span) => overlaps(span, expected))) &&
    student.every((span) => correct.some((expected) => overlaps(span, expected)));
}

function matchesNumberAnswer(rawAnswer: string, input: Record<string, unknown>): boolean {
  let numeric = rawAnswer;
  const unit = typeof input.unit === 'string' ? input.unit : '';
  if (unit) {
    numeric = numeric.replace(new RegExp(`\\s*${escapeRegExp(unit)}\\s*$`), '').trim();
    if (input.requireUnit === true && numeric === rawAnswer) return false;
  }

  const value = Number.parseFloat(numeric);
  const expected = Number(input.correctValue);
  if (!Number.isFinite(value) || !Number.isFinite(expected)) return false;
  if (input.allowNegative === false && value < 0) return false;

  if (Number.isInteger(input.requiredPrecision)) {
    const decimals = numeric.includes('.') ? numeric.split('.')[1]?.length ?? 0 : 0;
    if (decimals !== input.requiredPrecision) return false;
  }

  return Math.abs(value - expected) <= Number(input.tolerance ?? 0);
}

function matchesAcceptedAnswer(answer: string | undefined, acceptedAnswers: string[], caseSensitive: boolean): boolean {
  const normalized = (answer ?? '').trim();
  if (!normalized) return false;
  return acceptedAnswers.some((accepted) =>
    caseSensitive ? normalized === accepted : normalized.toLowerCase() === accepted.toLowerCase(),
  );
}

function graded(isCorrect: boolean, maxScore: number): GradeItemResult {
  return {
    contentBlockId: '',
    status: 'graded',
    score: isCorrect ? maxScore : 0,
    maxScore,
    isCorrect,
  };
}

function unsupported(maxScore: number): GradeItemResult {
  return {
    contentBlockId: '',
    status: 'unsupported',
    score: null,
    maxScore,
  };
}

function sameStringSet(left: string[], right: string[]): boolean {
  const leftSet = new Set(left);
  const rightSet = new Set(right);
  return leftSet.size === rightSet.size && [...leftSet].every((value) => rightSet.has(value));
}

function sameStringArray(left: string[], right: string[]): boolean {
  return left.length === right.length && left.every((value, index) => value === right[index]);
}

function overlaps(span: { start: number; end: number }, expected: Record<string, unknown>): boolean {
  return span.start < Number(expected.end ?? 0) && span.end > Number(expected.start ?? 0);
}

function asQuizQuestion(value: unknown): QuizQuestionLike | null {
  return asRecord(value) as QuizQuestionLike | null;
}

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === 'object' && !Array.isArray(value) ? value as Record<string, unknown> : null;
}

function asRecordArray(value: unknown): Array<Record<string, unknown>> {
  return Array.isArray(value) ? value.filter((item): item is Record<string, unknown> => Boolean(asRecord(item))) : [];
}

function asStringArray(value: unknown): string[] {
  return Array.isArray(value) ? value.map((item) => String(item)) : [];
}

function asAnswerRecord(value: unknown): Record<string, StructuredAnswer> {
  const record = asRecord(value);
  return record ? record as Record<string, StructuredAnswer> : {};
}

function normalizeQuestionPoints(points: unknown): number {
  return Number.isFinite(points) && Number(points) > 0 ? Number(points) : 1;
}

function cloneValue<T>(value: T): T {
  if (value == null || typeof value !== 'object') return value;
  return JSON.parse(JSON.stringify(value)) as T;
}

function escapeRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
