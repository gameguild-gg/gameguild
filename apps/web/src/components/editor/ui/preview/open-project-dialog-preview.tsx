"use client"

import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"
import { ProjectSearchFilters } from "@/components/editor/ui/project-dialog/project-search-filters"
import { ProjectList } from "@/components/editor/ui/project-dialog/project-list"
import { ProjectPagination } from "@/components/editor/ui/project-dialog/project-pagination"
import { FolderOpen, Eye } from "lucide-react"
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
  searchProjects: (searchTerm: string, tags: string[], filterMode: "all" | "any") => Promise<ProjectData[]>
}

interface OpenProjectDialogPreviewProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  isDbInitialized: boolean
  storageAdapter: StorageAdapter
  availableTags: Array<{ name: string; usageCount: number }>
  onProjectLoad: (projectData: ProjectData) => void
  formatStorageSize: (sizeInKB: number) => string
}

export function OpenProjectDialogPreview({
  open,
  onOpenChange,
  isDbInitialized,
  storageAdapter,
  availableTags,
  onProjectLoad,
  formatStorageSize,
}: OpenProjectDialogPreviewProps) {
  const [searchTerm, setSearchTerm] = useState("")
  const [selectedTags, setSelectedTags] = useState<string[]>([])
  const [currentPage, setCurrentPage] = useState(1)
  const [itemsPerPage, setItemsPerPage] = useState(10)
  const [filteredProjects, setFilteredProjects] = useState<ProjectData[]>([])
  const [totalProjects, setTotalProjects] = useState(0)
  const [tagFilterMode, setTagFilterMode] = useState<"all" | "any">("any")

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
        setCurrentPage(1) // Reset to first page when filtering
      } catch (error) {
        console.error("Failed to filter projects:", error)
      }
    }

    filterProjects()
  }, [searchTerm, selectedTags, isDbInitialized, tagFilterMode, storageAdapter])

  const handleOpen = async (projectId: string) => {
    try {
      const projectData = await storageAdapter.load(projectId)
      if (projectData) {
        onProjectLoad(projectData)
        onOpenChange(false)
        toast.success("Project loaded for preview", {
          description: `"${projectData.name}" is now being previewed`,
          duration: 2500,
          icon: "👁️",
        })
      } else {
        toast.error("Project not found", {
          description: "Could not locate the project file",
          duration: 3000,
          icon: "🔍",
        })
      }
    } catch (error) {
      console.error("Open error:", error)
      toast.error("Error opening project", {
        description: "Could not open the project. Please try again.",
        duration: 4000,
        icon: "❌",
      })
    }
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(open) => {
        onOpenChange(open)
      }}
    >
      <DialogTrigger asChild>
        <Button variant="outline" size="sm" className="gap-2 bg-transparent" disabled={!isDbInitialized}>
          <FolderOpen className="w-4 h-4" />
          Open Project
        </Button>
      </DialogTrigger>
      <DialogContent className="max-w-2xl min-w-xl max-h-[80vh]" onInteractOutside={(e) => e.preventDefault()}>
        <DialogHeader>
          <DialogTitle>Open Project for Preview</DialogTitle>
          <p className="text-sm text-muted-foreground">Select a project to preview its content</p>
        </DialogHeader>
        <div className="space-y-4">
          <ProjectSearchFilters
            searchTerm={searchTerm}
            onSearchChange={setSearchTerm}
            selectedTags={selectedTags}
            onTagsChange={setSelectedTags}
            availableTags={availableTags}
            tagFilterMode={tagFilterMode}
            onTagFilterModeChange={setTagFilterMode}
            itemsPerPage={itemsPerPage}
            onItemsPerPageChange={setItemsPerPage}
          />

          <ProjectList
            projects={filteredProjects}
            currentPage={currentPage}
            itemsPerPage={itemsPerPage}
            searchTerm={searchTerm}
            selectedTags={selectedTags}
            onOpen={handleOpen}
            showDeleteButton={false}
            openButtonText="Preview"
            openButtonIcon={<Eye className="w-4 h-4" />}
          />

          <ProjectPagination
            currentPage={currentPage}
            totalProjects={totalProjects}
            itemsPerPage={itemsPerPage}
            onPageChange={setCurrentPage}
          />

          <div className="flex justify-end items-center pt-4 border-t dark:border-gray-700">
            <Button variant="outline" onClick={() => onOpenChange(false)} className="bg-transparent">
              Close
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
