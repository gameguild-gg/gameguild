/**
 * Block Storage
 *
 * Centralizes (de)serialization for the Block Array Engine. Replaces the
 * legacy layout-detector + cell-converters layer. End-to-end shape:
 *
 *   serializeProject(blocks)   →  string (JSON of BlockStorage)
 *   deserializeProject(data)   →  BlockArray
 *   blockToPreviewNode(block)  →  fake serialized Lexical node for preview components
 */

import type { Block, BlockArray, BlockCellType, BlockStorage, BlockStorageEntry } from "./block-structure"

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
// Preview adapter — Block → fake serialized Lexical node
//
// Preview components (plugins/preview-components/*) were originally written to
// consume Lexical-serialized nodes ({ type, data, version } or { type, entry,
// version } for quiz). We keep that shape here so preview components do not
// have to change. The aliases are intentionally inlined to avoid depending on
// any removed cell-converters module.
// ============================================================================

const BLOCK_TYPE_TO_PREVIEW_ALIAS: Record<BlockCellType, string> = {
  quiz: "quiz",
  code: "code-studio",
  img: "image",
  vid: "video",
  aud: "audio",
  gal: "gallery",
  yt: "youtube",
  spot: "spotify",
  mmd: "mermaid",
  vega: "vega-lite",
  pres: "presentation",
  src: "source",
  md: "markdown",
  html: "html",
  rt: "rich-text",
  hdr: "header",
  div: "divider",
  btn: "button",
  adm: "admonition",
  tbl: "table",
  proj: "project",
}

export function blockToPreviewNode(block: Block): any {
  const alias = BLOCK_TYPE_TO_PREVIEW_ALIAS[block.type]
  if (block.type === "quiz") {
    return { type: alias, entry: block.data, version: 1 }
  }
  return { type: alias, data: block.data, version: 1 }
}
