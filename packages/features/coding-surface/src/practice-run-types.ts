import type { CodingLearnerDelivery, CodingScalarValue } from '@game-guild/coding';
import type { CodingPersonalCase, CodingPracticeCase, CodingPracticeSelection } from './learner-practice-types';
import type { CodingRuntimeSessionFactory } from './runtime-types';

/** Public identity is resolved from delivery; a caller cannot replace its oracle. */
export type CodingPracticeRunRequest = { readonly contextKey: string } & (
  { readonly source: 'public'; readonly id: string } | { readonly source: 'personal'; readonly value: CodingPersonalCase }
);

export interface CodingPracticeCheck {
  readonly field: 'stdout' | 'stderr' | 'exitCode' | 'returnValue';
  readonly expected: string;
  readonly actual: string;
  readonly matches: boolean;
}

export interface CodingPracticeRunResult {
  readonly contextKey: string;
  readonly executionId: number;
  readonly selection: CodingPracticeSelection;
  readonly case: CodingPracticeCase;
  readonly observation: {
    readonly stdout: string;
    readonly stderr: string;
    readonly exitCode: number;
    readonly durationMs: number;
    readonly returnValue: CodingScalarValue | null;
  };
  readonly comparison: { readonly status: 'observed' } | { readonly status: 'passed' | 'failed'; readonly checks: readonly CodingPracticeCheck[] };
  readonly academic: false;
}

export type CodingPracticeExecutionState =
  | { readonly status: 'idle' }
  | { readonly status: 'running'; readonly name: string }
  | { readonly status: 'cancelled'; readonly name: string }
  | { readonly status: 'error'; readonly name: string; readonly message: string }
  | { readonly status: 'completed'; readonly result: CodingPracticeRunResult; readonly stale: boolean };

export interface CodingPracticeRunOptions {
  readonly contextKey: string;
  readonly executionId: number;
  readonly delivery: CodingLearnerDelivery;
  readonly request: CodingPracticeRunRequest;
  readonly manifestUrl: string;
  readonly signal: AbortSignal;
  readonly createRuntime?: CodingRuntimeSessionFactory;
}

export interface CodingPracticeRunner {
  (options: CodingPracticeRunOptions): Promise<CodingPracticeRunResult>;
}
