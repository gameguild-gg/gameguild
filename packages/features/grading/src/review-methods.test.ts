import { describe, expect, it } from "vitest";
import {
  parseReviewMethods,
  reviewMethodsToSequence,
  reviewSequenceToMethods,
} from "./index";

describe("ReviewMethods", () => {
  it("accepts exactly the draft and nine canonical workflows", () => {
    const valid = new Set([0, 1, 2, 4, 8, 9, 10, 12, 16, 24]);
    for (let value = 0; value <= 31; value += 1) {
      const parse = () => parseReviewMethods(value, { allowDraft: true });
      if (valid.has(value)) expect(parse, String(value)).not.toThrow();
      else expect(parse, String(value)).toThrow(TypeError);
    }
  });

  it("always places InstructorReview last", () => {
    const methods = reviewSequenceToMethods(["AutomatedReview", "InstructorReview"]);
    expect(methods).toBe(12);
    expect(reviewMethodsToSequence(methods)).toEqual(["AutomatedReview", "InstructorReview"]);
  });
});
