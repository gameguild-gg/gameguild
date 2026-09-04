import {
  tryParseContentGradingDefinition,
  validateContentGradingDefinition,
} from "./config";
import type { ContentGradingDefinitionV2 } from "./types";

export const CONTENT_GRADING_STORAGE_KEY = "grading";

type ObjectRecord = Record<string, unknown>;
type ContentBodyWithGrading<T extends ObjectRecord> = Omit<T, typeof CONTENT_GRADING_STORAGE_KEY> & {
  [CONTENT_GRADING_STORAGE_KEY]?: ContentGradingDefinitionV2;
};

export function readContentGradingDefinition(contentBody: unknown): ContentGradingDefinitionV2 | null {
  const body = parseObject(contentBody);
  if (!body || !(CONTENT_GRADING_STORAGE_KEY in body)) return null;
  return tryParseContentGradingDefinition(body[CONTENT_GRADING_STORAGE_KEY]);
}

export function writeContentGradingDefinition<T extends ObjectRecord>(
  contentBody: T,
  grading: ContentGradingDefinitionV2 | null | undefined,
): ContentBodyWithGrading<T> {
  const next: ContentBodyWithGrading<T> = { ...contentBody };
  if (!grading) {
    delete next[CONTENT_GRADING_STORAGE_KEY];
    return next;
  }

  next[CONTENT_GRADING_STORAGE_KEY] = validateContentGradingDefinition(grading);
  return next;
}

export function parseContentBodyObject(contentBody: unknown): ObjectRecord | null {
  return parseObject(contentBody);
}

function parseObject(value: unknown): ObjectRecord | null {
  if (!value || typeof value !== "object" || Array.isArray(value)) return null;
  return value as ObjectRecord;
}
