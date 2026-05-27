import type { SerializedBlockNode } from "./base/serialized-block-node"
import type { EmbeddableBlock } from "../embed/types"

/**
 * Markdown block data.
 *
 * Markdown is plain text — it has no inline rich-text editing surface.
 * To embed other block types (image, gallery, mermaid, html, …) the
 * source includes self-closing custom tokens:
 *
 *   <block-embed id="abc-123" />
 *
 * The matching block payload is stored side-by-side in `embeds`, keyed
 * by the same id. The shared `MarkdownRenderer` resolves each token at
 * render time via the embed registry. Embeds whose id no longer appears
 * in `content` are pruned on save.
 */
export interface MarkdownData {
  content: string
  embeds?: Record<string, EmbeddableBlock>
  title?: string
  caption?: string
}

export type SerializedMarkdownNode = SerializedBlockNode<"markdown", MarkdownData>
