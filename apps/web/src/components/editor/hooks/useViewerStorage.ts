"use client"

import { useState, useEffect, useRef } from "react"
import { toast } from "sonner"
import { EnhancedStorageAdapter } from "@/lib/storage/editor/enhanced-storage-adapter"
import { detectProjectLayout, extractEditorStates } from "@/lib/storage/editor/layout-detector"
import { getLayoutFromType, type ProjectType, type InternalLayout, ENGINE_TYPES } from "@/lib/storage/editor/project-types"
import { checkSelectedProject as checkProjectPreview } from "@/components/editor/extras/preview/preview-load-operations"
import type { ProjectData } from "@/components/editor/extras/preview/preview-load-operations"
import type { ProjectData as StorageProjectData } from "@/lib/storage/editor/enhanced-storage-adapter"
import { cellsToLexical } from "@/lib/storage/editor/cell-converters/lexical"
import { storageToBlocks } from "@/lib/storage/editor/cell-converters/blocks"
import type { BlockArray } from "@/lib/storage/editor/block-structure"

export interface ViewerLayoutInfo {
  layout: InternalLayout
  states: { blocks: Record<string, any> }
  hasSlides: boolean
  slideshowData?: any
  projectType?: ProjectType
  previewMode?: "continuous" | "slide"
  isBlocksEngine?: boolean
  blocksArray?: BlockArray
}

export interface UseViewerStorageReturn {
  currentProject: ProjectData | null
  setCurrentProject: (project: ProjectData | null) => void
  isDbInitialized: boolean
  availableTags: Array<{ name: string; usageCount: number }>
  resolvedProjects: Map<string, StorageProjectData | null>
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
  const [resolvedProjects, setResolvedProjects] = useState<Map<string, StorageProjectData | null>>(new Map())

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
          setResolvedProjects,
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

    // Load independent projects for slideshows
    const layoutInfo = detectProjectLayout(projectData.data)
    if (layoutInfo.hasSlides && layoutInfo.slideshowData && dbStorage.current) {
      const independentSlides = layoutInfo.slideshowData.slides.filter(
        (slide: any) => slide.projectRef && !slide.projectRef.isDependent,
      )
      if (independentSlides.length > 0) {
        ;(async () => {
          const results = new Map<string, StorageProjectData | null>()
          await Promise.all(
            independentSlides.map(async (slide: any) => {
              const projectId = slide.projectRef!.projectId
              try {
                const project = await dbStorage.current!.load(projectId)
                results.set(slide.id, project)
              } catch (error) {
                console.error(`Failed to load independent project ${projectId}:`, error)
                results.set(slide.id, null)
              }
            }),
          )
          setResolvedProjects(results)
        })()
      }
    }
  }

  // ── Compute layout + states from current project ──
  const computeLayoutInfo = (): ViewerLayoutInfo => {
    if (!currentProject) {
      return { layout: "single", states: { blocks: {} }, hasSlides: false }
    }

    // Check if project uses blocks engine
    const projectEngine = (currentProject as any).engine
    if (projectEngine === ENGINE_TYPES.BLOCKS) {
      const cellStates = extractEditorStates(currentProject.data, currentProject.type)
      const storageData = cellStates.blocks.b1 || { order: [], blocks: {} }
      const safeData = storageData && typeof storageData === "object" && !Array.isArray(storageData)
        ? storageData
        : { order: [], blocks: {} }
      return {
        layout: "single",
        states: { blocks: {} },
        hasSlides: false,
        isBlocksEngine: true,
        blocksArray: storageToBlocks(safeData),
      }
    }

    const detectedLayout = detectProjectLayout(currentProject.data)
    const finalLayout = getLayoutFromType(currentProject.type)
    const cellStates = extractEditorStates(currentProject.data, currentProject.type)

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

    const previewMode = currentProject.preferences?.global?.previewMode || "continuous"

    return {
      layout: finalLayout,
      states,
      hasSlides: detectedLayout.hasSlides,
      slideshowData: detectedLayout.slideshowData,
      projectType: currentProject.type,
      previewMode,
    }
  }

  const layoutInfo = computeLayoutInfo()

  return {
    currentProject,
    setCurrentProject,
    isDbInitialized,
    availableTags,
    resolvedProjects,
    storageAdapter,
    loadProject,
    layoutInfo,
  }
}
