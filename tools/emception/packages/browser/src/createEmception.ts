/**
 * Public, embedder-friendly entry point for Emception.
 *
 * The historical `boot()` and `bootInWorker()` exports return a grab-bag of
 * internal classes (`ToolRunner`, `MiniShell`, `TTYBridge`, `WorkerClient`)
 * which is great for the bundled IDE but overwhelming for an LMS course
 * widget that just wants to "compile and run this C file".
 *
 * `createEmception()` is the small, stable surface for those embedders:
 *
 *     import { createEmception } from '@gameguild/emception-browser';
 *
 *     const em = await createEmception({
 *         container: document.getElementById('terminal')!,
 *         manifestUrl: '/cdn/manifest.json',
 *     });
 *
 *     await em.workspace.writeFile('/home/user/main.c', 'int main(){return 0;}');
 *     const result = await em.run('clang', ['/home/user/main.c', '-o', '/home/user/a.wasm']);
 *     console.log(result.exitCode, result.stdout, result.stderr);
 *
 *     em.dispose();
 *
 * The implementation is a thin façade over `bootInWorker()`. The richer
 * `boot()` / `bootInWorker()` exports remain available for advanced use.
 */

import { HeadlessIOProvider, ToolchainPreset } from 'emception';
import type {
    EmceptionAPI,
    EmceptionEventListener,
    EmceptionEventMap,
    EmceptionEventName,
    FileEntry,
    RunOptions,
    ToolResult,
    Unsubscribe,
    WorkspaceBuildConfig,
    WorkspaceAPI,
} from 'emception';
import { bootInWorker } from './index.js';
import type { RunOptions as BrowserRunOptions } from './tool-runner.js';
import { WorkerClient } from './worker-client.js';

/**
 * Default manifest URL used when `manifestUrl` is omitted. Points at the
 * canonical GameGuild CDN payload so a host can boot emception with zero
 * configuration:
 *
 *     await createEmception({ container: el }); // uses DEFAULT_MANIFEST_URL
 *
 * Pin or self-host a toolchain release in production by passing
 * `manifestUrl` explicitly.
 */
export const DEFAULT_MANIFEST_URL = 'https://gameguild-gg.github.io/gameguild/cdn/manifest.json';

export interface CreateEmceptionOptions {
    /**
     * Optional. Either an HTMLElement to mount a new xterm.js Terminal into,
     * or an existing `Terminal` instance to reuse (advanced).
     *
     * Required when `tty` is `'xterm'` (the default). Ignored when `tty`
     * is `'none'`.
     */
    container?: HTMLElement | import('@xterm/xterm').Terminal;
    /**
    * Terminal binding. Defaults to `'xterm'` when `container`
     * is provided, `'none'` otherwise.
     *
     * - `'xterm'` — mount or reuse an xterm.js Terminal in `container`.
     * - `'none'` — no terminal; stdout/stderr go to `onStdout` / `onStderr`
     *   callbacks. Useful for headless / batch / SSR-friendly use cases
     *   like an LMS auto-grader running tests in a web worker.
     */
    tty?: 'xterm' | 'none';
    /** Headless stdout sink. Only used when `tty: 'none'`. */
    onStdout?: (text: string) => void;
    /** Headless stderr sink. Only used when `tty: 'none'`. */
    onStderr?: (text: string) => void;
    /**
     * URL of the manifest produced by `npm run build:manifest`.
     *
    * If omitted, falls back to {@link DEFAULT_MANIFEST_URL}. For production
     * deploys, pin or self-host the manifest under your own origin to avoid
     * CDN drift.
     */
    manifestUrl?: string;
}

export async function createEmception(opts: CreateEmceptionOptions = {}): Promise<EmceptionAPI> {
    const manifestUrl = opts.manifestUrl ?? DEFAULT_MANIFEST_URL;
    const tty = opts.tty ?? (opts.container ? 'xterm' : 'none');

    if (tty === 'xterm') {
        if (!opts.container) {
            throw new Error(
                "createEmception: tty: 'xterm' requires a `container` (HTMLElement or Terminal). " +
                "Pass `tty: 'none'` for headless mode.",
            );
        }
        const { client } = await bootInWorker(manifestUrl, opts.container);
        return wrap(client);
    }

    // tty: 'none' — headless: spawn worker + WorkerClient directly with
    // a HeadlessIOProvider. No xterm, no DOM.
    const io = new HeadlessIOProvider({ onStdout: opts.onStdout, onStderr: opts.onStderr });
    const worker = new Worker(new URL('./worker-entry', import.meta.url), {
        type: 'module',
        name: 'emception-toolchain-headless',
    });
    const client = new WorkerClient(worker, io);
    const absoluteManifestUrl = new URL(manifestUrl, self.location.href).href;
    await client.boot(absoluteManifestUrl);
    return wrap(client);
}

