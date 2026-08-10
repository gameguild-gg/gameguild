import { tryParseGradingConfig, validateGradingConfig } from './config';
import type { ContentGradingConfig } from './types';

export const CONTENT_GRADING_STORAGE_KEY = 'grading';

type ObjectRecord = Record<string, unknown>;
type ContentBodyWithGrading<T extends ObjectRecord> = Omit<T, typeof CONTENT_GRADING_STORAGE_KEY> & {
  [CONTENT_GRADING_STORAGE_KEY]?: ContentGradingConfig;
};

export function readContentGradingConfig(contentBody: unknown): ContentGradingConfig | null {
  const body = parseObject(contentBody);
  if (!body || !(CONTENT_GRADING_STORAGE_KEY in body)) return null;
  return tryParseGradingConfig(body[CONTENT_GRADING_STORAGE_KEY]);
}

export function writeContentGradingConfig<T extends ObjectRecord>(
  contentBody: T,
  grading: ContentGradingConfig | null | undefined,
): ContentBodyWithGrading<T> {
  const next: ContentBodyWithGrading<T> = { ...contentBody };
  if (!grading || !grading.enabled) {
    delete next[CONTENT_GRADING_STORAGE_KEY];
    return next;
  }

  next[CONTENT_GRADING_STORAGE_KEY] = validateGradingConfig(grading);
  return next;
}

export function parseContentBodyObject(contentBody: unknown): ObjectRecord | null {
  return parseObject(contentBody);
}

function parseObject(value: unknown): ObjectRecord | null {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return null;
  return value as ObjectRecord;
}
