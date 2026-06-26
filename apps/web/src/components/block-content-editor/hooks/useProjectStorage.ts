"use client"

/**
 * useProjectStorage
 *
 * The main editor hook. Composes the focused sub-hooks under `hooks/editor/`
 * to expose a flat surface to the editor page. Owns only the cross-cutting
 * concerns that don't fit a single sub-hook:
 *
 *   1. The `storageAdapter` — a thin wrapper around the raw DB that gates
 *      every operation on `isDbInitialized`.
 *   2. The URL-hash bootstrap — when `window.location.hash` is a project id,
 *      load it as soon as the DB is ready.
 *   3. The auto-save effect — debounced, gated by `readOnlyRef.current`.
 *
 * Sub-hooks:
 *   - {@link useProjectDbInit}    DB singleton + init + readOnlyRef
 *   - {@link useProjectState}     id/name/tags/blocks/preferences state
 *   - {@link useProjectLists}     savedProjects / availableTags + refresh
 *   - {@link useProjectSizes}     projectSize / assetsSize / assets
 *   - {@link useProjectSync}      syncStats + autoSaveEnabled
 *   - {@link useProjectOperations} save / saveAs / load / create / title / snapshot
 *
 * See `docs/DATA-FLOW.md` ("Editor Flow — Write Path").
 */

import { useCallback, useEffect, useMemo, type Dispatch, type SetStateAction } from "react"
import { toast } from "sonner"
import {
  EnhancedStorageAdapter,
  type ProjectPreferences,
} from "@/components/block-content-editor/lib/storage/editor/enhanced-storage-adapter"
import { serializeProject } from "@/components/block-content-editor/lib/storage/editor/block-storage"

import type { BlockArray, BlockCellType } from "@/components/block-content-editor/lib/storage/editor/block-structure"
import type { ProjectType } from "@/components/block-content-editor/lib/storage/editor/project-types"
import type { StorageType } from "@/components/block-content-editor/lib/storage/editor/storage-types"
import type { SyncStats } from "@/components/block-content-editor/lib/sync/editor/sync-types"
import { checkSelectedProject as checkProject } from "@/components/block-content-editor/extras/editor/project-load-operations"

import { useProjectDbInit } from "./editor/useProjectDbInit"
import { useProjectState } from "./editor/useProjectState"
import { useProjectLists } from "./editor/useProjectLists"
import { useProjectSizes, type ProjectAsset } from "./editor/useProjectSizes"
import { useProjectSync } from "./editor/useProjectSync"
import { useProjectOperations } from "./editor/useProjectOperations"

// ─── Types ───────────────────────────────────────────────────────────────────

export type { ProjectData } from "@/components/block-content-editor/lib/storage/editor/project-data"
import type { ProjectData, ProjectMetadata } from "@/components/block-content-editor/lib/storage/editor/project-data"

export interface StorageAdapterInterface {
  save: (id: string, name: string, data: string, tags?: string[], storageType?: StorageType, preferences?: ProjectPreferences) => Promise<void>
  load: (id: string) => Promise<ProjectData | null>
  delete: (id: string) => Promise<void>
  list: () => Promise<ProjectData[]>
  getProjectInfo: (id: string) => Promise<Pick<ProjectMetadata, "size" | "createdAt" | "updatedAt"> | null>
  searchProjects: (searchTerm: string, tags: string[], filterMode?: "all" | "any", storageTypeFilter?: StorageType) => Promise<ProjectData[]>
}

export interface CreateProjectInput {
  id: string
  name: string
  tags: string[]
  storageType: StorageType
  preferences?: ProjectPreferences
}

export interface ProjectStorageDefaults {
  /** Refuse to hash-load projects whose type isn't allowed by the current page. */
  allowedProjectTypes?: ProjectType[]
  /**
   * Page-declared structural defaults. Used to fill the new project's
   * preferences on Save As when the current project has none (e.g. saving
   * from a fresh untouched editor).
   */
  projectType?: ProjectType
  singleBlockMode?: boolean
  allowedBlockTypes?: BlockCellType[]
}

export interface UseProjectStorageReturn {
  // Status
  isDbInitialized: boolean
  isFirstTime: boolean

