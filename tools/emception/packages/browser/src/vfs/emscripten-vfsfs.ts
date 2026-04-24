/**
 * VFSFS — Custom Emscripten filesystem backed by the kernel VFS.
 *
 * Implements Emscripten's low-level FS type interface (`mount`, `createNode`,
 * `node_ops`, `stream_ops`) so it can be registered as a proper FS mount.
 *
 * This replaces the old proxy approach (lookupPath patching) which was broken
 * because Emscripten's internal `lookupNode` (used by `__syscall_getdents64`)
 * bypassed lookupPath.
 *
 *  Architecture:
 *   - Backed by a JS object store (`fileData: Map<string, Uint8Array>`) for
 *     file content. This store is populated on-demand by the Asyncify hooks
 *     (onPreOpen, onPreStat, onPreAccess) installed on the Module. File data
 *     is fetched from IDB/CDN — no persistent in-memory cache is used.
 *   - Directory structure + stat metadata come from the kernel VFS
 *     (LazyFS manifest + IDBFS write layer).
 *   - Write-through: writes update both the local store and the kernel VFS.
 *
 *  Asyncify integration:
 *   The Emscripten glue code's ASYNCIFY_IMPORTS list includes the FS
 *   syscalls (___syscall_openat, etc.) which call Module["onPreOpen"](path) /
 *   Module["onPreStat"](path) BEFORE the actual syscall. These async hooks
 *   fetch the file from the kernel VFS (IDB → CDN fallback) and populate
 *   our `fileData` map. Asyncify suspends/resumes the WASM stack while the
 *   fetch is in flight. By the time the real syscall runs, the data is
 *   available synchronously.
 *
 *  Usage: call `mountVFSFS(FS, vfs, mountPoints)` after WASM instantiation
 *  but before callMain().
 */

import type { VFSManager } from './index';

const LOG_PREFIX = '[Emception:VFSFS]';

/**
 * Normalize a VFS path: resolve . and .., ensure leading /.
 */
function normalizePath(path: string): string {
    const parts = path.split('/').filter((p) => p && p !== '.');
    const result: string[] = [];
    for (const part of parts) {
        if (part === '..') result.pop();
        else result.push(part);
    }
    return '/' + result.join('/');
}

/**
 * Paths managed by Emscripten's MEMFS (device nodes, proc, etc.)
 */
function isMemfsPath(path: string): boolean {
    return (
        path === '/' ||
        path.startsWith('/dev') ||
        path.startsWith('/proc')
    );
}

/**
 * Dynamic subprocess IPC files used by the Python subprocess shim.
 * These must always go through async syscall hooks (no sync cache bypass),
 * otherwise follow-up subprocess calls can be skipped.
 */
function isDynamicIpcPath(path: string): boolean {
    return path === '/tmp/__dispatch_subprocess__' || path.startsWith('/tmp/.subprocess_');
}

/**
 * Writable per-process work paths.
 *
 * These paths are expected to be created or updated by the running tool
 * itself (temporary objects, build outputs, user workspace files). They
 * should never trigger async CDN/IDB fetches before open/stat/access — doing
 * so can suspend open(O_CREAT) on files that do not exist yet and wedge the
 * Asyncify state machine while the tool is trying to create a temp output.
 */
function isWritableWorkPath(path: string): boolean {
    const normalized = normalizePath(path);

    if (normalized === '/tmp' || normalized.startsWith('/tmp/')) {
        return !isDynamicIpcPath(normalized);
    }

    if (normalized === '/home/user' || normalized.startsWith('/home/user/')) {
        return !normalized.startsWith('/home/user/.emscripten_cache');
    }

    return false;
}

/**
 * Resolve a path through registered aliases.
 */
function resolveAlias(path: string, aliases: Map<string, string>): string {
    for (const [prefix, target] of aliases) {
        if (path === prefix) return target;
        if (path.startsWith(prefix + '/')) {
            return target + path.slice(prefix.length);
        }
    }
    return path;
}

