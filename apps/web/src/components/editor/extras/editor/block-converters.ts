/**
 * Block ↔ Storage Converters
 *
 * Converts between Block (runtime model) and BlockStorage (persistence format).
 * This is the only file in the block engine that should import from cell-structure.
 */

import type { Cell } from "@/lib/storage/editor/cell-structure"
import type { DecoratorCellData } from "@/lib/storage/editor/cell-converters/cell-data"
import type { CellType } from "@/lib/storage/editor/cell-converters/cell-data"
import { createDecoratorMeta } from "@/lib/storage/editor/cell-converters/cell-metadata"
import type { Block, BlockStorage } from "./block-types"
import type { BlockCellType } from "./block-component-registry"

/**
 * Convert a BlockArray to BlockStorage for persistence.
 * Produces { order: [id1, id2, ...], blocks: { id1: Cell, id2: Cell, ... } }
 */
export function blocksToStorage(blocks: Block[]): BlockStorage {
  const order: string[] = []
  const blockMap: Record<string, Cell> = {}

  for (const block of blocks) {
    order.push(block.id)
    blockMap[block.id] = [
      { d: block.data } as DecoratorCellData<any>,
      createDecoratorMeta(block.type as CellType),
    ]
  }

  return { order, blocks: blockMap }
}

/**
 * Convert BlockStorage (from persistence) back to a BlockArray.
 * Reads order array and resolves each id from the blocks map.
 */
export function storageToBlocks(storage: BlockStorage): Block[] {
  if (!storage || !storage.order || !storage.blocks) return []

  return storage.order
    .filter((id) => id in storage.blocks)
    .map((id): Block => {
      const cell = storage.blocks[id]!
      return {
        id,
        type: cell[1].t as BlockCellType,
        data: (cell[0] as DecoratorCellData<any>).d,
      }
    })
}
