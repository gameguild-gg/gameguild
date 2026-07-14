/**
 * RpcChannel.
 *
 * Thin transport-agnostic wrapper that pairs a `RequestCorrelator` with a
 * postMessage-style channel (Web Worker, Node `MessagePort`, anything with
 * `postMessage(msg)` + a message subscription). Knows how to:
 *
 *   - Allocate an id, post a request envelope, await the matching reply.
 *   - Route incoming envelopes either to the correlator (terminal responses)
 *     or to a caller-supplied notification handler (streaming/incremental
 *     messages — stdout chunks, stdin requests, log forwards).
 *   - Tear down: terminate the channel (if it has `terminate()`), unsubscribe
 *     the listener, and dispose the correlator (rejecting any in-flight
 *     requests).
 *
 * Out of scope:
 *   - Knowing the message schema. Caller supplies request type, response type,
 *     and a `responseId` extractor that returns the correlation id (or
 *     `undefined` for notifications).
 *
 * Validated with real `node:worker_threads` `MessageChannel`s — no mocks.
 */

import { CorrelatorDisposedError, RequestCorrelator } from './request-correlator.js';

/**
 * Minimal channel surface RpcChannel needs. Both `globalThis.Worker` and
 * Node `MessagePort` satisfy the duck-type via small adapters.
 */
export interface RpcTransport {
  /** Send a message envelope. Optional transferables list (browser/Node parity). */
  postMessage(message: unknown, transfer?: unknown[]): void;
  /** Subscribe to incoming messages. Return an unsubscribe function. */
  onMessage(handler: (message: unknown) => void): () => void;
  /** Optional graceful shutdown (Worker.terminate, MessagePort.close). */
  terminate?(): void | Promise<void>;
}

export interface RpcChannelOptions<TResponse> {
  /**
   * Inspect an incoming message and return its correlation id if it is a
   * terminal response, or `undefined` if it is a notification (streaming
   * chunk, log forward, etc.) that should be delivered to `onNotification`.
   */
  responseId(message: TResponse): number | undefined;
  /**
   * Optional handler for notifications (messages with no response id).
   * Defaults to a no-op.
   */
  onNotification?(message: TResponse): void;
  /**
   * Optional handler for non-correlated errors surfaced by the transport
   * (e.g. Worker `onerror`). Defaults to logging via `console.error`.
   */
  onTransportError?(error: unknown): void;
  /** Starting id for the underlying correlator. Defaults to `1`. */
  startId?: number;
}

/**
 * Generic request/response channel built on top of `RequestCorrelator`.
 *
 * Typical use:
 *
 * ```ts
 * const rpc = new RpcChannel<MyMsg, MyMsg>(transport, {
 *   responseId: (m) => (m.kind === 'response' ? m.id : undefined),
 *   onNotification: (m) => routeNotification(m),
 * });
 * const reply = await rpc.request((id) => ({ kind: 'request', id, op: 'foo' }));
 * await rpc.dispose();
 * ```
 */
export class RpcChannel<TRequest = unknown, TResponse = unknown> {
  private readonly transport: RpcTransport;
  private readonly correlator: RequestCorrelator<TResponse>;
  private readonly unsubscribe: () => void;
  private readonly opts: RpcChannelOptions<TResponse>;
  private disposed = false;

  constructor(transport: RpcTransport, opts: RpcChannelOptions<TResponse>) {
    this.transport = transport;
    this.opts = opts;
    this.correlator = new RequestCorrelator<TResponse>({ startId: opts.startId });
    this.unsubscribe = transport.onMessage((raw) => {
      const msg = raw as TResponse;
      const id = opts.responseId(msg);
      if (id !== undefined) {
        this.correlator.complete(id, msg);
      } else {
        opts.onNotification?.(msg);
      }
    });
  }

  /** Number of in-flight requests awaiting a response. */
  get inFlightCount(): number {
    return this.correlator.pendingCount;
  }

