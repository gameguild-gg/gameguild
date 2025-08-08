"use client"

import type React from "react"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"
import { Label } from "@/components/ui/label"
import { DeleteConfirmDialog } from "@/components/editor/ui/delete-confirm-dialog"
import { FolderOpen, Trash2 } from 'lucide-react'
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
  const [tagSearchInput, setTagSearchInput] = useState("")
  const [showTagDropdown, setShowTagDropdown] = useState(false)
  const [tagFilterMode, setTagFilterMode] = useState<"all" | "any">("any")
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)
  const [projectToDelete, setProjectToDelete] = useState<{ id: string; name: string } | null>(null)

  // Format file size
  const formatSize = (sizeInKB: number): string => {
    if (sizeInKB < 1024) {
      return `${sizeInKB.toFixed(1)}KB`
    } else {
      return `${(sizeInKB / 1024).toFixed(1)}MB`
    }
  }

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

  // Close tag dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (showTagDropdown) {
        const target = event.target as Element
        if (!target.closest(".relative")) {
          setShowTagDropdown(false)
        }
      }
    }

    document.addEventListener("mousedown", handleClickOutside)
    return () => document.removeEventListener("mousedown", handleClickOutside)
  }, [showTagDropdown])

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
            <div className="space-y-3 p-4 bg-gray-50 dark:bg-gray-800 rounded-lg">

              {/* Search and Filter Section */}
              <div className="flex items-center justify-between">
                  <Label className="text-sm font-medium">Filter by Projects:</Label>
                  <div className="flex items-center gap-2">
                    <Label className="text-xs text-gray-700 dark:text-gray-400">Items per page:</Label>
                      <select
                        value={itemsPerPage}
                        onChange={(e) => setItemsPerPage(Number(e.target.value))}
                        className="px-2 py-1 border rounded text-sm bg-background"
                      >
                      <option value={3}>3</option>
                      <option value={6}>6</option>
                      <option value={12}>12</option>
                      <option value={24}>24</option>
                  </select>
                  </div>
                </div>
                <div className="flex-1">
                  <Input
                    placeholder="Search projects by name..."
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    className="w-full"
                  />
                </div>

              {/* Tags Filter */}
              <div className="space-y-2 min-h-[15vh] border-b">
                <div className="flex items-center justify-between">
                  <Label className="text-sm font-medium">Filter by tags:</Label>
                  <div className="flex items-center gap-2">
                    <Label className="text-xs text-gray-700 dark:text-gray-400">Match tags:</Label>
                    <select
                      value={tagFilterMode}
                      onChange={(e) => setTagFilterMode(e.target.value as "all" | "any")}
                      className="px-2 py-1 border rounded text-xs bg-background"
                    >
                      <option value="any">Any tags</option>
                      <option value="all">All tags</option>
                    </select>
                  </div>
                </div>
                
                <div className="relative">
                  <div className="flex gap-2">
                    <div className="relative flex-1">
                      <Input
                        placeholder="Search tags or leave empty to see all..."
                        value={tagSearchInput}
                        onChange={(e) => {
                          setTagSearchInput(e.target.value)
                          setShowTagDropdown(true)
                        }}
                        onFocus={() => setShowTagDropdown(true)}
                        className="pr-10"
                      />
                      <button
                        type="button"
                        onClick={() => setShowTagDropdown(!showTagDropdown)}
                        className="absolute right-2 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
                      >
                        <svg
                          className={`w-4 h-4 transition-transform ${showTagDropdown ? "rotate-180" : ""}`}
                          fill="none"
                          stroke="currentColor"
                          viewBox="0 0 24 24"
                        >
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                        </svg>
                      </button>
                    </div>
                  </div>

                  {showTagDropdown && (
                    <div className="absolute z-10 w-full mt-1 bg-white dark:bg-gray-800 border dark:border-gray-700 rounded-md shadow-lg max-h-48 overflow-y-auto">
                      {availableTags.length === 0 ? (
                        <div className="px-3 py-2 text-sm text-gray-500 dark:text-gray-400">No tags available</div>
                      ) : (
                        (() => {
                          const filteredTags = tagSearchInput.trim()
                            ? availableTags.filter((tag) =>
                                tag.name.toLowerCase().includes(tagSearchInput.toLowerCase()),
                              )
                            : availableTags

                          return filteredTags.length === 0 ? (
                            <div className="px-3 py-2 text-sm text-gray-500 dark:text-gray-400">
                              No tags found matching "{tagSearchInput}"
                            </div>
                          ) : (
                            filteredTags.map((tag) => (
                              <button
                                key={tag.name}
                                type="button"
                                onClick={() => {
                                  setSelectedTags((prev) =>
                                    prev.includes(tag.name) ? prev.filter((t) => t !== tag.name) : [...prev, tag.name],
                                  )
                                }}
                                className="w-full px-3 py-2 text-left hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center justify-between transition-colors"
                              >
                                <div className="flex items-center gap-2">
                                  <div
                                    className={`w-4 h-4 border rounded flex items-center justify-center ${
                                      selectedTags.includes(tag.name)
                                        ? "bg-blue-500 border-blue-500"
                                        : "border-gray-300 dark:border-gray-600"
                                    }`}
                                  >
                                    {selectedTags.includes(tag.name) && (
                                      <svg
                                        className="w-3 h-3 text-white"
                                        fill="none"
                                        stroke="currentColor"
                                        viewBox="0 0 24 24"
                                      >
                                        <path
                                          strokeLinecap="round"
                                          strokeLinejoin="round"
                                          strokeWidth={2}
                                          d="M5 13l4 4L19 7"
                                        />
                                      </svg>
                                    )}
                                  </div>
                                  <span className="text-sm">{tag.name}</span>
                                </div>
                                <span className="text-xs text-gray-500 dark:text-gray-400">({tag.usageCount})</span>
                              </button>
                            ))
                          )
                        })()
                      )}
                    </div>
                  )}
                </div>

                {/* Selected tags display */}
                {selectedTags.length > 0 && (
                  <div className="space-y-2">
                    <div className="flex flex-wrap gap-2">
                      {selectedTags.map((tagName) => (
                        <span
                          key={tagName}
                          className="inline-flex items-center gap-1 px-2 py-1 bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200 text-xs rounded-full"
                        >
                          {tagName}
                          <button
                            type="button"
                            onClick={() => setSelectedTags((prev) => prev.filter((t) => t !== tagName))}
                            className="hover:text-blue-600 dark:hover:text-blue-300"
                          >
                            <svg className="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                              <path
                                strokeLinecap="round"
                                strokeLinejoin="round"
                                strokeWidth={2}
                                d="M6 18L18 6M6 6l12 12"
                              />
                            </svg>
                          </button>
                        </span>
                      ))}
                    </div>
                    <div className="flex items-center justify-between">
                      <button
                        type="button"
                        onClick={() => {
                          setSelectedTags([])
                          setTagSearchInput("")
                        }}
                        className="text-xs text-blue-600 hover:text-blue-700 dark:text-blue-400 dark:hover:text-blue-300"
                      >
                        Clear all filters
                      </button>
                      <span className="text-xs text-gray-500 dark:text-gray-400">
                        Showing projects with {tagFilterMode === "all" ? "all" : "any"} of these tags
                      </span>
                    </div>
                  </div>
                )}
              </div>
              

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
              

              {/* Projects List */}
              <div className="max-h-[30vh] overflow-y-auto">
                {filteredProjects.length === 0 ? (
                  <div className="text-center py-12">
                    <FolderOpen className="w-12 h-12 text-gray-300 dark:text-gray-600 mx-auto mb-3" />
                    <p className="text-sm text-gray-500 dark:text-gray-400">
                      {searchTerm || selectedTags.length > 0
                        ? "No projects found matching your criteria"
                        : "No saved projects found"}
                    </p>
                    <p className="text-xs text-gray-400 dark:text-gray-500 mt-1">
                      {searchTerm || selectedTags.length > 0
                        ? "Try adjusting your search or filters"
                        : "Create your first project to get started"}
                    </p>
                  </div>
                ) : (
                  <>
                    <div className="space-y-2">
                      {filteredProjects
                        .slice((currentPage - 1) * itemsPerPage, currentPage * itemsPerPage)
                        .map((project) => {
                          return (
                            <div
                              key={project.id}
                              className="flex items-center justify-between p-3 border dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors"
                            >
                              <div className="flex flex-col flex-1 min-w-0">
                                <div className="flex items-center gap-2 mb-1">
                                  <span className="text-sm font-medium text-gray-900 dark:text-gray-100 truncate">
                                    {project.name}
                                  </span>
                                  {project.tags && project.tags.length > 0 && (
                                    <div className="flex gap-1">
                                      {project.tags.slice(0, 3).map((tag) => (
                                        <span
                                          key={tag}
                                          className="inline-flex items-center px-1.5 py-0.5 rounded text-xs bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200"
                                        >
                                          {tag}
                                        </span>
                                      ))}
                                      {project.tags.length > 3 && (
                                        <span className="text-xs text-gray-500 dark:text-gray-400">
                                          +{project.tags.length - 3}
                                        </span>
                                      )}
                                    </div>
                                  )}
                                </div>
                                <div className="flex items-center gap-3 text-xs text-gray-500 dark:text-gray-400">
                                  <span>{formatSize(project.size)}</span>
                                  <span>•</span>
                                  <span>Updated {new Date(project.updatedAt).toLocaleDateString()}</span>
                                  <span>•</span>
                                  <span className="text-gray-400 dark:text-gray-500 font-mono text-xs">
                                    ID: {project.id.slice(0, 8)}...
                                  </span>
                                </div>
                              </div>
                              <div className="flex gap-1 ml-3">
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => handleOpen(project.id)}
                                  className="text-blue-600 hover:text-blue-700 dark:text-blue-400 dark:hover:text-blue-300"
                                >
                                  Open
                                </Button>
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => handleConfirmDelete(project.id, project.name)}
                                  className="text-red-600 hover:text-red-700 dark:text-red-400 dark:hover:text-red-300"
                                >
                                  <Trash2 className="w-4 h-4" />
                                </Button>
                              </div>
                            </div>
                          )
                        })}
                    </div>
                  </>
                )}
              </div>

              {/* Pagination */}
                    {Math.ceil(totalProjects / itemsPerPage) > 1 && (
                      <div className="flex items-center justify-between mt-4 pt-4 border-t dark:border-gray-700">
                        <div className="text-sm text-gray-500 dark:text-gray-400">
                          Showing {(currentPage - 1) * itemsPerPage + 1} to{" "}
                          {Math.min(currentPage * itemsPerPage, totalProjects)} of {totalProjects} projects
                        </div>
                        <div className="flex items-center gap-2">
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => setCurrentPage((prev) => Math.max(1, prev - 1))}
                            disabled={currentPage === 1}
                            className="bg-transparent"
                          >
                            Previous
                          </Button>
                          <div className="flex items-center gap-1">
                            {Array.from({ length: Math.ceil(totalProjects / itemsPerPage) }, (_, i) => i + 1)
                              .filter((page) => {
                                const totalPages = Math.ceil(totalProjects / itemsPerPage)
                                if (totalPages <= 7) return true
                                if (page === 1 || page === totalPages) return true
                                if (page >= currentPage - 1 && page <= currentPage + 1) return true
                                return false
                              })
                              .map((page, index, array) => (
                                <div key={page} className="flex items-center">
                                  {index > 0 && array[index - 1] !== page - 1 && (
                                    <span className="px-2 text-gray-400">...</span>
                                  )}
                                  <Button
                                    variant={currentPage === page ? "default" : "ghost"}
                                    size="sm"
                                    onClick={() => setCurrentPage(page)}
                                    className={currentPage === page ? "" : "bg-transparent"}
                                  >
                                    {page}
                                  </Button>
                                </div>
                              ))}
                          </div>
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() =>
                              setCurrentPage((prev) => Math.min(Math.ceil(totalProjects / itemsPerPage), prev + 1))
                            }
                            disabled={currentPage === Math.ceil(totalProjects / itemsPerPage)}
                            className="bg-transparent"
                          >
                            Next
                          </Button>
                        </div>
                      </div>
                    )}

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
          </div>
        </DialogContent>
      </Dialog>

      <DeleteConfirmDialog
        open={deleteDialogOpen}
        onOpenChange={setDeleteDialogOpen}
        itemName={projectToDelete?.name}
        itemType="projeto"
        onConfirm={handleDelete}
      />
    </>
  )
}