function wrap(client: WorkerClient): EmceptionAPI {
    const encoder = new TextEncoder();
    const decoder = new TextDecoder();
    const toBytes = (data: Uint8Array | string): Uint8Array =>
        typeof data === 'string' ? encoder.encode(data) : data;

    // Minimal in-closure event emitter.
    const listeners = new Map<EmceptionEventName, Set<EmceptionEventListener<EmceptionEventName>>>();
    function on<E extends EmceptionEventName>(event: E, listener: EmceptionEventListener<E>): Unsubscribe {
        if (!listeners.has(event)) listeners.set(event, new Set());
        listeners.get(event)!.add(listener as EmceptionEventListener<EmceptionEventName>);
        return () => listeners.get(event)?.delete(listener as EmceptionEventListener<EmceptionEventName>);
    }
    function emit<E extends EmceptionEventName>(event: E, payload: EmceptionEventMap[E]): void {
        listeners.get(event)?.forEach((fn) => (fn as EmceptionEventListener<E>)(payload));
    }

    // Local workspace state.
    let currentBuild: WorkspaceBuildConfig = { toolchain: ToolchainPreset.CPP };
    let currentWorkspaceName = 'default';
    const mountPath = () => `/home/user/${currentWorkspaceName}`;

    /** Translate a core RunOptions into the browser WorkerClient RunOptions. */
    function toBrowserOpts(opts: RunOptions): BrowserRunOptions {
        const browser: BrowserRunOptions = { cwd: opts.cwd ?? mountPath(), env: { ...currentBuild.env, ...opts.env } };
        if (typeof opts.stdout === 'function') {
            const fn = opts.stdout;
            browser.onStdout = (text: string) => { (fn as (c: Uint8Array) => void)(encoder.encode(text)); };
        }
        if (typeof opts.stderr === 'function') {
            const fn = opts.stderr;
            browser.onStderr = (text: string) => { (fn as (c: Uint8Array) => void)(encoder.encode(text)); };
        }
        if (typeof opts.stdin === 'string') {
            const bytes = encoder.encode(opts.stdin.endsWith('\n') ? opts.stdin : opts.stdin + '\n');
            let i = 0;
            browser.stdin = () => (i < bytes.length ? bytes[i++] : null);
        } else if (opts.stdin instanceof Uint8Array) {
            const bytes = opts.stdin;
            let i = 0;
            browser.stdin = () => (i < bytes.length ? bytes[i++] : null);
        } else if (typeof opts.stdin === 'function') {
            browser.stdin = opts.stdin as () => number | null | Promise<number>;
        }
        return browser;
    }

    async function run(cmd: string, argv?: string[], opts?: RunOptions): Promise<ToolResult> {
        const start = Date.now();
        const result = await client.run(cmd, [...(argv ?? [])], toBrowserOpts(opts ?? {}));
        const toolResult: ToolResult = { ...result, durationMs: Date.now() - start, timedOut: false };
        emit('exit', { exitCode: toolResult.exitCode, durationMs: toolResult.durationMs });
        return toolResult;
    }

    // Recursive VFS listing helper.
    async function walkDir(dir: string): Promise<Array<{ path: string } & FileEntry>> {
        const names = await client.listDir(dir);
        const entries: Array<{ path: string } & FileEntry> = [];
        for (const name of names) {
            const p = dir === '/' ? `/${name}` : `${dir}/${name}`;
            const children = await client.listDir(p);
            if (children.length > 0) {
                entries.push(...(await walkDir(p)));
            } else {
                entries.push({ path: p, content: '', visibility: 'public' });
            }
        }
        return entries;
    }

    const workspace: WorkspaceAPI = {
        list: async () => ['default'],
        switch: async (name: string) => { currentWorkspaceName = name; },
        reset: async () => client.resetVfs(),
        readFile: (path: string) => client.getFile(path),
        writeFile: async (path: string, data: Uint8Array | string, _meta?: Partial<FileEntry>) =>
            client.writeFile(path, toBytes(data)),
        listFiles: async (_opts?) => walkDir(mountPath()),
        setVisibility: async (_path: string, _v: FileEntry['visibility']) => { /* metadata-only, no-op */ },
        getBuild: async () => currentBuild,
        setBuild: async (build: WorkspaceBuildConfig) => { currentBuild = build; },
        exportZip: async () => { throw new Error('exportZip is not supported in the browser embedder'); },
        importZip: async (_blob: Blob) => { throw new Error('importZip is not supported in the browser embedder'); },
    };

    return {
        workspace,
        run,
        compileAndRun: async (_sourceOrFiles?, _opts?) => {
            throw new Error(
                'compileAndRun: use workspace.writeFile() + run() directly in the browser embedder, ' +
                'or use compileAndRun() from @gameguild/emception-browser/presets for the argv-based pipeline.',
            );
        },
        runTests: async (_plan, _opts?) => {
            throw new Error('runTests is not yet supported in the browser embedder');
        },
        on,
        dispose: () => {
            emit('exit', { exitCode: -1, durationMs: 0 });
            client.terminate();
        },
    };
}
