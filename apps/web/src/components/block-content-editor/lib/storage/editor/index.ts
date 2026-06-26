/**
 * Editor Storage \u2014 Public Types Barrel.
 *
 * Re-exports the canonical type modules. Import from this barrel so callers
 * never need to know which file actually owns a given type.
 */

export {
  STORAGE_TYPES,
  type StorageType,
  SYNC_STATUS,
  type SyncStatus,
} from "./storage-types"

export {
  type ProjectData,
  type ProjectMetadata,
  type ProjectMetadataRecord,
} from "./project-data"

export {
  type ProjectType,
  DEFAULT_PROJECT_TYPE,
  PROJECT_TYPE_LABELS,
  type ProjectTypeStructure,
  getProjectTypeStructure,
  getProjectTypeLabel,
} from "./project-types"

export {
  type ProjectPreferences,
} from "./project-preferences"

export {
  type Block,
  type BlockArray,
  type BlockStorage,
  type BlockDataMap,
  type AnyBlockData,
  type BlockCellType,
  BLOCK_CELL_TYPES,
  nextBlockId,
} from "./block-structure"

export {
  EMPTY_PROJECT_DATA,
  serializeProject,
  deserializeProject,
  blockToPreviewNode,
  type PreviewNode,
} from "./block-storage"

export { generateProjectId } from "./project-id"
