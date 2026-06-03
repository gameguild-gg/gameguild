/**
 * Surface-agnostic helpers for embeddable blocks.
 *
 * Used by the Lexical surface today and the future Markdown surface. Must
 * not import from `lexical`, `@lexical/*` or anything under `../nodes/` /
 * `../plugins/`.
 */

import { BLOCK_REGISTRY } from "../engines/blocks/block-component-registry"
import type {
  Block,
} from "../lib/storage/editor/block-structure"
import {
  EMBEDDABLE_BLOCK_TYPES,
  isEmbeddableBlockType,
  type EmbeddableBlock,
  type EmbeddableBlockType,
} from "./types"

export { EMBEDDABLE_BLOCK_TYPES, isEmbeddableBlockType }
export type { EmbeddableBlock, EmbeddableBlockType }

/**
 * Build a fresh `Block` envelope of an embeddable type, reusing the
 * canonical `createEmpty()` defined in `BLOCK_REGISTRY` so embed creation
 * and top-level creation produce identical defaults.
 *
 * Embedded blocks live inside a Lexical surface and are not part of the
 * project's top-level `BlockStorage`, so their ids are scoped to the
 * surface they're inserted into — a millisecond timestamp is unique
 * enough for that purpose.
 */
export function createEmbeddableBlock<T extends EmbeddableBlockType>(type: T, id?: string): Block<T> {
  const config = BLOCK_REGISTRY[type]
  return config.createEmpty(id ?? String(Date.now())) as Block<T>
}
