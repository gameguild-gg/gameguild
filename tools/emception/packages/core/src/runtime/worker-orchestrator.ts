/**
 * WorkerOrchestrator.
 *
 * Assembles `RpcChannel` + `BootHandshake` into a complete main-thread proxy
 * for the Worker-based toolchain. Exposes:
 *
 *   boot()          — handshake (booted / bootError notifications → BootHandshake)
 *   run()           — correlated request/response + per-run stdout/stderr callbacks
 *   getFile()       — correlated VFS read
 *   writeFile()     — correlated VFS write (zero-copy buffer transfer)
 *   listDir()       — correlated VFS directory listing
 *   resetVfs()      — correlated VFS reset
 *   sendStdinByte() — fire-and-forget stdin byte to the worker
 *   dispose()       — tear down channel, cancel pending boot
 *
 * Shell I/O (shellOutput, shellWrite, shellClear, etc.) and SAB stdin pump
 * (stdinRequest / shellStdinRequest handling) are **not** implemented here —
 * they stay in `@emception/browser/worker-client.ts` which is the platform
 * adapter that has access to the DOM IOProvider and SharedArrayBuffer APIs.
 * The orchestrator merely routes those notifications to optional callbacks.
 */

import type {
  BootErrorMessage,
  GetFileResultMessage,
  ListDirResultMessage,
  MainToWorkerMessage,
  ResetVfsResultMessage,
  RunResultMessage,
  WorkerToMainMessage,
  WriteFileResultMessage,
} from '../worker-protocol.js';
import { BootHandshake } from './boot-handshake.js';
import type { RpcTransport } from './rpc-channel.js';
import { RpcChannel } from './rpc-channel.js';

/* ------------------------------------------------------------------ */
/*  Public types                                                        */
/* ------------------------------------------------------------------ */

/**
 * Per-run options accepted by `WorkerOrchestrator.run()`.
 *
 * Deliberately **not** the same as the high-level `RunOptions` in
 * `@emception/core/types.ts`: this is the low-level wire format that maps
 * 1:1 to what goes into `RunMessage` plus the two streaming callbacks that
 * the orchestrator manages locally per-run.
 */
export interface WorkerRunOptions {
  env?: Record<string, string>;
  cwd?: string;
  /** Receive incremental stdout chunks while the run is in progress. */
  onStdout?: (text: string) => void;
  /** Receive incremental stderr chunks while the run is in progress. */
  onStderr?: (text: string) => void;
  /** If true, the worker will allocate a SAB stdin channel and send `stdinRequest`. */
  wantStdin?: boolean;
  /**
   * Called when the worker sends `stdinRequest` for this specific run.
   * Takes priority over `WorkerOrchestratorOptions.onStdinRequest`.
   * Used by platform adapters (e.g. browser `WorkerClient`) to pump SAB bytes
   * for an interactive run without needing a global run-id → handler map.
   */
  onStdinRequest?: (controlBuffer: SharedArrayBuffer, dataBuffer: SharedArrayBuffer) => void;
  /** Opaque hints forwarded to ToolRunner (e.g. which CDN bundles are needed). */
  hints?: { bundlesNeeded?: string[] };
}

/**
 * Minimal tool-execution result returned by `WorkerOrchestrator.run()`.
 * Matches the core `ToolResult` shape (subset — durationMs is always 0
 * since the wire protocol does not include timing).
 */
export interface WorkerToolResult {
  exitCode: number;
  stdout: string;
  stderr: string;
  /** Always `0` — timing is not reported by the worker protocol. */
  durationMs: number;
  /** Always `false` — timeout is enforced by the caller, not the worker. */
  timedOut: boolean;
}

/**
 * Optional callbacks for protocol-level notifications that the orchestrator
 * cannot act on itself (platform-specific concerns).
 */
