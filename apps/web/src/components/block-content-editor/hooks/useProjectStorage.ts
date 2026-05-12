"use client"

import { useState, useEffect, useRef, useCallback, type Dispatch, type SetStateAction } from "react"
import { toast } from "sonner"
import type { LexicalEditor } from "lexical"
import { EnhancedStorageAdapter, type ProjectPreferences } from "@/components/block-content-editor/lib/storage/editor/enhanced-storage-adapter"
import type { ProjectData as StorageProjectData } from "@/components/block-content-editor/lib/storage/editor/enhanced-storage-adapter"
import { syncConfig } from "@/components/block-content-editor/lib/sync/editor/sync-config"
import { type ProjectMode } from "@/components/block-content-editor/lib/storage/editor/project-modes"
import { extractEditorStates, createProjectData } from "@/components/block-content-editor/lib/storage/editor/layout-detector"
import { type EngineType, ENGINE_TYPES } from "@/components/block-content-editor/lib/storage/editor/project-types"
import type { CellularContent } from "@/components/block-content-editor/lib/storage/editor/cell-structure"
import type { BlockArray } from "@/components/block-content-editor/lib/storage/editor/block-structure"
import { blocksToStorage, storageToBlocks } from "@/components/block-content-editor/lib/storage/editor/cell-converters/blocks"
import { cellsToLexical } from "@/components/block-content-editor/lib/storage/editor/cell-converters/lexical"
import { handleSave as saveProject, handleSaveAs as saveAsProject } from "@/components/block-content-editor/extras/editor/project-save-operations"
import { handleTitleEdit as titleEdit, handleTitleSave as titleSave } from "@/components/block-content-editor/extras/editor/project-title-operations"
import { calculateProjectAssetsSize as calculateAssets } from "@/components/block-content-editor/extras/editor/project-assets-operations"
import { checkSelectedProject as checkProject } from "@/components/block-content-editor/extras/editor/project-load-operations"

// ─── Types ───────────────────────────────────────────────────────────────────

export interface ProjectData {
  id: string
  name: string
  data: string
  tags: string[]
  size: number
  createdAt: string
  updatedAt: string
  storageType?: "local" | "gameguild-cloud" | "google-drive"
  preferences?: ProjectPreferences
}

type StorageType = "local" | "gameguild-cloud" | "google-drive"

export interface StorageAdapterInterface {
  save: (id: string, name: string, data: string, tags?: string[], storageType?: StorageType, preferences?: ProjectPreferences, engine?: EngineType) => Promise<void>
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
  engine: EngineType
  setEngine: Dispatch<SetStateAction<EngineType>>
  projectMode: ProjectMode
  storageType: StorageType
  tags: string[]
  setTags: Dispatch<SetStateAction<string[]>>
  preferences: ProjectPreferences | undefined
  setPreferences: (p: ProjectPreferences) => void

  // Editor content (Lexical single)
  editorState: string
  editorRef: React.RefObject<LexicalEditor | null>
  setEditorState: Dispatch<SetStateAction<string>>

  // Editor content (Block Array)
  blockArrayBlocks: BlockArray
  setBlockArrayBlocks: Dispatch<SetStateAction<BlockArray>>

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

  // Loading ref (for editor layouts)
  setLoadingRef: React.MutableRefObject<((loading: boolean) => void) | null>

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
  engine?: EngineType
  mode?: ProjectMode
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
  const [currentEngine, setCurrentEngine] = useState<EngineType>(initialDefaults?.engine || ENGINE_TYPES.LEXICAL)
  const [currentProjectMode, setCurrentProjectMode] = useState<ProjectMode>(initialDefaults?.mode || "free-page")
  const [currentProjectPreferences, setCurrentProjectPreferences] = useState<ProjectPreferences | undefined>(undefined)
  const [isFirstTime, setIsFirstTime] = useState(true)

  // ── Editor content (Lexical single) ──
  const [editorState, setEditorState] = useState<string>("")
  const editorRef = useRef<LexicalEditor | null>(null)

