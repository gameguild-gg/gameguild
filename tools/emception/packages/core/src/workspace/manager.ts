// WorkspaceManager interface (runtime-agnostic skeleton).
//
// The interface defined here is what every storage backend
// (`@emception/browser` IDB store, in-memory store
// in core) must implement. Concrete implementations live in those packages;
// keeping the contract here means the core test engine, build resolver, and
// EmceptionAPI surface can talk about workspaces without importing platform
// code and without breaking SSR.
//
// Sidecar layout under each workspace mount root:
//   .emception/meta.json   — file metadata (visibility/readonly/executable)
//   .emception/build.json  — WorkspaceBuildConfig
//   .emception/seed.lock   — seed-hash marker ('once' policy)

import type { FileEntry, WorkspaceBuildConfig, WorkspaceSeed } from '../types.js';

/** How a seed is reconciled against an existing workspace on switch/open. */
export type SeedPolicy = 'once' | 'overwrite' | 'merge';

/**
 * Marker persisted at `.emception/seed.lock` so 'once' policy can detect
 * tampering and 'merge' can avoid clobbering edits.
 */
export interface SeedMarker {
    /** SHA-256 (hex) of the canonical seed encoding. */
    hash: string;
    /** Wall-clock ms when the seed was first applied. */
    appliedAt: number;
    /** Policy used at apply time, for debuggability. */
    policy: SeedPolicy;
}

/** A single file metadata record (mirrors FileEntry minus content). */
export interface FileMeta {
    visibility?: FileEntry['visibility'];
    readonly?: boolean;
    executable?: boolean;
}

/** Per-workspace metadata sidecar (`.emception/meta.json`). */
export interface MetaSidecar {
    /** Path → metadata. Paths are workspace-relative, forward-slash. */
    files: Record<string, FileMeta>;
}

/** Open / create a workspace through a `WorkspaceManager`. */
export interface OpenWorkspaceOptions {
    name: string;
    seed?: WorkspaceSeed;
    seedPolicy?: SeedPolicy;
    /** Where to mount the workspace inside the worker VFS. Default: /workspace/<name>. */
    mountPath?: string;
    /** Initial build config (only honored when the workspace is first created). */
    build?: WorkspaceBuildConfig;
}

/**
 * Handle returned by `WorkspaceManager.open()`. All paths are workspace-relative
 * and use forward slashes regardless of host OS.
 */
export interface WorkspaceHandle {
    readonly name: string;
    readonly mountPath: string;

    /** Read raw bytes; returns null when the file does not exist. */
    readFile(path: string): Promise<Uint8Array | null>;
    /** Atomically write bytes (or a UTF-8 string) and update metadata. */
    writeFile(path: string, data: Uint8Array | string, meta?: FileMeta): Promise<void>;
    /** Delete a file. Throws if the file does not exist. */
    deleteFile(path: string): Promise<void>;

    /** List files; visibility filters apply only when explicitly requested false. */
    listFiles(opts?: {
        includeHidden?: boolean;
        includeSolution?: boolean;
    }): Promise<Array<{ path: string } & FileMeta & { size: number }>>;

    /** Update the visibility tag on an existing file. */
    setVisibility(path: string, v: FileEntry['visibility']): Promise<void>;

    /** Read the persisted build config (`.emception/build.json`). */
    getBuild(): Promise<WorkspaceBuildConfig>;
    /** Replace the persisted build config wholesale. */
    setBuild(build: WorkspaceBuildConfig): Promise<void>;

    /** Drop everything in this workspace (files, sidecars, seed marker). */
    reset(): Promise<void>;

    /** Release any backend resources (close IDB cursors, file handles, etc.). */
    dispose(): Promise<void>;
}

/**
 * Top-level workspace manager — what `RuntimeAdapter.openWorkspaceStore()`
 * eventually returns once platform-specific backends are wired.
 */
export interface WorkspaceManager {
    /** List every known workspace name. */
    list(): Promise<string[]>;
    /** Open (or create) a workspace and return a handle. */
    open(opts: OpenWorkspaceOptions): Promise<WorkspaceHandle>;
    /** Permanently delete a workspace. Throws if it does not exist. */
    remove(name: string): Promise<void>;
    /** Tear down the manager itself; subsequent calls should reject. */
    dispose(): Promise<void>;
}
