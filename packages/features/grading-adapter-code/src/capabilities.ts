import type { AssessmentReviewMethod, IReviewCapabilityRegistry, ReviewExecutionContext } from '@game-guild/grading';
import { CODE_ASSESSMENT_TYPE_ADAPTER, CODE_AUTOMATED_REVIEW_HANDLER, type CodeReviewCapabilityV1 } from './contracts';

const DEFAULT_CONTEXTS = ['author-test', 'official-submission'] as const satisfies readonly ReviewExecutionContext[];

export function classifyCodeReviewCapability(method: AssessmentReviewMethod): CodeReviewCapabilityV1 {
  if (method === 'AutomatedReview') return 'automated-review-after-trusted-execution';
  if (method === 'InstructorReview') return 'instructor-review';
  return 'unsupported';
}

export function registerCodeGradingCapabilities(registry: IReviewCapabilityRegistry, contexts: readonly ReviewExecutionContext[] = DEFAULT_CONTEXTS): void {
  registry.registerComponent({
    kind: 'assessment-type-adapter',
    ...CODE_ASSESSMENT_TYPE_ADAPTER,
    contexts,
  });
  registry.registerReview({
    method: 'AutomatedReview',
    handlerKey: CODE_AUTOMATED_REVIEW_HANDLER.key,
    handlerVersion: CODE_AUTOMATED_REVIEW_HANDLER.version,
    contexts,
  });
}

export const CODE_REVIEW_CONTEXTS = DEFAULT_CONTEXTS;
