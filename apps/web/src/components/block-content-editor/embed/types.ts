/**
 * Surface-agnostic embed types.
 *
 * This module is consumed by both the Lexical surface (today) and the
 * Markdown surface (future). It MUST NOT import from `lexical`, `@lexical/*`
 * or anything under `../nodes/` or `../plugins/`.
 */

import type { ComponentType } from "react"
import type {
  Block,
  BlockCellType,
  BlockDataMap,
} from "../lib/storage/editor/block-structure"

/**
 * Block types allowed inside an embed context (Lexical rich-text, Lexical
 * essay, future Markdown). Single source of truth — extend this tuple to
 * surface a new type in every embed UI at once.
 */
export const EMBEDDABLE_BLOCK_TYPES = [
  "image",
  "video",
  "audio",
  "gallery",
  "code-studio",
  "mermaid",
  "vega-lite",
  "admonition",
  "divider",
  "button",
  "html",
  "markdown",
] as const

export type EmbeddableBlockType = (typeof EMBEDDABLE_BLOCK_TYPES)[number]

export type EmbeddableBlock = Block<EmbeddableBlockType>

export type EmbeddableBlockData<T extends EmbeddableBlockType = EmbeddableBlockType> =
  BlockDataMap[T]

/**
 * Props all preview adapters receive. Each preview adapter is responsible
 * for translating from a `Block` envelope to whatever shape its underlying
 * `preview-*.tsx` component expects.
 */
export interface EmbedPreviewProps<T extends EmbeddableBlockType = EmbeddableBlockType> {
  block: Block<T>
}

/**
 * Per-type configuration consumed by `<BlockEmbedView>`.
 */
export interface EmbeddableBlockEntry<T extends EmbeddableBlockType = EmbeddableBlockType> {
  /** React component that renders the read-only preview for this type. */
  Preview: ComponentType<EmbedPreviewProps<T>>
}

export type EmbeddableBlockConfig = {
  [T in EmbeddableBlockType]: EmbeddableBlockEntry<T>
}

/**
 * Type guard: is this `BlockCellType` allowed as an embed?
 */
export function isEmbeddableBlockType(t: BlockCellType): t is EmbeddableBlockType {
  return (EMBEDDABLE_BLOCK_TYPES as readonly BlockCellType[]).includes(t)
}
