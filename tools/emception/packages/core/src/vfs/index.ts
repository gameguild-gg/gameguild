// VFS surface re-exports. Pure runtime-agnostic pieces.

export type { FSStats, IFileSystem } from './interface.js';
export { OverlayFS } from './overlay.js';
export type { FSManifest, ManifestBundle, ManifestEntry } from './manifest.js';
