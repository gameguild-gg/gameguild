/**
 * Main-thread proxy client for the Worker-based toolchain.
 *
 * Mirrors the ToolRunner + VFS API surface but forwards all calls to the
 * Worker via postMessage. The IDE and shell interact with this class as if
 * the toolchain were running on the main thread.
 */

import type { RunOptions, ToolResult } from './tool-runner';
import type { IOProvider } from './tty/io-provider';
import type {
    MainToWorkerMessage,
    WorkerToMainMessage,
} from './worker-protocol';

export interface WorkerBootOptions {
    manifestUrl: string;
    /** An IOProvider (typically TTYBridge) for routing shell I/O. */
    io: IOProvider & {
        enterExclusiveStdin?(): void;
        exitExclusiveStdin?(): void;
        readByteExclusive?(): number | null | Promise<number>;
    };
    toolVersions?: { pythonMajorMinor?: string; pythonMajorMinorCompact?: string };
}

/**
 * Pending request tracker.
 * Each run/getFile/writeFile/listDir call gets a unique id;
 * the Worker responses are correlated by id.
 */
interface PendingRequest<T> {
    resolve: (value: T) => void;
    reject: (reason: unknown) => void;
}

export class WorkerClient {
    private worker: Worker;
    private io: WorkerBootOptions['io'];
    private nextId = 1;
    private pending = new Map<number, PendingRequest<unknown>>();

    /** Callbacks for incremental stdout/stderr during a run. */
    private runCallbacks = new Map<number, { onStdout?: (t: string) => void; onStderr?: (t: string) => void }>();

    /** Per-run stdin feed functions. */
    private stdinFeeds = new Map<number, () => number | null | Promise<number>>();

    private bootResolve: ((value: void) => void) | null = null;
    private bootReject: ((reason: unknown) => void) | null = null;

    constructor(worker: Worker, io: WorkerBootOptions['io']) {
        this.worker = worker;
        this.io = io;

        this.worker.onmessage = (ev: MessageEvent<WorkerToMainMessage>) => {
            this.handleMessage(ev.data);
        };

        this.worker.onerror = (ev) => {
            console.error('[Emception:WorkerClient] Worker error:', ev);
        };
    }

    /* ---------------------------------------------------------------- */
    /*  Boot                                                             */
    /* ---------------------------------------------------------------- */

    boot(manifestUrl: string, toolVersions?: WorkerBootOptions['toolVersions']): Promise<void> {
        return new Promise<void>((resolve, reject) => {
            this.bootResolve = resolve;
            this.bootReject = reject;
            this.send({ type: 'boot', manifestUrl, origin: self.location.origin, toolVersions });
        });
    }

    /* ---------------------------------------------------------------- */
    /*  ToolRunner-compatible API                                        */
    /* ---------------------------------------------------------------- */

    async run(tool: string, argv: string[], options: RunOptions = {}): Promise<ToolResult> {
        const id = this.nextId++;

        // Store callbacks for incremental output
        if (options.onStdout || options.onStderr) {
            this.runCallbacks.set(id, {
                onStdout: options.onStdout,
                onStderr: options.onStderr,
            });
        }

        // Store stdin provider
        if (options.stdin) {
            this.stdinFeeds.set(id, options.stdin);
        }

        return new Promise<ToolResult>((resolve, reject) => {
            this.pending.set(id, { resolve: resolve as (v: unknown) => void, reject });
            this.send({
                type: 'run',
                id,
                tool,
                argv,
                options: {
                    env: options.env,
                    cwd: options.cwd,
                    wantStdin: !!options.stdin,
                },
            });
        });
    }

    async getFile(path: string): Promise<Uint8Array | null> {
        const id = this.nextId++;
        return new Promise<Uint8Array | null>((resolve, reject) => {
            this.pending.set(id, { resolve: resolve as (v: unknown) => void, reject });
            this.send({ type: 'getFile', id, path });
        });
    }

    async writeFile(path: string, data: Uint8Array): Promise<void> {
        const id = this.nextId++;
        // Transfer the buffer for zero-copy
        const copy = new Uint8Array(data);
        return new Promise<void>((resolve, reject) => {
            this.pending.set(id, {
                resolve: () => resolve(),
                reject,
            });
            this.worker.postMessage(
                { type: 'writeFile', id, path, data: copy } satisfies MainToWorkerMessage,
                [copy.buffer],
            );
        });
    }

    async listDir(path: string): Promise<string[]> {
        const id = this.nextId++;
        return new Promise<string[]>((resolve, reject) => {
            this.pending.set(id, { resolve: resolve as (v: unknown) => void, reject });
            this.send({ type: 'listDir', id, path });
        });
    }

    /* ---------------------------------------------------------------- */
    /*  Cleanup                                                          */
    /* ---------------------------------------------------------------- */

    terminate(): void {
        this.worker.terminate();
        for (const [, p] of this.pending) {
            p.reject(new Error('Worker terminated'));
        }
        this.pending.clear();
        this.runCallbacks.clear();
        // Release any pending exclusive-stdin readers so the feedStdin loop
        // exits immediately and keyboard input returns to normal (Monaco editor).
        this.stdinFeeds.clear();
        this.io.setStdinEcho?.(false);
        this.io.exitExclusiveStdin?.();
    }

