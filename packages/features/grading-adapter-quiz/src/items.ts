import {
  addScoreValues,
  createContentGradingDefinition,
  parseScoreValue,
  syncContentGradingDefinition,
  type ContentGradingDefinitionV2,
  type ScoreValue,
} from "@game-guild/grading";
import {
  DEFAULT_QUIZ_POINTS,
  QuizEntryType,
  validateQuizAuthoringEntry,
} from "@game-guild/quiz";
import {
  QUIZ_CONTENT_TYPE,
  QUIZ_ITEM_PROJECTION_SCHEMA_VERSION,
  type QuizAnswerKeyV1,
  type QuizGradingItemInputV1,
  type QuizItemProjectionV1,
  type QuizReviewCapabilityV1,
} from "./contracts";

export function createQuizGradingDefinition(
  items: readonly QuizGradingItemInputV1[],
): ContentGradingDefinitionV2 {
  assertQuizGradingItems(items);
  return createContentGradingDefinition(items.map(({ itemId }) => itemId));
}

export function syncQuizGradingDefinition(
  items: readonly QuizGradingItemInputV1[],
  definition: ContentGradingDefinitionV2,
): ContentGradingDefinitionV2 {
  assertQuizGradingItems(items);
  return syncContentGradingDefinition(items.map(({ itemId }) => itemId), definition);
}

export function projectQuizGradingItems(
  items: readonly QuizGradingItemInputV1[],
): QuizItemProjectionV1[] {
  assertQuizGradingItems(items);
  return items.map(({ itemId, entry }) => ({
    schemaVersion: QUIZ_ITEM_PROJECTION_SCHEMA_VERSION,
    itemId,
    itemType: entry.type,
    maxScore: getQuizItemMaxScore(entry),
    source: { contentType: QUIZ_CONTENT_TYPE, itemId },
    authoringEntry: structuredClone(entry),
  }));
}

export function getQuizItemMaxScore(
  entry: QuizGradingItemInputV1["entry"],
): ScoreValue {
  return parseScoreValue(entry.points ?? DEFAULT_QUIZ_POINTS);
}

export function sumQuizItemPoints(items: readonly QuizGradingItemInputV1[]): ScoreValue {
  return addScoreValues(items.map(({ entry }) => getQuizItemMaxScore(entry)));
}

export function classifyQuizReviewCapability(
  entry: QuizGradingItemInputV1["entry"],
): QuizReviewCapabilityV1 {
  if (entry.type === QuizEntryType.Essay) return "instructor-review";
  if (entry.type === QuizEntryType.Numeric || entry.type === QuizEntryType.Formula) {
    return "unsupported";
  }
  if (entry.type === QuizEntryType.Rating && entry.correctRating === undefined) {
    return "unsupported";
  }
  return validateQuizAuthoringEntry(entry).length === 0
    ? "automated-review"
    : "unsupported";
}

export function extractQuizAnswerKey(
  items: readonly QuizGradingItemInputV1[],
): QuizAnswerKeyV1 {
  assertQuizGradingItems(items);
  return {
    schemaVersion: 1,
    entries: Object.fromEntries(items.map(({ itemId, entry }) => [itemId, structuredClone(entry)])),
  };
}

export function assertQuizGradingItems(items: readonly QuizGradingItemInputV1[]): void {
  const seen = new Set<string>();
  for (const { itemId } of items) {
    if (!itemId.trim()) throw new TypeError("Quiz grading item IDs must be non-empty.");
    if (seen.has(itemId)) throw new TypeError(`Duplicate quiz grading item ID: ${itemId}.`);
    seen.add(itemId);
  }
}
