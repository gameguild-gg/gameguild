/**
 * Editor Configuration Types
 *
 * Two config types control what the editor can do:
 * - FieldConfig: what the editor field supports (engines, layouts, node/block types)
 * - ToolbarConfig: what controls the header/toolbar shows
 *
 * Pages compose freely by passing different configs to EditorField/EditorToolbar.
 */

import type { EngineType } from "@/components/block-content-editor/lib/storage/editor/project-types"
import type { ProjectMode, NodeRestrictions } from "@/components/block-content-editor/lib/storage/editor/project-modes"
import type { BlockCellType } from "@/components/block-content-editor/lib/storage/editor/block-structure"

// ============================================================================
// Field Config — what the editor/viewer field supports
// ============================================================================

export interface FieldConfig {
  /** Which engines are available (default: both) */
  engines: EngineType[]
  /** For blocks engine: which block types to show in the picker (undefined = all) */
  allowedBlockTypes?: BlockCellType[]
  /** For lexical engine: which decorator node types are allowed (undefined = all) */
  allowedNodeTypes?: string[]
  /** Which content modes are available in create dialog (undefined = all) */
  allowedModes?: ProjectMode[]
  /** Default engine when creating a project */
  defaultEngine?: EngineType
  /** Default project mode */
  defaultMode?: ProjectMode
}

export const DEFAULT_FIELD_CONFIG: FieldConfig = {
  engines: ["lexical", "blocks"],
  defaultEngine: "lexical",
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

/**
 * Convert allowedNodeTypes from FieldConfig into NodeRestrictions format.
 * When allowedNodeTypes is set, creates restrictions that block all nodes
 * except the listed ones, applied uniformly to all blocks.
 */
export function configToRestrictions(config: FieldConfig): NodeRestrictions | undefined {
  if (!config.allowedNodeTypes) return undefined
  return {
    blocks: {
      b1: ["*", config.allowedNodeTypes],
    },
  }
}
