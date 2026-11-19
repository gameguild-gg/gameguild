import { describe, expect, it } from "vitest";

import {
  buildReviewWorkflow,
  hasReviewMethod,
  isPrimaryReviewMethodAvailable,
  readReviewWorkflow,
  resolveAIReviewConfiguration,
  REVIEW_METHOD_FLAGS,
  type AIReviewProviderCapability,
} from "./assessment-grading-methods";

describe("assessment grading method helpers", () => {
  it("reads a canonical two-stage review workflow", () => {
    expect(
      readReviewWorkflow(
        REVIEW_METHOD_FLAGS.AutomatedReview |
          REVIEW_METHOD_FLAGS.InstructorReview,
      ),
    ).toEqual({
      methods: 12,
      primary: "AutomatedReview",
      requiresInstructorReview: true,
    });
    expect(readReviewWorkflow(0)).toEqual({
      methods: 0,
      primary: null,
      requiresInstructorReview: false,
    });
  });

  it("builds and inspects canonical review flags", () => {
    const methods = buildReviewWorkflow("PeerReview", true);
    expect(methods).toBe(9);
    expect(hasReviewMethod(methods, "PeerReview")).toBe(true);
    expect(hasReviewMethod(methods, "InstructorReview")).toBe(true);
    expect(hasReviewMethod(methods, "AIReview")).toBe(false);
    expect(buildReviewWorkflow("InstructorReview", true)).toBe(8);
  });

  it("keeps implemented reviews available and gates AI review on sanitized capabilities", () => {
    expect(isPrimaryReviewMethodAvailable("AutomatedReview")).toBe(true);
    expect(isPrimaryReviewMethodAvailable("InstructorReview")).toBe(true);
    expect(isPrimaryReviewMethodAvailable("SelfReview")).toBe(true);
    expect(isPrimaryReviewMethodAvailable("PeerReview")).toBe(true);
    expect(isPrimaryReviewMethodAvailable("AIReview")).toBe(false);

    const unavailable: AIReviewProviderCapability[] = [{
      providerKey: "gameguild-ai",
      policyVersions: ["v1"],
      contexts: ["author-test", "official-submission"],
      available: false,
      code: "credential-missing",
      message: "The configured provider has no server credential.",
    }];
    expect(isPrimaryReviewMethodAvailable("AIReview", unavailable)).toBe(false);

    const available = [{ ...unavailable[0], available: true }];
    expect(isPrimaryReviewMethodAvailable("AIReview", available)).toBe(true);
    expect(resolveAIReviewConfiguration(available)).toEqual({
      providerKey: "gameguild-ai",
      policyVersion: "v1",
    });
  });

  it("requires AI review support in author test and official submission", () => {
    expect(resolveAIReviewConfiguration([{
      providerKey: "author-test-only",
      policyVersions: ["v1"],
      contexts: ["author-test"],
      available: true,
      code: null,
      message: null,
    }])).toBeNull();
  });
});
