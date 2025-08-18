"use client"

import type { SerializedMarkdownNode } from "../../nodes/markdown-node"
import ReactMarkdown from "react-markdown"

export function PreviewMarkdown({ node }: { node: SerializedMarkdownNode }) {
  if (!node?.data) {
    console.error("Invalid markdown node structure:", node)
    return null
  }

  return (
    <div className="my-4">
      <div className="prose prose-stone dark:prose-invert max-w-none">
        <ReactMarkdown>{node.data.content}</ReactMarkdown>
      </div>
    </div>
  )
}
