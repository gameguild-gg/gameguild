"use client"

import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Badge } from "@/components/ui/badge"
import { Save, Eye, Blocks, Home, History, RotateCcw } from "lucide-react"
import { useState, useEffect, useRef, useCallback } from "react"
import { toast } from "sonner"
import Link from "next/link"
import { useRouter } from "next/navigation"
import type { LexicalEditor } from "lexical"
import { OpenProjectDialog } from "@/components/editor/extras/editor/open-project-dialog"
import { CreateProjectDialog } from "@/components/editor/extras/editor/create-project-dialog"
import { SizeDetailsDialog } from "@/components/editor/extras/editor/size-details-dialog"
import { SyncStatusDialog } from "@/components/editor/extras/editor/sync-status-dialog"
import { AutoSaveToggle } from "@/components/editor/extras/editor/auto-save-toggle"
import { ProjectSizeIndicator } from "@/components/editor/extras/editor/project-size-indicator"
import { SyncStatusIndicator } from "@/components/editor/extras/editor/sync-status-indicator"
import { EditableProjectTitle } from "@/components/editor/extras/editor/editable-project-title"
import { ProjectStorageInfo } from "@/components/editor/extras/editor/project-storage-info"
import { ProjectModeIndicator } from "@/components/editor/extras/editor/project-mode-indicator"
import { PreviewModeSelector } from "@/components/editor/extras/editor/preview-mode-selector"
import { handleTitleEdit as titleEdit, handleTitleSave as titleSave } from "@/components/editor/extras/editor/project-title-operations"
import { handleSave as saveProject, handleSaveAs as saveAsProject } from "@/components/editor/extras/editor/project-save-operations"
import { calculateProjectAssetsSize as calculateAssets } from "@/components/editor/extras/editor/project-assets-operations"
import { checkSelectedProject as checkProject } from "@/components/editor/extras/editor/project-load-operations"
import { EditorLayoutType1 } from "@/components/editor/extras/editor/editor-layout-type1"
import { EditorLayoutType2 } from "@/components/editor/extras/editor/editor-layout-type2"
import { EditorLayoutSlideshow } from "@/components/editor/extras/editor/editor-layout-slideshow"
import { ProjectImportDialog } from "@/components/editor/extras/editor/project-import-dialog"
import { EnhancedStorageAdapter, type ProjectPreferences } from "@/lib/storage/editor/enhanced-storage-adapter"
import { syncConfig } from "@/lib/sync/editor/sync-config"
import { SaveAsDialog } from "@/components/editor/extras/editor/save-as-dialog"
import { type ProjectMode } from "@/lib/storage/editor/project-modes"
import { detectProjectLayout, extractEditorStates, createProjectData } from "@/lib/storage/editor/layout-detector"
import { getLayoutFromType, type ProjectType, type InternalLayout, PROJECT_TYPES, type EngineType, ENGINE_TYPES } from "@/lib/storage/editor/project-types"
import { ExitConfirmDialog } from "@/components/editor/extras/dialogs/exit-confirm-dialog"
import { ProjectHistoryDialog } from "@/components/editor/extras/dialogs/project-history-dialog"
import { PreviewRenderer } from "@/components/editor/extras/preview/preview-renderer"
import { PreviewRendererType2 } from "@/components/editor/extras/preview/preview-renderer-type2"
import { PreviewRendererSlideshowContinuous } from "@/components/editor/extras/preview/preview-renderer-slideshow-continuous"
import { PreviewRendererSlideshowSlide } from "@/components/editor/extras/preview/preview-renderer-slideshow-slide"
import type { SerializedEditorState } from "lexical"
import { 
  type SlideshowStructure, 
  type PreviewMode,
  createEmptySlideshowStructure,
  serializeSlideshowStructure,
  convertToIndependent,
  convertToDependent,
  importProjectToSlide,
  getDependentProject,
} from "@/lib/storage/editor/slideshow-structure"
import type { ProjectData as StorageProjectData } from "@/lib/storage/editor/enhanced-storage-adapter"
import type { CellularContent } from "@/lib/storage/editor/cell-structure"
import { BlockArrayEditor } from "@/components/editor/extras/editor/block-array-editor"
import { BlockArrayViewer } from "@/components/editor/extras/editor/block-array-viewer"

interface ProjectData {
  id: string
  name: string
  type: ProjectType // Project type (not layout - layout is auto-detected)
  data: string // Serialized project data (format detected by layout-detector)
  tags: string[]
  size: number
  createdAt: string
  updatedAt: string
  storageType?: "local" | "gameguild-cloud" | "google-drive"
  preferences?: ProjectPreferences
  deps?: StorageProjectData[]
}

// Generate unique ID for projects
function generateProjectId(): string {
  if (typeof crypto !== "undefined" && crypto.randomUUID) {
    return crypto.randomUUID()
  }
  // Fallback for environments without crypto.randomUUID
  return "proj_" + Date.now().toString(36) + "_" + Math.random().toString(36).substr(2, 9)
}

// Função para estimar o tamanho dos dados em KB
function estimateSize(data: string): number {
  return new Blob([data]).size / 1024
}

// Função para formatar tamanho em KB/MB
function formatSize(sizeInKB: number): string {
  if (sizeInKB < 1024) {
    return `${sizeInKB.toFixed(1)}KB`
  } else {
    return `${(sizeInKB / 1024).toFixed(1)}MB`
  }
}

