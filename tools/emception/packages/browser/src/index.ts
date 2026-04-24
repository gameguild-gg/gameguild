// @emception/browser — Web Worker + IDB + OffscreenCanvas adapter.
// Phase 0.2 in progress. createEmception() lands later this phase once
// worker-client/worker-entry/tool-runner are migrated.

export type { EmceptionAPI, RunOptions, ToolResult, WorkspaceOptions } from '@emception/core';

// VFS subsurface: LazyFS (CDN-backed lazy load) + IDBFS (write layer)
// + OverlayFS composition + Emscripten FS bridge. Migrated Phase 0.2.
export {
  IDBFS,
  LazyFS,
  createVFSManager,
  mountVFSFS,
  type FileEntry,
  type FSManifest,
  type IDBFSOptions,
  type MountVFSFSOptions,
  type VFSFSRuntime,
  type VFSManager,
} from './vfs/index';

export const DEFAULT_MANIFEST_URL =
  'https://cdn.jsdelivr.net/npm/@emception/sysroot@0.20.0/manifest.json';

export async function createEmception(_opts?: unknown): Promise<never> {
  throw new Error(
    '@emception/browser: createEmception() not yet implemented. ' +
      'Phase 0.2 will migrate the working implementation from tools/emception/src/.'
  );
}
