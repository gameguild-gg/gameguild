"use client"

import type React from "react"
import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"
import { DeleteConfirmDialog } from "@/components/editor/extras/dialogs/delete-confirm-dialog"
import { ProjectSearchFilters } from "@/components/editor/extras/project-dialog/project-search-filters"
import { ProjectList } from "@/components/editor/extras/project-dialog/project-list"
import { ProjectPagination } from "@/components/editor/extras/project-dialog/project-pagination"
import { useProjectDialog } from "@/hooks/editor/use-project-dialog"
import { FolderOpen, Upload, Info } from "lucide-react"
import { useState } from "react"
import { toast } from "sonner"
import type { LexicalEditor } from "lexical"
import { ImportProjectDialog } from "./import-project-dialog"
import { InfoDialog } from "./info-dialog"

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
  save: (id: string, name: string, data: string, tags: string[]) => Promise<void>
  list: () => Promise<ProjectData[]>
  load: (id: string) => Promise<ProjectData | null>
  delete: (id: string) => Promise<void>
  searchProjects: (searchTerm: string, tags: string[], filterMode: "all" | "any") => Promise<ProjectData[]>
}

interface OpenProjectDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  isFirstTime: boolean
  isDbInitialized: boolean
  storageAdapter: StorageAdapter
  availableTags: Array<{ name: string }>
  editorRef: React.RefObject<LexicalEditor | null>
  setLoadingRef: React.RefObject<((loading: boolean) => void) | null>
  onProjectLoad: (projectData: ProjectData) => void
  onProjectsListUpdate: () => void
  onCreateNew: () => void
  currentProjectName: string
}

