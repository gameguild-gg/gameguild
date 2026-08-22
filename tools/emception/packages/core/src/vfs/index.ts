// VFS surface exports. Pure runtime-agnostic pieces.

export type { FSStats, IFileSystem } from './interface.js';
export type {
  FSManifest,
  LegacyFSManifest,
  ManifestBundle,
  ManifestEntry,
  ManifestToolVersions,
  ToolchainSourceProvenance,
  ReleaseFSManifest,
  WasmArtifactProfile,
} from './manifest.js';
export { OverlayFS } from './overlay.js';

