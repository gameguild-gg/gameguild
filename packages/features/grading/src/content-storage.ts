import { tryParseGradingDefinition, validateGradingDefinition } from './config';
import type { ContentGradingDefinition } from './types';

export const CONTENT_GRADING_STORAGE_KEY = 'grading';

type ObjectRecord = Record<string, unknown>;
type ContentBodyWithGrading<T extends ObjectRecord> = Omit<T, typeof CONTENT_GRADING_STORAGE_KEY> & {
  [CONTENT_GRADING_STORAGE_KEY]?: ContentGradingDefinition;
};

export function readContentGradingDefinition(contentBody: unknown): ContentGradingDefinition | null {
  const body = parseObject(contentBody);
  if (!body || !(CONTENT_GRADING_STORAGE_KEY in body)) return null;
  return tryParseGradingDefinition(body[CONTENT_GRADING_STORAGE_KEY]);
}

export function writeContentGradingDefinition<T extends ObjectRecord>(
  contentBody: T,
  grading: ContentGradingDefinition | null | undefined,
): ContentBodyWithGrading<T> {
  const next: ContentBodyWithGrading<T> = { ...contentBody };
  if (!grading || !grading.enabled) {
    delete next[CONTENT_GRADING_STORAGE_KEY];
    return next;
  }

  next[CONTENT_GRADING_STORAGE_KEY] = validateGradingDefinition(grading);
  return next;
}

export function parseContentBodyObject(contentBody: unknown): ObjectRecord | null {
  return parseObject(contentBody);
}

function parseObject(value: unknown): ObjectRecord | null {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return null;
  return value as ObjectRecord;
}
