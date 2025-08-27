"use client"

import { useState, useEffect } from "react"
import { toast } from "sonner"
import JSZip from "jszip"

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
  const [itemsPerPage, setItemsPerPage] = useState(16) // Changed initial itemsPerPage from 10 to 12 to match available options in selector
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

  const handleDownload = async (
    projectId: string,
    projectName: string,
    projectData: string,
    projectTags: string[],
    createdAt: string,
    updatedAt: string,
  ) => {
    try {
      const zip = new JSZip()

      // Add the lexical file with .gglexical extension
      zip.file(`${projectName}.gglexical`, projectData)

      // Create index.json with project metadata
      const metadata = {
        id: projectId,
        name: projectName,
        tags: projectTags,
        size: new Blob([projectData]).size,
        createdAt: createdAt,
        updatedAt: updatedAt,
        version: "1.0",
        type: "gg-lexical-project",
      }

      zip.file("index.json", JSON.stringify(metadata, null, 2))

      // Generate zip file
      const zipBlob = await zip.generateAsync({ type: "blob" })
      const url = URL.createObjectURL(zipBlob)

      // Create download link
      const link = document.createElement("a")
      link.href = url
      link.download = `gg-lexical-editor-${projectName}.zip`
      document.body.appendChild(link)
      link.click()

      // Cleanup
      document.body.removeChild(link)
      URL.revokeObjectURL(url)

      toast.success("Download started", {
        description: `Folder "gg-lexical-editor-${projectName}.zip" is being downloaded`,
        duration: 2500,
        icon: "📥",
      })
    } catch (error) {
      console.error("Download error:", error)
      toast.error("Download error", {
        description: "Could not download the project folder. Please try again.",
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
