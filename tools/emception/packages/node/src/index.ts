// @emception/node — worker_threads + fs adapter. Phase 7 implementation.

export type { EmceptionAPI, RunOptions, ToolResult, WorkspaceOptions } from '@emception/core';

export {
    createFsWorkspaceManager,
    FsWorkspaceManager,
    type FsWorkspaceManagerOptions,
} from './workspace/store-fs.js';

export {
    processStdio,
    readableToWeb,
    writableToWeb,
} from './io/node-streams.js';

export {
    loadManifest,
    type LoadManifestOptions,
} from './runtime/manifest.js';

export async function createEmception(_opts?: unknown): Promise<never> {
  throw new Error(
    '@emception/node: createEmception() not yet implemented. Phase 7 will land the worker_threads runtime.'
  );
}
