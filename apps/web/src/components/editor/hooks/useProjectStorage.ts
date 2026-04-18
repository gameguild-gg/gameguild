"use client"

import { useState, useEffect, useRef, useCallback, type Dispatch, type SetStateAction } from "react"
import { toast } from "sonner"
import type { LexicalEditor } from "lexical"
import { EnhancedStorageAdapter, type ProjectPreferences } from "@/lib/storage/editor/enhanced-storage-adapter"
import type { ProjectData as StorageProjectData } from "@/lib/storage/editor/enhanced-storage-adapter"
import { syncConfig } from "@/lib/sync/editor/sync-config"
import { type ProjectMode } from "@/lib/storage/editor/project-modes"
import { detectProjectLayout, extractEditorStates, createProjectData } from "@/lib/storage/editor/layout-detector"
import { getLayoutFromType, type ProjectType, type InternalLayout, PROJECT_TYPES, type EngineType, ENGINE_TYPES } from "@/lib/storage/editor/project-types"
import {
  type SlideshowStructure,
  type PreviewMode,
  createEmptySlideshowStructure,
  serializeSlideshowStructure,
  convertToIndependent,
  convertToDependent,
  importProjectToSlide,
} from "@/lib/storage/editor/slideshow-structure"
import type { CellularContent } from "@/lib/storage/editor/cell-structure"
import type { BlockArray } from "@/lib/storage/editor/block-structure"
import { blocksToStorage, storageToBlocks } from "@/lib/storage/editor/cell-converters/blocks"
import { cellsToLexical } from "@/lib/storage/editor/cell-converters/lexical"
import { handleSave as saveProject, handleSaveAs as saveAsProject } from "@/components/editor/extras/editor/project-save-operations"
import { handleTitleEdit as titleEdit, handleTitleSave as titleSave } from "@/components/editor/extras/editor/project-title-operations"
import { calculateProjectAssetsSize as calculateAssets } from "@/components/editor/extras/editor/project-assets-operations"
import { checkSelectedProject as checkProject } from "@/components/editor/extras/editor/project-load-operations"

// ─── Types ───────────────────────────────────────────────────────────────────

export interface ProjectData {
  id: string
  name: string
  type: ProjectType
  data: string
  tags: string[]
  size: number
  createdAt: string
  updatedAt: string
  storageType?: "local" | "gameguild-cloud" | "google-drive"
  preferences?: ProjectPreferences
  deps?: StorageProjectData[]
}

type StorageType = "local" | "gameguild-cloud" | "google-drive"

export interface StorageAdapterInterface {
  save: (id: string, name: string, data: string, tags?: string[], storageType?: StorageType, preferences?: ProjectPreferences, type?: string, deps?: StorageProjectData[], engine?: EngineType) => Promise<void>
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
  projectType: ProjectType
  layout: InternalLayout
  engine: EngineType
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

  // Editor content (Lexical multi-block)
  blockStates: Record<string, string>
  blockRefs: React.MutableRefObject<Record<string, LexicalEditor | null>>
  setBlockStates: Dispatch<SetStateAction<Record<string, string>>>

  // Editor content (Block Array)
  blockArrayBlocks: BlockArray
  setBlockArrayBlocks: Dispatch<SetStateAction<BlockArray>>

  // Slideshow
  slideshowStructure: SlideshowStructure | null
  setSlideshowStructure: Dispatch<SetStateAction<SlideshowStructure | null>>
  slideshowDeps: StorageProjectData[]
  setSlideshowDeps: Dispatch<SetStateAction<StorageProjectData[]>>
  currentSlideIndex: number
  setCurrentSlideIndex: Dispatch<SetStateAction<number>>
  slideEditorRefs: Map<string, React.RefObject<LexicalEditor>>
  setSlideEditorRefs: Dispatch<SetStateAction<Map<string, React.RefObject<LexicalEditor>>>>
  previewMode: PreviewMode
  setPreviewMode: Dispatch<SetStateAction<PreviewMode>>
  resolvedProjects: Map<string, StorageProjectData | null>
  setResolvedProjects: Dispatch<SetStateAction<Map<string, StorageProjectData | null>>>

