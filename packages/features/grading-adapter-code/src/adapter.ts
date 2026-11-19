import type {
  AssessmentExecutionDeliveryV1,
  AssessmentItemManifestV1,
  AssessmentResponseEnvelopeV1,
  ExecutableComponentDescriptorV1,
  ReviewExecutionContext,
} from '@game-guild/grading';
import type { CodingExecutionReport } from '@game-guild/coding';
import { CODE_REVIEW_CONTEXTS } from './capabilities';
import {
  CODE_ASSESSMENT_TYPE_ADAPTER,
  CODE_CONTENT_TYPE,
  type CodeGradeMapping,
  type CodeGradingItemInput,
  type CodeItemProjection,
  type CodeLearnerDeliveryItem,
  type CodeSubmissionPayload,
} from './contracts';
import { createCodeExecutionDelivery, createCodeItemManifest } from './delivery';
import { mapCodeExecutionReportToGrade } from './evaluation';
import { projectCodeGradingItems } from './items';
import { decodeCodeSubmissionEnvelope } from './responses';

/**
 * Coding intentionally does not implement AssessmentTypeAdapterV1: that
 * interface owns synchronous deterministic evaluation. The trusted executor
 * runs on the server and supplies a versioned report to mapExecutionReport.
 */
export interface CodeAssessmentTypeAdapter {
  readonly key: typeof CODE_ASSESSMENT_TYPE_ADAPTER.key;
  readonly version: typeof CODE_ASSESSMENT_TYPE_ADAPTER.version;
  readonly contentType: typeof CODE_CONTENT_TYPE;
  readonly isCurrentForAuthoring: true;
  readonly contexts: readonly ReviewExecutionContext[];
  readonly capability: ExecutableComponentDescriptorV1 & {
    readonly kind: 'assessment-type-adapter';
  };
  projectAuthoring(input: CodeGradingItemInput): readonly CodeItemProjection[];
  createManifest(items: readonly CodeItemProjection[]): AssessmentItemManifestV1[];
  createDelivery(
    definitionRevisionId: string,
    executionSnapshotHash: string,
    items: readonly CodeItemProjection[],
    itemOrder?: readonly string[],
  ): AssessmentExecutionDeliveryV1<CodeLearnerDeliveryItem>;
  decodeResponse(envelope: AssessmentResponseEnvelopeV1, items: readonly CodeItemProjection[]): CodeSubmissionPayload;
  mapExecutionReport(items: readonly CodeItemProjection[], report: CodingExecutionReport): CodeGradeMapping;
}

export const codeAssessmentTypeAdapter: CodeAssessmentTypeAdapter = Object.freeze({
  ...CODE_ASSESSMENT_TYPE_ADAPTER,
  contentType: CODE_CONTENT_TYPE,
  isCurrentForAuthoring: true as const,
  contexts: CODE_REVIEW_CONTEXTS,
  capability: Object.freeze({
    kind: 'assessment-type-adapter' as const,
    ...CODE_ASSESSMENT_TYPE_ADAPTER,
    contexts: CODE_REVIEW_CONTEXTS,
  }),
  projectAuthoring: projectCodeGradingItems,
  createManifest: createCodeItemManifest,
  createDelivery: createCodeExecutionDelivery,
  decodeResponse: decodeCodeSubmissionEnvelope,
  mapExecutionReport: mapCodeExecutionReportToGrade,
});
