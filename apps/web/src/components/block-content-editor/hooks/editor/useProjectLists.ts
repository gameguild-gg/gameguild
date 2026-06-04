"use client"

/**
 * useProjectLists
 *
 * Reads-only view of "all projects" and "all tags" from the DB. Refresh
 * callbacks are exposed so operations (save / saveAs / delete) can prompt
 * a reload.
 */

import { useCallback, useEffect, useState } from "react"
import type { EnhancedStorageAdapter } from "@/components/block-content-editor/lib/storage/editor/enhanced-storage-adapter"
import type { ProjectData } from "@/components/block-content-editor/lib/storage/editor/project-data"

export interface UseProjectListsReturn {
  savedProjects: ProjectData[]
  availableTags: Array<{ name: string; usageCount: number }>
  refreshProjects: () => Promise<void>
  refreshTags: () => Promise<void>
}

export function useProjectLists(
  db: EnhancedStorageAdapter,
  isDbInitialized: boolean,
): UseProjectListsReturn {
  const [savedProjects, setSavedProjects] = useState<ProjectData[]>([])
  const [availableTags, setAvailableTags] = useState<Array<{ name: string; usageCount: number }>>([])

  const refreshProjects = useCallback(async () => {
    if (!isDbInitialized) return
    try { setSavedProjects(await db.list()) }
    catch (error) { console.error("Failed to load projects list:", error) }
  }, [db, isDbInitialized])

  const refreshTags = useCallback(async () => {
    try { setAvailableTags(await db.getAllTags()) }
    catch (error) { console.error("Failed to load tags:", error) }
  }, [db])

  useEffect(() => {
    if (!isDbInitialized) return
    refreshProjects()
    refreshTags()
  }, [isDbInitialized, refreshProjects, refreshTags])

  return { savedProjects, availableTags, refreshProjects, refreshTags }
}
