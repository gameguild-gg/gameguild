/**
 * Editor Configuration Types
 *
 * Two config types control what the editor can do:
 * - FieldConfig: what the editor field supports (allowed block types, modes)
 * - ToolbarConfig: what controls the header/toolbar shows
 *
 * Pages compose freely by passing different configs to EditorField/EditorToolbar.
 */

import type { ProjectMode } from "@/components/block-content-editor/lib/storage/editor/project-modes"
import type { BlockCellType } from "@/components/block-content-editor/lib/storage/editor/block-structure"

// ============================================================================
// Field Config — what the editor/viewer field supports
// ============================================================================

export interface FieldConfig {
  /** Which block types to show in the picker (undefined = all) */
  allowedBlockTypes?: BlockCellType[]
  /** Which content modes are available in create dialog (undefined = all) */
  allowedModes?: ProjectMode[]
  /** Default project mode */
  defaultMode?: ProjectMode
  /**
   * Mode "single block document". When true, the editor:
   *  - automatically creates a single block of the type defined in
   *    `allowedBlockTypes[0]` (or `"rich-text"`) when mounting with an empty list;
   *  - hides the insertion lines between blocks and the empty state with
   *    the "add" button;
   *  - hides the remove button in the block header;
   *  - hides the move arrows (there's no other block to move to).
   * Saving uses the standard project flow (only with a single block).
   */
  singleBlockMode?: boolean
}

export const DEFAULT_FIELD_CONFIG: FieldConfig = {
  defaultMode: "free-page",
}

// ============================================================================
// Toolbar Config — what the header/toolbar shows
// ============================================================================

export interface ToolbarConfig {
  showSave?: boolean
  showSaveAs?: boolean
  showOpen?: boolean
  showCreate?: boolean
  showPreview?: boolean
  showHistory?: boolean
  showAutoSave?: boolean
  showSizeIndicator?: boolean
  showSyncStatus?: boolean
  showProjectTitle?: boolean
  showModeIndicator?: boolean
  showStorageInfo?: boolean
  showNavHome?: boolean
  showNavViewer?: boolean
  showNavStudio?: boolean
}

export const DEFAULT_TOOLBAR_CONFIG: ToolbarConfig = {
  showSave: true,
  showSaveAs: true,
  showOpen: true,
  showCreate: true,
  showPreview: true,
  showHistory: true,
  showAutoSave: true,
  showSizeIndicator: true,
  showSyncStatus: true,
  showProjectTitle: true,
  showModeIndicator: true,
  showStorageInfo: true,
  showNavHome: true,
  showNavViewer: true,
  showNavStudio: true,
}

// ============================================================================
// Helpers
// ============================================================================

export function mergeFieldConfig(partial?: Partial<FieldConfig>): FieldConfig {
  if (!partial) return DEFAULT_FIELD_CONFIG
  return { ...DEFAULT_FIELD_CONFIG, ...partial }
}

export function mergeToolbarConfig(partial?: Partial<ToolbarConfig>): ToolbarConfig {
  if (!partial) return DEFAULT_TOOLBAR_CONFIG
  return { ...DEFAULT_TOOLBAR_CONFIG, ...partial }
}
