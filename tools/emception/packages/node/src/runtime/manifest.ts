// Phase 7.6 — Manifest loader for the Node runtime.
//
// In the browser, manifests are fetched via HTTP. Under Node, the canonical
// path is to resolve `@emception/sysroot/manifest.json` from `node_modules`
// and read it from disk. We also accept an explicit `path` (for self-host or
// CI artifacts) and `url` (rare but useful for ad-hoc CDN testing).

import { promises as fsp } from 'node:fs';
import { createRequire } from 'node:module';

import type { FSManifest } from '@emception/core';
import { BuildConfigError } from '@emception/core';

export interface LoadManifestOptions {
    /** Absolute path to a manifest.json on disk. */
    path?: string;
    /** Fetchable URL (uses global fetch — Node 18+). */
    url?: string;
}

const require = createRequire(import.meta.url);

/**
 * Resolve and parse a sysroot manifest. Lookup order:
 *
 *   1. `opts.path`           — explicit disk path, parsed directly.
 *   2. `opts.url`            — fetched via global fetch.
 *   3. `@emception/sysroot/manifest.json` — resolved through Node's module
 *      lookup so the consumer's `node_modules` decides which version wins.
 */
export async function loadManifest(opts: LoadManifestOptions = {}): Promise<FSManifest> {
    if (opts.path) return readJson(opts.path);

    if (opts.url) {
        const res = await fetch(opts.url);
        if (!res.ok) {
            throw new BuildConfigError(
                `Failed to fetch manifest from ${opts.url}: ${res.status} ${res.statusText}`,
            );
        }
        return (await res.json()) as FSManifest;
    }

    let resolved: string;
    try {
        resolved = require.resolve('@emception/sysroot/manifest.json');
    } catch (err) {
        throw new BuildConfigError(
            '@emception/sysroot/manifest.json could not be resolved. ' +
            'Install @emception/sysroot or pass {path} / {url} explicitly.',
            err instanceof Error ? err : undefined,
        );
    }
    return readJson(resolved);
}

async function readJson(filePath: string): Promise<FSManifest> {
    const raw = await fsp.readFile(filePath, 'utf8');
    return JSON.parse(raw) as FSManifest;
}
