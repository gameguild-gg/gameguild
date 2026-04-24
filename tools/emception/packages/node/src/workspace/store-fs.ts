// Phase 7.3 — fs-backed WorkspaceManager.
//
// Mirrors the in-memory store from @emception/core/workspace/store-memory but
// persists to a real filesystem. Each workspace lives at:
//
//   <root>/<workspace-name>/
//     <workspace-files...>
//     .emception/build.json   — WorkspaceBuildConfig sidecar (Phase 3.4)
//     .emception/meta.json    — file metadata sidecar     (Phase 3.3)
//     .emception/seed.lock    — seed marker               (Phase 3.2)
//
// Writes are atomic via temp+rename. File-locking via proper-lockfile is
// optional (deferred — single-tenant CI usage is the primary v1 case).

import { promises as fsp } from 'node:fs';
import * as path from 'node:path';

import {
    BuildConfigError,
    WorkspaceConflictError,
    hashSeed,
    normalizeSeedEntry,
    type FileEntry,
    type FileMeta,
    type MetaSidecar,
    type OpenWorkspaceOptions,
    type SeedMarker,
    type SeedPolicy,
    type WorkspaceBuildConfig,
    type WorkspaceHandle,
    type WorkspaceManager,
    type WorkspaceSeed,
} from '@emception/core';

const SIDECAR_DIR = '.emception';
const BUILD_FILE = 'build.json';
const META_FILE = 'meta.json';
const SEED_FILE = 'seed.lock';

function defaultMountPath(name: string): string {
    return `/workspace/${name}`;
}

async function readJsonOr<T>(filePath: string, fallback: T): Promise<T> {
    try {
        const raw = await fsp.readFile(filePath, 'utf8');
        return JSON.parse(raw) as T;
    } catch (err) {
        if ((err as NodeJS.ErrnoException).code === 'ENOENT') return fallback;
        throw err;
    }
}

async function writeJsonAtomic(filePath: string, data: unknown): Promise<void> {
    await fsp.mkdir(path.dirname(filePath), { recursive: true });
    const tmp = `${filePath}.tmp-${process.pid}-${Date.now()}`;
    await fsp.writeFile(tmp, JSON.stringify(data, null, 2), 'utf8');
    await fsp.rename(tmp, filePath);
}

async function writeBytesAtomic(
    filePath: string,
    data: Uint8Array | string,
): Promise<void> {
    await fsp.mkdir(path.dirname(filePath), { recursive: true });
    const tmp = `${filePath}.tmp-${process.pid}-${Date.now()}`;
    await fsp.writeFile(tmp, data);
    await fsp.rename(tmp, filePath);
}

/**
 * Walk a workspace root and collect every file that is NOT inside the
 * .emception/ sidecar dir. Paths returned are workspace-relative with
 * forward slashes.
 */
async function walkWorkspaceFiles(root: string): Promise<string[]> {
    const out: string[] = [];
    async function visit(dir: string, rel: string): Promise<void> {
        const entries = await fsp.readdir(dir, { withFileTypes: true }).catch(() => []);
        for (const entry of entries) {
            const childRel = rel ? `${rel}/${entry.name}` : entry.name;
            if (childRel.startsWith(SIDECAR_DIR)) continue;
            const childAbs = path.join(dir, entry.name);
            if (entry.isDirectory()) await visit(childAbs, childRel);
            else if (entry.isFile()) out.push(childRel);
        }
    }
    await visit(root, '');
    out.sort();
    return out;
}

class FsWorkspaceHandle implements WorkspaceHandle {
    constructor(
        public readonly name: string,
        public readonly mountPath: string,
        private readonly root: string,
    ) { }

    private resolve(rel: string): string {
        // Reject path-traversal attempts; the workspace root is a security
        // boundary even when callers are trusted.
        const norm = path.posix.normalize(rel.replace(/\\/g, '/'));
        if (norm.startsWith('..') || path.isAbsolute(norm)) {
            throw new BuildConfigError(`Path escapes workspace root: ${rel}`);
        }
        return path.join(this.root, norm);
    }

