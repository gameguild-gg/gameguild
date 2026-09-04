import type {
  AssessmentExecutionDeliveryV1,
  AssessmentItemManifestV1,
} from "@game-guild/grading";
import { toQuizLearnerEntry } from "@game-guild/quiz";
import {
  QUIZ_ANSWER_DECODER,
  QUIZ_DELIVERY_GENERATOR,
  QUIZ_PROJECTOR,
  type QuizGradingItemInputV1,
  type QuizLearnerDeliveryItemV1,
} from "./contracts";
import { assertQuizGradingItems } from "./items";

export function createQuizItemManifest(
  items: readonly QuizGradingItemInputV1[],
): AssessmentItemManifestV1[] {
  assertQuizGradingItems(items);
  return items.map(({ itemId, entry }) => ({
    itemId,
    itemType: entry.type,
    projectorKey: QUIZ_PROJECTOR.key,
    projectorVersion: QUIZ_PROJECTOR.version,
    deliveryGeneratorKey: QUIZ_DELIVERY_GENERATOR.key,
    deliveryGeneratorVersion: QUIZ_DELIVERY_GENERATOR.version,
    answerDecoderKey: QUIZ_ANSWER_DECODER.key,
    answerDecoderVersion: QUIZ_ANSWER_DECODER.version,
  }));
}

export function createQuizExecutionDelivery(
  definitionRevisionId: string,
  executionSnapshotHash: string,
  items: readonly QuizGradingItemInputV1[],
  itemOrder: readonly string[] = items.map(({ itemId }) => itemId),
): AssessmentExecutionDeliveryV1<QuizLearnerDeliveryItemV1> {
  assertQuizGradingItems(items);
  assertItemOrder(items, itemOrder);
  const byId = new Map(items.map((item) => [item.itemId, item]));
  return {
    schemaVersion: 1,
    definitionRevisionId,
    executionSnapshotHash,
    itemOrder: [...itemOrder],
    items: Object.fromEntries(itemOrder.map((itemId) => {
      const item = byId.get(itemId)!;
      return [itemId, {
        deliveryGeneratorKey: QUIZ_DELIVERY_GENERATOR.key,
        deliveryGeneratorVersion: QUIZ_DELIVERY_GENERATOR.version,
        learnerPayload: {
          itemId,
          entry: toQuizLearnerEntry(item.entry),
        },
      }];
    })),
  };
}

function assertItemOrder(
  items: readonly QuizGradingItemInputV1[],
  itemOrder: readonly string[],
): void {
  const expected = new Set(items.map(({ itemId }) => itemId));
  const actual = new Set(itemOrder);
  if (actual.size !== itemOrder.length || actual.size !== expected.size) {
    throw new TypeError("Quiz delivery itemOrder must contain every item exactly once.");
  }
  for (const itemId of actual) {
    if (!expected.has(itemId)) throw new TypeError(`Quiz delivery itemOrder contains unknown item ${itemId}.`);
  }
}
