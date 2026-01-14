"use client"

import { Editor } from "@/components/editor/lexical-editor"
import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Save, HardDrive, Eye, Blocks, Home, Monitor, Presentation, Plus } from "lucide-react"
import { useState, useEffect, useRef } from "react"
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
import { PanelNavigationSidebar } from "@/components/editor/extras/editor/panel-navigation-sidebar"
import { handleTitleEdit as titleEdit, handleTitleSave as titleSave } from "@/components/editor/extras/editor/project-title-operations"
import { handleSave as saveProject, handleSaveAs as saveAsProject } from "@/components/editor/extras/editor/project-save-operations"
import { calculateProjectAssetsSize as calculateAssets } from "@/components/editor/extras/editor/project-assets-operations"
import { checkSelectedProject as checkProject } from "@/components/editor/extras/editor/project-load-operations"
import { EditorLayoutType1 } from "@/components/editor/extras/editor/editor-layout-type1"
import { EditorLayoutType2 } from "@/components/editor/extras/editor/editor-layout-type2"
import { EnhancedStorageAdapter, type ProjectPreferences } from "@/lib/storage/editor/enhanced-storage-adapter"
import { syncConfig } from "@/lib/sync/editor/sync-config"
import { SaveAsDialog } from "@/components/editor/extras/editor/save-as-dialog"
import { type ProjectMode, NODE_RESTRICTIONS, PROJECT_MODES } from "@/lib/storage/editor/project-modes"
import { detectProjectLayout, extractEditorStates, createProjectData, type LayoutType } from "@/lib/storage/editor/layout-detector"
import { ExitConfirmDialog } from "@/components/editor/extras/dialogs/exit-confirm-dialog"
import { assetManager } from "@/lib/storage/assets/asset-manager"
import { PreviewRenderer } from "@/components/editor/extras/preview/preview-renderer"
import { PreviewRendererType2 } from "@/components/editor/extras/preview/preview-renderer-type2"
import type { SerializedEditorState } from "lexical"
import { 
  type SequentialPanelStructure, 
  type PanelLayoutType, 
  type PreviewMode,
  isSequentialStructure,
  parseSequentialStructure,
  createEmptySequentialStructure,
  addPanel,
  removePanel,
  reorderPanels,
  updatePanelName,
  updatePanelState,
  updatePreviewMode,
  serializeSequentialStructure
} from "@/lib/storage/editor/panel-structure"

export type ProjectType = "type1" | "type2" | "type3"

export interface ProjectDataType1 {
  data: string // Single editor JSON state
}

export interface ProjectDataType2 {
  left: string // Left editor JSON state
  right: string // Right editor JSON state
}

interface ProjectData {
  id: string
  name: string
  type: ProjectType // Project type (not layout - layout is auto-detected)
  data: string // If dual panel: {left, right}, if single panel: direct state
  tags: string[]
  size: number
  createdAt: string
  updatedAt: string
  storageType?: "local" | "gameguild-cloud" | "google-drive"
  preferences?: ProjectPreferences
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
  const [currentLayout, setCurrentLayout] = useState<LayoutType>("single")
  const [currentProjectType, setCurrentProjectType] = useState<ProjectType>("type1")
  
  // Type1 states (single editor)
  const [editorState, setEditorState] = useState<string>("")
  const editorRef = useRef<LexicalEditor | null>(null)
  
  // Type2 states (dual editors)
  const [leftEditorState, setLeftEditorState] = useState<string>("")
  const [rightEditorState, setRightEditorState] = useState<string>("")
  const leftEditorRef = useRef<LexicalEditor | null>(null)
  const rightEditorRef = useRef<LexicalEditor | null>(null)
  
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
  const [previewLeftState, setPreviewLeftState] = useState<SerializedEditorState | null>(null)
  const [previewRightState, setPreviewRightState] = useState<SerializedEditorState | null>(null)
  const [previewLayout, setPreviewLayout] = useState<LayoutType>("single")
  const [lastProjectLoadTime, setLastProjectLoadTime] = useState<number>(0)
  const [currentProjectMode, setCurrentProjectMode] = useState<ProjectMode>("free-page")

