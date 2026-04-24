// @emception/core — runtime-agnostic surface.
// Phase 0 skeleton + Phase 0.2 first slice (pure VFS / TTY / worker-protocol types).

export * from './errors';
export * from './events';
export * from './presets';
export * from './types';

// Subsystem namespaces (full surface).
export * as io from './io';
export * as runtime from './runtime/adapter';
export * as tty from './tty';
export * as vfs from './vfs';
export * as workerProtocol from './worker-protocol';

// Top-level re-exports for the most commonly imported helpers.
export type {
    ManifestSource,
    RuntimeAdapter,
    SpawnWorkerOptions,
    WorkerHandle,
    WorkspaceStoreHandle,
    WorkspaceStoreOptions
} from './runtime/adapter';
export { HeadlessIOProvider, type HeadlessIOProviderOptions } from './tty/headless';
export type { IOProvider } from './tty/io-provider';
export { LineBuffer } from './tty/line-buffer';
export {
    decodeCollected,
    normalizeStdin,
    normalizeStdout,
    type NormalizedStdout,
} from './io/streams';
export type { FSStats, IFileSystem } from './vfs/interface';
export type { FSManifest, ManifestBundle, ManifestEntry } from './vfs/manifest';
export { OverlayFS } from './vfs/overlay';
export type { MainToWorkerMessage, WorkerToMainMessage } from './worker-protocol';

