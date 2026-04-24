// VFS surface re-exports. Pure runtime-agnostic pieces.

export type { FSStats, IFileSystem } from './interface';
export { OverlayFS } from './overlay';
export type { FSManifest, ManifestBundle, ManifestEntry } from './manifest';
