"use client"

import { useState } from "react"
import { FolderOpen, Plus, Upload } from "lucide-react"
import { toast } from "sonner"

import { Button } from "@/components/ui/button"
import { DeleteConfirmDialog } from "@/components/block-content-editor/extras/dialogs/delete-confirm-dialog"
import { ProjectSearchFilters } from "@/components/block-content-editor/extras/project-dialog/project-search-filters"
import { ProjectList } from "@/components/block-content-editor/extras/project-dialog/project-list"
import { ProjectPagination } from "@/components/block-content-editor/extras/project-dialog/project-pagination"
import { ProjectPickerShell } from "@/components/block-content-editor/extras/project-dialog/project-picker-shell"
import { useProjectDialog } from "@/components/block-content-editor/hooks/editor/use-project-dialog"
import { useProjectActions } from "@/components/block-content-editor/hooks/editor/use-project-actions"
import { ImportProjectDialog } from "./import-project-dialog"
import { InfoDialog } from "./info-dialog"
import type { ProjectData } from "@/components/block-content-editor/lib/storage/editor/project-data"
import type { ProjectType } from "@/components/block-content-editor/lib/storage/editor/project-types"
import type { StorageType } from "@/components/block-content-editor/lib/storage/editor/storage-types"
import type { ProjectPreferences } from "@/components/block-content-editor/lib/storage/editor/project-preferences"

interface StorageAdapter {
  save: (
    id: string,
    name: string,
    data: string,
    tags: string[],
    storageType?: StorageType,
    preferences?: ProjectPreferences,
  ) => Promise<void>
  list: () => Promise<ProjectData[]>
  load: (id: string) => Promise<ProjectData | null>
  delete: (id: string) => Promise<void>
  searchProjects: (
    searchTerm: string,
    tags: string[],
    filterMode: "all" | "any",
    storageTypeFilter?: StorageType,
  ) => Promise<ProjectData[]>
}

interface OpenProjectDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  isFirstTime: boolean
  isDbInitialized: boolean
  storageAdapter: StorageAdapter
  availableTags: Array<{ name: string }>
  onProjectLoad: (projectData: ProjectData) => void
  onProjectsListUpdate: () => void
  onCreateNew: () => void
  currentProjectName: string
  /**
   * If set, the dialog only lists projects whose `preferences.global.projectType`
   * matches one of these values. Projects with no `projectType` are treated as
   * "general". Undefined = accept all.
   */
  allowedProjectTypes?: ProjectType[]
}

