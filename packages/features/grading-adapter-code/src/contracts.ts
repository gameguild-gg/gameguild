import type {
  CodingAuthoringDefinition,
  CodingExecutionReport,
  CodingLearnerDelivery,
  CodingSubmission,
  CodingTestOutcomeState,
  CodingToolchainIdentity,
} from '@game-guild/coding';
import type { AssessmentItemProjectionV1, AssessmentResponseEnvelopeV1, GradeResultV1, ScoreValue } from '@game-guild/grading';

export const CODE_CONTENT_TYPE = 'coding-assignment' as const;
export const CODE_RESPONSE_PAYLOAD_SCHEMA = 'code-files' as const;
export const CODE_ITEM_PROJECTION_SCHEMA_VERSION = 1 as const;
export const CODE_EXECUTION_EVIDENCE_SCHEMA_VERSION = 1 as const;
export const CODE_EXECUTION_MODE = 'server-external' as const;

export const CODE_ASSESSMENT_TYPE_ADAPTER = {
  key: 'code-assessment-type',
  version: '1',
} as const;
export const CODE_AUTOMATED_REVIEW_HANDLER = {
  key: 'code-wasm-review',
  version: '1',
} as const;

export const CODE_SUPPORTED_REVIEW_METHODS = ['AutomatedReview', 'InstructorReview'] as const;

export interface CodeGradingItemInput {
  itemId: string;
  maxScore: ScoreValue;
  definition: CodingAuthoringDefinition;
}

export interface CodeTestScoreAllocation {
  testId: string;
  maxScore: ScoreValue;
}

export interface CodeItemProjection extends AssessmentItemProjectionV1 {
  schemaVersion: typeof CODE_ITEM_PROJECTION_SCHEMA_VERSION;
  itemType: 'code';
  source: {
    contentType: typeof CODE_CONTENT_TYPE;
    itemId: string;
  };
  definition: CodingAuthoringDefinition;
  testScoreAllocation: CodeTestScoreAllocation[];
}

export interface CodeLearnerDeliveryItem {
  itemId: string;
  definition: CodingLearnerDelivery;
}

export interface CodeResponseFile {
  content: string;
  encoding: 'text';
}

export interface CodeResponsePayload {
  files: Record<string, CodeResponseFile>;
}

export interface CodeSubmissionPayload {
  submission: CodingSubmission;
}

export type CodeSubmissionEnvelope = AssessmentResponseEnvelopeV1<CodeResponsePayload> & {
  contentType: typeof CODE_CONTENT_TYPE;
  payloadSchema: typeof CODE_RESPONSE_PAYLOAD_SCHEMA;
};

export interface CodeExecutionEvidenceTest {
  testId: string;
  state: CodingTestOutcomeState;
  awardedScore: ScoreValue | null;
  maxScore: ScoreValue;
  evidenceRef?: string;
}

export interface CodeExecutionEvidence {
  schemaVersion: typeof CODE_EXECUTION_EVIDENCE_SCHEMA_VERSION;
  kind: 'code-execution-report';
  evidenceId: string;
  reportId: string;
  executionState: CodingExecutionReport['executionState'];
  toolchain: CodingToolchainIdentity;
  receiptHash?: string;
  tests: CodeExecutionEvidenceTest[];
  failureCode?: string;
}

export interface CodeGradeMapping {
  result: GradeResultV1;
  evidence: CodeExecutionEvidence;
}

export type CodeReviewCapability = 'automated-review-after-trusted-execution' | 'instructor-review' | 'unsupported';
