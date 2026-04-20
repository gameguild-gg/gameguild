/**
 * Block Structure
 *
 * Defines the Block runtime model and BlockStorage persistence format
 * for the Block Array Engine. Analogous to cell-structure.ts for Lexical.
 *
 * Block = runtime unit: { id, type, data }
 * BlockStorage = persistence format: { order: [ids], blocks: { id: Cell } }
 */

import type { Cell } from "./cell-structure"

// ============================================================================
// Block Cell Types — decorator types supported by the Block Array Engine
// ============================================================================

export const BLOCK_CELL_TYPES = [
  "quiz", "code", "img", "vid", "aud", "gal", "yt", "spot",
  "mmd", "vega", "pres", "src", "md", "html", "rt", "hdr", "div",
  "btn", "adm", "tbl", "proj",
] as const

export type BlockCellType = typeof BLOCK_CELL_TYPES[number]

// ============================================================================
// Block — runtime model
// ============================================================================

export interface Block<D = any> {
  id: string
  type: BlockCellType
  data: D
}

export type BlockArray = Block[]

// ============================================================================
// BlockStorage — persistence format
// ============================================================================

/** Persistence format: order array + blocks map keyed by id */
export interface BlockStorage {
  order: string[]
  blocks: Record<string, Cell>
}
