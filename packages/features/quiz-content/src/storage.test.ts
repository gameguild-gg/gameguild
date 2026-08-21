import { describe, expect, it } from "vitest";
import { createTrueFalseEntry } from "@game-guild/quiz";
import { nextQuizContentItemId } from "./ids";
import {
  quizContentItemsToStorage,
  quizStorageToContentItems,
} from "./storage";
import type { QuizContentItem } from "./types";

describe("quiz content storage", () => {
  it("round-trips items without changing order or payload identity", () => {
    const items: QuizContentItem[] = [
      { id: "2", entry: createTrueFalseEntry("Second") },
      { id: "1", entry: createTrueFalseEntry("First") },
    ];
    expect(quizStorageToContentItems(quizContentItemsToStorage(items))).toEqual(items);
  });

  it("allocates after the greatest numeric id without requiring numeric ids", () => {
    const items: QuizContentItem[] = [
      { id: "draft", entry: createTrueFalseEntry("Draft") },
      { id: "9", entry: createTrueFalseEntry("Nine") },
    ];
    expect(nextQuizContentItemId(items)).toBe("10");
  });
});
