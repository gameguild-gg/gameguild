import type {
  ContentGradingDefinition,
  StructuredAnswer,
  StructuredAnswerPayload,
} from '../../types';
import {
  asRecord,
  asStringArray,
  uniqueStrings,
} from './utils';

export function buildQuizStructuredAnswerPayload(
  input: unknown,
  grading?: ContentGradingDefinition,
): StructuredAnswerPayload {
  const source = extractAnswerRecord(input);
  const blockIds = grading?.enabled
    ? uniqueStrings(Object.values(grading.items).map((item) => item.contentBlockId))
    : Object.keys(source);

  return {
    answers: Object.fromEntries(
      blockIds.map((blockId) => [blockId, normalizeStructuredAnswer(source[blockId])]),
    ),
  };
}

function normalizeStructuredAnswer(value: unknown): StructuredAnswer {
  const answer = asRecord(value);
  if (!answer) return {};

  const normalized: StructuredAnswer = {};
  const selectedOptionIds = uniqueStrings(asStringArray(answer.selectedOptionIds));
  const textAnswers = normalizeTextAnswerRecord(answer.textAnswers);
  const categorizations = normalizeCategorizationAnswerRecord(answer.categorizations);
  const ordering = uniqueStrings(asStringArray(answer.ordering));
  const rating = typeof answer.rating === 'number' && Number.isFinite(answer.rating)
    ? answer.rating
    : undefined;

  if (selectedOptionIds.length > 0) normalized.selectedOptionIds = selectedOptionIds;
  if (Object.keys(textAnswers).length > 0) normalized.textAnswers = textAnswers;
  if (Object.keys(categorizations).length > 0) normalized.categorizations = categorizations;
  if (ordering.length > 0) normalized.ordering = ordering;
  if (rating !== undefined) normalized.rating = rating;

  return normalized;
}

function normalizeTextAnswerRecord(value: unknown): Record<string, string> {
  const record = asRecord(value);
  if (!record) return {};

  const normalized: Record<string, string> = {};
  for (const [key, raw] of Object.entries(record)) {
    if (isUnsafeAnswerPayloadField(key)) continue;
    if (raw == null) continue;
    normalized[key] = String(raw);
  }
  return normalized;
}

function normalizeCategorizationAnswerRecord(value: unknown): Record<string, string[]> {
  const record = asRecord(value);
  if (!record) return {};

  const normalized: Record<string, string[]> = {};
  for (const [key, raw] of Object.entries(record)) {
    if (isUnsafeAnswerPayloadField(key)) continue;
    const values = uniqueStrings(asStringArray(raw));
    if (values.length > 0) normalized[key] = values;
  }
  return normalized;
}

function isUnsafeAnswerPayloadField(key: string): boolean {
  return [
    'acceptedAnswers',
    'answerKey',
    'correctAnswer',
    'correctAnswerPlain',
    'correctCategoryIds',
    'correctness',
    'correctOptionId',
    'correctOptionIds',
    'correctPosition',
    'correctRating',
    'correctValue',
    'feedback',
    'formula',
    'grade',
    'highlights',
    'hotspots',
    'isCorrect',
    'score',
  ].includes(key);
}

function extractAnswerRecord(input: unknown): Record<string, unknown> {
  const record = asRecord(input);
  if (!record) return {};
  const nestedAnswers = asRecord(record.answers);
  return nestedAnswers ?? record;
}