/* ------------------------------------------------------------------ */
/*  Types for the Emscripten FS internals                              */
/* ------------------------------------------------------------------ */

/** Minimal shape of an Emscripten FS node */
interface EmNode {
    id: number;
    name: string;
    parent: EmNode;
    mode: number;
    mount: EmMount;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    node_ops: Record<string, (...args: any[]) => any>;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    stream_ops: Record<string, (...args: any[]) => any>;
    contents?: Record<string, EmNode>;
    usedBytes?: number;
    // Custom extension: path for our VFS-backed nodes
    __vfsPath?: string;
}

/** Minimal shape of an Emscripten FS mount */
interface EmMount {
    mountpoint: string;
    root: EmNode;
    opts: Record<string, unknown>;
}

/** Minimal shape of an Emscripten FS stream */
interface EmStream {
    node: EmNode;
    position: number;
    flags: number;
}

export interface VFSFSRuntime {
    HEAP8?: Int8Array;
    HEAPU8?: Uint8Array;
    _malloc?: (size: number) => number;
}

/* eslint-disable @typescript-eslint/no-explicit-any */
/** Emscripten FS object — only methods we use */
interface EmscriptenFS {
    createNode(parent: EmNode, name: string, mode: number, dev?: number): EmNode;
    registerDevice(dev: number, ops: Record<string, (...args: unknown[]) => unknown>): void;
    isDir(mode: number): boolean;
    isFile(mode: number): boolean;
    // Internal constants
    ErrnoError: new (errno: number) => Error;
    // FS.lookupPath, FS.analyzePath, etc.
    lookupPath(path: string, opts?: Record<string, unknown>): { path: string; node: EmNode };
    analyzePath(path: string, dontResolveLastLink?: boolean): { exists: boolean; path: string; object: EmNode | null };
    mkdirTree(path: string): void;
    mkdir(path: string): void;
    writeFile(path: string, data: string | Uint8Array): void;
    readFile(path: string, opts?: { encoding?: string }): Uint8Array;
    stat(path: string): { size: number; mode: number };
    chdir(path: string): void;
    unlink(path: string): void;
    // Used to register custom FS types
    filesystems: Record<string, unknown>;
    // Internal mount tracking
    mount(type: unknown, opts: Record<string, unknown>, mountpoint: string): EmMount;
    // Internal next inode counter
    nextInode?: number;
    [key: string]: any;
}
/* eslint-enable @typescript-eslint/no-explicit-any */

/* ------------------------------------------------------------------ */
/*  VFSFS implementation                                               */
/* ------------------------------------------------------------------ */

/**
 * Build the VFSFS type object compatible with Emscripten's FS.mount().
 */
