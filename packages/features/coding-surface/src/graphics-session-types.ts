import type { CodingLearnerDelivery } from '@game-guild/coding';
import type { CodingRuntimeSessionFactory } from './runtime-types';

export type CodingGraphicsFailureReason =
  | 'unsupported-context'
  | 'runtime-unavailable'
  | 'compile-failed'
  | 'link-failed'
  | 'preparation-timeout'
  | 'execution-timeout'
  | 'output-limit'
  | 'runtime-failed'
  | 'cleanup-failed';

export interface CodingGraphicsOutput {
  readonly stdout: string;
  readonly stderr: string;
  readonly truncated: boolean;
}

interface CodingGraphicsStateBase {
  readonly candidateKey: string;
  readonly output: CodingGraphicsOutput;
}

/** Diagnostic rendering state, never a case verdict or academic score. */
export type CodingGraphicsSessionState = CodingGraphicsStateBase & (
  | { readonly status: 'idle' | 'loading' | 'building' | 'running' | 'stopping' | 'stopped' | 'disposed' }
  | { readonly status: 'failed'; readonly reason: CodingGraphicsFailureReason }
);

export interface CodingGraphicsSessionOptions {
  /** The host changes this key/session whenever candidate files/revision change. */
  readonly candidateKey: string;
  /** Public, already authorized projection with the exact editor file contents. */
  readonly delivery: CodingLearnerDelivery;
  readonly manifestUrl: string;
  readonly createRuntime?: CodingRuntimeSessionFactory;
}

export interface CodingGraphicsSession {
  getState(): CodingGraphicsSessionState;
  /** Lazy. The supported SDL artifact requires the unique connected #canvas target. */
  run(canvas: HTMLCanvasElement): Promise<void>;
  /** Settles only after the previous operation and runtime release ownership. */
  stop(): Promise<void>;
  dispose(): Promise<void>;
}

export interface CodingGraphicsSessionFactory {
  (options: CodingGraphicsSessionOptions): CodingGraphicsSession;
}
