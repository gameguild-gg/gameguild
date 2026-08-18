import { nextBlockId } from "@game-guild/block-list";
import { createSingleChoiceEntry, type QuizEntry } from "@game-guild/quiz";
import type { QuizContentItem } from "./types";

export function nextQuizContentItemId(
  items: readonly QuizContentItem[],
): string {
  return nextBlockId(items);
}

export function createQuizContentItem(
  entry: QuizEntry = createSingleChoiceEntry(""),
  id?: string,
): QuizContentItem {
  const normalizedId = id?.trim();
  return {
    id: normalizedId || "1",
    entry,
  };
}
