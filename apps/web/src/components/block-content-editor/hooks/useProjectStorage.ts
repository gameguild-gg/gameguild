"use client"

/**
 * useProjectStorage
 *
 * The main editor hook. Owns all project state (id/name/tags/blocks/preferences)
 * and is the only application-level caller of `EnhancedStorageAdapter`.
 *
 * Responsibilities:
 *   - Lifecycle: init the DB on mount; if `window.location.hash` is a project id,
 *     load it through `db.load(id)` and seed state.
 *   - State: `blocks: BlockArray` is the runtime working copy. Auto-save is
 *     debounced and gated by `readOnlyRef.current` (set true while viewing a
 *     past commit through `useProjectHistory`).
 *   - Operations: `save`, `saveAs`, `loadProject`, `createProject`,
 *     `titleEdit`, `titleSave`, `createSnapshot`. The dialog wrappers in
 *     `extras/editor/project-*-operations.ts` are invoked from here.
 *
 * On every save:
 *   1. `serializeProject(blocks)` → JSON string.
 *   2. `db.save(…)` persists to IndexedDB (3 stores) and auto-commits to Git.
 *   3. The SyncManager queue picks the entry up and pushes to Google Drive
 *      / GameGuild Cloud depending on `storageType`.
 *
 * See `docs/DATA-FLOW.md` ("Editor Flow — Write Path").
 */

import { useCallback, useEffect, useRef, useState, type Dispatch, type SetStateAction } from "react"
import { toast } from "sonner"
import {
  EnhancedStorageAdapter,
  type ProjectPreferences,
} from "@/components/block-content-editor/lib/storage/editor/enhanced-storage-adapter"
import {
  EMPTY_PROJECT_DATA,
  deserializeProject,
  serializeProject,
} from "@/components/block-content-editor/lib/storage/editor/block-storage"

import type { BlockArray, BlockCellType } from "@/components/block-content-editor/lib/storage/editor/block-structure"
import type { ProjectType } from "@/components/block-content-editor/lib/storage/editor/project-types"
import { handleSave as saveProject, handleSaveAs as saveAsProject } from "@/components/block-content-editor/extras/editor/project-save-operations"
import { handleTitleEdit as titleEdit, handleTitleSave as titleSave } from "@/components/block-content-editor/extras/editor/project-title-operations"
import { calculateProjectAssetsSize as calculateAssets } from "@/components/block-content-editor/extras/editor/project-assets-operations"
import { checkSelectedProject as checkProject } from "@/components/block-content-editor/extras/editor/project-load-operations"

// ─── Types ───────────────────────────────────────────────────────────────────

export type { ProjectData } from "@/components/block-content-editor/lib/storage/editor/project-data"
import type { ProjectData } from "@/components/block-content-editor/lib/storage/editor/project-data"

type StorageType = "local" | "gameguild-cloud" | "google-drive"

export interface StorageAdapterInterface {
  save: (id: string, name: string, data: string, tags?: string[], storageType?: StorageType, preferences?: ProjectPreferences) => Promise<void>
  load: (id: string) => Promise<ProjectData | null>
  delete: (id: string) => Promise<void>
  list: () => Promise<ProjectData[]>
  getProjectInfo: (id: string) => Promise<any>
  searchProjects: (searchTerm: string, tags: string[], filterMode?: "all" | "any", storageTypeFilter?: StorageType) => Promise<ProjectData[]>
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
  loadProject(projectData: any): void
  createProject(projectData: any): void

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
  assets: Array<{ id: string; name: string; size: number; thumbnail?: string; mimeType?: string }>

