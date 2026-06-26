"use client"

import { BlockArrayViewer } from "@/components/block-content-editor/engines/blocks/block-array-viewer"
import type { BlockArray } from "@/components/block-content-editor/lib/storage/editor/block-structure"

interface ProjectContentRendererProps {
  blocks: BlockArray
}

/**
 * Shared content renderer for both Viewer and StaticViewer pages.
 */
export function ProjectContentRenderer({ blocks }: ProjectContentRendererProps) {
  return (
    <div className="border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900 p-6">
      <BlockArrayViewer blocks={blocks} />
    </div>
  )
}
