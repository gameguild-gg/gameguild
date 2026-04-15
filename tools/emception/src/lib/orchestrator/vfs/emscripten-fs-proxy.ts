/**
 * Emscripten FS Proxy — lazy-loading + write-through patches for process FS.
 *
 * Instead of copying thousands of files into each WASM process's MEMFS before
 * callMain(), this module patches the Emscripten FS object to:
 *
 *   1. **Lazy-load** files on first access from the kernel VFS (IDB/CDN).
 *      When Emscripten's lookupPath encounters ENOENT, the proxy checks the
 *      VFS and lazily creates the file/dir in MEMFS with data from IDB.
 *
 *   2. **Write-through** file mutations to the kernel VFS overlay.
 *      When the process writes/creates/deletes files, the proxy mirrors the
 *      change to the VFS so other processes (subprocesses) see it immediately
 *      without a separate harvest step.
 *
 *   3. **Merge readdir** results with VFS entries, so directory listings
 *      include files that haven't been lazily loaded into MEMFS yet.
 *
 * Pre-warming: Callers should preload bundles to IDB (via preloadBundle) before
 * callMain so that sync reads succeed.  Files NOT in IDB/write cache will be
 * treated as non-existent (lazy read returns null → ENOENT).
 *
 * Lifecycle: call patchEmscriptenFS once per process, after instantiation
 * but before callMain.
 */

import type { VFSManager } from './index';

const LOG_PREFIX = '[Emception:FSProxy]';

/**
 * Minimal Emscripten FS interface used by the proxy patches.
 * The actual FS object has many more properties; we only type what we touch.
 */
interface EmscriptenFS {
    writeFile(path: string, data: string | Uint8Array): void;
    readFile(path: string, opts?: { encoding?: string }): Uint8Array;
    readdir(path: string): string[];
    stat(path: string): { size: number; mode: number };
    mkdirTree(path: string): void;
    mkdir(path: string): void;
    chdir(path: string): void;
    unlink(path: string): void;
    isDir(mode: number): boolean;
    symlink(target: string, path: string): void;
    // Internal methods used by the proxy
    lookupPath?(path: string, opts?: Record<string, unknown>): { path: string; node: unknown };
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    [key: string]: any;
}

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
 * Paths that should NOT be proxied to VFS. These are managed by
 * Emscripten's own MEMFS setup (device nodes, proc info, etc.).
 */
function isSystemPath(path: string): boolean {
    return (
        path.startsWith('/dev') ||
        path.startsWith('/proc') ||
        path === '/'
    );
}

/**
 * Resolve a path through registered aliases.
 *
 * When Emscripten follows a symlink and a later path component fails,
 * the error still carries the ORIGINAL (pre-symlink) path.  Aliases let
 * us map those original paths to the correct VFS paths without relying
 * on MEMFS symlinks.
 *
 * Example:
 *   alias: '/home/user/.emscripten_cache/sysroot/lib' → '/usr/lib/emscripten/cache-lib'
 *   input: '/home/user/.emscripten_cache/sysroot/lib/wasm32-emscripten/libfoo.a'
 *   output: '/usr/lib/emscripten/cache-lib/wasm32-emscripten/libfoo.a'
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

/**
 * Patch an Emscripten FS object so that file access goes through
 * the kernel VFS instead of requiring upfront copy.
 *
 * @param FS - The Emscripten module's FS object
 * @param vfs - The kernel VFS manager (with sync methods)
 * @param pathAliases - Optional map of path prefix aliases.  When the proxy
 *   catches an ENOENT, the path is translated through these aliases before
 *   querying the VFS.  This replaces symlinks for sysroot cache mapping.
 */
