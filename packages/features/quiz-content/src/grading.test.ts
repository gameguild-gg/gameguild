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
    expect(enabled.grading?.items.question).toMatchObject({
      contentBlockId: "question",
      gradingKind: "deterministic",
    });
    expect(disableQuizContentGrading(enabled)).not.toHaveProperty("grading");
  });
});
