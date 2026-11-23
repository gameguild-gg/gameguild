import type { CodingLearnerDelivery } from '@game-guild/coding';
import type { CodingRuntimeSessionFactory } from './runtime-types';

export type CodingProgramInput =
  | { readonly kind: 'batch'; readonly text: string }
  | { readonly kind: 'interactive' };

export type CodingProgramFailureReason =
  | 'unsupported-context' | 'runtime-unavailable' | 'compile-failed' | 'link-failed'
  | 'preparation-timeout' | 'execution-timeout' | 'output-limit' | 'input-limit'
  | 'runtime-failed' | 'cleanup-failed';

interface CodingProgramStateBase {
  readonly candidateKey: string;
  readonly output: { readonly stdout: string; readonly stderr: string; readonly truncated: boolean };
  /** The public stdin bridge awaits bytes; it may prefetch before native fd_read. */
  readonly waitingForInput: boolean;
}

/** Program diagnostics only: an exit code is not a test verdict. */
export type CodingProgramSessionState = CodingProgramStateBase & (
  | { readonly status: 'idle' | 'loading' | 'building' | 'running' | 'stopping' | 'stopped' | 'disposed' }
  | { readonly status: 'completed'; readonly exitCode: number }
  | { readonly status: 'failed'; readonly reason: CodingProgramFailureReason }
);

export interface CodingProgramSessionOptions {
  readonly candidateKey: string;
  /** Already authorized public projection with exact editor bytes. */
  readonly delivery: CodingLearnerDelivery;
  readonly manifestUrl: string;
  readonly createRuntime?: CodingRuntimeSessionFactory;
}

export interface CodingProgramSession {
  getState(): CodingProgramSessionState;
  /** Lazy; compilation and student execution use separate bounded budgets. */
  run(input: CodingProgramInput): Promise<void>;
  /** UTF-8 bytes, unchanged. Only accepted during interactive program execution. */
  writeInput(text: string): void;
  /** End interactive stdin without stopping the program. */
  endInput(): void;
  /** Cancellation is immediate, but acknowledgement waits for actual disposal. */
  stop(): Promise<void>;
  dispose(): Promise<void>;
}

export interface CodingProgramSessionFactory {
  (options: CodingProgramSessionOptions): CodingProgramSession;
}
