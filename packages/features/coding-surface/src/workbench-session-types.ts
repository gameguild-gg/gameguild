import type { CodingProgramInput, CodingProgramSessionFactory, CodingProgramSessionState } from './program-session-types';
import type { CodingRuntimeSessionFactory } from './runtime-types';
import type { CodingEditorController, CodingPreviewResult, CodingSurfaceDefinition, CodingSurfaceMode, CodingSurfaceSession } from './types';

export interface CodingWorkbenchState {
  readonly candidateKey: string;
  readonly executionId: number;
  readonly operation: 'idle' | 'tests' | 'program' | 'stopping' | 'disposed';
  readonly result: CodingPreviewResult | null;
  readonly stale: boolean;
  readonly programStale: boolean;
  readonly program: CodingProgramSessionState | null;
  readonly error: string | null;
}

export interface CodingWorkbenchSessionOptions {
  readonly candidateKey: string;
  readonly definition: CodingSurfaceDefinition;
  readonly mode: CodingSurfaceMode;
  readonly editor: Pick<CodingEditorController, 'getFiles'>;
  readonly manifestUrl: string;
  readonly maxScore?: number;
  readonly passingScore?: number;
  /** A previous context's teardown must finish before acquiring another runtime. */
  readonly beforeExecution?: Promise<void>;
  readonly createRuntime?: CodingRuntimeSessionFactory;
  readonly createProgram?: CodingProgramSessionFactory;
}

/** Local diagnostics only. Official submission/review/release belong to the host. */
export interface CodingWorkbenchSession extends CodingSurfaceSession {
  getState(): CodingWorkbenchState;
  subscribe(listener: () => void): () => void;
  runProgram(input: CodingProgramInput): Promise<void>;
  writeInput(text: string): void;
  endInput(): void;
  /** Edits invalidate captured results and cancel an older execution. */
  invalidate(): Promise<void>;
  stop(): Promise<void>;
  dispose(): Promise<void>;
}
