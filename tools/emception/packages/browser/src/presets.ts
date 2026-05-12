// Headless build presets for the browser API.
//
// These encapsulate the argv arrays that the IDE has historically inlined
// for direct (driver-less) clang -cc1 + wasm-ld + wasi-run pipelines.
// Lifting them into the `@emception/browser` package lets minimal demos
// (and any future consumer) drive a compile+run cycle without copying
// the dozens of cc1 flags by hand.
//
// This is the *low-level* preset surface: it returns argv ready to feed
// to `EmceptionAPI.run`. The richer `compileAndRun` orchestration in
// `emception` (events, workspace seeding, cancellation) is a
// separate, higher-level layer that lands later in the roadmap.

import type { EmceptionAPI } from './createEmception';
import type { ToolResult } from './tool-runner';

export type BrowserBuildPresetName = 'c' | 'cpp' | 'sdl' | 'raylib';

export interface CompilePaths {
    /** Source file path inside the VFS (also appears in error messages). */
    sourcePath: string;
    /** Object file path produced by the compile step. */
    objectPath: string;
    /** Final wasm artifact path produced by the link step. */
    wasmPath: string;
}

const DEFAULT_PATHS: CompilePaths = {
    sourcePath: '/home/user/main.cpp',
    objectPath: '/home/user/main.o',
    wasmPath: '/home/user/main.wasm',
};

/**
 * Common clang -cc1 frontend flags (no isystem entries — those are
 * appended per-language so the C++ stdlib search path can be inserted
 * BEFORE /usr/include, which is required by libc++'s `<cctype>`,
 * `<cwchar>`, `<cwctype>` etc.
 */
const CC1_FRONTEND: readonly string[] = [
    '-cc1',
    '-triple',
    'wasm32-unknown-emscripten',
    '-emit-obj',
    '-O1',
    '-disable-free',
    '-clear-ast-before-backend',
    '-disable-llvm-verifier',
    '-discard-value-names',
    '-mrelocation-model',
    'static',
    '-mframe-pointer=none',
    '-ffp-contract=on',
    '-fno-rounding-math',
    '-mconstructor-aliases',
    '-target-cpu',
    'generic',
    '-fvisibility=hidden',
];

/** Trailing cc1 flags shared by all presets. */
const CC1_TAIL: readonly string[] = ['-fdeprecated-macro', '-ferror-limit', '19', '-fgnuc-version=4.2.1'];

/** C-language include search path. */
const CC1_C_INCLUDES: readonly string[] = [
    '-resource-dir',
    '/usr/lib/clang/23',
    '-internal-isystem',
    '/usr/lib/clang/23/include',
    '-internal-isystem',
    '/usr/include',
];

/**
 * C++ include search path — libc++ headers must come BEFORE /usr/include
 * so that `<cctype>` resolves to libc++'s shim that re-exports `<ctype.h>`
 * via `__cxx_libc++` rather than directly grabbing the C `<ctype.h>`.
 */
const CC1_CPP_INCLUDES: readonly string[] = [
    '-internal-isystem',
    '/usr/include/c++/v1',
    '-internal-isystem',
    '/usr/include/compat',
    '-internal-isystem',
    '/usr/lib/clang/23/include',
    '-resource-dir',
    '/usr/lib/clang/23',
    '-internal-isystem',
    '/usr/include',
];

const CC1_CPP_EXC: readonly string[] = ['-fcxx-exceptions', '-fexceptions'];

/** Standard wasm-ld link line for emscripten-style libc. */
const WASM_LD_BASE: readonly string[] = [
    '-L/usr/lib/emscripten/cache-lib/wasm32-emscripten',
    '--entry=main',
    '--import-undefined',
    '--allow-undefined',
    '--export-table',
    '--table-base=1',
    '--export=__wasm_call_ctors',
    '-lc',
    '-ldlmalloc',
    '-lcompiler_rt',
];

const WASM_LD_CPP_LIBS: readonly string[] = ['-lc++-noexcept', '-lc++abi-noexcept', '-lsockets'];

const WASM_LD_C_LIBS: readonly string[] = ['-lsockets'];

/**
 * SDL3 link line. Differs significantly from the WASI runtime path: uses
 * the emscripten sysroot (not cache-lib), pulls in `crt1.o` + `libSDL3.a`,
 * uses `--no-entry` and SDL_App* exports for the SDL3 callback model, and
 * links the GL/al/html5/stubs emscripten libs needed for browser-side SDL.
 */
