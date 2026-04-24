// Workspace subsystem barrel (Phase 3 in progress).
export {
    resolveBuild,
    type ResolveBuildInput,
    type ResolvedBuild
} from './build-resolver.js';
export {
    buildArgv,
    type BuildArgvOptions,
    type CompileInvocation
} from './compile-argv.js';
export type {
    FileMeta,
    MetaSidecar,
    OpenWorkspaceOptions,
    SeedMarker,
    SeedPolicy,
    WorkspaceHandle,
    WorkspaceManager
} from './manager.js';
export { hashSeed, normalizeSeedEntry } from './seed.js';
export {
    createMemoryWorkspaceManager,
    MemoryWorkspaceManager
} from './store-memory.js';

