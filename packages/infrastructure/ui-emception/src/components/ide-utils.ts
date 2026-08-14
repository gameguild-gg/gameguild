import type { TreeNode, WorkspaceFile } from './ide-types';

export function isSourceFile(path: string): boolean {
    return path.endsWith('.cpp') || path.endsWith('.cc') || path.endsWith('.cxx') || path.endsWith('.c');
}

export function isTextFile(path: string): boolean {
    return (
        !path.endsWith('.svg') && !path.endsWith('.png') && !path.endsWith('.jpg') && !path.endsWith('.jpeg') && !path.endsWith('.gif') && !path.endsWith('.webp')
    );
}

/**
 * Map any workspace path to the flat VFS mount `/app/<name>`. localStorage
 * isolation (per-assessment) lives in `workspaceStorageKey`, not in the path.
 * `_assignmentToken` is kept in the signature for backward-compat only.
 */
export function toWorkspaceFsPath(path: string, _assignmentToken?: string): string {
    const rest = path.startsWith('/user/') ? path.slice('/user/'.length) : fileName(path);
    return `/app/${rest}`;
}

export function fileName(path: string): string {
    const parts = path.split('/').filter(Boolean);
    return parts[parts.length - 1] || path;
}

export function inferLanguage(path: string): string {
    if (path.endsWith('.cpp') || path.endsWith('.cc') || path.endsWith('.cxx')) return 'cpp';
    if (path.endsWith('.c')) return 'c';
    if (path.endsWith('.h') || path.endsWith('.hpp')) return 'cpp';
    if (path.endsWith('.md')) return 'markdown';
    if (path.endsWith('.json')) return 'json';
    return 'plaintext';
}

export function buildFileTree(paths: string[]): TreeNode[] {
    const root: TreeNode = { name: '/', path: '/', isDir: true, children: [] };

    for (const rawPath of paths.sort()) {
        const parts = rawPath.split('/').filter(Boolean);
        let current = root;
        let currentPath = '';

        for (let i = 0; i < parts.length; i++) {
            const part = parts[i];
            currentPath += `/${part}`;
            const isDir = i < parts.length - 1;

            let next = current.children.find((c) => c.name === part && c.isDir === isDir);
            if (!next) {
                next = { name: part, path: currentPath, isDir, children: [] };
                current.children.push(next);
                current.children.sort((a, b) => {
                    if (a.isDir !== b.isDir) return a.isDir ? -1 : 1;
                    return a.name.localeCompare(b.name);
                });
            }
            current = next;
        }
    }
    return root.children;
}

/**
 * Returns true if any text source file in the workspace includes SDL3 headers.
 * Used to select the SDL3 compile path over the standard WASI path.
 */
export function detectsSDL(files: Record<string, WorkspaceFile>): boolean {
    return Object.values(files)
        .filter((f) => f.type === 'text' && isSourceFile(f.path))
        .some((f) => f.content.includes('#include <SDL3/') || f.content.includes('#include "SDL3/'));
}

/**
 * Returns emcc args that use emscripten's built-in SDL3 port (-sUSE_SDL=3).
 *
 * Output is main.wasm (standalone/WASI mode). The linker's JS-generation step
 * (compiler.mjs) is not available in the Emception browser environment, so we
 * cannot use -o main.js. Instead, the WASI imports required by the standalone
 * binary are satisfied by makeWasiStubs() at runtime in Ide.tsx.
 */
export function buildSDL3ArgsPort(targetFsPath: string): string[] {
    return [
        'emcc', targetFsPath,
        '-sUSE_SDL=3',
        '-I/usr/include',
        '-sALLOW_MEMORY_GROWTH=1',
        '-sENVIRONMENT=web',
        '-O1',
        '-o', '/app/main.wasm',
    ];
}

/**
 * Build a wasi_snapshot_preview1 shim suitable for SDL3 canvas apps.
 *
 * Standalone WASM compiled with emcc -o main.wasm imports WASI symbols.
 * SDL3 canvas apps don't use the filesystem or args at runtime; they only need
 * basic clock/stdio stubs so the startup sequence doesn't abort.
 *
 * The `getMemory` callback lets the stubs lazily resolve the WASM linear memory
 * (available only after WebAssembly.instantiate returns).
 */
