"use client"

import { useState, useEffect } from "react"
import { toast } from "sonner"
import { ProjectExporter, type ProjectData as ExportProjectData } from "@/lib/interopAdapter/project-exporter"
import { HashManager } from "@/lib/sync/editor/hash-manager"

interface ProjectData {
  id: string
  name: string
  data: string
  tags: string[]
  size: number
  createdAt: string
  updatedAt: string
  storageType?: "local" | "gameguild-cloud" | "google-drive"
  isLocallyAvailable?: boolean
}

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

  // Reset pagination only when filter criteria actually change
  useEffect(() => {
    setCurrentPage(1)
  }, [searchTerm, selectedTags, tagFilterMode, storageTypeFilter])

  useEffect(() => {
    setCurrentPage(1)
  }, [itemsPerPage])

  // Filter projects based on search and tags
  useEffect(() => {
    const filterProjects = async () => {
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
        console.error("Failed to filter projects:", error)
      }
    }

    filterProjects()
  }, [searchTerm, selectedTags, isDbInitialized, tagFilterMode, storageTypeFilter, storageAdapter])

  const handleDownload = async (
    projectId: string,
    projectName: string,
    projectData: string,
    projectTags: string[],
    createdAt: string,
    updatedAt: string,
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
        storageType: "local"
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
  }
}