export function OpenProjectDialog({
  open,
  onOpenChange,
  isFirstTime,
  isDbInitialized,
  storageAdapter,
  availableTags,
  onProjectLoad,
  onProjectsListUpdate,
  onCreateNew,
  currentProjectName,
  allowedProjectTypes,
}: OpenProjectDialogProps) {
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
    loadProject,
  } = useProjectDialog({ isDbInitialized, storageAdapter })

  const [importDialogOpen, setImportDialogOpen] = useState(false)

  const projectActions = useProjectActions({
    storageAdapter,
    onProjectsListUpdate,
    onProjectUpdate: async () => {
      onProjectsListUpdate()
    },
  })

  const handleOpen = async (projectId: string) => {
    const projectData = await loadProject(projectId)
    if (!projectData) {
      console.error("Project data not found")
      return
    }

    try {
      if (!projectData.data) {
        throw new Error("Project data is missing")
      }

      // Guard: don't allow opening a project whose type this page doesn't accept.
      if (allowedProjectTypes && allowedProjectTypes.length > 0) {
        const t = projectData.preferences?.global?.projectType ?? "general"
        if (!allowedProjectTypes.includes(t)) {
          toast.error("Projeto incompatível com esta página", {
            description: `Este editor não abre projetos do tipo "${t}".`,
            duration: 4000,
            icon: "🚫",
          })
          return
        }
      }

      onProjectLoad(projectData)
      onOpenChange(false)
      await new Promise((resolve) => setTimeout(resolve, 100))

      toast.success("Projeto carregado", {
        description: `"${projectData.name}" foi aberto com sucesso`,
        duration: 2500,
        icon: "📂",
      })
    } catch (error) {
      console.error("Failed to load project:", error, "Project data:", projectData)
      const errorMessage = error instanceof Error ? error.message : "Unknown error"
      toast.error("Erro ao carregar projeto", {
        description: `O arquivo do projeto está corrompido ou em formato inválido: ${errorMessage}`,
        duration: 4000,
        icon: "❌",
      })
    }
  }

  const handleImportProject = (projectData: { id: string; name: string; tags: string[] }) => {
    handleOpen(projectData.id)
  }

  const generateProjectId = () => {
    return Date.now().toString() + Math.random().toString(36).substr(2, 9)
  }

  // Page-level filter by project type. Projects with no projectType stored
  // are treated as "general".
  const visibleProjects = allowedProjectTypes && allowedProjectTypes.length > 0
    ? filteredProjects.filter((p) => {
        const t = p.preferences?.global?.projectType ?? "general"
        return allowedProjectTypes.includes(t)
      })
    : filteredProjects
  const visibleTotal = allowedProjectTypes && allowedProjectTypes.length > 0
    ? visibleProjects.length
    : totalProjects

  return (
    <>
      <ProjectPickerShell
        open={open}
        onOpenChange={onOpenChange}
        title={isFirstTime ? "Welcome! Choose an Option" : "Open Project"}
        description={
          isFirstTime
            ? "To get started, please open an existing project or create a new one."
            : undefined
        }
        trigger={
          <Button variant="outline" size="sm" className="gap-2 bg-transparent" disabled={!isDbInitialized}>
            <FolderOpen className="w-4 h-4" />
            Open
          </Button>
        }
        onAuthSuccess={onProjectsListUpdate}
        authSuccessToast={{
          title: "Google Drive connected successfully!",
          description: "You can now access your Google Drive projects.",
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
            projects={visibleProjects}
            currentPage={currentPage}
            itemsPerPage={itemsPerPage}
            searchTerm={searchTerm}
            selectedTags={selectedTags}
            viewMode="grid"
            onOpen={handleOpen}
            onDelete={projectActions.handleConfirmDelete}
            onDownload={projectActions.handleDownload}
            onInfo={projectActions.handleOpenInfo}
            showDeleteButton={true}
            openButtonText="Open"
          />
        }
        pagination={
          <ProjectPagination
            currentPage={currentPage}
            totalProjects={visibleTotal}
            itemsPerPage={itemsPerPage}
            onPageChange={setCurrentPage}
          />
        }
        footerLeft={
          <>
            <Button
              variant="ghost"
              onClick={() => {
                onOpenChange(false)
                onCreateNew()
              }}
              className="gap-2"
              disabled={!isDbInitialized}
            >
              <Plus className="w-4 h-4" />
              Create New
            </Button>
            <Button
              variant="ghost"
              onClick={() => {
                onOpenChange(false)
                setImportDialogOpen(true)
              }}
              className="gap-2"
              disabled={!isDbInitialized}
            >
              <Upload className="w-4 h-4" />
              Import Project
            </Button>
          </>
        }
        footerRight={
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={!currentProjectName}
            className="bg-transparent"
          >
            Fechar
          </Button>
        }
      />

      <DeleteConfirmDialog
        open={projectActions.deleteDialogOpen}
        onOpenChange={projectActions.setDeleteDialogOpen}
        itemName={projectActions.projectToDelete?.name}
        itemType="projeto"
        onConfirm={projectActions.handleDelete}
        title={""}
      />

      <ImportProjectDialog
        open={importDialogOpen}
        onOpenChange={(open) => {
          setImportDialogOpen(open)
          if (!open) {
            onOpenChange(true)
          }
        }}
        isDbInitialized={isDbInitialized}
        storageAdapter={{
          ...storageAdapter,
          save: storageAdapter.save,
        }}
        availableTags={availableTags}
        onProjectCreate={(projectData) => {
          const { id, name, tags } = projectData
          onProjectLoad({
            id,
            name,
            tags,
            data: "",
            metadata: { size: 0, hash: "", createdAt: "", updatedAt: "" },
            storageType: "local",
          })
        }}
        onProjectsListUpdate={onProjectsListUpdate}
        onAvailableTagsUpdate={() => {}}
        generateProjectId={generateProjectId}
        onOpenProject={handleImportProject}
      />

      <InfoDialog
        open={projectActions.infoDialogOpen}
        onOpenChange={projectActions.setInfoDialogOpen}
        project={projectActions.projectToEdit}
        onSave={projectActions.handleSaveInfo}
        availableTags={availableTags}
        storageAdapter={storageAdapter}
      />
    </>
  )
}
