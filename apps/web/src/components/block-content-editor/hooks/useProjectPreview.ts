"use client"

import { useState, useCallback } from "react"
import { toast } from "sonner"
import type { SerializedEditorState } from "lexical"
import { ENGINE_TYPES } from "@/components/block-content-editor/lib/storage/editor/project-types"
import { cellsToLexical } from "@/components/block-content-editor/lib/storage/editor/cell-converters/lexical"
import type { UseProjectStorageReturn } from "./useProjectStorage"

export interface UseProjectPreviewReturn {
  previewOpen: boolean
  setPreviewOpen: (open: boolean) => void
  previewState: SerializedEditorState | null
  openPreview(): void
}

export function useProjectPreview(project: UseProjectStorageReturn): UseProjectPreviewReturn {
  const [previewOpen, setPreviewOpen] = useState(false)
  const [previewState, setPreviewState] = useState<SerializedEditorState | null>(null)

  const openPreview = useCallback(() => {
    if (!project.projectId) {
      toast.error("No project loaded", {
        description: "Please load or create a project first", duration: 3000,
      })
      return
    }

    try {
      if (project.engine === ENGINE_TYPES.BLOCKS) {
        // Block Array engine — preview handled directly by BlockArrayViewer in the page
        setPreviewOpen(true)
        return
      }

      if (!project.editorState) {
        toast.error("No content", { description: "Editor is empty", duration: 3000 })
        return
      }
      const parsed = JSON.parse(project.editorState)
      const lexicalState = cellsToLexical(parsed)
      setPreviewState(lexicalState)
      setPreviewOpen(true)
    } catch (error) {
      console.error("Failed to parse editor state:", error)
      toast.error("Preview error", { description: "Failed to load preview", duration: 3000 })
    }
  }, [project.projectId, project.engine, project.editorState])

  return {
    previewOpen,
    setPreviewOpen,
    previewState,
    openPreview,
  }
}
