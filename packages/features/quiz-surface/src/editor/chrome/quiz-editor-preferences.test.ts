import { describe, expect, it } from "vitest";
import {
  DEFAULT_QUIZ_EDITOR_MODAL_SIZE,
  normalizeQuizEditorModalSize,
} from "./quiz-editor-preferences";

describe("quiz editor preferences", () => {
  it("accepts supported workspace sizes", () => {
    expect(normalizeQuizEditorModalSize("compact")).toBe("compact");
    expect(normalizeQuizEditorModalSize("fullscreen")).toBe("fullscreen");
  });

  it("falls back when persisted data is invalid", () => {
    expect(normalizeQuizEditorModalSize("unknown")).toBe(
      DEFAULT_QUIZ_EDITOR_MODAL_SIZE,
    );
  });
});
