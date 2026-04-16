/**
 * Block Types
 *
 * Runtime model for the Block Array Engine.
 * Blocks are the engine's own structure — simple { type, data } objects.
 * Conversion to/from Cell (the storage format) happens only at persistence boundaries.
 */

import type { BlockCellType } from "./block-component-registry"

export interface Block<D = any> {
  type: BlockCellType
  data: D
}

export type BlockArray = Block[]
