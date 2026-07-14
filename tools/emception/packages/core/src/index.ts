// @emception/core — runtime-agnostic surface.
// Public surface (pure VFS / TTY / worker-protocol types).

export * from './build-presets';
export * from './errors';
export * from './events';
export * from './tools';
export * from './types';
export * from './workspace-config';
export * from './workspace-presets';

// Subsystem namespaces (full surface).
export * as io from './io/index';
export * as runtime from './runtime/index';
export * as testing from './testing/index';
export * as tty from './tty/index';
export * as ui from './ui/index';
export * as vfs from './vfs/index';
export * as workerProtocol from './worker-protocol';
export * as workspace from './workspace/index';

// Top-level exports for the most commonly imported helpers.
export { decodeCollected, normalizeStdin, normalizeStdout, type NormalizedStdout } from './io/streams';
export type { ManifestSource, RuntimeAdapter, SpawnWorkerOptions, WorkerHandle, WorkspaceStoreHandle, WorkspaceStoreOptions } from './runtime/adapter';
export { BootCancelledError, BootError, BootHandshake } from './runtime/boot-handshake';
export { withCancellation, withTimeoutOrThrow, type CancellationOutcome, type WithCancellationOptions } from './runtime/cancellation';
export {
    assertCanvasUnsupported,
    assertNoBrowserOnlyFeatures,
    assertXtermStdinUnsupported,
    looksLikeCanvas,
    looksLikeXtermTerminal,
    type RuntimeLabel
} from './runtime/feature-guards';
export { CorrelatorDisposedError, RequestCorrelator, type RequestCorrelatorOptions } from './runtime/request-correlator';
export { messagePortTransport, RpcChannel, workerTransport, type RpcChannelOptions, type RpcTransport } from './runtime/rpc-channel';
export { assertToolResult, isToolResult } from './runtime/tool-result';
export { WorkerOrchestrator, type WorkerOrchestratorOptions, type WorkerRunOptions, type WorkerToolResult } from './runtime/worker-orchestrator';
export { compileMatcher, queryClangAst, runMatcher, type ClangAstNode, type CompiledMatcher, type MatchResult } from './testing/clang-query/matcher';
export { parseDoctestConsole, type DoctestCounts, type DoctestFailure, type DoctestReport } from './testing/doctest/parse';
export { runTests, type TestKindHandler } from './testing/engine';
export { HeadlessIOProvider, type HeadlessIOProviderOptions } from './tty/headless';
export type { IOProvider } from './tty/io-provider';
export { LineBuffer } from './tty/line-buffer';
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
} from './ui/adapters';
export { diffViewConfigs, normalizeViewConfig, toAttributes, type NormalizedViewConfig, type ViewConfigInput } from './ui/config';
export type { FSStats, IFileSystem } from './vfs/interface';
export type { FSManifest, ManifestBundle, ManifestEntry } from './vfs/manifest';
export { OverlayFS } from './vfs/overlay';
export type { MainToWorkerMessage, WorkerToMainMessage } from './worker-protocol';
export { resolveBuild, type ResolveBuildInput, type ResolvedBuild } from './workspace/build-resolver';
export { buildArgv, type BuildArgvOptions, type CompileInvocation } from './workspace/compile-argv';
export type { FileMeta, MetaSidecar, OpenWorkspaceOptions, SeedMarker, SeedPolicy, WorkspaceHandle, WorkspaceManager } from './workspace/manager';
export { hashSeed, normalizeSeedEntry } from './workspace/seed';
export { createMemoryWorkspaceManager, MemoryWorkspaceManager } from './workspace/store-memory';

