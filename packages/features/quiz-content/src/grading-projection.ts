import type { QuizGradingItemInputV1 } from "@game-guild/grading-adapter-quiz";
import type { QuizContentDocument } from "./types";

export function quizDocumentToGradingItems(
  document: QuizContentDocument,
): QuizGradingItemInputV1[] {
  return document.order.map(([itemId]) => ({
    itemId,
    entry: document.blocks[itemId]!,
  }));
}