function createVFSFS(
    FS: EmscriptenFS,
    vfs: VFSManager,
    pathAliases: Map<string, string>,
    fileData: Map<string, Uint8Array>,
    protectedPaths: Set<string>,
    stats: { opens: number; writes: number; bytes: number; mmaps: number },
    runtime: VFSFSRuntime | undefined,
): Record<string, unknown> {
    // Permission modes
    const DIR_MODE = 0o40755;  // S_IFDIR | 0755
    const FILE_MODE = 0o100755; // S_IFREG | 0755 — needs execute for access(X_OK)

    /**
     * Get the full VFS path for a node by walking up to the mount root.
     */
    function getNodePath(node: EmNode): string {
        if (node.__vfsPath) return node.__vfsPath;
        const parts: string[] = [];
        let cur: EmNode = node;
        while (cur && cur !== cur.parent) {
            parts.unshift(cur.name);
            cur = cur.parent;
        }
        // The mount root's name is the mountpoint path
        const mountpoint = node.mount?.mountpoint || '';
        const subpath = parts.join('/');
        return normalizePath(mountpoint + (subpath ? '/' + subpath : ''));
    }

    function getHeapU8(): Uint8Array | null {
        if (runtime?.HEAPU8 instanceof Uint8Array) {
            return runtime.HEAPU8;
        }
        if (runtime?.HEAP8 instanceof Int8Array) {
            return new Uint8Array(runtime.HEAP8.buffer);
        }
        return null;
    }

    // ── node_ops ──────────────────────────────────────────────────────

    const node_ops = {
        /**
         * lookup: called when Emscripten resolves a path component.
         * Creates child nodes on-demand from the VFS manifest.
         */
        lookup(parent: EmNode, name: string): EmNode {
            const parentPath = getNodePath(parent);
            const childPath = normalizePath(parentPath + '/' + name);
            const resolved = resolveAlias(childPath, pathAliases);

            // Check existing children first
            if (parent.contents && parent.contents[name]) {
                return parent.contents[name];
            }

            // Check VFS for existence
            const stat = vfs.statSync(resolved);
            if (!stat) {
                // Also check local fileData (written files not yet in VFS)
                if (fileData.has(childPath)) {
                    const child = FS.createNode(parent, name, FILE_MODE, 0);
                    child.__vfsPath = childPath;
                    child.node_ops = node_ops;
                    child.stream_ops = stream_ops;
                    child.usedBytes = fileData.get(childPath)!.length;
                    return child;
                }

                // Check for implicit directories: if any fileData key starts
                // with childPath + '/', then childPath is a directory that
                // contains pre-seeded files (e.g. cmake build dir files).
                // Without this, mkdirTree failures on VFSFS leave intermediate
                // directories unresolvable, causing cmake to fail with
                // "Could not find cmake module file: .../CMakeCXXCompiler.cmake"
                const childPrefix = childPath + '/';
                for (const key of fileData.keys()) {
                    if (key.startsWith(childPrefix)) {
                        const child = FS.createNode(parent, name, DIR_MODE, 0);
                        child.__vfsPath = childPath;
                        child.node_ops = node_ops;
                        child.stream_ops = stream_ops;
                        child.contents = {};
                        return child;
                    }
                }

                throw new (FS.ErrnoError)(44); // ENOENT
            }

            const mode = stat.type === 'dir' ? DIR_MODE : FILE_MODE;
            const child = FS.createNode(parent, name, mode, 0);
            child.__vfsPath = childPath;
            child.node_ops = node_ops;
            child.stream_ops = stream_ops;

            if (stat.type === 'dir') {
                child.contents = {};
            } else {
                child.usedBytes = stat.size;
            }

            return child;
        },

        /**
         * getattr: return stat metadata for a node.
         */
        getattr(node: EmNode): Record<string, unknown> {
            const nodePath = getNodePath(node);
            const resolved = resolveAlias(nodePath, pathAliases);
            const stat = vfs.statSync(resolved);
            const isDir = FS.isDir(node.mode);
            const size = isDir ? 4096 : (fileData.get(nodePath)?.length ?? stat?.size ?? 0);
            const mtime = stat?.mtimeNs ? Number(stat.mtimeNs / 1_000_000n) : Date.now();
            return {
                dev: 1,
                ino: node.id,
                mode: node.mode,
                nlink: isDir ? 2 : 1,
                uid: 0,
                gid: 0,
                rdev: 0,
                size,
                atime: new Date(mtime),
                mtime: new Date(mtime),
                ctime: new Date(mtime),
                blksize: 4096,
                blocks: Math.ceil(size / 512),
            };
        },

        /**
         * readdir: list directory contents by merging VFS + local writes.
         */
        readdir(node: EmNode): string[] {
            const nodePath = getNodePath(node);
            const resolved = resolveAlias(nodePath, pathAliases);
            const entries = new Set<string>(['.', '..']);

            // VFS entries
            const vfsEntries = vfs.readdirSync(resolved);
            for (const e of vfsEntries) entries.add(e);

            // Locally-held children (from contents or from writes to fileData)
            if (node.contents) {
                for (const name of Object.keys(node.contents)) {
                    entries.add(name);
                }
            }

            // Also check fileData for files written under this directory
            const prefix = nodePath === '/' ? '/' : nodePath + '/';
            for (const key of fileData.keys()) {
                if (key.startsWith(prefix)) {
                    const rel = key.slice(prefix.length);
                    const first = rel.split('/')[0];
                    if (first) entries.add(first);
                }
            }

            return [...entries];
        },

        /**
         * mknod: create a new node (file or directory).
         */
        mknod(parent: EmNode, name: string, mode: number): EmNode {
            const parentPath = getNodePath(parent);
            const childPath = normalizePath(parentPath + '/' + name);
            const child = FS.createNode(parent, name, mode, 0);
            child.__vfsPath = childPath;
            child.node_ops = node_ops;
            child.stream_ops = stream_ops;

            if (FS.isDir(mode)) {
                child.contents = {};
                // Write-through to VFS
                try { vfs.mkdirSync(childPath); } catch { /* non-fatal */ }
            } else {
                child.usedBytes = 0;
                fileData.set(childPath, new Uint8Array(0));
            }

            return child;
        },

        /**
         * unlink: remove a file.
         * Protected paths (e.g. pre-seeded cmake compiler info files) are
         * kept in fileData so that a subsequent lookup() can recover them
         * even after cmake's C++ code removes the file during EnableLanguage.
         */
        unlink(parent: EmNode, name: string): void {
            const parentPath = getNodePath(parent);
            const childPath = normalizePath(parentPath + '/' + name);
            if (!protectedPaths.has(childPath)) {
                fileData.delete(childPath);
            }
            // Remove from parent contents if tracked
            if (parent.contents) {
                delete parent.contents[name];
            }
            if (!protectedPaths.has(childPath)) {
                try { vfs.deleteFileSync(childPath); } catch { /* non-fatal */ }
            }
        },

        /**
         * rmdir: remove an empty directory.
         */
        rmdir(parent: EmNode, name: string): void {
            const parentPath = getNodePath(parent);
            const childPath = normalizePath(parentPath + '/' + name);
            if (parent.contents) {
                delete parent.contents[name];
            }
            // VFS doesn't have rmdir, try deleteFile
            try { vfs.deleteFileSync(childPath); } catch { /* non-fatal */ }
        },

        /**
         * rename: move a node.
         */
        rename(oldNode: EmNode, newDir: EmNode, newName: string): void {
            const oldPath = getNodePath(oldNode);
            const newDirPath = getNodePath(newDir);
            const newPath = normalizePath(newDirPath + '/' + newName);

            // Move file data
            const data = fileData.get(oldPath);
            if (data) {
                fileData.set(newPath, data);
                fileData.delete(oldPath);
                // Write-through
                vfs.writeFileSync(newPath, data);
                try { vfs.deleteFileSync(oldPath); } catch { /* non-fatal */ }
            }

            oldNode.__vfsPath = newPath;
            oldNode.name = newName;
        },

        /**
         * setattr: update node attributes (mode, size, timestamps).
         */
        setattr(node: EmNode, attr: Record<string, unknown>): void {
            if (attr.mode !== undefined) {
                node.mode = attr.mode as number;
            }
            if (attr.size !== undefined) {
                const size = attr.size as number;
                const nodePath = getNodePath(node);
                const existing = fileData.get(nodePath);
                if (existing) {
                    if (size === 0) {
                        fileData.set(nodePath, new Uint8Array(0));
                    } else if (size < existing.length) {
                        fileData.set(nodePath, existing.slice(0, size));
                    } else if (size > existing.length) {
                        const newData = new Uint8Array(size);
                        newData.set(existing);
                        fileData.set(nodePath, newData);
                    }
                }
                node.usedBytes = size;
            }
        },
    };

    // ── stream_ops ────────────────────────────────────────────────────

    const stream_ops = {
        /**
         * open: called when a file is opened.
         * Data should already be in `fileData` thanks to the Asyncify onPreOpen hook.
         * The sync fallback below handles edge cases (e.g. files written by the process).
         */
        open(stream: EmStream): void {
            const node = stream.node;
            if (FS.isDir(node.mode)) return;

            const nodePath = getNodePath(node);
            stats.opens++;
            if (stats.opens <= 20) {
                console.log(`${LOG_PREFIX} [stream.open #${stats.opens}] ${nodePath} hasData=${fileData.has(nodePath)}`);
            }

            // If we don't have the data yet, try sync read from IDBFS write layer
            if (!fileData.has(nodePath)) {
                const resolved = resolveAlias(nodePath, pathAliases);
                const data = vfs.readFileSync(resolved);
                if (data) {
                    fileData.set(nodePath, data);
                }
                // If still missing, the file might be genuinely empty or a new file
            }
        },

        /**
         * read: copy bytes from fileData into the buffer.
         */
        read(
            stream: EmStream,
            buffer: Uint8Array,
            offset: number,
            length: number,
            position: number,
        ): number {
            const node = stream.node;
            const nodePath = getNodePath(node);
            const data = fileData.get(nodePath);

            if (!data || position >= data.length) return 0;

            const available = Math.min(length, data.length - position);
            buffer.set(data.subarray(position, position + available), offset);
            return available;
        },

        /**
         * write: write bytes from the buffer into fileData.
         */
        write(
            stream: EmStream,
            buffer: Uint8Array,
            offset: number,
            length: number,
            position: number,
        ): number {
            const node = stream.node;
            const nodePath = getNodePath(node);

            let existing = fileData.get(nodePath) ?? new Uint8Array(0);
            const endPos = position + length;

            if (endPos > existing.length) {
                const newData = new Uint8Array(endPos);
                newData.set(existing);
                existing = newData;
            }

            existing.set(buffer.subarray(offset, offset + length), position);
            fileData.set(nodePath, existing);
            node.usedBytes = existing.length;

            stats.writes++;
            stats.bytes += length;
            if (stats.writes <= 20 || stats.writes % 100 === 0) {
                console.log(`${LOG_PREFIX} [stream.write #${stats.writes}] ${nodePath} +${length}B @${position} (total=${existing.length}B, accBytes=${stats.bytes})`);
            }

            // Write-through to VFS (fire-and-forget)
            if (!isMemfsPath(nodePath)) {
                try { vfs.writeFileSync(nodePath, existing); } catch { /* non-fatal */ }
            }

            return length;
        },

        /**
         * close: flush the latest file contents back to the shared VFS.
         * This is mostly redundant with write-through, but it helps tools that
         * finish writes at close time and mirrors MEMFS/IDBFS behavior.
         */
        close(stream: EmStream): void {
            const node = stream.node;
            if (FS.isDir(node.mode)) return;

            const nodePath = getNodePath(node);
            const data = fileData.get(nodePath);
            if (!data || isMemfsPath(nodePath)) return;

            try {
                vfs.writeFileSync(nodePath, data);
            } catch {
                // Non-fatal: process-local fileData still holds the latest bytes.
            }
        },

        /**
         * mmap: back file mappings with WASM linear memory so tools like clang
         * and lld can write output files through FileOutputBuffer.
         */
        mmap(
            stream: EmStream,
            length: number,
            position: number,
            _prot: number,
            _flags: number,
        ): { ptr: number; allocated: boolean } {
            const node = stream.node;
            if (!FS.isFile(node.mode)) {
                throw new (FS.ErrnoError)(43); // ENODEV
            }

            const malloc = runtime?._malloc;
            const heap = getHeapU8();
            if (typeof malloc !== 'function' || !heap) {
                throw new (FS.ErrnoError)(43); // ENODEV
            }

            const ptr = malloc(length);
            if (!ptr) {
                throw new (FS.ErrnoError)(48); // ENOMEM
            }

            const nodePath = getNodePath(node);
            const existing = fileData.get(nodePath);
            if (existing && position < existing.length) {
                const end = Math.min(existing.length, position + length);
                heap.set(existing.subarray(position, end), ptr);
            }

            stats.mmaps++;
            if (stats.mmaps <= 20 || stats.mmaps % 100 === 0) {
                console.log(`${LOG_PREFIX} [stream.mmap #${stats.mmaps}] ${nodePath} len=${length} pos=${position} ptr=${ptr}`);
            }

            return { ptr, allocated: true };
        },

        /**
         * msync: copy the mapped bytes back into fileData and the shared VFS.
         */
        msync(
            stream: EmStream,
            buffer: Uint8Array,
            offset: number,
            length: number,
            _mmapFlags: number,
        ): number {
            stream_ops.write(stream, buffer, 0, length, offset);
            return 0;
        },

        /**
         * llseek: seek to a position in the file.
         */
        llseek(stream: EmStream, offset: number, whence: number): number {
            const node = stream.node;
            const nodePath = getNodePath(node);
            const size = fileData.get(nodePath)?.length ?? 0;

            let pos = stream.position;
            if (whence === 0) {
                pos = offset; // SEEK_SET
            } else if (whence === 1) {
                pos += offset; // SEEK_CUR
            } else if (whence === 2) {
                pos = size + offset; // SEEK_END
            }

            if (pos < 0) throw new (FS.ErrnoError)(28); // EINVAL
            return pos;
        },
    };

    // ── VFSFS type object ─────────────────────────────────────────────

    return {
        mount(mount: EmMount): EmNode {
            const root = FS.createNode(null as unknown as EmNode, '/', DIR_MODE, 0);
            root.__vfsPath = mount.mountpoint;
            root.node_ops = node_ops;
            root.stream_ops = stream_ops;
            root.contents = {};
            return root;
        },
        createNode: FS.createNode,
        node_ops,
        stream_ops,
    };
}

