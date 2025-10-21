"use client"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { ArrowRight, Eye, Blocks, Plus, Search, MoreVertical, Calendar, Tag, User, Grid, List, LayoutGrid } from "lucide-react"
import Link from "next/link"
import React, { useState, useEffect, useRef, useMemo, useCallback } from 'react';
import { ProjectList } from "@/components/editor/extras/project-dialog/project-list"
import { ProjectSearchFilters } from "@/components/editor/extras/project-dialog/project-search-filters"
import { ProjectPagination } from "@/components/editor/extras/project-dialog/project-pagination"
import { useProjectDialog } from "@/hooks/editor/use-project-dialog"
import { useProjectActions } from "@/hooks/editor/use-project-actions"
import { EnhancedStorageAdapter } from "@/lib/storage/editor/enhanced-storage-adapter"
import { toast } from "sonner"
import { DeleteConfirmDialog } from "@/components/editor/extras/dialogs/delete-confirm-dialog"
import { InfoDialog } from "@/components/editor/extras/editor/info-dialog"

interface ProjectData {
  id: string
  name: string
  data: string
  tags: string[]
  size: number
  createdAt: string
  updatedAt: string
  storageType?: "local" | "gameguild-cloud" | "google-drive"
  isLocallyAvailable?: boolean
}