  // Operations
  save(): Promise<{ needsSaveAs: boolean }>
  saveAs(name: string, storageOption: StorageType): Promise<void>
  loadProject(projectData: any): void
  createProject(projectData: any): void

  // Title
  titleEdit(setEditingProjectName: (n: string) => void, setIsEditingTitle: (b: boolean) => void): void
  titleSave(editingName: string, setEditingProjectName: (n: string) => void, setIsEditingTitle: (b: boolean) => void): Promise<void>

  // Slideshow ops
  convertToIndependent(slideId: string): Promise<void>
  convertToDependent(slideId: string): Promise<void>
  importConfirm(slideId: string, projectId: string, loadMode: "snapshot" | "head", snapshotTag?: string): void
  createSnapshot(name?: string): Promise<void>

  // Block ops (Type2 layout)
  addBlock(): void
  removeBlock(blockId: string): void

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
  layout?: ProjectType
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
  const [currentLayout, setCurrentLayout] = useState<InternalLayout>(initialDefaults?.layout ? getLayoutFromType(initialDefaults.layout) : "single")
  const [currentProjectType, setCurrentProjectType] = useState<ProjectType>(initialDefaults?.layout || PROJECT_TYPES.TYPE1)
  const [currentEngine, setCurrentEngine] = useState<EngineType>(initialDefaults?.engine || ENGINE_TYPES.LEXICAL)
  const [currentProjectMode, setCurrentProjectMode] = useState<ProjectMode>(initialDefaults?.mode || "free-page")
  const [currentProjectPreferences, setCurrentProjectPreferences] = useState<ProjectPreferences | undefined>(undefined)
  const [isFirstTime, setIsFirstTime] = useState(true)

  // ── Editor content (Lexical single) ──
  const [editorState, setEditorState] = useState<string>("")
  const editorRef = useRef<LexicalEditor | null>(null)

  // ── Editor content (Lexical multi-block) ──
  const [blockStates, setBlockStates] = useState<Record<string, string>>({})
  const blockRefs = useRef<Record<string, LexicalEditor | null>>({})

  // ── Editor content (Block Array) ──
  const [blockArrayBlocks, setBlockArrayBlocks] = useState<BlockArray>([])

  // ── Slideshow ──
  const [slideshowStructure, setSlideshowStructure] = useState<SlideshowStructure | null>(null)
  const [slideshowDeps, setSlideshowDeps] = useState<StorageProjectData[]>([])
  const [currentSlideIndex, setCurrentSlideIndex] = useState(0)
  const [slideEditorRefs, setSlideEditorRefs] = useState<Map<string, React.RefObject<LexicalEditor>>>(new Map())
  const [previewMode, setPreviewMode] = useState<PreviewMode>("continuous")
  const [resolvedProjects, setResolvedProjects] = useState<Map<string, StorageProjectData | null>>(new Map())

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
    save: async (id, name, data, tags = [], storageType = "local", preferences?, type = "type1", deps?, engine?) => {
      if (!id || !name || !data) { console.warn("Invalid id, name or data"); return }
      if (!isDbInitialized) throw new Error("Database not initialized")
      try {
        await dbStorage.current.save(id, name, data, tags, storageType, preferences, type as any, deps, engine)
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
      blockRefs,
      setCurrentProjectId,
      setCurrentProjectName,
      setCurrentProjectStorageType,
      setProjectTags,
      setIsFirstTime,
      setCurrentLayout,
      setCurrentProjectType: (type: string) => setCurrentProjectType(type as ProjectType),
      setEditorState,
      setBlockStates,
      setSlideshowStructure,
      setDeps: setSlideshowDeps,
      setResolvedProjects,
      setCurrentSlideIndex,
      setSlideEditorRefs,
      setPreviewMode,
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
    if (currentLayout === "slideshow" && slideshowStructure) {
      dataToCalculate = serializeSlideshowStructure(slideshowStructure)
    } else {
      const blocks: Record<string, any> = {}
      if (currentLayout === "single") {
        blocks.b1 = editorState ? JSON.parse(editorState) : null
      } else {
        Object.entries(blockStates).forEach(([blockId, state]) => {
          blocks[blockId] = state ? JSON.parse(state) : null
        })
      }
      dataToCalculate = createProjectData(currentProjectType, { blocks })
    }
    setCurrentProjectSize(estimateSize(dataToCalculate))
  }, [editorState, blockStates, currentLayout, slideshowStructure])

