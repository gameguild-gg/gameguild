/**
 * Block Storage
 *
 * Centralizes (de)serialization for the Block Array Engine. End-to-end shape:
 *
 *   serializeProject(blocks)   →  string (JSON of BlockStorage)
 *   deserializeProject(data)   →  BlockArray
 *   blockToPreviewNode(block)  →  fake serialized node shape consumed by
 *                                 preview components in plugins/preview-components/*
 */

import type { Block, BlockArray, BlockStorage, BlockStorageEntry } from "./block-structure"

// ============================================================================
// Conversion: BlockArray ↔ BlockStorage
// ============================================================================

export function blocksToStorage(blocks: BlockArray): BlockStorage {
  const order: string[] = []
  const map: Record<string, BlockStorageEntry> = {}
  for (const block of blocks) {
    order.push(block.id)
    map[block.id] = { type: block.type, data: block.data } as BlockStorageEntry
  }
  return { order, blocks: map }
}

export function storageToBlocks(storage: BlockStorage | null | undefined): BlockArray {
  if (!storage || !Array.isArray(storage.order) || !storage.blocks) return []
  const out: BlockArray = []
  for (const id of storage.order) {
    const entry = storage.blocks[id]
    if (!entry) continue
    out.push({ id, type: entry.type, data: entry.data } as Block)
  }
  return out
}

// ============================================================================
// Project-level serialization
// ============================================================================

export const EMPTY_PROJECT_DATA: string = JSON.stringify({ order: [], blocks: {} })

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

export function blockToPreviewNode(block: Block): any {
  if (block.type === "quiz") {
    return { type: "quiz", entry: block.data, version: 1 }
  }
  return { type: block.type, data: block.data, version: 1 }
}
