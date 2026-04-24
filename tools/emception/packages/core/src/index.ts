// @emception/core — runtime-agnostic surface.
// Phase 0 skeleton + Phase 0.2 first slice (pure VFS / TTY / worker-protocol types).

export * from './errors.js';
export * from './events.js';
export * from './presets.js';
export * from './tools.js';
export * from './types.js';

// Subsystem namespaces (full surface).
export * as io from './io/index.js';
export * as runtime from './runtime/adapter.js';
export * as testing from './testing/index.js';
export * as tty from './tty/index.js';
export * as ui from './ui/index.js';
export * as vfs from './vfs/index.js';
export * as workerProtocol from './worker-protocol.js';
export * as workspace from './workspace/index.js';

// Top-level re-exports for the most commonly imported helpers.
export {
    decodeCollected,
    normalizeStdin,
    normalizeStdout,
    type NormalizedStdout
} from './io/streams.js';
export type {
    ManifestSource,
    RuntimeAdapter,
    SpawnWorkerOptions,
    WorkerHandle,
    WorkspaceStoreHandle,
    WorkspaceStoreOptions
} from './runtime/adapter.js';
export { runTests, type TestKindHandler } from './testing/engine.js';
export { HeadlessIOProvider, type HeadlessIOProviderOptions } from './tty/headless.js';
export type { IOProvider } from './tty/io-provider.js';
export { LineBuffer } from './tty/line-buffer.js';
export type { FSStats, IFileSystem } from './vfs/interface.js';
export type { FSManifest, ManifestBundle, ManifestEntry } from './vfs/manifest.js';
export { OverlayFS } from './vfs/overlay.js';
export type { MainToWorkerMessage, WorkerToMainMessage } from './worker-protocol.js';
export {
    resolveBuild,
    type ResolveBuildInput,
    type ResolvedBuild
} from './workspace/build-resolver.js';
export {
    buildArgv,
    type BuildArgvOptions,
    type CompileInvocation
} from './workspace/compile-argv.js';
export type {
    FileMeta,
    MetaSidecar,
    OpenWorkspaceOptions,
    SeedMarker,
    SeedPolicy,
    WorkspaceHandle,
    WorkspaceManager
} from './workspace/manager.js';
export { hashSeed, normalizeSeedEntry } from './workspace/seed.js';
export {
    createMemoryWorkspaceManager,
    MemoryWorkspaceManager
} from './workspace/store-memory.js';

