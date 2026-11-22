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
export const MAX_CODING_TIME_LIMIT_MS = 30_000;
export const MAX_CODING_OUTPUT_LIMIT_BYTES = 2_000_000;
export const CODING_EXECUTION_CAPABILITIES = Object.freeze({
  memoryLimit: Object.freeze({ available: false, reason: 'unsupported-runtime' }),
});
export const CODING_TOOLCHAIN = Object.freeze({
  version: '4.5.2',
  runtimeAbi: 'emception-browser-v1',
  artifactDigest: 'sha256:405e9f1fdd936ba31e11247fa1f901eaee24c4fcf641e343505df75c3d539b37',
} as const);

export type CodingFileEncoding = 'text' | 'base64';
export type CodingVisibility = 'public' | 'private';
export type CodingScalarType = 'string' | 'boolean' | 'integer' | 'float';
export type CodingExerciseType = 'function' | 'standard-io';

export interface CodingProblem {
  statement: string;
  examples: { id: string; input: string; output: string; explanation: string }[];
  constraints: string[];
}

export interface CodingValidationSolution {
  id: string;
  name: string;
  role: 'reference' | 'incorrect';
  files: { fileId: string; path: string; content: string }[];
}

export interface CodingToolchainIdentity {
  key: string;
  version: string;
  runtimeAbi: string;
  artifactDigest: `sha256:${string}`;
}

export interface CodingAuthoringFile {
  id: string;
  path: string;
  encoding: CodingFileEncoding;
  content: string;
  mutable: boolean;
  visibility: CodingVisibility;
}

export interface CodingLimits {
  timeLimitMs: number;
  /** Retained historical value; configured RAM is not supported for new publication. */
  memoryLimitBytes?: number;
  outputLimitBytes: number;
  maxFileCount: number;
}

export interface CodingEntryPoint {
  path: string;
  symbol?: string;
}

export interface CodingExecutionEnvironment {
  language: 'c' | 'cpp';
  runtime: 'wasi' | 'sdl3' | 'raylib' | 'allegro5';
}

export interface CodingExecutionConfiguration {
  environment: CodingExecutionEnvironment;
  toolchain: CodingToolchainIdentity;
  limits: Omit<CodingLimits, 'memoryLimitBytes'>;
  entryPoint: CodingEntryPoint;
}

export type CodingScalarValue =
  | { type: 'string'; value: string }
  | { type: 'boolean'; value: boolean }
  | { type: 'integer'; value: number }
  | { type: 'float'; value: string };

interface CodingTestBase {
  id: string;
  name?: string;
  visibility: CodingVisibility;
  weightUnits: number;
}

export interface CodingStdioTest extends CodingTestBase {
  kind: 'stdio';
  stdin: string;
  expected: {
    stdout: string;
    stderr?: string;
    exitCode?: number;
  };
}

export interface CodingFunctionParameter {
  name: string;
  type: CodingScalarType;
}

export interface CodingFunctionTestCase {
  id: string;
  inputs: CodingScalarValue[];
  expected: CodingScalarValue;
}

export interface CodingFunctionTest extends CodingTestBase {
  kind: 'function';
  function: {
    name: string;
    parameters: CodingFunctionParameter[];
    returnType: CodingScalarType;
  };
  cases: CodingFunctionTestCase[];
}

export type CodingTest = CodingStdioTest | CodingFunctionTest;

export interface CodingAuthoringDefinition {
  schemaVersion: typeof CODING_AUTHORING_SCHEMA_VERSION;
  type: typeof CODING_CONTENT_TYPE;
  exerciseType: CodingExerciseType;
  environment?: CodingExecutionEnvironment;
  /** Absent on retained definitions written before structured problems. */
  problem?: CodingProblem;
  /** Author-only replacements, never included in learner delivery or starter files. */
  validationSolutions?: CodingValidationSolution[];
  toolchain: CodingToolchainIdentity;
  files: CodingAuthoringFile[];
  tests: CodingTest[];
  limits: CodingLimits;
  entryPoint: CodingEntryPoint;
  allowLearnerCreatedFiles: boolean;
}

export interface CodingLearnerFile {
  id: string;
  path: string;
  encoding: CodingFileEncoding;
  content: string;
  mutable: boolean;
}

export type CodingLearnerTest = Omit<CodingStdioTest, 'visibility'> | Omit<CodingFunctionTest, 'visibility'>;

export interface CodingLearnerDelivery {
  schemaVersion: typeof CODING_DELIVERY_SCHEMA_VERSION;
  type: typeof CODING_CONTENT_TYPE;
  exerciseType: CodingExerciseType;
  environment?: CodingExecutionEnvironment;
  problem?: CodingProblem;
  toolchain: CodingToolchainIdentity;
  files: CodingLearnerFile[];
  publicTests: CodingLearnerTest[];
  limits: CodingLimits;
  entryPoint: CodingEntryPoint;
  allowLearnerCreatedFiles: boolean;
}

export interface CodingSubmissionFile {
  fileId: string;
  path: string;
  encoding: 'text';
  content: string;
}

export interface CodingSubmission {
  schemaVersion: typeof CODING_SUBMISSION_SCHEMA_VERSION;
  files: CodingSubmissionFile[];
}

export type CodingExecutionState = 'completed' | 'partial' | 'executor-error' | 'cancelled' | 'timed-out';

export type CodingTestOutcomeState = 'passed' | 'failed' | 'error' | 'not-run';

export interface CodingTestOutcome {
  testId: string;
  state: CodingTestOutcomeState;
  durationMs: number;
  evidenceRef?: string;
  feedback?: string;
}

export interface CodingExecutionReport {
  schemaVersion: typeof CODING_EXECUTION_REPORT_SCHEMA_VERSION;
  reportId: string;
  executionState: CodingExecutionState;
  toolchain: CodingToolchainIdentity;
  receiptHash?: string;
  outcomes: CodingTestOutcome[];
  failureCode?: string;
}
