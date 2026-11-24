import type { CodingAuthoringDraft, CodingValidationSolution } from '@game-guild/coding';
import type { CodingRuntimeSessionFactory } from './runtime-types';
import type { CodingPreviewResult } from './types';

interface SolutionIdentity {
  readonly solutionId: string;
  readonly name: string;
  readonly role: CodingValidationSolution['role'];
}

/** Technical exercise preparation only; there is deliberately no academic score. */
export type CodingSolutionCheck = SolutionIdentity &
  (
    | {
        readonly status: 'reference-passed' | 'reference-failed' | 'incorrect-detected' | 'incorrect-undetected';
        readonly report: CodingPreviewResult['report'];
      }
    | { readonly status: 'error'; readonly message: string }
  );

export interface CodingExerciseValidationState {
  readonly operation: 'idle' | 'running' | 'stopping' | 'disposed';
  readonly activeSolutionId: string | null;
  readonly results: readonly CodingSolutionCheck[];
  readonly stale: boolean;
  readonly error: string | null;
}

export interface CodingExerciseValidationOptions {
  readonly contextKey: string;
  readonly draft: CodingAuthoringDraft;
  readonly manifestUrl: string;
  /** Read at execution time, after a previous preview/context begins teardown. */
  readonly beforeExecution?: () => Promise<void>;
  readonly createRuntime?: CodingRuntimeSessionFactory;
}

export interface CodingExerciseValidationSession {
  getState(): CodingExerciseValidationState;
  subscribe(listener: () => void): () => void;
  matches(draft: CodingAuthoringDraft): boolean;
  update(draft: CodingAuthoringDraft): Promise<void>;
  run(solutionIds?: readonly string[]): Promise<void>;
  stop(): Promise<void>;
  dispose(): Promise<void>;
}
