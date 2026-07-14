// @emception/browser — Web Worker + IDB + OffscreenCanvas adapter.
// Exports the boot surface (createEmception, boot, bootInWorker) plus VFS, shell,
// tool-runner, loader, emscripten bridge, and async-strategy helpers.

export {
    boot,
    bootInWorker,
    clearModuleCache,
    createBrowserBridge,
    createEmception,
    createVFSManager,
    decompressBrotli,
    detectAsyncStrategy,
    isBrotliSupported,
    LineBuffer,
    loadModuleFactory,
    MiniShell,
    SUBPROCESS_SHIM,
    ToolRunner,
    TTYBridge,
    type BootResult,
    type BrowserBridge,
    type CreateEmceptionOptions,
    type EmceptionAPI,
    type IOProvider,
    type RunOptions,
    type ToolResult,
    type VFSManager,
    type WorkerBootResult
} from './boot.js';

// Lower-level VFS surface (LazyFS, IDBFS, mountVFSFS) for advanced consumers
// that want to compose their own VFSManager.
export { IDBFS, LazyFS, mountVFSFS, type FileEntry, type FSManifest, type IDBFSOptions, type MountVFSFSOptions, type VFSFSRuntime } from './vfs/index.js';

export { DEFAULT_MANIFEST_URL } from './createEmception.js';

// Headless build presets + compileAndRun helper.
export {
    compileAndRun,
    TOOLCHAIN_PRESETS,
    type CMakePreset,
    type CompileAndRunOptions,
    type CompileAndRunResult,
    type CompilePaths,
    type CompilePhase,
    type NativePreset,
    type Preset,
    type PythonPreset
} from './presets.js';
