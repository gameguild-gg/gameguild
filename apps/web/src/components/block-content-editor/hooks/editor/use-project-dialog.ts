"use client"

import { ProjectExporter, type ProjectData as ExportProjectData } from "@/components/block-content-editor/lib/interopAdapter/project-exporter"
import { HashManager } from "@/components/block-content-editor/lib/sync/editor/hash-manager"
import type { ProjectData } from "@/components/block-content-editor/lib/storage/editor/project-data"
import { useCallback, useEffect, useState } from "react"
import { toast } from "sonner"

interface StorageAdapter {
  list: () => Promise<ProjectData[]>
  load: (id: string) => Promise<ProjectData | null>
  delete?: (id: string) => Promise<void>
  searchProjects: (searchTerm: string, tags: string[], filterMode: "all" | "any", storageTypeFilter?: "local" | "gameguild-cloud" | "google-drive") => Promise<ProjectData[]>
}

interface UseProjectDialogProps {
  isDbInitialized: boolean
  storageAdapter: StorageAdapter
}

export function useProjectDialog({ isDbInitialized, storageAdapter }: UseProjectDialogProps) {
  const [searchTerm, setSearchTerm] = useState("")
  const [selectedTags, setSelectedTags] = useState<string[]>([])
  const [storageTypeFilter, setStorageTypeFilter] = useState<"local" | "gameguild-cloud" | "google-drive" | undefined>(undefined)
  const [currentPage, setCurrentPage] = useState(1)
  const [itemsPerPage, setItemsPerPage] = useState(16) // Changed initial itemsPerPage from 10 to 12 to match available options in selector
  const [filteredProjects, setFilteredProjects] = useState<ProjectData[]>([])
  const [totalProjects, setTotalProjects] = useState(0)
  const [tagFilterMode, setTagFilterMode] = useState<"all" | "any">("any")

  // Function to refresh projects list
  const refreshProjects = useCallback(async () => {
    if (!isDbInitialized) return

    try {
      let projects: ProjectData[]

      if (searchTerm || selectedTags.length > 0 || storageTypeFilter) {
        projects = await storageAdapter.searchProjects(searchTerm, selectedTags, tagFilterMode, storageTypeFilter)
      } else {
        projects = await storageAdapter.list()
      }

      setTotalProjects(projects.length)
      setFilteredProjects(projects)
    } catch (error) {
      console.error("Failed to refresh projects:", error)
    }
  }, [isDbInitialized, searchTerm, selectedTags, storageTypeFilter, tagFilterMode, storageAdapter])

  // Reset pagination only when filter criteria actually change
  useEffect(() => {
    setCurrentPage(1)
  }, [searchTerm, selectedTags, tagFilterMode, storageTypeFilter])

  useEffect(() => {
    setCurrentPage(1)
  }, [itemsPerPage])

  // Filter projects based on search and tags
  useEffect(() => {
    refreshProjects()
  }, [refreshProjects])

  const handleDownload = async (
    projectId: string,
    projectName: string,
    projectData: string,
    projectTags: string[],
    createdAt: string,
    updatedAt: string,
    projectPreferences?: any
  ) => {
    try {
      // Generate hash for the project
      const hash = await HashManager.generateHash(projectData)

      // Prepare project data for export using ProjectExporter
      const exportProjectData: ExportProjectData = {
        id: projectId,
        name: projectName,
        data: projectData,
        tags: projectTags,
        size: new Blob([projectData]).size,
        createdAt: createdAt,
        updatedAt: updatedAt,
        hash: hash,
        storageType: "local",
        preferences: projectPreferences
      }

      // Use ProjectExporter to create the ZIP file
      const zipBlob = await ProjectExporter.createZipFile(exportProjectData, hash)

      // Create download link
      const url = URL.createObjectURL(zipBlob)
      const link = document.createElement("a")
      link.href = url
      link.download = ProjectExporter.getDownloadFilename(exportProjectData)
      document.body.appendChild(link)
      link.click()

      // Cleanup
      document.body.removeChild(link)
      URL.revokeObjectURL(url)

      toast.success("Export completed", {
        description: `Project "${projectName}" exported successfully`,
        duration: 2500,
        icon: "📥",
      })
    } catch (error) {
      console.error("Export failed:", error)
      toast.error("Export failed", {
        description: "Could not export the project. Please try again.",
        duration: 4000,
        icon: "❌",
      })
    }
  }

  const loadProject = async (projectId: string): Promise<ProjectData | null> => {
    try {
      const projectData = await storageAdapter.load(projectId)
      if (!projectData) {
        toast.error("Project not found", {
          description: "Could not locate the project file",
          duration: 3000,
          icon: "🔍",
        })
        return null
      }

      // Additional validation for project data structure
      if (!projectData.data) {
        console.error("Project data missing 'data' field:", projectData)
        toast.error("Project data incomplete", {
          description: "The project file appears to be missing content data",
          duration: 4000,
          icon: "⚠️",
        })
        return null
      }

      // Validate that data is a non-empty string
      if (typeof projectData.data !== 'string' || projectData.data.trim() === '') {
        console.error("Project data is not a valid string:", typeof projectData.data, projectData.data?.length)
        toast.error("Project data invalid", {
          description: "The project content is not in the expected format",
          duration: 4000,
          icon: "⚠️",
        })
        return null
      }

      return projectData
    } catch (error) {
      console.error("Load error:", error)
      toast.error("Error loading project", {
        description: "Could not load the project. Please try again.",
        duration: 4000,
        icon: "❌",
      })
      return null
    }
  }

  return {
    // State
    searchTerm,
    setSearchTerm,
    selectedTags,
    setSelectedTags,
    storageTypeFilter,
    setStorageTypeFilter,
    currentPage,
    setCurrentPage,
    itemsPerPage,
    setItemsPerPage,
    filteredProjects,
    totalProjects,
    tagFilterMode,
    setTagFilterMode,

    // Functions
    handleDownload,
    loadProject,
    refreshProjects,
  }
}
