"use client"

import { FolderOpen, Eye } from "lucide-react"
import { toast } from "sonner"

import { Button } from "@/components/ui/button"
import { ProjectSearchFilters } from "@/components/block-content-editor/extras/project-dialog/project-search-filters"
import { ProjectList } from "@/components/block-content-editor/extras/project-dialog/project-list"
import { ProjectPagination } from "@/components/block-content-editor/extras/project-dialog/project-pagination"
import { ProjectPickerShell } from "@/components/block-content-editor/extras/project-dialog/project-picker-shell"
import { useProjectDialog } from "@/components/block-content-editor/hooks/editor/use-project-dialog"
import type { ProjectData } from "./preview-load-operations"
import type { StorageType } from "@/components/block-content-editor/lib/storage/editor/storage-types"

interface StorageAdapter {
  list: () => Promise<ProjectData[]>
  load: (id: string) => Promise<ProjectData | null>
  searchProjects: (
    searchTerm: string,
    tags: string[],
    filterMode: "all" | "any",
    storageTypeFilter?: StorageType,
  ) => Promise<ProjectData[]>
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
    <ProjectPickerShell
      open={open}
      onOpenChange={onOpenChange}
      title="Open Project for Preview"
      description="Select a project to preview its content"
      trigger={
        <Button variant="outline" size="sm" className="gap-2 bg-transparent" disabled={!isDbInitialized}>
          <FolderOpen className="w-4 h-4" />
          Open Project
        </Button>
      }
      authSuccessToast={{
        title: "Google Drive connected successfully!",
        description: "You can now access your Google Drive projects for preview.",
      }}
      filters={
        <ProjectSearchFilters
          searchTerm={searchTerm}
          onSearchChange={setSearchTerm}
          selectedTags={selectedTags}
          onTagsChange={setSelectedTags}
          availableTags={availableTags}
          tagFilterMode={tagFilterMode}
          onTagFilterModeChange={setTagFilterMode}
          storageTypeFilter={storageTypeFilter}
          onStorageTypeFilterChange={setStorageTypeFilter}
          itemsPerPage={itemsPerPage}
          onItemsPerPageChange={setItemsPerPage}
          showFilters={true}
        />
      }
      list={
        <ProjectList
          projects={filteredProjects}
          currentPage={currentPage}
          itemsPerPage={itemsPerPage}
          searchTerm={searchTerm}
          selectedTags={selectedTags}
          viewMode="grid"
          onOpen={handleOpen}
          onDownload={handleDownload}
          showDeleteButton={false}
          openButtonText="Preview"
          openButtonIcon={<Eye className="w-4 h-4" />}
        />
      }
      pagination={
        <ProjectPagination
          currentPage={currentPage}
          totalProjects={totalProjects}
          itemsPerPage={itemsPerPage}
          onPageChange={setCurrentPage}
        />
      }
      footerRight={
        <Button variant="outline" onClick={() => onOpenChange(false)} className="bg-transparent">
          Close
        </Button>
      }
    />
  )
}
