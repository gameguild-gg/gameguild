"use client"

/**
 * Read-only renderer for persisted `SerializedEditorState` content.
 * Thin wrapper around `<LexicalSurface />` in read-only mode.
 */

import { useMemo } from "react"
import type { SerializedEditorState } from "lexical"

import { LexicalSurface, stripSelection } from "@game-guild/lexical-surface"
import { lexicalSurfaceAdapters } from "@/components/block-content-editor/lexical-surface-adapters"

interface RichTextPreviewRendererProps {
  content: SerializedEditorState | null | undefined
  className?: string
}

export function RichTextPreviewRenderer({ content, className }: RichTextPreviewRendererProps) {
  const sanitizedContent = useMemo(() => stripSelection(content), [content])
  const mountKey = useMemo(
    () => (sanitizedContent ? Math.random().toString(36).slice(2) : "empty"),
    [sanitizedContent],
  )

  if (!content) return null

  return (
    <LexicalSurface
      namespace="RichTextPreview"
      mountKey={mountKey}
      readOnly
      initialState={sanitizedContent}
      contentClassName={className || "text-sm text-foreground"}
      adapters={lexicalSurfaceAdapters}
    />
  )
}
