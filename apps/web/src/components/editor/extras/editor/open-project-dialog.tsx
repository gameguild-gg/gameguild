"use client"

import type React from "react"
import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"
import { DeleteConfirmDialog } from "@/components/editor/extras/dialogs/delete-confirm-dialog"
import { ProjectSearchFilters } from "@/components/editor/extras/project-dialog/project-search-filters"
import { ProjectList } from "@/components/editor/extras/project-dialog/project-list"
import { ProjectPagination } from "@/components/editor/extras/project-dialog/project-pagination"
import { useProjectDialog } from "@/hooks/editor/use-project-dialog"
import { useProjectActions } from "@/hooks/editor/use-project-actions"
import { FolderOpen, Upload, Cloud } from "lucide-react"
import { useState } from "react"
import { toast } from "sonner"
import type { LexicalEditor } from "lexical"
import { ImportProjectDialog } from "./import-project-dialog"
import { InfoDialog } from "./info-dialog"
import type { StorageOption } from "./storage-option-selector"
import { GoogleDriveAuthDialog } from "./google-drive-auth-dialog"
import { useGoogleDriveAuth } from "@/hooks/editor/use-google-drive-auth"
import type { ProjectPreferences } from "@/lib/storage/editor/enhanced-storage-adapter"

interface ProjectData {
  id: string
  name: string
  type: "type1" | "type2" | "type3"
  data: string
  tags: string[]
  size: number
  createdAt: string
  updatedAt: string
  storageType?: "local" | "gameguild-cloud" | "google-drive"
  isLocallyAvailable?: boolean
  preferences?: ProjectPreferences
}

interface StorageAdapter {
  save: (id: string, name: string, data: string, tags: string[], storageType?: StorageOption, preferences?: any, type?: "type1" | "type2" | "type3") => Promise<void>
  list: () => Promise<ProjectData[]>
  load: (id: string) => Promise<ProjectData | null>
  delete: (id: string) => Promise<void>
  searchProjects: (searchTerm: string, tags: string[], filterMode: "all" | "any", storageTypeFilter?: "local" | "gameguild-cloud" | "google-drive") => Promise<ProjectData[]>
}

interface OpenProjectDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  isFirstTime: boolean
  isDbInitialized: boolean
  storageAdapter: StorageAdapter
  availableTags: Array<{ name: string }>
  editorRef: React.RefObject<LexicalEditor | null>
  blockRefs: React.MutableRefObject<Record<string, LexicalEditor | null>>
  setLoadingRef: React.RefObject<((loading: boolean) => void) | null>
  onProjectLoad: (projectData: ProjectData) => void
  onProjectsListUpdate: () => void
  onCreateNew: (type: "type1" | "type2") => void
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
  blockRefs,
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

  const [importDialogOpen, setImportDialogOpen] = useState(false)
  const [googleDriveAuthDialogOpen, setGoogleDriveAuthDialogOpen] = useState(false)

  // Project actions (info, download, delete)
  const projectActions = useProjectActions({
    storageAdapter,
    onProjectsListUpdate,
    onProjectUpdate: async () => { onProjectsListUpdate() }
  })

  // Google Drive authentication hook
  const { isAuthenticated, isLoading, authenticate, signOut, refreshAuthState } = useGoogleDriveAuth()

  const handleOpen = async (projectId: string) => {
    const projectData = await loadProject(projectId)
    if (!projectData) {
      console.error("Project data not found")
      return
    }

    try {
      if (setLoadingRef.current) {
        setLoadingRef.current(true)
      }

      // Validate project data structure
      if (!projectData.data) {
        throw new Error("Project data is missing")
      }

      // First, notify parent to change layout type and let it handle loading
      onProjectLoad(projectData)
      onOpenChange(false)

      // Give React time to re-render with new layout type
      await new Promise((resolve) => setTimeout(resolve, 100))

      if (setLoadingRef.current) {
        setLoadingRef.current(false)
      }

      toast.success("Projeto carregado", {
        description: `"${projectData.name}" foi aberto com sucesso`,
        duration: 2500,
        icon: "📂",
      })
    } catch (error) {
      console.error("Failed to load project:", error, "Project data:", projectData)
      if (setLoadingRef.current) {
        setLoadingRef.current(false)
      }
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
          <DialogHeader className="shrink-0">
            <div className="flex items-center justify-between">
              <div>
                <DialogTitle>{isFirstTime ? "Welcome! Choose an Option" : "Open Project"}</DialogTitle>
                {isFirstTime && (
                  <p className="text-sm text-muted-foreground">
                    To get started, please open an existing project or create a new one.
                  </p>
                )}
              </div>
              
              {/* Google Drive Auth Button */}
              <div className="flex items-center gap-2">
                {isAuthenticated ? (
                  <div className="flex items-center gap-2">
                    <div className="flex items-center gap-1 text-xs text-green-600 dark:text-green-400">
                      <Cloud className="h-3 w-3" />
                      <span>Google Drive Connected</span>
                    </div>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={signOut}
                      className="text-xs text-gray-500 hover:text-red-600"
                      title="Disconnect Google Drive"
                    >
                      Disconnect
                    </Button>
                  </div>
                ) : (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setGoogleDriveAuthDialogOpen(true)}
                    disabled={isLoading}
                    className="gap-1 text-xs"
                    title="Connect to Google Drive to access your cloud projects"
                  >
                    <Cloud className="h-3 w-3" />
                    {isLoading ? "Connecting..." : "Connect Google Drive"}
                  </Button>
                )}
              </div>
            </div>
          </DialogHeader>

          <div className="flex flex-col flex-1 min-h-0 space-y-4">
            <div className="shrink-0">
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
            </div>



            <div className="flex-1 min-h-0 overflow-y-auto">
              <ProjectList
                projects={filteredProjects}
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
            </div>

            <div className="shrink-0 h-12 flex items-center justify-center">
              <ProjectPagination
                currentPage={currentPage}
                totalProjects={totalProjects}
                itemsPerPage={itemsPerPage}
                onPageChange={setCurrentPage}
              />
            </div>

            <div className="flex justify-between items-center pt-4 border-t dark:border-gray-700 shrink-0 h-16">
              <div className="flex gap-2">
                <Button
                  variant="ghost"
                  onClick={() => {
                    onOpenChange(false)
                    onCreateNew("type1")
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
            type: "type1",
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
        open={projectActions.infoDialogOpen}
        onOpenChange={projectActions.setInfoDialogOpen}
        project={projectActions.projectToEdit}
        onSave={projectActions.handleSaveInfo}
        availableTags={availableTags}
        storageAdapter={storageAdapter}
      />

      <GoogleDriveAuthDialog
        open={googleDriveAuthDialogOpen}
        onOpenChange={setGoogleDriveAuthDialogOpen}
        onAuthSuccess={() => {
          setGoogleDriveAuthDialogOpen(false)
          // Refresh auth state to ensure UI reflects the new authentication status
          refreshAuthState()
          // Refresh the projects list to include Google Drive projects
          onProjectsListUpdate()
          toast.success("Google Drive connected successfully!", {
            description: "You can now access your Google Drive projects.",
            duration: 3000,
            icon: "☁️",
          })
        }}
      />
    </>
  )
}
