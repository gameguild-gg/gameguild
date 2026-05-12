// VFS surface exports. Pure runtime-agnostic pieces.

export type { FSStats, IFileSystem } from './interface';
export type { FSManifest, ManifestBundle, ManifestEntry } from './manifest';
export { OverlayFS } from './overlay';

