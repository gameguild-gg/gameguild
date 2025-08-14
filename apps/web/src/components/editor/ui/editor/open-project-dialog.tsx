"use client"

import type React from "react"

import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"
import { DeleteConfirmDialog } from "@/components/editor/ui/delete-confirm-dialog"
import { ProjectSearchFilters } from "@/components/editor/ui/project-dialog/project-search-filters"
import { ProjectList } from "@/components/editor/ui/project-dialog/project-list"
import { ProjectPagination } from "@/components/editor/ui/project-dialog/project-pagination"
import { FolderOpen } from "lucide-react"
import { useState, useEffect } from "react"
import { toast } from "sonner"
import type { LexicalEditor } from "lexical"

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
  delete: (id: string) => Promise<void>
  searchProjects: (searchTerm: string, tags: string[], filterMode: "all" | "any") => Promise<ProjectData[]>
}

interface OpenProjectDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  isFirstTime: boolean
  isDbInitialized: boolean
  storageAdapter: StorageAdapter
  availableTags: Array<{ name: string; usageCount: number }>
  editorRef: React.RefObject<LexicalEditor | null>
  setLoadingRef: React.RefObject<((loading: boolean) => void) | null>
  onProjectLoad: (projectData: ProjectData) => void
  onProjectsListUpdate: () => void
  onCreateNew: () => void
  currentProjectName: string
  isStorageNearLimit: () => boolean
  getStorageUsagePercentage: () => number
  storageLimit: number | null
  totalStorageUsed: number
  formatStorageSize: (sizeInKB: number) => string
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
  isStorageNearLimit,
  getStorageUsagePercentage,
  storageLimit,
  totalStorageUsed,
  formatStorageSize,
}: OpenProjectDialogProps) {
  const [searchTerm, setSearchTerm] = useState("")
  const [selectedTags, setSelectedTags] = useState<string[]>([])
  const [currentPage, setCurrentPage] = useState(1)
  const [itemsPerPage, setItemsPerPage] = useState(10)
  const [filteredProjects, setFilteredProjects] = useState<ProjectData[]>([])
  const [totalProjects, setTotalProjects] = useState(0)
  const [tagFilterMode, setTagFilterMode] = useState<"all" | "any">("any")
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)
  const [projectToDelete, setProjectToDelete] = useState<{ id: string; name: string } | null>(null)

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
      if (projectData && editorRef.current) {
        try {
          // Ativar o estado de carregamento
          if (setLoadingRef.current) {
            setLoadingRef.current(true)
          }

          const editorState = editorRef.current.parseEditorState(projectData.data)
          editorRef.current.setEditorState(editorState)

          // Aguardar um pouco para que os nós sejam renderizados
          await new Promise((resolve) => setTimeout(resolve, 100))

          // Desativar o estado de carregamento
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
          // Desativar o estado de carregamento em caso de erro
          if (setLoadingRef.current) {
            setLoadingRef.current(false)
          }
          toast.error("Erro ao carregar projeto", {
            description: "O arquivo do projeto está corrompido ou em formato inválido",
            duration: 4000,
            icon: "❌",
          })
        }
      } else {
        toast.error("Projeto não encontrado", {
          description: "Não foi possível localizar o arquivo do projeto",
          duration: 3000,
          icon: "🔍",
        })
      }
    } catch (error) {
      console.error("Open error:", error)
      toast.error("Erro ao abrir projeto", {
        description: "Não foi possível abrir o projeto. Tente novamente.",
        duration: 4000,
        icon: "❌",
      })
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

      toast.success("Download iniciado", {
        description: `Arquivo "${projectName}.lexical" está sendo baixado`,
        duration: 2500,
        icon: "📥",
      })
    } catch (error) {
      console.error("Download error:", error)
      toast.error("Erro no download", {
        description: "Não foi possível baixar o arquivo. Tente novamente.",
        duration: 4000,
        icon: "❌",
      })
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
        <DialogContent className="max-w-2xl min-w-xl max-h-[80vh]" onInteractOutside={(e) => e.preventDefault()}>
          <DialogHeader>
            <DialogTitle>{isFirstTime ? "Welcome! Choose an Option" : "Open Project"}</DialogTitle>
            {isFirstTime && (
              <p className="text-sm text-muted-foreground">
                To get started, please open an existing project or create a new one.
              </p>
            )}
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

            {isStorageNearLimit() && (
              <div className="p-3 bg-amber-50 dark:bg-amber-900 border border-amber-200 dark:border-amber-700 rounded-lg mb-4">
                <div className="flex items-center gap-2 mb-1">
                  <svg className="w-4 h-4 text-amber-600 dark:text-amber-400" fill="currentColor" viewBox="0 0 20 20">
                    <path
                      fillRule="evenodd"
                      d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z"
                      clipRule="evenodd"
                    />
                  </svg>
                  <span className="text-sm font-medium text-amber-800 dark:text-amber-200">
                    Armazenamento quase cheio
                  </span>
                </div>
                <p className="text-xs text-amber-700 dark:text-amber-300">
                  Uso: {getStorageUsagePercentage().toFixed(1)}% ({formatStorageSize(totalStorageUsed)} de{" "}
                  {storageLimit}MB). Criação de novos projetos bloqueada para preservar margem de salvamento.
                </p>
              </div>
            )}

            <ProjectList
              projects={filteredProjects}
              currentPage={currentPage}
              itemsPerPage={itemsPerPage}
              searchTerm={searchTerm}
              selectedTags={selectedTags}
              onOpen={handleOpen}
              onDelete={handleConfirmDelete}
              onDownload={handleDownload} // Added download prop
              showDeleteButton={true}
              openButtonText="Open"
            />

            <ProjectPagination
              currentPage={currentPage}
              totalProjects={totalProjects}
              itemsPerPage={itemsPerPage}
              onPageChange={setCurrentPage}
            />

            <div className="flex justify-between items-center pt-4 border-t dark:border-gray-700">
              {isStorageNearLimit() ? (
                <div className="flex flex-col">
                  <Button variant="ghost" disabled className="gap-2 opacity-50 cursor-not-allowed">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                    </svg>
                    Novo Projeto
                  </Button>
                  <span className="text-xs text-red-600 dark:text-red-400 mt-1">
                    Armazenamento quase cheio ({getStorageUsagePercentage().toFixed(1)}%)
                  </span>
                </div>
              ) : (
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
              )}
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
    </>
  )
}