export function patchEmscriptenFS(
    FS: EmscriptenFS,
    vfs: VFSManager,
    pathAliases?: Map<string, string>,
): void {
    // Guard against patching twice
    if ((FS as Record<string, unknown>).__vfsProxyPatched) return;
    (FS as Record<string, unknown>).__vfsProxyPatched = true;

    const aliases = pathAliases ?? new Map<string, string>();

    // Capture all original FS methods upfront (before any patches).
    // Needed because the readdir patch creates stub MEMFS nodes using
    // the unpatch origWriteFile (to avoid triggering write-through to VFS).
    const origWriteFile = FS.writeFile.bind(FS);
    const origMkdir = FS.mkdir.bind(FS);
    const origUnlink = FS.unlink.bind(FS);

    // ── 1. Patch lookupPath: lazy-load from VFS on ENOENT ──────────

    if (typeof FS.lookupPath === 'function') {
        const origLookupPath = FS.lookupPath.bind(FS);
        let inRecovery = false;

        FS.lookupPath = (path: string, opts?: Record<string, unknown>) => {
            try {
                return origLookupPath(path, opts);
            } catch (e: unknown) {
                // Only attempt VFS recovery for ENOENT (errno 44 in Emscripten)
                const errno = (e as { errno?: number })?.errno;
                if (inRecovery || errno !== 44) throw e;

                const normalized = normalizePath(path);
                if (isSystemPath(normalized)) throw e;

                // Resolve path aliases (e.g. sysroot/lib → cache-lib)
                const resolved = resolveAlias(normalized, aliases);

                // DEBUG: trace proxy recovery attempts
                console.log(`${LOG_PREFIX} lookupPath recovery: "${path}" → normalized="${normalized}" resolved="${resolved}"`);

                inRecovery = true;
                try {
                    const stat = vfs.statSync(resolved);
                    if (!stat) {
                        console.log(`${LOG_PREFIX}   → NOT IN VFS (statSync=null)`);
                        throw e;
                    }

                    console.log(`${LOG_PREFIX}   → VFS stat: type=${stat.type}, size=${stat.size}`);

                    if (stat.type === 'dir') {
                        // Create the directory at the ORIGINAL path in MEMFS
                        // (not the alias target) so Emscripten's path walk succeeds.
                        try { FS.mkdirTree(normalized); } catch { /* exists */ }
                    } else if (stat.type === 'symlink' && stat.symlinkTarget) {
                        const dir = normalized.substring(0, normalized.lastIndexOf('/'));
                        if (dir) { try { FS.mkdirTree(dir); } catch { /* exists */ } }
                        try { FS.symlink(stat.symlinkTarget, normalized); } catch { /* exists */ }
                    } else {
                        // Regular file — load from VFS (IDB/CDN)
                        const data = vfs.readFileSync(resolved);
                        if (!data) {
                            console.log(`${LOG_PREFIX}   → FILE IN MANIFEST BUT NOT IN VFS: ${resolved}`);
                            throw e;
                        }

                        // Write at the ORIGINAL path so Emscripten's retry succeeds
                        const dir = normalized.substring(0, normalized.lastIndexOf('/'));
                        if (dir) { try { FS.mkdirTree(dir); } catch { /* exists */ } }
                        FS.writeFile(normalized, data);
                        console.log(`${LOG_PREFIX}   → LOADED file (${data.length}B) to MEMFS`);
                    }

                    // Re-attempt lookup now that the entry exists in MEMFS
                    const result = origLookupPath(path, opts);
                    console.log(`${LOG_PREFIX}   → recovery SUCCESS`);
                    return result;
                } catch (recoveryError) {
                    // If recovery itself fails, throw the original ENOENT
                    const rErrno = (recoveryError as { errno?: number })?.errno;
                    console.log(`${LOG_PREFIX}   → recovery FAILED: errno=${rErrno}`);
                    if (rErrno !== undefined) throw recoveryError;
                    throw e;
                } finally {
                    inRecovery = false;
                }
            }
        };
    } else {
        console.warn(`${LOG_PREFIX} FS.lookupPath not found — lazy loading disabled`);
    }

    // ── 2. Patch readdir: merge MEMFS + VFS entries ────────────────
    //
    // CRITICAL: Emscripten's __syscall_getdents64 calls FS.lookupNode()
    // (NOT FS.lookupPath()) on each readdir entry to get d_type metadata.
    // lookupNode bypasses our proxy, so VFS-only entries would cause
    // ENOENT and break os.listdir().  To fix this, we pre-create MEMFS
    // nodes (dirs as mkdir, files as 0-byte stubs) for VFS-only entries
    // during the merge.  Stub files get overwritten by the lookupPath
    // proxy's lazy-load when actually opened/read.

    const origReaddir = FS.readdir.bind(FS);
    FS.readdir = (path: string): string[] => {
        const normalized = normalizePath(path);
        // Resolve aliases for readdir too (e.g. listing sysroot/lib)
        const resolved = resolveAlias(normalized, aliases);

        // Ensure the directory exists in MEMFS (lazy-create from VFS)
        let memEntries: string[];
        try {
            memEntries = origReaddir(path);
        } catch {
            // Directory doesn't exist in MEMFS yet — try to create it from VFS
            if (!isSystemPath(resolved)) {
                const stat = vfs.statSync(resolved);
                if (stat?.type === 'dir') {
                    try { FS.mkdirTree(normalized); } catch { /* exists */ }
                    try { memEntries = origReaddir(path); } catch { memEntries = []; }
                } else {
                    memEntries = [];
                }
            } else {
                memEntries = [];
            }
        }

        // Merge VFS entries (use resolved/alias path for VFS query)
        if (!isSystemPath(resolved)) {
            const vfsEntries = vfs.readdirSync(resolved);
            if (vfsEntries.length > 0) {
                const memSet = new Set(memEntries);
                const merged = new Set(memEntries);

                // For VFS-only entries, create stub MEMFS nodes so that
                // FS.lookupNode() succeeds in __syscall_getdents64.
                for (const entry of vfsEntries) {
                    merged.add(entry);
                    if (!memSet.has(entry)) {
                        const entryPath = normalized === '/' ? `/${entry}` : `${normalized}/${entry}`;
                        // Resolve alias for the child path too
                        const resolvedEntry = resolveAlias(entryPath, aliases);
                        const entryStat = vfs.statSync(resolvedEntry);
                        if (entryStat) {
                            try {
                                if (entryStat.type === 'dir') {
                                    origMkdir(entryPath);
                                } else {
                                    // Load actual content from VFS so
                                    // reads return correct data (not an empty stub).
                                    // If not in VFS cache, fall back to 0-byte stub
                                    // (lookupPath proxy will overwrite on access).
                                    const fileData = vfs.readFileSync(resolvedEntry);
                                    origWriteFile(entryPath, fileData ?? new Uint8Array(0));
                                }
                            } catch {
                                // Entry may already exist from a previous readdir call
                            }
                        }
                    }
                }

                return [...merged];
            }
        }

        return memEntries;
    };

    // ── 3. Patch writeFile: write-through to VFS ───────────────────

    FS.writeFile = (path: string, data: string | Uint8Array): void => {
        origWriteFile(path, data);

        // Also write to VFS for inter-process visibility
        const normalized = normalizePath(path);
        if (!isSystemPath(normalized)) {
            const bytes = typeof data === 'string'
                ? new TextEncoder().encode(data)
                : data;
            try {
                vfs.writeFileSync(normalized, bytes);
            } catch {
                // Non-fatal: VFS write failure shouldn't break the process
            }
        }
    };

    // ── 4. Patch mkdir: write-through to VFS ───────────────────────

    FS.mkdir = (path: string): void => {
        origMkdir(path);

        const normalized = normalizePath(path);
        if (!isSystemPath(normalized)) {
            try { vfs.mkdirSync(normalized); } catch { /* non-fatal */ }
        }
    };

    // ── 5. Patch unlink: write-through to VFS ──────────────────────

    FS.unlink = (path: string): void => {
        origUnlink(path);

        const normalized = normalizePath(path);
        if (!isSystemPath(normalized)) {
            try { vfs.deleteFileSync(normalized); } catch { /* non-fatal */ }
        }
    };
}
