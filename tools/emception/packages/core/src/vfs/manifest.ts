/** Pure artifact-manifest contracts shared by core and browser adapters. */

export interface ManifestEntry {
  size?: number;
  hash?: string;
  executable?: boolean;
  symlink?: string;
  bundle?: string;
  priority?: 'critical' | 'high' | 'normal' | 'low';
}

export interface ManifestBundle {
  files: string[];
  url: string;
  size: number;
  hash: string;
}

export interface ManifestToolVersions {
  pythonMajorMinor: string;
  pythonMajorMinorCompact: string;
  readonly [tool: string]: string;
}

export interface WasmArtifactProfile {
  readonly kind: string;
  readonly glue: string;
  readonly wasm: string;
  readonly profileHash: string;
  readonly imports: readonly string[];
  readonly exports: readonly string[];
}

interface ManifestBase {
  version: 1 | 2;
  generated: string;
  baseUrl: string;
  toolVersions?: ManifestToolVersions;
  files: Record<string, ManifestEntry>;
  bundles: Record<string, ManifestBundle>;
}

export interface LegacyFSManifest extends ManifestBase {
  version: 1;
  schemaVersion?: never;
}

export interface ReleaseFSManifest extends ManifestBase {
  version: 2;
  schemaVersion: 2;
  artifactVersion: string;
  runtimeAbi: string;
  patchSetVersion: string;
  buildFingerprint: string;
  toolVersions: ManifestToolVersions;
  profiles: Record<string, WasmArtifactProfile>;
}

export type FSManifest = LegacyFSManifest | ReleaseFSManifest;