  // Project metadata
  projectId: string
  projectName: string
  setProjectName: Dispatch<SetStateAction<string>>
  storageType: StorageType
  tags: string[]
  setTags: Dispatch<SetStateAction<string[]>>
  preferences: ProjectPreferences | undefined
  setPreferences: (p: ProjectPreferences) => void

  // Editor content
  blocks: BlockArray
  setBlocks: Dispatch<SetStateAction<BlockArray>>

  // Operations
  save(): Promise<{ needsSaveAs: boolean }>
  saveAs(name: string, storageOption: StorageType, tags?: string[]): Promise<void>
  loadProject(projectData: ProjectData): void
  createProject(projectData: CreateProjectInput): void

  // Title
  titleEdit(setEditingProjectName: (n: string) => void, setIsEditingTitle: (b: boolean) => void): void
  titleSave(editingName: string, setEditingProjectName: (n: string) => void, setIsEditingTitle: (b: boolean) => void): Promise<void>

  // Snapshot
  createSnapshot(name?: string): Promise<void>

  // ID generation
  generateProjectId(): string

  // Lists
  savedProjects: ProjectData[]
  availableTags: Array<{ name: string; usageCount: number }>
  refreshProjects(): Promise<void>
  refreshTags(): Promise<void>

  // Size
  projectSize: number
  assetsSize: number
  assets: ProjectAsset[]

  // Sync
  syncStats: SyncStats | null

  // Auto-save
  autoSaveEnabled: boolean
  setAutoSaveEnabled: Dispatch<SetStateAction<boolean>>
  lastProjectLoadTime: number

  // Direct DB access (for history hook, import dialog, etc.)
  db: EnhancedStorageAdapter

  // Storage adapter (for components that need it)
  storageAdapter: StorageAdapterInterface

  // Read-only gate (set by page when viewing history — suppresses auto-save)
  readOnlyRef: React.MutableRefObject<boolean>
}

// ─── Hook ────────────────────────────────────────────────────────────────────