export interface WorkerOrchestratorOptions {
  /**
   * Called for every incremental stdout chunk, *after* any per-run
   * `onStdout` registered via `run()`.
   */
  onStdout?(id: number, text: string): void;
  /** Called for every incremental stderr chunk. */
  onStderr?(id: number, text: string): void;
  /**
   * Called when the worker sends `stdinRequest` (SAB ring-buffer for a
   * specific run). The browser WorkerClient uses this to pump bytes via
   * `writeByteToSharedChannel`. Node callers may ignore it.
   */
  onStdinRequest?(id: number, controlBuffer: SharedArrayBuffer, dataBuffer: SharedArrayBuffer): void;
  /**
   * Called when the shell sends `shellStdinRequest` (SAB ring-buffer for
   * a foreground interactive WASI run).
   */
  onShellStdinRequest?(controlBuffer: SharedArrayBuffer, dataBuffer: SharedArrayBuffer): void;
  /** Shell emitted a line of output. */
  onShellOutput?(text: string): void;
  /** Shell did a raw write (no trailing newline). */
  onShellWrite?(text: string): void;
  /** Shell wants to clear the terminal. */
  onShellClear?(): void;
  /** Shell changed stdin echo mode. */
  onShellSetEcho?(enabled: boolean): void;
  /** Shell entered or exited exclusive stdin mode. */
  onShellExclusiveStdin?(enter: boolean): void;
  /** Shell wants a single raw byte from the terminal. */
  onShellReadByte?(): void;
  /**
   * Forwarded console message from the Worker.
   * Defaults to routing through `console[level](...args)`.
   */
  onLog?(level: 'log' | 'warn' | 'error' | 'info' | 'debug', args: unknown[]): void;
  /** Transport-level error (Worker `onerror`, MessagePort `messageerror`). */
  onTransportError?(error: unknown): void;
}

/* ------------------------------------------------------------------ */
/*  WorkerOrchestrator                                                  */
/* ------------------------------------------------------------------ */

/**
 * Main-thread orchestrator for the Worker-based toolchain.
 *
 * Constructed with an `RpcTransport` (use `workerTransport(worker)` for
 * browser Web Workers or `messagePortTransport(port)` for Node
 * `worker_threads`).
 *
 * ```ts
 * // Browser:
 * const worker = new Worker(new URL('./worker-entry', import.meta.url), { type: 'module' });
 * const orch = new WorkerOrchestrator(workerTransport(worker), { onShellOutput: (t) => tty.writeLine(t) });
 * await orch.boot('/cdn/manifest.json', { origin: location.origin });
 * const result = await orch.run('clang', ['/home/user/main.c', '-o', '/home/user/a.out']);
 *
 * // Node (worker_threads):
 * import { Worker, MessageChannel } from 'node:worker_threads';
 * const worker = new Worker('./dist/worker-entry.mjs');
 * const orch = new WorkerOrchestrator(workerTransport(worker));
 * await orch.boot(require.resolve('emception/cdn/manifest.json'));
 * ```
 */
export class WorkerOrchestrator {
  private readonly rpc: RpcChannel<MainToWorkerMessage, WorkerToMainMessage>;
  private readonly bootHandshake = new BootHandshake();
  private readonly opts: WorkerOrchestratorOptions;

  /** Per-run callbacks, keyed by run id. */
  private readonly runCallbacks = new Map<
    number,
    {
      onStdout?: (text: string) => void;
      onStderr?: (text: string) => void;
      onStdinRequest?: (ctrl: SharedArrayBuffer, data: SharedArrayBuffer) => void;
    }
  >();

  constructor(transport: RpcTransport, opts: WorkerOrchestratorOptions = {}) {
    this.opts = opts;
    this.rpc = new RpcChannel<MainToWorkerMessage, WorkerToMainMessage>(transport, {
      responseId: extractResponseId,
      onNotification: (msg) => this.handleNotification(msg),
      onTransportError: opts.onTransportError,
    });
  }