export default function HomePage() {
  // View state
  const [viewMode, setViewMode] = useState<'list' | 'grid'>('list')
  
  // Database and storage
  const [isDbInitialized, setIsDbInitialized] = useState(false)
  const [availableTags, setAvailableTags] = useState<Array<{ name: string }>>([])
  const dbStorage = useRef<EnhancedStorageAdapter>(new EnhancedStorageAdapter())
  
  // Advanced filters
  const [authorFilter, setAuthorFilter] = useState("")
  const [statusFilter, setStatusFilter] = useState<"all" | "draft" | "published" | "scheduled">("all")
  const [dateFromFilter, setDateFromFilter] = useState("")
  const [dateToFilter, setDateToFilter] = useState("")
  const [accessFilter, setAccessFilter] = useState<"all" | "all-access" | "all-authors">("all")
  const [showAdvancedFilters, setShowAdvancedFilters] = useState(false) // Mudado para false por padrão para economizar recursos



  // Storage adapter - MEMOIZADO para evitar recriação
  const storageAdapter = useMemo(() => ({
    load: async (id: string): Promise<ProjectData | null> => {
      if (!isDbInitialized) {
        throw new Error("Database not initialized")
      }
      try {
        const projectData = await dbStorage.current.load(id)
        return projectData
      } catch (error) {
        console.error("Failed to load project:", error)
        return null
      }
    },

    list: async (): Promise<ProjectData[]> => {
      if (!isDbInitialized) {
        return []
      }
      try {
        const projects = await dbStorage.current.list()
        return projects
      } catch (error) {
        console.error("Failed to list projects:", error)
        return []
      }
    },

    delete: async (id: string): Promise<void> => {
      if (!isDbInitialized) {
        throw new Error("Database not initialized")
      }
      try {
        await dbStorage.current.delete(id)
      } catch (error) {
        console.error("Failed to delete project:", error)
        throw error
      }
    },

    save: async (id: string, name: string, data: string, tags: string[], storageType?: "local" | "gameguild-cloud" | "google-drive") => {
      if (!isDbInitialized) {
        throw new Error("Database not initialized")
      }
      try {
        await dbStorage.current.save(id, name, data, tags, storageType)
      } catch (error) {
        console.error("Failed to save project:", error)
        throw error
      }
    },

    searchProjects: async (searchTerm: string, tags: string[], filterMode: "all" | "any", storageTypeFilter?: "local" | "gameguild-cloud" | "google-drive"): Promise<ProjectData[]> => {
      if (!isDbInitialized) {
        return []
      }
      try {
        const projects = await dbStorage.current.searchProjects(searchTerm, tags, filterMode, storageTypeFilter)
        return projects
      } catch (error) {
        console.error("Failed to search projects:", error)
        return []
      }
    },
  }), [isDbInitialized])

  // Use the project dialog hook for project management
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
    refreshProjects,
  } = useProjectDialog({ isDbInitialized, storageAdapter })

  // Initialize database
  useEffect(() => {
    const initializeDatabase = async () => {
      try {
        await dbStorage.current.init()
        setIsDbInitialized(true)
        await loadAvailableTags()
      } catch (error) {
        console.error("Failed to initialize database:", error)
        toast.error("Failed to initialize storage", {
          description: "Could not connect to local storage. Some features may not work.",
          duration: 5000,
        })
      }
    }

    initializeDatabase()
  }, [])

  const loadAvailableTags = useCallback(async () => {
    try {
      const tags = await dbStorage.current.getAllTags()
      setAvailableTags(tags)
    } catch (error) {
      console.error("Failed to load tags:", error)
    }
  }, [])

  // Função para atualizar lista após mudanças - MEMOIZADA
  const updateProjectsList = useCallback(async () => {
    await refreshProjects()
    await loadAvailableTags()
  }, [refreshProjects, loadAvailableTags])

  // Project actions (info, download, delete) - MEMOIZADO para evitar recriação
  const projectActions = useProjectActions({
    storageAdapter,
    onProjectsListUpdate: updateProjectsList,
    onProjectUpdate: updateProjectsList
  })

  // Filter projects based on search and advanced filters - MEMOIZADO para evitar recálculo desnecessário
  const additionalFilteredProjects = useMemo(() => {
    return filteredProjects.filter(project => {
      const matchesAuthor = !authorFilter || "Miguel".toLowerCase().includes(authorFilter.toLowerCase()) // Placeholder author check
      const matchesStatus = statusFilter === "all" || statusFilter === "draft" // All projects are drafts for now
      const matchesDateFrom = !dateFromFilter || new Date(project.updatedAt) >= new Date(dateFromFilter)
      const matchesDateTo = !dateToFilter || new Date(project.updatedAt) <= new Date(dateToFilter)
      
      return matchesAuthor && matchesStatus && matchesDateFrom && matchesDateTo
    })
  }, [filteredProjects, authorFilter, statusFilter, dateFromFilter, dateToFilter])

  const totalPages = Math.ceil(additionalFilteredProjects.length / itemsPerPage)

  // Handle project actions - MEMOIZADAS para evitar recriação
  const handleProjectOpen = useCallback(async (projectId: string) => {
    try {
      toast.loading("Carregando projeto...", { id: `loading-${projectId}` })
      
      const projectData = await loadProject(projectId)
      if (projectData) {
        // Store project data in localStorage for the studio to pick up
        localStorage.setItem('selectedProject', JSON.stringify(projectData))
        
        toast.success("Redirecionando para Studio...", { 
          id: `loading-${projectId}`,
          duration: 1000
        })
        
        // Small delay to show success message
        setTimeout(() => {
          window.location.href = `/gglexical/studio`
        }, 500)
      }
    } catch (error) {
      toast.error("Erro ao carregar projeto", { 
        id: `loading-${projectId}`,
        description: "Não foi possível carregar o projeto"
      })
    }
  }, [loadProject])

  const handleProjectView = useCallback(async (projectId: string) => {
    try {
      toast.loading("Carregando projeto...", { id: `loading-view-${projectId}` })
      
      const projectData = await loadProject(projectId)
      if (projectData) {
        // Store project data in localStorage for the viewer to pick up
        localStorage.setItem('selectedProject', JSON.stringify(projectData))
        
        toast.success("Redirecionando para Viewer...", { 
          id: `loading-view-${projectId}`,
          duration: 1000
        })
        
        // Small delay to show success message
        setTimeout(() => {
          window.location.href = `/gglexical/viewer`
        }, 500)
      }
    } catch (error) {
      toast.error("Erro ao carregar projeto", { 
        id: `loading-view-${projectId}`,
        description: "Não foi possível carregar o projeto"
      })
    }
  }, [loadProject])

  // Wrapper function to match ProjectList expected signature - MEMOIZADA
  const handleProjectDownload = useCallback((
    projectId: string,
    projectName: string,
    projectData: string,
    projectTags: string[],
    createdAt: string,
    updatedAt: string
  ) => {
    projectActions.handleDownload(
      projectId,
      projectName,
      projectData,
      projectTags,
      createdAt,
      updatedAt
    )
  }, [projectActions])

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      <div className="flex h-screen">
        {/* Left Sidebar */}
        <div className="w-64 bg-white dark:bg-gray-800 border-r border-gray-200 dark:border-gray-700 flex flex-col">
          {/* Logo/Header */}
          <div className="p-6 border-b border-gray-200 dark:border-gray-700">
            <div className="flex items-center gap-2">
              <div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center">
                <Blocks className="w-5 h-5 text-white" />
              </div>
              <h1 className="text-xl font-bold text-gray-900 dark:text-gray-100">GameGuild</h1>
            </div>
            <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">Content Platform</p>
          </div>

          {/* Navigation */}
          <div className="flex-1 p-4 space-y-2">
            <Button 
              asChild 
              className="w-full justify-start bg-blue-600 hover:bg-blue-700 text-white"
            >
              <Link href="/gglexical/studio">
                <Blocks className="w-4 h-4 mr-3" />
                Studio
              </Link>
            </Button>
            
            <Button 
              asChild 
              variant="ghost" 
              className="w-full justify-start hover:bg-gray-100 dark:hover:bg-gray-700"
            >
              <Link href="/gglexical/viewer">
                <Eye className="w-4 h-4 mr-3" />
                Viewer
              </Link>
            </Button>
          </div>

          {/* Footer */}
          <div className="p-4 border-t border-gray-200 dark:border-gray-700">
            <div className="flex items-center gap-2 text-xs text-gray-500 dark:text-gray-400">
              <div className="w-2 h-2 bg-green-500 rounded-full animate-pulse"></div>
              All systems operational
            </div>
          </div>
        </div>

        {/* Main Content */}
        <div className="flex-1 flex flex-col">
          {/* Top Header */}
          <div className="bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 p-6">
            <div className="flex items-center justify-between mb-4">
              <div>
                <h2 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Posts</h2>
                <p className="text-gray-600 dark:text-gray-400">
                  {additionalFilteredProjects.length} of {totalProjects} projects
                </p>
              </div>
              <div className="flex items-center gap-3">
                <div className="flex items-center border rounded-lg">
                  <Button
                    variant={viewMode === 'list' ? 'default' : 'ghost'}
                    size="sm"
                    onClick={() => setViewMode('list')}
                    className="rounded-r-none"
                  >
                    <List className="w-4 h-4" />
                  </Button>
                  <Button
                    variant={viewMode === 'grid' ? 'default' : 'ghost'}
                    size="sm"
                    onClick={() => setViewMode('grid')}
                    className="rounded-l-none"
                  >
                    <LayoutGrid className="w-4 h-4" />
                  </Button>
                </div>
                <select
                  className="rounded border bg-background px-3 py-2 text-sm border-gray-300 dark:border-gray-600"
                  defaultValue="newest"
                >
                  <option value="newest">Newest first</option>
                  <option value="oldest">Oldest first</option>
                  <option value="name">Name A-Z</option>
                  <option value="name-desc">Name Z-A</option>
                </select>
                <Button 
                  variant="outline"
                  onClick={() => setShowAdvancedFilters(!showAdvancedFilters)}
                  className="gap-2"
                >
                  <Calendar className="w-4 h-4" />
                  Advanced
                </Button>
                <Button className="gap-2 bg-blue-600 hover:bg-blue-700">
                  <Plus className="w-4 h-4" />
                  New post
                </Button>
              </div>
            </div>
          </div>

          {/* Filters */}
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
            forceVerticalLayout={false}
            // Advanced filters props
            authorFilter={authorFilter}
            onAuthorFilterChange={setAuthorFilter}
            statusFilter={statusFilter}
            onStatusFilterChange={setStatusFilter}
            dateFromFilter={dateFromFilter}
            onDateFromFilterChange={setDateFromFilter}
            dateToFilter={dateToFilter}
            onDateToFilterChange={setDateToFilter}
            accessFilter={accessFilter}
            onAccessFilterChange={setAccessFilter}
            showAdvancedFilters={showAdvancedFilters}
          />

          {/* Projects List */}
          <div className="flex-1 p-6">
            {!isDbInitialized ? (
              <div className="flex items-center justify-center h-64">
                <div className="text-center">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto mb-4"></div>
                  <p className="text-gray-500 dark:text-gray-400">Loading projects...</p>
                </div>
              </div>
            ) : additionalFilteredProjects.length === 0 ? (
              <div className="flex items-center justify-center h-64">
                <div className="text-center">
                  <Blocks className="w-12 h-12 text-gray-300 dark:text-gray-600 mx-auto mb-4" />
                  <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-2">No projects found</h3>
                  <p className="text-gray-500 dark:text-gray-400 mb-4">
                    {searchTerm || selectedTags.length > 0 ? 
                      "Try adjusting your search or filters" : 
                      "Create your first project to get started"
                    }
                  </p>
                  <Button asChild className="bg-blue-600 hover:bg-blue-700">
                    <Link href="/gglexical/studio">
                      <Plus className="w-4 h-4 mr-2" />
                      Create New Project
                    </Link>
                  </Button>
                </div>
              </div>
            ) : (
              <ProjectList
                projects={additionalFilteredProjects}
                currentPage={currentPage}
                itemsPerPage={itemsPerPage}
                searchTerm={searchTerm}
                selectedTags={selectedTags}
                viewMode={viewMode}
                onOpen={handleProjectOpen}
                onView={handleProjectView}
                onDelete={projectActions.handleConfirmDelete}
                onInfo={projectActions.handleOpenInfo}
                onDownload={handleProjectDownload}
                showDeleteButton={true}
                showStudioViewerButtons={true}
              />
            )}

            {/* Pagination */}
            {totalPages > 1 && (
              <div className="mt-6">
                <ProjectPagination
                  currentPage={currentPage}
                  totalProjects={additionalFilteredProjects.length}
                  itemsPerPage={itemsPerPage}
                  onPageChange={setCurrentPage}
                />
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Delete Confirmation Dialog */}
      <DeleteConfirmDialog
        open={projectActions.deleteDialogOpen}
        onOpenChange={projectActions.setDeleteDialogOpen}
        itemName={projectActions.projectToDelete?.name}
        itemType="projeto"
        onConfirm={projectActions.handleDelete}
        title="Confirmar Exclusão"
      />

      {/* Info Dialog */}
      <InfoDialog
        open={projectActions.infoDialogOpen}
        onOpenChange={projectActions.setInfoDialogOpen}
        project={projectActions.projectToEdit}
        onSave={projectActions.handleSaveInfo}
        availableTags={availableTags}
        storageAdapter={storageAdapter}
      />
    </div>
  )
}