export default function Page() {
  const router = useRouter()
  // Layout detection and state management
  const [currentLayout, setCurrentLayout] = useState<InternalLayout>("single")
  const [currentProjectType, setCurrentProjectType] = useState<ProjectType>(PROJECT_TYPES.TYPE1)
  const [currentEngine, setCurrentEngine] = useState<EngineType>(ENGINE_TYPES.LEXICAL)
  
  // Block Array engine state
  const [blockArrayCells, setBlockArrayCells] = useState<CellularContent>([])
  
  // Type1 states (single editor with b1)
  const [editorState, setEditorState] = useState<string>("")
  const editorRef = useRef<LexicalEditor | null>(null)
  
  // Type2 states (multi-block editors: b1, b2, b3...)
  const [blockStates, setBlockStates] = useState<Record<string, string>>({})
  const blockRefs = useRef<Record<string, LexicalEditor | null>>({})
  
  const [currentProjectId, setCurrentProjectId] = useState<string>("")
  const [currentProjectName, setCurrentProjectName] = useState<string>("")
  const [currentProjectStorageType, setCurrentProjectStorageType] = useState<"local" | "gameguild-cloud" | "google-drive">("local")
  const [saveAsDialogOpen, setSaveAsDialogOpen] = useState(false)
  const [openDialogOpen, setOpenDialogOpen] = useState(false)
  const [newProjectName, setNewProjectName] = useState("")
  const [savedProjects, setSavedProjects] = useState<ProjectData[]>([])
  const [currentProjectSize, setCurrentProjectSize] = useState<number>(0)
  const [currentProjectAssetsSize, setCurrentProjectAssetsSize] = useState<number>(0)
  const [currentProjectAssets, setCurrentProjectAssets] = useState<Array<{ id: string; name: string; size: number; thumbnail?: string; mimeType?: string }>>([])
  const [showSizeDetails, setShowSizeDetails] = useState(false)
  const [totalStorageUsed, setTotalStorageUsed] = useState<number>(0)
  const setLoadingRef = useRef<((loading: boolean) => void) | null>(null)


  const dbStorage = useRef<EnhancedStorageAdapter>(new EnhancedStorageAdapter())
  const [isDbInitialized, setIsDbInitialized] = useState(false)

  // Add these state variables after the existing ones:
  const [syncStats, setSyncStats] = useState<any>(null)
  const [showSyncStatus, setShowSyncStatus] = useState(false)

  // Tamanho recomendado em KB (5120KB)
  const RECOMMENDED_SIZE_KB = 5120

  const [isFirstTime, setIsFirstTime] = useState(true)

  const [projectTags, setProjectTags] = useState<string[]>([])
  const [availableTags, setAvailableTags] = useState<Array<{ name: string; usageCount: number }>>([])

  const [showTagDropdown, setShowTagDropdown] = useState(false)

  const [createDialogOpen, setCreateDialogOpen] = useState(false)

  // Add these state variables after the existing state declarations:
  const [isEditingTitle, setIsEditingTitle] = useState(false)
  const [editingProjectName, setEditingProjectName] = useState("")
  const [previewOpen, setPreviewOpen] = useState(false)
  const [previewState, setPreviewState] = useState<SerializedEditorState | null>(null)
  const [previewBlockStates, setPreviewBlockStates] = useState<Record<string, SerializedEditorState>>({})
  const [previewLayout, setPreviewLayout] = useState<InternalLayout>("single")
  const [previewSlideshowStructure, setPreviewSlideshowStructure] = useState<SlideshowStructure | null>(null)
  const [previewSlideshowMode, setPreviewSlideshowMode] = useState<PreviewMode>("continuous")
  const [lastProjectLoadTime, setLastProjectLoadTime] = useState<number>(0)
  const [currentProjectMode, setCurrentProjectMode] = useState<ProjectMode>("free-page")
  const [currentProjectPreferences, setCurrentProjectPreferences] = useState<ProjectPreferences | undefined>(undefined)

  // Slideshow slide states
  const [slideshowStructure, setSlideshowStructure] = useState<SlideshowStructure | null>(null)
  const [currentSlideIndex, setCurrentSlideIndex] = useState(0)
  const [slideEditorRefs, setSlideEditorRefs] = useState<Map<string, React.RefObject<LexicalEditor>>>(new Map())
  const [previewMode, setPreviewMode] = useState<PreviewMode>("continuous")
  const [slideshowDeps, setSlideshowDeps] = useState<StorageProjectData[]>([])
  const [resolvedProjects, setResolvedProjects] = useState<Map<string, StorageProjectData | null>>(new Map())
  
  const [nextUrl, setNextUrl] = useState<string | null>(null)
  const [exitDialogOpen, setExitDialogOpen] = useState(false)

  // History viewing state
  const [historyDialogOpen, setHistoryDialogOpen] = useState(false)
  const [isViewingHistory, setIsViewingHistory] = useState(false)
  const [currentViewingSha, setCurrentViewingSha] = useState<string | null>(null)
  const [headProjectData, setHeadProjectData] = useState<string | null>(null) // Store HEAD data when viewing history
  const [headSlideshowDeps, setHeadSlideshowDeps] = useState<StorageProjectData[]>([]) // Store HEAD deps when viewing history

  // NOTE: Independent projects are loaded inline during:
  // 1. checkProject (URL hash loading) - in project-load-operations.ts
  // 2. onProjectLoad (Open dialog loading) - below in onProjectLoad callback
  // No useEffect needed here since both loading paths handle it inline

  const handleLinkNavigation = (event: React.MouseEvent<HTMLAnchorElement>, url: string) => {
    if (event.ctrlKey || event.metaKey || event.button === 1) {
      return
    }
    event.preventDefault()

    if (currentProjectId && editorState) {
      setNextUrl(url)
      setExitDialogOpen(true)
    } else {
      router.push(url)
    }
  }

  const handlePreferencesChange = (newPreferences: ProjectPreferences) => {
    // Update preferences in memory - they will be saved when project is saved
    setCurrentProjectPreferences(newPreferences)
  }

  // Initialize IndexedDB and load projects
  useEffect(() => {
    const initDB = async () => {
      try {
        await dbStorage.current.init()
        setIsDbInitialized(true)
        await loadSavedProjectsList()
        await loadAvailableTags()
      } catch (error) {
        console.error("Failed to initialize IndexedDB:", error)
        toast.error("Storage error", {
          description: "Unable to initialize database. Some features may not work.",
          duration: 5000,
          icon: "⚠️",
        })
      }
    }

    initDB()
  }, [])

  // Force open dialog on first visit
  useEffect(() => {
    if (!isDbInitialized) return
    
    // Check if there's a selected project from the main page or URL hash
    const checkSelectedProject = async () => {
      await checkProject({
        storageAdapter,
        // Pass directDbLoad to bypass closure issues with isDbInitialized
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
      })
    }
    
    checkSelectedProject()
  }, [isDbInitialized])

  // Function to calculate total asset size for current project
  const calculateProjectAssetsSize = async (projectId: string) => {
    await calculateAssets({
      projectId,
      setCurrentProjectAssetsSize,
      setCurrentProjectAssets,
    })
  }

  // Atualizar informações de armazenamento sempre que o editor mudar
  useEffect(() => {
    let dataToCalculate: string
    
    if (currentLayout === "slideshow" && slideshowStructure) {
      dataToCalculate = serializeSlideshowStructure(slideshowStructure)
    } else {
      const blocks: Record<string, any> = {}
      if (currentLayout === "single") {
        blocks.b1 = editorState ? JSON.parse(editorState) : null
      } else {
        // Multi-block: parse all block states
        Object.entries(blockStates).forEach(([blockId, state]) => {
          blocks[blockId] = state ? JSON.parse(state) : null
        })
      }
      dataToCalculate = createProjectData(currentProjectType, { blocks })
    }
    
    const size = estimateSize(dataToCalculate)
    setCurrentProjectSize(size)
  }, [editorState, blockStates, currentLayout, slideshowStructure])

  // Calculate assets size when project changes or editor content changes
  useEffect(() => {
    if (currentProjectId && isDbInitialized) {
      calculateProjectAssetsSize(currentProjectId)
    } else {
      setCurrentProjectAssetsSize(0)
    }
  }, [currentProjectId, isDbInitialized, editorState, blockStates, slideshowStructure])

  const storageAdapter = {
    save: async (id: string, name: string, data: string, tags: string[] = [], storageType: "local" | "gameguild-cloud" | "google-drive" = "local", preferences?: ProjectPreferences, type: string = "type1", deps?: StorageProjectData[]) => {
      if (!id || !name || !data) {
        console.warn("Invalid id, name or data")
        return
      }

      if (!isDbInitialized) {
        throw new Error("Database not initialized")
      }

      const originalSize = estimateSize(data)
      console.log(`Saving project "${name}" (${id}) to ${storageType} - Size: ${formatSize(originalSize)}`)

      try {
        await dbStorage.current.save(id, name, data, tags, storageType, preferences, type as any, deps)
        console.log(`Saved project "${name}" (${id}) to ${storageType} successfully`)
      } catch (error) {
        console.error("Failed to save project:", error)
        throw error
      }
    },

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

    delete: async (id: string) => {
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

    getProjectInfo: async (id: string) => {
      if (!isDbInitialized) {
        return null
      }

      try {
        return await dbStorage.current.getProjectInfo(id)
      } catch (error) {
        console.error("Failed to get project info:", error)
        return null
      }
    },

    searchProjects: async (
      searchTerm: string,
      tags: string[],
      filterMode: "all" | "any" = "any",
      storageTypeFilter?: "local" | "gameguild-cloud" | "google-drive",
    ): Promise<ProjectData[]> => {
      if (!isDbInitialized) {
        return []
      }

      try {
        return await dbStorage.current.searchProjects(searchTerm, tags, filterMode, storageTypeFilter)
      } catch (error) {
        console.error("Failed to search projects:", error)
        return []
      }
    },
  }

  const loadSavedProjectsList = async () => {
    try {
      const projects = await storageAdapter.list()
      setSavedProjects(projects)
    } catch (error) {
      console.error("Failed to load projects list:", error)
    }
  }

  const loadAvailableTags = async () => {
    try {
      const tags = await dbStorage.current.getAllTags()
      setAvailableTags(tags)
    } catch (error) {
      console.error("Failed to load tags:", error)
    }
  }

  // Add this useEffect after the existing ones:
  useEffect(() => {
    if (!isDbInitialized) return

    const updateSyncStats = async () => {
      try {
        const stats = await dbStorage.current.getSyncStats()
        setSyncStats(stats)
      } catch (error) {
        console.error("Failed to get sync stats:", error)
      }
    }

    // Update sync stats every 5 seconds
    const interval = setInterval(updateSyncStats, 5000)
    updateSyncStats() // Initial update

    // Setup sync event listeners
    dbStorage.current.onSyncStart(() => {
      console.log("Sync started")
      updateSyncStats()
    })

    dbStorage.current.onSyncComplete((stats) => {
      console.log("Sync completed:", stats)
      updateSyncStats()
      if (stats.processed > 0) {
        toast.success("Synchronization completed", {
          description: `${stats.processed} synchronized projects`,
          duration: 3000,
          icon: "🔄",
        })
      }
    })

    dbStorage.current.onSyncError((error) => {
      console.error("Sync error:", error)
      updateSyncStats()
      toast.error("Synchronization error", {
        description: "Some projects may not be synchronized",
        duration: 4000,
        icon: "⚠️",
      })
    })

    return () => {
      clearInterval(interval)
    }
  }, [isDbInitialized])

  const handleSave = async () => {
    // Prepare the correct state based on engine and layout type
    let dataToSave: string
    
    if (currentEngine === ENGINE_TYPES.BLOCKS) {
      // Block Array engine: save cells directly
      dataToSave = createProjectData(currentProjectType, {
        blocks: { b1: blockArrayCells },
      })
      
      const preferences: ProjectPreferences = {
        global: {
          ...currentProjectPreferences?.global,
          mode: currentProjectMode,
        },
        nodes: currentProjectPreferences?.nodes || {}
      }
      
      await saveProject({
        currentProjectId,
        currentProjectName,
        currentProjectStorageType,
        editorState: dataToSave,
        editorRef: { current: null } as React.RefObject<LexicalEditor | null>,
        projectTags,
        storageAdapter,
        calculateProjectAssetsSize,
        setSaveAsDialogOpen,
        preferences,
        type: currentProjectType,
      })
      return
    }
    
    if (currentLayout === "slideshow" && slideshowStructure) {
      // Slideshow layout: serialize the structure
      dataToSave = serializeSlideshowStructure(slideshowStructure)
    } else {
      // Single or Multi-block layout
      const blocks: Record<string, any> = {}
      if (currentLayout === "single") {
        blocks.b1 = editorState ? JSON.parse(editorState) : null
      } else {
        // Multi-block: parse all block states
        Object.entries(blockStates).forEach(([blockId, state]) => {
          blocks[blockId] = state ? JSON.parse(state) : null
        })
      }
      dataToSave = createProjectData(currentProjectType, { blocks })
    }
    
    const refToUse = currentLayout === "single" ? editorRef : {
      current: Object.values(blockRefs.current)[0] ?? null
    } as React.RefObject<LexicalEditor | null>
    
    // Build preferences, always including current previewMode for slideshow
    const preferences: ProjectPreferences = {
      global: {
        ...currentProjectPreferences?.global,
        mode: currentProjectMode,
        ...(currentLayout === "slideshow" && { previewMode: previewMode })
      },
      nodes: currentProjectPreferences?.nodes || {}
    }
    
    await saveProject({
      currentProjectId,
      currentProjectName,
      currentProjectStorageType,
      editorState: dataToSave,
      editorRef: refToUse,
      projectTags,
      storageAdapter,
      calculateProjectAssetsSize,
      setSaveAsDialogOpen,
      preferences,
      type: currentProjectType,
      deps: currentLayout === "slideshow" ? slideshowDeps : undefined,
    })
  }

  const handleSaveAs = async (storageOption: "local" | "gameguild-cloud" | "google-drive" = "local") => {
    // Prepare the correct state based on layout type
    let dataToSave: string
    
    if (currentLayout === "slideshow" && slideshowStructure) {
      // Slideshow layout: serialize the structure
      dataToSave = serializeSlideshowStructure(slideshowStructure)
    } else {
      // Single or Multi-block layout
      const blocks: Record<string, any> = {}
      if (currentLayout === "single") {
        blocks.b1 = editorState ? JSON.parse(editorState) : null
      } else {
        // Multi-block: parse all block states
        Object.entries(blockStates).forEach(([blockId, state]) => {
          blocks[blockId] = state ? JSON.parse(state) : null
        })
      }
      dataToSave = createProjectData(currentProjectType, { blocks })
    }
    
    const refToUse = currentLayout === "single" ? editorRef : {
      current: Object.values(blockRefs.current)[0] ?? null
    } as React.RefObject<LexicalEditor | null>
    
    await saveAsProject({
      newProjectName,
      editorState: dataToSave,
      editorRef: refToUse,
      projectTags,
      storageOption,
      storageAdapter,
      generateProjectId,
      setCurrentProjectId,
      setCurrentProjectName,
      setCurrentProjectStorageType,
      setNewProjectName,
      setSaveAsDialogOpen,
      loadSavedProjectsList,
      calculateProjectAssetsSize,
    })
  }

  // Determinar a cor do indicador de tamanho
  const getSizeIndicatorColor = () => {
    if (currentProjectSize > RECOMMENDED_SIZE_KB * 2) return "text-red-600"
    if (currentProjectSize > RECOMMENDED_SIZE_KB) return "text-amber-600"
    return "text-green-600"
  }

  const [autoSaveEnabled, setAutoSaveEnabled] = useState(false)

  const handleSaveRef = useRef(handleSave)
  const isViewingHistoryRef = useRef(isViewingHistory)

  useEffect(() => {
    // Update the ref when handleSave changes
    handleSaveRef.current = handleSave
    isViewingHistoryRef.current = isViewingHistory
  })

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.ctrlKey && event.key === "s") {
        event.preventDefault() // Prevent browser's default save dialog
        // Block save when viewing history (read-only mode)
        if (isViewingHistoryRef.current) {
          return
        }
        handleSaveRef.current()
      }
    }

    document.addEventListener("keydown", handleKeyDown)

    return () => {
      document.removeEventListener("keydown", handleKeyDown)
    }
  }, [])

  // Auto-save functionality
  useEffect(() => {
    // Block auto-save when viewing history (read-only mode)
    if (!autoSaveEnabled || !currentProjectId || !isDbInitialized || isViewingHistory) return

    // Check if we have any content to save
    const hasContent = currentLayout === "single" 
      ? editorState 
      : Object.keys(blockStates).length > 0
    
    if (!hasContent) return

    // Wait 1 second after project load before enabling auto-save
    const timeSinceLoad = Date.now() - lastProjectLoadTime
    if (timeSinceLoad < 1000) {
      return
    }

    const autoSaveTimer = setTimeout(async () => {
      try {
        // Prepare the correct state based on layout type
        const blocks: Record<string, any> = {}
        if (currentLayout === "single") {
          blocks.b1 = editorState ? JSON.parse(editorState) : null
        } else {
          // Multi-block: parse all block states
          Object.entries(blockStates).forEach(([blockId, state]) => {
            blocks[blockId] = state ? JSON.parse(state) : null
          })
        }
        const dataToSave = createProjectData(currentProjectType, { blocks })
        
        await storageAdapter.save(
          currentProjectId, 
          currentProjectName, 
          dataToSave, 
          projectTags,
          currentProjectStorageType,
          currentProjectPreferences,
          currentProjectType
        )
        
        // Show a very subtle auto-save notification
        toast.success("Auto-saved", {
          description: "Changes saved automatically",
          duration: 1500,
          icon: "💾",
          style: {
            opacity: 0.8,
            fontSize: "0.875rem",
          },
        })
        console.log("Auto-saved project:", currentProjectName)
      } catch (error) {
        console.error("Auto-save failed:", error)
        toast.error("Auto-save failed", {
          description: "Save manually to ensure",
          duration: 2000,
          icon: "⚠️",
        })
      }
    }, 2000) // Auto-save after 2 seconds of inactivity

    return () => clearTimeout(autoSaveTimer)
  }, [editorState, blockStates, autoSaveEnabled, currentProjectId, currentProjectName, projectTags, isDbInitialized, currentLayout, currentProjectType, currentProjectStorageType, lastProjectLoadTime, isViewingHistory])



  // Close tag dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (showTagDropdown) {
        const target = event.target as Element
        if (!target.closest(".relative")) {
          setShowTagDropdown(false)
        }
      }
    }

    document.addEventListener("mousedown", handleClickOutside)
    return () => document.removeEventListener("mousedown", handleClickOutside)
  }, [showTagDropdown])

  // Title operations
  const handleTitleEdit = () => {
    titleEdit({
      currentProjectId,
      currentProjectName,
      setEditingProjectName,
      setIsEditingTitle,
    })
  }

  const handleTitleSave = async () => {
    // Prepare the correct state and ref based on layout type
    const blocks: Record<string, any> = {}
    if (currentLayout === "single") {
      blocks.b1 = editorState ? JSON.parse(editorState) : null
    } else {
      // Multi-block: parse all block states
      Object.entries(blockStates).forEach(([blockId, state]) => {
        blocks[blockId] = state ? JSON.parse(state) : null
      })
    }
    const stateToUse = createProjectData(currentProjectType, { blocks })
    
    const refToUse = currentLayout === "single" ? editorRef : {
      current: Object.values(blockRefs.current)[0] ?? null
    } as React.RefObject<LexicalEditor | null>
    
    await titleSave({
      editingProjectName,
      currentProjectName,
      currentProjectId,
      editorState: stateToUse,
      editorRef: refToUse,
      projectTags,
      storageAdapter,
      setCurrentProjectName,
      setEditingProjectName,
      setIsEditingTitle,
      loadSavedProjectsList,
    })
  }

  const handleNavigation = (url: string) => {
    if (currentProjectId && editorState) {
      setNextUrl(url)
      setExitDialogOpen(true)
    } else {
      router.push(url)
    }
  }

  const handleSaveAndExit = async () => {
    await handleSave()
    if (nextUrl) {
      router.push(nextUrl)
    }
    setExitDialogOpen(false)
  }

  const handlePreview = () => {
    if (!currentProjectId) {
      toast.error("No project loaded", {
        description: "Please load or create a project first",
        duration: 3000,
      })
      return
    }

    try {
      // Handle preview based on layout type
      if (currentLayout === "slideshow") {
        // Slideshow layout
        if (!slideshowStructure || slideshowStructure.slides.length === 0) {
          toast.error("No content", {
            description: "Slideshow structure is empty",
            duration: 3000,
          })
          return
        }
        
        setPreviewSlideshowStructure(slideshowStructure)
        setPreviewSlideshowMode(previewMode)
        setPreviewLayout("slideshow")
        setPreviewOpen(true)
      } else if (currentLayout === "single") {
        if (!editorState) {
          toast.error("No content", {
            description: "Editor is empty",
            duration: 3000,
          })
          return
        }
        
        // Parse cells and convert to Lexical for preview
        const parsed = JSON.parse(editorState)
        const { cellsToLexical } = require("@/lib/storage/editor/cell-structure")
        const lexicalState = cellsToLexical(parsed)
        setPreviewState(lexicalState)
        setPreviewLayout("single")
        setPreviewOpen(true)
      } else {
        // Multi-block panel
        if (Object.keys(blockStates).length < 1) {
          toast.error("No content", {
            description: "Need at least 1 block for preview",
            duration: 3000,
          })
          return
        }
        
        // Parse all block states dynamically
        const { cellsToLexical } = require("@/lib/storage/editor/cell-structure")
        const parsedStates: Record<string, SerializedEditorState> = {}
        for (const [blockId, state] of Object.entries(blockStates)) {
          if (state) {
            try {
              const cellsData = JSON.parse(state)
              parsedStates[blockId] = cellsToLexical(cellsData)
            } catch (error) {
              console.error(`Failed to parse block ${blockId}:`, error)
            }
          }
        }
        
        if (Object.keys(parsedStates).length < 1) {
          toast.error("Invalid content", {
            description: "At least 1 block must have valid content",
            duration: 3000,
          })
          return
        }
        
        // Set all parsed states dynamically
        setPreviewBlockStates(parsedStates)
        setPreviewLayout("multiple")
        setPreviewOpen(true)
      }
    } catch (error) {
      console.error("Failed to parse editor state:", error)
      toast.error("Preview error", {
        description: "Failed to load preview",
        duration: 3000,
      })
    }
  }

  // History management handlers
  const handleLoadCommit = async (sha: string) => {
    if (!currentProjectId) return

    try {
      // Check if this commit is HEAD (first commit in history)
      const history = await dbStorage.current.listHistory(currentProjectId)
      const isHead = history.length > 0 && history[0]?.sha === sha
      
      // If loading HEAD, just return to normal mode (not read-only)
      if (isHead) {
        // If already viewing history, restore HEAD data
        if (isViewingHistory && headProjectData) {
          const layoutInfo = detectProjectLayout(headProjectData)
          const states = extractEditorStates(headProjectData, currentProjectType)
          
          if (layoutInfo.hasSlides && layoutInfo.slideshowData) {
            setSlideshowStructure(layoutInfo.slideshowData)
            setSlideshowDeps(headSlideshowDeps)
            setCurrentSlideIndex(0)
          } else if (currentLayout === "single" && states.blocks.b1) {
            setEditorState(JSON.stringify(states.blocks.b1))
            if (editorRef.current) {
              const { cellsToLexical } = require("@/lib/storage/editor/cell-structure")
              const lexicalState = cellsToLexical(states.blocks.b1)
              const editorState = editorRef.current.parseEditorState(JSON.stringify(lexicalState))
              editorRef.current.setEditorState(editorState)
            }
          } else if (currentLayout === "multiple" && states.blocks) {
            const newBlockStates: Record<string, string> = {}
            Object.entries(states.blocks).forEach(([blockId, blockState]: [string, any]) => {
              if (blockState) {
                newBlockStates[blockId] = JSON.stringify(blockState)
              }
            })
            setBlockStates(newBlockStates)
          }
        }
        
        setIsViewingHistory(false)
        setCurrentViewingSha(null)
        setHeadProjectData(null)
        setHeadSlideshowDeps([])
        
        toast.success("Viewing latest version", {
          description: "You can edit the project",
          duration: 2000,
          icon: "✏️",
        })
        return
      }
      
      // Store current HEAD data if not already viewing history
      if (!isViewingHistory) {
        let currentData: string
        if (currentLayout === "slideshow" && slideshowStructure) {
          currentData = serializeSlideshowStructure(slideshowStructure)
        } else {
          const blocks: Record<string, any> = {}
          if (currentLayout === "single") {
            blocks.b1 = editorState ? JSON.parse(editorState) : null
          } else {
            Object.entries(blockStates).forEach(([blockId, state]) => {
              blocks[blockId] = state ? JSON.parse(state) : null
            })
          }
          currentData = createProjectData(currentProjectType, { blocks })
        }
        setHeadProjectData(currentData)
        if (currentLayout === "slideshow") {
          setHeadSlideshowDeps([...slideshowDeps])
        }
      }

      // Load the commit data
      const commitData = await dbStorage.current.loadFromHistory(currentProjectId, sha)
      if (!commitData) {
        toast.error("Failed to load commit", {
          description: "The historical version could not be found",
          duration: 3000,
        })
        return
      }
      
      if (!commitData.data || !commitData.type) {
        toast.error("Invalid historical data", {
          description: "This commit contains incomplete data. It may be from an older version.",
          duration: 4000,
        })
        console.error("Invalid commit data:", { hasData: !!commitData.data, hasType: !!commitData.type, sha })
        return
      }

      // Load into editor (similar to onProjectLoad)
      const layoutInfo = detectProjectLayout(commitData.data)
      const states = extractEditorStates(commitData.data, commitData.type)
      
      if (layoutInfo.hasSlides && layoutInfo.slideshowData) {
        setSlideshowStructure(layoutInfo.slideshowData)
        setSlideshowDeps(commitData.deps || [])
        setCurrentSlideIndex(0)
      } else if (currentLayout === "single" && states.blocks.b1) {
        setEditorState(JSON.stringify(states.blocks.b1))
        if (editorRef.current) {
          const { cellsToLexical } = require("@/lib/storage/editor/cell-structure")
          const lexicalState = cellsToLexical(states.blocks.b1)
          const editorState = editorRef.current.parseEditorState(JSON.stringify(lexicalState))
          editorRef.current.setEditorState(editorState)
        }
      } else if (currentLayout === "multiple" && states.blocks) {
        const newBlockStates: Record<string, string> = {}
        Object.entries(states.blocks).forEach(([blockId, blockState]: [string, any]) => {
          if (blockState) {
            newBlockStates[blockId] = JSON.stringify(blockState)
          }
        })
        setBlockStates(newBlockStates)
      }

      setIsViewingHistory(true)
      setCurrentViewingSha(sha)
      
      toast.info("Viewing historical version", {
        description: "This is read-only. Return to latest to edit.",
        duration: 3000,
        icon: "📜",
      })
    } catch (error) {
      console.error("Failed to load commit:", error)
      toast.error("Failed to load historical version", {
        description: error instanceof Error ? error.message : "Unknown error",
        duration: 4000,
      })
    }
  }

  const handleLoadSnapshot = async (tag: string) => {
    if (!currentProjectId) return

    // Store current HEAD data if not already viewing history
    if (!isViewingHistory) {
      let currentData: string
      if (currentLayout === "slideshow" && slideshowStructure) {
        currentData = serializeSlideshowStructure(slideshowStructure)
      } else {
        const blocks: Record<string, any> = {}
        if (currentLayout === "single") {
          blocks.b1 = editorState ? JSON.parse(editorState) : null
        } else {
          Object.entries(blockStates).forEach(([blockId, state]) => {
            blocks[blockId] = state ? JSON.parse(state) : null
          })
        }
        currentData = createProjectData(currentProjectType, { blocks })
      }
      setHeadProjectData(currentData)
      if (currentLayout === "slideshow") {
        setHeadSlideshowDeps([...slideshowDeps])
      }
    }

    // Load the snapshot
    const snapshots = await dbStorage.current.listSnapshots(currentProjectId)
    const snapshot = snapshots.find(s => s.tag === tag)
    if (snapshot) {
      await handleLoadCommit(snapshot.sha)
    }
  }

  const handleReturnToHead = async () => {
    if (!currentProjectId || !headProjectData) return

    // Restore HEAD data
    const layoutInfo = detectProjectLayout(headProjectData)
    const states = extractEditorStates(headProjectData, currentProjectType)
    
    if (layoutInfo.hasSlides && layoutInfo.slideshowData) {
      setSlideshowStructure(layoutInfo.slideshowData)
      setSlideshowDeps(headSlideshowDeps)
      setCurrentSlideIndex(0)
    } else if (currentLayout === "single" && states.blocks.b1) {
      setEditorState(JSON.stringify(states.blocks.b1))
      if (editorRef.current) {
        const { cellsToLexical } = require("@/lib/storage/editor/cell-structure")
        const lexicalState = cellsToLexical(states.blocks.b1)
        const editorState = editorRef.current.parseEditorState(JSON.stringify(lexicalState))
        editorRef.current.setEditorState(editorState)
      }
    } else if (currentLayout === "multiple" && states.blocks) {
      const newBlockStates: Record<string, string> = {}
      Object.entries(states.blocks).forEach(([blockId, blockState]: [string, any]) => {
        if (blockState) {
          newBlockStates[blockId] = JSON.stringify(blockState)
        }
      })
      setBlockStates(newBlockStates)
    }

    setIsViewingHistory(false)
    setCurrentViewingSha(null)
    setHeadProjectData(null)
    setHeadSlideshowDeps([])
    
    toast.success("Returned to latest version", {
      description: "You can now edit the project",
      duration: 2000,
      icon: "✏️",
    })
  }

  // --- Slideshow slide-project management handlers ---

  const [importDialogOpen, setImportDialogOpen] = useState(false)
  const [importTargetSlideId, setImportTargetSlideId] = useState<string | null>(null)

  const handleConvertToIndependent = async (slideId: string) => {
    if (!slideshowStructure || !currentProjectId) return
    try {
      const newIndependentId = generateProjectId()
      const result = convertToIndependent(slideshowStructure, slideId, slideshowDeps, newIndependentId)
      
      // Save the extracted project as a standalone project
      await storageAdapter.save(
        result.extractedProject.id,
        result.extractedProject.name || `Slide ${slideId}`,
        result.extractedProject.data,
        result.extractedProject.tags || [],
        (result.extractedProject.storageType || "local") as "local" | "gameguild-cloud" | "google-drive",
        undefined,
        "type2"
      )
      
      setSlideshowStructure(result.structure)
      setSlideshowDeps(result.deps)
      
      toast.success("Slide converted to independent", {
        description: "The project was saved as a standalone type2 project.",
        duration: 3000,
        icon: "🔓",
      })
    } catch (error) {
      console.error("Failed to convert to independent:", error)
      toast.error("Conversion failed", {
        description: error instanceof Error ? error.message : "Unknown error",
        duration: 4000,
      })
    }
  }

  const handleConvertToDependent = async (slideId: string) => {
    if (!slideshowStructure || !currentProjectId) return
    try {
      const slide = slideshowStructure.slides.find(s => s.id === slideId)
      if (!slide || slide.projectRef.isDependent) return
      
      // Load the independent project data
      const independentProject = await storageAdapter.load(slide.projectRef.projectId)
      if (!independentProject) {
        toast.error("Project not found", {
          description: "Could not load the independent project",
          duration: 3000,
        })
        return
      }
      
      const result = convertToDependent(
        slideshowStructure, slideId, slideshowDeps,
        independentProject as StorageProjectData, currentProjectId
      )
      
      setSlideshowStructure(result.structure)
      setSlideshowDeps(result.deps)
      
      toast.success("Slide unlocked for editing", {
        description: "A dependent copy was created. Changes won't affect the original.",
        duration: 3000,
        icon: "🔓",
      })
    } catch (error) {
      console.error("Failed to convert to dependent:", error)
      toast.error("Unlock failed", {
        description: error instanceof Error ? error.message : "Unknown error",
        duration: 4000,
      })
    }
  }

  const handleImportProject = (slideId: string) => {
    setImportTargetSlideId(slideId)
    setImportDialogOpen(true)
  }

  const handleImportConfirm = (projectId: string, loadMode: 'snapshot' | 'head', snapshotTag?: string) => {
    if (!slideshowStructure || !importTargetSlideId) return
    
    // Remove old dependent project from deps if the slide was dependent
    const slide = slideshowStructure.slides.find(s => s.id === importTargetSlideId)
    let updatedDeps = slideshowDeps
    if (slide?.projectRef.isDependent) {
      updatedDeps = slideshowDeps.filter(d => d.id !== slide.projectRef.projectId)
    }
    
    const newStructure = importProjectToSlide(
      slideshowStructure, importTargetSlideId,
      projectId, loadMode, snapshotTag
    )
    
    setSlideshowStructure(newStructure)
    setSlideshowDeps(updatedDeps)
    setImportDialogOpen(false)
    setImportTargetSlideId(null)
    
    toast.success("Project imported", {
      description: `Slide now references project ${projectId.substring(0, 8)}...`,
      duration: 3000,
      icon: "📥",
    })
  }

  const handleCreateSnapshot = async (name?: string) => {
    if (!currentProjectId) return
    
    // First save current state
    await handleSave()
    
    // Then create snapshot
    await dbStorage.current.createSnapshot(currentProjectId, name)
  }

  return (
    <>
      <div className="min-h-screen bg-gray-50 dark:bg-gray-950">
        <div className="container mx-auto py-8">
          <div className={`mx-auto space-y-6 px-4 sm:px-4 lg:px-4 ${currentLayout === "single" ? "max-w-4xl" : "max-w-9xl"}`}>
            {/* Professional Header */}
            <div className="border border-gray-200 bg-white shadow-sm dark:border-gray-700 dark:bg-gray-900">
              <div className="flex items-center justify-between p-3 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
                <div className="flex items-center gap-3 flex-1 min-w-0">
                  <div className="p-1.5 bg-blue-50 dark:bg-blue-900/30 shrink-0">
                    <Blocks className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                  </div>
                  <div className="flex items-center gap-3 flex-1 min-w-0">
                    <h1 className="text-base font-semibold text-gray-900 dark:text-gray-100 whitespace-nowrap shrink-0">Content Studio</h1>
                    <div className="h-4 w-px bg-gray-300 dark:bg-gray-600 shrink-0"></div>
                    <div className="flex items-center gap-2 flex-1 min-w-0">
                      {currentProjectId ? (
                        <div className="min-w-0 flex-1">
                          <EditableProjectTitle
                            projectName={currentProjectName}
                            isEditing={isEditingTitle}
                            editingName={editingProjectName}
                            onEditStart={handleTitleEdit}
                            onEditEnd={() => {
                              setIsEditingTitle(false)
                              setEditingProjectName(currentProjectName)
                            }}
                            onNameChange={setEditingProjectName}
                            onSave={handleTitleSave}
                          />
                        </div>
                      ) : (
                        <span className="text-sm text-gray-500 dark:text-gray-400 italic">Untitled Project</span>
                      )}
                      {currentProjectId && (
                        <div className="flex items-center gap-2 shrink-0">
                          <ProjectModeIndicator mode={currentProjectMode} />
                          <ProjectStorageInfo storageType={currentProjectStorageType} />
                        </div>
                      )}
                    </div>
                  </div>
                </div>
                <div className="flex items-center gap-2">
                  <Link href="/gglexical" passHref>
                    <Button
                      onClick={(e: any) => handleLinkNavigation(e, "/gglexical")}
                      variant="ghost"
                      size="sm"
                      className="gap-2 hover:bg-gray-100 dark:hover:bg-gray-800"
                    >
                      <Home className="h-4 w-4" />
                      Home
                    </Button>
                  </Link>
                  <Link href="/gglexical/viewer" passHref>
                    <Button
                      onClick={(e: any) => handleLinkNavigation(e, "/gglexical/viewer")}
                      variant="ghost"
                      size="sm"
                      className="gap-2 hover:bg-gray-100 dark:hover:bg-gray-800"
                    >
                      <Eye className="h-4 w-4" />
                      Viewer
                    </Button>
                  </Link>
                </div>
              </div>

              {/* Action Bar */}
              <div className="flex items-center justify-between gap-4 p-4 bg-white dark:bg-gray-900">
                <div className="flex items-center gap-3">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={handleSave}
                    className="gap-2 border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800 bg-transparent"
                    disabled={!isDbInitialized || isViewingHistory}
                    title={isViewingHistory ? "Return to latest to save" : "Save project"}
                  >
                    <Save className="h-4 w-4" />
                    Save
                  </Button>

                  <SaveAsDialog
                    open={saveAsDialogOpen}
                    onOpenChange={setSaveAsDialogOpen}
                    projectName={newProjectName}
                    onProjectNameChange={setNewProjectName}
                    onSave={handleSaveAs}
                    currentProjectSize={currentProjectSize}
                    getSizeIndicatorColor={getSizeIndicatorColor}
                    formatSize={formatSize}
                    isDbInitialized={isDbInitialized}
                  />

                  <OpenProjectDialog
                    open={openDialogOpen}
                    onOpenChange={setOpenDialogOpen}
                    isFirstTime={isFirstTime}
                    isDbInitialized={isDbInitialized}
                    storageAdapter={storageAdapter}
                    availableTags={availableTags}
                    editorRef={editorRef}
                    blockRefs={blockRefs}
                    setLoadingRef={setLoadingRef}
                    onProjectLoad={(projectData) => {
                      // Detect engine type
                      const projectEngine: EngineType = (projectData as any).engine || ENGINE_TYPES.LEXICAL
                      setCurrentEngine(projectEngine)
                      
                      if (projectEngine === ENGINE_TYPES.BLOCKS) {
                        // Block Array engine: load cells directly
                        const states = extractEditorStates(projectData.data, projectData.type)
                        const cellsData = states.blocks.b1 || []
                        setBlockArrayCells(Array.isArray(cellsData) ? cellsData : [])
                        
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
                        window.history.pushState(null, '', `#${projectData.id}`)
                        return
                      }
                      
                      // Lexical engine: existing flow
                      // Detectar layout automaticamente baseado na estrutura de data
                      const layoutInfo = detectProjectLayout(projectData.data)
                      
                      // Layout é derivado diretamente do tipo de projeto
                      const finalLayout = getLayoutFromType(projectData.type)
                      
                      // Extract mode from preferences or default to free-page
                      const projectMode = projectData.preferences?.global?.mode || "free-page"
                      
                      // Update project metadata
                      setCurrentProjectId(projectData.id)
                      setCurrentProjectName(projectData.name)
                      setCurrentProjectType(projectData.type)  // tipo do projeto
                      setCurrentLayout(finalLayout)  // layout derivado do tipo
                      setCurrentProjectStorageType(projectData.storageType || "local")
                      setProjectTags(projectData.tags || [])
                      setCurrentProjectMode(projectMode)
                      setCurrentProjectPreferences(projectData.preferences)
                      setIsFirstTime(false)
                      
                      // Mark project load time to prevent auto-save for 1 second
                      setLastProjectLoadTime(Date.now())
                      
                      // Handle slideshow layout
                      if (layoutInfo.hasSlides && layoutInfo.slideshowData) {
                        setSlideshowStructure(layoutInfo.slideshowData)
                        setSlideshowDeps(projectData.deps || [])
                        setCurrentSlideIndex(0)
                        
                        // Load previewMode from preferences or default to continuous
                        const savedPreviewMode = projectData.preferences?.global?.previewMode || "continuous"
                        setPreviewMode(savedPreviewMode as PreviewMode)
                        
                        // Initialize editor refs for all slides
                        const newRefs = new Map<string, React.RefObject<LexicalEditor>>()
                        layoutInfo.slideshowData.slides.forEach(slide => {
                          newRefs.set(slide.id, { current: undefined as any })
                        })
                        setSlideEditorRefs(newRefs)
                        
                        // Load independent projects inline to avoid race conditions
                        const independentSlides = layoutInfo.slideshowData.slides.filter(
                          (slide) => slide.projectRef && !slide.projectRef.isDependent
                        )
                        if (independentSlides.length > 0 && dbStorage.current) {
                          ;(async () => {
                            const results = new Map<string, StorageProjectData | null>()
                            await Promise.all(
                              independentSlides.map(async (slide) => {
                                const projectId = slide.projectRef!.projectId
                                try {
                                  const project = await dbStorage.current!.load(projectId)
                                  results.set(slide.id, project)
                                } catch (error) {
                                  console.error(`Failed to load independent project ${projectId}:`, error)
                                  results.set(slide.id, null)
                                }
                              })
                            )
                            setResolvedProjects(results)
                          })()
                        }
                        
                        // Update URL hash with project ID
                        window.history.pushState(null, '', `#${projectData.id}`)
                        return
                      }
                      
                      // Extract editor states baseado no tipo de projeto
                      const states = extractEditorStates(projectData.data, projectData.type)
                      
                      // Load editor data based on project type
                      setTimeout(() => {
                        try {
                          if (finalLayout === "single" && editorRef.current && states.blocks.b1) {
                            // Single panel: load single editor from b1 (cells format)
                            // Store cells format directly
                            setEditorState(JSON.stringify(states.blocks.b1))
                            
                            // Convert cells to Lexical for UI
                            const { cellsToLexical } = require("@/lib/storage/editor/cell-structure")
                            const lexicalState = cellsToLexical(states.blocks.b1)
                            const editorState = editorRef.current.parseEditorState(JSON.stringify(lexicalState))
                            editorRef.current.setEditorState(editorState)
                          } else if (finalLayout === "multiple" && states.blocks) {
                            // Multi-panel layout: load all blocks
                            // Clear existing blockRefs when loading a new project
                            blockRefs.current = {}
                            
                            const newBlockStates: Record<string, string> = {}
                            const { cellsToLexical } = require("@/lib/storage/editor/cell-structure")
                            
                            Object.entries(states.blocks).forEach(([blockId, blockState]: [string, any]) => {
                              if (blockState) {
                                // Initialize ref for each block
                                blockRefs.current[blockId] = null
                                // Store state in cells format
                                newBlockStates[blockId] = JSON.stringify(blockState)
                              }
                            })
                            
                            // Set the new block states - this will trigger re-render
                            setBlockStates(newBlockStates)
                            
                            // Wait for refs to be populated and load states into editors
                            setTimeout(() => {
                              Object.entries(newBlockStates).forEach(([blockId, stateString]) => {
                                const ref = blockRefs.current[blockId]
                                if (ref) {
                                  try {
                                    // Convert cells to Lexical for UI
                                    const cellsData = JSON.parse(stateString)
                                    const lexicalState = cellsToLexical(cellsData)
                                    const editorState = ref.parseEditorState(JSON.stringify(lexicalState))
                                    ref.setEditorState(editorState)
                                  } catch (error) {
                                    console.error(`Failed to load state for block ${blockId}:`, error)
                                  }
                                }
                              })
                            }, 150)
                          }
                        } catch (error) {
                          console.error("Failed to load editor data:", error)
                          toast.error("Erro ao carregar dados do editor", {
                            description: error instanceof Error ? error.message : "Unknown error",
                            duration: 4000,
                            icon: "❌",
                          })
                        }
                      }, 100)
                      
                      // Update URL hash with project ID
                      window.history.pushState(null, '', `#${projectData.id}`)
                    }}
                    onProjectsListUpdate={loadSavedProjectsList}
                    onCreateNew={(type: "type1" | "type2") => {
                      // Reset current project data when creating new
                      setCurrentProjectId("")
                      setCurrentProjectName("")
                      setCurrentProjectStorageType("local")
                      setProjectTags([])
                      setCreateDialogOpen(true)
                      // Clear URL hash
                      window.history.pushState(null, '', window.location.pathname)
                    }}
                    currentProjectName={currentProjectName}
                  />
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={handlePreview}
                    className="gap-2 border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800 bg-transparent"
                    disabled={!currentProjectId}
                    title="Preview in new tab"
                  >
                    <Eye className="h-4 w-4" />
                    Preview
                  </Button>

                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setHistoryDialogOpen(true)}
                    className="gap-2 border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800 bg-transparent"
                    disabled={!currentProjectId}
                    title="View history and snapshots"
                  >
                    <History className="h-4 w-4" />
                    History
                  </Button>

                  {/* Preview Mode Selector (Slideshow Layout Only) */}
                  {currentLayout === "slideshow" && slideshowStructure && (
                    <PreviewModeSelector
                      previewMode={previewMode}
                      onPreviewModeChange={setPreviewMode}
                    />
                  )}
                </div>

                {/* Status Indicators */}
                <div className="flex items-center gap-4">
                  <AutoSaveToggle
                    enabled={autoSaveEnabled}
                    onToggle={() => setAutoSaveEnabled(!autoSaveEnabled)}
                    disabled={!isDbInitialized}
                  />

                  <ProjectSizeIndicator
                    currentProjectSize={currentProjectSize}
                    currentProjectAssetsSize={currentProjectAssetsSize}
                    formatSize={formatSize}
                    getSizeIndicatorColor={getSizeIndicatorColor}
                    onClick={() => setShowSizeDetails(true)}
                  />

                  {syncStats && (
                    <SyncStatusIndicator
                      syncStats={syncStats}
                      isSyncEnabled={syncConfig.isEnabled()}
                      onClick={() => setShowSyncStatus(!showSyncStatus)}
                    />
                  )}
                </div>
              </div>
            </div>

            {/* History Viewing Banner */}
            {isViewingHistory && (
              <div className="flex items-center justify-between p-3 bg-amber-50 border border-amber-200 dark:bg-amber-900/20 dark:border-amber-800">
                <div className="flex items-center gap-2">
                  <History className="h-4 w-4 text-amber-600 dark:text-amber-400" />
                  <span className="text-sm font-medium text-amber-800 dark:text-amber-200">
                    Viewing historical version
                  </span>
                  {currentViewingSha && (
                    <Badge variant="outline" className="font-mono text-xs">
                      {currentViewingSha.substring(0, 7)}
                    </Badge>
                  )}
                  <span className="text-xs text-amber-600 dark:text-amber-400">
                    (Read-only)
                  </span>
                </div>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleReturnToHead}
                  className="gap-2 bg-white dark:bg-gray-800 border-amber-300 dark:border-amber-700 hover:bg-amber-100 dark:hover:bg-amber-900/40"
                >
                  <RotateCcw className="h-4 w-4" />
                  Return to Latest
                </Button>
              </div>
            )}

            <CreateProjectDialog
              open={createDialogOpen}
              onOpenChange={(open) => {
                setCreateDialogOpen(open)
                if (!open) setOpenDialogOpen(true)
              }}
              isDbInitialized={isDbInitialized}
              storageAdapter={storageAdapter}
              availableTags={availableTags}
              onProjectCreate={(projectData) => {
                // Set engine
                setCurrentEngine(projectData.engine || ENGINE_TYPES.LEXICAL)
                
                if (projectData.engine === ENGINE_TYPES.BLOCKS) {
                  // Block Array engine: just set empty cells, no Lexical needed
                  setBlockArrayCells([])
                  setCurrentLayout("single")
                  setCurrentProjectType((projectData.type || "type1") as ProjectType)
                  setCurrentProjectMode(projectData.mode || "free-page")
                  setLastProjectLoadTime(Date.now())
                  setCurrentProjectId(projectData.id)
                  setCurrentProjectName(projectData.name)
                  setCurrentProjectStorageType(projectData.storageType)
                  setProjectTags(projectData.tags)
                  setIsFirstTime(false)
                  window.history.pushState(null, '', `#${projectData.id}`)
                  return
                }
                
                // Lexical engine: existing flow
                // Create empty cells structure (basilar format)
                const emptyCells: CellularContent = []
                
                // Project data já vem com layout definido - usar tipo para determinar layout
                // Se não tiver data, criar estrutura baseada no tipo de projeto
                let dataString: string
                const layoutType = getLayoutFromType(projectData.type)
                
                if (layoutType === "slideshow") {
                  // Slideshow slides
                  const { structure: initialStructure, deps: initialDeps } = createEmptySlideshowStructure(projectData.id)
                  dataString = serializeSlideshowStructure(initialStructure)
                  setSlideshowStructure(initialStructure)
                  setSlideshowDeps(initialDeps)
                  setCurrentSlideIndex(0)
                  
                  // Initialize editor refs for first slide
                  const newRefs = new Map<string, React.RefObject<LexicalEditor>>()
                  if (initialStructure.slides[0]) {
                    newRefs.set(initialStructure.slides[0].id, { current: undefined as any })
                  }
                  setSlideEditorRefs(newRefs)
                  
                  // Update the project in storage with the correct slideshow structure
                  setTimeout(async () => {
                    try {
                      await storageAdapter.save(
                        projectData.id,
                        projectData.name,
                        dataString,
                        projectData.tags,
                        projectData.storageType,
                        undefined,
                        projectData.type,
                        initialDeps
                      )
                    } catch (error) {
                      console.error("Failed to save slideshow structure:", error)
                    }
                  }, 200)
                } else if (layoutType === "multiple") {
                  // Multiple panel
                  dataString = createProjectData(projectData.type, { blocks: { b1: emptyCells } })
                } else {
                  // Single panel (default)
                  dataString = createProjectData(projectData.type, { blocks: { b1: emptyCells } })
                }
                
                // Set the layout and project type from the project data
                setCurrentLayout(layoutType)
                setCurrentProjectType((projectData.type || "type1") as ProjectType)
                setCurrentProjectMode(projectData.mode || "free-page")
                
                // Mark project creation time to prevent auto-save for 1 second
                setLastProjectLoadTime(Date.now())
                
                // Wait for layout to render, then initialize editors
                setTimeout(() => {
                  // Convert cells to Lexical for editor initialization
                  const { cellsToLexical } = require("@/lib/storage/editor/cell-structure")
                  const lexicalState = cellsToLexical(emptyCells)
                  const lexicalStateString = JSON.stringify(lexicalState)
                  
                  if (layoutType === "single") {
                    // Store cells format in state
                    setEditorState(JSON.stringify(emptyCells))
                    // Initialize editor with Lexical format
                    if (editorRef.current) {
                      editorRef.current.setEditorState(editorRef.current.parseEditorState(lexicalStateString))
                    }
                  } else if (layoutType === "multiple") {
                    // multiple panel - initialize blocks (starts with b1, extensible to b2, b3...)
                    // Store cells format in state
                    const newBlockStates: Record<string, string> = {
                      b1: JSON.stringify(emptyCells),
                    }
                    setBlockStates(newBlockStates)
                  }
                  // Slideshow slides will be initialized as they render
                }, 100)
                
                setCurrentProjectId(projectData.id)
                setCurrentProjectName(projectData.name)
                setCurrentProjectStorageType(projectData.storageType)
                setProjectTags(projectData.tags)
                setIsFirstTime(false)
                // Update URL hash with project ID
                window.history.pushState(null, '', `#${projectData.id}`)
              }}
              onProjectsListUpdate={loadSavedProjectsList}
              onAvailableTagsUpdate={loadAvailableTags}
              generateProjectId={generateProjectId}
            />

            {/* Editor Container - Render based on engine and layout type */}
            {currentEngine === ENGINE_TYPES.BLOCKS ? (
              <div className="border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 p-4">
                <BlockArrayEditor
                  cells={blockArrayCells}
                  onChange={setBlockArrayCells}
                  readOnly={isViewingHistory}
                />
              </div>
            ) : currentLayout === "slideshow" && slideshowStructure ? (
              <EditorLayoutSlideshow
                structure={slideshowStructure}
                onStructureChange={setSlideshowStructure}
                deps={slideshowDeps}
                onDepsChange={setSlideshowDeps}
                currentSlideIndex={currentSlideIndex}
                onSlideIndexChange={setCurrentSlideIndex}
                slideEditorRefs={slideEditorRefs}
                onSlideEditorRefsChange={setSlideEditorRefs}
                onLoadingChange={(setLoading) => {
                  setLoadingRef.current = setLoading
                }}
                projectId={currentProjectId}
                mode={currentProjectMode}
                currentProjectType={currentProjectType}
                storageAdapter={storageAdapter}
                preferences={currentProjectPreferences}
                onPreferencesChange={handlePreferencesChange}
                readOnly={isViewingHistory}
                resolvedProjects={resolvedProjects}
                onConvertToIndependent={handleConvertToIndependent}
                onConvertToDependent={handleConvertToDependent}
                onImportProject={handleImportProject}
              />
            ) : currentLayout === "single" ? (
              <EditorLayoutType1
                editorRef={editorRef}
                editorState={editorState}
                onEditorChange={setEditorState}
                onLoadingChange={(setLoading) => {
                  setLoadingRef.current = setLoading
                }}
                projectId={currentProjectId}
                mode={currentProjectMode}
                currentProjectType={currentProjectType}
                storageAdapter={storageAdapter}
                readOnly={isViewingHistory}
              />
            ) : (
              <EditorLayoutType2
                blockRefs={blockRefs}
                blockStates={blockStates}
                onBlockChange={(blockId, newState) => {
                  setBlockStates(prev => ({ ...prev, [blockId]: newState }))
                }}
                onBlockAdd={() => {
                  // Find next block number
                  const blockNumbers = Object.keys(blockStates).map(key => parseInt(key.slice(1)))
                  const nextNum = Math.max(...blockNumbers, 0) + 1
                  const newBlockId = `b${nextNum}`
                  
                  // Create empty cells structure (basilar format)
                  const emptyCells = JSON.stringify([])
                  
                  // Add new block
                  setBlockStates(prev => ({ ...prev, [newBlockId]: emptyCells }))
                  
                  // Initialize ref
                  blockRefs.current[newBlockId] = null
                }}
                onBlockRemove={(blockId) => {
                  if (Object.keys(blockStates).length <= 1) {
                    return // Prevent removing last block
                  }
                  
                  // Remove block state
                  setBlockStates(prev => {
                    const newStates = { ...prev }
                    delete newStates[blockId]
                    return newStates
                  })
                  
                  // Remove ref
                  delete blockRefs.current[blockId]
                }}
                onLoadingChange={(setLoading) => {
                  setLoadingRef.current = setLoading
                }}
                projectId={currentProjectId}
                mode={currentProjectMode}
                currentProjectType={currentProjectType}
                storageAdapter={storageAdapter}
                preferences={currentProjectPreferences}
                onPreferencesChange={handlePreferencesChange}
                currentProjectId={currentProjectId}
                readOnly={isViewingHistory}
              />
            )}
          </div>
        </div>
      </div>
      <SizeDetailsDialog
        open={showSizeDetails}
        onOpenChange={setShowSizeDetails}
        currentProjectSize={currentProjectSize}
        currentProjectAssetsSize={currentProjectAssetsSize}
        currentProjectAssets={currentProjectAssets}
        recommendedSizeKB={RECOMMENDED_SIZE_KB}
        formatSize={formatSize}
        getSizeIndicatorColor={getSizeIndicatorColor}
      />
      <SyncStatusDialog
        open={showSyncStatus}
        onOpenChange={setShowSyncStatus}
        syncStats={syncStats}
        onRetryFailed={() => dbStorage.current.retryFailedSync()}
      />
      <ExitConfirmDialog
        open={exitDialogOpen}
        onOpenChange={setExitDialogOpen}
        onConfirm={() => {
          if (nextUrl) {
            router.push(nextUrl)
          }
        }}
        itemName={currentProjectName}
        itemType="project"
        showSaveAndExit={true}
        onSaveAndExit={handleSaveAndExit}
      />

      {/* Preview Dialog */}
      <Dialog open={previewOpen} onOpenChange={setPreviewOpen}>
        <DialogContent 
          className={(previewLayout === "multiple" || previewLayout === "slideshow") ? "max-w-none! p-6" : "max-w-4xl max-h-[90vh] overflow-y-auto"}
          style={(previewLayout === "multiple" || previewLayout === "slideshow") ? { width: '95vw', maxWidth: '95vw' } : undefined}
        >
          <DialogHeader>
            <DialogTitle>Preview</DialogTitle>
          </DialogHeader>
          {currentEngine === ENGINE_TYPES.BLOCKS && (
            <div className="w-full max-h-[80vh] overflow-y-auto">
              <BlockArrayViewer cells={blockArrayCells} />
            </div>
          )}
          {currentEngine !== ENGINE_TYPES.BLOCKS && previewLayout === "single" && previewState && (
            <PreviewRenderer serializedState={previewState} />
          )}
          {previewLayout === "multiple" && Object.keys(previewBlockStates).length >= 1 && (
            <div className="w-full max-h-[80vh] overflow-y-auto">
              <PreviewRendererType2 
                blockStates={previewBlockStates} 
                preferences={currentProjectPreferences}
                onLayoutChange={(panels, direction) => {
                  if (currentProjectPreferences) {
                    handlePreferencesChange({
                      ...currentProjectPreferences,
                      global: {
                        ...currentProjectPreferences.global,
                        advancedMultiBlockPanels: panels,
                        multiBlockDirection: direction,
                      }
                    })
                  }
                }}
              />
            </div>
          )}
          {previewLayout === "slideshow" && previewSlideshowStructure && (
            <div className="w-full max-h-[80vh] overflow-y-auto">
              {previewSlideshowMode === "slide" ? (
                <PreviewRendererSlideshowSlide
                  structure={previewSlideshowStructure}
                  projectId={currentProjectId}
                  projectName={currentProjectName}
                  deps={slideshowDeps}
                  resolvedProjects={resolvedProjects}
                  storageAdapter={storageAdapter}
                  preferences={currentProjectPreferences}
                />
              ) : (
                <PreviewRendererSlideshowContinuous
                  structure={previewSlideshowStructure}
                  projectId={currentProjectId}
                  projectName={currentProjectName}
                  deps={slideshowDeps}
                  resolvedProjects={resolvedProjects}
                  storageAdapter={storageAdapter}
                  preferences={currentProjectPreferences}
                />
              )}
            </div>
          )}
        </DialogContent>
      </Dialog>

      {/* History Dialog */}
      <ProjectHistoryDialog
        open={historyDialogOpen}
        onOpenChange={setHistoryDialogOpen}
        projectId={currentProjectId}
        projectName={currentProjectName}
        isViewingHistory={isViewingHistory}
        currentViewingSha={currentViewingSha}
        onLoadCommit={handleLoadCommit}
        onLoadSnapshot={handleLoadSnapshot}
        onReturnToHead={handleReturnToHead}
        onCreateSnapshot={handleCreateSnapshot}
        listHistory={(id) => dbStorage.current.listHistory(id)}
        listSnapshots={(id) => dbStorage.current.listSnapshots(id)}
      />

      {/* Project Import Dialog for slideshow slides */}
      <ProjectImportDialog
        open={importDialogOpen}
        onOpenChange={setImportDialogOpen}
        storageAdapter={{
          list: () => storageAdapter.list(),
          listSnapshots: (id: string) => dbStorage.current.listSnapshots(id),
        }}
        onConfirm={handleImportConfirm}
        currentProjectId={currentProjectId}
      />
    </>
  )
}