  /** `true` after `dispose()` has been called. */
  get isDisposed(): boolean {
    return this.rpc.isDisposed;
  }

  /**
   * Boot the toolchain inside the Worker.
   *
   * Sends a `boot` message and returns a Promise that resolves when the
   * worker emits `booted`, or rejects with a `BootError` when it emits
   * `bootError`.
   *
   * @param manifestUrl  URL of the toolchain manifest JSON.
   * @param opts.origin  Page origin for the worker to resolve relative URLs.
   *   Pass `location.origin` in a browser context; omit (or pass `''`) in Node.
   */
  boot(
    manifestUrl: string,
    opts?: {
      origin?: string;
      toolVersions?: { pythonMajorMinor?: string; pythonMajorMinorCompact?: string };
    },
  ): Promise<void> {
    const promise = this.bootHandshake.start();
    this.rpc.notify({
      type: 'boot',
      manifestUrl,
      origin: opts?.origin ?? '',
      toolVersions: opts?.toolVersions,
    });
    return promise;
  }

  /**
   * Run a tool (emcc, clang, wasi-run, …) inside the Worker.
   *
   * Returns a `WorkerToolResult` when the worker responds with `runResult`.
   * Per-run `onStdout` / `onStderr` callbacks are invoked for each chunk
   * before the final result is resolved.
   */
  async run(tool: string, argv: string[], options: WorkerRunOptions = {}): Promise<WorkerToolResult> {
    let capturedId = -1;

    const responsePromise = this.rpc.request(
      (id) => {
        capturedId = id;
        if (options.onStdout !== undefined || options.onStderr !== undefined || options.onStdinRequest !== undefined) {
          this.runCallbacks.set(id, {
            onStdout: options.onStdout,
            onStderr: options.onStderr,
            onStdinRequest: options.onStdinRequest,
          });
        }
        return {
          type: 'run' as const,
          id,
          tool,
          argv,
          options: {
            env: options.env,
            cwd: options.cwd,
            wantStdin: options.wantStdin ?? false,
            hints: options.hints,
          },
        };
      },
      undefined,
      `run(${tool})`,
    );

    try {
      const msg = (await responsePromise) as RunResultMessage;
      return {
        exitCode: msg.exitCode,
        stdout: msg.stdout,
        stderr: msg.stderr,
        durationMs: 0,
        timedOut: false,
      };
    } finally {
      if (capturedId !== -1) {
        this.runCallbacks.delete(capturedId);
      }
    }
  }

  /**
   * Read a file from the Worker VFS.
   *
   * Returns `null` when the file does not exist.
   */
  async getFile(path: string): Promise<Uint8Array | null> {
    const msg = (await this.rpc.request((id) => ({ type: 'getFile' as const, id, path }), undefined, `getFile(${path})`)) as GetFileResultMessage;
    return msg.data ?? null;
  }

  /**
   * Write a file to the Worker VFS overlay.
   *
   * The buffer is copied before transfer so the caller can reuse `data`
   * after the call returns.
   */
  async writeFile(path: string, data: Uint8Array): Promise<void> {
    const copy = new Uint8Array(data);
    const msg = (await this.rpc.request(
      (id) => ({ type: 'writeFile' as const, id, path, data: copy }),
      [copy.buffer],
      `writeFile(${path})`,
    )) as WriteFileResultMessage;
    if (!msg.ok) {
      throw new Error(msg.error ?? 'writeFile failed');
    }
  }

  /** List the entries in a VFS directory. */
  async listDir(path: string): Promise<string[]> {
    const msg = (await this.rpc.request((id) => ({ type: 'listDir' as const, id, path }), undefined, `listDir(${path})`)) as ListDirResultMessage;
    return msg.entries;
  }

