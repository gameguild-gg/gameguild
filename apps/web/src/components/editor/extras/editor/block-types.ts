/**
 * Block Types
 *
 * Runtime model for the Block Array Engine.
 * Blocks are the engine's own structure — simple { id, type, data } objects.
 * Conversion to/from storage happens only at persistence boundaries.
 */

import type { Cell } from "@/lib/storage/editor/cell-structure"
import type { BlockCellType } from "./block-component-registry"

export interface Block<D = any> {
  id: string
  type: BlockCellType
  data: D
}

export type BlockArray = Block[]

/** Persistence format: order array + blocks map keyed by id */
export interface BlockStorage {
  order: string[]
  blocks: Record<string, Cell>
}
