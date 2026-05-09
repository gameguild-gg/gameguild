/**
 * VFS manager: exposes fetchFile and getUrl for the tool runner.
 * Composes OverlayFS (unified view, from `emception`) with LazyFS (CDN URLs).
 */

import type { FSStats, OverlayFS } from 'emception';
import { LazyFS } from './lazy';

export { mountVFSFS, type MountVFSFSOptions, type VFSFSRuntime } from './emscripten-vfsfs';
export { IDBFS } from './idb';
export type { IDBFSOptions } from './idb';
export { LazyFS, type FileEntry, type FSManifest } from './lazy';

export interface VFSManager {
    /** Used by tool runner when a WASM module needs a file not yet in Emscripten FS. */
    fetchFile(path: string): Promise<Uint8Array | null>;
    /** Used by tool runner for Emscripten locateFile (WASM/JS glue URLs). */
    getUrl(path: string): string;
    /** Async getUrl: ensures the file's bundle is loaded first, returns blob URL for bundled files. */
    getUrlAsync(path: string): Promise<string>;
    /** Preload a bundle by name so that subsequent getUrl calls return blob URLs. */
    preloadBundle(bundleName: string): Promise<void>;
    /** Get the bundle name a file belongs to, or undefined. */
    getBundleForFile(path: string): string | undefined;
    /** Get all file paths belonging to a bundle. */
    getBundleFilePaths(bundleName: string): string[];
    /** Expose overlay for direct read/write (e.g. tests, shell). */
    readonly overlay: OverlayFS;
    /** Expose LazyFS for CDN asset loading (bundle preloading, URL resolution). */
    readonly lazyFs: LazyFS;

    // ---------- Sync VFS access for Emscripten FS proxy ----------
    /** Sync file read from write cache (null on miss). VFSFS uses fetchFile for on-demand loading. */
    readFileSync(path: string): Uint8Array | null;
    /** Sync file write to overlay write layer (write cache + fire-and-forget IDB). */
    writeFileSync(path: string, data: Uint8Array): void;
    /** Sync stat from manifest metadata or write cache. */
    statSync(path: string): FSStats | null;
    /** Sync readdir from dirIndex + write cache keys. */
    readdirSync(path: string): string[];
    /** Sync existence check from manifest + write cache. */
    existsSync(path: string): boolean;
    /** Sync mkdir in overlay. */
    mkdirSync(path: string): void;
    /** Sync delete from overlay. */
    deleteFileSync(path: string): boolean;
    /** Preload all files under a directory into IDB. */
    preloadDir(path: string): Promise<void>;
}

export function createVFSManager(overlay: OverlayFS, lazyFs: LazyFS): VFSManager {
    return {
        overlay,
        lazyFs,
        async fetchFile(path: string): Promise<Uint8Array | null> {
            return overlay.readFile(path);
        },
        getUrl(path: string): string {
            return lazyFs.getUrl(path);
        },
        async getUrlAsync(path: string): Promise<string> {
            return lazyFs.getUrlAsync(path);
        },
        async preloadBundle(bundleName: string): Promise<void> {
            return lazyFs.preloadBundle(bundleName);
        },
        getBundleForFile(path: string): string | undefined {
            return lazyFs.getBundleForFile(path);
        },
        getBundleFilePaths(bundleName: string): string[] {
            return lazyFs.getBundleFilePaths(bundleName);
        },

        // Sync methods delegate to overlay's sync methods
        readFileSync(path: string): Uint8Array | null {
            return overlay.readFileSync(path);
        },
        writeFileSync(path: string, data: Uint8Array): void {
            overlay.writeFileSync(path, data);
        },
        statSync(path: string): FSStats | null {
            return overlay.statSync(path);
        },
        readdirSync(path: string): string[] {
            return overlay.readdirSync(path);
        },
        existsSync(path: string): boolean {
            return overlay.existsSync(path);
        },
        mkdirSync(path: string): void {
            overlay.mkdirSync(path);
        },
        deleteFileSync(path: string): boolean {
            return overlay.deleteFileSync(path);
        },
        async preloadDir(path: string): Promise<void> {
            return lazyFs.preloadDir(path);
        },
    };
}