  // Sync
  syncStats: any

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

// ─── Utilities ───────────────────────────────────────────────────────────────

function generateProjectId(): string {
  if (typeof crypto !== "undefined" && crypto.randomUUID) {
    return crypto.randomUUID()
  }
  return "proj_" + Date.now().toString(36) + "_" + Math.random().toString(36).substr(2, 9)
}

function estimateSize(data: string): number {
  return new Blob([data]).size / 1024
}

// ─── Hook ────────────────────────────────────────────────────────────────────

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

export function useProjectStorage(initialDefaults?: ProjectStorageDefaults): UseProjectStorageReturn {
  // ── DB ──
  const dbStorage = useRef<EnhancedStorageAdapter>(new EnhancedStorageAdapter())
  const [isDbInitialized, setIsDbInitialized] = useState(false)

  // ── Project metadata ──
  const [currentProjectId, setCurrentProjectId] = useState<string>("")
  const [currentProjectName, setCurrentProjectName] = useState<string>("")
  const [currentProjectStorageType, setCurrentProjectStorageType] = useState<StorageType>("local")
  const [projectTags, setProjectTags] = useState<string[]>([])
  const [currentProjectPreferences, setCurrentProjectPreferences] = useState<ProjectPreferences | undefined>(undefined)
  const [isFirstTime, setIsFirstTime] = useState(true)

  // ── Editor content ──
  const [blocks, setBlocks] = useState<BlockArray>([])

  // ── Lists ──
  const [savedProjects, setSavedProjects] = useState<ProjectData[]>([])
  const [availableTags, setAvailableTags] = useState<Array<{ name: string; usageCount: number }>>([])

  // ── Size ──
  const [currentProjectSize, setCurrentProjectSize] = useState<number>(0)
  const [currentProjectAssetsSize, setCurrentProjectAssetsSize] = useState<number>(0)
  const [currentProjectAssets, setCurrentProjectAssets] = useState<Array<{ id: string; name: string; size: number; thumbnail?: string; mimeType?: string }>>([])

  // ── Sync ──
  const [syncStats, setSyncStats] = useState<any>(null)

  // ── Auto-save ──
  const [autoSaveEnabled, setAutoSaveEnabled] = useState(false)
  const [lastProjectLoadTime, setLastProjectLoadTime] = useState<number>(0)

  // ── Read-only gate ──
  const readOnlyRef = useRef(false)

  // ═══════════════════════════════════════════════════════════════════════════
  // Storage adapter — wraps dbStorage with isDbInitialized guard
  // ═══════════════════════════════════════════════════════════════════════════

  const storageAdapter: StorageAdapterInterface = {
    save: async (id, name, data, tags = [], storageType = "local", preferences?) => {
      if (!id || !name || !data) { console.warn("Invalid id, name or data"); return }
      if (!isDbInitialized) throw new Error("Database not initialized")
      try {
        await dbStorage.current.save(id, name, data, tags, storageType, preferences)
      } catch (error) { console.error("Failed to save project:", error); throw error }
    },
    load: async (id) => {
      if (!isDbInitialized) throw new Error("Database not initialized")
      try { return await dbStorage.current.load(id) }
      catch (error) { console.error("Failed to load project:", error); return null }
    },
    delete: async (id) => {
      if (!isDbInitialized) throw new Error("Database not initialized")
      try { await dbStorage.current.delete(id) }
      catch (error) { console.error("Failed to delete project:", error); throw error }
    },
    list: async () => {
      if (!isDbInitialized) return []
      try { return await dbStorage.current.list() }
      catch (error) { console.error("Failed to list projects:", error); return [] }
    },
    getProjectInfo: async (id) => {
      if (!isDbInitialized) return null
      try { return await dbStorage.current.getProjectInfo(id) }
      catch (error) { console.error("Failed to get project info:", error); return null }
    },
    searchProjects: async (searchTerm, tags, filterMode = "any", storageTypeFilter?) => {
      if (!isDbInitialized) return []
      try { return await dbStorage.current.searchProjects(searchTerm, tags, filterMode, storageTypeFilter) }
      catch (error) { console.error("Failed to search projects:", error); return [] }
    },
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // List helpers
  // ═══════════════════════════════════════════════════════════════════════════

  const refreshProjects = useCallback(async () => {
    try { setSavedProjects(await storageAdapter.list()) }
    catch (error) { console.error("Failed to load projects list:", error) }
  }, [isDbInitialized])

  const refreshTags = useCallback(async () => {
    try { setAvailableTags(await dbStorage.current.getAllTags()) }
    catch (error) { console.error("Failed to load tags:", error) }
  }, [])

  // ═══════════════════════════════════════════════════════════════════════════
  // Asset calculation
  // ═══════════════════════════════════════════════════════════════════════════

  const calcAssets = useCallback(async (projectId: string) => {
    await calculateAssets({ projectId, setCurrentProjectAssetsSize, setCurrentProjectAssets })
  }, [])

  // ═══════════════════════════════════════════════════════════════════════════
  // DB initialization
  // ═══════════════════════════════════════════════════════════════════════════

  useEffect(() => {
    const initDB = async () => {
      try {
        await dbStorage.current.init()
        setIsDbInitialized(true)
      } catch (error) {
        console.error("Failed to initialize IndexedDB:", error)
        toast.error("Storage error", {
          description: "Unable to initialize database. Some features may not work.",
          duration: 5000, icon: "⚠️",
        })
      }
    }
    initDB()
  }, [])

  // Load lists after DB init
  useEffect(() => {
    if (!isDbInitialized) return
    refreshProjects()
    refreshTags()
  }, [isDbInitialized])

  // ═══════════════════════════════════════════════════════════════════════════
  // URL hash check — load project from hash on mount
  // ═══════════════════════════════════════════════════════════════════════════

  useEffect(() => {
    if (!isDbInitialized) return
    checkProject({
      storageAdapter,
      directDbLoad: (id: string) => dbStorage.current.load(id),
      setCurrentProjectId,
      setCurrentProjectName,
      setCurrentProjectStorageType,
      setProjectTags,
      setIsFirstTime,
      setLastProjectLoadTime,
      setCurrentProjectPreferences,
      setBlocks,
      allowedProjectTypes: initialDefaults?.allowedProjectTypes,
    })
  }, [isDbInitialized])

  // ═══════════════════════════════════════════════════════════════════════════
  // Size calculation
  // ═══════════════════════════════════════════════════════════════════════════

  useEffect(() => {
    setCurrentProjectSize(estimateSize(serializeProject(blocks)))
  }, [blocks])

  // Assets size
  useEffect(() => {
    if (currentProjectId && isDbInitialized) {
      calcAssets(currentProjectId)
    } else {
      setCurrentProjectAssetsSize(0)
    }
  }, [currentProjectId, isDbInitialized, blocks])

  // ═══════════════════════════════════════════════════════════════════════════
  // Sync monitoring
  // ═══════════════════════════════════════════════════════════════════════════

  useEffect(() => {
    if (!isDbInitialized) return

    const updateSyncStats = async () => {
      try { setSyncStats(await dbStorage.current.getSyncStats()) }
      catch (error) { console.error("Failed to get sync stats:", error) }
    }

    const interval = setInterval(updateSyncStats, 5000)
    updateSyncStats()

    dbStorage.current.onSyncStart(() => { updateSyncStats() })
    dbStorage.current.onSyncComplete((stats: any) => {
      updateSyncStats()
      if (stats.processed > 0) {
        toast.success("Synchronization completed", {
          description: `${stats.processed} synchronized projects`,
          duration: 3000, icon: "🔄",
        })
      }
    })
    dbStorage.current.onSyncError(() => {
      updateSyncStats()
      toast.error("Synchronization error", {
        description: "Some projects may not be synchronized",
        duration: 4000, icon: "⚠️",
      })
    })

    return () => { clearInterval(interval) }
  }, [isDbInitialized])

  // ═══════════════════════════════════════════════════════════════════════════
  // Auto-save
  // ═══════════════════════════════════════════════════════════════════════════

  useEffect(() => {
    if (!autoSaveEnabled || !currentProjectId || !isDbInitialized || readOnlyRef.current) return
    if (blocks.length === 0) return

    const timeSinceLoad = Date.now() - lastProjectLoadTime
    if (timeSinceLoad < 1000) return

    const autoSaveTimer = setTimeout(async () => {
      try {
        const dataToSave = serializeProject(blocks)
        await storageAdapter.save(
          currentProjectId, currentProjectName, dataToSave, projectTags,
          currentProjectStorageType, currentProjectPreferences
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
  }, [blocks, autoSaveEnabled, currentProjectId, currentProjectName, projectTags, isDbInitialized, currentProjectStorageType, lastProjectLoadTime])

  // ═══════════════════════════════════════════════════════════════════════════
  // Save
  // ═══════════════════════════════════════════════════════════════════════════

  const save = useCallback(async (): Promise<{ needsSaveAs: boolean }> => {
    if (!currentProjectId) return { needsSaveAs: true }

    const dataToSave = serializeProject(blocks)
    const preferences: ProjectPreferences = {
      global: {
        // Page-declared defaults fill in any missing structural fields.
        ...(initialDefaults?.projectType !== undefined ? { projectType: initialDefaults.projectType } : {}),
        ...(initialDefaults?.singleBlockMode !== undefined ? { singleBlockMode: initialDefaults.singleBlockMode } : {}),
        ...(initialDefaults?.allowedBlockTypes !== undefined ? { allowedBlockTypes: initialDefaults.allowedBlockTypes } : {}),
        ...currentProjectPreferences?.global,
      },
    }

    await saveProject({
      currentProjectId, currentProjectName, currentProjectStorageType,
      data: dataToSave,
      projectTags, storageAdapter, calculateProjectAssetsSize: calcAssets,
      setSaveAsDialogOpen: () => {},
      preferences,
    })
    return { needsSaveAs: false }
  }, [currentProjectId, currentProjectName, currentProjectStorageType, blocks, projectTags, currentProjectPreferences, isDbInitialized])

  // ═══════════════════════════════════════════════════════════════════════════
  // Save As
  // ═══════════════════════════════════════════════════════════════════════════

  const saveAs = useCallback(async (name: string, storageOption: StorageType, tags?: string[]) => {
    const dataToSave = serializeProject(blocks)
    const tagsToSave = tags ?? projectTags

    // Carry the current project's preferences (project type, structural rules)
    // forward to the new copy so its identity follows. When the current project
    // has no preferences yet (fresh editor), fall back to page-declared defaults.
    const preferences: ProjectPreferences = {
      global: {
        ...(initialDefaults?.projectType !== undefined ? { projectType: initialDefaults.projectType } : {}),
        ...(initialDefaults?.singleBlockMode !== undefined ? { singleBlockMode: initialDefaults.singleBlockMode } : {}),
        ...(initialDefaults?.allowedBlockTypes !== undefined ? { allowedBlockTypes: initialDefaults.allowedBlockTypes } : {}),
        ...currentProjectPreferences?.global,
      },
    }

    await saveAsProject({
      newProjectName: name,
      data: dataToSave,
      projectTags: tagsToSave,
      storageOption,
      storageAdapter,
      generateProjectId,
      setCurrentProjectId,
      setCurrentProjectName,
      setCurrentProjectStorageType,
      setNewProjectName: () => {},
      setSaveAsDialogOpen: () => {},
      loadSavedProjectsList: refreshProjects,
      calculateProjectAssetsSize: calcAssets,
      preferences,
    })

    if (tags) setProjectTags(tagsToSave)
  }, [blocks, projectTags, isDbInitialized, currentProjectPreferences])

  // ═══════════════════════════════════════════════════════════════════════════
  // Load project (called from OpenProjectDialog onProjectLoad)
  // ═══════════════════════════════════════════════════════════════════════════

  const loadProject = useCallback((projectData: any) => {
    setBlocks(deserializeProject(projectData.data))
    setCurrentProjectId(projectData.id)
    setCurrentProjectName(projectData.name)
    setCurrentProjectStorageType(projectData.storageType || "local")
    setProjectTags(projectData.tags || [])
    setCurrentProjectPreferences(projectData.preferences)
    setIsFirstTime(false)
    setLastProjectLoadTime(Date.now())
    window.history.pushState(null, "", `#${projectData.id}`)
  }, [])

  // ═══════════════════════════════════════════════════════════════════════════
  // Create project (called from CreateProjectDialog onProjectCreate)
  // ═══════════════════════════════════════════════════════════════════════════

  const createProject = useCallback((projectData: any) => {
    setBlocks([])
    setLastProjectLoadTime(Date.now())
    setCurrentProjectId(projectData.id)
    setCurrentProjectName(projectData.name)
    setCurrentProjectStorageType(projectData.storageType)
    setProjectTags(projectData.tags)
    if (projectData.preferences) setCurrentProjectPreferences(projectData.preferences)
    setIsFirstTime(false)
    window.history.pushState(null, "", `#${projectData.id}`)
  }, [])

  // ═══════════════════════════════════════════════════════════════════════════
  // Title operations
  // ═══════════════════════════════════════════════════════════════════════════

  const handleTitleEdit = useCallback((
    setEditingProjectName: (n: string) => void,
    setIsEditingTitle: (b: boolean) => void,
  ) => {
    titleEdit({ currentProjectId, currentProjectName, setEditingProjectName, setIsEditingTitle })
  }, [currentProjectId, currentProjectName])

  const handleTitleSave = useCallback(async (
    editingName: string,
    setEditingProjectName: (n: string) => void,
    setIsEditingTitle: (b: boolean) => void,
  ) => {
    const dataToSave = serializeProject(blocks)
    await titleSave({
      editingProjectName: editingName,
      currentProjectName,
      currentProjectId,
      data: dataToSave,
      projectTags,
      storageAdapter,
      setCurrentProjectName,
      setEditingProjectName,
      setIsEditingTitle,
      loadSavedProjectsList: refreshProjects,
    })
  }, [currentProjectId, currentProjectName, blocks, projectTags, isDbInitialized])

  // ═══════════════════════════════════════════════════════════════════════════
  // Snapshot
  // ═══════════════════════════════════════════════════════════════════════════

  const handleCreateSnapshot = useCallback(async (name?: string) => {
    if (!currentProjectId) return
    await save()
    await dbStorage.current.createSnapshot(currentProjectId, name)
  }, [currentProjectId, save])

  // ═══════════════════════════════════════════════════════════════════════════
  // Return
  // ═══════════════════════════════════════════════════════════════════════════

  return {
    // Status
    isDbInitialized,
    isFirstTime,

    // Project metadata
    projectId: currentProjectId,
    projectName: currentProjectName,
    setProjectName: setCurrentProjectName,
    storageType: currentProjectStorageType,
    tags: projectTags,
    setTags: setProjectTags,
    preferences: currentProjectPreferences,
    setPreferences: setCurrentProjectPreferences,

    // Editor content
    blocks,
    setBlocks,

    // Operations
    save,
    saveAs,
    loadProject,
    createProject,

    // Title
    titleEdit: handleTitleEdit,
    titleSave: handleTitleSave,

    // Snapshot
    createSnapshot: handleCreateSnapshot,

    // ID generation
    generateProjectId,

    // Lists
    savedProjects,
    availableTags,
    refreshProjects,
    refreshTags,

    // Size
    projectSize: currentProjectSize,
    assetsSize: currentProjectAssetsSize,
    assets: currentProjectAssets,

    // Sync
    syncStats,

    // Auto-save
    autoSaveEnabled,
    setAutoSaveEnabled,
    lastProjectLoadTime,

    // Direct DB access
    db: dbStorage.current,

    // Storage adapter
    storageAdapter,

    // Read-only gate
    readOnlyRef,
  }
}

// Suppress unused EMPTY constant warning (referenced by tests/other modules indirectly via re-export)
void EMPTY_PROJECT_DATA
