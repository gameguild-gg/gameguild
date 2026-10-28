import {
  createReviewMethods,
  parseReviewMethods,
  REVIEW_METHOD_FLAGS,
  reviewMethodsToSequence,
  type AssessmentReviewMethod,
  type ReviewMethods,
} from "@game-guild/grading";

export const ASSESSMENT_PRIMARY_REVIEW_METHODS = [
  "AutomatedReview",
  "InstructorReview",
  "PeerReview",
  "AIReview",
  "SelfReview",
] as const satisfies readonly AssessmentReviewMethod[];

export const AVAILABLE_PRIMARY_REVIEW_METHODS = [
  "AutomatedReview",
  "InstructorReview",
  "PeerReview",
  "SelfReview",
] as const satisfies readonly AssessmentReviewMethod[];

export type AIReviewExecutionContext =
  | "author-test"
  | "official-submission";

export interface AIReviewProviderCapability {
  providerKey: string;
  policyVersions: string[];
  contexts: AIReviewExecutionContext[];
  available: boolean;
  code: string | null;
  message: string | null;
}

export interface AIReviewConfiguration {
  providerKey: string;
  policyVersion: string;
}

const REQUIRED_AI_REVIEW_CONTEXTS = [
  "author-test",
  "official-submission",
] as const satisfies readonly AIReviewExecutionContext[];

export function isPrimaryReviewMethodAvailable(
  method: AssessmentReviewMethod,
  aiReviewProviders: readonly AIReviewProviderCapability[] = [],
): boolean {
  if (method === "AIReview") {
    return resolveAIReviewConfiguration(aiReviewProviders) !== null;
  }
  return (AVAILABLE_PRIMARY_REVIEW_METHODS as readonly AssessmentReviewMethod[])
    .includes(method);
}

export function resolveAIReviewConfiguration(
  aiReviewProviders: readonly AIReviewProviderCapability[],
): AIReviewConfiguration | null {
  const provider = aiReviewProviders.find(
    (candidate) =>
      candidate.available &&
      candidate.providerKey.trim().length > 0 &&
      candidate.policyVersions.length > 0 &&
      REQUIRED_AI_REVIEW_CONTEXTS.every((context) =>
        candidate.contexts.includes(context),
      ),
  );
  const policyVersion = provider?.policyVersions.find(
    (candidate) => candidate.trim().length > 0,
  );
  return provider && policyVersion
    ? { providerKey: provider.providerKey, policyVersion }
    : null;
}

export const REVIEW_METHOD_LABELS: Record<AssessmentReviewMethod, string> = {
  AutomatedReview: "Automated review",
  InstructorReview: "Instructor review",
  PeerReview: "Peer review",
  AIReview: "AI review",
  SelfReview: "Self review",
};

export function readReviewWorkflow(value: unknown): {
  methods: ReviewMethods;
  primary: AssessmentReviewMethod | null;
  requiresInstructorReview: boolean;
} {
  const methods = parseReviewMethods(value, { allowDraft: true });
  const sequence = reviewMethodsToSequence(methods);
  return {
    methods,
    primary: sequence[0] ?? null,
    requiresInstructorReview:
      sequence.length === 2 && sequence[1] === "InstructorReview",
  };
}

export function buildReviewWorkflow(
  primary: AssessmentReviewMethod,
  requiresInstructorReview: boolean,
): ReviewMethods {
  return createReviewMethods(
    primary,
    primary !== "InstructorReview" && requiresInstructorReview,
  );
}

export function hasReviewMethod(
  methods: ReviewMethods,
  method: AssessmentReviewMethod,
): boolean {
  return (methods & REVIEW_METHOD_FLAGS[method]) !== 0;
}

export {
  REVIEW_METHOD_FLAGS,
  type AssessmentReviewMethod,
  type ReviewMethods,
};