  // ── Editor content (Block Array) ──
  const [blockArrayBlocks, setBlockArrayBlocks] = useState<BlockArray>([])

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

  // ── Loading ref ──
  const setLoadingRef = useRef<((loading: boolean) => void) | null>(null)

  // ── Read-only gate ──
  const readOnlyRef = useRef(false)

  // ═══════════════════════════════════════════════════════════════════════════
  // Storage adapter — wraps dbStorage with isDbInitialized guard
  // ═══════════════════════════════════════════════════════════════════════════

  const storageAdapter: StorageAdapterInterface = {
    save: async (id, name, data, tags = [], storageType = "local", preferences?, engine?) => {
      if (!id || !name || !data) { console.warn("Invalid id, name or data"); return }
      if (!isDbInitialized) throw new Error("Database not initialized")
      try {
        await dbStorage.current.save(id, name, data, tags, storageType, preferences, engine)
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
      editorRef,
      setCurrentProjectId,
      setCurrentProjectName,
      setCurrentProjectStorageType,
      setProjectTags,
      setIsFirstTime,
      setEditorState,
      setCurrentProjectMode,
      setLastProjectLoadTime,
      setCurrentProjectPreferences,
      setCurrentEngine,
      setBlockArrayCells: (cells) => setBlockArrayBlocks(
        storageToBlocks(cells && typeof cells === "object" && !Array.isArray(cells) ? cells : { order: [], blocks: {} })
      ),
    })
  }, [isDbInitialized])

  // ═══════════════════════════════════════════════════════════════════════════
  // Size calculation
  // ═══════════════════════════════════════════════════════════════════════════

  useEffect(() => {
    let dataToCalculate: string
    if (currentEngine === ENGINE_TYPES.BLOCKS) {
      dataToCalculate = createProjectData({ blocks: { b1: blocksToStorage(blockArrayBlocks) } })
    } else {
      dataToCalculate = createProjectData({ blocks: { b1: editorState ? JSON.parse(editorState) : null } })
    }
    setCurrentProjectSize(estimateSize(dataToCalculate))
  }, [editorState, blockArrayBlocks, currentEngine])

  // Assets size
  useEffect(() => {
    if (currentProjectId && isDbInitialized) {
      calcAssets(currentProjectId)
    } else {
      setCurrentProjectAssetsSize(0)
    }
  }, [currentProjectId, isDbInitialized, editorState])

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

    const hasContent = currentEngine === ENGINE_TYPES.BLOCKS
      ? blockArrayBlocks.length > 0
      : !!editorState

    if (!hasContent) return

    const timeSinceLoad = Date.now() - lastProjectLoadTime
    if (timeSinceLoad < 1000) return

    const autoSaveTimer = setTimeout(async () => {
      try {
        let dataToSave: string
        if (currentEngine === ENGINE_TYPES.BLOCKS) {
          dataToSave = createProjectData({ blocks: { b1: blocksToStorage(blockArrayBlocks) } })
        } else {
          dataToSave = createProjectData({ blocks: { b1: editorState ? JSON.parse(editorState) : null } })
        }

        await storageAdapter.save(
          currentProjectId, currentProjectName, dataToSave, projectTags,
          currentProjectStorageType, currentProjectPreferences, currentEngine
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
  }, [editorState, blockArrayBlocks, currentEngine, autoSaveEnabled, currentProjectId, currentProjectName, projectTags, isDbInitialized, currentProjectStorageType, lastProjectLoadTime])

  // ═══════════════════════════════════════════════════════════════════════════
  // Save
  // ═══════════════════════════════════════════════════════════════════════════

  const save = useCallback(async (): Promise<{ needsSaveAs: boolean }> => {
    if (!currentProjectId) return { needsSaveAs: true }

    let dataToSave: string

    if (currentEngine === ENGINE_TYPES.BLOCKS) {
      dataToSave = createProjectData({
        blocks: { b1: blocksToStorage(blockArrayBlocks) },
      })
      const preferences: ProjectPreferences = {
        global: { ...currentProjectPreferences?.global, mode: currentProjectMode },
        nodes: currentProjectPreferences?.nodes || {},
      }
      await saveProject({
        currentProjectId, currentProjectName, currentProjectStorageType,
        editorState: dataToSave,
        editorRef: { current: null } as React.RefObject<LexicalEditor | null>,
        projectTags, storageAdapter, calculateProjectAssetsSize: calcAssets,
        setSaveAsDialogOpen: () => {},
        preferences, engine: currentEngine,
      })
      return { needsSaveAs: false }
    }

    dataToSave = createProjectData({ blocks: { b1: editorState ? JSON.parse(editorState) : null } })

    const preferences: ProjectPreferences = {
      global: {
        ...currentProjectPreferences?.global,
        mode: currentProjectMode,
      },
      nodes: currentProjectPreferences?.nodes || {},
    }

    await saveProject({
      currentProjectId, currentProjectName, currentProjectStorageType,
      editorState: dataToSave, editorRef, projectTags,
      storageAdapter, calculateProjectAssetsSize: calcAssets,
      setSaveAsDialogOpen: () => {},
      preferences, engine: currentEngine,
    })
    return { needsSaveAs: false }
  }, [currentProjectId, currentEngine, currentProjectName, currentProjectStorageType, editorState, blockArrayBlocks, projectTags, currentProjectPreferences, currentProjectMode, isDbInitialized])

  // ═══════════════════════════════════════════════════════════════════════════
  // Save As
  // ═══════════════════════════════════════════════════════════════════════════

  const saveAs = useCallback(async (name: string, storageOption: StorageType, tags?: string[]) => {
    let dataToSave: string
    if (currentEngine === ENGINE_TYPES.BLOCKS) {
      dataToSave = createProjectData({ blocks: { b1: blocksToStorage(blockArrayBlocks) } })
    } else {
      dataToSave = createProjectData({ blocks: { b1: editorState ? JSON.parse(editorState) : null } })
    }

    const tagsToSave = tags ?? projectTags

    await saveAsProject({
      newProjectName: name,
      editorState: dataToSave,
      editorRef,
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
      engine: currentEngine,
    })

    if (tags) {
      setProjectTags(tagsToSave)
    }
  }, [editorState, blockArrayBlocks, projectTags, currentEngine, isDbInitialized])

  // ═══════════════════════════════════════════════════════════════════════════
  // Load project (called from OpenProjectDialog onProjectLoad)
  // ═══════════════════════════════════════════════════════════════════════════

  const loadProject = useCallback((projectData: any) => {
    const projectEngine: EngineType = projectData.engine || ENGINE_TYPES.LEXICAL
    setCurrentEngine(projectEngine)

    if (projectEngine === ENGINE_TYPES.BLOCKS) {
      const states = extractEditorStates(projectData.data)
      const storageData = states.blocks.b1 || { order: [], blocks: {} }
      setBlockArrayBlocks(storageToBlocks(
        storageData && typeof storageData === "object" && !Array.isArray(storageData) ? storageData : { order: [], blocks: {} }
      ))
      setCurrentProjectId(projectData.id)
      setCurrentProjectName(projectData.name)
      setCurrentProjectStorageType(projectData.storageType || "local")
      setProjectTags(projectData.tags || [])
      setCurrentProjectMode(projectData.preferences?.global?.mode || "free-page")
      setCurrentProjectPreferences(projectData.preferences)
      setIsFirstTime(false)
      setLastProjectLoadTime(Date.now())
      window.history.pushState(null, "", `#${projectData.id}`)
      return
    }

    // Lexical engine
    const projectMode = projectData.preferences?.global?.mode || "free-page"

    setCurrentProjectId(projectData.id)
    setCurrentProjectName(projectData.name)
    setCurrentProjectStorageType(projectData.storageType || "local")
    setProjectTags(projectData.tags || [])
    setCurrentProjectMode(projectMode)
    setCurrentProjectPreferences(projectData.preferences)
    setIsFirstTime(false)
    setLastProjectLoadTime(Date.now())

    // Extract editor states (single block)
    const states = extractEditorStates(projectData.data)

    setTimeout(() => {
      try {
        if (editorRef.current && states.blocks.b1) {
          setEditorState(JSON.stringify(states.blocks.b1))
          const lexicalState = cellsToLexical(states.blocks.b1)
          const parsed = editorRef.current.parseEditorState(JSON.stringify(lexicalState))
          editorRef.current.setEditorState(parsed)
        }
      } catch (error) {
        console.error("Failed to load editor data:", error)
        toast.error("Erro ao carregar dados do editor", {
          description: error instanceof Error ? error.message : "Unknown error",
          duration: 4000, icon: "❌",
        })
      }
    }, 100)

    window.history.pushState(null, "", `#${projectData.id}`)
  }, [])

  // ═══════════════════════════════════════════════════════════════════════════
  // Create project (called from CreateProjectDialog onProjectCreate)
  // ═══════════════════════════════════════════════════════════════════════════

  const createProject = useCallback((projectData: any) => {
    setCurrentEngine(projectData.engine || ENGINE_TYPES.LEXICAL)

    if (projectData.engine === ENGINE_TYPES.BLOCKS) {
      setBlockArrayBlocks([])
      setCurrentProjectMode(projectData.mode || "free-page")
      setLastProjectLoadTime(Date.now())
      setCurrentProjectId(projectData.id)
      setCurrentProjectName(projectData.name)
      setCurrentProjectStorageType(projectData.storageType)
      setProjectTags(projectData.tags)
      setIsFirstTime(false)
      window.history.pushState(null, "", `#${projectData.id}`)
      return
    }

    // Lexical engine
    const emptyCells: CellularContent = []
    const dataString = createProjectData({ blocks: { b1: emptyCells } })

    setCurrentProjectMode(projectData.mode || "free-page")
    setLastProjectLoadTime(Date.now())

    // Initialize editor after layout renders
    setTimeout(() => {
      const lexicalState = cellsToLexical(emptyCells)
      const lexicalStateString = JSON.stringify(lexicalState)
      setEditorState(JSON.stringify(emptyCells))
      if (editorRef.current) {
        editorRef.current.setEditorState(editorRef.current.parseEditorState(lexicalStateString))
      }
    }, 100)

    setCurrentProjectId(projectData.id)
    setCurrentProjectName(projectData.name)
    setCurrentProjectStorageType(projectData.storageType)
    setProjectTags(projectData.tags)
    setIsFirstTime(false)
    window.history.pushState(null, "", `#${projectData.id}`)
  }, [isDbInitialized])

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
    const stateToUse = createProjectData({ blocks: { b1: editorState ? JSON.parse(editorState) : null } })

    await titleSave({
      editingProjectName: editingName,
      currentProjectName,
      currentProjectId,
      editorState: stateToUse,
      editorRef,
      projectTags,
      storageAdapter,
      setCurrentProjectName,
      setEditingProjectName,
      setIsEditingTitle,
      loadSavedProjectsList: refreshProjects,
    })
  }, [currentProjectId, currentProjectName, editorState, projectTags, isDbInitialized])

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
    engine: currentEngine,
    setEngine: setCurrentEngine,
    projectMode: currentProjectMode,
    storageType: currentProjectStorageType,
    tags: projectTags,
    setTags: setProjectTags,
    preferences: currentProjectPreferences,
    setPreferences: setCurrentProjectPreferences,

    // Editor content (Lexical single)
    editorState,
    editorRef,
    setEditorState,

    // Editor content (Block Array)
    blockArrayBlocks,
    setBlockArrayBlocks,

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

    // Loading ref
    setLoadingRef,

    // Read-only gate
    readOnlyRef,
  }
}
