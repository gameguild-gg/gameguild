// @emception/core — runtime-agnostic surface.
// Phase 0 skeleton + Phase 0.2 first slice (pure VFS / TTY / worker-protocol types).

export * from './errors';
export * from './presets';
export * from './types';

// Subsystem namespaces (full surface).
export * as vfs from './vfs';
export * as tty from './tty';
export * as workerProtocol from './worker-protocol';

// Top-level re-exports for the most commonly imported helpers.
export type { IOProvider } from './tty/io-provider';
export { LineBuffer } from './tty/line-buffer';
export type { FSStats, IFileSystem } from './vfs/interface';
export { OverlayFS } from './vfs/overlay';
export type { FSManifest, ManifestBundle, ManifestEntry } from './vfs/manifest';

