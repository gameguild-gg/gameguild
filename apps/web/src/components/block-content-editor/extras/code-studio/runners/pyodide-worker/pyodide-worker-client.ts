/**
 * Main-thread typed client for the Pyodide Web Worker (#111).
 *
 * Responsibilities:
 *  - Correlated request/response over postMessage (id-routed promises).
 *  - Streamed stdout/stderr events for live output and testing-automation
 *    capture (`progress` events are global — emitted between, not during,
 *    runs — so they go through a single listener).
 *  - Lifecycle: lazy boot, `dispose()` terminating the worker, and automatic
 *    worker replacement after a timeout kill (`terminate()` is the only way
 *    to actually stop a blocked WASM interpreter — a Promise.race alone,
 *    like the legacy runner used, leaves the interpreter running).
 *  - Graceful degradation: construction failures surface as
 *    `PyodideWorkerInfrastructureError` so `PythonRunner` can fall back to
 *    the main-thread implementation.
 */

import { createPyodideWorker } from './create-pyodide-worker';
import {
  DEFAULT_PYODIDE_WORKER_CONFIG,
  isPyodideWorkerMessage,
  PyodideExecutionTimeoutError,
  PyodideWorkerInfrastructureError,
  type PyodideExecPayload,
  type PyodideExecResult,
  type PyodideWorkerConfig,
  type PyodideWorkerMessage,
  type PyodideWorkerRequest,
} from './protocol';

interface PendingRun {
  resolve: (result: PyodideExecResult) => void;
  reject: (error: Error) => void;
  onStdout?: (text: string) => void;
  onStderr?: (text: string) => void;
  timeoutTimer: ReturnType<typeof setTimeout> | null;
  timeoutMs: number;
}

export interface PyodideWorkerRunOptions {
  timeoutMs: number;
  onStdout?: (text: string) => void;
  onStderr?: (text: string) => void;
}

/** SIGINT marker Pyodide looks for in the interrupt buffer. */
const INTERRUPT_SIGNAL = 2;
const DEFAULT_TIMEOUT_MS = 30_000;

export class PyodideWorkerClient {
  private worker: Worker | null = null;
  private nextRequestId = 1;
  private pendingRuns = new Map<number, PendingRun>();
  private bootWaiters: Array<{ resolve: () => void; reject: (error: Error) => void }> = [];
  private bootPromise: Promise<void> | null = null;
  private interruptBuffer: Int32Array | null = null;
  private disposed = false;
  private progressListener: ((message: string) => void) | null = null;
  private readonly config: PyodideWorkerConfig;

  constructor(
    config: Partial<PyodideWorkerConfig> = {},
    private readonly workerFactory: () => Worker | null = createPyodideWorker,
  ) {
    this.config = { ...DEFAULT_PYODIDE_WORKER_CONFIG, ...config };
  }

  /** Boots (or re-boots) the worker runtime. Safe to call concurrently. */
  async boot(): Promise<void> {
    this.assertLive();
    if (!this.bootPromise) {
      this.bootPromise = this.bootInternal();
    }
    return this.bootPromise;
  }

  private async bootInternal(): Promise<void> {
    const worker = this.spawnWorker();
    if (!worker) {
      throw new PyodideWorkerInfrastructureError('Web Worker construction failed');
    }

    const id = this.nextRequestId++;
    const posted = new Promise<void>((resolve, reject) => {
      this.bootWaiters.push({ resolve, reject });
      try {
        worker.postMessage({ type: 'boot', id, config: this.config } satisfies PyodideWorkerRequest);
      } catch (error) {
        this.bootWaiters.pop();
        reject(new PyodideWorkerInfrastructureError(`failed to post boot request: ${String(error)}`));
      }
    });
    try {
      await posted;
    } catch (error) {
      // Allow a later retry (e.g. transient network failure loading the runtime).
      this.bootPromise = null;
      throw error;
    }
  }

  private spawnWorker(): Worker | null {
    if (this.worker) return this.worker;
    const worker = this.workerFactory();
    if (!worker) return null;
    this.worker = worker;
    this.interruptBuffer = null;
    worker.addEventListener('message', this.handleMessage);
    worker.addEventListener('error', this.handleError);
    worker.addEventListener('messageerror', this.handleError);
    return worker;
  }

  /**
   * Executes Python code in the worker.
   *
   * On timeout the worker is terminated (the only reliable way to stop a
   * blocked interpreter), the run rejects with `PyodideExecutionTimeoutError`,
   * and the next call transparently boots a fresh worker.
   */
  async run(payload: PyodideExecPayload, options: Partial<PyodideWorkerRunOptions> = {}): Promise<PyodideExecResult> {
    this.assertLive();
    const timeoutMs = options.timeoutMs ?? DEFAULT_TIMEOUT_MS;

    await this.boot();

    const worker = this.worker;
    if (!worker) {
      throw new PyodideWorkerInfrastructureError('worker unavailable after boot');
    }

    const id = this.nextRequestId++;
    return new Promise<PyodideExecResult>((resolve, reject) => {
      const pending: PendingRun = {
        resolve,
        reject,
        onStdout: options.onStdout,
        onStderr: options.onStderr,
        timeoutMs,
        timeoutTimer: setTimeout(() => this.handleTimeout(id), timeoutMs),
      };
      this.pendingRuns.set(id, pending);

      try {
        worker.postMessage({ type: 'run', id, payload } satisfies PyodideWorkerRequest);
      } catch (error) {
        this.rejectRun(id, new PyodideWorkerInfrastructureError(`failed to post run request: ${String(error)}`));
      }
    });
  }

