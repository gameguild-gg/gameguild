"use client"

import { useState, useCallback } from "react"
import { toast } from "sonner"
import { detectProjectLayout, extractEditorStates, createProjectData } from "@/lib/storage/editor/layout-detector"
import { serializeSlideshowStructure } from "@/lib/storage/editor/slideshow-structure"
import { cellsToLexical } from "@/lib/storage/editor/cell-converters/lexical"
import type { ProjectData as StorageProjectData } from "@/lib/storage/editor/enhanced-storage-adapter"
import type { UseProjectStorageReturn } from "./useProjectStorage"

export interface UseProjectHistoryReturn {
  isViewingHistory: boolean
  currentViewingSha: string | null
  loadCommit(sha: string): Promise<void>
  loadSnapshot(tag: string): Promise<void>
  returnToHead(): Promise<void>
}

export function useProjectHistory(project: UseProjectStorageReturn): UseProjectHistoryReturn {
  const [isViewingHistory, setIsViewingHistory] = useState(false)
  const [currentViewingSha, setCurrentViewingSha] = useState<string | null>(null)
  const [headProjectData, setHeadProjectData] = useState<string | null>(null)
  const [headSlideshowDeps, setHeadSlideshowDeps] = useState<StorageProjectData[]>([])

  // ── Helper: restore states from serialized data ──

  const restoreStates = useCallback((data: string, deps?: StorageProjectData[]) => {
    const layoutInfo = detectProjectLayout(data)
    const states = extractEditorStates(data, project.projectType)

    if (layoutInfo.hasSlides && layoutInfo.slideshowData) {
      project.setSlideshowStructure(layoutInfo.slideshowData)
      if (deps) project.setSlideshowDeps(deps)
      project.setCurrentSlideIndex(0)
    } else if (project.layout === "single" && states.blocks.b1) {
      project.setEditorState(JSON.stringify(states.blocks.b1))
      if (project.editorRef.current) {
        const lexicalState = cellsToLexical(states.blocks.b1)
        const parsed = project.editorRef.current.parseEditorState(JSON.stringify(lexicalState))
        project.editorRef.current.setEditorState(parsed)
      }
    } else if (project.layout === "multiple" && states.blocks) {
      const newBlockStates: Record<string, string> = {}
      Object.entries(states.blocks).forEach(([blockId, blockState]: [string, any]) => {
        if (blockState) newBlockStates[blockId] = JSON.stringify(blockState)
      })
      project.setBlockStates(newBlockStates)
    }
  }, [project.projectType, project.layout, project.editorRef, project.setSlideshowStructure, project.setSlideshowDeps, project.setCurrentSlideIndex, project.setEditorState, project.setBlockStates])

  // ── Helper: serialize current state for HEAD preservation ──

  const serializeCurrentState = useCallback((): string => {
    if (project.layout === "slideshow" && project.slideshowStructure) {
      return serializeSlideshowStructure(project.slideshowStructure)
    }
    const blocks: Record<string, any> = {}
    if (project.layout === "single") {
      blocks.b1 = project.editorState ? JSON.parse(project.editorState) : null
    } else {
      Object.entries(project.blockStates).forEach(([blockId, state]) => {
        blocks[blockId] = state ? JSON.parse(state) : null
      })
    }
    return createProjectData(project.projectType, { blocks })
  }, [project.layout, project.slideshowStructure, project.editorState, project.blockStates, project.projectType])

  // ── Load commit ──

  const loadCommit = useCallback(async (sha: string) => {
    if (!project.projectId) return

    try {
      const history = await project.db.listHistory(project.projectId)
      const isHead = history.length > 0 && history[0]?.sha === sha

      // If loading HEAD, return to normal editing mode
      if (isHead) {
        if (isViewingHistory && headProjectData) {
          restoreStates(headProjectData, headSlideshowDeps)
        }
        setIsViewingHistory(false)
        setCurrentViewingSha(null)
        setHeadProjectData(null)
        setHeadSlideshowDeps([])
        toast.success("Viewing latest version", {
          description: "You can edit the project", duration: 2000, icon: "✏️",
        })
        return
      }

      // Preserve HEAD data before switching to history
      if (!isViewingHistory) {
        setHeadProjectData(serializeCurrentState())
        if (project.layout === "slideshow") {
          setHeadSlideshowDeps([...project.slideshowDeps])
        }
      }

      // Load the commit
      const commitData = await project.db.loadFromHistory(project.projectId, sha)
      if (!commitData) {
        toast.error("Failed to load commit", {
          description: "The historical version could not be found", duration: 3000,
        })
        return
      }

      if (!commitData.data || !commitData.type) {
        toast.error("Invalid historical data", {
          description: "This commit contains incomplete data. It may be from an older version.",
          duration: 4000,
        })
        return
      }

      restoreStates(commitData.data, commitData.deps)

      setIsViewingHistory(true)
      setCurrentViewingSha(sha)

      toast.info("Viewing historical version", {
        description: "This is read-only. Return to latest to edit.",
        duration: 3000, icon: "📜",
      })
    } catch (error) {
      console.error("Failed to load commit:", error)
      toast.error("Failed to load historical version", {
        description: error instanceof Error ? error.message : "Unknown error",
        duration: 4000,
      })
    }
  }, [project.projectId, project.db, project.layout, project.slideshowDeps, isViewingHistory, headProjectData, headSlideshowDeps, restoreStates, serializeCurrentState])

  // ── Load snapshot ──

  const loadSnapshot = useCallback(async (tag: string) => {
    if (!project.projectId) return

    if (!isViewingHistory) {
      setHeadProjectData(serializeCurrentState())
      if (project.layout === "slideshow") {
        setHeadSlideshowDeps([...project.slideshowDeps])
      }
    }

    const snapshots = await project.db.listSnapshots(project.projectId)
    const snapshot = snapshots.find((s: any) => s.tag === tag)
    if (snapshot) {
      await loadCommit(snapshot.sha)
    }
  }, [project.projectId, project.db, project.layout, project.slideshowDeps, isViewingHistory, serializeCurrentState, loadCommit])

  // ── Return to HEAD ──

  const returnToHead = useCallback(async () => {
    if (!project.projectId || !headProjectData) return

    restoreStates(headProjectData, headSlideshowDeps)

    setIsViewingHistory(false)
    setCurrentViewingSha(null)
    setHeadProjectData(null)
    setHeadSlideshowDeps([])

    toast.success("Returned to latest version", {
      description: "You can now edit the project", duration: 2000, icon: "✏️",
    })
  }, [project.projectId, headProjectData, headSlideshowDeps, restoreStates])

  return {
    isViewingHistory,
    currentViewingSha,
    loadCommit,
    loadSnapshot,
    returnToHead,
  }
}
