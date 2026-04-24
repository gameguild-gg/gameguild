// @emception/core — runtime-agnostic surface.
// Phase 0 skeleton + Phase 0.2 first slice (pure VFS / TTY / worker-protocol types).

export * from './errors';
export * from './presets';
export * from './types';

// Subsystem namespaces (full surface).
export * as tty from './tty';
export * as vfs from './vfs';
export * as workerProtocol from './worker-protocol';
export * as runtime from './runtime/adapter';

// Top-level re-exports for the most commonly imported helpers.
export type { IOProvider } from './tty/io-provider';
export { LineBuffer } from './tty/line-buffer';
export type { FSStats, IFileSystem } from './vfs/interface';
export type { FSManifest, ManifestBundle, ManifestEntry } from './vfs/manifest';
export { OverlayFS } from './vfs/overlay';
export type { MainToWorkerMessage, WorkerToMainMessage } from './worker-protocol';
export type {
  ManifestSource,
  RuntimeAdapter,
  SpawnWorkerOptions,
  WorkerHandle,
  WorkspaceStoreHandle,
  WorkspaceStoreOptions,
} from './runtime/adapter';

