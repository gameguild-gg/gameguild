import type { TreeNode, WorkspaceFile } from './ide-types';

export function isSourceFile(path: string): boolean {
    return path.endsWith('.cpp') || path.endsWith('.cc') || path.endsWith('.cxx') || path.endsWith('.c');
}

export function isTextFile(path: string): boolean {
    return (
        !path.endsWith('.svg') && !path.endsWith('.png') && !path.endsWith('.jpg') && !path.endsWith('.jpeg') && !path.endsWith('.gif') && !path.endsWith('.webp')
    );
}

export function toWorkspaceFsPath(path: string): string {
    if (path.startsWith('/src/')) return `/home/user/${path.slice('/src/'.length)}`;
    return `/home/user/${fileName(path)}`;
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
 * Content for a `--js-library` file written to the emception VFS before every
 * SDL3 compile.  `--js-library` files are read by emscripten's `jsifier.mjs`
 * subprocess *during* JS-glue generation.  Writing an `addToLibrary` stub here
 * makes `emscripten_asm_const_int_sync_on_main_thread` (a helper referenced by
 * the CDN libSDL3.a objects compiled with MAIN_THREAD_EM_ASM) available to
 * jsifier.mjs so it does not abort with an empty stdout, which would otherwise
 * trigger:
 *   AssertionError: Did not receive forwarded data in pre output
 *   (emscripten.py:427, phase_emscript → emscript → assert)
 *
 * NOTE: emscripten 5.0.4 (the sysroot version) uses `addToLibrary({...})` —
 * the old `mergeInto(LibraryManager.library, {...})` API was removed in
 * emscripten 4.0 and causes a ReferenceError in jsifier.mjs context.
 *
 * NOTE: `--pre-js` does NOT fix this because it is appended to the *final*
 * output after jsifier.mjs has already finished (or crashed).
 */
export const SDL3_JS_LIB_STUB = `// Stub implementations of EM_ASM main-thread helpers for single-threaded
// SDL3 builds.  The CDN libSDL3.a includes object files compiled with
// MAIN_THREAD_EM_ASM that reference these functions; providing no-op stubs
// via --js-library prevents emscripten's jsifier.mjs from aborting during
// JS-glue generation.
// emscripten 5.0.4+ API: use addToLibrary (mergeInto was removed in 4.0).
addToLibrary({
  emscripten_asm_const_int_sync_on_main_thread__sig: 'ippp',
  emscripten_asm_const_int_sync_on_main_thread: (code, sig_ptr, arg_buf) => 0,
  emscripten_asm_const_async_on_main_thread__sig: 'vppp',
  emscripten_asm_const_async_on_main_thread: (code, sig_ptr, arg_buf) => {},
});
`;

/**
 * Returns emcc args that link against the CDN-deployed /usr/lib/libSDL3.a.
 *
 * Two flags are required at two different pipeline stages:
 *  1. `-Wl,--unresolved-symbols=ignore-all` (wasm-ld stage) — lets the linker
 *     continue despite undefined symbols from SDL3 camera/audio object files.
 *  2. `--js-library __sdl_lib.js` (compiler.js stage) — makes no-op stubs for
 *     the same pthread-only symbols available to the JS-glue generator so it
 *     does not crash.  The caller must write SDL3_JS_LIB_STUB to that VFS path
 *     before invoking emcc.
 */
export function buildSDL3Args(targetFsPath: string): string[] {
    return [
        'emcc',
        targetFsPath,
        '/usr/lib/libSDL3.a',
        '-I/usr/include',
        '-sSINGLE_FILE=1',
        '-sALLOW_MEMORY_GROWTH=1',
        '-sENVIRONMENT=web',
        // [stage 1] wasm-ld: skip undefined pthread-only symbols from SDL3
        // camera/sensor .o files so the linker does not abort early.
        '-Wl,--unresolved-symbols=ignore-all',
        // [stage 2] compiler.js: provide no-op JS stubs for the same symbols
        // so the internal JS-glue generator does not crash.
        // IMPORTANT: --js-library (not --pre-js) is required here because
        // --pre-js content is appended AFTER compiler.js finishes — it cannot
        // save compiler.js from an in-process crash.
        '--js-library', '/home/user/__sdl_lib.js',
        '-O1',
        '-o',
        '/home/user/main.js',
    ];
}

/**
 * Returns emcc args that use emscripten's built-in SDL3 port (-sUSE_SDL=3).
 * The port is built cleanly without camera/sensor modules so none of the
 * pthread EM_ASM issues apply.  Does NOT require /usr/lib/libSDL3.a or the
 * --js-library stub.  Preferred over buildSDL3Args when the port is cached in
 * the emception sysroot; falls back to buildSDL3Args otherwise.
 */
export function buildSDL3ArgsPort(targetFsPath: string): string[] {
    return [
        'emcc',
        targetFsPath,
        '-sUSE_SDL=3',
        '-I/usr/include',
        '-sSINGLE_FILE=1',
        '-sALLOW_MEMORY_GROWTH=1',
        '-sENVIRONMENT=web',
        '-O1',
        '-o',
        '/home/user/main.js',
    ];
}
