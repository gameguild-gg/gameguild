import { describe, expect, it } from "vitest";
import {
  normalizeProfessorFacingType,
  resolveAuthoringContentKind,
} from "./authoring-content-kind";

describe("normalizeProfessorFacingType", () => {
  it("maps legacy Page to Lesson and Challenge to Assignment", () => {
    expect(normalizeProfessorFacingType("Page")).toBe("Lesson");
    expect(normalizeProfessorFacingType("Challenge")).toBe("Assignment");
  });

  it("passes through professor-facing types unchanged", () => {
    expect(normalizeProfessorFacingType("Lesson")).toBe("Lesson");
    expect(normalizeProfessorFacingType("Code")).toBe("Code");
    expect(normalizeProfessorFacingType("Questionnaire")).toBe("Questionnaire");
    expect(normalizeProfessorFacingType("Assignment")).toBe("Assignment");
    expect(normalizeProfessorFacingType("Project")).toBe("Project");
  });

  it("returns null for missing values", () => {
    expect(normalizeProfessorFacingType(null)).toBeNull();
    expect(normalizeProfessorFacingType(undefined)).toBeNull();
  });
});

describe("resolveAuthoringContentKind", () => {
  it("selects the code editor when the published item is Code", () => {
    expect(resolveAuthoringContentKind("Code", "Lesson")).toBe("code");
  });

  it("selects the quiz editor when the published item is Questionnaire", () => {
    expect(resolveAuthoringContentKind("Questionnaire", "Lesson")).toBe("quiz");
  });

  it("selects the lesson editor when both agree on Lesson", () => {
    expect(resolveAuthoringContentKind("Lesson", "Lesson")).toBe("lesson");
  });

  it("falls back to the payload type when the item type is missing", () => {
    expect(resolveAuthoringContentKind(null, "Questionnaire")).toBe("quiz");
    expect(resolveAuthoringContentKind(null, "Code")).toBe("code");
    expect(resolveAuthoringContentKind(null, "Lesson")).toBe("lesson");
  });

  it("gives a legacy Page/Challenge payload the same editor as its normalized form", () => {
    expect(resolveAuthoringContentKind("Page", "Page")).toBe("lesson");
    expect(resolveAuthoringContentKind(null, "Page")).toBe("lesson");
    expect(resolveAuthoringContentKind("Challenge", "Challenge")).toBe(
      "lesson",
    );
  });

  it("prefers the published item type over a stale legacy payload type", () => {
    expect(resolveAuthoringContentKind("Code", "Lesson")).toBe("code");
    expect(resolveAuthoringContentKind("Questionnaire", "Lesson")).toBe("quiz");
  });
});
