import { describe, expect, it } from "vitest";
import { evaluateQuizAnswer } from "./evaluation/evaluate-answer";
import {
  FillBlankInputType,
  QuizEntryType,
  createCategorizationEntry,
  createFillInTheBlankEntry,
  createFormulaEntry,
  createHighlightEntry,
  createMatchingEntry,
  createMultipleChoiceEntry,
  createOrderingEntry,
} from "./questions/question-types";

describe("evaluateQuizAnswer", () => {
  it("grades set and ordered answers with their distinct semantics", () => {
    const multiple = createMultipleChoiceEntry();
    expect(evaluateQuizAnswer(multiple, {
      type: QuizEntryType.MultipleChoice,
      optionIds: [...multiple.correctOptionIds].reverse(),
    }).status).toBe("correct");

    const ordering = createOrderingEntry();
    expect(evaluateQuizAnswer(ordering, {
      type: QuizEntryType.Ordering,
      itemIds: [...ordering.items]
        .sort((a, b) => a.correctPosition - b.correctPosition)
        .map((item) => item.id),
    }).status).toBe("correct");
  });

  it("grades structured matching and categorization answers", () => {
    const matching = createMatchingEntry();
    expect(evaluateQuizAnswer(matching, {
      type: QuizEntryType.Matching,
      matches: Object.fromEntries(matching.pairs.map((pair) => [pair.id, pair.right])),
    }).status).toBe("correct");

    const categorization = createCategorizationEntry();
    expect(evaluateQuizAnswer(categorization, {
      type: QuizEntryType.Categorization,
      categoryIdsByItem: Object.fromEntries(
        categorization.items.map((item) => [item.id, item.correctCategoryIds]),
      ),
    }).status).toBe("correct");
  });

  it("applies fill-blank number units and precision", () => {
    const entry = createFillInTheBlankEntry("Mass: ___");
    entry.blanks[0]!.input = {
      type: FillBlankInputType.Number,
      correctValue: 12.5,
      tolerance: 0,
      unit: "kg",
      requireUnit: true,
      requiredPrecision: 1,
    };
    expect(evaluateQuizAnswer(entry, {
      type: QuizEntryType.FillInTheBlank,
      values: { [entry.blanks[0]!.id]: "12.5 kg" },
    }).status).toBe("correct");
  });

  it("requires an explicit prompt to evaluate formula answers", () => {
    const formula = createFormulaEntry();
    const answer = { type: QuizEntryType.Formula, expression: formula.formula } as const;
    expect(evaluateQuizAnswer(formula, answer).status).toBe("unsupported");
    expect(evaluateQuizAnswer(formula, answer, { formulaPrompts: [{ x: 2, y: 3 }] }).status)
      .toBe("correct");
  });

  it("grades highlight overlap symmetrically", () => {
    const entry = createHighlightEntry();
    expect(evaluateQuizAnswer(entry, {
      type: QuizEntryType.Highlight,
      spans: entry.highlights,
    }).status).toBe("correct");
    expect(evaluateQuizAnswer(entry, {
      type: QuizEntryType.Highlight,
      spans: [...entry.highlights, { start: 0, end: 3 }],
    }).status).toBe("incorrect");
  });
});
