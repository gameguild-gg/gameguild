// @emception/core — runtime-agnostic surface.
// Public surface (pure VFS / TTY / worker-protocol types).

export * from './build-presets.js';
export * from './errors.js';
export * from './events.js';
export * from './tools.js';
export * from './types.js';
export * from './workspace-config.js';
export * from './workspace-presets.js';

// Subsystem namespaces (full surface).
export * as io from './io/index.js';
export * as runtime from './runtime/index.js';
export * as testing from './testing/index.js';
export * as tty from './tty/index.js';
export * as ui from './ui/index.js';
export * as vfs from './vfs/index.js';
export * as workerProtocol from './worker-protocol.js';
export * as workspace from './workspace/index.js';

// Top-level exports for the most commonly imported helpers.
export { decodeCollected, normalizeStdin, normalizeStdout, type NormalizedStdout } from './io/streams.js';
export type { ManifestSource, RuntimeAdapter, SpawnWorkerOptions, WorkerHandle, WorkspaceStoreHandle, WorkspaceStoreOptions } from './runtime/adapter.js';
export { BootCancelledError, BootError, BootHandshake } from './runtime/boot-handshake.js';
export { withCancellation, withTimeoutOrThrow, type CancellationOutcome, type WithCancellationOptions } from './runtime/cancellation.js';
export {
    assertCanvasUnsupported,
    assertNoBrowserOnlyFeatures,
    assertXtermStdinUnsupported,
    looksLikeCanvas,
    looksLikeXtermTerminal,
    type RuntimeLabel
} from './runtime/feature-guards.js';
export { CorrelatorDisposedError, RequestCorrelator, type RequestCorrelatorOptions } from './runtime/request-correlator.js';
export { messagePortTransport, RpcChannel, workerTransport, type RpcChannelOptions, type RpcTransport } from './runtime/rpc-channel.js';
export { assertToolResult, isToolResult } from './runtime/tool-result.js';
export { WorkerOrchestrator, type WorkerOrchestratorOptions, type WorkerRunOptions, type WorkerToolResult } from './runtime/worker-orchestrator.js';
export { compileMatcher, queryClangAst, runMatcher, type ClangAstNode, type CompiledMatcher, type MatchResult } from './testing/clang-query/matcher.js';
export { parseDoctestConsole, type DoctestCounts, type DoctestFailure, type DoctestReport } from './testing/doctest/parse.js';
export { runTests, type TestKindHandler } from './testing/engine.js';
export { HeadlessIOProvider, type HeadlessIOProviderOptions } from './tty/headless.js';
export type { IOProvider } from './tty/io-provider.js';
export { LineBuffer } from './tty/line-buffer.js';
export {
    ATTRIBUTE_SCHEMA,
    camelToKebab,
    domEventNameFor,
    EVENT_DOM_NAMES,
    kebabToCamel,
    parseAttributesToInput,
    parseBooleanAttr,
    parseListAttr,
    type EventDomName,
    type ParseAttributesOptions
} from './ui/adapters.js';
export { diffViewConfigs, normalizeViewConfig, toAttributes, type NormalizedViewConfig, type ViewConfigInput } from './ui/config.js';
export type { FSStats, IFileSystem } from './vfs/interface.js';
export type {
  FSManifest,
  LegacyFSManifest,
  ManifestBundle,
  ManifestEntry,
  ManifestToolVersions,
  ReleaseFSManifest,
  WasmArtifactProfile,
} from './vfs/manifest.js';
export { OverlayFS } from './vfs/overlay.js';
export type { MainToWorkerMessage, WorkerToMainMessage } from './worker-protocol.js';
export { resolveBuild, type ResolveBuildInput, type ResolvedBuild } from './workspace/build-resolver.js';
export { buildArgv, type BuildArgvOptions, type CompileInvocation } from './workspace/compile-argv.js';
export type { FileMeta, MetaSidecar, OpenWorkspaceOptions, SeedMarker, SeedPolicy, WorkspaceHandle, WorkspaceManager } from './workspace/manager.js';
export { hashSeed, normalizeSeedEntry } from './workspace/seed.js';
export { createMemoryWorkspaceManager, MemoryWorkspaceManager } from './workspace/store-memory.js';

