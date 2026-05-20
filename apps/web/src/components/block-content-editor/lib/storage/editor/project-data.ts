import type { ProjectPreferences } from "./project-preferences"
import type { StorageType, SyncStatus } from "./storage-types"

/**
 * Tracking metadata for a project (size, content hash, timestamps).
 * Centralized so callers don't need to know which fields live "on the project"
 * versus "in metadata".
 */
export interface ProjectMetadata {
  size: number
  hash: string
  createdAt: string
  updatedAt: string
}

/**
 * Full project record stored in IndexedDB / synced with remote storage.
 *
 * This is the single source of truth for the project shape — DO NOT redefine
 * locally in components/hooks. Import from this file instead.
 */
export interface ProjectData {
  id: string
  name: string
  /** Serialized project content (BlockStorage JSON, etc.). */
  data: string
  tags: string[]
  metadata: ProjectMetadata
  syncStatus?: SyncStatus
  storageType: StorageType
  /** Computed dynamically based on local storage check. */
  isLocallyAvailable?: boolean
  preferences?: ProjectPreferences
}

/**
 * Lightweight metadata record persisted in the sync-optimization store.
 * Mirrors {@link ProjectData} minus the heavy `data` payload.
 */
export interface ProjectMetadataRecord {
  id: string
  name: string
  tags: string[]
  metadata: ProjectMetadata
  syncStatus?: SyncStatus
  storageType: StorageType
  preferences?: ProjectPreferences
}
