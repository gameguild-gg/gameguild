"use client"

import type { SerializedMarkdownNode } from "../../nodes/markdown-node"
import ReactMarkdown from "react-markdown"
import remarkGfm from "remark-gfm"
import rehypeRaw from "rehype-raw"
import { useMarkdownComponents } from "../../extras/markdown/markdown-components"

export function PreviewMarkdown({ node }: { node: SerializedMarkdownNode }) {
  const markdownComponents = useMarkdownComponents()

  if (!node?.data) {
    console.error("Invalid markdown node structure:", node)
    return null
  }

  const { data } = node

  return (
    <div className="my-4">
      <div className="">
        {data.title && (
          <h1 className="text-3xl font-bold mb-2 text-gray-900 dark:text-gray-100">{data.title}</h1>
        )}
        {data.caption && (
          <p className="text-sm text-gray-600 dark:text-gray-400 mb-6">{data.caption}</p>
        )}
        
        {data.content ? (
          <ReactMarkdown 
            remarkPlugins={[remarkGfm]}
            rehypePlugins={[rehypeRaw]}
            components={markdownComponents}
          >
            {data.content}
          </ReactMarkdown>
        ) : (
          <p className="text-gray-400 dark:text-gray-600 italic">
            No markdown content
          </p>
        )}
      </div>
    </div>
  )
}
