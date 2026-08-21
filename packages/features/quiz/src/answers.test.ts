import { describe, expect, it } from "vitest";
import {
  createEmptyQuizAnswer,
  fromStructuredGradingAnswer,
  normalizeQuizAnswer,
  toStructuredGradingAnswer,
} from "./answers/answers";
import { QuizEntryType } from "./questions/question-types";

describe("quiz answers", () => {
  it("creates a type-specific empty answer", () => {
    expect(createEmptyQuizAnswer(QuizEntryType.Hotspot)).toEqual({
      type: QuizEntryType.Hotspot,
      point: null,
    });
  });

  it("round-trips structured matching answers without storing delimiters in runtime state", () => {
    const answer = {
      type: QuizEntryType.Matching,
      matches: { france: "Europe:West", japan: "Asia" },
    } as const;
    expect(fromStructuredGradingAnswer(
      QuizEntryType.Matching,
      toStructuredGradingAnswer(answer),
    )).toEqual(answer);
  });

  it("normalizes malformed values to safe typed defaults", () => {
    expect(normalizeQuizAnswer(QuizEntryType.Hotspot, {
      type: QuizEntryType.Hotspot,
      point: { x: "10", y: 20 },
    })).toEqual({ type: QuizEntryType.Hotspot, point: null });
    expect(normalizeQuizAnswer(QuizEntryType.Highlight, {
      type: QuizEntryType.Highlight,
      spans: [{ start: 2, end: 1 }, { start: 1, end: 3 }],
    })).toEqual({ type: QuizEntryType.Highlight, spans: [{ start: 1, end: 3 }] });
  });
});