    async readFile(rel: string): Promise<Uint8Array | null> {
        try {
            const buf = await fsp.readFile(this.resolve(rel));
            return new Uint8Array(buf.buffer, buf.byteOffset, buf.byteLength);
        } catch (err) {
            if ((err as NodeJS.ErrnoException).code === 'ENOENT') return null;
            throw err;
        }
    }

    async writeFile(
        rel: string,
        data: Uint8Array | string,
        meta?: FileMeta,
    ): Promise<void> {
        await writeBytesAtomic(this.resolve(rel), data);
        if (meta && Object.keys(meta).length > 0) {
            const sidecar = await this.readMeta();
            sidecar.files[rel] = { ...(sidecar.files[rel] ?? {}), ...meta };
            await this.writeMeta(sidecar);
        }
    }

    async deleteFile(rel: string): Promise<void> {
        try {
            await fsp.unlink(this.resolve(rel));
        } catch (err) {
            if ((err as NodeJS.ErrnoException).code === 'ENOENT') {
                throw new BuildConfigError(`File not found: ${rel}`);
            }
            throw err;
        }
        const sidecar = await this.readMeta();
        if (sidecar.files[rel]) {
            delete sidecar.files[rel];
            await this.writeMeta(sidecar);
        }
    }

    async listFiles(opts?: {
        includeHidden?: boolean;
        includeSolution?: boolean;
    }): Promise<Array<{ path: string } & FileMeta & { size: number }>> {
        const includeHidden = opts?.includeHidden ?? true;
        const includeSolution = opts?.includeSolution ?? true;
        const sidecar = await this.readMeta();
        const paths = await walkWorkspaceFiles(this.root);
        const out: Array<{ path: string } & FileMeta & { size: number }> = [];
        for (const p of paths) {
            const meta = sidecar.files[p] ?? {};
            const v = meta.visibility;
            if (v === 'hidden' && !includeHidden) continue;
            if (v === 'solution' && !includeSolution) continue;
            const stat = await fsp.stat(this.resolve(p));
            out.push({ path: p, size: stat.size, ...meta });
        }
        return out;
    }

    async setVisibility(rel: string, v: FileEntry['visibility']): Promise<void> {
        // Confirm the file actually exists before tagging it.
        try {
            await fsp.access(this.resolve(rel));
        } catch {
            throw new BuildConfigError(`File not found: ${rel}`);
        }
        const sidecar = await this.readMeta();
        sidecar.files[rel] = { ...(sidecar.files[rel] ?? {}), visibility: v };
        await this.writeMeta(sidecar);
    }

    async getBuild(): Promise<WorkspaceBuildConfig> {
        return readJsonOr<WorkspaceBuildConfig>(
            path.join(this.root, SIDECAR_DIR, BUILD_FILE),
            {},
        );
    }

    async setBuild(build: WorkspaceBuildConfig): Promise<void> {
        await writeJsonAtomic(
            path.join(this.root, SIDECAR_DIR, BUILD_FILE),
            build,
        );
    }

    async reset(): Promise<void> {
        await fsp.rm(this.root, { recursive: true, force: true });
        await fsp.mkdir(this.root, { recursive: true });
    }

    async dispose(): Promise<void> {
        // No persistent handles held; nothing to release.
    }

    /** Read meta sidecar, returning an empty record when missing. */
    async readMeta(): Promise<MetaSidecar> {
        return readJsonOr<MetaSidecar>(
            path.join(this.root, SIDECAR_DIR, META_FILE),
            { files: {} },
        );
    }

    async writeMeta(sidecar: MetaSidecar): Promise<void> {
        await writeJsonAtomic(
            path.join(this.root, SIDECAR_DIR, META_FILE),
            sidecar,
        );
    }

    async readSeedMarker(): Promise<SeedMarker | null> {
        return readJsonOr<SeedMarker | null>(
            path.join(this.root, SIDECAR_DIR, SEED_FILE),
            null,
        );
    }

    async writeSeedMarker(marker: SeedMarker): Promise<void> {
        await writeJsonAtomic(
            path.join(this.root, SIDECAR_DIR, SEED_FILE),
            marker,
        );
    }
}

