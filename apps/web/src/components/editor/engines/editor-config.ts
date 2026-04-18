/**
 * Editor Configuration Types
 *
 * Two config types control what the editor can do:
 * - FieldConfig: what the editor field supports (engines, layouts, node/block types)
 * - ToolbarConfig: what controls the header/toolbar shows
 *
 * Pages compose freely by passing different configs to EditorField/EditorToolbar.
 */

import type { EngineType, ProjectType } from "@/lib/storage/editor/project-types"
import type { ProjectMode, NodeRestrictions } from "@/lib/storage/editor/project-modes"
import type { BlockCellType } from "@/lib/storage/editor/block-structure"

// ============================================================================
// Field Config — what the editor/viewer field supports
// ============================================================================

export interface FieldConfig {
  /** Which engines are available (default: both) */
  engines: EngineType[]
  /** Which layout types are available (default: all three) */
  layouts: ProjectType[]
  /** For blocks engine: which block types to show in the picker (undefined = all) */
  allowedBlockTypes?: BlockCellType[]
  /** For lexical engine: which decorator node types are allowed (undefined = all) */
  allowedNodeTypes?: string[]
  /** Which content modes are available in create dialog (undefined = all) */
  allowedModes?: ProjectMode[]
  /** Default engine when creating a project */
  defaultEngine?: EngineType
  /** Default layout when creating a project */
  defaultLayout?: ProjectType
  /** Default project mode */
  defaultMode?: ProjectMode
}

export const DEFAULT_FIELD_CONFIG: FieldConfig = {
  engines: ["lexical", "blocks"],
  layouts: ["type1", "type2", "type3"],
  defaultEngine: "lexical",
  defaultLayout: "type1",
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
  showPreviewModeSelector?: boolean
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
  showPreviewModeSelector: true,
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
      b2: ["*", config.allowedNodeTypes],
    },
  }
}