export function makeWasiStubs(
    getMemory: () => WebAssembly.Memory | null,
    writeLine: (s: string) => void,
): Record<string, CallableFunction> {
    // WASI error codes
    const WASI_ESUCCESS = 0;
    const WASI_EBADF = 8;
    const WASI_ESPIPE = 70;

    return {
        // argc = 0, argv_buf_size = 0
        args_sizes_get(argc_ptr: number, argv_buf_size_ptr: number): number {
            const mem = getMemory();
            if (mem) {
                const v = new DataView(mem.buffer);
                v.setUint32(argc_ptr, 0, true);
                v.setUint32(argv_buf_size_ptr, 0, true);
            }
            return WASI_ESUCCESS;
        },
        args_get(): number { return WASI_ESUCCESS; },

        // environ_count = 0, environ_buf_size = 0
        environ_sizes_get(count_ptr: number, buf_size_ptr: number): number {
            const mem = getMemory();
            if (mem) {
                const v = new DataView(mem.buffer);
                v.setUint32(count_ptr, 0, true);
                v.setUint32(buf_size_ptr, 0, true);
            }
            return WASI_ESUCCESS;
        },
        environ_get(): number { return WASI_ESUCCESS; },

        // Write iov buffers (fd 1 = stdout, fd 2 = stderr)
        fd_write(fd: number, iovs_ptr: number, iovs_len: number, nwritten_ptr: number): number {
            const mem = getMemory();
            if (!mem) return WASI_EBADF;
            const v = new DataView(mem.buffer);
            const u8 = new Uint8Array(mem.buffer);
            let totalWritten = 0;
            const decoder = new TextDecoder();
            for (let i = 0; i < iovs_len; i++) {
                const base = v.getUint32(iovs_ptr + i * 8, true);
                const len = v.getUint32(iovs_ptr + i * 8 + 4, true);
                if (len > 0) {
                    writeLine(decoder.decode(u8.subarray(base, base + len)));
                    totalWritten += len;
                }
            }
            v.setUint32(nwritten_ptr, totalWritten, true);
            return WASI_ESUCCESS;
        },
        fd_close(): number { return WASI_ESUCCESS; },
        fd_seek(): number { return WASI_ESPIPE; },
        fd_read(): number { return WASI_EBADF; },
        fd_fdstat_get(): number { return WASI_EBADF; },
        path_open(): number { return WASI_EBADF; },
        path_filestat_get(): number { return WASI_EBADF; },
        path_unlink_file(): number { return WASI_EBADF; },

        // Monotonic clock → nanoseconds via performance.now()
        clock_time_get(clk_id: number, _precision_lo: number, _precision_hi: number, time_ptr: number): number {
            const mem = getMemory();
            if (mem) {
                const t = BigInt(Math.round(performance.now() * 1_000_000));
                new DataView(mem.buffer).setBigUint64(time_ptr, t, true);
            }
            return WASI_ESUCCESS;
        },
        clock_res_get(clk_id: number, res_ptr: number): number {
            const mem = getMemory();
            if (mem) new DataView(mem.buffer).setBigUint64(res_ptr, 1n, true);
            return WASI_ESUCCESS;
        },

        random_get(buf_ptr: number, buf_len: number): number {
            const mem = getMemory();
            if (mem) crypto.getRandomValues(new Uint8Array(mem.buffer, buf_ptr, buf_len));
            return WASI_ESUCCESS;
        },

        proc_exit(code: number): void {
            // code 0 = clean exit (SDL3 with SDL_MAIN_USE_CALLBACKS exits with 0
            // after SDL_AppInit + emscripten_set_main_loop registration).  Let
            // _start return normally; the animation-frame loop is already live.
            if (code !== 0) throw new Error(`SDL3 proc_exit(${code})`);
        },
    };
}
