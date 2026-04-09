/**
 * Web Worker entry point for the Emception toolchain.
 *
 * This script runs inside a dedicated Worker. It boots the full toolchain
 * (VFS, LazyFS, IDBFS, ToolRunner, MiniShell) and communicates with the
 * main thread via the postMessage protocol defined in worker-protocol.ts.
 *
 * The main thread never touches WASM — all compilation happens here.
 * JSPI works in Workers, and IndexedDB is available, so the entire
 * existing architecture works unchanged inside the Worker.
 */

import { detectAsyncStrategy } from './async-bridge';
import { FetchBridge } from './net/fetch-bridge';
import { MiniShell } from './shell';
import { ToolRunner } from './tool-runner';
import type { IOProvider } from './tty/io-provider';
import { IDBFS } from './vfs/idb';
import { createVFSManager } from './vfs/index';
import type { FSManifest } from './vfs/lazy';
import { LazyFS } from './vfs/lazy';
import { OverlayFS } from './vfs/overlay';
import type {
    MainToWorkerMessage,
    WorkerToMainMessage,
} from './worker-protocol';

const P = '[Emception:Worker]';

// Forward Worker console output to the main thread so Playwright
// (and DevTools) can capture [Emception:*] logs from the Worker.
const _nativeConsole = {
    log: console.log.bind(console),
    warn: console.warn.bind(console),
    error: console.error.bind(console),
    info: console.info.bind(console),
    debug: console.debug.bind(console),
};

function forwardLog(level: 'log' | 'warn' | 'error' | 'info' | 'debug', args: unknown[]): void {
    _nativeConsole[level](...args);
    try {
        // Only forward serialisable data; stringify complex objects.
        const safeArgs = args.map(a =>
            typeof a === 'string' || typeof a === 'number' || typeof a === 'boolean' || a === null || a === undefined
                ? a
                : String(a),
        );
        self.postMessage({ type: 'log', level, args: safeArgs });
    } catch { /* ignore serialisation errors */ }
}

console.log = (...args: unknown[]) => forwardLog('log', args);
console.warn = (...args: unknown[]) => forwardLog('warn', args);
console.error = (...args: unknown[]) => forwardLog('error', args);
console.info = (...args: unknown[]) => forwardLog('info', args);
console.debug = (...args: unknown[]) => forwardLog('debug', args);

/**
 * Page origin, set when the main thread sends the boot message.
 * Used to resolve relative URLs (e.g. /_next/static/...) that
 * the default Worker fetch() cannot handle because the Worker's
 * own URL is a webpack chunk/blob.
 */
let pageOrigin = '';

// Patch fetch so relative URLs (starting with /) are resolved
// against the page origin instead of the Worker's blob URL.
const nativeFetch = self.fetch.bind(self);
// eslint-disable-next-line @typescript-eslint/no-explicit-any
(self as any).fetch = (input: RequestInfo | URL, init?: RequestInit) => {
    if (typeof input === 'string' && input.startsWith('/') && pageOrigin) {
        input = `${pageOrigin}${input}`;
    }
    return nativeFetch(input, init);
};

/* ------------------------------------------------------------------ */
/*  State                                                              */
/* ------------------------------------------------------------------ */

let runner: ToolRunner | null = null;
let vfs: ReturnType<typeof createVFSManager> | null = null;
let shell: MiniShell | null = null;

/**
 * Map of pending stdin resolvers, keyed by run id.
 * When the Worker needs a stdin byte for a specific run, it stores
 * a resolve callback here and posts a 'stdinRequest' to the main thread.
 */
const stdinResolvers = new Map<number, Array<(byte: number) => void>>();

/* ------------------------------------------------------------------ */
/*  Helpers                                                            */
/* ------------------------------------------------------------------ */

function post(msg: WorkerToMainMessage, transfer?: Transferable[]): void {
    if (transfer && transfer.length > 0) {
        self.postMessage(msg, { transfer });
    } else {
        self.postMessage(msg);
    }
}

/**
 * IOProvider that proxies all I/O back to the main thread's xterm.js.
 * The MiniShell uses this instead of a direct TTYBridge.
 */
class WorkerIOProvider implements IOProvider {
    private byteBuffer: number[] = [];
    private byteResolvers: Array<(byte: number) => void> = [];

    readByte(): number | null | Promise<number> {
        if (this.byteBuffer.length > 0) {
            return this.byteBuffer.shift()!;
        }
        return new Promise<number>((resolve) => {
            this.byteResolvers.push(resolve);
        });
    }

    /** Called when the main thread sends a keystroke. */
    pushByte(byte: number): void {
        if (this.byteResolvers.length > 0) {
            this.byteResolvers.shift()!(byte);
        } else {
            this.byteBuffer.push(byte);
        }
    }

    writeLine(text: string): void {
        post({ type: 'shellOutput', text });
    }

    write(text: string): void {
        post({ type: 'shellWrite', text });
    }

    writeError(text: string): void {
        post({ type: 'shellOutput', text: `\x1b[31m${text}\x1b[0m` });
    }

    clear(): void {
        post({ type: 'shellClear' });
    }

