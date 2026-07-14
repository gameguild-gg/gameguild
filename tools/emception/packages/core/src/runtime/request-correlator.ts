/**
 * Request/response correlator.
 *
 * Pure id-based correlation layer extracted from
 * `@emception/browser/worker-client.ts`. Generic over the request and response
 * envelope shapes; knows nothing about WASM, workspaces, or DOM.
 *
 * Responsibilities (intentionally narrow):
 *   - Allocate monotonically increasing numeric request ids.
 *   - Track pending Promises keyed by id.
 *   - Resolve / reject pending Promises when responses arrive.
 *   - Reject every outstanding request when the channel terminates.
 *
 * Out of scope:
 *   - Streaming notifications (e.g. incremental stdout chunks). Those should
 *     be routed by the caller before reaching the correlator — the correlator
 *     only handles terminal responses.
 *   - Transport (postMessage / MessagePort / fetch). Caller wires that up.
 *
 * Validated with real `node:worker_threads` `MessageChannel`s — no mocks.
 */

import { EmceptionError } from '../errors';

/**
 * Default error raised when `dispose()` is called with outstanding requests
 * and the caller did not supply a custom one.
 */
export class CorrelatorDisposedError extends EmceptionError {
  constructor(message = 'RequestCorrelator disposed before response arrived') {
    super(message);
    this.name = 'CorrelatorDisposedError';
  }
}

interface PendingRequest<TResponse> {
  resolve: (value: TResponse) => void;
  reject: (reason: unknown) => void;
  /** Optional human-readable label for diagnostics (e.g. `'run'`, `'getFile'`). */
  label?: string;
}

export interface RequestCorrelatorOptions {
  /** Starting id. Defaults to `1`. */
  startId?: number;
}

/**
 * Generic request/response correlator.
 *
 * Typical use:
 *
 * ```ts
 * const corr = new RequestCorrelator<MyResponse>();
 * const { id, promise } = corr.allocate('myCall');
 * channel.postMessage({ type: 'myCall', id, args });
 * channel.onmessage = (ev) => corr.complete(ev.data.id, ev.data.payload);
 * const result = await promise;
 * ```
 */
export class RequestCorrelator<TResponse = unknown> {
  private nextId: number;
  private readonly pending = new Map<number, PendingRequest<TResponse>>();
  private disposed = false;

  constructor(opts: RequestCorrelatorOptions = {}) {
    this.nextId = opts.startId ?? 1;
  }

  /** Number of outstanding (unresolved) requests. */
  get pendingCount(): number {
    return this.pending.size;
  }

  /**
   * Allocate a new request slot. Returns the assigned id and a Promise that
   * resolves when `complete(id, value)` is called with that id.
   *
   * Throws if the correlator has been disposed.
   */
  allocate(label?: string): { id: number; promise: Promise<TResponse> } {
    if (this.disposed) {
      throw new CorrelatorDisposedError('Cannot allocate request: RequestCorrelator already disposed.');
    }
    const id = this.nextId++;
    const promise = new Promise<TResponse>((resolve, reject) => {
      this.pending.set(id, { resolve, reject, label });
    });
    return { id, promise };
  }

  /**
   * Resolve the pending request with the given id. Returns true if a
   * matching pending request was found, false otherwise (stale response).
   */
  complete(id: number, value: TResponse): boolean {
    const entry = this.pending.get(id);
    if (!entry) return false;
    this.pending.delete(id);
    entry.resolve(value);
    return true;
  }

  /**
   * Reject the pending request with the given id. Returns true if a
   * matching pending request was found, false otherwise.
   */
  fail(id: number, reason: unknown): boolean {
    const entry = this.pending.get(id);
    if (!entry) return false;
    this.pending.delete(id);
    entry.reject(reason);
    return true;
  }

  /**
   * Reject every outstanding request and mark the correlator as disposed.
   * Subsequent `allocate()` calls throw; subsequent `complete()` /
   * `fail()` calls return false.
   */
  dispose(reason?: unknown): void {
    if (this.disposed) return;
    this.disposed = true;
    const err = reason ?? new CorrelatorDisposedError();
    for (const [, entry] of this.pending) {
      entry.reject(err);
    }
    this.pending.clear();
  }

  /** True iff `dispose()` has been called. */
  get isDisposed(): boolean {
    return this.disposed;
  }
}
