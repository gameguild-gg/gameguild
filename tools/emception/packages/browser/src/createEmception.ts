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

import { bootInWorker } from './index';
import type { RunOptions, ToolResult } from './tool-runner';

/**
 * Default manifest URL used when `manifestUrl` is omitted. Points at the
 * latest published `@emception/sysroot` package on jsDelivr so a host can
 * boot emception with zero configuration:
 *
 *     await createEmception({ container: el }); // uses DEFAULT_MANIFEST_URL
 *
 * Pin to a specific version in production by passing `manifestUrl`
 * explicitly, e.g. `https://cdn.jsdelivr.net/npm/@emception/sysroot@1.2.3/manifest.json`.
 */
export const DEFAULT_MANIFEST_URL = 'https://cdn.jsdelivr.net/npm/@emception/sysroot/manifest.json';

export interface CreateEmceptionOptions {
    /**
     * Either an HTMLElement to mount a new xterm.js Terminal into, or an
     * existing `Terminal` instance to reuse (advanced).
     */
    container: HTMLElement | import('@xterm/xterm').Terminal;
    /**
     * URL of the manifest produced by `npm run build:manifest`.
     *
     * If omitted, falls back to {@link DEFAULT_MANIFEST_URL} (the latest
     * published `@emception/sysroot` on jsDelivr). For production deploys
     * pin a specific version or self-host the manifest under your own
     * origin to avoid CDN drift.
     */
    manifestUrl?: string;
}

export interface EmceptionAPI {
    /** Run a tool by name (e.g. `'clang'`, `'ninja'`, `'python'`). */
    run(tool: string, argv: readonly string[], options?: RunOptions): Promise<ToolResult>;
    /** Read a file from the in-browser VFS (returns `null` if missing). */
    readFile(path: string): Promise<Uint8Array | null>;
    /** Write a file into the in-browser VFS (creates parent dirs as needed). */
    writeFile(path: string, data: Uint8Array | string): Promise<void>;
    /** List directory entries (returns `[]` if the path doesn't exist). */
    listDir(path: string): Promise<string[]>;
    /** Erase the persistent writable VFS layers (`/tmp`, `/home/user`). */
    resetVfs(): Promise<void>;
    /** Terminate the worker and tear down internal resources. */
    dispose(): void;
}

export async function createEmception(opts: CreateEmceptionOptions): Promise<EmceptionAPI> {
    const manifestUrl = opts.manifestUrl ?? DEFAULT_MANIFEST_URL;
    const { client } = await bootInWorker(manifestUrl, opts.container);

    const encoder = new TextEncoder();
    const toBytes = (data: Uint8Array | string): Uint8Array =>
        typeof data === 'string' ? encoder.encode(data) : data;

    return {
        run: (tool, argv, options) => client.run(tool, [...argv], options ?? {}),
        readFile: (path) => client.getFile(path),
        writeFile: (path, data) => client.writeFile(path, toBytes(data)),
        listDir: (path) => client.listDir(path),
        resetVfs: () => client.resetVfs(),
        dispose: () => client.terminate(),
    };
}
