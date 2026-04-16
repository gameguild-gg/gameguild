/**
 * Block Converter
 *
 * Bidirectional conversion between Block (runtime) and BlockStorage (persistence).
 * Also provides blockToSerializedNode() for preview rendering.
 *
 * Follows the same pattern as lexical.ts for the Lexical converter.
 */

import type { Cell } from "../cell-structure"
import type { DecoratorCellData } from "./cell-data"
import type { CellType } from "./cell-data"
import { createDecoratorMeta } from "./cell-metadata"
import { CELL_TO_LEXICAL_TYPE } from "./cell-data"
import type { Block, BlockCellType, BlockStorage } from "../block-structure"

// ============================================================================
// Block[] → BlockStorage (for persistence)
// ============================================================================

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

// ============================================================================
// BlockStorage → Block[] (from persistence)
// ============================================================================

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

// ============================================================================
// Block → Serialized Lexical Node (for preview rendering)
// ============================================================================

/**
 * Convert a Block to a fake serialized Lexical node
 * that the preview components can consume.
 *
 * Most decorators use `{ type, data, version }`.
 * Quiz is the exception: it uses `entry` instead of `data`.
 */
export function blockToSerializedNode(block: Block): any {
  const lexicalType = CELL_TO_LEXICAL_TYPE[block.type as keyof typeof CELL_TO_LEXICAL_TYPE]

  if (block.type === "quiz") {
    return { type: lexicalType, entry: block.data, version: 1 }
  }

  return { type: lexicalType, data: block.data, version: 1 }
}