export function useProjectStorage(initialDefaults?: ProjectStorageDefaults): UseProjectStorageReturn {
  const { db, isDbInitialized, readOnlyRef } = useProjectDbInit()
  const state = useProjectState()
  const lists = useProjectLists(db, isDbInitialized)
  const sizes = useProjectSizes(state.blocks, state.projectId, isDbInitialized, db)
  const sync = useProjectSync(db, isDbInitialized)

  // ─── Storage adapter (gated wrapper around the raw DB) ───────────────────

  const storageAdapter: StorageAdapterInterface = useMemo(() => ({
    save: async (id, name, data, tags = [], storageType = "local", preferences?) => {
      if (!id || !name || !data) { console.warn("Invalid id, name or data"); return }
      if (!isDbInitialized) throw new Error("Database not initialized")
      try { await db.save(id, name, data, tags, storageType, preferences) }
      catch (error) { console.error("Failed to save project:", error); throw error }
    },
    load: async (id) => {
      if (!isDbInitialized) throw new Error("Database not initialized")
      try { return await db.load(id) }
      catch (error) { console.error("Failed to load project:", error); return null }
    },
    delete: async (id) => {
      if (!isDbInitialized) throw new Error("Database not initialized")
      try { await db.delete(id) }
      catch (error) { console.error("Failed to delete project:", error); throw error }
    },
    list: async () => {
      if (!isDbInitialized) return []
      try { return await db.list() }
      catch (error) { console.error("Failed to list projects:", error); return [] }
    },
    getProjectInfo: async (id) => {
      if (!isDbInitialized) return null
      try { return await db.getProjectInfo(id) }
      catch (error) { console.error("Failed to get project info:", error); return null }
    },
    searchProjects: async (searchTerm, tags, filterMode = "any", storageTypeFilter?) => {
      if (!isDbInitialized) return []
      try { return await db.searchProjects(searchTerm, tags, filterMode, storageTypeFilter) }
      catch (error) { console.error("Failed to search projects:", error); return [] }
    },
  }), [db, isDbInitialized])

  const operations = useProjectOperations({
    state,
    storageAdapter,
    db,
    refreshProjects: lists.refreshProjects,
    recalcAssets: sizes.recalcAssets,
    defaults: {
      projectType: initialDefaults?.projectType,
      singleBlockMode: initialDefaults?.singleBlockMode,
      allowedBlockTypes: initialDefaults?.allowedBlockTypes,
    },
  })

  // ─── URL hash bootstrap — load project from #hash on DB init ─────────────

  useEffect(() => {
    if (!isDbInitialized) return
    checkProject({
      storageAdapter,
      directDbLoad: (id: string) => db.load(id),
      setCurrentProjectId: state.setProjectId,
      setCurrentProjectName: state.setProjectName,
      setCurrentProjectStorageType: state.setStorageType,
      setProjectTags: state.setTags,
      setIsFirstTime: state.setIsFirstTime,
      setLastProjectLoadTime: state.setLastProjectLoadTime,
      setCurrentProjectPreferences: state.setPreferences,
      setBlocks: state.setBlocks,
      allowedProjectTypes: initialDefaults?.allowedProjectTypes,
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isDbInitialized])

  // ─── Auto-save effect — debounced, gated by readOnlyRef ──────────────────

  useEffect(() => {
    if (!sync.autoSaveEnabled || !state.projectId || !isDbInitialized || readOnlyRef.current) return
    if (state.blocks.length === 0) return

    const timeSinceLoad = Date.now() - state.lastProjectLoadTime
    if (timeSinceLoad < 1000) return

    const autoSaveTimer = setTimeout(async () => {
      try {
        const dataToSave = serializeProject(state.blocks)
        await storageAdapter.save(
          state.projectId, state.projectName, dataToSave, state.tags,
          state.storageType, state.preferences,
        )
        toast.success("Auto-saved", {
          description: "Changes saved automatically", duration: 1500, icon: "💾",
          style: { opacity: 0.8, fontSize: "0.875rem" },
        })
      } catch (error) {
        console.error("Auto-save failed:", error)
        toast.error("Auto-save failed", { description: "Save manually to ensure", duration: 2000, icon: "⚠️" })
      }
    }, 2000)

    return () => clearTimeout(autoSaveTimer)
  }, [
    state.blocks, sync.autoSaveEnabled, state.projectId, state.projectName,
    state.tags, isDbInitialized, state.storageType, state.lastProjectLoadTime,
    state.preferences, storageAdapter, readOnlyRef,
  ])

  // ─── Setter adapter — preferences needs (p) => void, state has Dispatch ──

  const setPreferences = useCallback((p: ProjectPreferences) => {
    state.setPreferences(p)
  }, [state])

  // ─── Flat return ─────────────────────────────────────────────────────────

  return {
    // Status
    isDbInitialized,
    isFirstTime: state.isFirstTime,

    // Project metadata
    projectId: state.projectId,
    projectName: state.projectName,
    setProjectName: state.setProjectName,
    storageType: state.storageType,
    tags: state.tags,
    setTags: state.setTags,
    preferences: state.preferences,
    setPreferences,

    // Editor content
    blocks: state.blocks,
    setBlocks: state.setBlocks,

    // Operations
    save: operations.save,
    saveAs: operations.saveAs,
    loadProject: operations.loadProject,
    createProject: operations.createProject,

    // Title
    titleEdit: operations.titleEdit,
    titleSave: operations.titleSave,

    // Snapshot
    createSnapshot: operations.createSnapshot,

    // ID generation
    generateProjectId: operations.generateProjectId,

    // Lists
    savedProjects: lists.savedProjects,
    availableTags: lists.availableTags,
    refreshProjects: lists.refreshProjects,
    refreshTags: lists.refreshTags,

    // Size
    projectSize: sizes.projectSize,
    assetsSize: sizes.assetsSize,
    assets: sizes.assets,

    // Sync
    syncStats: sync.syncStats,

    // Auto-save
    autoSaveEnabled: sync.autoSaveEnabled,
    setAutoSaveEnabled: sync.setAutoSaveEnabled,
    lastProjectLoadTime: state.lastProjectLoadTime,

    // Direct DB access
    db,

    // Storage adapter
    storageAdapter,

    // Read-only gate
    readOnlyRef,
  }
}
