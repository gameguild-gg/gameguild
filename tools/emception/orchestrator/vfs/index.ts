/**
 * VFS manager: exposes fetchFile and getUrl for the tool runner.
 * Composes OverlayFS (unified view) with LazyFS (CDN URLs).
 */

import { LazyFS } from './lazy';
import { OverlayFS } from './overlay';

export { IDBFS } from './idb';
export type { IDBFSOptions } from './idb';
export type { FSStats, IFileSystem } from './interface';
export { LazyFS, type FileEntry, type FSManifest } from './lazy';
export { OverlayFS } from './overlay';

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
  /** Expose overlay for direct read/write (e.g. tests, shell). */
  readonly overlay: OverlayFS;
}

export function createVFSManager(overlay: OverlayFS, lazyFs: LazyFS): VFSManager {
  return {
    overlay,
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
  };
}
