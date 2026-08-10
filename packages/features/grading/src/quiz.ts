import {
  normalizeGradingConfig,
  sumGradedItemPoints,
  validateGradingConfig,
} from './config';
import type {
  ContentGradingConfig,
  GradeItemResult,
  GradeResult,
  GradeSubmissionArgs,
  GradedItemConfig,
  GradingValidationMode,
  StructuredAnswer,
  StructuredAnswerPayload,
} from './types';

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

export interface QuizQuestionLike {
  type?: string;
  points?: number;
  [key: string]: unknown;
}

export interface QuizGradingOptions {
  validationMode?: GradingValidationMode;
  maxScore?: number;
  passingScore?: number;
  required?: boolean;
}

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

export function createQuizGradingConfig(
  blocks: readonly QuizBlockLike[],
  options: QuizGradingOptions = {},
): ContentGradingConfig {
  const items = buildQuizGradingItemsFromBlocks(blocks);
  const itemTotal = Object.values(items).reduce((sum, item) => sum + item.points, 0);
  const maxScore = options.maxScore ?? itemTotal;

  return validateGradingConfig({
    enabled: true,
    schemaVersion: 1,
    validationMode: options.validationMode ?? 'public',
    gradebook: {
      maxScore: Math.max(1, maxScore),
      passingScore: options.passingScore,
      required: options.required ?? true,
      official: false,
    },
    policy: {
      feedbackMode: 'immediate',
      presentationMode: 'continuous',
    },
    items,
  });
}

export function syncQuizGradingConfig(
  blocks: readonly QuizBlockLike[],
  config: ContentGradingConfig,
): ContentGradingConfig {
  if (!config.enabled) return normalizeGradingConfig(config);
  const items = buildQuizGradingItemsFromBlocks(blocks);
  const next = normalizeGradingConfig({ ...config, items });
  if (next.gradebook.maxScore <= 0) {
    const itemTotal = sumGradedItemPoints(next);
    next.gradebook.maxScore = Math.max(1, itemTotal);
  }
  return next;
}

export function isDeterministicQuizQuestionType(type: unknown): boolean {
  return type !== 'ESSAY' && type !== undefined;
}

export function gradeQuizAnswer(entry: unknown, answer: StructuredAnswer): GradeItemResult {
  const question = asQuizQuestion(entry);
  const maxScore = normalizeQuestionPoints(question?.points);
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

export function gradeDeterministicSubmission(_args: GradeSubmissionArgs): GradeResult {
  return {
    status: 'unsupported',
    score: null,
    maxScore: 0,
    feedback: 'Server-side deterministic grading is implemented in Part 2.',
  };
}

export function redactLearnerPayload(contentBody: unknown, _grading: ContentGradingConfig): unknown {
  return contentBody;
}

export function buildStructuredSubmissionPayload(
  answers: Record<string, StructuredAnswer>,
  _grading?: ContentGradingConfig,
): StructuredAnswerPayload {
  return {
    answers: Object.fromEntries(
      Object.entries(answers).map(([blockId, answer]) => [blockId, cloneStructuredAnswer(answer)]),
    ),
  };
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

function normalizeQuestionPoints(points: unknown): number {
  return Number.isFinite(points) && Number(points) > 0 ? Number(points) : 1;
}

function escapeRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
