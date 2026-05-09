// Workspace subsystem barrel.
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
export {
    exportWorkspace,
    importWorkspace,
    type ExportWorkspaceOptions,
    type ImportPolicy,
    type ImportWorkspaceOptions,
    type ImportWorkspaceReport
} from './transfer.js';
export {
    crc32, createZip, readZip,
    type CreateZipOptions,
    type ZipEntry
} from './zip.js';