export function OpenProjectDialog({
  open,
  onOpenChange,
  isFirstTime,
  isDbInitialized,
  storageAdapter,
  availableTags,
  editorRef,
  setLoadingRef,
  onProjectLoad,
  onProjectsListUpdate,
  onCreateNew,
  currentProjectName,
}: OpenProjectDialogProps) {
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

  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)
  const [projectToDelete, setProjectToDelete] = useState<{ id: string; name: string } | null>(null)
  const [importDialogOpen, setImportDialogOpen] = useState(false)
  const [infoDialogOpen, setInfoDialogOpen] = useState(false)
  const [projectToEdit, setProjectToEdit] = useState<ProjectData | null>(null)

  const handleOpen = async (projectId: string) => {
    const projectData = await loadProject(projectId)
    if (projectData && editorRef.current) {
      try {
        if (setLoadingRef.current) {
          setLoadingRef.current(true)
        }

        const editorState = editorRef.current.parseEditorState(projectData.data)
        editorRef.current.setEditorState(editorState)

        await new Promise((resolve) => setTimeout(resolve, 100))

        if (setLoadingRef.current) {
          setLoadingRef.current(false)
        }

        onProjectLoad(projectData)
        onOpenChange(false)
        toast.success("Projeto carregado", {
          description: `"${projectData.name}" foi aberto com sucesso`,
          duration: 2500,
          icon: "📂",
        })
      } catch (error) {
        if (setLoadingRef.current) {
          setLoadingRef.current(false)
        }
        toast.error("Erro ao carregar projeto", {
          description: "O arquivo do projeto está corrompido ou em formato inválido",
          duration: 4000,
          icon: "❌",
        })
      }
    }
  }

  const handleImportProject = (projectData: { id: string; name: string; tags: string[] }) => {
    handleOpen(projectData.id)
  }

  const generateProjectId = () => {
    return Date.now().toString() + Math.random().toString(36).substr(2, 9)
  }

  const handleOpenInfo = (project: ProjectData) => {
    setProjectToEdit(project)
    setInfoDialogOpen(true)
  }

  const handleSaveInfo = async (projectId: string, newName: string, newTags: string[]) => {
    const projectToUpdate = await storageAdapter.load(projectId)
    if (!projectToUpdate) {
      toast.error("Error finding project to update.")
      return
    }

    try {
      await storageAdapter.save(projectId, newName, projectToUpdate.data, newTags)
      toast.success("Project updated", {
        description: `"${newName}" has been updated successfully.`,
      })
      onProjectsListUpdate() // Re-fetch projects
    } catch (error) {
      console.error("Failed to save info:", error)
      toast.error("Failed to update project.")
      throw error // re-throw to prevent dialog from closing
    }
  }

  const handleConfirmDelete = (projectId: string, projectName: string) => {
    setProjectToDelete({ id: projectId, name: projectName })
    setDeleteDialogOpen(true)
  }

  const handleDelete = async () => {
    if (!projectToDelete) return

    try {
      await storageAdapter.delete(projectToDelete.id)
      await onProjectsListUpdate()

      toast.success("Projeto excluído", {
        description: `"${projectToDelete.name}" foi removido permanentemente`,
        duration: 3000,
        icon: "🗑️",
      })
    } catch (error) {
      console.error("Delete error:", error)
      toast.error("Erro ao excluir projeto", {
        description: "Não foi possível excluir o projeto. Tente novamente.",
        duration: 4000,
        icon: "❌",
      })
    } finally {
      setProjectToDelete(null)
    }
  }

  return (
    <>
      <Dialog
        open={open}
        onOpenChange={(open) => {
          onOpenChange(open)
        }}
      >
        <DialogTrigger asChild>
          <Button variant="outline" size="sm" className="gap-2 bg-transparent" disabled={!isDbInitialized}>
            <FolderOpen className="w-4 h-4" />
            Open
          </Button>
        </DialogTrigger>
        <DialogContent
          className="max-w-2xl lg:max-w-4xl w-full h-[95vh] overflow-hidden flex flex-col"
          onInteractOutside={(e) => e.preventDefault()}
        >
          <DialogHeader className="flex-shrink-0">
            <DialogTitle>{isFirstTime ? "Welcome! Choose an Option" : "Open Project"}</DialogTitle>
            {isFirstTime && (
              <p className="text-sm text-muted-foreground">
                To get started, please open an existing project or create a new one.
              </p>
            )}
          </DialogHeader>

          <div className="flex flex-col flex-1 min-h-0 space-y-4">
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
                onDelete={handleConfirmDelete}
                onDownload={handleDownload}
                onInfo={handleOpenInfo}
                showDeleteButton={true}
                openButtonText="Open"
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

            <div className="flex justify-between items-center pt-4 border-t dark:border-gray-700 flex-shrink-0 h-16">
              <div className="flex gap-2">
                <Button
                  variant="ghost"
                  onClick={() => {
                    onOpenChange(false)
                    onCreateNew()
                  }}
                  className="gap-2"
                  disabled={!isDbInitialized}
                >
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                  </svg>
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
              </div>
              <Button
                variant="outline"
                onClick={() => onOpenChange(false)}
                disabled={!currentProjectName}
                className="bg-transparent"
              >
                Fechar
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>

      <DeleteConfirmDialog
        open={deleteDialogOpen}
        onOpenChange={setDeleteDialogOpen}
        itemName={projectToDelete?.name}
        itemType="projeto"
        onConfirm={handleDelete}
        title={""}
      />

      <ImportProjectDialog
        open={importDialogOpen}
        onOpenChange={(open) => {
          setImportDialogOpen(open)
          if (!open) {
            onOpenChange(true)
          }
        } }
        isDbInitialized={isDbInitialized}
        storageAdapter={{
          ...storageAdapter,
          save: storageAdapter.save // Garante que a propriedade 'save' está presente
        }}
        availableTags={availableTags}
        onProjectCreate={(projectData) => {
          // Adapta ProjectData para o tipo esperado pelo ImportProjectDialog
          const { id, name, tags } = projectData
          onProjectLoad({
            id, name, tags,
            data: "",
            size: 0,
            createdAt: "",
            updatedAt: ""
          })
        } }
        onProjectsListUpdate={onProjectsListUpdate}
        onAvailableTagsUpdate={() => { } } // Isso precisaria ser passado do componente pai, se necessário
        generateProjectId={generateProjectId}
        onOpenProject={handleImportProject}
        />
      
      <InfoDialog
        open={infoDialogOpen}
        onOpenChange={setInfoDialogOpen}
        project={projectToEdit}
        onSave={handleSaveInfo}
        availableTags={availableTags}
        storageAdapter={storageAdapter}
      />
    </>
  )
}