async function applySeed(
    handle: FsWorkspaceHandle,
    seed: WorkspaceSeed,
    policy: SeedPolicy,
): Promise<SeedMarker> {
    const hash = hashSeed(seed);
    const existing = await handle.readSeedMarker();

    if (existing && existing.hash === hash && policy === 'once') {
        return existing;
    }
    if (existing && existing.hash !== hash && policy === 'once') {
        throw new WorkspaceConflictError(
            `Workspace '${handle.name}' was seeded with a different content hash; ` +
            `seedPolicy='once' refuses to overwrite (expected ${existing.hash}, got ${hash}).`,
        );
    }

    const isMerge = policy === 'merge';
    if (!isMerge) {
        // Wipe non-sidecar files so 'overwrite' truly overwrites.
        const existingFiles = await walkWorkspaceFiles((handle as unknown as { root: string }).root);
        await Promise.all(
            existingFiles.map((p) =>
                fsp.unlink(path.join((handle as unknown as { root: string }).root, p)).catch(() => undefined),
            ),
        );
    }

    const meta = await handle.readMeta();
    for (const [p, raw] of Object.entries(seed)) {
        const entry = normalizeSeedEntry(raw);
        if (isMerge && (await handle.readFile(p)) !== null) continue;

        await handle.writeFile(p, entry.content);

        const fileMeta: FileMeta = {};
        if (entry.visibility !== undefined) fileMeta.visibility = entry.visibility;
        if (entry.readonly !== undefined) fileMeta.readonly = entry.readonly;
        if (entry.executable !== undefined) fileMeta.executable = entry.executable;
        if (Object.keys(fileMeta).length > 0) {
            meta.files[p] = { ...(meta.files[p] ?? {}), ...fileMeta };
        }
    }
    await handle.writeMeta(meta);

    const marker: SeedMarker = { hash, appliedAt: Date.now(), policy };
    await handle.writeSeedMarker(marker);
    return marker;
}

export interface FsWorkspaceManagerOptions {
    /** Filesystem root that holds every workspace as a child directory. */
    root: string;
}

/**
 * Filesystem-backed WorkspaceManager. Workspaces are top-level directories
 * under `root`; each carries its own .emception/ sidecar.
 */
export class FsWorkspaceManager implements WorkspaceManager {
    private disposed = false;
    constructor(private readonly opts: FsWorkspaceManagerOptions) { }

    async list(): Promise<string[]> {
        this.assertLive();
        const entries = await fsp.readdir(this.opts.root, { withFileTypes: true }).catch((err) => {
            if ((err as NodeJS.ErrnoException).code === 'ENOENT') return [] as never[];
            throw err;
        });
        return entries
            .filter((e) => e.isDirectory())
            .map((e) => e.name)
            .sort();
    }

    async open(opts: OpenWorkspaceOptions): Promise<WorkspaceHandle> {
        this.assertLive();
        const root = path.join(this.opts.root, opts.name);
        await fsp.mkdir(root, { recursive: true });

        const handle = new FsWorkspaceHandle(
            opts.name,
            opts.mountPath ?? defaultMountPath(opts.name),
            root,
        );

        if (opts.build) {
            const existing = await handle.getBuild();
            if (Object.keys(existing).length === 0) {
                await handle.setBuild(opts.build);
            }
        }

        if (opts.seed) {
            await applySeed(handle, opts.seed, opts.seedPolicy ?? 'overwrite');
        }

        return handle;
    }

    async remove(name: string): Promise<void> {
        this.assertLive();
        const target = path.join(this.opts.root, name);
        try {
            await fsp.rm(target, { recursive: true, force: false });
        } catch (err) {
            if ((err as NodeJS.ErrnoException).code === 'ENOENT') {
                throw new BuildConfigError(`Workspace not found: ${name}`);
            }
            throw err;
        }
    }

    async dispose(): Promise<void> {
        this.disposed = true;
    }

    private assertLive(): void {
        if (this.disposed) {
            throw new BuildConfigError('FsWorkspaceManager has been disposed.');
        }
    }
}

/** Convenience factory mirroring the createMemoryWorkspaceManager() naming. */
export function createFsWorkspaceManager(
    opts: FsWorkspaceManagerOptions,
): WorkspaceManager {
    return new FsWorkspaceManager(opts);
}
