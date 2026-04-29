// @emception/browser — Web Worker + IDB + OffscreenCanvas adapter.
// Phase 0.2: full source migrated from `tools/emception/src/`. Re-exports
// the boot surface (createEmception, boot, bootInWorker) plus VFS, shell,
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
} from './boot';

// Lower-level VFS surface (LazyFS, IDBFS, mountVFSFS) for advanced consumers
// that want to compose their own VFSManager.
export { IDBFS, LazyFS, mountVFSFS, type FileEntry, type FSManifest, type IDBFSOptions, type MountVFSFSOptions, type VFSFSRuntime } from './vfs/index';

export const DEFAULT_MANIFEST_URL = 'https://cdn.jsdelivr.net/npm/@emception/sysroot@0.20.0/manifest.json';

// Headless build presets + compileAndRun helper.
export {
    BROWSER_BUILD_PRESETS,
    compileAndRun,
    type BrowserBuildPreset,
    type BrowserBuildPresetName,
    type CompileAndRunOptions,
    type CompileAndRunResult,
    type CompilePaths,
    type CompilePhase
} from './presets';

