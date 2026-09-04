import type {
  IReviewCapabilityRegistry,
  ReviewExecutionContext,
} from "@game-guild/grading";
import {
  QUIZ_ANSWER_DECODER,
  QUIZ_AUTOMATED_REVIEW_HANDLER,
  QUIZ_DELIVERY_GENERATOR,
  QUIZ_DETERMINISTIC_ALGORITHM,
  QUIZ_PROJECTOR,
} from "./contracts";

const AUTHOR_TEST_ONLY = ["author-test"] as const satisfies readonly ReviewExecutionContext[];

export function registerQuizGradingCapabilities(
  registry: IReviewCapabilityRegistry,
  contexts: readonly ReviewExecutionContext[] = AUTHOR_TEST_ONLY,
): void {
  registry.registerComponent({
    kind: "item-projector",
    ...QUIZ_PROJECTOR,
    contexts,
  });
  registry.registerComponent({
    kind: "delivery-generator",
    ...QUIZ_DELIVERY_GENERATOR,
    contexts,
  });
  registry.registerComponent({
    kind: "answer-decoder",
    ...QUIZ_ANSWER_DECODER,
    contexts,
  });
  registry.registerComponent({
    kind: "grading-algorithm",
    ...QUIZ_DETERMINISTIC_ALGORITHM,
    contexts,
  });
  registry.registerReview({
    method: "AutomatedReview",
    contexts,
    handlerKey: QUIZ_AUTOMATED_REVIEW_HANDLER.key,
    handlerVersion: QUIZ_AUTOMATED_REVIEW_HANDLER.version,
  });
}
