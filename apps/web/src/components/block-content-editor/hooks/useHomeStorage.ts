"use client"

import { useState, useEffect, useRef, useMemo, useCallback } from 'react'
import { EnhancedStorageAdapter } from "@/components/block-content-editor/lib/storage/editor/enhanced-storage-adapter"
import { getDefaultBrowserAssetRepository } from "@game-guild/assets/browser"
import type { ProjectData } from "@/components/block-content-editor/lib/storage/editor/project-data"
import type { ProjectPreferences } from "@/components/block-content-editor/lib/storage/editor/project-preferences"
import type { StorageType } from "@/components/block-content-editor/lib/storage/editor/storage-types"
import { toast } from "sonner"

const assetRepository = getDefaultBrowserAssetRepository()

export interface HomeStorageAdapter {
  load: (id: string) => Promise<ProjectData | null>
  list: () => Promise<ProjectData[]>
  delete: (id: string) => Promise<void>
  save: (id: string, name: string, data: string, tags: string[], storageType?: StorageType, preferences?: ProjectPreferences, type?: string, deps?: unknown, engine?: string) => Promise<void>
  searchProjects: (searchTerm: string, tags: string[], filterMode: "all" | "any", storageTypeFilter?: StorageType) => Promise<ProjectData[]>
}

import { generateProjectId } from "@/components/block-content-editor/lib/storage/editor/project-id"

export interface UseHomeStorageReturn {
  isDbInitialized: boolean
  availableTags: Array<{ name: string }>
  loadAvailableTags: () => Promise<void>
  storageAdapter: HomeStorageAdapter
  generateProjectId: () => string
}

export function useHomeStorage(): UseHomeStorageReturn {
  const [isDbInitialized, setIsDbInitialized] = useState(false)
  const [availableTags, setAvailableTags] = useState<Array<{ name: string }>>([])
  const dbStorage = useRef<EnhancedStorageAdapter>(new EnhancedStorageAdapter())

  const loadAvailableTags = useCallback(async () => {
    try {
      const tags = await dbStorage.current.getAllTags()
      setAvailableTags(tags)
    } catch (error) {
      console.error("Failed to load tags:", error)
    }
  }, [])

  // Initialize database
  useEffect(() => {
    const initializeDatabase = async () => {
      try {
        console.log("Initializing databases...")
        await dbStorage.current.init()
        await assetRepository.getStorageStatus()
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
  }, []) // eslint-disable-line react-hooks/exhaustive-deps

  const storageAdapter = useMemo<HomeStorageAdapter>(() => ({
    load: async (id: string): Promise<ProjectData | null> => {
      if (!isDbInitialized) throw new Error("Database not initialized")
      try {
        return await dbStorage.current.load(id)
      } catch (error) {
        console.error("Failed to load project:", error)
        return null
      }
    },

    list: async (): Promise<ProjectData[]> => {
      if (!isDbInitialized) return []
      try {
        return await dbStorage.current.list()
      } catch (error) {
        console.error("Failed to list projects:", error)
        return []
      }
    },

    delete: async (id: string): Promise<void> => {
      if (!isDbInitialized) throw new Error("Database not initialized")
      try {
        await dbStorage.current.delete(id)
      } catch (error) {
        console.error("Failed to delete project:", error)
        throw error
      }
    },

    save: async (id: string, name: string, data: string, tags: string[], storageType?: StorageType, preferences?: ProjectPreferences) => {
      if (!isDbInitialized) throw new Error("Database not initialized")
      try {
        await dbStorage.current.save(id, name, data, tags, storageType, preferences)
      } catch (error) {
        console.error("Failed to save project:", error)
        throw error
      }
    },

    searchProjects: async (searchTerm: string, tags: string[], filterMode: "all" | "any", storageTypeFilter?: StorageType): Promise<ProjectData[]> => {
      if (!isDbInitialized) return []
      try {
        return await dbStorage.current.searchProjects(searchTerm, tags, filterMode, storageTypeFilter)
      } catch (error) {
        console.error("Failed to search projects:", error)
        return []
      }
    },
  }), [isDbInitialized])

  return {
    isDbInitialized,
    availableTags,
    loadAvailableTags,
    storageAdapter,
    generateProjectId,
  }
}
