import { describe, expect, it } from "vitest";
import { createTrueFalseEntry } from "@game-guild/quiz";
import {
  disableQuizContentGrading,
  enableQuizContentGrading,
  quizContentItemsToDocument,
} from "./grading";

describe("quiz content grading", () => {
  it("enables, synchronizes, and omits disabled grading", () => {
    const document = quizContentItemsToDocument({
      items: [{ id: "question", entry: createTrueFalseEntry("Question") }],
    });
    const enabled = enableQuizContentGrading(document);
    expect(enabled.grading).toEqual({
      schemaVersion: 2,
      items: { question: {} },
    });
    expect(enabled.grading?.items.question).not.toHaveProperty("points");
    expect(enabled.grading?.items.question).not.toHaveProperty("gradingKind");
    expect(disableQuizContentGrading(enabled)).not.toHaveProperty("grading");
  });
});
