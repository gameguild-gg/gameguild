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

    /** Shared-memory stdin channels for interactive WASI runs. */
    private stdinSharedChannels = new Map<number, { control: Int32Array; data: Uint8Array }>();

    /** Shared-memory stdin channel used by shell-launched foreground WASI runs. */
    private shellStdinChannel: { control: Int32Array; data: Uint8Array } | null = null;

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

    /** Reset the Worker VFS writable layers (clear /tmp, /home/user). */
    async resetVfs(): Promise<void> {
        const id = this.nextId++;
        return new Promise<void>((resolve, reject) => {
            this.pending.set(id, {
                resolve: () => resolve(),
                reject,
            });
            this.send({ type: 'resetVfs', id });
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
        for (const [, channel] of this.stdinSharedChannels) {
            this.closeSharedChannel(channel);
        }
        this.stdinSharedChannels.clear();
        if (this.shellStdinChannel) {
            this.closeSharedChannel(this.shellStdinChannel);
            this.shellStdinChannel = null;
        }
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
                this.feedStdin(msg.id, msg.controlBuffer, msg.dataBuffer);
                break;

            case 'shellStdinRequest':
                this.feedShellStdin(msg.controlBuffer, msg.dataBuffer);
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
                    const channel = this.stdinSharedChannels.get(msg.id);
                    if (channel) {
                        this.closeSharedChannel(channel);
                        this.stdinSharedChannels.delete(msg.id);
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

            case 'resetVfsResult': {
                const p = this.pending.get(msg.id);
                if (p) {
                    this.pending.delete(msg.id);
                    if (msg.ok) {
                        p.resolve(undefined);
                    } else {
                        p.reject(new Error(msg.error ?? 'resetVfs failed'));
                    }
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
                if (!msg.enter) {
                    if (this.shellStdinChannel) {
                        this.closeSharedChannel(this.shellStdinChannel);
                        this.shellStdinChannel = null;
                    }
                    this.io.setStdinEcho?.(false);
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
    private async feedStdin(id: number, controlBuffer: SharedArrayBuffer, dataBuffer: SharedArrayBuffer): Promise<void> {
        const stdinFn = this.stdinFeeds.get(id);
        if (!stdinFn) return;

        const channel = {
            control: new Int32Array(controlBuffer),
            data: new Uint8Array(dataBuffer),
        };
        this.stdinSharedChannels.set(id, channel);

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
                const wrote = await this.writeByteToSharedChannel(channel, byte);
                if (!wrote) break;
            }

            // Restore normal input mode
            this.io.setStdinEcho?.(false);
            this.io.exitExclusiveStdin?.();
            this.closeSharedChannel(channel);
            this.stdinSharedChannels.delete(id);
        };

        feed();
    }

    private async feedShellStdin(controlBuffer: SharedArrayBuffer, dataBuffer: SharedArrayBuffer): Promise<void> {
        const channel = {
            control: new Int32Array(controlBuffer),
            data: new Uint8Array(dataBuffer),
        };
        this.shellStdinChannel = channel;

        this.io.enterExclusiveStdin?.();
        this.io.setStdinEcho?.(true);

        let lineBuf = '';
        let lineCursor = 0;
        const lineQueue: number[] = [];

        const nextByte = async (): Promise<number | null> => {
            while (this.shellStdinChannel === channel) {
                if (lineQueue.length > 0) {
                    return lineQueue.shift()!;
                }

                const raw = this.io.readByteExclusive?.() ?? this.io.readByte();
                let byte: number | null;
                if (raw !== null && typeof raw === 'object' && 'then' in raw) {
                    byte = await raw;
                } else {
                    byte = raw as number | null;
                }

                if (byte === null || byte === -1) {
                    if (this.shellStdinChannel !== channel) return null;
                    continue;
                }

                if (byte === 127 || byte === 8) {
                    if (lineCursor > 0) {
                        lineBuf = lineBuf.slice(0, lineCursor - 1) + lineBuf.slice(lineCursor);
                        lineCursor--;
                    }
                    continue;
                }

                if (byte === 13 || byte === 10) {
                    for (let i = 0; i < lineBuf.length; i++) {
                        lineQueue.push(lineBuf.charCodeAt(i));
                    }
                    lineQueue.push(10);
                    lineBuf = '';
                    lineCursor = 0;
                    return lineQueue.shift()!;
                }

                if (byte >= 32) {
                    const ch = String.fromCharCode(byte);
                    lineBuf = lineBuf.slice(0, lineCursor) + ch + lineBuf.slice(lineCursor);
                    lineCursor++;
                }
            }

            return null;
        };

        const feed = async () => {
            while (this.shellStdinChannel === channel) {
                const byte = await nextByte();
                if (byte === null) break;
                const wrote = await this.writeByteToSharedChannel(channel, byte);
                if (!wrote) break;
            }

            if (this.shellStdinChannel === channel) {
                this.io.setStdinEcho?.(false);
                this.io.exitExclusiveStdin?.();
                this.closeSharedChannel(channel);
                this.shellStdinChannel = null;
            }
        };

        feed();
    }

    private async writeByteToSharedChannel(channel: { control: Int32Array; data: Uint8Array }, byte: number): Promise<boolean> {
        const readIndex = 0;
        const writeIndex = 1;
        const closedIndex = 2;
        const ringSize = channel.data.length;

        while (true) {
            if (Atomics.load(channel.control, closedIndex) === 1) {
                return false;
            }

            const read = Atomics.load(channel.control, readIndex);
            const write = Atomics.load(channel.control, writeIndex);
            const nextWrite = (write + 1) % ringSize;

            if (nextWrite !== read) {
                channel.data[write] = byte & 0xff;
                Atomics.store(channel.control, writeIndex, nextWrite);
                Atomics.notify(channel.control, writeIndex, 1);
                return true;
            }

            await new Promise<void>((resolve) => setTimeout(resolve, 1));
        }
    }

    private closeSharedChannel(channel: { control: Int32Array; data: Uint8Array }): void {
        const closedIndex = 2;
        const writeIndex = 1;
        Atomics.store(channel.control, closedIndex, 1);
        Atomics.notify(channel.control, writeIndex, 1);
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