  // Sequential panel states
  const [sequentialStructure, setSequentialStructure] = useState<SequentialPanelStructure | null>(null)
  const [currentPanelIndex, setCurrentPanelIndex] = useState(0)
  const [panelEditorRefs, setPanelEditorRefs] = useState<Map<string, React.RefObject<LexicalEditor>>>(new Map())
  
  const [nextUrl, setNextUrl] = useState<string | null>(null)
  const [exitDialogOpen, setExitDialogOpen] = useState(false)

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
        editorRef,
        leftEditorRef,
        rightEditorRef,
        setCurrentProjectId,
        setCurrentProjectName,
        setCurrentProjectStorageType,
        setProjectTags,
        setIsFirstTime,
        setCurrentLayout,
        setCurrentProjectType: (type: string) => setCurrentProjectType(type as ProjectType),
        setEditorState,
        setLeftEditorState,
        setRightEditorState,
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
    
    if (currentLayout === "sequential" && sequentialStructure) {
      dataToCalculate = serializeSequentialStructure(sequentialStructure)
    } else {
      dataToCalculate = createProjectData(currentLayout, {
        single: currentLayout === "single" ? (editorState ? JSON.parse(editorState) : null) : null,
        left: currentLayout === "dual" ? (leftEditorState ? JSON.parse(leftEditorState) : null) : null,
        right: currentLayout === "dual" ? (rightEditorState ? JSON.parse(rightEditorState) : null) : null,
      })
    }
    
