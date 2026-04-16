/**
 * Block ↔ Cell Converters
 *
 * Converts between Block (runtime model) and Cell (storage/persistence format).
 * This is the only file in the block engine that should import from cell-structure.
 */

import type { Cell, CellularContent } from "@/lib/storage/editor/cell-structure"
import type { DecoratorCellData } from "@/lib/storage/editor/cell-converters/cell-data"
import type { CellType } from "@/lib/storage/editor/cell-converters/cell-data"
import { createDecoratorMeta } from "@/lib/storage/editor/cell-converters/cell-metadata"
import type { Block, BlockArray } from "./block-types"
import type { BlockCellType } from "./block-component-registry"

/**
 * Convert a BlockArray to CellularContent for persistence.
 * Each Block { type, data } becomes a Cell tuple [{ d: data }, { t: type, v: 1 }].
 */
export function blocksToCells(blocks: BlockArray): CellularContent {
  return blocks.map((block): Cell => [
    { d: block.data } as DecoratorCellData<any>,
    createDecoratorMeta(block.type as CellType),
  ])
}

/**
 * Convert CellularContent (from storage) back to a BlockArray.
 * Each Cell tuple [{ d: data }, { t: type, v: ... }] becomes Block { type, data }.
 */
export function cellsToBlocks(cells: CellularContent): BlockArray {
  return cells.map((cell): Block => ({
    type: cell[1].t as BlockCellType,
    data: (cell[0] as DecoratorCellData<any>).d,
  }))
}
