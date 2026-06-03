/**
 * Project Type System
 *
 * Identifies the high-level kind of a project. Stored in `ProjectPreferences.global.projectType`
 * so the editor structure (allowed blocks, single block mode, etc.) follows the project
 * across pages — instead of being dictated only by the page that opens it.
 *
 * - "document" : single-block rich-text document (created by doc-editor)
 * - "quiz"     : quiz-focused project (created by quiz-editor)
 * - "general"  : generalist project, no structural constraints (full/block editors)
 */

import type { ProjectMode } from "./project-modes"
import type { BlockCellType } from "./block-structure"

export type ProjectType = "document" | "quiz" | "general"

export const DEFAULT_PROJECT_TYPE: ProjectType = "general"

export const PROJECT_TYPE_LABELS: Record<ProjectType, string> = {
  document: "Document",
  quiz: "Quiz",
  general: "General",
}

export function getProjectTypeLabel(type: ProjectType | undefined): string {
  return PROJECT_TYPE_LABELS[type ?? DEFAULT_PROJECT_TYPE]
}

/**
 * Map a project type to its corresponding `ProjectMode` used by the editor
 * restrictions system. The mode is fully derived from the project type — no
 * user-pickable "mode" option exists anymore.
 */
export function projectTypeToMode(type: ProjectType | undefined): ProjectMode {
  switch (type) {
    case "quiz":
      return "quiz-page"
    case "document":
    case "general":
    default:
      return "free-page"
  }
}

/**
 * Default structural rules (singleBlockMode/allowedBlockTypes) implied by a
 * project type. Used when a project is created from a page that doesn't pin
 * these rules itself (e.g. a "general" page where the user picks the type).
 */
export interface ProjectTypeStructure {
  singleBlockMode?: boolean
  allowedBlockTypes?: BlockCellType[]
}

export function getProjectTypeStructure(type: ProjectType): ProjectTypeStructure {
  switch (type) {
    case "document":
      return { singleBlockMode: true, allowedBlockTypes: ["rich-text"] }
    case "quiz":
      return { allowedBlockTypes: [] }
    case "general":
    default:
      return {}
  }
}
