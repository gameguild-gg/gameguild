/**
 * Canonical Interop (Import/Export) Types.
 *
 * The exported ZIP/folder format uses a *flat* metadata shape (size/createdAt
 * at top level), distinct from the storage-side `ProjectData` (nested under
 * `metadata`). Centralizing those types here prevents drift between the
 * exporter and importer.
 */

import type { StorageType } from "../storage/editor/storage-types"
import type { ProjectPreferences } from "../storage/editor/project-preferences"

/**
 * Flat-shape input accepted by the exporter and produced by the importer.
 * Callers assembling this from a storage `ProjectData` must flatten
 * `metadata.size/createdAt/updatedAt` themselves.
 */
export interface ProjectExportInput {
  id: string
  name: string
  data: string
  tags: string[]
  size: number
  createdAt: string
  updatedAt: string
  hash?: string
  storageType?: StorageType
  isLocallyAvailable?: boolean
  preferences?: ProjectPreferences
}

/**
 * Metadata block written to `index.json` inside the exported folder/ZIP.
 * Superset of {@link ProjectExportInput} fields plus export bookkeeping.
 */
export interface ProjectExportMetadata {
  id: string
  name: string
  tags: string[]
  size: number
  hash: string
  createdAt: string
  updatedAt: string
  storageType: string
  version: string
  exportedAt?: string
  assetsCount?: number
  preferences?: ProjectPreferences
}
