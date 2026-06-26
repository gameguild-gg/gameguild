"use client"

import { useEffect, useMemo, useRef, useState } from "react"
import { toast } from "sonner"
import { EnhancedStorageAdapter } from "@/components/block-content-editor/lib/storage/editor/enhanced-storage-adapter"
import { deserializeProject } from "@/components/block-content-editor/lib/storage/editor/block-storage"
import { checkSelectedProject as checkProjectPreview } from "@/components/block-content-editor/extras/preview/preview-load-operations"
import type { ProjectData } from "@/components/block-content-editor/extras/preview/preview-load-operations"
import type { BlockArray } from "@/components/block-content-editor/lib/storage/editor/block-structure"
import type { StorageType } from "@/components/block-content-editor/lib/storage/editor/storage-types"

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
      storageTypeFilter?: StorageType,
    ) => Promise<ProjectData[]>
  }
  loadProject: (projectData: ProjectData) => void
  blocks: BlockArray
}

export function useViewerStorage(): UseViewerStorageReturn {
  const [currentProject, setCurrentProject] = useState<ProjectData | null>(null)
  const [availableTags, setAvailableTags] = useState<Array<{ name: string; usageCount: number }>>([])
  const [isDbInitialized, setIsDbInitialized] = useState(false)

  const dbStorage = useRef<EnhancedStorageAdapter>(new EnhancedStorageAdapter())

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
      try { return await dbStorage.current.list() }
      catch (error) { console.error("Failed to list projects:", error); return [] }
    },
    searchProjects: async (
      searchTerm: string,
      tags: string[],
      filterMode?: "all" | "any",
      storageTypeFilter?: StorageType,
    ): Promise<ProjectData[]> => {
      if (!isDbInitialized) return []
      try { return await dbStorage.current.searchProjects(searchTerm, tags, filterMode || "any", storageTypeFilter) }
      catch (error) { console.error("Failed to search projects:", error); return [] }
    },
  }

  useEffect(() => {
    const initDB = async () => {
      try {
        await dbStorage.current.init()
        setIsDbInitialized(true)

        try {
          const tags = await dbStorage.current.getAllTags()
          setAvailableTags(tags)
        } catch (error) {
          console.error("Failed to load tags:", error)
        }

        await checkProjectPreview({
          storageAdapter: { load: (id: string) => dbStorage.current.load(id) },
          setCurrentProject,
        })
      } catch (error) {
        console.error("Failed to initialize IndexedDB:", error)
        toast.error("Storage error", {
          description: "Could not initialize database. Some features may not work.",
          duration: 5000, icon: "⚠️",
        })
      }
    }
    initDB()
  }, [])

  const loadProject = (projectData: ProjectData) => {
    setCurrentProject(projectData)
    window.history.pushState(null, "", `#${projectData.id}`)
  }

  const blocks = useMemo<BlockArray>(() => {
    if (!currentProject) return []
    return deserializeProject(currentProject.data)
  }, [currentProject])

  return {
    currentProject,
    setCurrentProject,
    isDbInitialized,
    availableTags,
    storageAdapter,
    loadProject,
    blocks,
  }
}
