/**
 * `emception cdn-export <dir>` — export the sysroot CDN payload to a local
 * directory (Phase 1.3).
 *
 * Useful when a host wants to self-serve the manifest + bundles instead of
 * fetching from jsDelivr at runtime (offline deploys, on-prem LMS, CSP
 * locked-down origins, etc.).
 *
 * Strategy: fetch the manifest, then fetch every bundle URL referenced by
 * it, and write everything under `<dir>/` preserving the relative layout.
 * The manifest itself lands at `<dir>/manifest.json`.
 */

import { mkdir, writeFile } from 'node:fs/promises';
import { dirname, join, posix } from 'node:path';

const DEFAULT_MANIFEST_URL = 'https://cdn.jsdelivr.net/npm/@emception/sysroot/manifest.json';

export interface CdnExportOptions {
  /** Source manifest URL. Defaults to latest @emception/sysroot on jsDelivr. */
  fromUrl?: string;
  /** Destination directory (must be writable, will be created). */
  toDir: string;
  /** Per-asset progress callback. */
  onProgress?: (info: { asset: string; index: number; total: number; bytes: number }) => void;
}

export interface CdnExportResult {
  manifestUrl: string;
  toDir: string;
  bundleCount: number;
  totalBytes: number;
  durationMs: number;
}

interface ManifestShape {
  bundles?: Array<{ name?: string; url?: string; path?: string; bytes?: number }>;
  // Older shape compatibility:
  files?: Array<{ url?: string; path?: string }>;
}

async function fetchBytes(url: string): Promise<Uint8Array> {
  const res = await fetch(url);
  if (!res.ok) throw new Error(`fetch ${url} -> HTTP ${res.status}`);
  const buf = await res.arrayBuffer();
  return new Uint8Array(buf);
}

async function writeAsset(toDir: string, relPath: string, data: Uint8Array): Promise<void> {
  // Reject path traversal — relPath must be a clean relative path.
  if (relPath.includes('..') || relPath.startsWith('/')) {
    throw new Error(`cdn-export: refusing to write outside target dir: ${relPath}`);
  }
  const abs = join(toDir, relPath);
  await mkdir(dirname(abs), { recursive: true });
  await writeFile(abs, data);
}

function relativeFromManifestDir(manifestUrl: string, assetUrl: string): string {
  // If the asset URL is absolute, derive the path relative to the manifest dir.
  // If it's already relative, just use it.
  try {
    const manifestDir = manifestUrl.replace(/\/[^/]*$/, '/');
    const abs = new URL(assetUrl, manifestDir).href;
    if (abs.startsWith(manifestDir)) {
      return abs.slice(manifestDir.length);
    }
    // Asset is on a different host; fall back to a sanitized basename path.
    const u = new URL(abs);
    return posix.join(u.host, u.pathname.replace(/^\//, ''));
  } catch {
    return assetUrl.replace(/^\.?\//, '');
  }
}

export async function runCdnExport(opts: CdnExportOptions): Promise<CdnExportResult> {
  const t0 = Date.now();
  const fromUrl = opts.fromUrl ?? DEFAULT_MANIFEST_URL;
  const toDir = opts.toDir;

  await mkdir(toDir, { recursive: true });

  // 1) Fetch + write the manifest itself.
  const manifestBytes = await fetchBytes(fromUrl);
  await writeAsset(toDir, 'manifest.json', manifestBytes);

  // 2) Parse and walk the bundle list.
  const manifest = JSON.parse(new TextDecoder().decode(manifestBytes)) as ManifestShape;
  const entries = [...(manifest.bundles ?? []), ...(manifest.files ?? [])];

  let totalBytes = manifestBytes.byteLength;
  let i = 0;
  for (const entry of entries) {
    const url = entry.url ?? entry.path;
    if (!url) continue;
    const absUrl = new URL(url, fromUrl).href;
    const data = await fetchBytes(absUrl);
    const rel = relativeFromManifestDir(fromUrl, url);
    await writeAsset(toDir, rel, data);
    totalBytes += data.byteLength;
    i += 1;
    opts.onProgress?.({ asset: rel, index: i, total: entries.length, bytes: data.byteLength });
  }

  return {
    manifestUrl: fromUrl,
    toDir,
    bundleCount: entries.length,
    totalBytes,
    durationMs: Date.now() - t0,
  };
}

export function formatExportResult(r: CdnExportResult): string {
  const mb = (r.totalBytes / (1024 * 1024)).toFixed(1);
  const sec = (r.durationMs / 1000).toFixed(1);
  return [
    `emception cdn-export complete`,
    `  source:  ${r.manifestUrl}`,
    `  target:  ${r.toDir}`,
    `  bundles: ${r.bundleCount}`,
    `  size:    ${mb} MiB`,
    `  time:    ${sec}s`,
  ].join('\n');
}
