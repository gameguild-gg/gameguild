"use client"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Eye, Blocks, Plus, Calendar, List, LayoutGrid, FolderOpen, ImageIcon, Upload } from "lucide-react"
import Link from "next/link"
import React, { useState, useEffect, useRef, useMemo, useCallback } from 'react';
import { ProjectList } from "@/components/editor/extras/project-dialog/project-list"
import { ProjectSearchFilters } from "@/components/editor/extras/project-dialog/project-search-filters"
import { ProjectPagination } from "@/components/editor/extras/project-dialog/project-pagination"
import { useProjectDialog } from "@/hooks/editor/use-project-dialog"
import { useProjectActions } from "@/hooks/editor/use-project-actions"
import { EnhancedStorageAdapter } from "@/lib/storage/editor/enhanced-storage-adapter"
import { toast } from "sonner"
import { AssetList } from "@/components/editor/extras/asset-manager/asset-list"
import { AssetFilters } from "@/components/editor/extras/asset-manager/asset-filters"
import { MediaUploadDialog } from "@/components/editor/extras/media-upload-dialog"
import { assetManager } from "@/lib/storage/assets/asset-manager"
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
  // Active context/view
  const [activeContext, setActiveContext] = useState<'projects' | 'assets'>('projects')
  
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
  const [sortOrder, setSortOrder] = useState<"newest" | "oldest" | "name" | "name-desc">("newest")
  const [showAdvancedFilters, setShowAdvancedFilters] = useState(false) // Mudado para false por padrão para economizar recursos

  // Asset management states
  const [assets, setAssets] = useState<Array<{ id: string; name: string; mimeType: string; size: number; createdAt: string; projects?: string[] }>>([])
  const [assetSearchTerm, setAssetSearchTerm] = useState("")
  const [assetMimeTypeFilter, setAssetMimeTypeFilter] = useState("all")
  const [assetProjectFilter, setAssetProjectFilter] = useState("all")
  const [assetUsageFilter, setAssetUsageFilter] = useState<"all" | "used" | "unused">("all")
  const [assetItemsPerPage, setAssetItemsPerPage] = useState(24)
  const [assetCurrentPage, setAssetCurrentPage] = useState(1)
  const [uploadDialogOpen, setUploadDialogOpen] = useState(false)
  const [assetToDelete, setAssetToDelete] = useState<{ id: string; name: string } | null>(null)
  const [assetToEdit, setAssetToEdit] = useState<{ id: string; name: string } | null>(null)
  const [newAssetName, setNewAssetName] = useState("")



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
        console.log("Initializing databases...")
        await dbStorage.current.init()
        await assetManager.init()
        console.log("Databases initialized")
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

  // Load assets from assetManager
  const loadAssets = useCallback(async () => {
    try {
      console.log("Loading assets...")
      const assetList = await assetManager.listAssets()
      console.log("Assets loaded:", assetList)
      
      const assetsWithProjects = assetList.map((asset) => {
        return {
          id: asset.id,
          name: asset.name || asset.id,
          mimeType: asset.mimeType || 'application/octet-stream',
          size: asset.size || 0,
          createdAt: asset.createdAt || new Date().toISOString(),
          projects: [], // TODO: Implement project tracking
        }
      })
      
      setAssets(assetsWithProjects)
      console.log("Assets state updated:", assetsWithProjects)
    } catch (error) {
      console.error("Failed to load assets:", error)
      toast.error("Failed to load assets", {
        description: error instanceof Error ? error.message : "Unknown error"
      })
    }
  }, [])

  // Load assets when switching to assets context
  useEffect(() => {
    console.log("Asset context effect:", { activeContext, isDbInitialized })
    if (activeContext === 'assets' && isDbInitialized) {
      console.log("Loading assets now...")
      loadAssets()
    }
  }, [activeContext, isDbInitialized, loadAssets])

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
    const filtered = filteredProjects.filter(project => {
      const matchesAuthor = !authorFilter || "Miguel".toLowerCase().includes(authorFilter.toLowerCase()) // Placeholder author check
      const matchesStatus = statusFilter === "all" || statusFilter === "draft" // All projects are drafts for now
      const matchesDateFrom = !dateFromFilter || new Date(project.updatedAt) >= new Date(dateFromFilter)
      const matchesDateTo = !dateToFilter || new Date(project.updatedAt) <= new Date(dateToFilter)
      
      return matchesAuthor && matchesStatus && matchesDateFrom && matchesDateTo
    })

    // Apply sorting
    return filtered.sort((a, b) => {
      switch (sortOrder) {
        case "oldest":
          return new Date(a.updatedAt).getTime() - new Date(b.updatedAt).getTime()
        case "name":
          return a.name.toLowerCase().localeCompare(b.name.toLowerCase())
        case "name-desc":
          return b.name.toLowerCase().localeCompare(a.name.toLowerCase())
        case "newest":
        default:
          return new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime()
      }
    })
  }, [filteredProjects, authorFilter, statusFilter, dateFromFilter, dateToFilter, sortOrder])

  const totalPages = Math.ceil(additionalFilteredProjects.length / itemsPerPage)

  // Handle project actions - MEMOIZADAS para evitar recriação
  const handleProjectOpen = useCallback(async (projectId: string, event?: React.MouseEvent) => {
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
          if (event?.ctrlKey || event?.metaKey) {
            // Open in new tab if Ctrl/Cmd was pressed
            window.open(`/gglexical/studio`, '_blank')
          } else {
            // Navigate in current tab
            window.location.href = `/gglexical/studio`
          }
        }, 500)
      }
    } catch (error) {
      toast.error("Erro ao carregar projeto", { 
        id: `loading-${projectId}`,
        description: "Não foi possível carregar o projeto"
      })
    }
  }, [loadProject])

  const handleProjectView = useCallback(async (projectId: string, event?: React.MouseEvent) => {
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
          if (event?.ctrlKey || event?.metaKey) {
            // Open in new tab if Ctrl/Cmd was pressed
            window.open(`/gglexical/viewer`, '_blank')
          } else {
            // Navigate in current tab
            window.location.href = `/gglexical/viewer`
          }
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

  // Asset management functions
  const filteredAssets = useMemo(() => {
    return assets.filter((asset) => {
      // Search filter
      const matchesSearch = !assetSearchTerm || asset.name.toLowerCase().includes(assetSearchTerm.toLowerCase())
      
      // MIME type filter
      const matchesMimeType = assetMimeTypeFilter === "all" || asset.mimeType.startsWith(assetMimeTypeFilter + "/")
      
      // Project filter
      const matchesProject = assetProjectFilter === "all" || asset.projects?.includes(assetProjectFilter)
      
      // Usage filter
      const matchesUsage =
        assetUsageFilter === "all" ||
        (assetUsageFilter === "used" && asset.projects && asset.projects.length > 0) ||
        (assetUsageFilter === "unused" && (!asset.projects || asset.projects.length === 0))
      
      return matchesSearch && matchesMimeType && matchesProject && matchesUsage
    })
  }, [assets, assetSearchTerm, assetMimeTypeFilter, assetProjectFilter, assetUsageFilter])

  const handleAssetDelete = useCallback((assetId: string, assetName: string) => {
    setAssetToDelete({ id: assetId, name: assetName })
  }, [])

  const handleAssetEdit = useCallback((assetId: string, currentName: string) => {
    setAssetToEdit({ id: assetId, name: currentName })
    setNewAssetName(currentName)
  }, [])

  const handleConfirmAssetDelete = useCallback(async () => {
    if (!assetToDelete) return
    
    try {
      await assetManager.deleteAsset(assetToDelete.id)
      toast.success("Asset deleted successfully")
      await loadAssets()
      setAssetToDelete(null)
    } catch (error) {
      console.error("Failed to delete asset:", error)
      toast.error("Failed to delete asset")
    }
  }, [assetToDelete, loadAssets])

  const handleConfirmAssetEdit = useCallback(async () => {
    if (!assetToEdit || !newAssetName.trim()) return
    
    try {
      const assetData = await assetManager.getAsset(assetToEdit.id)
      if (!assetData) {
        toast.error("Asset not found")
        return
      }
      
      // Update the asset name in metadata
      assetData.metadata.name = newAssetName.trim()
      
      // Save the updated asset back to IndexedDB
      // We need to access the private method through a workaround
      // Instead, we'll delete and recreate with new name
      const blob = await fetch(assetData.data).then(r => r.blob())
      const file = new File([blob], newAssetName.trim(), { type: assetData.metadata.mimeType })
      
      // Delete old asset
      await assetManager.deleteAsset(assetToEdit.id)
      
      // Save with new name
      await assetManager.saveAsset({ file })
      
      toast.success("Asset renamed successfully")
      await loadAssets()
      setAssetToEdit(null)
      setNewAssetName("")
    } catch (error) {
      console.error("Failed to rename asset:", error)
      toast.error("Failed to rename asset")
    }
  }, [assetToEdit, newAssetName, loadAssets])

  const handleAssetDownload = useCallback(async (assetId: string, assetName: string) => {
    try {
      const asset = await assetManager.getAsset(assetId)
      if (!asset || !asset.data) {
        toast.error("Asset not found")
        return
      }
      
      // Download the asset
      const link = document.createElement('a')
      link.href = asset.data
      link.download = assetName
      document.body.appendChild(link)
      link.click()
      document.body.removeChild(link)
      
      toast.success("Asset downloaded successfully")
    } catch (error) {
      console.error("Failed to download asset:", error)
      toast.error("Failed to download asset")
    }
  }, [])

  const handleUploadComplete = useCallback(async () => {
    await loadAssets()
    setUploadDialogOpen(false)
  }, [loadAssets])

  const assetTotalPages = Math.ceil(filteredAssets.length / assetItemsPerPage)

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

            {/* Context Buttons */}
            <div className="pt-4 space-y-2">
              <p className="text-xs text-gray-500 dark:text-gray-400 px-3 mb-2">MANAGER</p>
              <Button
                variant={activeContext === 'projects' ? 'secondary' : 'ghost'}
                className="w-full justify-start"
                onClick={() => setActiveContext('projects')}
              >
                <FolderOpen className="w-4 h-4 mr-3" />
                Projects
              </Button>
              <Button
                variant={activeContext === 'assets' ? 'secondary' : 'ghost'}
                className="w-full justify-start"
                onClick={() => setActiveContext('assets')}
              >
                <ImageIcon className="w-4 h-4 mr-3" />
                Assets
              </Button>
            </div>
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
                <h2 className="text-2xl font-bold text-gray-900 dark:text-gray-100">
                  {activeContext === 'projects' ? 'Projects' : 'Assets'}
                </h2>
                <p className="text-gray-600 dark:text-gray-400">
                  {activeContext === 'projects' 
                    ? `${additionalFilteredProjects.length} of ${totalProjects} projects`
                    : `${filteredAssets.length} of ${assets.length} assets`
                  }
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
                {activeContext === 'projects' && (
                  <>
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
                      New project
                    </Button>
                  </>
                )}
                {activeContext === 'assets' && (
                  <Button 
                    className="gap-2 bg-blue-600 hover:bg-blue-700"
                    onClick={() => setUploadDialogOpen(true)}
                  >
                    <Upload className="w-4 h-4" />
                    Upload Assets
                  </Button>
                )}
              </div>
            </div>
          </div>

          {/* Filters */}
          {activeContext === 'projects' ? (
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
              sortOrder={sortOrder}
              onSortOrderChange={setSortOrder}
              showAdvancedFilters={showAdvancedFilters}
            />
          ) : (
            <AssetFilters
              searchTerm={assetSearchTerm}
              onSearchChange={setAssetSearchTerm}
              mimeTypeFilter={assetMimeTypeFilter}
              onMimeTypeFilterChange={setAssetMimeTypeFilter}
              projectFilter={assetProjectFilter}
              onProjectFilterChange={setAssetProjectFilter}
              usageFilter={assetUsageFilter}
              onUsageFilterChange={setAssetUsageFilter}
              itemsPerPage={assetItemsPerPage}
              onItemsPerPageChange={setAssetItemsPerPage}
              availableProjects={additionalFilteredProjects.map(p => ({ id: p.id, name: p.name }))}
            />
          )}

          {/* Projects List */}
          <div className="flex-1 p-6">
            {!isDbInitialized ? (
              <div className="flex items-center justify-center h-64">
                <div className="text-center">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto mb-4"></div>
                  <p className="text-gray-500 dark:text-gray-400">
                    Loading {activeContext === 'projects' ? 'projects' : 'assets'}...
                  </p>
                </div>
              </div>
            ) : activeContext === 'projects' ? (
              // Projects View
              additionalFilteredProjects.length === 0 ? (
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
                <>
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
                    showDeleteButton={true}
                    showStudioViewerButtons={true}
                  />

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
                </>
              )
            ) : (
              // Assets View
              filteredAssets.length === 0 ? (
                <div className="flex items-center justify-center h-64">
                  <div className="text-center">
                    <ImageIcon className="w-12 h-12 text-gray-300 dark:text-gray-600 mx-auto mb-4" />
                    <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-2">No assets found</h3>
                    <p className="text-gray-500 dark:text-gray-400 mb-4">
                      {assetSearchTerm || assetMimeTypeFilter !== 'all' ? 
                        "Try adjusting your search or filters" : 
                        "Upload your first asset to get started"
                      }
                    </p>
                    <Button 
                      onClick={() => setUploadDialogOpen(true)}
                      className="bg-blue-600 hover:bg-blue-700"
                    >
                      <Upload className="w-4 h-4 mr-2" />
                      Upload Assets
                    </Button>
                  </div>
                </div>
              ) : (
                <>
                  <AssetList
                    assets={filteredAssets.slice(
                      (assetCurrentPage - 1) * assetItemsPerPage,
                      assetCurrentPage * assetItemsPerPage
                    )}
                    viewMode={viewMode}
                    onDelete={handleAssetDelete}
                    onDownload={handleAssetDownload}
                    onEdit={handleAssetEdit}
                  />

                  {/* Pagination for Assets */}
                  {assetTotalPages > 1 && (
                    <div className="mt-6">
                      <ProjectPagination
                        currentPage={assetCurrentPage}
                        totalProjects={filteredAssets.length}
                        itemsPerPage={assetItemsPerPage}
                        onPageChange={setAssetCurrentPage}
                      />
                    </div>
                  )}
                </>
              )
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

      {/* Asset Delete Confirmation Dialog */}
      <DeleteConfirmDialog
        open={!!assetToDelete}
        onOpenChange={(open) => !open && setAssetToDelete(null)}
        itemName={assetToDelete?.name}
        itemType="asset"
        onConfirm={handleConfirmAssetDelete}
        title="Confirmar Exclusão de Asset"
        description="Este asset pode estar sendo usado em projetos. A exclusão afetará todos os projetos que o utilizam."
      />

      {/* Asset Edit Dialog */}
      <Dialog open={!!assetToEdit} onOpenChange={(open) => !open && setAssetToEdit(null)}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Rename Asset</DialogTitle>
            <DialogDescription>
              Enter a new name for this asset. The file extension will be preserved.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <label htmlFor="asset-name" className="text-sm font-medium">
                Asset Name
              </label>
              <Input
                id="asset-name"
                value={newAssetName}
                onChange={(e) => setNewAssetName(e.target.value)}
                placeholder="Enter asset name"
                onKeyDown={(e) => {
                  if (e.key === 'Enter' && newAssetName.trim()) {
                    handleConfirmAssetEdit()
                  }
                }}
              />
            </div>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => {
                setAssetToEdit(null)
                setNewAssetName("")
              }}
            >
              Cancel
            </Button>
            <Button
              onClick={handleConfirmAssetEdit}
              disabled={!newAssetName.trim()}
            >
              Rename
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Info Dialog */}
      <InfoDialog
        open={projectActions.infoDialogOpen}
        onOpenChange={projectActions.setInfoDialogOpen}
        project={projectActions.projectToEdit}
        onSave={projectActions.handleSaveInfo}
        availableTags={availableTags}
        storageAdapter={storageAdapter}
      />

      {/* Upload Assets Dialog */}
      <MediaUploadDialog
        open={uploadDialogOpen}
        onOpenChange={setUploadDialogOpen}
        onMediaSelected={handleUploadComplete}
        title="Upload Assets"
        sources={{ files: true }}
        multiple={true}
        compress={true}
        allowCompressionToggle={true}
        hideLocalAssets={true}
      />
    </div>
  )
}
