import { describe, expect, it } from "vitest";

import {
  buildAssessmentPayload,
  buildContentActivityPayload,
  getPreferredSubmissionModality,
} from "./activity-contracts";

describe("getPreferredSubmissionModality", () => {
  it("routes quizzes through their dedicated runtime and projects through project submissions", () => {
    expect(getPreferredSubmissionModality("Quiz", "File")).toBe("None");
    expect(getPreferredSubmissionModality("Project", "Text")).toBe("Project");
  });

  it("preserves an explicitly configured usable modality", () => {
    expect(getPreferredSubmissionModality("Assignment", "Url")).toBe("Url");
  });

  it("falls back to text when the modality is absent or disabled", () => {
    expect(getPreferredSubmissionModality("Assignment", "None")).toBe("Text");
    expect(getPreferredSubmissionModality(undefined, undefined)).toBe("Text");
  });
});

describe("buildAssessmentPayload", () => {
  it.each([
    ["File", { filePayload: "answer" }],
    ["Url", { urlPayload: "answer" }],
    ["Code", { codePayload: "answer" }],
    ["Media", { mediaPayload: "answer" }],
    ["Project", { projectPayload: "answer" }],
    ["Text", { textPayload: "answer" }],
  ] as const)("builds the %s wire payload", (modality, expected) => {
    expect(buildAssessmentPayload(modality, "  answer  ")).toEqual(expected);
  });

  it("rejects blank submissions", () => {
    expect(() => buildAssessmentPayload("Text", "   ")).toThrow(
      "A submission response is required.",
    );
  });

  it("rejects disabled and unknown submission modalities", () => {
    expect(() => buildAssessmentPayload("None", "answer")).toThrow(
      "This assessment does not have a valid submission method.",
    );
    expect(() =>
      buildAssessmentPayload("Unsupported" as never, "answer"),
    ).toThrow("This assessment does not have a valid submission method.");
    expect(() =>
      buildAssessmentPayload("StructuredAnswer", "answer"),
    ).toThrow("This assessment does not have a valid submission method.");
  });
});

describe("buildContentActivityPayload", () => {
  it("builds discussion, reflection, and survey responses", () => {
    expect(buildContentActivityPayload("discussion", "  message  ")).toEqual({
      kind: "discussion",
      body: "message",
    });
    expect(buildContentActivityPayload("reflection", "  insight  ")).toEqual({
      kind: "reflection",
      body: "insight",
    });
    expect(buildContentActivityPayload("survey", "  choice  ")).toEqual({
      kind: "survey",
      answers: { response: "choice" },
    });
  });

  it("rejects blank activity responses", () => {
    expect(() => buildContentActivityPayload("discussion", "   ")).toThrow(
      "A response is required.",
    );
  });
});
