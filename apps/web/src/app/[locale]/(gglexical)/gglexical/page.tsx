"use client"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Eye, Blocks, Plus, FolderOpen as FolderOpenIcon, Edit, Trash, Download, Info } from "lucide-react"
import Link from "next/link"
import React, { useState, useEffect, useRef, useMemo, useCallback } from 'react';
import { 
  ManagerLayout, 
  ManagerFilters, 
  GridView, 
  ListView,
  applySorting,
  type ManagerCard,
  type CardAction,
  type FilterConfig 
} from "@/components/editor/extras/manager-page"
import { useProjectDialog } from "@/hooks/editor/use-project-dialog"
import { useProjectActions } from "@/hooks/editor/use-project-actions"
import { EnhancedStorageAdapter } from "@/lib/storage/editor/enhanced-storage-adapter"
import { toast } from "sonner"
import { MediaUploadDialog } from "@/components/editor/extras/media-upload-dialog"
import { assetManager } from "@/lib/storage/assets/asset-manager"
import { DeleteConfirmDialog } from "@/components/editor/extras/dialogs/delete-confirm-dialog"
import { InfoDialog } from "@/components/editor/extras/editor/info-dialog"
import { ProjectPagination } from "@/components/editor/extras/project-dialog/project-pagination"
import { MIME_TYPES } from "@/lib/storage/assets/types"

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
  const [activeContext, setActiveContext] = useState<'projects' | 'assets' | 'collections'>('projects')
  
  // View state
  const [viewMode, setViewMode] = useState<'list' | 'grid'>('grid')
  const [gridColumns, setGridColumns] = useState(4)
  const [listColumns, setListColumns] = useState(1)
  
  // Database and storage
  const [isDbInitialized, setIsDbInitialized] = useState(false)
  const [availableTags, setAvailableTags] = useState<Array<{ name: string }>>([])
  const dbStorage = useRef<EnhancedStorageAdapter>(new EnhancedStorageAdapter())
  
  // Unified filters
  const [filters, setFilters] = useState<FilterConfig>({
    searchTerm: '',
    tags: [],
    tagFilterMode: 'all',
    storageType: 'all',
    mimeTypes: [],
    assetType: 'all',
    projectFilter: 'all',
    usageFilter: 'all',
    sortOrder: []
  })

  const [itemsPerPage, setItemsPerPage] = useState(24)
  const [currentPage, setCurrentPage] = useState(1)
  
  // Asset management states
  const [assets, setAssets] = useState<Array<{ id: string; name: string; mimeType: string; size: number; createdAt: string; projects?: string[]; type?: 'standard' | 'bundler' }>>([])
  const [uploadDialogOpen, setUploadDialogOpen] = useState(false)
  const [assetToDelete, setAssetToDelete] = useState<{ id: string; name: string; projects: string[] } | null>(null)
  const [assetToEdit, setAssetToEdit] = useState<{ id: string; name: string } | null>(null)
  const [newAssetName, setNewAssetName] = useState("")

  // Collection management states
  const [collections, setCollections] = useState<Array<{ id: string; name: string; description?: string; tags?: string[]; fileCount: number; totalSize: number; created: string; updated: string }>>([])
  const [collectionToDelete, setCollectionToDelete] = useState<{ id: string; name: string } | null>(null)
  const [collectionToEdit, setCollectionToEdit] = useState<{ id: string; name: string } | null>(null)
  const [newCollectionName, setNewCollectionName] = useState("")



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
    filteredProjects,
    totalProjects,
    tagFilterMode,
    setTagFilterMode,
    handleDownload,
    loadProject,
    refreshProjects,
  } = useProjectDialog({ 
    isDbInitialized, 
    storageAdapter
  })

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
      const assetList = await assetManager.listAssetsWithUsage()
      console.log("Assets loaded:", assetList)
      
      const assetsWithProjects = assetList.map((asset) => {
        return {
          id: asset.id,
          name: asset.name || asset.id,
          mimeType: asset.mimeType || 'application/octet-stream',
          size: asset.size || 0,
          createdAt: asset.createdAt || new Date().toISOString(),
          projects: asset.projects || [],
          type: asset.type || 'standard',
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

  // Load collections from assetManager
  const loadCollections = useCallback(async () => {
    try {
      console.log("Loading collections...")
      const collectionList = await assetManager.listCollections()
      console.log("Collections loaded:", collectionList)
      
      // Calculate file count and total size from manifest
      const collectionsWithDetails = await Promise.all(
        collectionList.map(async (collection) => {
          try {
            const manifest = await assetManager.getCollection(collection.id)
            if (manifest) {
              const fileCount = countFilesInStructure(manifest.structure)
              const totalSize = calculateTotalSize(manifest.structure)
              
              return {
                id: collection.id,
                name: collection.name,
                description: collection.description,
                tags: collection.tags,
                fileCount,
                totalSize,
                created: new Date(collection.created).toISOString(),
                updated: new Date(collection.updated).toISOString(),
              }
            }
          } catch (error) {
            console.error(`Failed to load manifest for collection ${collection.id}:`, error)
          }
          
          return {
            id: collection.id,
            name: collection.name,
            description: collection.description,
            tags: collection.tags,
            fileCount: 0,
            totalSize: 0,
            created: new Date(collection.created).toISOString(),
            updated: new Date(collection.updated).toISOString(),
          }
        })
      )
      
      setCollections(collectionsWithDetails)
      console.log("Collections state updated:", collectionsWithDetails)
    } catch (error) {
      console.error("Failed to load collections:", error)
      toast.error("Failed to load collections", {
        description: error instanceof Error ? error.message : "Unknown error"
      })
    }
  }, [])

  // Helper functions for collection details
  const countFilesInStructure = (structure: any): number => {
    let count = 0
    if (structure.files) count += structure.files.length
    if (structure.folders) {
      structure.folders.forEach((folder: any) => {
        count += countFilesInStructure(folder)
      })
    }
    return count
  }

  const calculateTotalSize = (structure: any): number => {
    let size = 0
    if (structure.files) {
      structure.files.forEach((file: any) => {
        if (file.content) {
          size += new TextEncoder().encode(file.content).length
        }
      })
    }
    if (structure.folders) {
      structure.folders.forEach((folder: any) => {
        size += calculateTotalSize(folder)
      })
    }
    return size
  }

  // Load collections when switching to collections context
  useEffect(() => {
    console.log("Collection context effect:", { activeContext, isDbInitialized })
    if (activeContext === 'collections' && isDbInitialized) {
      console.log("Loading collections now...")
      loadCollections()
    }
  }, [activeContext, isDbInitialized, loadCollections])

  // Load assets when switching to assets context
  useEffect(() => {
    console.log("Asset context effect:", { activeContext, isDbInitialized })
    if (activeContext === 'assets' && isDbInitialized) {
      console.log("Loading assets now...")
      loadAssets()
    }
  }, [activeContext, isDbInitialized, loadAssets])

  // Sync filters with legacy hooks
  useEffect(() => {
    if (filters.searchTerm !== searchTerm) setSearchTerm(filters.searchTerm)
  }, [filters.searchTerm, searchTerm, setSearchTerm])

  useEffect(() => {
    if (JSON.stringify(filters.tags) !== JSON.stringify(selectedTags)) {
      setSelectedTags(filters.tags || [])
    }
  }, [filters.tags, selectedTags, setSelectedTags])

  useEffect(() => {
    const storageType = filters.storageType === 'all' ? undefined : filters.storageType
    if (storageType !== storageTypeFilter) {
      setStorageTypeFilter(storageType)
    }
  }, [filters.storageType, storageTypeFilter, setStorageTypeFilter])

  useEffect(() => {
    if (filters.tagFilterMode && filters.tagFilterMode !== tagFilterMode) {
      setTagFilterMode(filters.tagFilterMode)
    }
  }, [filters.tagFilterMode, tagFilterMode, setTagFilterMode])

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

  // Filter projects based on unified filters
  const additionalFilteredProjects = useMemo(() => {
    return applySorting(filteredProjects, filters.sortOrder || [], 'updatedAt')
  }, [filteredProjects, filters.sortOrder])

  // Handle project actions - MEMOIZADAS para evitar recriação
  const handleProjectOpen = useCallback(async (projectId: string, event?: React.MouseEvent) => {
    try {
      toast.loading("Loading project...", { id: `loading-${projectId}` })
      
      const projectData = await loadProject(projectId)
      if (projectData) {
        // Store project data in localStorage for the studio to pick up
        localStorage.setItem('selectedProject', JSON.stringify(projectData))
        
        toast.success("Redirecting to Studio...", { 
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
      toast.error("Error loading project", { 
        id: `loading-${projectId}`,
        description: "Could not load the project"
      })
    }
  }, [loadProject])

  const handleProjectView = useCallback(async (projectId: string, event?: React.MouseEvent) => {
    try {
      toast.loading("Loading project...", { id: `loading-view-${projectId}` })
      
      const projectData = await loadProject(projectId)
      if (projectData) {
        // Store project data in localStorage for the viewer to pick up
        localStorage.setItem('selectedProject', JSON.stringify(projectData))
        
        toast.success("Redirecting to Viewer...", { 
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
      toast.error("Error loading project", { 
        id: `loading-view-${projectId}`,
        description: "Could not load the project"
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
    const filtered = assets.filter((asset) => {
      // Search filter - searches in both name and mimeType
      
      const matchesSearch = !filters.searchTerm || 
        asset.name.toLowerCase().includes(filters.searchTerm.toLowerCase()) ||
        asset.mimeType.toLowerCase().includes(filters.searchTerm.toLowerCase())
      
      // MIME type filter - now using extensions array mapped to MIME types
      let matchesMimeType = true
      if (filters.mimeTypes && filters.mimeTypes.length > 0) {
        // Convert selected extensions to their MIME types
        const selectedMimeTypes = filters.mimeTypes.map(ext => {
          const extWithoutDot = ext.substring(1) // Remove the leading dot
          return MIME_TYPES[extWithoutDot]
        }).filter(Boolean) // Remove undefined values
        
        // Check if asset's MIME type matches any of the selected MIME types
        // Also check by file extension as fallback
        const assetExt = '.' + asset.name.split('.').pop()?.toLowerCase()
        matchesMimeType = selectedMimeTypes.includes(asset.mimeType) || filters.mimeTypes.includes(assetExt)
      }
      
      // Asset type filter
      const assetType = asset.type || 'standard'
      const matchesAssetType = 
        filters.assetType === "all" || 
        (filters.assetType === "standard" && assetType === "standard") ||
        (filters.assetType === "bundler" && assetType === "bundler")
      
      // Project filter
      const matchesProject = !filters.projectFilter || filters.projectFilter === "all" || asset.projects?.includes(filters.projectFilter)
      
      // Usage filter
      const matchesUsage =
        filters.usageFilter === "all" ||
        (filters.usageFilter === "used" && asset.projects && asset.projects.length > 0) ||
        (filters.usageFilter === "unused" && (!asset.projects || asset.projects.length === 0))
      
      return matchesSearch && matchesMimeType && matchesAssetType && matchesProject && matchesUsage
    })
    
    // Apply sorting
    return applySorting(filtered, filters.sortOrder || [], 'createdAt')
  }, [assets, filters])

  const handleAssetDelete = useCallback((assetId: string, assetName: string) => {
    const asset = assets.find(a => a.id === assetId)
    setAssetToDelete({ 
      id: assetId, 
      name: assetName, 
      projects: asset?.projects || [] 
    })
  }, [assets])

  const handleAssetEdit = useCallback((assetId: string, currentName: string) => {
    setAssetToEdit({ id: assetId, name: currentName })
    setNewAssetName(currentName)
  }, [])

  const handleConfirmAssetDelete = useCallback(async () => {
    if (!assetToDelete) return
    
    try {
      // Delete asset completely (from index and store)
      await assetManager.deleteAssetCompletely(assetToDelete.id)
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
      // Rename the asset
      const success = await assetManager.renameAsset(assetToEdit.id, newAssetName.trim())
      
      if (success) {
        toast.success("Asset renamed successfully")
        await loadAssets()
        setAssetToEdit(null)
        setNewAssetName("")
      } else {
        toast.error("Failed to rename asset")
      }
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
      
      let downloadUrl: string
      let shouldRevokeUrl = false
      
      // Check if data is a data URL or plain text
      if (asset.data.startsWith('data:') || asset.data.startsWith('http')) {
        // Already a data URL or external URL, use directly
        downloadUrl = asset.data
      } else {
        // Plain text content (from collections), create a blob URL
        const mimeType = asset.metadata.mimeType || 'text/plain'
        const blob = new Blob([asset.data], { type: mimeType })
        downloadUrl = URL.createObjectURL(blob)
        shouldRevokeUrl = true
      }
      
      // Download the asset
      const link = document.createElement('a')
      link.href = downloadUrl
      link.download = assetName
      document.body.appendChild(link)
      link.click()
      document.body.removeChild(link)
      
      // Revoke blob URL if we created one
      if (shouldRevokeUrl) {
        setTimeout(() => URL.revokeObjectURL(downloadUrl), 100)
      }
      
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

  // Collection management functions
  const handleCollectionDelete = useCallback((collectionId: string, collectionName: string) => {
    setCollectionToDelete({ id: collectionId, name: collectionName })
  }, [])

  const handleCollectionEdit = useCallback((collectionId: string, currentName: string) => {
    setCollectionToEdit({ id: collectionId, name: currentName })
    setNewCollectionName(currentName)
  }, [])

  const handleConfirmCollectionDelete = useCallback(async () => {
    if (!collectionToDelete) return
    
    try {
      await assetManager.deleteCollection(collectionToDelete.id)
      toast.success("Collection deleted successfully")
      await loadCollections()
      setCollectionToDelete(null)
    } catch (error) {
      console.error("Failed to delete collection:", error)
      toast.error("Failed to delete collection")
    }
  }, [collectionToDelete, loadCollections])

  const handleConfirmCollectionEdit = useCallback(async () => {
    if (!collectionToEdit || !newCollectionName.trim()) return
    
    try {
      const success = await assetManager.renameCollection(collectionToEdit.id, newCollectionName.trim())
      
      if (success) {
        toast.success("Collection renamed successfully")
        await loadCollections()
        setCollectionToEdit(null)
        setNewCollectionName("")
      } else {
        toast.error("Failed to rename collection")
      }
    } catch (error) {
      console.error("Failed to rename collection:", error)
      toast.error("Failed to rename collection")
    }
  }, [collectionToEdit, newCollectionName, loadCollections])

  // Convert projects to ManagerCard format
  const projectCards: ManagerCard[] = useMemo(() => {
    const startIndex = (currentPage - 1) * itemsPerPage
    const endIndex = startIndex + itemsPerPage
    
    return additionalFilteredProjects.slice(startIndex, endIndex).map(project => ({
      type: 'project' as const,
      id: project.id,
      name: project.name,
      tags: project.tags,
      size: project.size,
      data: project.data,
      storageType: project.storageType,
      createdAt: project.createdAt,
      updatedAt: project.updatedAt,
    }))
  }, [additionalFilteredProjects, currentPage, itemsPerPage])

  // Convert assets to ManagerCard format
  const assetCards: ManagerCard[] = useMemo(() => {
    const startIndex = (currentPage - 1) * itemsPerPage
    const endIndex = startIndex + itemsPerPage
    
    return filteredAssets.slice(startIndex, endIndex).map(asset => ({
      type: 'asset' as const,
      id: asset.id,
      name: asset.name,
      mimeType: asset.mimeType,
      size: asset.size,
      projects: asset.projects,
      createdAt: asset.createdAt,
      updatedAt: asset.createdAt,
      assetType: asset.type,
    }))
  }, [filteredAssets, currentPage, itemsPerPage])

  // Filter and sort collections
  const filteredCollections = useMemo(() => {
    const filtered = collections.filter((collection) => {
      // Search filter
      const matchesSearch = !filters.searchTerm || 
        collection.name.toLowerCase().includes(filters.searchTerm.toLowerCase()) ||
        (collection.description && collection.description.toLowerCase().includes(filters.searchTerm.toLowerCase()))
      
      // Tags filter
      const matchesTags = !filters.tags || filters.tags.length === 0 || (
        collection.tags && (
          filters.tagFilterMode === 'all'
            ? filters.tags.every(tag => collection.tags?.includes(tag))
            : filters.tags.some(tag => collection.tags?.includes(tag))
        )
      )
      
      return matchesSearch && matchesTags
    })
    
    // Apply sorting
    return applySorting(filtered, filters.sortOrder || [], 'updated')
  }, [collections, filters])

  // Convert collections to ManagerCard format
  const collectionCards: ManagerCard[] = useMemo(() => {
    const startIndex = (currentPage - 1) * itemsPerPage
    const endIndex = startIndex + itemsPerPage
    
    return filteredCollections.slice(startIndex, endIndex).map(collection => ({
      type: 'collection' as const,
      id: collection.id,
      name: collection.name,
      description: collection.description,
      tags: collection.tags,
      fileCount: collection.fileCount,
      totalSize: collection.totalSize,
      createdAt: new Date(collection.created).toISOString(),
      updatedAt: new Date(collection.updated).toISOString(),
    }))
  }, [filteredCollections, currentPage, itemsPerPage])

  // Define card actions for projects
  const projectPrimaryActions: CardAction[] = useMemo(() => [
    {
      label: 'Open in Studio',
      icon: <Blocks className="h-4 w-4" />,
      onClick: (card) => handleProjectOpen(card.id),
    },
    {
      label: 'View',
      icon: <Eye className="h-4 w-4" />,
      onClick: (card) => handleProjectView(card.id),
    },
    {
      label: 'Information',
      icon: <Info className="h-4 w-4" />,
      onClick: async (card) => {
        const projectData = await loadProject(card.id)
        if (projectData) {
          projectActions.handleOpenInfo(projectData)
        }
      },
    },
  ], [handleProjectOpen, handleProjectView, loadProject, projectActions])

  const projectSecondaryActions: CardAction[] = useMemo(() => [
    {
      label: 'Download',
      icon: <Download className="h-4 w-4" />,
      onClick: (card) => {
        if (card.type === 'project') {
          handleProjectDownload(card.id, card.name, card.data, card.tags, card.createdAt, card.updatedAt)
        }
      },
    },
    {
      label: 'Delete',
      icon: <Trash className="h-4 w-4" />,
      onClick: (card) => projectActions.handleConfirmDelete(card.id, card.name),
      variant: 'destructive' as const,
    },
  ], [handleProjectDownload, projectActions])

  // Define card actions for assets
  const assetPrimaryActions: CardAction[] = useMemo(() => [
    {
      label: 'Download',
      icon: <Download className="h-4 w-4" />,
      onClick: (card) => handleAssetDownload(card.id, card.name),
    },
    {
      label: 'Rename',
      icon: <Edit className="h-4 w-4" />,
      onClick: (card) => handleAssetEdit(card.id, card.name),
    },
  ], [handleAssetDownload, handleAssetEdit])

  const assetSecondaryActions: CardAction[] = useMemo(() => [
    {
      label: 'Delete',
      icon: <Trash className="h-4 w-4" />,
      onClick: (card) => handleAssetDelete(card.id, card.name),
      variant: 'destructive' as const,
    },
  ], [handleAssetDelete])

  // Define card actions for collections
  const collectionPrimaryActions: CardAction[] = useMemo(() => [
    {
      label: 'Rename',
      icon: <Edit className="h-4 w-4" />,
      onClick: (card) => handleCollectionEdit(card.id, card.name),
    },
  ], [handleCollectionEdit])

  const collectionSecondaryActions: CardAction[] = useMemo(() => [
    {
      label: 'Delete',
      icon: <Trash className="h-4 w-4" />,
      onClick: (card) => handleCollectionDelete(card.id, card.name),
      variant: 'destructive' as const,
    },
  ], [handleCollectionDelete])

  const totalPages = Math.ceil(
    (activeContext === 'projects' ? additionalFilteredProjects.length : activeContext === 'assets' ? filteredAssets.length : filteredCollections.length) / itemsPerPage
  )

  return (
    <>
      <ManagerLayout
      activeContext={activeContext}
      viewMode={viewMode}
      gridColumns={gridColumns}
      listColumns={listColumns}
      onContextChange={setActiveContext}
      onViewModeChange={setViewMode}
      onGridColumnsChange={setGridColumns}
      onListColumnsChange={setListColumns}
      onCreateNew={() => {
        if (activeContext === 'projects') {
          window.location.href = '/gglexical/studio'
        } else {
          setUploadDialogOpen(true)
        }
      }}
      filterSection={
        <ManagerFilters
          filters={filters}
          onFilterChange={(newFilters) => setFilters({ ...filters, ...newFilters })}
          availableTags={availableTags}
          availableProjects={additionalFilteredProjects.map(p => ({ id: p.id, name: p.name }))}
          contextType={activeContext}
          itemsPerPage={itemsPerPage}
          onItemsPerPageChange={setItemsPerPage}
        />
      }
      paginationSection={
        totalPages > 1 ? (
          <ProjectPagination
            currentPage={currentPage}
            totalProjects={activeContext === 'projects' ? additionalFilteredProjects.length : activeContext === 'assets' ? filteredAssets.length : filteredCollections.length}
            itemsPerPage={itemsPerPage}
            onPageChange={setCurrentPage}
          />
        ) : undefined
      }
    >
      {!isDbInitialized ? (
        <div className="flex items-center justify-center h-64">
          <div className="text-center">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto mb-4"></div>
            <p className="text-gray-500 dark:text-gray-400">
              Loading {activeContext === 'projects' ? 'projects' : activeContext === 'assets' ? 'assets' : 'collections'}...
            </p>
          </div>
        </div>
      ) : activeContext === 'projects' ? (
        viewMode === 'grid' ? (
          <GridView
            cards={projectCards}
            columns={gridColumns}
            viewMode="grid"
            primaryActions={projectPrimaryActions}
            secondaryActions={projectSecondaryActions}
          />
        ) : (
          <ListView
            cards={projectCards}
            columns={listColumns}
            viewMode="list"
            primaryActions={projectPrimaryActions}
            secondaryActions={projectSecondaryActions}
          />
        )
      ) : activeContext === 'assets' ? (
        viewMode === 'grid' ? (
          <GridView
            cards={assetCards}
            columns={gridColumns}
            viewMode="grid"
            primaryActions={assetPrimaryActions}
            secondaryActions={assetSecondaryActions}
          />
        ) : (
          <ListView
            cards={assetCards}
            columns={listColumns}
            viewMode="list"
            primaryActions={assetPrimaryActions}
            secondaryActions={assetSecondaryActions}
          />
        )
      ) : (
        viewMode === 'grid' ? (
          <GridView
            cards={collectionCards}
            columns={gridColumns}
            viewMode="grid"
            primaryActions={collectionPrimaryActions}
            secondaryActions={collectionSecondaryActions}
          />
        ) : (
          <ListView
            cards={collectionCards}
            columns={listColumns}
            viewMode="list"
            primaryActions={collectionPrimaryActions}
            secondaryActions={collectionSecondaryActions}
          />
        )
      )}
    </ManagerLayout>

    {/* Delete Confirmation Dialog */}
    <DeleteConfirmDialog
      open={projectActions.deleteDialogOpen}
      onOpenChange={projectActions.setDeleteDialogOpen}
      itemName={projectActions.projectToDelete?.name}
      itemType="project"
      onConfirm={projectActions.handleDelete}
      title="Confirm Deletion"
    />

    {/* Asset Delete Confirmation Dialog */}
    <DeleteConfirmDialog
      open={!!assetToDelete}
      onOpenChange={(open) => !open && setAssetToDelete(null)}
      itemName={assetToDelete?.name}
      itemType="asset"
      onConfirm={handleConfirmAssetDelete}
      title="Confirm Asset Deletion"
      description={
        assetToDelete?.projects && assetToDelete.projects.length > 0
          ? `This asset is used by ${assetToDelete.projects.length} project${assetToDelete.projects.length > 1 ? 's' : ''}${assetToDelete.projects.length <= 5 ? ': ' + assetToDelete.projects.map(pid => additionalFilteredProjects.find(p => p.id === pid)?.name || pid).join(', ') : ''}. Deleting it will affect all projects that use it.`
          : `Are you sure you want to delete "${assetToDelete?.name}"? This action cannot be undone.`
      }
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
  
      <DeleteConfirmDialog
        open={projectActions.deleteDialogOpen}
        onOpenChange={projectActions.setDeleteDialogOpen}
        itemName={projectActions.projectToDelete?.name}
        itemType="project"
        onConfirm={projectActions.handleDelete}
        title="Confirm Deletion"
      />

      {/* Asset Delete Confirmation Dialog */}
      <DeleteConfirmDialog
        open={!!assetToDelete}
        onOpenChange={(open) => !open && setAssetToDelete(null)}
        itemName={assetToDelete?.name}
        itemType="asset"
        onConfirm={handleConfirmAssetDelete}
        title="Confirm Asset Deletion"
        description={
          assetToDelete?.projects && assetToDelete.projects.length > 0
            ? `This asset is used by ${assetToDelete.projects.length} project${assetToDelete.projects.length > 1 ? 's' : ''}${assetToDelete.projects.length <= 5 ? ': ' + assetToDelete.projects.map(pid => additionalFilteredProjects.find(p => p.id === pid)?.name || pid).join(', ') : ''}. Deleting it will affect all projects that use it.`
            : `Are you sure you want to delete "${assetToDelete?.name}"? This action cannot be undone.`
        }
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

      {/* Collection Delete Confirmation Dialog */}
      <DeleteConfirmDialog
        open={!!collectionToDelete}
        onOpenChange={(open) => !open && setCollectionToDelete(null)}
        itemName={collectionToDelete?.name}
        itemType="collection"
        onConfirm={handleConfirmCollectionDelete}
        title="Confirm Collection Deletion"
        description={`Are you sure you want to delete "${collectionToDelete?.name}"? This will not delete the individual assets, only the collection. This action cannot be undone.`}
      />

      {/* Collection Edit Dialog */}
      <Dialog open={!!collectionToEdit} onOpenChange={(open) => !open && setCollectionToEdit(null)}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Rename Collection</DialogTitle>
            <DialogDescription>
              Enter a new name for this collection.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <label htmlFor="collection-name" className="text-sm font-medium">
                Collection Name
              </label>
              <Input
                id="collection-name"
                value={newCollectionName}
                onChange={(e) => setNewCollectionName(e.target.value)}
                placeholder="Enter collection name"
                onKeyDown={(e) => {
                  if (e.key === 'Enter' && newCollectionName.trim()) {
                    handleConfirmCollectionEdit()
                  }
                }}
              />
            </div>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => {
                setCollectionToEdit(null)
                setNewCollectionName("")
              }}
            >
              Cancel
            </Button>
            <Button
              onClick={handleConfirmCollectionEdit}
              disabled={!newCollectionName.trim()}
            >
              Rename
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}