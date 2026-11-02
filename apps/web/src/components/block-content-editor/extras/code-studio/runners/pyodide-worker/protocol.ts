/**
 * Typed message protocol for the Pyodide Web Worker bridge (#111).
 *
 * The main thread talks to `pyodide.worker.ts` exclusively through these
 * discriminated unions, so both sides share one compile-time-checked
 * contract — no untyped `jsdelivr` script embedding, no `any` plumbing.
 *
 * Requests are correlated by `id` (monotonic on the main thread). Streamed
 * `stdout`/`stderr` chunks and the terminal `runOk`/`runError` response all
 * carry the originating run's `id`.
 */

/** Worker chunk name; must match the `name` passed to the Worker constructor. */
export const PYODIDE_WORKER_NAME = 'pyodide-runtime';

export interface PyodideWorkerConfig {
  /** URL of the gzipped Pyodide bootstrap script (fetch + gunzip + blob import). */
  scriptUrl: string;
  /** `indexURL` handed to `loadPyodide` — where the runtime fetches its .wasm/.data files. */
  indexUrl: string;
}

export const DEFAULT_PYODIDE_WORKER_CONFIG: PyodideWorkerConfig = {
  // Same assets the legacy main-thread runner loads, so deployments that
  // provision `/langs/pyodide.js.gz` today keep working unchanged.
  scriptUrl: '/langs/pyodide.js.gz',
  indexUrl: '/langs/',
};

export interface PyodideExecPayload {
  /** Python source to execute (single-file runs). */
  code: string;
  /** Batched stdin replayed line-by-line into `pyodide.setStdin`. */
  stdin?: string;
  /** Multi-file projects: mapped into the Pyodide MEMFS before execution. */
  files?: Record<string, string>;
  /** Entry file for multi-file runs; when set, `code` is ignored. */
  entryPoint?: string;
}

export interface PyodideExecResult {
  stdout: string;
  stderr: string;
  exitCode: number;
}

/** Main thread -> Worker. */
export type PyodideWorkerRequest =
  | { type: 'boot'; id: number; config: PyodideWorkerConfig }
  | { type: 'run'; id: number; payload: PyodideExecPayload }
  | { type: 'interrupt'; id: number };

/**
 * Worker -> main thread. `interruptBuffer` is a SharedArrayBuffer the main
 * thread writes `2` (SIGINT) into to stop a running program; `null` when
 * SharedArrayBuffer is unavailable (non-COI pages) and the `interrupt`
 * message fallback must be used instead.
 */
export type PyodideWorkerResponse =
  | { type: 'booted'; id: number; interruptBuffer: SharedArrayBuffer | null }
  | { type: 'bootError'; id: number; error: string }
  | { type: 'runOk'; id: number; result: PyodideExecResult }
  | { type: 'runError'; id: number; error: string }
  | { type: 'interrupted'; id: number };

/** Worker -> main thread, streamed during a run (no reply expected). */
export type PyodideWorkerEvent = { type: 'stdout'; id: number; text: string } | { type: 'stderr'; id: number; text: string } | { type: 'progress'; message: string };

export type PyodideWorkerMessage = PyodideWorkerResponse | PyodideWorkerEvent;

/** Sentinel marker for worker-infrastructure failures (not user-code errors). */
export const PYODIDE_WORKER_ERROR_PREFIX = '[pyodide-worker]';

export class PyodideWorkerInfrastructureError extends Error {
  constructor(message: string) {
    super(`${PYODIDE_WORKER_ERROR_PREFIX} ${message}`);
    this.name = 'PyodideWorkerInfrastructureError';
  }
}

export class PyodideExecutionTimeoutError extends Error {
  constructor(timeoutMs: number) {
    super(`Execution timeout after ${timeoutMs}ms; worker was terminated and will be reloaded`);
    this.name = 'PyodideExecutionTimeoutError';
  }
}

/** Type guard: did a failure come from the worker plumbing rather than user code? */
export function isPyodideWorkerInfrastructureError(error: unknown): boolean {
  return error instanceof PyodideWorkerInfrastructureError || error instanceof PyodideExecutionTimeoutError;
}

/** Type guard for incoming worker messages (defensive against malformed posts). */
export function isPyodideWorkerMessage(data: unknown): data is PyodideWorkerMessage {
  if (typeof data !== 'object' || data === null) return false;
  const type = (data as { type?: unknown }).type;
  return (
    type === 'booted' ||
    type === 'bootError' ||
    type === 'runOk' ||
    type === 'runError' ||
    type === 'interrupted' ||
    type === 'stdout' ||
    type === 'stderr' ||
    type === 'progress'
  );
}
