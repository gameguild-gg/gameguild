"use client"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { ArrowRight, Eye, Blocks, Plus, Search, Filter, MoreVertical, Calendar, Tag, User, Grid, List, LayoutGrid } from "lucide-react"
import Link from "next/link"
import React, { useState, useEffect, useRef } from 'react';
import { ProjectList } from "@/components/editor/extras/project-dialog/project-list"
import { ProjectSearchFilters } from "@/components/editor/extras/project-dialog/project-search-filters"
import { ProjectPagination } from "@/components/editor/extras/project-dialog/project-pagination"
import { AdvancedFilters } from "@/components/editor/extras/project-dialog/advanced-filters"
import { useProjectDialog } from "@/hooks/editor/use-project-dialog"
import { EnhancedStorageAdapter } from "@/lib/storage/editor/enhanced-storage-adapter"
import { toast } from "sonner"
import { DeleteConfirmDialog } from "@/components/editor/extras/dialogs/delete-confirm-dialog"

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
  const [showAdvancedFilters, setShowAdvancedFilters] = useState(false)

  // Delete confirmation
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)
  const [projectToDelete, setProjectToDelete] = useState<{ id: string; name: string } | null>(null)

  // Storage adapter
  const storageAdapter = {
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
        await dbStorage.current.save(id, name, data, tags)
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
        const projects = await dbStorage.current.searchProjects(searchTerm, tags, filterMode)
        return projects
      } catch (error) {
        console.error("Failed to search projects:", error)
        return []
      }
    },
  }

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
  } = useProjectDialog({ isDbInitialized, storageAdapter })

  const [showFilters, setShowFilters] = useState(false)

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

  const loadAvailableTags = async () => {
    try {
      const tags = await dbStorage.current.getAllTags()
      setAvailableTags(tags)
    } catch (error) {
      console.error("Failed to load tags:", error)
    }
  }

  // Filter projects based on search and advanced filters
  const additionalFilteredProjects = filteredProjects.filter(project => {
    const matchesAuthor = !authorFilter || "Miguel Moroni".toLowerCase().includes(authorFilter.toLowerCase())
    const matchesStatus = statusFilter === "all" || statusFilter === "draft" // All projects are drafts for now
    const matchesDateFrom = !dateFromFilter || new Date(project.updatedAt) >= new Date(dateFromFilter)
    const matchesDateTo = !dateToFilter || new Date(project.updatedAt) <= new Date(dateToFilter)
    
    return matchesAuthor && matchesStatus && matchesDateFrom && matchesDateTo
  })

  const totalPages = Math.ceil(additionalFilteredProjects.length / itemsPerPage)

  // Handle project actions
  const handleProjectOpen = async (projectId: string) => {
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
  }

  const handleProjectView = async (projectId: string) => {
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
  }

  const handleConfirmDelete = (projectId: string, projectName: string) => {
    setProjectToDelete({ id: projectId, name: projectName })
    setDeleteDialogOpen(true)
  }

  const handleDelete = async () => {
    if (!projectToDelete) return

    try {
      await storageAdapter.delete(projectToDelete.id)
      await loadAvailableTags() // Refresh tags

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
                  onClick={() => setShowFilters(!showFilters)}
                  className="gap-2"
                >
                  <Filter className="w-4 h-4" />
                  Filters
                </Button>
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
            
            {/* Search Bar */}
            <div className="relative">
              <Search className="w-4 h-4 absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400" />
              <Input
                placeholder="Search posts..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-10"
              />
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
            showFilters={showFilters}
            forceVerticalLayout={false}
          />

          {/* Advanced Filters */}
          <AdvancedFilters
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
            showAdvanced={showAdvancedFilters}
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
                    {searchTerm || selectedTags.length > 0 || showFilters ? 
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
            ) : viewMode === 'grid' ? (
              /* Grid View */
              <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-4">
                {additionalFilteredProjects.slice((currentPage - 1) * itemsPerPage, currentPage * itemsPerPage).map((project) => (
                  <div
                    key={project.id}
                    className="group relative flex h-40 cursor-pointer flex-col justify-between overflow-hidden rounded-lg border bg-card text-card-foreground shadow-sm transition-all duration-200 ease-in-out hover:shadow-md dark:border-gray-800 dark:hover:border-gray-700"
                  >
                    <div className="flex flex-col p-4">
                      <div className="mb-2 flex items-start justify-between">
                        <span
                          className="block truncate font-semibold text-gray-900 dark:text-gray-100"
                          title={project.name}
                        >
                          {project.name}
                        </span>
                        <div className={`flex items-center gap-1 text-xs ${
                          project.storageType === 'local' ? 'text-gray-600 dark:text-gray-400' :
                          project.storageType === 'gameguild-cloud' ? 'text-blue-600 dark:text-blue-400' :
                          'text-green-600 dark:text-green-400'
                        }`} title={`Stored ${project.storageType === 'local' ? 'locally' : project.storageType === 'gameguild-cloud' ? 'on GameGuild Cloud' : 'on Google Drive'}`}>
                          {project.storageType === 'local' ? '💾' : project.storageType === 'gameguild-cloud' ? '🏢' : '☁️'}
                          <span>{project.storageType === 'local' ? 'Local' : project.storageType === 'gameguild-cloud' ? 'GameGuild' : 'Drive'}</span>
                        </div>
                      </div>
                      {project.tags && project.tags.length > 0 && (
                        <div className="mb-3 flex flex-wrap gap-1" title={project.tags.join(", ")}>
                          {project.tags.slice(0, 3).map((tag) => (
                            <span
                              key={tag}
                              className="inline-flex items-center rounded-md bg-blue-50 px-2 py-0.5 text-xs font-medium text-blue-700 ring-1 ring-inset ring-blue-700/10 dark:bg-blue-900/50 dark:text-blue-300 dark:ring-blue-700/30"
                            >
                              {tag}
                            </span>
                          ))}
                          {project.tags.length > 3 && (
                            <span className="text-xs text-gray-500 dark:text-gray-400">+{project.tags.length - 3}</span>
                          )}
                        </div>
                      )}
                      <div className="mt-auto text-xs text-gray-500 dark:text-gray-400">
                        <span>{(project.size / 1024).toFixed(1)}KB</span>
                        <span className="mx-1.5">•</span>
                        <span>Updated {new Date(project.updatedAt).toLocaleDateString()}</span>
                      </div>
                    </div>
                    <div className="absolute bottom-2 right-2 flex items-center gap-1 opacity-0 transition-opacity duration-200 group-hover:opacity-100">
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={(e) => {
                          e.stopPropagation()
                          handleProjectOpen(project.id)
                        }}
                        className="h-7 w-7 text-gray-500 hover:bg-gray-100 hover:text-blue-600 dark:hover:bg-gray-800"
                        title="Open in Studio"
                      >
                        <Blocks className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={(e) => {
                          e.stopPropagation()
                          handleProjectView(project.id)
                        }}
                        className="h-7 w-7 text-gray-500 hover:bg-gray-100 hover:text-purple-600 dark:hover:bg-gray-800"
                        title="Open in Viewer"
                      >
                        <Eye className="h-4 w-4" />
                      </Button>
                      {handleDownload && (
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={(e) => {
                            e.stopPropagation()
                            handleDownload(
                              project.id,
                              project.name,
                              project.data,
                              project.tags,
                              project.createdAt,
                              project.updatedAt
                            )
                          }}
                          className="h-7 w-7 text-gray-500 hover:bg-gray-100 hover:text-green-600 dark:hover:bg-gray-800"
                          title="Download project"
                        >
                          <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                          </svg>
                        </Button>
                      )}
                      {handleConfirmDelete && (
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={(e) => {
                            e.stopPropagation()
                            handleConfirmDelete(project.id, project.name)
                          }}
                          className="h-7 w-7 text-gray-500 hover:bg-gray-100 hover:text-red-600 dark:hover:bg-gray-800"
                          title="Delete project"
                        >
                          <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                          </svg>
                        </Button>
                      )}
                    </div>
                    <div className="absolute top-2 right-2 text-xs font-mono text-gray-400/50 dark:text-gray-500/50">
                      {project.id.slice(0, 8)}
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              /* List View */
              <div className="space-y-4">
                {additionalFilteredProjects.slice((currentPage - 1) * itemsPerPage, currentPage * itemsPerPage).map((project) => (
                  <Card key={project.id} className="group hover:shadow-md transition-all duration-200">
                    <CardContent className="p-6">
                      <div className="flex items-center justify-between">
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center gap-3 mb-2">
                            <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors truncate">
                              {project.name}
                            </h3>
                            <div className="flex gap-1 flex-shrink-0">
                              {project.tags.slice(0, 2).map((tag) => (
                                <span
                                  key={tag}
                                  className="inline-flex items-center rounded-md bg-blue-50 px-2 py-1 text-xs font-medium text-blue-700 ring-1 ring-inset ring-blue-700/10 dark:bg-blue-900/50 dark:text-blue-300"
                                >
                                  {tag}
                                </span>
                              ))}
                            </div>
                          </div>
                          <div className="flex items-center gap-4 text-sm text-gray-500 dark:text-gray-400">
                            <span className="flex items-center gap-1">
                              <User className="w-4 h-4" />
                              Miguel Moroni
                            </span>
                            <span className="flex items-center gap-1">
                              <Calendar className="w-4 h-4" />
                              {new Date(project.updatedAt).toLocaleDateString()}
                            </span>
                            <span className={`px-2 py-1 rounded-full text-xs ${
                              project.storageType === 'local' ? 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-200' :
                              project.storageType === 'gameguild-cloud' ? 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200' :
                              'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200'
                            }`}>
                              Draft
                            </span>
                          </div>
                        </div>
                        
                        {/* Action Buttons */}
                        <div className="flex items-center gap-2 opacity-0 group-hover:opacity-100 transition-opacity duration-200">
                          <Button
                            onClick={() => handleProjectOpen(project.id)}
                            size="sm"
                            className="bg-blue-600 hover:bg-blue-700 text-white"
                          >
                            <Blocks className="w-4 h-4 mr-2" />
                            Studio
                          </Button>
                          <Button
                            onClick={() => handleProjectView(project.id)}
                            size="sm"
                            variant="outline"
                            className="border-purple-200 text-purple-700 hover:bg-purple-50 dark:border-purple-700 dark:text-purple-300 dark:hover:bg-purple-900/20"
                          >
                            <Eye className="w-4 h-4 mr-2" />
                            Viewer
                          </Button>
                          <Button
                            size="sm"
                            variant="ghost"
                            className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
                          >
                            <MoreVertical className="w-4 h-4" />
                          </Button>
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
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
        open={deleteDialogOpen}
        onOpenChange={setDeleteDialogOpen}
        itemName={projectToDelete?.name}
        itemType="projeto"
        onConfirm={handleDelete}
        title="Confirmar Exclusão"
      />
    </div>
  )
}
