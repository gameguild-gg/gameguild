import type {
  IReviewCapabilityRegistry,
  ReviewExecutionContext,
} from "@game-guild/grading";
import {
  QUIZ_ASSESSMENT_TYPE_ADAPTER,
  QUIZ_AUTOMATED_REVIEW_HANDLER,
} from "./contracts";

const RUNTIME_CONTEXTS = ["author-test", "official-submission"] as const satisfies readonly ReviewExecutionContext[];

export function registerQuizGradingCapabilities(
  registry: IReviewCapabilityRegistry,
  contexts: readonly ReviewExecutionContext[] = RUNTIME_CONTEXTS,
): void {
  registry.registerComponent({
    kind: "assessment-type-adapter",
    ...QUIZ_ASSESSMENT_TYPE_ADAPTER,
    contexts,
  });
  registry.registerReview({
    method: "AutomatedReview",
    contexts,
    handlerKey: QUIZ_AUTOMATED_REVIEW_HANDLER.key,
    handlerVersion: QUIZ_AUTOMATED_REVIEW_HANDLER.version,
  });
}
