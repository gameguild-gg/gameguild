import type {
  QuizBlockLike,
  QuizBlockStorageLike,
  QuizQuestionLike,
  QuizQuestionType,
} from './types';

export function toQuizBlocks(payload: readonly QuizBlockLike[] | QuizBlockStorageLike): QuizBlockLike[] {
  if (Array.isArray(payload)) return [...payload];
  if (!isBlockStorage(payload)) return [];
  return payload.order
    .map(([id, type]) => ({ id, type, data: payload.blocks[id] }))
    .filter((block) => block.data !== undefined);
}

export function isBlockStorage(
  value: unknown,
): value is Required<Pick<QuizBlockStorageLike, 'order' | 'blocks'>> & QuizBlockStorageLike {
  const candidate = asRecord(value);
  return Boolean(
    candidate &&
      Array.isArray(candidate.order) &&
      candidate.blocks &&
      typeof candidate.blocks === 'object' &&
      !Array.isArray(candidate.blocks),
  );
}

export function getQuizQuestionType(value: unknown): QuizQuestionType | null {
  const rawType = typeof value === 'string' ? value : asRecord(value)?.type;
  return isQuizQuestionType(rawType) ? rawType : null;
}

export function isQuizQuestionType(value: unknown): value is QuizQuestionType {
  return typeof value === 'string' && [
    'SINGLE_CHOICE',
    'MULTIPLE_CHOICE',
    'TRUE_FALSE',
    'FILL_IN_THE_BLANK',
    'SHORT_ANSWER',
    'ESSAY',
    'MATCHING',
    'ORDERING',
    'CATEGORIZATION',
    'RATING',
    'NUMERIC',
    'FORMULA',
    'HOTSPOT',
    'HIGHLIGHT',
  ].includes(value);
}

export function hasOnlySupportedFillBlankInputs(question: QuizQuestionLike | null): boolean {
  const blanks = asRecordArray(question?.blanks);
  return blanks.length > 0 && blanks.every((blank) => {
    const input = asRecord(blank.input);
    return ['TEXT', 'NUMBER', 'DROPDOWN', 'WORDBANK'].includes(String(input?.type ?? ''));
  });
}

export function normalizeQuestionPoints(points: unknown): number {
  return Number.isFinite(points) && Number(points) > 0 ? Number(points) : 1;
}

export function asQuizQuestion(value: unknown): QuizQuestionLike | null {
  return asRecord(value) as QuizQuestionLike | null;
}

export function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === 'object' && !Array.isArray(value) ? value as Record<string, unknown> : null;
}

export function asRecordArray(value: unknown): Array<Record<string, unknown>> {
  return Array.isArray(value) ? value.filter((item): item is Record<string, unknown> => Boolean(asRecord(item))) : [];
}

export function asStringArray(value: unknown): string[] {
  return Array.isArray(value) ? value.map((item) => String(item)) : [];
}

export function cloneValue<T>(value: T): T {
  if (value == null || typeof value !== 'object') return value;
  return JSON.parse(JSON.stringify(value)) as T;
}

export function pickQuestionFields(source: Record<string, unknown>, fields: readonly string[]): Record<string, unknown> {
  const picked: Record<string, unknown> = {};
  for (const field of fields) {
    if (source[field] !== undefined) picked[field] = cloneValue(source[field]);
  }
  return picked;
}

export function withDefinedFields(
  base: Record<string, unknown>,
  fields: Record<string, unknown>,
): Record<string, unknown> {
  const next = { ...base };
  for (const [key, value] of Object.entries(fields)) {
    if (value !== undefined) next[key] = value;
  }
  return next;
}

export function rotateAnswerKeyFirstValues(values: string[]): string[] {
  const clean = values.filter((value) => value.trim());
  if (clean.length <= 1) return clean;
  return [...clean.slice(1), clean[0]!];
}

export function uniqueStrings(values: string[]): string[] {
  return [...new Set(values.filter((value) => value.trim()))];
}

export function escapeRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
