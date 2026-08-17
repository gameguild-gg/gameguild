import { describe, expect, it } from "vitest";
import { QuizEntryType } from "@game-guild/quiz";
import { createQuizSessionState, quizSessionReducer } from "./quiz-session-reducer";

describe("quizSessionReducer", () => {
  it("keeps typed answers and resets when the question type changes", () => {
    const initial = createQuizSessionState(QuizEntryType.SingleChoice);
    const answered = quizSessionReducer(initial, {
      type: "replace-answer",
      answer: { type: QuizEntryType.SingleChoice, optionId: "option" },
    });
    expect(answered.answer).toEqual({ type: QuizEntryType.SingleChoice, optionId: "option" });

    const reset = quizSessionReducer(answered, {
      type: "reset",
      questionType: QuizEntryType.Hotspot,
    });
    expect(reset).toEqual({
      answer: { type: QuizEntryType.Hotspot, point: null },
      phase: "answering",
    });
  });
});
