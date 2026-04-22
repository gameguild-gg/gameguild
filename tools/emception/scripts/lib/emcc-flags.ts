/**
 * Shared Emscripten link flag presets.
 *
 * Every standalone-tool build script (binaryen, ninja, cmake, llvm, cpython,
 * imgui) used to inline the same ~20-line block of `-sSTANDALONE_FLAGS`. They
 * drifted: stack sizes were inconsistent, `ASYNCIFY_IMPORTS` lists fell out of
 * sync, and adding a new syscall required editing 6+ files.
 *
 * `standaloneFlags(opts)` returns the canonical preset as a single space-joined
 * string ready to drop into a CMake `EMSCRIPTEN_LINK_FLAGS` or `emcc` command.
 * Per-tool overrides (stack size, extra exports, extra asyncify imports) live
 * in the `opts` argument so the call-site stays self-documenting.
 *
 * If a flag is identical for every tool, change it here once.
 */

/**
 * The set of imported functions that may suspend execution via Asyncify.
 *
 * Keep this in sync with the JS hooks installed by `tool-runner.ts`. Adding a
 * new VFS hook without listing it here will hard-crash with
 * "RuntimeError: function signature mismatch" the first time it's called.
 */
export const ASYNCIFY_IMPORTS: readonly string[] = Object.freeze([
    '__syscall_openat',
    '__syscall_stat64',
    '__syscall_lstat64',
    '__syscall_faccessat',
    '__syscall_readlinkat',
    '__syscall_newfstatat',
    '__emscripten_system',
]);

export interface StandaloneFlagsOptions {
    /** Asyncify wind/unwind stack in bytes. Default: 64 KB. */
    asyncifyStackSize?: number;
    /** WASM linear-memory stack in bytes. Default: 4 MB. */
    stackSize?: number;
    /** Extra `-sEXPORTED_FUNCTIONS=` entries beyond `_main`. */
    extraExports?: readonly string[];
    /** Extra Asyncify imports unique to this tool. */
    extraAsyncifyImports?: readonly string[];
    /** Append additional raw flags verbatim. */
    extra?: readonly string[];
}

/**
 * Canonical link flags for a "standalone tool" Emscripten module
 * (one entry-point `main`, ES6 module, Asyncify, no shared libs).
 */
export function standaloneFlags(opts: StandaloneFlagsOptions = {}): string {
    const {
        asyncifyStackSize = 65536,
        stackSize = 4 * 1024 * 1024,
        extraExports = [],
        extraAsyncifyImports = [],
        extra = [],
    } = opts;

    const exports = ['_main', ...extraExports];
    const imports = [...ASYNCIFY_IMPORTS, ...extraAsyncifyImports];

    return [
        '-sALLOW_MEMORY_GROWTH=1',
        `-sSTACK_SIZE=${stackSize}`,
        '-sFORCE_FILESYSTEM=1',
        '-sMODULARIZE=1',
        '-sEXPORT_ES6=1',
        '-sEXIT_RUNTIME=1',
        // Don't auto-run main — kernel calls callMain() itself.
        '-sINVOKE_RUN=0',
        `-sEXPORTED_FUNCTIONS=${exports.join(',')}`,
        '-sEXPORTED_RUNTIME_METHODS=FS,callMain',
        // Emscripten JS-based exception handling — compatible with Asyncify.
        // -fwasm-exceptions requires reference-types, which conflicts with
        // -mno-reference-types needed for Asyncify instrumentation.
        '-sDISABLE_EXCEPTION_CATCHING=0',
        // Asyncify: transparent async suspension for FS hooks + subprocess dispatch.
        '-sASYNCIFY',
        `-sASYNCIFY_STACK_SIZE=${asyncifyStackSize}`,
        `-sASYNCIFY_IMPORTS=${JSON.stringify(imports)}`,
        '-mno-reference-types',
        ...extra,
    ].join(' ');
}