    /* ---------------------------------------------------------------- */
    /*  Internal                                                         */
    /* ---------------------------------------------------------------- */

    private send(msg: MainToWorkerMessage): void {
        this.worker.postMessage(msg);
    }

    private handleMessage(msg: WorkerToMainMessage): void {
        switch (msg.type) {
            case 'booted':
                this.bootResolve?.();
                this.bootResolve = null;
                this.bootReject = null;
                break;

            case 'bootError':
                this.bootReject?.(new Error(msg.error));
                this.bootResolve = null;
                this.bootReject = null;
                break;

            case 'stdout': {
                const cb = this.runCallbacks.get(msg.id);
                cb?.onStdout?.(msg.text);
                break;
            }

            case 'stderr': {
                const cb = this.runCallbacks.get(msg.id);
                cb?.onStderr?.(msg.text);
                break;
            }

            case 'stdinRequest':
                this.feedStdin(msg.id);
                break;

            case 'runResult': {
                const p = this.pending.get(msg.id);
                if (p) {
                    this.pending.delete(msg.id);
                    this.runCallbacks.delete(msg.id);
                    // Immediately restore normal input mode if this run used stdin,
                    // so the feed loop's pending readByteExclusive() is cancelled
                    // and doesn't swallow the user's next keystroke.
                    if (this.stdinFeeds.has(msg.id)) {
                        this.stdinFeeds.delete(msg.id);
                        this.io.setStdinEcho?.(false);
                        this.io.exitExclusiveStdin?.();
                    }
                    p.resolve({
                        exitCode: msg.exitCode,
                        stdout: msg.stdout,
                        stderr: msg.stderr,
                    } as ToolResult);
                }
                break;
            }

            case 'getFileResult': {
                const p = this.pending.get(msg.id);
                if (p) {
                    this.pending.delete(msg.id);
                    p.resolve(msg.data);
                }
                break;
            }

            case 'writeFileResult': {
                const p = this.pending.get(msg.id);
                if (p) {
                    this.pending.delete(msg.id);
                    if (msg.ok) {
                        p.resolve(undefined);
                    } else {
                        p.reject(new Error(msg.error ?? 'writeFile failed'));
                    }
                }
                break;
            }

            case 'listDirResult': {
                const p = this.pending.get(msg.id);
                if (p) {
                    this.pending.delete(msg.id);
                    p.resolve(msg.entries);
                }
                break;
            }

            // Shell I/O — proxy to the main-thread IOProvider (xterm.js)
            case 'shellOutput':
                this.io.writeLine(msg.text);
                break;

            case 'shellWrite':
                this.io.write(msg.text);
                break;

            case 'shellClear':
                this.io.clear();
                break;

            case 'shellSetEcho':
                this.io.setStdinEcho?.(msg.enabled);
                break;

            case 'shellExclusiveStdin':
                if (msg.enter) {
                    this.io.enterExclusiveStdin?.();
                } else {
                    this.io.exitExclusiveStdin?.();
                }
                break;

            case 'shellReadByte':
                // Shell wants a byte — read from IO and send it back
                this.readAndSendByte();
                break;

            case 'log':
                // Re-emit Worker console messages on the main thread
                // so Playwright and DevTools capture them.
                console[msg.level](...msg.args);
                break;
        }
    }

    /**
     * Feed stdin bytes to the Worker for a specific run.
     * Continuously reads from the stdin provider and sends bytes.
     */
    private async feedStdin(id: number): Promise<void> {
        const stdinFn = this.stdinFeeds.get(id);
        if (!stdinFn) return;

        // Enable exclusive stdin so the shell doesn't steal input
        this.io.enterExclusiveStdin?.();
        this.io.setStdinEcho?.(true);

        // Start a loop that feeds bytes as they arrive
        const feed = async () => {
            while (this.stdinFeeds.has(id)) {
                const byteOrPromise = stdinFn();
                let byte: number | null;
                if (byteOrPromise !== null && typeof byteOrPromise === 'object' && 'then' in byteOrPromise) {
                    byte = await byteOrPromise;
                } else {
                    byte = byteOrPromise as number | null;
                }

                if (byte === null || byte === -1 || !this.stdinFeeds.has(id)) break;
                this.send({ type: 'stdin', id, byte });
            }

            // Restore normal input mode
            this.io.setStdinEcho?.(false);
            this.io.exitExclusiveStdin?.();
        };

        feed();
    }

    /**
     * Read a byte from the IO provider and send it to the Worker for the shell.
     */
    private async readAndSendByte(): Promise<void> {
        const result = this.io.readByte();
        let byte: number | null;
        if (result !== null && typeof result === 'object' && 'then' in result) {
            byte = await result;
        } else {
            byte = result;
        }
        if (byte !== null) {
            // Send as stdin with id 0 (shell)
            this.send({ type: 'stdin', id: 0, byte });
        }
    }
}