  /** Reset the Worker VFS writable layers (clears /tmp, /home/user). */
  async resetVfs(): Promise<void> {
    const msg = (await this.rpc.request((id) => ({ type: 'resetVfs' as const, id }), undefined, 'resetVfs()')) as ResetVfsResultMessage;
    if (!msg.ok) {
      throw new Error(msg.error ?? 'resetVfs failed');
    }
  }

  /**
   * Send a single stdin byte to the Worker for a specific run.
   *
   * Used by the browser Worker client's `readAndSendByte()` helper
   * (shell `shellReadByte` response path).
   */
  sendStdinByte(id: number, byte: number): void {
    this.rpc.notify({ type: 'stdin', id, byte });
  }

  /**
   * Tear down: cancel any pending boot, dispose the RPC channel (rejecting
   * all in-flight requests), and terminate the transport.
   *
   * Idempotent — safe to call multiple times.
   */
  async dispose(reason?: unknown): Promise<void> {
    if (this.bootHandshake.currentState === 'booting') {
      this.bootHandshake.cancel(reason);
    }
    await this.rpc.dispose(reason);
  }

  /* ---------------------------------------------------------------- */
  /*  Internal — notification routing                                  */
  /* ---------------------------------------------------------------- */

  private handleNotification(msg: WorkerToMainMessage): void {
    switch (msg.type) {
      case 'booted':
        this.bootHandshake.succeed();
        break;

      case 'bootError':
        this.bootHandshake.fail((msg as BootErrorMessage).error);
        break;

      case 'stdout': {
        const cb = this.runCallbacks.get(msg.id);
        cb?.onStdout?.(msg.text);
        this.opts.onStdout?.(msg.id, msg.text);
        break;
      }

      case 'stderr': {
        const cb = this.runCallbacks.get(msg.id);
        cb?.onStderr?.(msg.text);
        this.opts.onStderr?.(msg.id, msg.text);
        break;
      }

      case 'stdinRequest': {
        const stdinCb = this.runCallbacks.get(msg.id);
        if (stdinCb?.onStdinRequest) {
          stdinCb.onStdinRequest(msg.controlBuffer, msg.dataBuffer);
        } else {
          this.opts.onStdinRequest?.(msg.id, msg.controlBuffer, msg.dataBuffer);
        }
        break;
      }

      case 'shellStdinRequest':
        this.opts.onShellStdinRequest?.(msg.controlBuffer, msg.dataBuffer);
        break;

      case 'shellOutput':
        this.opts.onShellOutput?.(msg.text);
        break;

      case 'shellWrite':
        this.opts.onShellWrite?.(msg.text);
        break;

      case 'shellClear':
        this.opts.onShellClear?.();
        break;

      case 'shellSetEcho':
        this.opts.onShellSetEcho?.(msg.enabled);
        break;

      case 'shellExclusiveStdin':
        this.opts.onShellExclusiveStdin?.(msg.enter);
        break;

      case 'shellReadByte':
        this.opts.onShellReadByte?.();
        break;

      case 'log': {
        if (this.opts.onLog) {
          this.opts.onLog(msg.level, msg.args);
        } else {
          console[msg.level](...(msg.args as Parameters<typeof console.log>));
        }
        break;
      }

      default:
        // Exhaustive guard — should not happen if protocol is correct.
        break;
    }
  }
}

/* ------------------------------------------------------------------ */
/*  Helpers                                                             */
/* ------------------------------------------------------------------ */

/**
 * Extract the correlation id from a Worker-to-Main terminal response.
 * Returns `undefined` for notification-only messages (boot events,
 * streaming chunks, shell I/O, logs).
 */
function extractResponseId(msg: WorkerToMainMessage): number | undefined {
  switch (msg.type) {
    case 'runResult':
    case 'getFileResult':
    case 'writeFileResult':
    case 'listDirResult':
    case 'resetVfsResult':
      return (msg as RunResultMessage | GetFileResultMessage | WriteFileResultMessage | ListDirResultMessage | ResetVfsResultMessage).id;
    default:
      return undefined;
  }
}
