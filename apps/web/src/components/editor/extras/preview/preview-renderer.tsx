"use client"
import type { SerializedEditorState } from "lexical"

import { SerializedContentRenderer } from "./serialized-content-renderer"
import { PreviewHeader } from "./preview-header"

interface PreviewRendererProps {
  serializedState: SerializedEditorState
  showHeader?: boolean
  projectName?: string
  className?: string
}

export function PreviewRenderer({
  serializedState,
  showHeader = false,
  projectName,
  className = "prose prose-stone dark:prose-invert max-w-none",
}: PreviewRendererProps) {
  return (
    <div className={className}>
      {showHeader && projectName && <PreviewHeader projectName={projectName} />}
      <SerializedContentRenderer serializedState={serializedState} />
    </div>
  )
}