    setStdinEcho(enabled: boolean): void {
        post({ type: 'shellSetEcho', enabled });
    }
}

const workerIO = new WorkerIOProvider();

/* ------------------------------------------------------------------ */
/*  Boot                                                               */
/* ------------------------------------------------------------------ */

async function handleBoot(manifestUrl: string, toolVersions?: { pythonMajorMinor?: string; pythonMajorMinorCompact?: string }): Promise<void> {
    const tBoot = performance.now();
    const ms = (t0: number) => `${(performance.now() - t0).toFixed(1)}ms`;

    console.log(`${P} ===== BOOT START =====`);
    console.log(`${P} Async strategy: ${detectAsyncStrategy()}`);

    // Step 1: Fetch manifest
    const t1 = performance.now();
    const response = await fetch(manifestUrl);
    const manifest = (await response.json()) as FSManifest & { toolVersions?: Record<string, string> };
    // Derive the CDN base URL from the manifest URL's directory.
    // e.g. "https://host/gameguild/cdn/manifest.json" → "https://host/gameguild/cdn"
    // This works regardless of deploy subpath (GitHub Pages, custom domain, etc.)
    const manifestDir = manifestUrl.replace(/\/[^/]*$/, ''); // strip filename
    manifest.baseUrl = manifestDir;

    // Also resolve bundle URLs relative to the manifest directory
    if (manifest.bundles) {
        for (const bundle of Object.values(manifest.bundles as Record<string, { url?: string }>)) {
            if (bundle.url && !bundle.url.startsWith('http')) {
                // Bundle URLs like "/cdn/usr/lib/clang.tar.br" need the same treatment.
                // Strip the baseUrl prefix (e.g. "/cdn") from the baked-in URL to get
                // the relative path, then resolve against the manifest directory.
                const bakedBase = '/cdn';
                const relativePath = bundle.url.startsWith(bakedBase)
                    ? bundle.url.slice(bakedBase.length)
                    : bundle.url;
                bundle.url = manifestDir + relativePath;
            }
        }
    }
    const fileCount = Object.keys(manifest.files).length;
    const bundleCount = manifest.bundles ? Object.keys(manifest.bundles).length : 0;
    console.log(`${P} Step 1/6 done: manifest loaded (${fileCount} files, ${bundleCount} bundles, baseUrl=${manifest.baseUrl}) in ${ms(t1)}`);

    // Step 2: Initialize LazyFS
    const t2 = performance.now();
    const lazyFs = new LazyFS(manifest);
    await lazyFs.init();
    console.log(`${P} Step 2/6 done: LazyFS ready in ${ms(t2)}`);

    // Step 3: Initialize writable layers
    const t3 = performance.now();
    const manifestVersion = `${manifest.baseUrl}:${Object.keys(manifest.files).length}`;
    const writeFs = new IDBFS('overlay-writes', { version: manifestVersion });
    const tmpFs = new IDBFS('tmp-fs', { volatile: true });
    const idbFs = new IDBFS('user-files');
    await writeFs.init();
    await tmpFs.init();
    await idbFs.init();
    console.log(`${P} Step 3/6 done: writable FS layers ready in ${ms(t3)}`);

    // Step 4: Assemble OverlayFS + VFSManager
    const t4 = performance.now();
    const overlay = new OverlayFS(lazyFs, writeFs);
    overlay.mount('/tmp', tmpFs);
    overlay.mount('/home', idbFs);
    await overlay.mkdir('/tmp');
    await overlay.mkdir('/home');
    await overlay.mkdir('/home/user');
    vfs = createVFSManager(overlay, lazyFs);
    console.log(`${P} Step 4/6 done: VFS assembled in ${ms(t4)}`);

    // Step 5: Create runner and shell
    const t5 = performance.now();
    new FetchBridge();

    const versions = {
        pythonMajorMinor: toolVersions?.pythonMajorMinor ?? manifest.toolVersions?.pythonMajorMinor ?? '3.13',
        pythonMajorMinorCompact: toolVersions?.pythonMajorMinorCompact ?? manifest.toolVersions?.pythonMajorMinorCompact ?? '313',
    };
    runner = new ToolRunner(vfs, versions);
    shell = new MiniShell(runner, workerIO, vfs);
    console.log(`${P} Step 5/6 done: all components created in ${ms(t5)}`);

    // Step 6: Start shell (runs the REPL loop asynchronously)
    shell.start();
    console.log(`${P} Step 6/6 done: shell started`);

    console.log(`${P} ===== BOOT COMPLETE in ${ms(tBoot)} =====`);
}

/* ------------------------------------------------------------------ */
/*  Run handler                                                        */
/* ------------------------------------------------------------------ */

