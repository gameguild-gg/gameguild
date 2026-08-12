import type {
  AnswerKey,
  ContentGradingDefinition,
  GradedItemConfig,
  StructuredAnswerPayload,
} from '../types';

export interface GradingAdapter<TAuthoringPayload = unknown> {
  contentType: string;
  // Reads authored content and creates the item-level grading map.
  extractItems(payload: TAuthoringPayload): Record<string, GradedItemConfig>;
  // Extracts server-owned answer-key material from the authored payload.
  extractAnswerKey(payload: TAuthoringPayload, grading: ContentGradingDefinition): AnswerKey;
  // Produces the learner payload for grading-enabled runtime.
  redactLearnerPayload(payload: TAuthoringPayload, grading: ContentGradingDefinition): unknown;
  // Whitelists learner answer fields and drops any client-sent grading claims.
  buildStructuredAnswerPayload(input: unknown, grading: ContentGradingDefinition): StructuredAnswerPayload;
}
