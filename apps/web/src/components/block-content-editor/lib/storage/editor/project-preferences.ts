import type { ProjectType } from "./project-types"
import type { BlockCellType } from "./block-structure"

/**
 * Project-level preferences stored with each project. Carries the project's
 * identity (type) and structural constraints (single block? which block types?)
 * so any editor page that opens the project respects them.
 */
export interface ProjectPreferences {
  global: {
    /** High-level kind of the project (document/quiz/general). */
    projectType?: ProjectType
    /** When true, the project is a single-block document. */
    singleBlockMode?: boolean
    /** When set, only these block types can be inserted. */
    allowedBlockTypes?: BlockCellType[]
  }
}
