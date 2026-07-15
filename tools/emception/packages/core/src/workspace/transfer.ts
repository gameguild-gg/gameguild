// High-level workspace export/import wrappers around `zip.ts`.
//
// `exportWorkspace(handle, opts?)` snapshots a workspace into a stored ZIP
// archive that includes the file contents plus the `.emception/{meta,build}`
// sidecars so an instructor's seed workspace round-trips perfectly.
//
// `importWorkspace(handle, archive, opts?)` is the inverse: it reads the ZIP
// and writes every entry into the target workspace handle. Behaviour for
// pre-existing files is governed by `opts.policy`:
//   - `'overwrite'` (default) — every entry from the archive replaces any
//     same-named entry already in the workspace.
//   - `'merge'` — existing files are kept; only entries the workspace does
//     not yet have are written.
//
// Pure logic — no DOM, no Node imports — so both browsers and Node graders
// can use the same code path.

import { EmceptionError } from '../errors';
import type { WorkspaceBuildConfig } from '../types';
import type { MetaSidecar, WorkspaceHandle } from './manager';
import { createZip, readZip, type CreateZipOptions, type ZipEntry } from './zip';

const TEXT_ENC = new TextEncoder();
const TEXT_DEC = new TextDecoder('utf-8', { fatal: false });

const META_PATH = '.emception/meta.json';
const BUILD_PATH = '.emception/build.json';

export interface ExportWorkspaceOptions extends CreateZipOptions {
  /** Include hidden files (default: true). */
  includeHidden?: boolean;
  /** Include solution files (default: true). */
  includeSolution?: boolean;
  /** Include the `.emception/build.json` sidecar (default: true). */
  includeBuild?: boolean;
  /** Include the `.emception/meta.json` sidecar (default: true). */
  includeMeta?: boolean;
}

/**
 * Snapshot a workspace into a ZIP byte buffer. The result is a complete,
 * standalone archive: it can be re-imported by {@link importWorkspace} or
 * unzipped by any standard ZIP tool.
 */
export async function exportWorkspace(handle: WorkspaceHandle, opts: ExportWorkspaceOptions = {}): Promise<Uint8Array> {
  if (!handle) throw new EmceptionError('exportWorkspace: handle is required');
  const includeHidden = opts.includeHidden ?? true;
  const includeSolution = opts.includeSolution ?? true;
  const includeBuild = opts.includeBuild ?? true;
  const includeMeta = opts.includeMeta ?? true;

  const files = await handle.listFiles({ includeHidden, includeSolution });
  const entries: ZipEntry[] = [];
  const meta: MetaSidecar = { files: {} };

  // Sort for stable output across runs.
  const sorted = [...files].sort((a, b) => (a.path < b.path ? -1 : a.path > b.path ? 1 : 0));
  for (const f of sorted) {
    if (f.path === META_PATH || f.path === BUILD_PATH) continue; // synthesized below
    const bytes = await handle.readFile(f.path);
    if (!bytes) continue; // race or vanished file — skip silently
    entries.push({ path: f.path, data: bytes });
    const fileMeta: Record<string, unknown> = {};
    if (f.visibility !== undefined) fileMeta.visibility = f.visibility;
    if (f.readonly !== undefined) fileMeta.readonly = f.readonly;
    if (f.executable !== undefined) fileMeta.executable = f.executable;
    if (Object.keys(fileMeta).length > 0) meta.files[f.path] = fileMeta;
  }

  if (includeMeta && Object.keys(meta.files).length > 0) {
    entries.push({ path: META_PATH, data: TEXT_ENC.encode(JSON.stringify(meta, null, 2)) });
  }
  if (includeBuild) {
    const build = await handle.getBuild();
    entries.push({ path: BUILD_PATH, data: TEXT_ENC.encode(JSON.stringify(build, null, 2)) });
  }

  return createZip(entries, opts);
}

export type ImportPolicy = 'overwrite' | 'merge';

export interface ImportWorkspaceOptions {
  /** How to reconcile entries that already exist (default: `'overwrite'`). */
  policy?: ImportPolicy;
  /** Apply the included `.emception/build.json` if present (default: true). */
  applyBuild?: boolean;
}

export interface ImportWorkspaceReport {
  /** Files written (newly created or overwritten). */
  written: string[];
  /** Files skipped because of `'merge'` policy. */
  skipped: string[];
  /** Whether `.emception/build.json` from the archive was applied. */
  appliedBuild: boolean;
}

/**
 * Read a ZIP archive and write its entries into a workspace.
 */
export async function importWorkspace(handle: WorkspaceHandle, archive: Uint8Array, opts: ImportWorkspaceOptions = {}): Promise<ImportWorkspaceReport> {
  if (!handle) throw new EmceptionError('importWorkspace: handle is required');
  const policy: ImportPolicy = opts.policy ?? 'overwrite';
  const applyBuild = opts.applyBuild ?? true;

  const entries = readZip(archive);
  const report: ImportWorkspaceReport = { written: [], skipped: [], appliedBuild: false };

  // Resolve the meta sidecar up-front so per-file writes can carry metadata.
  let meta: MetaSidecar | null = null;
  let buildEntry: ZipEntry | undefined;
  const fileEntries: ZipEntry[] = [];
  for (const e of entries) {
    if (e.path === META_PATH) {
      try {
        const parsed = JSON.parse(TEXT_DEC.decode(e.data)) as MetaSidecar;
        meta = parsed && typeof parsed === 'object' && parsed.files ? parsed : null;
      } catch (cause) {
        throw new EmceptionError(`importWorkspace: failed to parse ${META_PATH}`, cause);
      }
    } else if (e.path === BUILD_PATH) {
      buildEntry = e;
    } else {
      fileEntries.push(e);
    }
  }

  let existing: Set<string> | null = null;
  if (policy === 'merge') {
    const list = await handle.listFiles({ includeHidden: true, includeSolution: true });
    existing = new Set(list.map((f) => f.path));
  }

  for (const e of fileEntries) {
    if (existing && existing.has(e.path)) {
      report.skipped.push(e.path);
      continue;
    }
    const fileMeta = meta?.files[e.path];
    await handle.writeFile(e.path, e.data, fileMeta);
    report.written.push(e.path);
  }

  if (applyBuild && buildEntry) {
    try {
      const parsed = JSON.parse(TEXT_DEC.decode(buildEntry.data)) as WorkspaceBuildConfig;
      await handle.setBuild(parsed);
      report.appliedBuild = true;
    } catch (cause) {
      throw new EmceptionError(`importWorkspace: failed to apply ${BUILD_PATH}`, cause);
    }
  }

  return report;
}
