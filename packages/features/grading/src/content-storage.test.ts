import { describe, expect, it } from "vitest";
import {
  CONTENT_GRADING_STORAGE_KEY,
  type ContentGradingDefinitionV2,
  readContentGradingDefinition,
  writeContentGradingDefinition,
} from "./index";

const grading: ContentGradingDefinitionV2 = {
  schemaVersion: 2,
  items: { q1: {} },
};

describe("content storage grading metadata", () => {
  it("writes and reads grading beside content data", () => {
    const body = writeContentGradingDefinition({ order: [["q1", "quiz"]] }, grading);
    expect(body[CONTENT_GRADING_STORAGE_KEY]).toEqual(grading);
    expect(readContentGradingDefinition(body)).toEqual(grading);
    expect(readContentGradingDefinition(JSON.stringify(body))).toBeNull();
  });

  it("removes grading when the feature is disabled", () => {
    expect(writeContentGradingDefinition({ order: [], grading }, null)).toEqual({ order: [] });
  });
});
