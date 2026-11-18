export const CODING_AUTHORING_SCHEMA_VERSION = 1 as const;
export const CODING_DELIVERY_SCHEMA_VERSION = 1 as const;
export const CODING_SUBMISSION_SCHEMA_VERSION = 1 as const;
export const CODING_EXECUTION_REPORT_SCHEMA_VERSION = 1 as const;
export const CODING_CONTENT_TYPE = 'coding-assignment' as const;
export const CODING_WORKSPACE_ROOT = '/home/user' as const;

export const MAX_CODING_FILES = 100;
export const MAX_CODING_TESTS = 100;
export const MAX_CODING_FILE_PATH_LENGTH = 240;
export const MAX_CODING_FILE_CONTENT_BYTES = 2_000_000;
export const MAX_CODING_DEFINITION_BYTES = 10_000_000;
export const MAX_CODING_SUBMISSION_BYTES = 2_000_000;

export type CodingFileEncodingV1 = 'text' | 'base64';
export type CodingVisibilityV1 = 'public' | 'private';
export type CodingScalarTypeV1 = 'string' | 'boolean' | 'integer' | 'float';

export interface CodingToolchainIdentityV1 {
  key: string;
  version: string;
  runtimeAbi: string;
  artifactDigest: `sha256:${string}`;
}

export interface CodingAuthoringFileV1 {
  id: string;
  path: string;
  encoding: CodingFileEncodingV1;
  content: string;
  mutable: boolean;
  visibility: CodingVisibilityV1;
}

export interface CodingLimitsV1 {
  timeLimitMs: number;
  memoryLimitBytes: number;
  outputLimitBytes: number;
  maxFileCount: number;
}

export interface CodingEntryPointV1 {
  path: string;
  symbol?: string;
}

export type CodingScalarValueV1 =
  { type: 'string'; value: string } | { type: 'boolean'; value: boolean } | { type: 'integer'; value: number } | { type: 'float'; value: string };

interface CodingTestBaseV1 {
  id: string;
  name?: string;
  visibility: CodingVisibilityV1;
  weightUnits: number;
}

export interface CodingStdioTestV1 extends CodingTestBaseV1 {
  kind: 'stdio';
  stdin: string;
  expected: {
    stdout: string;
    stderr?: string;
    exitCode?: number;
  };
}

export interface CodingFunctionParameterV1 {
  name: string;
  type: CodingScalarTypeV1;
}

export interface CodingFunctionTestCaseV1 {
  id: string;
  inputs: CodingScalarValueV1[];
  expected: CodingScalarValueV1;
}

export interface CodingFunctionTestV1 extends CodingTestBaseV1 {
  kind: 'function';
  function: {
    name: string;
    parameters: CodingFunctionParameterV1[];
    returnType: CodingScalarTypeV1;
  };
  cases: CodingFunctionTestCaseV1[];
}

export type CodingTestV1 = CodingStdioTestV1 | CodingFunctionTestV1;

export interface CodingAuthoringDefinitionV1 {
  schemaVersion: typeof CODING_AUTHORING_SCHEMA_VERSION;
  type: typeof CODING_CONTENT_TYPE;
  toolchain: CodingToolchainIdentityV1;
  files: CodingAuthoringFileV1[];
  tests: CodingTestV1[];
  limits: CodingLimitsV1;
  entryPoint: CodingEntryPointV1;
  allowLearnerCreatedFiles: boolean;
}

export interface CodingLearnerFileV1 {
  id: string;
  path: string;
  encoding: CodingFileEncodingV1;
  content: string;
  mutable: boolean;
}

export type CodingLearnerTestV1 = Omit<CodingStdioTestV1, 'visibility'> | Omit<CodingFunctionTestV1, 'visibility'>;

export interface CodingLearnerDeliveryV1 {
  schemaVersion: typeof CODING_DELIVERY_SCHEMA_VERSION;
  type: typeof CODING_CONTENT_TYPE;
  toolchain: CodingToolchainIdentityV1;
  files: CodingLearnerFileV1[];
  publicTests: CodingLearnerTestV1[];
  limits: CodingLimitsV1;
  entryPoint: CodingEntryPointV1;
  allowLearnerCreatedFiles: boolean;
}

export interface CodingSubmissionFileV1 {
  fileId: string;
  path: string;
  encoding: 'text';
  content: string;
}

export interface CodingSubmissionV1 {
  schemaVersion: typeof CODING_SUBMISSION_SCHEMA_VERSION;
  files: CodingSubmissionFileV1[];
}

export type CodingExecutionStateV1 = 'completed' | 'partial' | 'executor-error' | 'cancelled' | 'timed-out';

export type CodingTestOutcomeStateV1 = 'passed' | 'failed' | 'error' | 'not-run';

export interface CodingTestOutcomeV1 {
  testId: string;
  state: CodingTestOutcomeStateV1;
  durationMs: number;
  evidenceRef?: string;
  feedback?: string;
}

export interface CodingExecutionReportV1 {
  schemaVersion: typeof CODING_EXECUTION_REPORT_SCHEMA_VERSION;
  reportId: string;
  executionState: CodingExecutionStateV1;
  toolchain: CodingToolchainIdentityV1;
  receiptHash?: string;
  outcomes: CodingTestOutcomeV1[];
  failureCode?: string;
}
