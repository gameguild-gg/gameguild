import type {
  CodingAuthoringDefinition,
  CodingAuthoringFile,
  CodingExerciseType,
  CodingFunctionParameter,
  CodingProblem,
  CodingScalarType,
  CodingToolchainIdentity,
  CodingValidationSolution,
  CodingVisibility,
} from './contracts';

export const CODING_DRAFT_TYPE = 'coding-authoring-draft' as const;

export type { CodingProblem, CodingValidationSolution } from './contracts';

export interface CodingFunctionSignature {
  name: string;
  parameters: CodingFunctionParameter[];
  returnType: CodingScalarType;
}

export interface CodingDraftEnvironment {
  language: 'c' | 'cpp';
  runtime: 'wasi' | 'sdl3' | 'raylib' | 'allegro5';
}

/** Text buffers preserve invalid/incomplete numeric and boolean edits for recovery. */
export interface CodingDraftScalar {
  type: CodingScalarType;
  text: string;
}

interface CodingDraftTestBase {
  id: string;
  name: string;
  visibility: CodingVisibility;
  weightUnits: string;
}

export interface CodingDraftStdioTest extends CodingDraftTestBase {
  kind: 'stdio';
  stdin: string;
  expected: { stdout: string | null; stderr: string | null; exitCode: string };
}

export interface CodingDraftFunctionCase {
  id: string;
  inputs: CodingDraftScalar[];
  expected: CodingDraftScalar | null;
}

export interface CodingDraftFunctionTest extends CodingDraftTestBase {
  kind: 'function';
  cases: CodingDraftFunctionCase[];
}

export type CodingDraftTest = CodingDraftStdioTest | CodingDraftFunctionTest;

/** Author-only document for Learning's JSON draft, not an executable definition. */
export interface CodingAuthoringDraft {
  type: typeof CODING_DRAFT_TYPE;
  schemaVersion: 1;
  exerciseType: CodingExerciseType;
  problem: CodingProblem;
  environment: CodingDraftEnvironment;
  toolchain: CodingToolchainIdentity;
  files: CodingAuthoringFile[];
  entryPoint: { path: string };
  functionSignature: CodingFunctionSignature | null;
  tests: CodingDraftTest[];
  validationSolutions: CodingValidationSolution[];
  limits: {
    timeLimitMs: string;
    memoryLimitBytes: string;
    outputLimitBytes: string;
    maxFileCount: string;
  };
  allowLearnerCreatedFiles: boolean;
}

export type CodingDraftChange =
  | { kind: 'family'; exerciseType: CodingExerciseType }
  | { kind: 'signature'; signature: CodingFunctionSignature }
  | { kind: 'environment'; environment: CodingDraftEnvironment }
  | { kind: 'toolchain'; toolchain: CodingToolchainIdentity };

export interface CodingDraftChangePlan {
  baseline: string;
  candidate: CodingAuthoringDraft;
  impact: {
    removedTestIds: string[];
    changedCaseIds: string[];
    filesToReview: string[];
  };
}

/** Host supplies identities; the pure domain never uses storage, routes or random IDs. */
export interface CodingDraftOperations {
  fromDefinition(definition: CodingAuthoringDefinition, environment: CodingDraftEnvironment): CodingAuthoringDraft;
  planChange(draft: CodingAuthoringDraft, change: CodingDraftChange): CodingDraftChangePlan;
  resolveChange(draft: CodingAuthoringDraft, plan: CodingDraftChangePlan, decision: 'confirm' | 'cancel'): CodingAuthoringDraft;
  duplicateTest(draft: CodingAuthoringDraft, testId: string, newId: string, newCaseIds: readonly string[]): CodingAuthoringDraft;
}
