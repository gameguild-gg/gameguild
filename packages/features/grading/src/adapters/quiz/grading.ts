import { sumGradedItemPoints } from '../../config';
import type {
  GradeItemResult,
  GradeResult,
  GradeSubmissionArgs,
  StructuredAnswer,
} from '../../types';
import type { QuizQuestionLike } from './types';
import { buildQuizStructuredAnswerPayload } from './structured-answer';
import {
  asQuizQuestion,
  asRecordArray,
  asStringArray,
  escapeRegExp,
  normalizeQuestionPoints,
} from './utils';

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
        ? unsupported(maxScore)
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

  const normalizedPayload = buildQuizStructuredAnswerPayload(payload, grading);
  const itemResults = Object.entries(grading.items).map(([itemId, item]) => {
    if (item.gradingKind !== 'deterministic') {
      return {
        contentBlockId: item.contentBlockId,
        status: item.gradingKind === 'manual' ? 'pending' : 'unsupported',
        score: null,
        maxScore: item.points,
      } satisfies GradeItemResult;
    }

    const result = gradeQuizAnswer(answerKey.items[itemId], normalizedPayload.answers[item.contentBlockId] ?? {}, item.points);
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

function gradeFillInTheBlank(question: QuizQuestionLike, answer: StructuredAnswer): boolean {
  const blanks = asRecordArray(question.blanks);
  if (blanks.length === 0) return false;

  return blanks.every((blank) => {
    const blankId = String(blank.id ?? '');
    const rawAnswer = (answer.textAnswers?.[blankId] ?? '').trim();
    if (!rawAnswer) return false;

    const input = blank.input && typeof blank.input === 'object' && !Array.isArray(blank.input)
      ? blank.input as Record<string, unknown>
      : null;
    switch (input?.type) {
      case 'TEXT':
        return matchesAcceptedAnswer(rawAnswer, asStringArray(input.acceptedAnswers), input.caseSensitive === true);
      case 'NUMBER':
        return matchesNumberAnswer(rawAnswer, input);
      case 'DROPDOWN':
        return rawAnswer === fillBlankCorrectValue(input, 'options');
      case 'WORDBANK':
        return (rawAnswer.includes('|') ? rawAnswer.split('|')[0] : rawAnswer) === fillBlankCorrectValue(input, 'words');
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

function fillBlankCorrectValue(input: Record<string, unknown>, fallbackKey: string): string {
  if (typeof input.correctValue === 'string') return input.correctValue;
  return asStringArray(input[fallbackKey])[0] ?? '';
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
