"use client"

import React, { useState, useEffect, useMemo, useCallback } from 'react'
import { Eye, Blocks, Download, Trash, Info } from "lucide-react"
import type { StorageType } from "@/components/block-content-editor/lib/storage/editor/storage-types"
import type { ProjectPreferences } from "@/components/block-content-editor/lib/storage/editor/project-preferences"
import { useProjectDialog } from "@/components/block-content-editor/hooks/editor/use-project-dialog"
import { useProjectActions } from "@/components/block-content-editor/hooks/editor/use-project-actions"
import {
  applySorting,
  type ManagerCard,
  type CardAction,
  type FilterConfig,
} from "@/components/block-content-editor/extras/manager-page"
import { toast } from "sonner"
import type { HomeStorageAdapter } from "./useHomeStorage"
import type { ProjectData } from "@/components/block-content-editor/lib/storage/editor/project-data"
// Engine removed: all projects use the unified Blocks engine.

interface UseProjectManagerProps {
  isDbInitialized: boolean
  storageAdapter: HomeStorageAdapter
  availableTags: Array<{ name: string }>
  loadAvailableTags: () => Promise<void>
  filters: FilterConfig
  currentPage: number
  itemsPerPage: number
}

export interface UseProjectManagerReturn {
  projectCards: ManagerCard[]
  projectPrimaryActions: CardAction[]
  projectSecondaryActions: CardAction[]
  projectActions: ReturnType<typeof useProjectActions>
  filteredCount: number
  additionalFilteredProjects: ProjectData[]
  createDialogOpen: boolean
  setCreateDialogOpen: (open: boolean) => void
  handleCreateNewProject: () => void
  handleProjectCreate: (projectData: { id: string; name: string; tags: string[]; storageType: StorageType }) => void
  refreshProjects: () => Promise<void>
}

export function useProjectManager({
  isDbInitialized,
  storageAdapter,
  availableTags,
  loadAvailableTags,
  filters,
  currentPage,
  itemsPerPage,
}: UseProjectManagerProps): UseProjectManagerReturn {
  // New project dialog state
  const [createDialogOpen, setCreateDialogOpen] = useState(false)

  // Use the project dialog hook
  const {
    searchTerm,
    setSearchTerm,
    selectedTags,
    setSelectedTags,
    storageTypeFilter,
    setStorageTypeFilter,
    filteredProjects,
    tagFilterMode,
    setTagFilterMode,
    loadProject,
    refreshProjects,
  } = useProjectDialog({
    isDbInitialized,
    storageAdapter,
  })

  // Sync filters with useProjectDialog state
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

  const updateProjectsList = useCallback(async () => {
    await refreshProjects()
    await loadAvailableTags()
  }, [refreshProjects, loadAvailableTags])

  const projectActions = useProjectActions({
    storageAdapter,
    onProjectsListUpdate: updateProjectsList,
    onProjectUpdate: updateProjectsList,
  })

  // Filter and sort projects
  const additionalFilteredProjects = useMemo(() => {
    return applySorting(filteredProjects, filters.sortOrder || [], 'updatedAt')
  }, [filteredProjects, filters.sortOrder])

  // Navigation handlers
  const handleProjectOpen = useCallback(async (projectId: string, event?: React.MouseEvent) => {
    try {
      toast.loading("Loading project...", { id: `loading-${projectId}` })
      const projectData = await loadProject(projectId)
      if (projectData) {
        toast.success("Redirecting to Studio...", { id: `loading-${projectId}`, duration: 1000 })
        setTimeout(() => {
          const url = `/block-content-editor/studio#${projectId}`
          if (event?.ctrlKey || event?.metaKey) {
            window.open(url, '_blank')
          } else {
            window.location.href = url
          }
        }, 500)
      }
    } catch (error) {
      toast.error("Error loading project", { id: `loading-${projectId}`, description: "Could not load the project" })
    }
  }, [loadProject])

  const handleProjectView = useCallback(async (projectId: string, event?: React.MouseEvent) => {
    try {
      toast.loading("Loading project...", { id: `loading-view-${projectId}` })
      const projectData = await loadProject(projectId)
      if (projectData) {
        toast.success("Redirecting to Viewer...", { id: `loading-view-${projectId}`, duration: 1000 })
        setTimeout(() => {
          const url = `/block-content-editor/viewer#${projectId}`
          if (event?.ctrlKey || event?.metaKey) {
            window.open(url, '_blank')
          } else {
            window.location.href = url
          }
        }, 500)
      }
    } catch (error) {
      toast.error("Error loading project", { id: `loading-view-${projectId}`, description: "Could not load the project" })
    }
  }, [loadProject])

  const handleProjectDownload = useCallback((
    projectId: string,
    projectName: string,
    projectData: string,
    projectTags: string[],
    createdAt: string,
    updatedAt: string,
    projectPreferences?: ProjectPreferences
  ) => {
    projectActions.handleDownload(projectId, projectName, projectData, projectTags, createdAt, updatedAt, projectPreferences)
  }, [projectActions])

  // Convert to ManagerCard format
  const projectCards: ManagerCard[] = useMemo(() => {
    const startIndex = (currentPage - 1) * itemsPerPage
    const endIndex = startIndex + itemsPerPage
    return additionalFilteredProjects.slice(startIndex, endIndex).map(project => ({
      type: 'project' as const,
      id: project.id,
      name: project.name,
      tags: project.tags,
      size: project.metadata.size,
      data: project.data,
      storageType: project.storageType,
      createdAt: project.metadata.createdAt,
      updatedAt: project.metadata.updatedAt,
      projectType: project.preferences?.global?.projectType ?? 'general',
    }))
  }, [additionalFilteredProjects, currentPage, itemsPerPage])

  // Card actions
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

  // New project handlers
  const handleCreateNewProject = useCallback(() => {
    setCreateDialogOpen(true)
  }, [])

  const handleProjectCreate = useCallback((projectData: { id: string; name: string; tags: string[]; storageType: StorageType }) => {
    // Navigate to studio with the newly created project's ID
    window.location.href = `/block-content-editor/studio#${projectData.id}`
  }, [])

  return {
    projectCards,
    projectPrimaryActions,
    projectSecondaryActions,
    projectActions,
    filteredCount: additionalFilteredProjects.length,
    additionalFilteredProjects,
    createDialogOpen,
    setCreateDialogOpen,
    handleCreateNewProject,
    handleProjectCreate,
    refreshProjects,
  }
}
