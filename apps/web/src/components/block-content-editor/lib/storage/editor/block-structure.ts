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

// todo: probably an enum should be better here, so we can have both integere and alias interchangeables
export const BLOCK_CELL_TYPES = [
  // todo: every block type should have a better namming convention, e.g. "code" => "code-editor", "img" => "image", "vid" => "video", etc.
  "quiz", "code", "img", "vid", "aud", "gal", "yt", "spot",
  "mmd", "vega", "pres", "src", "md", "html", "rt", "hdr", "div",
  "btn", "adm", "tbl", "proj",
] as const

export type BlockCellType = typeof BLOCK_CELL_TYPES[number]

// ============================================================================
// Block — runtime model
// ============================================================================

// todo: D cannot be of type any
// todo: if the type D is specified, the data should be validated against the expected structure for that type, e.g. if type is "code", data should have ex.: { language: string, code: string.. ??? }
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
export interface BlockStorage<T extends string | number = string> {
  // the id could be integers instead of strings, but it doenst matter much, as long they are unique and consistent
  order: T[]
  blocks: Record<T, Cell>
}
