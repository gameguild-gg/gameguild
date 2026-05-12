"use client"

import type { SerializedRichTextNode } from "@/components/block-content-editor/nodes/rich-text-node"
import { RichTextPreviewRenderer } from "@/components/block-content-editor/extras/rich-text/rich-text-preview-renderer"
import { FileText } from "lucide-react"

interface PreviewRichTextProps {
  node: SerializedRichTextNode
}

export function PreviewRichText({ node }: PreviewRichTextProps) {
  const { data } = node

  if (!data?.content) {
    return (
      <div className="p-4 text-center text-sm text-muted-foreground italic border border-dashed border-gray-300 dark:border-gray-600 rounded">
        Empty rich text block
      </div>
    )
  }

  return (
    <div className="my-2">
      {data.title && (
        <div className="flex items-center gap-2 mb-1 text-xs text-muted-foreground">
          <FileText className="h-3.5 w-3.5" />
          <span className="font-medium">{data.title}</span>
        </div>
      )}
      <RichTextPreviewRenderer content={data.content} />
    </div>
  )
}
