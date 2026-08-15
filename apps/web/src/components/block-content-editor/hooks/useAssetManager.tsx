"use client"

import { useState, useEffect, useMemo, useCallback } from 'react'
import { Download, Edit, Trash } from "lucide-react"
import { toAssetUri } from "@game-guild/assets"
import { getDefaultBrowserAssetRepository } from "@game-guild/assets/browser"
import {
  applySorting,
  type ManagerCard,
  type CardAction,
  type FilterConfig,
} from "@/components/block-content-editor/extras/manager-page"
import { toast } from "sonner"

const assetRepository = getDefaultBrowserAssetRepository()

interface AssetItem {
  id: string
  name: string
  mimeType: string
  size: number
  createdAt: string
  projects?: string[]
  type?: 'standard' | 'bundler'
}

interface UseAssetManagerProps {
  isDbInitialized: boolean
  activeContext: 'projects' | 'assets' | 'collections'
  filters: FilterConfig
  currentPage: number
  itemsPerPage: number
  additionalFilteredProjects: Array<{ id: string; name: string }>
}

export interface UseAssetManagerReturn {
  assetCards: ManagerCard[]
  assetPrimaryActions: CardAction[]
  assetSecondaryActions: CardAction[]
  filteredCount: number
  uploadDialogOpen: boolean
  setUploadDialogOpen: (open: boolean) => void
  assetToDelete: { id: string; name: string; projects: string[] } | null
  setAssetToDelete: (asset: { id: string; name: string; projects: string[] } | null) => void
  assetToEdit: { id: string; name: string } | null
  setAssetToEdit: (asset: { id: string; name: string } | null) => void
  newAssetName: string
  setNewAssetName: (name: string) => void
  handleConfirmAssetDelete: () => Promise<void>
  handleConfirmAssetEdit: () => Promise<void>
  handleUploadComplete: () => Promise<void>
}

export function useAssetManager({
  isDbInitialized,
  activeContext,
  filters,
  currentPage,
  itemsPerPage,
  additionalFilteredProjects,
}: UseAssetManagerProps): UseAssetManagerReturn {
  const [assets, setAssets] = useState<AssetItem[]>([])
  const [uploadDialogOpen, setUploadDialogOpen] = useState(false)
  const [assetToDelete, setAssetToDelete] = useState<{ id: string; name: string; projects: string[] } | null>(null)
  const [assetToEdit, setAssetToEdit] = useState<{ id: string; name: string } | null>(null)
  const [newAssetName, setNewAssetName] = useState("")

  const loadAssets = useCallback(async () => {
    try {
      const assetList = (await assetRepository.list({ limit: 200 })).items

      const assetsWithProjects = assetList.map((asset) => ({
        id: asset.uri,
        name: asset.name,
        mimeType: asset.mimeType || 'application/octet-stream',
        size: asset.size || 0,
        createdAt: asset.createdAt || new Date().toISOString(),
        projects: [],
        type: 'standard' as const,
      }))

      setAssets(assetsWithProjects)
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

  // Filter and sort assets
  const filteredAssets = useMemo(() => {
    const filtered = assets.filter((asset) => {
      const matchesSearch = !filters.searchTerm ||
        asset.name.toLowerCase().includes(filters.searchTerm.toLowerCase()) ||
        asset.mimeType.toLowerCase().includes(filters.searchTerm.toLowerCase())

      let matchesMimeType = true
      if (filters.mimeTypes && filters.mimeTypes.length > 0) {
        const assetExt = '.' + asset.name.split('.').pop()?.toLowerCase()
        matchesMimeType = filters.mimeTypes.includes(assetExt)
      }

      const assetType = asset.type || 'standard'
      const matchesAssetType =
        filters.assetType === "all" ||
        (filters.assetType === "standard" && assetType === "standard") ||
        (filters.assetType === "bundler" && assetType === "bundler")

      const matchesProject = !filters.projectFilter || filters.projectFilter === "all" || asset.projects?.includes(filters.projectFilter)

      const matchesUsage =
        filters.usageFilter === "all" ||
        (filters.usageFilter === "used" && asset.projects && asset.projects.length > 0) ||
        (filters.usageFilter === "unused" && (!asset.projects || asset.projects.length === 0))

      return matchesSearch && matchesMimeType && matchesAssetType && matchesProject && matchesUsage
    })

    return applySorting(filtered, filters.sortOrder || [], 'createdAt')
  }, [assets, filters])

  // CRUD handlers
  const handleAssetDelete = useCallback((assetId: string, assetName: string) => {
    const asset = assets.find(a => a.id === assetId)
    setAssetToDelete({ id: assetId, name: assetName, projects: asset?.projects || [] })
  }, [assets])

  const handleAssetEdit = useCallback((assetId: string, currentName: string) => {
    setAssetToEdit({ id: assetId, name: currentName })
    setNewAssetName(currentName)
  }, [])

  const handleConfirmAssetDelete = useCallback(async () => {
    if (!assetToDelete) return
    try {
      await assetRepository.remove(toAssetUri(assetToDelete.id), { force: true })
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
      await assetRepository.rename(toAssetUri(assetToEdit.id), newAssetName.trim())
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
      const resolved = await assetRepository.createObjectUrl(toAssetUri(assetId))

      const link = document.createElement('a')
      link.href = resolved.url
      link.download = assetName
      document.body.appendChild(link)
      link.click()
      document.body.removeChild(link)

      setTimeout(resolved.release, 100)

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

  // Convert to ManagerCard format
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

  // Card actions
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

  return {
    assetCards,
    assetPrimaryActions,
    assetSecondaryActions,
    filteredCount: filteredAssets.length,
    uploadDialogOpen,
    setUploadDialogOpen,
    assetToDelete,
    setAssetToDelete,
    assetToEdit,
    setAssetToEdit,
    newAssetName,
    setNewAssetName,
    handleConfirmAssetDelete,
    handleConfirmAssetEdit,
    handleUploadComplete,
  }
}
