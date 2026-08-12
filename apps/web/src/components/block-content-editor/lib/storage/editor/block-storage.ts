/**
 * Block Storage
 *
 * Centralizes (de)serialization for the Block Array Engine. End-to-end shape:
 *
 *   serializeProject(blocks)   →  string (JSON of BlockStorage)
 *   deserializeProject(data)   →  BlockArray
 *   blockToPreviewNode(block)  →  fake serialized node shape consumed by
 *                                 preview components in plugins/preview-components/*
 *
 * The persisted shape is:
 *   {
 *     order: [["1", "rich-text"], ["2", "image"], …],
 *     blocks: { "1": <rich-text data>, "2": <image data>, … }
 *   }
 */

import type {
  AnyBlockData,
  Block,
  BlockArray,
  BlockCellType,
  BlockDataMap,
  BlockStorage,
} from "./block-structure"
import {
  EMPTY_BLOCK_STORAGE,
  blockToView,
  blocksToStorage as blocksToBlockListStorage,
  deserializeBlockList,
  serializeBlockList,
  storageToBlocks as storageToBlockList,
  type BlockView,
} from "@game-guild/block-list"

// ============================================================================
// Conversion: BlockArray ↔ BlockStorage
// ============================================================================

export function blocksToStorage(blocks: BlockArray): BlockStorage {
  return blocksToBlockListStorage(blocks) as BlockStorage
}

export function storageToBlocks(storage: BlockStorage | null | undefined): BlockArray {
  return storageToBlockList<BlockCellType, AnyBlockData, Block>(storage)
}

// ============================================================================
// Project-level serialization
// ============================================================================

export const EMPTY_PROJECT_DATA: string = JSON.stringify(EMPTY_BLOCK_STORAGE)

export function serializeProject(blocks: BlockArray): string {
  return serializeBlockList(blocks)
}

export function deserializeProject(data: string | null | undefined): BlockArray {
  return deserializeBlockList<Block>(data)
}

// ============================================================================
// Preview adapter — Block → serialized node shape
//
// Preview components in plugins/preview-components/* consume the generic
// `{ id, type, data, version }` read model from @game-guild/block-list.
// ============================================================================

export type PreviewNode<T extends BlockCellType = BlockCellType> = BlockView<
  T,
  BlockDataMap[T]
>

export function blockToPreviewNode<B extends Block>(block: B): PreviewNode<B["type"]> {
  return blockToView(block) as PreviewNode<B["type"]>
}
