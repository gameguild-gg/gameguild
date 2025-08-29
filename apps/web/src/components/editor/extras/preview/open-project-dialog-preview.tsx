"use client"

import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"
import { ProjectSearchFilters } from "@/components/editor/extras/project-dialog/project-search-filters"
import { ProjectList } from "@/components/editor/extras/project-dialog/project-list"
import { ProjectPagination } from "@/components/editor/extras/project-dialog/project-pagination"
import { useProjectDialog } from "@/hooks/editor/use-project-dialog"
import { FolderOpen, Eye } from "lucide-react"
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
  availableTags: Array<{ name: string }>
  onProjectLoad: (projectData: ProjectData) => void
}

export function OpenProjectDialogPreview({
  open,
  onOpenChange,
  isDbInitialized,
  storageAdapter,
  availableTags,
  onProjectLoad,
}: OpenProjectDialogPreviewProps) {
  const {
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
    handleDownload,
    loadProject,
  } = useProjectDialog({ isDbInitialized, storageAdapter })

  const handleOpen = async (projectId: string) => {
    const projectData = await loadProject(projectId)
    if (projectData) {
      onProjectLoad(projectData)
      onOpenChange(false)
      toast.success("Project loaded for preview", {
        description: `"${projectData.name}" is now being previewed`,
        duration: 2500,
        icon: "👁️",
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
      <DialogContent
        className="max-w-2xl lg:max-w-4xl w-full h-[95vh] flex flex-col overflow-hidden"
        onInteractOutside={(e) => e.preventDefault()}
      >
        <DialogHeader className="flex-shrink-0">
          <DialogTitle>Open Project for Preview</DialogTitle>
          <p className="text-sm text-muted-foreground">Select a project to preview its content</p>
        </DialogHeader>

        <div className="flex-1 min-h-0 flex flex-col space-y-4">
          <div className="flex-shrink-0">
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
          </div>

          <div className="flex-1 min-h-0 overflow-y-auto">
            <ProjectList
              projects={filteredProjects}
              currentPage={currentPage}
              itemsPerPage={itemsPerPage}
              searchTerm={searchTerm}
              selectedTags={selectedTags}
              onOpen={handleOpen}
              onDownload={handleDownload}
              showDeleteButton={false}
              openButtonText="Preview"
              openButtonIcon={<Eye className="w-4 h-4" />}
            />
          </div>

          <div className="flex-shrink-0 h-12 flex items-center justify-center">
            <ProjectPagination
              currentPage={currentPage}
              totalProjects={totalProjects}
              itemsPerPage={itemsPerPage}
              onPageChange={setCurrentPage}
            />
          </div>

          <div className="flex justify-end items-center pt-4 border-t dark:border-gray-700 flex-shrink-0 h-16">
            <Button variant="outline" onClick={() => onOpenChange(false)} className="bg-transparent">
              Close
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
