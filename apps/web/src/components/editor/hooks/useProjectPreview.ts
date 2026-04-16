"use client"

import { useState, useCallback } from "react"
import { toast } from "sonner"
import type { SerializedEditorState } from "lexical"
import type { InternalLayout } from "@/lib/storage/editor/project-types"
import { ENGINE_TYPES } from "@/lib/storage/editor/project-types"
import type { SlideshowStructure, PreviewMode } from "@/lib/storage/editor/slideshow-structure"
import { cellsToLexical } from "@/lib/storage/editor/cell-converters/lexical"
import type { UseProjectStorageReturn } from "./useProjectStorage"

export interface UseProjectPreviewReturn {
  previewOpen: boolean
  setPreviewOpen: (open: boolean) => void
  previewState: SerializedEditorState | null
  previewBlockStates: Record<string, SerializedEditorState>
  previewLayout: InternalLayout
  previewSlideshowStructure: SlideshowStructure | null
  previewSlideshowMode: PreviewMode
  openPreview(): void
}

export function useProjectPreview(project: UseProjectStorageReturn): UseProjectPreviewReturn {
  const [previewOpen, setPreviewOpen] = useState(false)
  const [previewState, setPreviewState] = useState<SerializedEditorState | null>(null)
  const [previewBlockStates, setPreviewBlockStates] = useState<Record<string, SerializedEditorState>>({})
  const [previewLayout, setPreviewLayout] = useState<InternalLayout>("single")
  const [previewSlideshowStructure, setPreviewSlideshowStructure] = useState<SlideshowStructure | null>(null)
  const [previewSlideshowMode, setPreviewSlideshowMode] = useState<PreviewMode>("continuous")

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
        setPreviewLayout("single")
        setPreviewOpen(true)
        return
      }

      if (project.layout === "slideshow") {
        if (!project.slideshowStructure || project.slideshowStructure.slides.length === 0) {
          toast.error("No content", { description: "Slideshow structure is empty", duration: 3000 })
          return
        }
        setPreviewSlideshowStructure(project.slideshowStructure)
        setPreviewSlideshowMode(project.previewMode)
        setPreviewLayout("slideshow")
        setPreviewOpen(true)

      } else if (project.layout === "single") {
        if (!project.editorState) {
          toast.error("No content", { description: "Editor is empty", duration: 3000 })
          return
        }
        const parsed = JSON.parse(project.editorState)
        const lexicalState = cellsToLexical(parsed)
        setPreviewState(lexicalState)
        setPreviewLayout("single")
        setPreviewOpen(true)

      } else {
        // Multiple layout
        if (Object.keys(project.blockStates).length < 1) {
          toast.error("No content", { description: "Need at least 1 block for preview", duration: 3000 })
          return
        }
        const parsedStates: Record<string, SerializedEditorState> = {}
        for (const [blockId, state] of Object.entries(project.blockStates)) {
          if (state) {
            try {
              const cellsData = JSON.parse(state)
              parsedStates[blockId] = cellsToLexical(cellsData)
            } catch (error) {
              console.error(`Failed to parse block ${blockId}:`, error)
            }
          }
        }
        if (Object.keys(parsedStates).length < 1) {
          toast.error("Invalid content", { description: "At least 1 block must have valid content", duration: 3000 })
          return
        }
        setPreviewBlockStates(parsedStates)
        setPreviewLayout("multiple")
        setPreviewOpen(true)
      }
    } catch (error) {
      console.error("Failed to parse editor state:", error)
      toast.error("Preview error", { description: "Failed to load preview", duration: 3000 })
    }
  }, [project.projectId, project.engine, project.layout, project.editorState, project.blockStates, project.slideshowStructure, project.previewMode])

  return {
    previewOpen,
    setPreviewOpen,
    previewState,
    previewBlockStates,
    previewLayout,
    previewSlideshowStructure,
    previewSlideshowMode,
    openPreview,
  }
}