const WASM_LD_SDL_FLAGS: readonly string[] = [
    '-L/usr/lib/emscripten/cache/sysroot/lib/wasm32-emscripten',
    '-L/usr/lib/emscripten/src/lib',
    '/usr/lib/emscripten/cache/sysroot/lib/wasm32-emscripten/crt1.o',
    '/usr/lib/emscripten/cache/sysroot/lib/wasm32-emscripten/libSDL3.a',
    '--no-entry',
    '--import-undefined',
    '--allow-undefined',
    '--export-if-defined=SDL_AppInit',
    '--export-if-defined=SDL_AppIterate',
    '--export-if-defined=SDL_AppEvent',
    '--export-if-defined=SDL_AppQuit',
    '--export-table',
    '--table-base=1',
    '-z',
    'stack-size=65536',
    '-lGL-getprocaddr',
    '-lal',
    '-lhtml5',
    '-lstubs',
    '-lc',
    '-ldlmalloc',
    '-lcompiler_rt',
    '-lc++-noexcept',
    '-lc++abi-noexcept',
    '-lsockets',
];

/** Extra cc1 -internal-isystem entries needed to find SDL3 headers. */
const CC1_SDL_EXTRA: readonly string[] = ['-internal-isystem', '/usr/include/fakesdl', '-internal-isystem', '/usr/include/SDL3'];

/** Extra cc1 -internal-isystem entries needed to find raylib headers. */
const CC1_RAYLIB_EXTRA: readonly string[] = ['-internal-isystem', '/usr/include/raylib'];

/**
 * Raylib link line. Uses prebuilt libraylib.a with the sdl3-runtime.mjs JS runtime.
 *
 * Key: do NOT link -lhtml5. libhtml5.a provides a WASM emscripten_set_main_loop
 * that bypasses sdl3-runtime.mjs's MainLoop.func setup, leaving MainLoop.func=null
 * so the RAF callback crashes. By omitting -lhtml5, emscripten_set_main_loop and
 * other HTML5 API functions become WASM imports resolved by sdl3-runtime.mjs which
 * correctly calls setMainLoop() → MainLoop.func = iterFunc → RAF works.
 *
 * Exports `main` so the runtime calls it via callMain(). emscripten_set_main_loop
 * (with simulate_infinite_loop=1) throws 'unwind' which callMain() catches, leaving
 * the RAF-based draw loop active.
 */
const WASM_LD_RAYLIB_FLAGS: readonly string[] = [
    '-L/usr/lib/emscripten/cache/sysroot/lib/wasm32-emscripten',
    '-L/usr/lib/emscripten/src/lib',
    '/usr/lib/emscripten/cache/sysroot/lib/wasm32-emscripten/crt1.o',
    '/usr/lib/libraylib.a',
    '--no-entry',
    '--import-undefined',
    '--allow-undefined',
    '--export=main',
    '--export=malloc',
    '--export=free',
    '--export=__wasm_call_ctors',
    '--export-table',
    '--table-base=1',
    '-z',
    'stack-size=2097152',
    '-lGL-getprocaddr',
    '-lal',
    '-lstubs',
    '-lc',
    '-ldlmalloc',
    '-lcompiler_rt',
    '-lc++-noexcept',
    '-lc++abi-noexcept',
    '-lsockets',
];

export interface BrowserBuildPreset {
    readonly name: BrowserBuildPresetName;
    /** Tool name to spawn for the compile step (always `'clang'` today). */
    readonly compileTool: string;
    /** Tool name to spawn for the link step. */
    readonly linkTool: string;
    /** Build the compile-step argv (argv[0] is the tool name). */
    compileArgv(paths: CompilePaths): string[];
    /** Build the link-step argv (argv[0] is the tool name). */
    linkArgv(paths: CompilePaths): string[];
}

export const BROWSER_BUILD_PRESETS: Record<BrowserBuildPresetName, BrowserBuildPreset> = {
    c: {
        name: 'c',
        compileTool: 'clang',
        linkTool: 'wasm-ld',
        compileArgv: ({ sourcePath, objectPath }) => [
            'clang',
            ...CC1_FRONTEND,
            ...CC1_C_INCLUDES,
            ...CC1_TAIL,
            '-main-file-name',
            basename(sourcePath),
            '-o',
            objectPath,
            '-x',
            'c',
            sourcePath,
        ],
        linkArgv: ({ objectPath, wasmPath }) => ['wasm-ld', objectPath, '-o', wasmPath, ...WASM_LD_BASE, ...WASM_LD_C_LIBS],
    },
    cpp: {
        name: 'cpp',
        compileTool: 'clang',
        linkTool: 'wasm-ld',
        compileArgv: ({ sourcePath, objectPath }) => [
            'clang',
            ...CC1_FRONTEND,
            ...CC1_CPP_INCLUDES,
            ...CC1_TAIL,
            ...CC1_CPP_EXC,
            '-main-file-name',
            basename(sourcePath),
            '-o',
            objectPath,
            '-x',
            'c++',
            sourcePath,
        ],
        linkArgv: ({ objectPath, wasmPath }) => ['wasm-ld', objectPath, '-o', wasmPath, ...WASM_LD_BASE, ...WASM_LD_CPP_LIBS],
    },
    sdl: {
        name: 'sdl',
        compileTool: 'clang',
        linkTool: 'wasm-ld',
        compileArgv: ({ sourcePath, objectPath }) => [
            'clang',
            ...CC1_FRONTEND,
            ...CC1_CPP_INCLUDES,
            ...CC1_SDL_EXTRA,
            ...CC1_TAIL,
            ...CC1_CPP_EXC,
            '-main-file-name',
            basename(sourcePath),
            '-o',
            objectPath,
            '-x',
            'c++',
            sourcePath,
        ],
        linkArgv: ({ objectPath, wasmPath }) => ['wasm-ld', objectPath, '-o', wasmPath, ...WASM_LD_SDL_FLAGS],
    },
    raylib: {
        name: 'raylib',
        compileTool: 'clang',
        linkTool: 'wasm-ld',
        compileArgv: ({ sourcePath, objectPath }) => [
            'clang',
            ...CC1_FRONTEND,
            ...CC1_CPP_INCLUDES,
            ...CC1_RAYLIB_EXTRA,
            ...CC1_TAIL,
            ...CC1_CPP_EXC,
            '-main-file-name',
            basename(sourcePath),
            '-o',
            objectPath,
            '-x',
            'c++',
            sourcePath,
        ],
        linkArgv: ({ objectPath, wasmPath }) => ['wasm-ld', objectPath, '-o', wasmPath, ...WASM_LD_RAYLIB_FLAGS],
    },
};

