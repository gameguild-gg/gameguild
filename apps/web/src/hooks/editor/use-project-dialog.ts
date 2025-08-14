"use client"

import { useState, useEffect } from "react"
import { toast } from "sonner"

interface ProjectData {
  id: string
  name: string
  data: string
  tags: string[]
  size: number
  createdAt: string
  updatedAt: string
}

interface StorageAdapter {
  list: () => Promise<ProjectData[]>
  load: (id: string) => Promise<ProjectData | null>
  delete?: (id: string) => Promise<void>
  searchProjects: (searchTerm: string, tags: string[], filterMode: "all" | "any") => Promise<ProjectData[]>
}

interface UseProjectDialogProps {
  isDbInitialized: boolean
  storageAdapter: StorageAdapter
}

export function useProjectDialog({ isDbInitialized, storageAdapter }: UseProjectDialogProps) {
  const [searchTerm, setSearchTerm] = useState("")
  const [selectedTags, setSelectedTags] = useState<string[]>([])
  const [currentPage, setCurrentPage] = useState(1)
  const [itemsPerPage, setItemsPerPage] = useState(12) // Changed initial itemsPerPage from 10 to 12 to match available options in selector
  const [filteredProjects, setFilteredProjects] = useState<ProjectData[]>([])
  const [totalProjects, setTotalProjects] = useState(0)
  const [tagFilterMode, setTagFilterMode] = useState<"all" | "any">("any")

  // Reset pagination only when filter criteria actually change
  useEffect(() => {
    setCurrentPage(1)
  }, [searchTerm, selectedTags, tagFilterMode])

  useEffect(() => {
    setCurrentPage(1)
  }, [itemsPerPage])

  // Filter projects based on search and tags
  useEffect(() => {
    const filterProjects = async () => {
      if (!isDbInitialized) return

      try {
        let projects: ProjectData[]

        if (searchTerm || selectedTags.length > 0) {
          projects = await storageAdapter.searchProjects(searchTerm, selectedTags, tagFilterMode)
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
  }, [searchTerm, selectedTags, isDbInitialized, tagFilterMode, storageAdapter])

  const handleDownload = (projectId: string, projectName: string, projectData: string) => {
    try {
      // Create blob with lexical data
      const blob = new Blob([projectData], { type: "application/json" })
      const url = URL.createObjectURL(blob)

      // Create download link
      const link = document.createElement("a")
      link.href = url
      link.download = `${projectName}.lexical`
      document.body.appendChild(link)
      link.click()

      // Cleanup
      document.body.removeChild(link)
      URL.revokeObjectURL(url)

      toast.success("Download started", {
        description: `File "${projectName}.lexical" is being downloaded`,
        duration: 2500,
        icon: "📥",
      })
    } catch (error) {
      console.error("Download error:", error)
      toast.error("Download error", {
        description: "Could not download the file. Please try again.",
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
