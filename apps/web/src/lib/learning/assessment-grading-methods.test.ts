import { describe, expect, it } from "vitest";

import {
  parseGradingMethods,
  serializeGradingMethods,
} from "./assessment-grading-methods";

describe("assessment grading method helpers", () => {
  it("parses supported flags while ignoring blanks, unknown values, and duplicates", () => {
    expect([
      ...parseGradingMethods(" PeerReview,AIGraded,Unknown,,PeerReview "),
    ]).toEqual(["PeerReview", "AIGraded"]);
    expect([...parseGradingMethods(null)]).toEqual([]);
  });

  it("serializes flags in their supplied order", () => {
    expect(serializeGradingMethods(["AutoGraded", "InstructorGraded"])).toBe(
      "AutoGraded,InstructorGraded",
    );
    expect(serializeGradingMethods([])).toBe("");
  });
});