  /**
   * Interrupts the running program. Fast path writes SIGINT directly into the
   * shared interrupt buffer (works even while the worker is busy); the
   * message fallback covers non-SAB environments.
   */
  interrupt(): void {
    const worker = this.worker;
    if (!worker) return;
    if (this.interruptBuffer) {
      this.interruptBuffer[0] = INTERRUPT_SIGNAL;
      return;
    }
    worker.postMessage({ type: 'interrupt', id: this.nextRequestId++ } satisfies PyodideWorkerRequest);
  }

  /** True while a run is in flight (used by tests and diagnostics). */
  get busy(): boolean {
    return this.pendingRuns.size > 0;
  }

  /** Progress events are global; a single listener receives them all. */
  setProgressListener(listener: ((message: string) => void) | null): void {
    this.progressListener = listener;
  }

  /** Terminates the worker and rejects everything still pending. */
  dispose(): void {
    if (this.disposed) return;
    this.disposed = true;
    this.dropWorker();
    const error = new PyodideWorkerInfrastructureError('client disposed');
    for (const pending of this.pendingRuns.values()) {
      this.settlePending(pending, error);
    }
    this.pendingRuns.clear();
    this.failBootWaiters(error);
  }

  private handleTimeout(id: number): void {
    const pending = this.pendingRuns.get(id);
    if (!pending) return;
    // terminate() is what actually stops the blocked interpreter; it also
    // invalidates the worker, so drop it, fail every sibling run (they can
    // never complete now), and let the next run re-boot a fresh worker.
    this.dropWorker();
    const siblings = this.pendingRuns;
    this.pendingRuns = new Map();
    for (const [siblingId, sibling] of siblings) {
      this.settlePending(sibling, siblingId === id ? new PyodideExecutionTimeoutError(sibling.timeoutMs) : new PyodideWorkerInfrastructureError('worker terminated after a sibling run timed out'));
    }
  }

  private dropWorker(): void {
    if (this.worker) {
      this.worker.removeEventListener('message', this.handleMessage);
      this.worker.removeEventListener('error', this.handleError);
      this.worker.removeEventListener('messageerror', this.handleError);
      this.worker.terminate();
    }
    this.worker = null;
    this.interruptBuffer = null;
    this.bootPromise = null;
  }

  private rejectRun(id: number, error: Error): void {
    const pending = this.pendingRuns.get(id);
    if (!pending) return;
    this.pendingRuns.delete(id);
    this.settlePending(pending, error);
  }

  private settlePending(pending: PendingRun, error: Error): void {
    if (pending.timeoutTimer !== null) clearTimeout(pending.timeoutTimer);
    pending.reject(error);
  }

  private failBootWaiters(error: Error): void {
    const waiters = this.bootWaiters;
    this.bootWaiters = [];
    for (const waiter of waiters) {
      waiter.reject(error);
    }
  }

  private handleMessage = (event: MessageEvent<PyodideWorkerMessage>): void => {
    const message = event.data;
    if (!isPyodideWorkerMessage(message)) return;

    switch (message.type) {
      case 'progress':
        this.progressListener?.(message.message);
        return;
      case 'stdout':
      case 'stderr': {
        const pending = this.pendingRuns.get(message.id);
        if (!pending) return;
        if (message.type === 'stdout') pending.onStdout?.(message.text);
        else pending.onStderr?.(message.text);
        return;
      }
      case 'runOk': {
        const pending = this.pendingRuns.get(message.id);
        if (!pending) return;
        this.pendingRuns.delete(message.id);
        if (pending.timeoutTimer !== null) clearTimeout(pending.timeoutTimer);
        pending.resolve(message.result);
        return;
      }
      case 'runError': {
        this.rejectRun(message.id, new PyodideWorkerInfrastructureError(message.error));
        return;
      }
      case 'booted': {
        this.interruptBuffer = message.interruptBuffer ? new Int32Array(message.interruptBuffer) : null;
        // Keep the resolved boot promise: subsequent runs must reuse the
        // booted worker instead of restarting the handshake. It is reset only
        // by failures (bootInternal catch), termination (dropWorker) or dispose.
        const waiters = this.bootWaiters;
        this.bootWaiters = [];
        for (const waiter of waiters) {
          waiter.resolve();
        }
        return;
      }
      case 'bootError': {
        this.bootPromise = null;
        this.dropWorker();
        this.failBootWaiters(new PyodideWorkerInfrastructureError(`boot failed: ${message.error}`));
        return;
      }
      case 'interrupted':
        // Informational only; the interrupted run resolves via runOk.
        return;
    }
  };

  private handleError = (event: ErrorEvent | MessageEvent): void => {
    const detail = 'message' in event && typeof event.message === 'string' ? event.message : 'worker failed';
    const error = new PyodideWorkerInfrastructureError(`worker crashed: ${detail}`);
    this.dropWorker();
    for (const pending of this.pendingRuns.values()) {
      this.settlePending(pending, error);
    }
    this.pendingRuns.clear();
    this.failBootWaiters(error);
  };

  private assertLive(): void {
    if (this.disposed) {
      throw new PyodideWorkerInfrastructureError('client disposed; create a new client');
    }
  }
}
