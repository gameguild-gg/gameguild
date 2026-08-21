import { describe, expect, it } from "vitest";
import { createTrueFalseEntry } from "@game-guild/quiz";
import { QUIZ_BLOCK_TYPE, QUIZ_CONTENT_SCHEMA_VERSION } from "./constants";
import {
  assertQuizContentDocument,
  parseQuizContentDocument,
  QuizContentValidationError,
} from "./parsing";
import { createAllQuestionTypesDocument } from "./testing/fixtures";

describe("quiz content parsing", () => {
  it("round-trips all supported question types", () => {
    const source = createAllQuestionTypesDocument();
    const result = parseQuizContentDocument(source);
    expect(result.issues).toEqual([]);
    expect(result.document).toEqual(source);
    expect(result.document.order).toHaveLength(14);
  });

  it("fails closed for missing or unsupported versions", () => {
    const missing = parseQuizContentDocument({ order: [], blocks: {} });
    expect(missing.document.order).toEqual([]);
    expect(missing.issues[0]?.code).toBe("unsupported-version");

    const future = parseQuizContentDocument({
      schemaVersion: 2,
      order: [],
      blocks: {},
    });
    expect(future.issues[0]?.code).toBe("unsupported-version");
  });

  it("preserves valid questions while reporting invalid collection entries", () => {
    const valid = createTrueFalseEntry("Valid");
    const result = parseQuizContentDocument({
      schemaVersion: QUIZ_CONTENT_SCHEMA_VERSION,
      order: [
        ["1", QUIZ_BLOCK_TYPE],
        ["1", QUIZ_BLOCK_TYPE],
        ["2", QUIZ_BLOCK_TYPE],
        ["3", "video"],
      ],
      blocks: {
        "1": valid,
        "2": { ...valid, unexpected: true },
        orphan: valid,
      },
    });

    expect(result.document.order).toEqual([["1", QUIZ_BLOCK_TYPE]]);
    expect(result.issues.map((issue) => issue.code)).toEqual(expect.arrayContaining([
      "duplicate-block-id",
      "invalid-quiz-entry",
      "invalid-order-entry",
      "orphan-block-payload",
    ]));
  });

  it("throws when an asserted document is not canonical", () => {
    expect(() => assertQuizContentDocument({
      schemaVersion: QUIZ_CONTENT_SCHEMA_VERSION,
      order: [],
      blocks: {},
      unknown: true,
    })).toThrow(QuizContentValidationError);
  });
});
