// In-memory WorkspaceManager (runtime-agnostic).
//
// Implements the full WorkspaceManager / WorkspaceHandle contract entirely in
// process memory. Useful as:
//   - default backend when @emception/core is consumed in tests
//   - opt-in `workspaceStore: { kind: 'memory' }` in tests
//   - ephemeral backend for IDE single-file mode (`enableWorkspace=false`)
//
// No DOM, no Node, no IDB references — safe to import anywhere.

import { BuildConfigError, WorkspaceConflictError } from '../errors.js';
import { ToolchainPreset } from '../types.js';
import type {
    FileEntry,
    WorkspaceBuildConfig,
    WorkspaceSeed,
} from '../types.js';
import type {
    FileMeta,
    OpenWorkspaceOptions,
    SeedMarker,
    SeedPolicy,
    WorkspaceHandle,
    WorkspaceManager,
} from './manager.js';
import { hashSeed, normalizeSeedEntry } from './seed.js';

interface MemFile {
    bytes: Uint8Array;
    meta: FileMeta;
}

interface MemWorkspace {
    name: string;
    mountPath: string;
    files: Map<string, MemFile>;
    build: WorkspaceBuildConfig;
    seedMarker: SeedMarker | null;
}

const TEXT_ENCODER = /* @__PURE__ */ new TextEncoder();

function toBytes(data: Uint8Array | string): Uint8Array {
    return typeof data === 'string' ? TEXT_ENCODER.encode(data) : data;
}

function defaultMountPath(name: string): string {
    return `/workspace/${name}`;
}

class MemoryWorkspaceHandle implements WorkspaceHandle {
    constructor(private readonly ws: MemWorkspace) { }

    get name(): string {
        return this.ws.name;
    }

    get mountPath(): string {
        return this.ws.mountPath;
    }

    async readFile(path: string): Promise<Uint8Array | null> {
        const f = this.ws.files.get(path);
        return f ? f.bytes : null;
    }

    async writeFile(
        path: string,
        data: Uint8Array | string,
        meta?: FileMeta,
    ): Promise<void> {
        const existing = this.ws.files.get(path);
        const merged: FileMeta = { ...(existing?.meta ?? {}), ...(meta ?? {}) };
        this.ws.files.set(path, { bytes: toBytes(data), meta: merged });
    }

    async deleteFile(path: string): Promise<void> {
        if (!this.ws.files.delete(path)) {
            throw new BuildConfigError(`File not found: ${path}`);
        }
    }

    async listFiles(opts?: {
        includeHidden?: boolean;
        includeSolution?: boolean;
    }): Promise<Array<{ path: string } & FileMeta & { size: number }>> {
        const includeHidden = opts?.includeHidden ?? true;
        const includeSolution = opts?.includeSolution ?? true;
        const out: Array<{ path: string } & FileMeta & { size: number }> = [];
        for (const [path, f] of this.ws.files) {
            const v = f.meta.visibility;
            if (v === 'hidden' && !includeHidden) continue;
            if (v === 'solution' && !includeSolution) continue;
            out.push({ path, size: f.bytes.length, ...f.meta });
        }
        out.sort((a, b) => (a.path < b.path ? -1 : a.path > b.path ? 1 : 0));
        return out;
    }

    async setVisibility(
        path: string,
        v: FileEntry['visibility'],
    ): Promise<void> {
        const f = this.ws.files.get(path);
        if (!f) throw new BuildConfigError(`File not found: ${path}`);
        f.meta = { ...f.meta, visibility: v };
    }

    async getBuild(): Promise<WorkspaceBuildConfig> {
        // Return a defensive copy so callers can't mutate internal state.
        return JSON.parse(JSON.stringify(this.ws.build)) as WorkspaceBuildConfig;
    }

    async setBuild(build: WorkspaceBuildConfig): Promise<void> {
        this.ws.build = JSON.parse(JSON.stringify(build)) as WorkspaceBuildConfig;
    }

    async reset(): Promise<void> {
        this.ws.files.clear();
        this.ws.build = { toolchain: ToolchainPreset.CPP };
        this.ws.seedMarker = null;
    }

    async dispose(): Promise<void> {
        // No external resources held; nothing to release.
    }
}

/**
 * Apply a seed to a workspace honoring `policy`. Returns the new seed marker,
 * or the existing one if the seed was skipped.
 */
function applySeed(
    ws: MemWorkspace,
    seed: WorkspaceSeed,
    policy: SeedPolicy,
): SeedMarker {
    const hash = hashSeed(seed);
    const existing = ws.seedMarker;

    if (existing && existing.hash === hash && policy === 'once') {
        return existing;
    }

    if (existing && existing.hash !== hash && policy === 'once') {
        throw new WorkspaceConflictError(
            `Workspace '${ws.name}' was seeded with a different content hash; ` +
            `seedPolicy='once' refuses to overwrite (expected ${existing.hash}, got ${hash}).`,
        );
    }

    const isMerge = policy === 'merge';
    if (!isMerge) ws.files.clear();

    for (const [path, raw] of Object.entries(seed)) {
        if (isMerge && ws.files.has(path)) continue;
        const entry = normalizeSeedEntry(raw);
        const meta: FileMeta = {};
        if (entry.visibility !== undefined) meta.visibility = entry.visibility;
        if (entry.readonly !== undefined) meta.readonly = entry.readonly;
        if (entry.executable !== undefined) meta.executable = entry.executable;
        ws.files.set(path, { bytes: toBytes(entry.content), meta });
    }

    return { hash, appliedAt: Date.now(), policy };
}

/**
 * In-memory WorkspaceManager. State lives entirely in the constructed Map; a
 * fresh manager always starts empty.
 */
export class MemoryWorkspaceManager implements WorkspaceManager {
    private readonly workspaces = new Map<string, MemWorkspace>();
    private disposed = false;

    async list(): Promise<string[]> {
        this.assertLive();
        return [...this.workspaces.keys()].sort();
    }

    async open(opts: OpenWorkspaceOptions): Promise<WorkspaceHandle> {
        this.assertLive();
        const policy: SeedPolicy = opts.seedPolicy ?? 'overwrite';
        let ws = this.workspaces.get(opts.name);
        if (!ws) {
            ws = {
                name: opts.name,
                mountPath: opts.mountPath ?? defaultMountPath(opts.name),
                files: new Map(),
                build: opts.build ?? { toolchain: ToolchainPreset.CPP },
                seedMarker: null,
            };
            this.workspaces.set(opts.name, ws);
        } else if (opts.mountPath && opts.mountPath !== ws.mountPath) {
            ws.mountPath = opts.mountPath;
        }

        if (opts.seed) {
            ws!.seedMarker = applySeed(ws!, opts.seed, policy);
        }

        return new MemoryWorkspaceHandle(ws!);
    }

    async remove(name: string): Promise<void> {
        this.assertLive();
        if (!this.workspaces.delete(name)) {
            throw new BuildConfigError(`Workspace not found: ${name}`);
        }
    }

    async dispose(): Promise<void> {
        this.disposed = true;
        this.workspaces.clear();
    }

    private assertLive(): void {
        if (this.disposed) {
            throw new BuildConfigError('WorkspaceManager has been disposed.');
        }
    }
}

/** Convenience factory mirroring the @emception/* `create*` naming pattern. */
export function createMemoryWorkspaceManager(): WorkspaceManager {
    return new MemoryWorkspaceManager();
}
