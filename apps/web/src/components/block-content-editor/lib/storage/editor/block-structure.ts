/**
 * Block Structure
 *
 * Single source of truth for the block content editor's runtime + persistence
 * model. There is only one engine: the Block Array Engine. Blocks are
 * decorator units rendered top-to-bottom; there is no text in between.
 *
 *   Block         = runtime unit:        { id, type, data }
 *   BlockArray    = ordered list:        Block[]
 *   BlockStorage  = persistence format:  { order: [id, type][], blocks: Record<id, data> }
 *
 * Block IDs are sequential numeric strings ("1", "2", "3", ...) — assigned
 * by `nextBlockId(blocks)` and never recycled. Project IDs remain UUIDs.
 */

import type { AdmonitionData } from "../../../nodes/admonition-node"
import type { AudioData } from "../../../nodes/audio-node"
import type { ButtonData } from "../../../nodes/button-node"
import type { DividerData } from "../../../nodes/divider-node"
import type { GalleryData } from "../../../nodes/gallery-node"
import type { HeaderData } from "../../../nodes/header-node"
import type { HTMLData } from "../../../nodes/html-node"
import type { ImageData } from "../../../nodes/image-node"
import type { MarkdownData } from "../../../nodes/markdown-node"
import type { MermaidData } from "../../../nodes/mermaid-node"
import type { ProjectData as ProjectNodeData } from "../../../nodes/project-node"
import type { RichTextData } from "../../../nodes/rich-text-node"
import type { SourceData } from "../../../nodes/source-node"
import type { VegaLiteData } from "../../../nodes/vega-lite-node"
import type { VideoData } from "../../../nodes/video-node"
import type { CodeStudioData } from "../../../extras/code-studio/types"
import type { QuizEntry } from "../../../extras/quiz"

// ============================================================================
// Block types — the 21 decorator kinds supported by the engine
// ============================================================================

export const BLOCK_CELL_TYPES = [
  "quiz",
  "code-studio",
  "image",
  "video",
  "audio",
  "gallery",
  "mermaid",
  "vega-lite",
  "presentation",
  "source",
  "markdown",
  "html",
  "rich-text",
  "header",
  "divider",
  "button",
  "admonition",
  "project",
] as const

export type BlockCellType = (typeof BLOCK_CELL_TYPES)[number]

// ============================================================================
// BlockDataMap — type-level mapping from BlockCellType to its data shape
// ============================================================================

export interface BlockDataMap {
  "quiz": QuizEntry
  "code-studio": CodeStudioData
  "image": ImageData
  "video": VideoData
  "audio": AudioData
  "gallery": GalleryData
  "mermaid": MermaidData
  "vega-lite": VegaLiteData
  "presentation": unknown
  "source": SourceData
  "markdown": MarkdownData
  "html": HTMLData
  "rich-text": RichTextData
  "header": HeaderData
  "divider": DividerData
  "button": ButtonData
  "admonition": AdmonitionData
  "project": ProjectNodeData
}

// ============================================================================
// Block — runtime unit. Generic in T allows narrowing via discriminant `type`.
// ============================================================================

type BlockMap = {
  [K in BlockCellType]: { id: string; type: K; data: BlockDataMap[K] }
}

export type Block<T extends BlockCellType = BlockCellType> = BlockMap[T]

export type BlockArray = Block[]

// ============================================================================
// BlockStorage — persistence format
//
// `order` pairs each block id with its type, so `blocks` only needs to hold
// the raw data payload (no `{type, data}` envelope).
// ============================================================================

/** `[id, type]` pair — one per block, in render order. */
export type BlockOrderEntry<T extends BlockCellType = BlockCellType> = readonly [id: string, type: T]

/** Union of every possible block data payload (one per known block type). */
export type AnyBlockData = BlockDataMap[BlockCellType]

export interface BlockStorage {
  order: BlockOrderEntry[]
  blocks: Record<string, AnyBlockData>
}

// ============================================================================
// ID helpers — block ids are sequential numeric strings: "1", "2", "3", …
// ============================================================================

/**
 * Compute the next sequential block id for the given array. Walks every
 * block to find the highest numeric id and returns `String(max + 1)`. Ids
 * are never recycled, so this stays stable across deletions.
 */
export function nextBlockId(blocks: BlockArray): string {
  let max = 0
  for (const b of blocks) {
    const n = Number(b.id)
    if (Number.isFinite(n) && n > max) max = n
  }
  return String(max + 1)
}
