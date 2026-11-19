import type {
  CodingAuthoringDefinition,
  CodingExecutionReportV1,
  CodingLearnerDelivery,
  CodingSubmissionV1,
  CodingTestOutcomeStateV1,
  CodingToolchainIdentityV1,
} from '@game-guild/coding';
import type { AssessmentItemProjectionV1, AssessmentResponseEnvelopeV1, GradeResultV1, ScoreValue } from '@game-guild/grading';

export const CODE_CONTENT_TYPE = 'coding-assignment' as const;
export const CODE_RESPONSE_PAYLOAD_SCHEMA = 'coding-response/v1' as const;
export const CODE_ITEM_PROJECTION_SCHEMA_VERSION = 1 as const;
export const CODE_EXECUTION_EVIDENCE_SCHEMA_VERSION = 1 as const;
export const CODE_EXECUTION_MODE = 'server-external' as const;

/** Frozen backend adapter versions that remain executable but are not rewritten here. */
export const FROZEN_CODE_ADAPTER_VERSIONS = ['1', '2', '3'] as const;
export const CODE_ASSESSMENT_TYPE_ADAPTER = {
  key: 'code-assessment-type',
  version: '4',
} as const;
export const CODE_AUTOMATED_REVIEW_HANDLER = {
  key: 'code-automated-review',
  version: '4',
} as const;

export const CODE_SUPPORTED_REVIEW_METHODS = ['AutomatedReview', 'InstructorReview'] as const;

export interface CodeGradingItemInputV1 {
  itemId: string;
  maxScore: ScoreValue;
  definition: CodingAuthoringDefinition;
}

export interface CodeTestScoreAllocationV1 {
  testId: string;
  maxScore: ScoreValue;
}

export interface CodeItemProjectionV1 extends AssessmentItemProjectionV1 {
  schemaVersion: typeof CODE_ITEM_PROJECTION_SCHEMA_VERSION;
  itemType: 'code';
  source: {
    contentType: typeof CODE_CONTENT_TYPE;
    itemId: string;
  };
  definition: CodingAuthoringDefinition;
  testScoreAllocation: CodeTestScoreAllocationV1[];
}

export interface CodeLearnerDeliveryItemV1 {
  itemId: string;
  definition: CodingLearnerDelivery;
}

export interface CodeSubmissionPayloadV1 {
  submission: CodingSubmissionV1;
}

export type CodeSubmissionEnvelopeV1 = AssessmentResponseEnvelopeV1<CodeSubmissionPayloadV1> & {
  contentType: typeof CODE_CONTENT_TYPE;
  payloadSchema: typeof CODE_RESPONSE_PAYLOAD_SCHEMA;
};

export interface CodeExecutionEvidenceTestV1 {
  testId: string;
  state: CodingTestOutcomeStateV1;
  awardedScore: ScoreValue | null;
  maxScore: ScoreValue;
  evidenceRef?: string;
}

export interface CodeExecutionEvidenceV1 {
  schemaVersion: typeof CODE_EXECUTION_EVIDENCE_SCHEMA_VERSION;
  kind: 'code-execution-report';
  evidenceId: string;
  reportId: string;
  executionState: CodingExecutionReportV1['executionState'];
  toolchain: CodingToolchainIdentityV1;
  receiptHash?: string;
  tests: CodeExecutionEvidenceTestV1[];
  failureCode?: string;
}

export interface CodeGradeMappingV1 {
  result: GradeResultV1;
  evidence: CodeExecutionEvidenceV1;
}

export type CodeReviewCapabilityV1 = 'automated-review-after-trusted-execution' | 'instructor-review' | 'unsupported';
