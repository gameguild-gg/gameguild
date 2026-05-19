import type { SerializedBlockNode } from "./base/serialized-block-node"

export interface MarkdownData {
  content: string
  title?: string
  caption?: string
}

export type SerializedMarkdownNode = SerializedBlockNode<"markdown", MarkdownData>
