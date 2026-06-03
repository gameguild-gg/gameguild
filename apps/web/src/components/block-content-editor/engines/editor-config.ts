/**
 * Editor Configuration Types
 *
 * Two config types control what the editor can do:
 * - FieldConfig: what the editor field supports (allowed block types,
 *   project type, etc.)
 * - ToolbarConfig: what controls the header/toolbar shows
 *
 * Pages compose freely by passing different configs to EditorField/EditorToolbar.
 */

import type { BlockCellType } from "@/components/block-content-editor/lib/storage/editor/block-structure"
import type { ProjectType } from "@/components/block-content-editor/lib/storage/editor/project-types"
import type { ProjectPreferences } from "@/components/block-content-editor/lib/storage/editor/project-preferences"

// ============================================================================
// Field Config — what the editor/viewer field supports
// ============================================================================

export interface FieldConfig {
  /** Which block types to show in the picker (undefined = all) */
  allowedBlockTypes?: BlockCellType[]
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
  /**
   * Project type this page creates when a new project is created from here.
   * Stored in the project's preferences and shown on the project card.
   * Default: "general".
   */
  projectType?: ProjectType
  /**
   * Restrict which project types this page can open. When set, the open dialog
   * only lists projects whose type matches, and loading a project via URL hash
   * with a non-matching type is rejected. Undefined = accepts all types.
   */
  allowedProjectTypes?: ProjectType[]
}

export const DEFAULT_FIELD_CONFIG: FieldConfig = {}

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
  showTypeIndicator?: boolean
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
  showTypeIndicator: true,
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

/**
 * Apply the structural constraints captured in a project's preferences on top
 * of the page's FieldConfig. When a project is loaded, the project's own
 * structural rules (singleBlockMode/allowedBlockTypes/projectType) take
 * precedence over whatever the page declares — so the same project always
 * behaves consistently regardless of which page opens it.
 *
 * The page-level `allowedProjectTypes` is NOT overridden: it belongs to the
 * page's filtering contract.
 */
export function applyProjectPreferencesToFieldConfig(
  base: FieldConfig,
  preferences: ProjectPreferences | undefined,
): FieldConfig {
  const g = preferences?.global
  if (!g) return base
  const out: FieldConfig = { ...base }
  if (g.singleBlockMode !== undefined) out.singleBlockMode = g.singleBlockMode
  if (g.allowedBlockTypes !== undefined) out.allowedBlockTypes = g.allowedBlockTypes
  if (g.projectType !== undefined) out.projectType = g.projectType
  return out
}

export function mergeToolbarConfig(partial?: Partial<ToolbarConfig>): ToolbarConfig {
  if (!partial) return DEFAULT_TOOLBAR_CONFIG
  return { ...DEFAULT_TOOLBAR_CONFIG, ...partial }
}
