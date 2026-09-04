import { describe, expect, it } from "vitest";
import {
  GradingContractValidationError,
  createContentGradingDefinition,
  syncContentGradingDefinition,
  validateContentGradingDefinition,
} from "./index";

describe("ContentGradingDefinitionV2", () => {
  it("stores only stable item IDs and optional rubric references", () => {
    expect(createContentGradingDefinition(["q1", "q2"])).toEqual({
      schemaVersion: 2,
      items: { q1: {}, q2: {} },
    });
    expect(validateContentGradingDefinition({
      schemaVersion: 2,
      items: { q1: { rubricRef: "rubric-1" } },
    })).toEqual({
      schemaVersion: 2,
      items: { q1: { rubricRef: "rubric-1" } },
    });
  });

  it("rejects duplicated IDs and operational or quiz-specific fields", () => {
    expect(() => createContentGradingDefinition(["q1", "q1"]))
      .toThrow(GradingContractValidationError);
    for (const forbidden of ["points", "gradingKind", "contentBlockId", "itemId"]) {
      expect(() => validateContentGradingDefinition({
        schemaVersion: 2,
        items: { q1: { [forbidden]: "value" } },
      }), forbidden).toThrow(GradingContractValidationError);
    }
  });

  it("synchronizes IDs without losing rubric references", () => {
    const result = syncContentGradingDefinition(["q2", "q3"], {
      schemaVersion: 2,
      items: { q1: {}, q2: { rubricRef: "rubric-2" } },
    });
    expect(result).toEqual({
      schemaVersion: 2,
      items: { q2: { rubricRef: "rubric-2" }, q3: {} },
    });
  });
});
