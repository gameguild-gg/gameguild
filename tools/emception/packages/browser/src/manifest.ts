import type {
  FSManifest,
  LegacyFSManifest,
  ManifestBundle,
  ManifestEntry,
  ManifestToolVersions,
  ReleaseFSManifest,
  WasmArtifactProfile,
} from 'emception';

export const RUNTIME_ABI = 'emception-browser-v1';
export const DEFAULT_MANIFEST_URL = 'https://cdn.jsdelivr.net/npm/@gameguild/emception-toolchain@4.2.0/cdn/manifest.json';

export class ManifestCompatibilityError extends Error {
  readonly name = 'ManifestCompatibilityError';
}

export interface ParseManifestOptions {
  readonly expectedRuntimeAbi?: string;
  readonly onLegacy?: (message: string) => void;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function recordField(record: Record<string, unknown>, key: string): Record<string, unknown> {
  const value = record[key];
  if (!isRecord(value)) throw new ManifestCompatibilityError(`manifest.${key} must be an object map`);
  return value;
}

function stringField(record: Record<string, unknown>, key: string): string {
  const value = record[key];
  if (typeof value !== 'string' || value.length === 0) {
    throw new ManifestCompatibilityError(`manifest.${key} must be a non-empty string`);
  }
  return value;
}

function numberField(record: Record<string, unknown>, key: string): number {
  const value = record[key];
  if (typeof value !== 'number' || !Number.isFinite(value) || value < 0) {
    throw new ManifestCompatibilityError(`manifest.${key} must be a non-negative number`);
  }
  return value;
}

function isStringArray(value: unknown): value is string[] {
  return Array.isArray(value) && value.every((entry) => typeof entry === 'string');
}

function parseEntry(value: unknown, filePath: string): ManifestEntry {
  if (!isRecord(value)) throw new ManifestCompatibilityError(`manifest.files[${filePath}] must be an object`);
  if (typeof value.symlink === 'string') return { symlink: value.symlink };
  return {
    size: numberField(value, 'size'),
    hash: stringField(value, 'hash'),
    executable: typeof value.executable === 'boolean' ? value.executable : undefined,
    bundle: typeof value.bundle === 'string' ? value.bundle : undefined,
    priority: value.priority === 'critical' || value.priority === 'high' || value.priority === 'normal' || value.priority === 'low'
      ? value.priority
      : undefined,
  };
}

function parseFiles(value: Record<string, unknown>): Record<string, ManifestEntry> {
  return Object.fromEntries(Object.entries(value).map(([filePath, entry]) => [filePath, parseEntry(entry, filePath)]));
}

function parseBundle(value: unknown, name: string): ManifestBundle {
  if (!isRecord(value)) throw new ManifestCompatibilityError(`manifest.bundles[${name}] must be an object`);
  if (!isStringArray(value.files)) {
    throw new ManifestCompatibilityError(`manifest.bundles[${name}].files must be a string array`);
  }
  return {
    files: value.files,
    url: stringField(value, 'url'),
    size: numberField(value, 'size'),
    hash: stringField(value, 'hash'),
  };
}

function parseBundles(value: Record<string, unknown>): Record<string, ManifestBundle> {
  return Object.fromEntries(Object.entries(value).map(([name, bundle]) => [name, parseBundle(bundle, name)]));
}

function parseToolVersions(value: unknown): ManifestToolVersions {
  if (!isRecord(value)) throw new ManifestCompatibilityError('manifest.toolVersions must be an object map');
  const entries = Object.entries(value);
  if (entries.some(([, version]) => typeof version !== 'string')) {
    throw new ManifestCompatibilityError('manifest.toolVersions values must be strings');
  }
  const pythonMajorMinor = stringField(value, 'pythonMajorMinor');
  const pythonMajorMinorCompact = stringField(value, 'pythonMajorMinorCompact');
  const versions: Record<string, string> = { pythonMajorMinor, pythonMajorMinorCompact };
  for (const [tool, version] of entries) {
    if (typeof version === 'string') versions[tool] = version;
  }
  return { ...versions, pythonMajorMinor, pythonMajorMinorCompact };
}

function parseProfile(value: unknown, name: string): WasmArtifactProfile {
  if (!isRecord(value)) throw new ManifestCompatibilityError(`manifest.profiles[${name}] must be an object`);
  if (!isStringArray(value.imports) || !isStringArray(value.exports)) {
    throw new ManifestCompatibilityError(`manifest.profiles[${name}] imports and exports must be string arrays`);
  }
  const profileHash = stringField(value, 'profileHash');
  if (!/^[a-f0-9]{64}$/.test(profileHash)) {
    throw new ManifestCompatibilityError(`manifest.profiles[${name}].profileHash must be a SHA-256 hex string`);
  }
  return {
    kind: stringField(value, 'kind'),
    glue: stringField(value, 'glue'),
    wasm: stringField(value, 'wasm'),
    profileHash,
    imports: value.imports,
    exports: value.exports,
  };
}

function parseRelease(record: Record<string, unknown>, expectedRuntimeAbi: string): ReleaseFSManifest {
  if (record.version !== 2) throw new ManifestCompatibilityError('manifest.version must be 2 for schemaVersion 2');
  const runtimeAbi = stringField(record, 'runtimeAbi');
  if (runtimeAbi !== expectedRuntimeAbi) {
    throw new ManifestCompatibilityError(`manifest runtime ABI '${runtimeAbi}' is incompatible with '${expectedRuntimeAbi}'`);
  }
  const buildFingerprint = stringField(record, 'buildFingerprint');
  if (!/^[a-f0-9]{64}$/.test(buildFingerprint)) {
    throw new ManifestCompatibilityError('manifest.buildFingerprint must be a SHA-256 hex string');
  }
  const profileMap = recordField(record, 'profiles');
  const profiles = Object.fromEntries(
    Object.entries(profileMap).map(([name, profile]) => [name, parseProfile(profile, name)]),
  );
  const files = parseFiles(recordField(record, 'files'));
  for (const [name, profile] of Object.entries(profiles)) {
    if (!files[profile.glue]?.hash || !files[profile.wasm]?.hash) {
      throw new ManifestCompatibilityError(`manifest profile '${name}' references missing release file`);
    }
  }
  return {
    schemaVersion: 2,
    version: 2,
    artifactVersion: stringField(record, 'artifactVersion'),
    runtimeAbi,
    patchSetVersion: stringField(record, 'patchSetVersion'),
    buildFingerprint,
    generated: stringField(record, 'generated'),
    baseUrl: stringField(record, 'baseUrl'),
    toolVersions: parseToolVersions(record.toolVersions),
    profiles,
    files,
    bundles: parseBundles(recordField(record, 'bundles')),
  };
}

function parseLegacy(record: Record<string, unknown>, onLegacy?: (message: string) => void): LegacyFSManifest {
  if (record.version !== 1) throw new ManifestCompatibilityError('unsupported manifest schema');
  onLegacy?.('Loading deprecated manifest schema v1; rebuild artifacts with @gameguild/emception-toolchain.');
  return {
    version: 1,
    generated: stringField(record, 'generated'),
    baseUrl: stringField(record, 'baseUrl'),
    toolVersions: record.toolVersions === undefined ? undefined : parseToolVersions(record.toolVersions),
    files: parseFiles(recordField(record, 'files')),
    bundles: parseBundles(recordField(record, 'bundles')),
  };
}

export function parseManifest(value: unknown, options: ParseManifestOptions = {}): FSManifest {
  if (!isRecord(value)) throw new ManifestCompatibilityError('manifest must be an object');
  if (value.schemaVersion === 2) return parseRelease(value, options.expectedRuntimeAbi ?? RUNTIME_ABI);
  return parseLegacy(value, options.onLegacy);
}