  // Assets size
  useEffect(() => {
    if (currentProjectId && isDbInitialized) {
      calcAssets(currentProjectId)
    } else {
      setCurrentProjectAssetsSize(0)
    }
  }, [currentProjectId, isDbInitialized, editorState, blockStates, slideshowStructure])

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
      : currentLayout === "single"
        ? editorState
        : Object.keys(blockStates).length > 0

    if (!hasContent) return

    const timeSinceLoad = Date.now() - lastProjectLoadTime
    if (timeSinceLoad < 1000) return

    const autoSaveTimer = setTimeout(async () => {
      try {
        let dataToSave: string
        if (currentEngine === ENGINE_TYPES.BLOCKS) {
          dataToSave = createProjectData(currentProjectType, { blocks: { b1: blocksToStorage(blockArrayBlocks) } })
        } else {
          const blocks: Record<string, any> = {}
          if (currentLayout === "single") {
            blocks.b1 = editorState ? JSON.parse(editorState) : null
          } else {
            Object.entries(blockStates).forEach(([blockId, state]) => {
              blocks[blockId] = state ? JSON.parse(state) : null
            })
          }
          dataToSave = createProjectData(currentProjectType, { blocks })
        }

        await storageAdapter.save(
          currentProjectId, currentProjectName, dataToSave, projectTags,
          currentProjectStorageType, currentProjectPreferences, currentProjectType,
          undefined, currentEngine
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
  }, [editorState, blockStates, blockArrayBlocks, currentEngine, autoSaveEnabled, currentProjectId, currentProjectName, projectTags, isDbInitialized, currentLayout, currentProjectType, currentProjectStorageType, lastProjectLoadTime])

  // ═══════════════════════════════════════════════════════════════════════════
  // Save
  // ═══════════════════════════════════════════════════════════════════════════

  const save = useCallback(async (): Promise<{ needsSaveAs: boolean }> => {
    if (!currentProjectId) return { needsSaveAs: true }

    let dataToSave: string

    if (currentEngine === ENGINE_TYPES.BLOCKS) {
      dataToSave = createProjectData(currentProjectType, {
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
        setSaveAsDialogOpen: () => {}, // no-op: we return needsSaveAs
        preferences, type: currentProjectType, engine: currentEngine,
      })
      return { needsSaveAs: false }
    }

    if (currentLayout === "slideshow" && slideshowStructure) {
      dataToSave = serializeSlideshowStructure(slideshowStructure)
    } else {
      const blocks: Record<string, any> = {}
      if (currentLayout === "single") {
        blocks.b1 = editorState ? JSON.parse(editorState) : null
      } else {
        Object.entries(blockStates).forEach(([blockId, state]) => {
          blocks[blockId] = state ? JSON.parse(state) : null
        })
      }
      dataToSave = createProjectData(currentProjectType, { blocks })
    }

    const refToUse = currentLayout === "single" ? editorRef : {
      current: Object.values(blockRefs.current)[0] ?? null,
    } as React.RefObject<LexicalEditor | null>

    const preferences: ProjectPreferences = {
      global: {
        ...currentProjectPreferences?.global,
        mode: currentProjectMode,
        ...(currentLayout === "slideshow" && { previewMode }),
      },
      nodes: currentProjectPreferences?.nodes || {},
    }

    await saveProject({
      currentProjectId, currentProjectName, currentProjectStorageType,
      editorState: dataToSave, editorRef: refToUse, projectTags,
      storageAdapter, calculateProjectAssetsSize: calcAssets,
      setSaveAsDialogOpen: () => {}, // no-op
      preferences, type: currentProjectType,
      deps: currentLayout === "slideshow" ? slideshowDeps : undefined,
      engine: currentEngine,
    })
    return { needsSaveAs: false }
  }, [currentProjectId, currentEngine, currentProjectType, currentProjectName, currentProjectStorageType, editorState, blockStates, blockArrayBlocks, projectTags, currentProjectPreferences, currentProjectMode, previewMode, currentLayout, slideshowStructure, slideshowDeps, isDbInitialized])

  // ═══════════════════════════════════════════════════════════════════════════
  // Save As
  // ═══════════════════════════════════════════════════════════════════════════

  const saveAs = useCallback(async (name: string, storageOption: StorageType) => {
    let dataToSave: string
    if (currentLayout === "slideshow" && slideshowStructure) {
      dataToSave = serializeSlideshowStructure(slideshowStructure)
    } else {
      const blocks: Record<string, any> = {}
      if (currentLayout === "single") {
        blocks.b1 = editorState ? JSON.parse(editorState) : null
      } else {
        Object.entries(blockStates).forEach(([blockId, state]) => {
          blocks[blockId] = state ? JSON.parse(state) : null
        })
      }
      dataToSave = createProjectData(currentProjectType, { blocks })
    }

    const refToUse = currentLayout === "single" ? editorRef : {
      current: Object.values(blockRefs.current)[0] ?? null,
    } as React.RefObject<LexicalEditor | null>

    // We call the operation with a thin adapter that sets our internal state
    await saveAsProject({
      newProjectName: name,
      editorState: dataToSave,
      editorRef: refToUse,
      projectTags,
      storageOption,
      storageAdapter,
      generateProjectId,
      setCurrentProjectId,
      setCurrentProjectName,
      setCurrentProjectStorageType,
      setNewProjectName: () => {}, // page handles its own input
      setSaveAsDialogOpen: () => {}, // page handles dialog
      loadSavedProjectsList: refreshProjects,
      calculateProjectAssetsSize: calcAssets,
    })
  }, [currentLayout, editorState, blockStates, slideshowStructure, currentProjectType, projectTags, isDbInitialized])

  // ═══════════════════════════════════════════════════════════════════════════
  // Load project (called from OpenProjectDialog onProjectLoad)
  // ═══════════════════════════════════════════════════════════════════════════

  const loadProject = useCallback((projectData: any) => {
    const projectEngine: EngineType = projectData.engine || ENGINE_TYPES.LEXICAL
    setCurrentEngine(projectEngine)

    if (projectEngine === ENGINE_TYPES.BLOCKS) {
      const states = extractEditorStates(projectData.data, projectData.type)
      const storageData = states.blocks.b1 || { order: [], blocks: {} }
      setBlockArrayBlocks(storageToBlocks(
        storageData && typeof storageData === "object" && !Array.isArray(storageData) ? storageData : { order: [], blocks: {} }
      ))
      setCurrentProjectId(projectData.id)
      setCurrentProjectName(projectData.name)
      setCurrentProjectType(projectData.type)
      setCurrentLayout("single")
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
    const layoutInfo = detectProjectLayout(projectData.data)
    const finalLayout = getLayoutFromType(projectData.type)
    const projectMode = projectData.preferences?.global?.mode || "free-page"

    setCurrentProjectId(projectData.id)
    setCurrentProjectName(projectData.name)
    setCurrentProjectType(projectData.type)
    setCurrentLayout(finalLayout)
    setCurrentProjectStorageType(projectData.storageType || "local")
    setProjectTags(projectData.tags || [])
    setCurrentProjectMode(projectMode)
    setCurrentProjectPreferences(projectData.preferences)
    setIsFirstTime(false)
    setLastProjectLoadTime(Date.now())

    // Slideshow
    if (layoutInfo.hasSlides && layoutInfo.slideshowData) {
      setSlideshowStructure(layoutInfo.slideshowData)
      setSlideshowDeps(projectData.deps || [])
      setCurrentSlideIndex(0)
      const savedPreviewMode = projectData.preferences?.global?.previewMode || "continuous"
      setPreviewMode(savedPreviewMode as PreviewMode)

      const newRefs = new Map<string, React.RefObject<LexicalEditor>>()
      layoutInfo.slideshowData.slides.forEach((slide: any) => {
        newRefs.set(slide.id, { current: undefined as any })
      })
      setSlideEditorRefs(newRefs)

      // Load independent projects
      const independentSlides = layoutInfo.slideshowData.slides.filter(
        (slide: any) => slide.projectRef && !slide.projectRef.isDependent
      )
      if (independentSlides.length > 0 && dbStorage.current) {
        ;(async () => {
          const results = new Map<string, StorageProjectData | null>()
          await Promise.all(
            independentSlides.map(async (slide: any) => {
              try {
                const project = await dbStorage.current!.load(slide.projectRef!.projectId)
                results.set(slide.id, project)
              } catch (error) {
                console.error(`Failed to load independent project ${slide.projectRef!.projectId}:`, error)
                results.set(slide.id, null)
              }
            })
          )
          setResolvedProjects(results)
        })()
      }
      window.history.pushState(null, "", `#${projectData.id}`)
      return
    }

    // Non-slideshow: extract editor states
    const states = extractEditorStates(projectData.data, projectData.type)

    setTimeout(() => {
      try {
        if (finalLayout === "single" && editorRef.current && states.blocks.b1) {
          setEditorState(JSON.stringify(states.blocks.b1))
          const lexicalState = cellsToLexical(states.blocks.b1)
          const parsed = editorRef.current.parseEditorState(JSON.stringify(lexicalState))
          editorRef.current.setEditorState(parsed)
        } else if (finalLayout === "multiple" && states.blocks) {
          blockRefs.current = {}
          const newBlockStates: Record<string, string> = {}
          Object.entries(states.blocks).forEach(([blockId, blockState]: [string, any]) => {
            if (blockState) {
              blockRefs.current[blockId] = null
              newBlockStates[blockId] = JSON.stringify(blockState)
            }
          })
          setBlockStates(newBlockStates)
          setTimeout(() => {
            Object.entries(newBlockStates).forEach(([blockId, stateString]) => {
              const ref = blockRefs.current[blockId]
              if (ref) {
                try {
                  const cellsData = JSON.parse(stateString)
                  const lexicalState = cellsToLexical(cellsData)
                  const parsed = ref.parseEditorState(JSON.stringify(lexicalState))
                  ref.setEditorState(parsed)
                } catch (error) { console.error(`Failed to load state for block ${blockId}:`, error) }
              }
            })
          }, 150)
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
      setCurrentLayout("single")
      setCurrentProjectType((projectData.type || "type1") as ProjectType)
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
    let dataString: string
    const layoutType = getLayoutFromType(projectData.type)

    if (layoutType === "slideshow") {
      const { structure: initialStructure, deps: initialDeps } = createEmptySlideshowStructure(projectData.id)
      dataString = serializeSlideshowStructure(initialStructure)
      setSlideshowStructure(initialStructure)
      setSlideshowDeps(initialDeps)
      setCurrentSlideIndex(0)
      const newRefs = new Map<string, React.RefObject<LexicalEditor>>()
      if (initialStructure.slides[0]) {
        newRefs.set(initialStructure.slides[0].id, { current: undefined as any })
      }
      setSlideEditorRefs(newRefs)
      // Save slideshow structure
      setTimeout(async () => {
        try {
          await storageAdapter.save(
            projectData.id, projectData.name, dataString,
            projectData.tags, projectData.storageType,
            undefined, projectData.type, initialDeps,
          )
        } catch (error) { console.error("Failed to save slideshow structure:", error) }
      }, 200)
    } else if (layoutType === "multiple") {
      dataString = createProjectData(projectData.type, { blocks: { b1: emptyCells } })
    } else {
      dataString = createProjectData(projectData.type, { blocks: { b1: emptyCells } })
    }

    setCurrentLayout(layoutType)
    setCurrentProjectType((projectData.type || "type1") as ProjectType)
    setCurrentProjectMode(projectData.mode || "free-page")
    setLastProjectLoadTime(Date.now())

    // Initialize editors after layout renders
    setTimeout(() => {
      const lexicalState = cellsToLexical(emptyCells)
      const lexicalStateString = JSON.stringify(lexicalState)
      if (layoutType === "single") {
        setEditorState(JSON.stringify(emptyCells))
        if (editorRef.current) {
          editorRef.current.setEditorState(editorRef.current.parseEditorState(lexicalStateString))
        }
      } else if (layoutType === "multiple") {
        setBlockStates({ b1: JSON.stringify(emptyCells) })
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
    const blocks: Record<string, any> = {}
    if (currentLayout === "single") {
      blocks.b1 = editorState ? JSON.parse(editorState) : null
    } else {
      Object.entries(blockStates).forEach(([blockId, state]) => {
        blocks[blockId] = state ? JSON.parse(state) : null
      })
    }
    const stateToUse = createProjectData(currentProjectType, { blocks })
    const refToUse = currentLayout === "single" ? editorRef : {
      current: Object.values(blockRefs.current)[0] ?? null,
    } as React.RefObject<LexicalEditor | null>

    await titleSave({
      editingProjectName: editingName,
      currentProjectName,
      currentProjectId,
      editorState: stateToUse,
      editorRef: refToUse,
      projectTags,
      storageAdapter,
      setCurrentProjectName,
      setEditingProjectName,
      setIsEditingTitle,
      loadSavedProjectsList: refreshProjects,
    })
  }, [currentProjectId, currentProjectName, currentLayout, editorState, blockStates, currentProjectType, projectTags, isDbInitialized])

  // ═══════════════════════════════════════════════════════════════════════════
  // Slideshow handlers
  // ═══════════════════════════════════════════════════════════════════════════

  const handleConvertToIndependent = useCallback(async (slideId: string) => {
    if (!slideshowStructure || !currentProjectId) return
    try {
      const newIndependentId = generateProjectId()
      const result = convertToIndependent(slideshowStructure, slideId, slideshowDeps, newIndependentId)
      await storageAdapter.save(
        result.extractedProject.id,
        result.extractedProject.name || `Slide ${slideId}`,
        result.extractedProject.data,
        result.extractedProject.tags || [],
        (result.extractedProject.storageType || "local") as StorageType,
        undefined, "type2",
      )
      setSlideshowStructure(result.structure)
      setSlideshowDeps(result.deps)
      toast.success("Slide converted to independent", {
        description: "The project was saved as a standalone type2 project.",
        duration: 3000, icon: "🔓",
      })
    } catch (error) {
      console.error("Failed to convert to independent:", error)
      toast.error("Conversion failed", {
        description: error instanceof Error ? error.message : "Unknown error",
        duration: 4000,
      })
    }
  }, [slideshowStructure, currentProjectId, slideshowDeps, isDbInitialized])

  const handleConvertToDependent = useCallback(async (slideId: string) => {
    if (!slideshowStructure || !currentProjectId) return
    try {
      const slide = slideshowStructure.slides.find(s => s.id === slideId)
      if (!slide || slide.projectRef.isDependent) return
      const independentProject = await storageAdapter.load(slide.projectRef.projectId)
      if (!independentProject) {
        toast.error("Project not found", { description: "Could not load the independent project", duration: 3000 })
        return
      }
      const result = convertToDependent(
        slideshowStructure, slideId, slideshowDeps,
        independentProject as StorageProjectData, currentProjectId,
      )
      setSlideshowStructure(result.structure)
      setSlideshowDeps(result.deps)
      toast.success("Slide unlocked for editing", {
        description: "A dependent copy was created. Changes won't affect the original.",
        duration: 3000, icon: "🔓",
      })
    } catch (error) {
      console.error("Failed to convert to dependent:", error)
      toast.error("Unlock failed", {
        description: error instanceof Error ? error.message : "Unknown error",
        duration: 4000,
      })
    }
  }, [slideshowStructure, currentProjectId, slideshowDeps, isDbInitialized])

  const importConfirm = useCallback((
    slideId: string, projectId: string, loadMode: "snapshot" | "head", snapshotTag?: string,
  ) => {
    if (!slideshowStructure) return
    const slide = slideshowStructure.slides.find(s => s.id === slideId)
    let updatedDeps = slideshowDeps
    if (slide?.projectRef.isDependent) {
      updatedDeps = slideshowDeps.filter(d => d.id !== slide.projectRef.projectId)
    }
    const newStructure = importProjectToSlide(slideshowStructure, slideId, projectId, loadMode, snapshotTag)
    setSlideshowStructure(newStructure)
    setSlideshowDeps(updatedDeps)
    toast.success("Project imported", {
      description: `Slide now references project ${projectId.substring(0, 8)}...`,
      duration: 3000, icon: "📥",
    })
  }, [slideshowStructure, slideshowDeps])

  const handleCreateSnapshot = useCallback(async (name?: string) => {
    if (!currentProjectId) return
    await save()
    await dbStorage.current.createSnapshot(currentProjectId, name)
  }, [currentProjectId, save])

  // ═══════════════════════════════════════════════════════════════════════════
  // Block ops (Type2 layout)
  // ═══════════════════════════════════════════════════════════════════════════

  const addBlock = useCallback(() => {
    const blockNumbers = Object.keys(blockStates).map(key => parseInt(key.slice(1)))
    const nextNum = Math.max(...blockNumbers, 0) + 1
    const newBlockId = `b${nextNum}`
    setBlockStates(prev => ({ ...prev, [newBlockId]: JSON.stringify([]) }))
    blockRefs.current[newBlockId] = null
  }, [blockStates])

  const removeBlock = useCallback((blockId: string) => {
    if (Object.keys(blockStates).length <= 1) return
    setBlockStates(prev => {
      const newStates = { ...prev }
      delete newStates[blockId]
      return newStates
    })
    delete blockRefs.current[blockId]
  }, [blockStates])

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
    projectType: currentProjectType,
    layout: currentLayout,
    engine: currentEngine,
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

    // Editor content (Lexical multi-block)
    blockStates,
    blockRefs,
    setBlockStates,

    // Editor content (Block Array)
    blockArrayBlocks,
    setBlockArrayBlocks,

    // Slideshow
    slideshowStructure, setSlideshowStructure,
    slideshowDeps, setSlideshowDeps,
    currentSlideIndex, setCurrentSlideIndex,
    slideEditorRefs, setSlideEditorRefs,
    previewMode, setPreviewMode,
    resolvedProjects, setResolvedProjects,

    // Operations
    save,
    saveAs,
    loadProject,
    createProject,

    // Title
    titleEdit: handleTitleEdit,
    titleSave: handleTitleSave,

    // Slideshow ops
    convertToIndependent: handleConvertToIndependent,
    convertToDependent: handleConvertToDependent,
    importConfirm,
    createSnapshot: handleCreateSnapshot,

    // Block ops
    addBlock,
    removeBlock,

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
