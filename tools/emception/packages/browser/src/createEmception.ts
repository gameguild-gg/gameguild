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
 *     import { createEmception } from 'emception';
 *
 *     const ide = await createEmception({
 *         container: document.getElementById('terminal')!,
 *         manifestUrl: '/cdn/manifest.json',
 *     });
 *
 *     await ide.writeFile('/home/user/main.c', 'int main(){return 0;}');
 *     const result = await ide.run('clang', ['/home/user/main.c', '-o', '/home/user/a.out']);
 *     console.log(result.exitCode, result.stdout, result.stderr);
 *
 *     ide.dispose();
 *
 * The implementation is a thin façade over `bootInWorker()`. The richer
 * `boot()` / `bootInWorker()` exports remain available for advanced use.
 */

import { HeadlessIOProvider } from 'emception';
import type { EmbedderEmceptionAPI } from 'emception';
import { bootInWorker } from './index';
import type { RunOptions, ToolResult } from './tool-runner';
import { WorkerClient } from './worker-client';

/**
 * Default manifest URL used when `manifestUrl` is omitted. Points at the
 * latest published `emception` package CDN payload on jsDelivr so a host can
 * boot emception with zero configuration:
 *
 *     await createEmception({ container: el }); // uses DEFAULT_MANIFEST_URL
 *
 * Pin to a specific version in production by passing `manifestUrl`
 * explicitly, e.g. `https://cdn.jsdelivr.net/npm/emception@1.2.3/cdn/manifest.json`.
 */
export const DEFAULT_MANIFEST_URL = 'https://cdn.jsdelivr.net/npm/emception/cdn/manifest.json';

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
    * If omitted, falls back to {@link DEFAULT_MANIFEST_URL} (the latest
    * published `emception` CDN payload on jsDelivr). For production deploys
     * pin a specific version or self-host the manifest under your own
     * origin to avoid CDN drift.
     */
    manifestUrl?: string;
}

/**
 * Flat embedder API returned by {@link createEmception}.
 *
 * This is a re-export of {@link EmbedderEmceptionAPI} from `emception` (core)
 * under the legacy name for backward compatibility. New code should import
 * `EmbedderEmceptionAPI` from `emception` directly.
 *
 * @deprecated Import `EmbedderEmceptionAPI` from `emception` instead.
 */
export type { EmbedderEmceptionAPI as EmceptionAPI } from 'emception';

export async function createEmception(opts: CreateEmceptionOptions = {}): Promise<EmbedderEmceptionAPI> {
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

function wrap(client: WorkerClient): EmbedderEmceptionAPI {
    const encoder = new TextEncoder();
    const toBytes = (data: Uint8Array | string): Uint8Array =>
        typeof data === 'string' ? encoder.encode(data) : data;

    return {
        run: (tool, argv?, options?) => client.run(tool, [...(argv ?? [])], options ?? {}),
        readFile: (path) => client.getFile(path),
        writeFile: (path, data) => client.writeFile(path, toBytes(data)),
        listDir: (path) => client.listDir(path),
        resetVfs: () => client.resetVfs(),
        dispose: () => client.terminate(),
    };
}
