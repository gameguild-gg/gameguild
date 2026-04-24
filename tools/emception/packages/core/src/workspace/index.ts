// Workspace subsystem barrel (Phase 3 in progress).
export {
    resolveBuild,
    type ResolveBuildInput,
    type ResolvedBuild
} from './build-resolver';
export type {
    FileMeta,
    MetaSidecar,
    OpenWorkspaceOptions,
    SeedMarker,
    SeedPolicy,
    WorkspaceHandle,
    WorkspaceManager
} from './manager';
export { hashSeed, normalizeSeedEntry } from './seed';
export {
    createMemoryWorkspaceManager,
    MemoryWorkspaceManager
} from './store-memory';

