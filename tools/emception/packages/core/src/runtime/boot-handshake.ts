/**
 * BootHandshake.
 *
 * Tiny state machine for the worker boot lifecycle, extracted from
 * `@emception/browser/worker-client.ts` (`bootResolve` / `bootReject` pair).
 *
 * The worker emits exactly one of `booted` or `bootError`; `BootHandshake`
 * tracks the corresponding Promise, enforces the once-only contract, and
 * exposes a `cancel()` so callers can abort a hung boot when the surrounding
 * lifecycle (e.g. `RpcChannel.dispose()`) tears down.
 *
 * Pure logic — no transport, no protocol.
 */

import { EmceptionError } from '../errors';

export class BootError extends EmceptionError {
  constructor(message: string) {
    super(message);
    this.name = 'BootError';
  }
}

export class BootCancelledError extends EmceptionError {
  constructor(message = 'Worker boot cancelled before completion') {
    super(message);
    this.name = 'BootCancelledError';
  }
}

type State = 'idle' | 'booting' | 'booted' | 'failed' | 'cancelled';

export class BootHandshake {
  private state: State = 'idle';
  private resolveFn: (() => void) | null = null;
  private rejectFn: ((reason: unknown) => void) | null = null;
  private settled: Promise<void> | null = null;

  /** Current state for diagnostics / tests. */
  get currentState(): State {
    return this.state;
  }

  /**
   * Begin a boot attempt. Returns the Promise the caller awaits. Calling
   * `start()` while a previous attempt is still pending throws — the
   * handshake is single-shot per instance.
   */
  start(): Promise<void> {
    if (this.state !== 'idle') {
      throw new BootError(`BootHandshake.start() invalid in state '${this.state}'.`);
    }
    this.state = 'booting';
    this.settled = new Promise<void>((resolve, reject) => {
      this.resolveFn = resolve;
      this.rejectFn = reject;
    });
    return this.settled;
  }

  /** Mark the boot as successful. Once-only; further calls are no-ops. */
  succeed(): void {
    if (this.state !== 'booting') return;
    this.state = 'booted';
    this.resolveFn?.();
    this.clearCallbacks();
  }

  /** Mark the boot as failed with a structured error. Once-only. */
  fail(reason: string | Error): void {
    if (this.state !== 'booting') return;
    this.state = 'failed';
    const err = reason instanceof Error ? reason : new BootError(reason);
    this.rejectFn?.(err);
    this.clearCallbacks();
  }

  /**
   * Cancel an in-flight boot (e.g. parent lifecycle teardown). Rejects with
   * `BootCancelledError` unless a custom reason is supplied. Once-only.
   */
  cancel(reason?: unknown): void {
    if (this.state !== 'booting') return;
    this.state = 'cancelled';
    this.rejectFn?.(reason ?? new BootCancelledError());
    this.clearCallbacks();
  }

  /** True iff `succeed()` has resolved the handshake. */
  get isBooted(): boolean {
    return this.state === 'booted';
  }

  /** True iff the handshake has reached a terminal state (booted/failed/cancelled). */
  get isSettled(): boolean {
    return this.state === 'booted' || this.state === 'failed' || this.state === 'cancelled';
  }

  private clearCallbacks(): void {
    this.resolveFn = null;
    this.rejectFn = null;
  }
}
