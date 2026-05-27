"use client"

import type { SerializedMarkdownNode } from "../../nodes/markdown-node"
import { MarkdownRenderer } from "../../extras/markdown/markdown-renderer"

export function PreviewMarkdown({ node }: { node: SerializedMarkdownNode }) {
  if (!node?.data) {
    console.error("Invalid markdown node structure:", node)
    return null
  }

  const { content, embeds, title, caption } = node.data

  return (
    <MarkdownRenderer
      content={content}
      embeds={embeds}
      title={title}
      caption={caption}
    />
  )
}