    const size = estimateSize(dataToCalculate)
    setCurrentProjectSize(size)
  }, [editorState, leftEditorState, rightEditorState, currentLayout, sequentialStructure])

  // Calculate assets size when project changes or editor content changes
  useEffect(() => {
    if (currentProjectId && isDbInitialized) {
      calculateProjectAssetsSize(currentProjectId)
    } else {
      setCurrentProjectAssetsSize(0)
    }
  }, [currentProjectId, isDbInitialized, editorState, leftEditorState, rightEditorState, sequentialStructure])

  const storageAdapter = {
    save: async (id: string, name: string, data: string, tags: string[] = [], storageType: "local" | "gameguild-cloud" | "google-drive" = "local", preferences?: ProjectPreferences, type: string = "type1") => {
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
        await dbStorage.current.save(id, name, data, tags, storageType, preferences, type as any)
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
    // Prepare the correct state based on layout type
    let dataToSave: string
    
    if (currentLayout === "sequential" && sequentialStructure) {
      // Sequential layout: serialize the structure
      dataToSave = serializeSequentialStructure(sequentialStructure)
    } else {
      // Single or Dual layout
      dataToSave = createProjectData(currentLayout, {
        single: currentLayout === "single" ? (editorState ? JSON.parse(editorState) : null) : null,
        left: currentLayout === "dual" ? (leftEditorState ? JSON.parse(leftEditorState) : null) : null,
        right: currentLayout === "dual" ? (rightEditorState ? JSON.parse(rightEditorState) : null) : null,
      })
    }
    
    const refToUse = currentLayout === "single" ? editorRef : leftEditorRef
    
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
    })
  }

  const handleSaveAs = async (storageOption: "local" | "gameguild-cloud" | "google-drive" = "local") => {
    // Prepare the correct state based on layout type
    let dataToSave: string
    
    if (currentLayout === "sequential" && sequentialStructure) {
      // Sequential layout: serialize the structure
      dataToSave = serializeSequentialStructure(sequentialStructure)
    } else {
      // Single or Dual layout
      dataToSave = createProjectData(currentLayout, {
        single: currentLayout === "single" ? (editorState ? JSON.parse(editorState) : null) : null,
        left: currentLayout === "dual" ? (leftEditorState ? JSON.parse(leftEditorState) : null) : null,
        right: currentLayout === "dual" ? (rightEditorState ? JSON.parse(rightEditorState) : null) : null,
      })
    }
    
    const refToUse = currentLayout === "single" ? editorRef : leftEditorRef
    
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

  useEffect(() => {
    // Update the ref when handleSave changes
    handleSaveRef.current = handleSave
  })

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.ctrlKey && event.key === "s") {
        event.preventDefault() // Prevent browser's default save dialog
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
    if (!autoSaveEnabled || !currentProjectId || !isDbInitialized) return

    // Check if we have any content to save
    const hasContent = currentLayout === "single" 
      ? editorState 
      : (leftEditorState || rightEditorState)
    
    if (!hasContent) return

    // Wait 1 second after project load before enabling auto-save
    const timeSinceLoad = Date.now() - lastProjectLoadTime
    if (timeSinceLoad < 1000) {
      return
    }

    const autoSaveTimer = setTimeout(async () => {
      try {
        // Prepare the correct state based on layout type
        const dataToSave = createProjectData(currentLayout, {
          single: currentLayout === "single" ? (editorState ? JSON.parse(editorState) : null) : null,
          left: currentLayout === "dual" ? (leftEditorState ? JSON.parse(leftEditorState) : null) : null,
          right: currentLayout === "dual" ? (rightEditorState ? JSON.parse(rightEditorState) : null) : null,
        })
        
        await storageAdapter.save(
          currentProjectId, 
          currentProjectName, 
          dataToSave, 
          projectTags,
          currentProjectStorageType,
          undefined,
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
  }, [editorState, leftEditorState, rightEditorState, autoSaveEnabled, currentProjectId, currentProjectName, projectTags, isDbInitialized, currentLayout, currentProjectType, currentProjectStorageType, lastProjectLoadTime])



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
    const stateToUse = createProjectData(currentLayout, {
      single: currentLayout === "single" ? (editorState ? JSON.parse(editorState) : null) : null,
      left: currentLayout === "dual" ? (leftEditorState ? JSON.parse(leftEditorState) : null) : null,
      right: currentLayout === "dual" ? (rightEditorState ? JSON.parse(rightEditorState) : null) : null,
    })
    
    const refToUse = currentLayout === "single" ? editorRef : leftEditorRef
    
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
      if (currentLayout === "single") {
        if (!editorState) {
          toast.error("No content", {
            description: "Editor is empty",
            duration: 3000,
          })
          return
        }
        
        // Parse and set single panel preview state
        const parsed = JSON.parse(editorState)
        setPreviewState(parsed)
        setPreviewLayout("single")
        setPreviewOpen(true)
      } else {
        // Dual panel: Both editors
        if (!leftEditorState && !rightEditorState) {
          toast.error("No content", {
            description: "Editors are empty",
            duration: 3000,
          })
          return
        }
        
        // Parse both editor states for dual panel
        const leftParsed = leftEditorState ? JSON.parse(leftEditorState) : null
        const rightParsed = rightEditorState ? JSON.parse(rightEditorState) : null
        
        if (!leftParsed || !rightParsed) {
          toast.error("Invalid content", {
            description: "Both editors must have content",
            duration: 3000,
          })
          return
        }
        
        setPreviewLeftState(leftParsed)
        setPreviewRightState(rightParsed)
        setPreviewLayout("dual")
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
                    disabled={!isDbInitialized}
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
                    leftEditorRef={leftEditorRef}
                    rightEditorRef={rightEditorRef}
                    setLoadingRef={setLoadingRef}
                    onProjectLoad={(projectData) => {
                      // Detectar layout automaticamente baseado na estrutura de data
                      const layoutInfo = detectProjectLayout(projectData.data)
                      
                      // Extract mode from preferences or default to free-page
                      const projectMode = projectData.preferences?.global?.mode || "free-page"
                      
                      // Update project metadata
                      setCurrentProjectId(projectData.id)
                      setCurrentProjectName(projectData.name)
                      setCurrentProjectType(projectData.type)  // tipo do projeto
                      setCurrentLayout(layoutInfo.layoutType)  // layout detectado
                      setCurrentProjectStorageType(projectData.storageType || "local")
                      setProjectTags(projectData.tags || [])
                      setCurrentProjectMode(projectMode)
                      setIsFirstTime(false)
                      
                      // Mark project load time to prevent auto-save for 1 second
                      setLastProjectLoadTime(Date.now())
                      
                      // Handle sequential layout
                      if (layoutInfo.isSequential && layoutInfo.sequentialData) {
                        setSequentialStructure(layoutInfo.sequentialData)
                        setCurrentPanelIndex(0)
                        
                        // Initialize editor refs for all panels
                        const newRefs = new Map<string, React.RefObject<LexicalEditor>>()
                        layoutInfo.sequentialData.panels.forEach(panel => {
                          newRefs.set(panel.id, { current: undefined as any })
                        })
                        setPanelEditorRefs(newRefs)
                        
                        // Update URL hash with project ID
                        window.history.pushState(null, '', `#${projectData.id}`)
                        return
                      }
                      
                      // Extract editor states baseado no layout detectado
                      const states = extractEditorStates(projectData.data, layoutInfo.layoutType)
                      
                      // Load editor data based on detected layout
                      setTimeout(() => {
                        try {
                          if (layoutInfo.isSinglePanel && editorRef.current && states.single) {
                            // Single panel: carregar editor único
                            if (!states.single.root) {
                              throw new Error("Invalid Lexical format")
                            }
                            const editorState = editorRef.current.parseEditorState(JSON.stringify(states.single))
                            editorRef.current.setEditorState(editorState)
                            setEditorState(JSON.stringify(states.single))
                          } else if (layoutInfo.isDualPanel && leftEditorRef.current && rightEditorRef.current && states.left && states.right) {
                            // Dual panel: carregar ambos editores
                            if (!states.left.root) throw new Error("Invalid left editor format")
                            if (!states.right.root) throw new Error("Invalid right editor format")
                            
                            const leftEditorState = leftEditorRef.current.parseEditorState(JSON.stringify(states.left))
                            leftEditorRef.current.setEditorState(leftEditorState)
                            setLeftEditorState(JSON.stringify(states.left))

                            const rightEditorState = rightEditorRef.current.parseEditorState(JSON.stringify(states.right))
                            rightEditorRef.current.setEditorState(rightEditorState)
                            setRightEditorState(JSON.stringify(states.right))
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
                  
                  {/* Preview Mode Selector (Sequential Layout Only) */}
                  {currentLayout === "sequential" && sequentialStructure && (
                    <div className="flex items-center gap-2 ml-2 pl-2 border-l border-gray-300 dark:border-gray-600">
                      <span className="text-xs text-gray-500 dark:text-gray-400">
                        Preview Mode:
                      </span>
                      <Button
                        variant={sequentialStructure.previewMode === "continuous" ? "default" : "outline"}
                        size="sm"
                        onClick={() => {
                          const newStructure = updatePreviewMode(sequentialStructure, "continuous")
                          setSequentialStructure(newStructure)
                          toast.success("Preview mode changed", {
                            description: "Preview will show all panels in continuous scroll",
                            duration: 2000
                          })
                        }}
                        className="gap-2 h-8"
                        title="Show all panels in continuous scroll"
                      >
                        <Monitor className="h-3.5 w-3.5" />
                        Continuous
                      </Button>
                      <Button
                        variant={sequentialStructure.previewMode === "slide" ? "default" : "outline"}
                        size="sm"
                        onClick={() => {
                          const newStructure = updatePreviewMode(sequentialStructure, "slide")
                          setSequentialStructure(newStructure)
                          toast.success("Preview mode changed", {
                            description: "Preview will show one panel at a time",
                            duration: 2000
                          })
                        }}
                        className="gap-2 h-8"
                        title="Show one panel at a time (presentation mode)"
                      >
                        <Presentation className="h-3.5 w-3.5" />
                        Slide
                      </Button>
                    </div>
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
                const emptyState =
                  {"root":{"children":[{"children":[],"direction":null,"format":"","indent":0,"type":"paragraph","version":1}],"direction":null,"format":"","indent":0,"type":"root","version":1}}
                
                // Project data já vem com layout definido - detectar automaticamente  
                // Se não tiver data, criar estrutura baseada no tipo de layout desejado
                let dataString: string
                let layoutType: LayoutType
                
                if (projectData.layout === "sequential") {
                  // Sequential panels
                  const initialStructure = createEmptySequentialStructure()
                  dataString = serializeSequentialStructure(initialStructure)
                  layoutType = "sequential"
                  setSequentialStructure(initialStructure)
                  setCurrentPanelIndex(0)
                  
                  // Initialize editor refs for first panel
                  const newRefs = new Map<string, React.RefObject<LexicalEditor>>()
                  if (initialStructure.panels[0]) {
                    newRefs.set(initialStructure.panels[0].id, { current: undefined as any })
                  }
                  setPanelEditorRefs(newRefs)
                  
                  // Update the project in storage with the correct sequential structure
                  setTimeout(async () => {
                    try {
                      await storageAdapter.save(
                        projectData.id,
                        projectData.name,
                        dataString,
                        projectData.tags,
                        projectData.storageType,
                        undefined,
                        projectData.type
                      )
                    } catch (error) {
                      console.error("Failed to save sequential structure:", error)
                    }
                  }, 200)
                } else if (projectData.layout === "dual") {
                  // Dual panel
                  dataString = createProjectData("dual", { left: emptyState, right: emptyState })
                  layoutType = "dual"
                } else {
                  // Single panel (default)
                  dataString = createProjectData("single", { single: emptyState })
                  layoutType = "single"
                }
                
                // Set the layout and project type from the project data
                setCurrentLayout(layoutType)
                setCurrentProjectType((projectData.type || "type1") as ProjectType)
                setCurrentProjectMode(projectData.mode || "free-page")
                
                // Mark project creation time to prevent auto-save for 1 second
                setLastProjectLoadTime(Date.now())
                
                // Wait for layout to render, then initialize editors
                setTimeout(() => {
                  const emptyStateString = JSON.stringify(emptyState)
                  if (layoutType === "single") {
                    if (editorRef.current) {
                      editorRef.current.setEditorState(editorRef.current.parseEditorState(emptyStateString))
                    }
                    setEditorState(emptyStateString)
                  } else if (layoutType === "dual") {
                    // dual panel - initialize both editors
                    if (leftEditorRef.current) {
                      leftEditorRef.current.setEditorState(leftEditorRef.current.parseEditorState(emptyStateString))
                    }
                    if (rightEditorRef.current) {
                      rightEditorRef.current.setEditorState(rightEditorRef.current.parseEditorState(emptyStateString))
                    }
                    setLeftEditorState(emptyStateString)
                    setRightEditorState(emptyStateString)
                  }
                  // Sequential panels will be initialized as they render
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

            {/* Editor Container - Render based on layout type */}
            {currentLayout === "sequential" && sequentialStructure ? (
              <div className="flex gap-0 border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden bg-white dark:bg-gray-900">
                {/* Panel Navigation Sidebar */}
                <div className="w-64 border-r border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800">
                  <PanelNavigationSidebar
                    panels={sequentialStructure.panels}
                    currentPanelIndex={currentPanelIndex}
                    onPanelSelect={(index: number) => {
                      setCurrentPanelIndex(index)
                    }}
                    onPanelAdd={(type: PanelLayoutType) => {
                      const newStructure = addPanel(sequentialStructure, type)
                      setSequentialStructure(newStructure)
                      // Initialize ref for new panel
                      const lastPanel = newStructure.panels[newStructure.panels.length - 1]
                      const newRefs = new Map(panelEditorRefs)
                      if (lastPanel) {
                        newRefs.set(lastPanel.id, { current: undefined as any })
                      }
                      setPanelEditorRefs(newRefs)
                      // Navigate to new panel
                      setCurrentPanelIndex(newStructure.panels.length - 1)
                    }}
                    onPanelRemove={(panelId: string) => {
                      if (sequentialStructure.panels.length === 1) {
                        toast.error("Cannot remove last panel", {
                          description: "At least one panel is required",
                          duration: 3000
                        })
                        return
                      }
                      const newStructure = removePanel(sequentialStructure, panelId)
                      setSequentialStructure(newStructure)
                      // Remove ref
                      const newRefs = new Map(panelEditorRefs)
                      newRefs.delete(panelId)
                      setPanelEditorRefs(newRefs)
                      // Adjust current index if needed
                      if (currentPanelIndex >= newStructure.panels.length) {
                        setCurrentPanelIndex(newStructure.panels.length - 1)
                      }
                    }}
                    onPanelReorder={(fromIndex: number, toIndex: number) => {
                      const newStructure = reorderPanels(sequentialStructure, fromIndex, toIndex)
                      setSequentialStructure(newStructure)
                      // Update current index to follow the moved panel
                      if (currentPanelIndex === fromIndex) {
                        setCurrentPanelIndex(toIndex)
                      } else if (currentPanelIndex === toIndex) {
                        setCurrentPanelIndex(fromIndex < toIndex ? toIndex - 1 : toIndex + 1)
                      }
                    }}
                    onPanelNameChange={(panelId: string, name: string) => {
                      const newStructure = updatePanelName(sequentialStructure, panelId, name)
                      setSequentialStructure(newStructure)
                    }}
                  />
                </div>
                
                {/* Continuous Scroll Container - All panels visible */}
                <div className="flex-1 overflow-y-auto max-h-[calc(100vh-16rem)] bg-gray-50 dark:bg-gray-950">
                  <div className="space-y-4 p-6">
                    {sequentialStructure.panels.map((panel, index) => (
                      <div key={panel.id}>
                        <div 
                          className={`border-2 transition-all rounded-lg overflow-hidden ${
                            currentPanelIndex === index
                              ? 'border-blue-500 shadow-lg ring-2 ring-blue-200 dark:ring-blue-800'
                              : 'border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600'
                          }`}
                          onClick={() => setCurrentPanelIndex(index)}
                        >
                          {/* Panel Header */}
                          <div className="bg-white dark:bg-gray-900 px-4 py-2 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between">
                            <div className="flex items-center gap-2">
                              <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
                                {panel.name || `Panel ${panel.order + 1}`}
                              </span>
                              <span className="text-xs px-2 py-0.5 rounded bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400">
                                {panel.type === "single" ? "Single" : "Dual"}
                              </span>
                            </div>
                          </div>
                          
                          {/* Panel Content */}
                          {panel.type === "single" ? (
                            <div className="bg-white dark:bg-gray-900">
                              <EditorLayoutType1
                                editorRef={panelEditorRefs.get(panel.id) as any}
                                editorState={typeof panel.state === "string" ? panel.state : JSON.stringify(panel.state || "")}
                                onEditorChange={(newState) => {
                                  const newStructure = updatePanelState(sequentialStructure, panel.id, { state: newState })
                                  setSequentialStructure(newStructure)
                                }}
                                onLoadingChange={(setLoading) => {
                                  if (currentPanelIndex === index) {
                                    setLoadingRef.current = setLoading
                                  }
                                }}
                                projectId={currentProjectId}
                                mode={currentProjectMode}
                              />
                            </div>
                          ) : (
                            <div className="bg-white dark:bg-gray-900">
                              <EditorLayoutType2
                                leftEditorRef={panelEditorRefs.get(`${panel.id}-left`) as any}
                                rightEditorRef={panelEditorRefs.get(`${panel.id}-right`) as any}
                                leftEditorState={typeof panel.left === "string" ? panel.left : JSON.stringify(panel.left || "")}
                                rightEditorState={typeof panel.right === "string" ? panel.right : JSON.stringify(panel.right || "")}
                                onLeftEditorChange={(newState) => {
                                  const newStructure = updatePanelState(sequentialStructure, panel.id, { left: newState })
                                  setSequentialStructure(newStructure)
                                }}
                                onRightEditorChange={(newState) => {
                                  const newStructure = updatePanelState(sequentialStructure, panel.id, { right: newState })
                                  setSequentialStructure(newStructure)
                                }}
                                onLoadingChange={(setLoading) => {
                                  if (currentPanelIndex === index) {
                                    setLoadingRef.current = setLoading
                                  }
                                }}
                                projectId={currentProjectId}
                                mode={currentProjectMode}
                              />
                            </div>
                          )}
                        </div>
                        
                        {/* Add Panel Button */}
                        <div className="flex justify-center my-4">
                          <div className="flex items-center gap-2">
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => {
                                const newStructure = addPanel(sequentialStructure, "single", index + 1)
                                setSequentialStructure(newStructure)
                                const lastPanel = newStructure.panels[index + 1]
                                const newRefs = new Map(panelEditorRefs)
                                if (lastPanel) {
                                  newRefs.set(lastPanel.id, { current: undefined as any })
                                }
                                setPanelEditorRefs(newRefs)
                                setCurrentPanelIndex(index + 1)
                              }}
                              className="gap-2 bg-white dark:bg-gray-800 border-dashed border-2 hover:border-blue-500 hover:bg-blue-50 dark:hover:bg-blue-950"
                            >
                              <Plus className="h-4 w-4" />
                              Add Single Panel
                            </Button>
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => {
                                const newStructure = addPanel(sequentialStructure, "dual", index + 1)
                                setSequentialStructure(newStructure)
                                const lastPanel = newStructure.panels[index + 1]
                                const newRefs = new Map(panelEditorRefs)
                                if (lastPanel) {
                                  newRefs.set(lastPanel.id, { current: undefined as any })
                                }
                                setPanelEditorRefs(newRefs)
                                setCurrentPanelIndex(index + 1)
                              }}
                              className="gap-2 bg-white dark:bg-gray-800 border-dashed border-2 hover:border-blue-500 hover:bg-blue-50 dark:hover:bg-blue-950"
                            >
                              <Plus className="h-4 w-4" />
                              Add Dual Panel
                            </Button>
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
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
              />
            ) : (
              <EditorLayoutType2
                leftEditorRef={leftEditorRef}
                rightEditorRef={rightEditorRef}
                leftEditorState={leftEditorState}
                rightEditorState={rightEditorState}
                onLeftEditorChange={setLeftEditorState}
                onRightEditorChange={setRightEditorState}
                onLoadingChange={(setLoading) => {
                  setLoadingRef.current = setLoading
                }}
                projectId={currentProjectId}
                mode={currentProjectMode}
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
          className={previewLayout === "dual" ? "max-w-none! p-6" : "max-w-4xl max-h-[90vh] overflow-y-auto"}
          style={previewLayout === "dual" ? { width: '95vw', maxWidth: '95vw' } : undefined}
        >
          <DialogHeader>
            <DialogTitle>Preview</DialogTitle>
          </DialogHeader>
          {previewLayout === "single" && previewState && (
            <PreviewRenderer serializedState={previewState} />
          )}
          {previewLayout === "dual" && previewLeftState && previewRightState && (
            <div className="w-full max-h-[80vh] overflow-y-auto">
              <PreviewRendererType2 leftState={previewLeftState} rightState={previewRightState} />
            </div>
          )}
        </DialogContent>
      </Dialog>
    </>
  )
}