function basename(path: string): string {
    const i = path.lastIndexOf('/');
    return i < 0 ? path : path.slice(i + 1);
}

export type CompilePhase = 'write' | 'compile' | 'link' | 'run';

export interface CompileAndRunOptions {
    preset: BrowserBuildPresetName;
    /** Source code to compile. Written to `paths.sourcePath` before compiling. */
    source: string;
    /** Optional working dir for each tool invocation. Defaults to dirname(sourcePath). */
    cwd?: string;
    /** Override the default `/home/user/main.{cpp,o,wasm}` paths. */
    paths?: Partial<CompilePaths>;
    /** Stdin payload for the run step. Sent byte-by-byte, EOF after exhaustion. */
    stdin?: string;
    /** Forwarded to each `api.run` call. */
    onStdout?: (text: string) => void;
    onStderr?: (text: string) => void;
    /** Lifecycle callback fired before each phase begins. */
    onPhase?: (phase: CompilePhase) => void;
}

export interface CompileAndRunResult {
    /** Phase that produced the final exit code. */
    finalPhase: CompilePhase;
    /** Exit code of the last tool that ran. Non-zero if compile or link failed. */
    exitCode: number;
    /** Per-phase results, populated as each phase completes. */
    compile?: ToolResult;
    link?: ToolResult;
    run?: ToolResult;
}

/**
 * End-to-end "edit → compile → link → run" cycle on top of the headless
 * `EmceptionAPI`. Stops early on compile or link failure.
 */
export async function compileAndRun(api: EmceptionAPI, opts: CompileAndRunOptions): Promise<CompileAndRunResult> {
    const preset = BROWSER_BUILD_PRESETS[opts.preset];
    if (!preset) {
        throw new Error(`compileAndRun: unknown preset '${opts.preset}'`);
    }

    const paths: CompilePaths = { ...DEFAULT_PATHS, ...opts.paths };
    const cwd = opts.cwd ?? dirname(paths.sourcePath);
    const runOpts = {
        cwd,
        onStdout: opts.onStdout,
        onStderr: opts.onStderr,
    };

    opts.onPhase?.('write');
    await api.writeFile(paths.sourcePath, opts.source);

    opts.onPhase?.('compile');
    const compile = await api.run(preset.compileTool, preset.compileArgv(paths), runOpts);
    if (compile.exitCode !== 0) {
        return { finalPhase: 'compile', exitCode: compile.exitCode, compile };
    }

    opts.onPhase?.('link');
    const link = await api.run(preset.linkTool, preset.linkArgv(paths), runOpts);
    if (link.exitCode !== 0) {
        return { finalPhase: 'link', exitCode: link.exitCode, compile, link };
    }

    opts.onPhase?.('run');
    const stdinFn = makeStdinFeeder(opts.stdin);
    const run = await api.run('wasi-run', ['wasi-run', paths.wasmPath], {
        ...runOpts,
        stdin: stdinFn,
    });
    return { finalPhase: 'run', exitCode: run.exitCode, compile, link, run };
}

function dirname(path: string): string {
    const i = path.lastIndexOf('/');
    return i <= 0 ? '/' : path.slice(0, i);
}

function makeStdinFeeder(stdin: string | undefined): (() => number | null) | undefined {
    if (stdin === undefined) return undefined;
    const enc = new TextEncoder();
    const bytes = enc.encode(stdin.endsWith('\n') ? stdin : stdin + '\n');
    let i = 0;
    return () => (i >= bytes.length ? null : bytes[i++]!);
}
