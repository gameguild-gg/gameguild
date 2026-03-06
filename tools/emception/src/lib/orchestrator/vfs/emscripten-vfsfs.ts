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
 *     file content. This store is populated on-demand by the JSPI hooks in
 *     the patched glue code (patch 6: `onPreOpen`, `onPreStat`, `onPreAccess`).
 *   - Directory structure + stat metadata come from the kernel VFS
 *     (LazyFS manifest + IDBFS write layer).
 *   - Write-through: writes update both the local store and the kernel VFS.
 *
 *  JSPI integration:
 *   The glue code's patched syscalls (___syscall_openat, etc.) call
 *   Module["onPreOpen"](path) / Module["onPreStat"](path) BEFORE the
 *   actual syscall.  These async hooks fetch the file from the kernel VFS
 *   (CDN → IDB cache → memCache) and populate our `fileData` map.  By the
 *   time the real syscall runs, the data is available synchronously.
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
): Record<string, unknown> {
    // Permission modes
    const DIR_MODE = 0o40755;  // S_IFDIR | 0755
    const FILE_MODE = 0o100644; // S_IFREG | 0644

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
         */
        unlink(parent: EmNode, name: string): void {
            const parentPath = getNodePath(parent);
            const childPath = normalizePath(parentPath + '/' + name);
            fileData.delete(childPath);
            // Remove from parent contents if tracked
            if (parent.contents) {
                delete parent.contents[name];
            }
            try { vfs.deleteFileSync(childPath); } catch { /* non-fatal */ }
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
         * Data should already be in `fileData` thanks to the JSPI onPreOpen hook.
         */
        open(stream: EmStream): void {
            const node = stream.node;
            if (FS.isDir(node.mode)) return;

            const nodePath = getNodePath(node);

            // If we don't have the data yet, try sync read from VFS memCache
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

            // Write-through to VFS (fire-and-forget)
            if (!isMemfsPath(nodePath)) {
                try { vfs.writeFileSync(nodePath, existing); } catch { /* non-fatal */ }
            }

            return length;
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
}

/**
 * Mount VFSFS at the given mount points and install JSPI hooks on the Module.
 *
 * Call after WASM instantiation but before callMain().
 *
 * @returns The shared fileData map (useful for injecting synthetic files)
 */
export function mountVFSFS(
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    FS: any,
    moduleConfig: Record<string, unknown>,
    vfs: VFSManager,
    options: MountVFSFSOptions,
): Map<string, Uint8Array> {
    const pathAliases = options.pathAliases ?? new Map<string, string>();
    const fileData = new Map<string, Uint8Array>();

    // Create the FS type
    const vfsfsType = createVFSFS(FS, vfs, pathAliases, fileData);

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

    // ── Install JSPI hooks on the Module ──────────────────────────────
    // These hooks are called by the patched glue code (patch 6) BEFORE each
    // filesystem syscall. They fetch file data from the kernel VFS asynchronously
    // (JSPI suspends the WASM stack while the fetch is in flight).

    /**
     * Ensure a file and its parent directories are populated in fileData.
     */
    async function ensureFile(path: string): Promise<void> {
        const normalized = normalizePath(path);
        if (isMemfsPath(normalized) || normalized === '/') return;

        // Already loaded?
        if (fileData.has(normalized)) return;

        const resolved = resolveAlias(normalized, pathAliases);
        const data = await vfs.fetchFile(resolved);
        if (data) {
            fileData.set(normalized, data);
        }
    }

    /**
     * Ensure a path is stat-able: populate fileData if it's a file,
     * for directories we don't need data — the VFS manifest has metadata.
     */
    async function ensureStat(path: string): Promise<void> {
        const normalized = normalizePath(path);
        if (isMemfsPath(normalized) || normalized === '/') return;

        // Already loaded?
        if (fileData.has(normalized)) return;

        const resolved = resolveAlias(normalized, pathAliases);
        const stat = vfs.statSync(resolved);
        if (stat && stat.type === 'file') {
            const data = await vfs.fetchFile(resolved);
            if (data) {
                fileData.set(normalized, data);
            }
        }
    }

    // Install hooks on Module for the glue code's JSPI wrappers.
    // These are called before each filesystem syscall (openat, stat64, etc.)
    // to lazily fetch files from the kernel VFS / CDN into the local fileData map.
    moduleConfig['onPreOpen'] = ensureFile;
    moduleConfig['onPreStat'] = ensureStat;
    moduleConfig['onPreAccess'] = ensureStat;

    return fileData;
}
