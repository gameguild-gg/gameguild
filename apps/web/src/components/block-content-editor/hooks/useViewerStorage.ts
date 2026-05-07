"use client"

import { useState, useEffect, useRef } from "react"
import { toast } from "sonner"
import { EnhancedStorageAdapter } from "@/lib/storage/editor/enhanced-storage-adapter"
import { extractEditorStates } from "@/lib/storage/editor/layout-detector"
import { ENGINE_TYPES } from "@/lib/storage/editor/project-types"
import { checkSelectedProject as checkProjectPreview } from "@/components/block-content-editor/extras/preview/preview-load-operations"
import type { ProjectData } from "@/components/block-content-editor/extras/preview/preview-load-operations"
import { cellsToLexical } from "@/lib/storage/editor/cell-converters/lexical"
import { storageToBlocks } from "@/lib/storage/editor/cell-converters/blocks"
import type { BlockArray } from "@/lib/storage/editor/block-structure"

export interface ViewerLayoutInfo {
  states: { blocks: Record<string, any> }
  isBlocksEngine?: boolean
  blocksArray?: BlockArray
}

export interface UseViewerStorageReturn {
  currentProject: ProjectData | null
  setCurrentProject: (project: ProjectData | null) => void
  isDbInitialized: boolean
  availableTags: Array<{ name: string; usageCount: number }>
  storageAdapter: {
    load: (id: string) => Promise<ProjectData | null>
    list: () => Promise<ProjectData[]>
    searchProjects: (
      searchTerm: string,
      tags: string[],
      filterMode?: "all" | "any",
      storageTypeFilter?: "local" | "gameguild-cloud" | "google-drive",
    ) => Promise<ProjectData[]>
  }
  loadProject: (projectData: ProjectData) => void
  layoutInfo: ViewerLayoutInfo
}

export function useViewerStorage(): UseViewerStorageReturn {
  const [currentProject, setCurrentProject] = useState<ProjectData | null>(null)
  const [availableTags, setAvailableTags] = useState<Array<{ name: string; usageCount: number }>>([])
  const [isDbInitialized, setIsDbInitialized] = useState(false)

  const dbStorage = useRef<EnhancedStorageAdapter>(new EnhancedStorageAdapter())

  // ── Storage adapter (read-only subset) ──
  const storageAdapter = {
    load: async (id: string): Promise<ProjectData | null> => {
      if (!isDbInitialized) throw new Error("Database not initialized")
      try {
        return await dbStorage.current.load(id)
      } catch (error) {
        console.error("Failed to load project:", error)
        return null
      }
    },

    list: async (): Promise<ProjectData[]> => {
      if (!isDbInitialized) return []
      try {
        return await dbStorage.current.list()
      } catch (error) {
        console.error("Failed to list projects:", error)
        return []
      }
    },

    searchProjects: async (
      searchTerm: string,
      tags: string[],
      filterMode?: "all" | "any",
      storageTypeFilter?: "local" | "gameguild-cloud" | "google-drive",
    ): Promise<ProjectData[]> => {
      if (!isDbInitialized) return []
      try {
        return await dbStorage.current.searchProjects(searchTerm, tags, filterMode || "any", storageTypeFilter)
      } catch (error) {
        console.error("Failed to search projects:", error)
        return []
      }
    },
  }

  // ── Initialize DB, load tags, check URL hash ──
  useEffect(() => {
    const initDB = async () => {
      try {
        await dbStorage.current.init()
        setIsDbInitialized(true)

        // Load tags
        try {
          const tags = await dbStorage.current.getAllTags()
          setAvailableTags(tags)
        } catch (error) {
          console.error("Failed to load tags:", error)
        }

        // Check for selected project from URL hash
        await checkProjectPreview({
          storageAdapter: {
            load: (id: string) => dbStorage.current.load(id),
          },
          setCurrentProject,
        })
      } catch (error) {
        console.error("Failed to initialize IndexedDB:", error)
        toast.error("Storage error", {
          description: "Could not initialize database. Some features may not work.",
          duration: 5000,
          icon: "⚠️",
        })
      }
    }

    initDB()
  }, [])

  // ── Load project (from dialog or list) ──
  const loadProject = (projectData: ProjectData) => {
    setCurrentProject(projectData)
    window.history.pushState(null, "", `#${projectData.id}`)
  }

  // ── Compute layout + states from current project ──
  const computeLayoutInfo = (): ViewerLayoutInfo => {
    if (!currentProject) {
      return { states: { blocks: {} } }
    }

    // Check if project uses blocks engine
    const projectEngine = (currentProject as any).engine
    if (projectEngine === ENGINE_TYPES.BLOCKS) {
      const cellStates = extractEditorStates(currentProject.data)
      const storageData = cellStates.blocks.b1 || { order: [], blocks: {} }
      const safeData = storageData && typeof storageData === "object" && !Array.isArray(storageData)
        ? storageData
        : { order: [], blocks: {} }
      return {
        states: { blocks: {} },
        isBlocksEngine: true,
        blocksArray: storageToBlocks(safeData),
      }
    }

    const cellStates = extractEditorStates(currentProject.data)

    // Convert cells to Lexical for preview renderers
    const states = {
      blocks: Object.entries(cellStates.blocks).reduce(
        (acc, [blockId, cellsData]) => {
          acc[blockId] = cellsToLexical(cellsData)
          return acc
        },
        {} as Record<string, any>,
      ),
    }

    return {
      states,
    }
  }

  const layoutInfo = computeLayoutInfo()

  return {
    currentProject,
    setCurrentProject,
    isDbInitialized,
    availableTags,
    storageAdapter,
    loadProject,
    layoutInfo,
  }
}