async function handleRun(id: number, tool: string, argv: string[], options: { env?: Record<string, string>; cwd?: string; wantStdin?: boolean }): Promise<void> {
    if (!runner) {
        post({ type: 'runResult', id, exitCode: 1, stdout: '', stderr: 'Worker not booted' });
        return;
    }

    // Create a stdin provider that waits for bytes from the main thread
    let stdinFn: (() => number | null | Promise<number>) | undefined;
    if (options.wantStdin) {
        stdinResolvers.set(id, []);
        stdinFn = () => {
            const resolvers = stdinResolvers.get(id);
            if (!resolvers) return null;
            return new Promise<number>((resolve) => {
                resolvers.push(resolve);
            });
        };
        // Tell the main thread we need stdin
        post({ type: 'stdinRequest', id });
    }

    const result = await runner.run(tool, argv, {
        env: options.env,
        cwd: options.cwd,
        onStdout: (text) => post({ type: 'stdout', id, text }),
        onStderr: (text) => post({ type: 'stderr', id, text }),
        stdin: stdinFn,
    });

    stdinResolvers.delete(id);

    post({ type: 'runResult', id, exitCode: result.exitCode, stdout: result.stdout, stderr: result.stderr });
}

/* ------------------------------------------------------------------ */
/*  Message handler                                                    */
/* ------------------------------------------------------------------ */

self.onmessage = async (ev: MessageEvent<MainToWorkerMessage>) => {
    const msg = ev.data;

    switch (msg.type) {
        case 'boot':
            try {
                pageOrigin = msg.origin || '';
                // Pre-load brotli-wasm and inject into LazyFS before boot.
                // The bundler's WASM loader can't resolve relative URLs in
                // Workers, so we fetch the WASM file manually from the CDN
                // (using our patched fetch that resolves '/' against pageOrigin)
                // and init the pkg.web entry with the fetched Response.
                try {
                    // Load brotli-wasm WASM directly, bypassing the bundler's
                    // module loader (which can't resolve relative URLs in Workers).
                    // We fetch the WASM from the CDN copy, load the JS glue via
                    // a blob URL to avoid bundler processing, and call init().
                    const wasmUrl = `${pageOrigin}/cdn/brotli_wasm_bg.wasm`;
                    const jsUrl = `${pageOrigin}/cdn/brotli_wasm.js`;

                    // Fetch the JS glue as text so the bundler doesn't process it
                    const jsResp = await nativeFetch(jsUrl);
                    const jsText = await jsResp.text();
                    const jsBlob = new Blob([jsText], { type: 'text/javascript' });
                    const jsBlobUrl = URL.createObjectURL(jsBlob);

                    // eslint-disable-next-line @typescript-eslint/no-explicit-any
                    const brotliMod = await import(/* webpackIgnore: true */ jsBlobUrl) as any;
                    await brotliMod.default(nativeFetch(wasmUrl));
                    URL.revokeObjectURL(jsBlobUrl);

                    LazyFS.customBrotliDecompressor = (data: Uint8Array) => brotliMod.decompress(data);
                    console.log(`${P} brotli-wasm loaded from CDN and injected into LazyFS`);
                } catch (e) {
                    console.warn(`${P} brotli-wasm pre-load failed, relying on DecompressionStream:`, e);
                }
                await handleBoot(msg.manifestUrl, msg.toolVersions);
                post({ type: 'booted' });
            } catch (err) {
                post({ type: 'bootError', error: err instanceof Error ? err.message : String(err) });
            }
            break;

        case 'run':
            handleRun(msg.id, msg.tool, msg.argv, msg.options);
            break;

        case 'stdin': {
            if (msg.id === 0) {
                // Shell input — feed to the Worker IO provider
                workerIO.pushByte(msg.byte);
            } else {
                // Program stdin — only resolve the run's stdin request
                const resolvers = stdinResolvers.get(msg.id);
                if (resolvers && resolvers.length > 0) {
                    resolvers.shift()!(msg.byte);
                }
            }
            break;
        }

        case 'getFile':
            if (!vfs) {
                post({ type: 'getFileResult', id: msg.id, data: null });
                break;
            }
            try {
                const data = await vfs.fetchFile(msg.path);
                if (data) {
                    // Transfer the buffer (zero-copy)
                    const copy = new Uint8Array(data);
                    post({ type: 'getFileResult', id: msg.id, data: copy }, [copy.buffer]);
                } else {
                    post({ type: 'getFileResult', id: msg.id, data: null });
                }
            } catch {
                post({ type: 'getFileResult', id: msg.id, data: null });
            }
            break;

        case 'writeFile':
            if (!vfs) {
                post({ type: 'writeFileResult', id: msg.id, ok: false, error: 'Not booted' });
                break;
            }
            try {
                await vfs.overlay.writeFile(msg.path, msg.data);
                post({ type: 'writeFileResult', id: msg.id, ok: true });
            } catch (err) {
                post({ type: 'writeFileResult', id: msg.id, ok: false, error: String(err) });
            }
            break;

        case 'listDir':
            if (!vfs) {
                post({ type: 'listDirResult', id: msg.id, entries: [] });
                break;
            }
            try {
                const entries = await vfs.overlay.readdir(msg.path);
                post({ type: 'listDirResult', id: msg.id, entries });
            } catch {
                post({ type: 'listDirResult', id: msg.id, entries: [] });
            }
            break;
    }
};

console.log(`${P} Worker script loaded, waiting for boot message...`);
