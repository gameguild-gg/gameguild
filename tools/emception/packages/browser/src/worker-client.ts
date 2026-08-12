/**
 * Main-thread proxy client for the Worker-based toolchain.
 *
 * Thin shim over `WorkerOrchestrator` (emception).
 * This file contains only the browser-specific concerns:
 *   - SharedArrayBuffer stdin pump (feedStdin, feedShellStdin)
 *   - IOProvider wiring (xterm.js / TTYBridge)
 *   - Cleanup of SAB channels on terminate()
 *
 * All correlated request/response logic and notification routing live in
 * WorkerOrchestrator.
 */

import type { IOProvider } from 'emception';
import { WorkerOrchestrator, workerTransport } from 'emception';
import type { RunOptions, ToolResult } from './tool-runner.js';

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

export class WorkerClient {
    private readonly orch: WorkerOrchestrator;
    private readonly io: WorkerBootOptions['io'];

    /** Shared-memory stdin channel used by shell-launched foreground WASI runs. */
    private shellStdinChannel: { control: Int32Array; data: Uint8Array } | null = null;

    /**
     * All currently-active SAB stdin channels (both per-run and shell),
     * tracked so terminate() can close them all.
     */
    private readonly activeStdinChannels = new Set<{ control: Int32Array; data: Uint8Array }>();

    constructor(worker: Worker, io: WorkerBootOptions['io']) {
        this.io = io;
        this.orch = new WorkerOrchestrator(workerTransport(worker), {
            onShellOutput: (t) => io.writeLine(t),
            onShellWrite: (t) => io.write(t),
            onShellClear: () => io.clear(),
            onShellSetEcho: (en) => io.setStdinEcho?.(en),
            onShellExclusiveStdin: (enter) => {
                if (!enter) {
                    if (this.shellStdinChannel) {
                        this.closeSharedChannel(this.shellStdinChannel);
                        this.activeStdinChannels.delete(this.shellStdinChannel);
                        this.shellStdinChannel = null;
                    }
                    io.setStdinEcho?.(false);
                    io.exitExclusiveStdin?.();
                }
            },
            onShellReadByte: () => this.readAndSendByte(),
            onShellStdinRequest: (ctrl, data) => this.feedShellStdin(ctrl, data),
            onLog: (level, args) => console[level](...args),
            onTransportError: (err) => console.error('[Emception:WorkerClient] Worker error:', err),
        });
    }

    /* ---------------------------------------------------------------- */
    /*  Boot                                                             */
    /* ---------------------------------------------------------------- */

    boot(manifestUrl: string, toolVersions?: WorkerBootOptions['toolVersions']): Promise<void> {
        return this.orch.boot(manifestUrl, {
            origin: self.location.origin,
            toolVersions,
        });
    }

    /* ---------------------------------------------------------------- */
    /*  ToolRunner-compatible API                                        */
    /* ---------------------------------------------------------------- */

    async run(tool: string, argv: string[], options: RunOptions = {}): Promise<ToolResult> {
        return await this.orch.run(tool, argv, {
            env: options.env,
            cwd: options.cwd,
            onStdout: options.onStdout,
            onStderr: options.onStderr,
            wantStdin: !!options.stdin,
            onStdinRequest: options.stdin ? (ctrl, data) => this.feedStdin(ctrl, data, options.stdin!) : undefined,
            hints: options.hints,
        });
    }

    async getFile(path: string): Promise<Uint8Array | null> {
        return this.orch.getFile(path);
    }

    async writeFile(path: string, data: Uint8Array): Promise<void> {
        return this.orch.writeFile(path, data);
    }

    async listDir(path: string): Promise<string[]> {
        return this.orch.listDir(path);
    }

    /** Reset the Worker VFS writable layers (clear /tmp, /home/user). */
    async resetVfs(): Promise<void> {
        return this.orch.resetVfs();
    }

    /* ---------------------------------------------------------------- */
    /*  Cleanup                                                          */
    /* ---------------------------------------------------------------- */

    terminate(): void {
        // Close all active SAB channels so pending feed loops exit immediately.
        for (const ch of this.activeStdinChannels) {
            this.closeSharedChannel(ch);
        }
        this.activeStdinChannels.clear();
        this.shellStdinChannel = null;

        // Restore normal input mode.
        this.io.setStdinEcho?.(false);
        this.io.exitExclusiveStdin?.();

        // Dispose the orchestrator (terminates the worker transport).
        this.orch.dispose();
    }

    /* ---------------------------------------------------------------- */
    /*  SAB stdin pumps (browser-specific)                              */
    /* ---------------------------------------------------------------- */

    /**
     * Feed stdin bytes to the Worker for a specific run.
     * Called from the per-run onStdinRequest callback.
     */
    private async feedStdin(controlBuffer: SharedArrayBuffer, dataBuffer: SharedArrayBuffer, stdinFn: () => number | null | Promise<number>): Promise<void> {
        const channel = {
            control: new Int32Array(controlBuffer),
            data: new Uint8Array(dataBuffer),
        };
        this.activeStdinChannels.add(channel);

        // Enable exclusive stdin so the shell doesn't steal input.
        this.io.enterExclusiveStdin?.();
        this.io.setStdinEcho?.(true);

        const feed = async () => {
            while (this.activeStdinChannels.has(channel)) {
                const byteOrPromise = stdinFn();
                let byte: number | null;
                if (byteOrPromise !== null && typeof byteOrPromise === 'object' && 'then' in byteOrPromise) {
                    byte = await byteOrPromise;
                } else {
                    byte = byteOrPromise as number | null;
                }

                if (byte === null || byte === -1 || !this.activeStdinChannels.has(channel)) break;
                const wrote = await this.writeByteToSharedChannel(channel, byte);
                if (!wrote) break;
            }

            this.io.setStdinEcho?.(false);
            this.io.exitExclusiveStdin?.();
            this.closeSharedChannel(channel);
            this.activeStdinChannels.delete(channel);
        };

        feed();
    }

    private async feedShellStdin(controlBuffer: SharedArrayBuffer, dataBuffer: SharedArrayBuffer): Promise<void> {
        const channel = {
            control: new Int32Array(controlBuffer),
            data: new Uint8Array(dataBuffer),
        };
        this.shellStdinChannel = channel;
        this.activeStdinChannels.add(channel);

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
                this.activeStdinChannels.delete(channel);
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
     * Called from the onShellReadByte notification.
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
            // Shell stdin uses id 0.
            this.orch.sendStdinByte(0, byte);
        }
    }
}
