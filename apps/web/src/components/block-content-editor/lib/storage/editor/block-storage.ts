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

// ============================================================================
// Conversion: BlockArray ↔ BlockStorage
// ============================================================================

export function blocksToStorage(blocks: BlockArray): BlockStorage {
  const order: BlockStorage["order"] = []
  const map: Record<string, AnyBlockData> = {}
  for (const block of blocks) {
    order.push([block.id, block.type])
    map[block.id] = block.data
  }
  return { order, blocks: map }
}

export function storageToBlocks(storage: BlockStorage | null | undefined): BlockArray {
  if (!storage || !Array.isArray(storage.order) || !storage.blocks) return []
  const out: BlockArray = []
  for (const entry of storage.order) {
    if (!Array.isArray(entry) || entry.length < 2) continue
    const [id, type] = entry
    const data = storage.blocks[id]
    if (data === undefined) continue
    out.push({ id, type, data } as Block)
  }
  return out
}

// ============================================================================
// Project-level serialization
// ============================================================================

export const EMPTY_PROJECT_DATA: string = JSON.stringify({ order: [], blocks: {} } satisfies BlockStorage)

export function serializeProject(blocks: BlockArray): string {
  return JSON.stringify(blocksToStorage(blocks))
}

export function deserializeProject(data: string | null | undefined): BlockArray {
  if (!data) return []
  try {
    const parsed = JSON.parse(data) as BlockStorage
    return storageToBlocks(parsed)
  } catch (error) {
    console.error("Failed to deserialize project data:", error)
    return []
  }
}

// ============================================================================
// Preview adapter — Block → serialized node shape
//
// Preview components in plugins/preview-components/* expect either
//   { type, data, version }    (most blocks)
//   { type, entry, version }   (quiz — historical shape)
// This wrapper keeps that shape so preview components do not need to change.
// ============================================================================

export type PreviewNode<T extends BlockCellType = BlockCellType> =
  T extends "quiz"
    ? { type: "quiz"; entry: BlockDataMap["quiz"]; version: 1 }
    : { type: T; data: BlockDataMap[T]; version: 1 }

export function blockToPreviewNode<B extends Block>(block: B): PreviewNode<B["type"]> {
  if (block.type === "quiz") {
    return { type: "quiz", entry: block.data, version: 1 } as PreviewNode<B["type"]>
  }
  return { type: block.type, data: block.data, version: 1 } as PreviewNode<B["type"]>
}