/* ------------------------------------------------------------------ */
/*  Public API                                                         */
/* ------------------------------------------------------------------ */

export interface MountVFSFSOptions {
    /** Mount points to create and mount VFSFS at (e.g. ['/usr', '/etc', '/home', '/tmp']) */
    mountPoints: string[];
    /** Path aliases for sysroot cache mapping */
    pathAliases?: Map<string, string>;
    /** Access to the module's linear memory for mmap/msync support. */
    runtime?: VFSFSRuntime;
}

/**
 * Mount VFSFS at the given mount points and install Asyncify hooks on the Module.
 *
 * Call after WASM instantiation but before callMain().
 *
 * @returns Object with fileData map and protectedPaths set
 */
export function mountVFSFS(
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    FS: any,
    moduleConfig: Record<string, unknown>,
    vfs: VFSManager,
    options: MountVFSFSOptions,
): { fileData: Map<string, Uint8Array>; protectedPaths: Set<string> } {
    const pathAliases = options.pathAliases ?? new Map<string, string>();
    const fileData = new Map<string, Uint8Array>();
    const protectedPaths = new Set<string>();

    // Negative cache: tracks normalized paths confirmed absent from all backends.
    // Also used for known directories (which don't need fileData).
    // Avoids repeated async IDB lookups for paths the compiler probes.
    const negativeStatCache = new Set<string>();

    // Per-process stream stats
    const stats = { opens: 0, writes: 0, bytes: 0, mmaps: 0 };

    // Create the FS type
    const vfsfsType = createVFSFS(FS, vfs, pathAliases, fileData, protectedPaths, stats, options.runtime);

    // Register the type
    FS.filesystems = FS.filesystems || {};
    FS.filesystems['VFSFS'] = vfsfsType;

    // Mount at each mount point
    for (const mp of options.mountPoints) {
        // Ensure mount point directory exists in MEMFS first
        try { FS.mkdirTree(mp); } catch { /* exists */ }

        try {
            FS.mount(vfsfsType, {}, mp);
            console.log(`${LOG_PREFIX} Mounted VFSFS at ${mp}`);
        } catch (e) {
            console.warn(`${LOG_PREFIX} Failed to mount at ${mp}:`, e);
        }
    }

    // ── Install Asyncify hooks on the Module ─────────────────────────
    // These hooks are called by the Emscripten glue code BEFORE each
    // filesystem syscall (via ASYNCIFY_IMPORTS). They fetch file data from
    // the kernel VFS asynchronously (Asyncify suspends/resumes the WASM stack).

    /**
     * Resolve a path that may be relative by prepending the Emscripten CWD.
     */
    function resolveWithCwd(path: string): string {
        if (path.startsWith('/')) return path;
        try {
            const cwd: string = FS.cwd?.() ?? '/';
            return cwd === '/' ? '/' + path : cwd + '/' + path;
        } catch {
            return '/' + path;
        }
    }

    /**
     * Ensure a file and its parent directories are populated in fileData.
     */
    async function ensureFile(path: string): Promise<void> {
        const normalized = normalizePath(resolveWithCwd(path));
        if (isMemfsPath(normalized) || normalized === '/') return;
        if (isDynamicIpcPath(normalized)) return;
        if (isWritableWorkPath(normalized)) return;
        if (fileData.has(normalized)) return;
        if (negativeStatCache.has(normalized)) return;

        const resolved = resolveAlias(normalized, pathAliases);

        // Fast path: check IDBFS write layer for recently-written files.
        // LazyFS no longer caches in RAM — files are in IDB only.
        const syncData = vfs.readFileSync(resolved);
        if (syncData) {
            fileData.set(normalized, syncData);
            return;
        }
        const syncStat = vfs.statSync(resolved);
        if (!syncStat) {
            negativeStatCache.add(normalized);
            return;
        }
        if (syncStat.type !== 'file') return;

        // Slow path: async fetch from IDB/CDN
        const data = await vfs.fetchFile(resolved);
        if (data) {
            fileData.set(normalized, data);
        } else {
            // Don't cache as negative if it's an implicit directory
            const prefix = normalized + '/';
            let isImplicitDir = false;
            for (const key of fileData.keys()) {
                if (key.startsWith(prefix)) { isImplicitDir = true; break; }
            }
            if (!isImplicitDir) {
                negativeStatCache.add(normalized);
            }
        }
    }

    /**
     * Ensure a path is stat-able: populate fileData if it's a file,
     * for directories we don't need data — the VFS manifest has metadata.
     */
    async function ensureStat(path: string): Promise<void> {
        const normalized = normalizePath(resolveWithCwd(path));
        if (isMemfsPath(normalized) || normalized === '/') return;
        if (isDynamicIpcPath(normalized)) return;
        if (isWritableWorkPath(normalized)) return;
        if (fileData.has(normalized)) return;
        if (negativeStatCache.has(normalized)) return;

        const resolved = resolveAlias(normalized, pathAliases);

        // Fast path: check IDBFS write layer for recently-written files.
        // Files in the usr-share bundle are in IDB, not RAM — the async
        // path below handles loading via Asyncify suspension.
        const syncStat = vfs.statSync(resolved);
        if (syncStat !== null) {
            if (syncStat.type === 'file') {
                const syncData = vfs.readFileSync(resolved);
                if (syncData) fileData.set(normalized, syncData);
            }
            return;
        }

        // Missing from both the overlay write layer and LazyFS manifest:
        // treat as a synchronous miss so the real syscall can report ENOENT
        // without taking the Asyncify path.
        const prefix = normalized + '/';
        let isImplicitDir = false;
        for (const key of fileData.keys()) {
            if (key.startsWith(prefix)) { isImplicitDir = true; break; }
        }
        if (!isImplicitDir) {
            negativeStatCache.add(normalized);
        }
    }

    // Install hooks on Module for the Asyncify-wrapped syscalls.
    // These are called before each filesystem syscall (openat, stat64, etc.)
    // to lazily fetch files from the kernel VFS / CDN into the local fileData map.

    let _hookCallCount = 0;
    let _lastHookDesc = '';
    let _asyncHits = 0;

    // Activity monitor: logs every 5s so we can see if hooks are still firing
    const _activityTimer = setInterval(() => {
        if (_hookCallCount > 0 || _syncBypass > 0) {
            console.log(`${LOG_PREFIX} [activity] bypass=${_syncBypass} asyncHooks=${_asyncHits} last="${_lastHookDesc}"`);
        }
    }, 5000);

    // Stop the activity timer when this WASM process finishes
    // (the timer reference is captured in the closure; GC will handle it,
    // but being explicit avoids log noise from completed processes)
    const origOnExit = moduleConfig['onExit'] as (() => void) | undefined;
    moduleConfig['onExit'] = () => {
        clearInterval(_activityTimer);
        // Final per-process diagnostics
        const writtenPaths: string[] = [];
        for (const [k, v] of fileData) {
            if (v.length > 0 && !k.startsWith('/usr/') && !k.startsWith('/etc/')) {
                writtenPaths.push(`${k}(${v.length}B)`);
            }
        }
        console.log(`${LOG_PREFIX} [onExit] streams: opens=${stats.opens} writes=${stats.writes} bytes=${stats.bytes}; mmaps=${stats.mmaps}`);
        console.log(`${LOG_PREFIX} [onExit] non-/usr fileData entries (${writtenPaths.length}): ${writtenPaths.slice(0, 30).join(', ')}${writtenPaths.length > 30 ? ', ...' : ''}`);
        origOnExit?.();
    };

    const _hookLog = (op: string, path: string) => {
        _hookCallCount++;
        _lastHookDesc = `${op}: ${path}`;
        if (_hookCallCount <= 100 || _hookCallCount % 100 === 0) {
            console.log(`${LOG_PREFIX} [hook #${_hookCallCount}] ${op}: ${path} (bypass=${_syncBypass} async=${_asyncHits})`);
        }
    };

    // ── isCachedSync: called before any async hook. Returns true →    ──
    // ── the original syscall runs directly (no Asyncify suspend).       ──
    // ── Returns false → the async hook runs (Asyncify suspends). This  ──
    // ── eliminates Asyncify suspend/resume overhead for files already   ──
    // ── in the per-process fileData or negativeStatCache.               ──
    let _syncBypass = 0;
    moduleConfig['isCachedSync'] = (path: string): boolean => {
        const n = normalizePath(resolveWithCwd(path));
        if (isDynamicIpcPath(n)) return false;
        if (isWritableWorkPath(n)) { _syncBypass++; return true; }
        if (isMemfsPath(n) || n === '/') { _syncBypass++; return true; }
        if (fileData.has(n) || negativeStatCache.has(n)) { _syncBypass++; return true; }
        const r = resolveAlias(n, pathAliases);
        const ss = vfs.statSync(r);
        if (ss !== null) {
            if (ss.type === 'file') {
                // Try sync read — returns null if data is only in IDB/CDN
                const d = vfs.readFileSync(r);
                if (d) { fileData.set(n, d); _syncBypass++; return true; }
                // File exists but data not available synchronously — need async fetch
                return false;
            }
            // Directory — no file data needed
            _syncBypass++;
            return true;
        }
        // Check for implicit directories: fileData entries with this path as prefix.
        // Without this, cmake stat() on runtime-created dirs (e.g. build/CMakeFiles/)
        // falls through to async hooks which add the path to negativeStatCache,
        // making the dir permanently invisible.
        const prefix = n + '/';
        for (const key of fileData.keys()) {
            if (key.startsWith(prefix)) { _syncBypass++; return true; }
        }
        negativeStatCache.add(n);
        _syncBypass++;
        return true;
    };

    moduleConfig['onPreOpen'] = async (path: string) => {
        _hookLog('open', path);
        _asyncHits++;
        await ensureFile(path);
    };
    moduleConfig['onPreStat'] = async (path: string) => {
        _hookLog('stat', path);
        _asyncHits++;
        await ensureStat(path);
    };
    moduleConfig['onPreAccess'] = async (path: string) => {
        _hookLog('access', path);
        _asyncHits++;
        await ensureStat(path);
    };

    return { fileData, protectedPaths };
}