  get isDisposed(): boolean {
    return this.disposed;
  }

  /**
   * Issue a request and await its terminal response.
   *
   * The `build` callback receives the freshly allocated id so the caller can
   * embed it in the request envelope. Optional `transfer` array is forwarded
   * to `postMessage` for zero-copy ArrayBuffer / MessagePort handoff.
   */
  async request(build: (id: number) => TRequest, transfer?: unknown[], label?: string): Promise<TResponse> {
    if (this.disposed) {
      throw new CorrelatorDisposedError('Cannot issue request: RpcChannel already disposed.');
    }
    const { id, promise } = this.correlator.allocate(label);
    this.transport.postMessage(build(id), transfer);
    return promise;
  }

  /**
   * Send a fire-and-forget message (no correlation id, no response). Useful
   * for notifications back to the worker (e.g. stdin bytes, cancellation
   * signals).
   */
  notify(message: TRequest, transfer?: unknown[]): void {
    if (this.disposed) {
      throw new CorrelatorDisposedError('Cannot notify: RpcChannel already disposed.');
    }
    this.transport.postMessage(message, transfer);
  }

  /**
   * Surface a transport-level error (caller wires this from Worker `onerror`
   * / MessagePort `messageerror`). Routes through `onTransportError`.
   */
  reportTransportError(error: unknown): void {
    if (this.opts.onTransportError) {
      this.opts.onTransportError(error);
    } else {
      console.error('[Emception:RpcChannel] transport error', error);
    }
  }

  /**
   * Tear down: unsubscribe the listener, dispose the correlator (rejecting
   * every in-flight request), and call `transport.terminate()` if available.
   * Idempotent.
   */
  async dispose(reason?: unknown): Promise<void> {
    if (this.disposed) return;
    this.disposed = true;
    this.unsubscribe();
    this.correlator.dispose(reason);
    if (this.transport.terminate) {
      await this.transport.terminate();
    }
  }
}

/* ------------------------------------------------------------------------- */
/*  Transport adapters                                                        */
/* ------------------------------------------------------------------------- */

/**
 * Wrap a Web `Worker` (or anything with `postMessage` + `addEventListener`)
 * as an `RpcTransport`. Subscribes via `addEventListener('message', …)` and
 * returns a matching `removeEventListener` unsubscribe.
 */
export function workerTransport(worker: {
  postMessage(message: unknown, transfer?: Transferable[]): void;
  addEventListener(type: 'message', listener: (ev: MessageEvent) => void): void;
  removeEventListener(type: 'message', listener: (ev: MessageEvent) => void): void;
  terminate?(): void;
}): RpcTransport {
  return {
    postMessage(message, transfer) {
      worker.postMessage(message, (transfer ?? []) as Transferable[]);
    },
    onMessage(handler) {
      const listener = (ev: MessageEvent) => handler(ev.data);
      worker.addEventListener('message', listener);
      return () => worker.removeEventListener('message', listener);
    },
    terminate: worker.terminate ? () => worker.terminate!() : undefined,
  };
}

/**
 * Wrap a Node `MessagePort` (from `node:worker_threads`) as an `RpcTransport`.
 * Subscribes via `port.on('message', …)` and returns an `off('message', …)`
 * unsubscribe; calls `port.close()` on terminate.
 */
export function messagePortTransport(port: {
  postMessage(message: unknown, transfer?: unknown[]): void;
  on(event: 'message', listener: (msg: unknown) => void): void;
  off(event: 'message', listener: (msg: unknown) => void): void;
  close(): void;
}): RpcTransport {
  return {
    postMessage(message, transfer) {
      port.postMessage(message, transfer);
    },
    onMessage(handler) {
      const listener = (msg: unknown) => handler(msg);
      port.on('message', listener);
      return () => port.off('message', listener);
    },
    terminate: () => port.close(),
  };
}
