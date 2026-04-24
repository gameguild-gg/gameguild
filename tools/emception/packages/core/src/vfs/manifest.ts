/**
 * Sysroot manifest types: shape produced by `tools/emception/scripts/generate-manifest.ts`
 * and consumed by LazyFS (in `@emception/browser`).
 *
 * Pure types — no runtime dependencies.
 */

export interface ManifestEntry {
    size: number;
    hash: string;
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

export interface FSManifest {
    version: number;
    generated: string;
    baseUrl: string;
    toolVersions?: {
        pythonMajorMinor: string;
        pythonMajorMinorCompact: string;
    };
    files: {
        [path: string]: ManifestEntry;
    };
    bundles: {
        [name: string]: ManifestBundle;
    };
}
