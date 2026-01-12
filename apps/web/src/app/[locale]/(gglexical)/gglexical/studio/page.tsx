"use client"

import { Editor } from "@/components/editor/lexical-editor"
import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Save, HardDrive, Eye, Blocks, Home } from "lucide-react"
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
import { handleTitleEdit as titleEdit, handleTitleSave as titleSave } from "@/components/editor/extras/editor/project-title-operations"
import { handleSave as saveProject, handleSaveAs as saveAsProject } from "@/components/editor/extras/editor/project-save-operations"
import { calculateProjectAssetsSize as calculateAssets } from "@/components/editor/extras/editor/project-assets-operations"
import { checkSelectedProject as checkProject } from "@/components/editor/extras/editor/project-load-operations"
import { EditorLayoutType1 } from "@/components/editor/extras/editor/editor-layout-type1"
import { EditorLayoutType2 } from "@/components/editor/extras/editor/editor-layout-type2"
import { EnhancedStorageAdapter } from "@/lib/storage/editor/enhanced-storage-adapter"
import { syncConfig } from "@/lib/sync/editor/sync-config"
import { SaveAsDialog } from "@/components/editor/extras/editor/save-as-dialog"
import { ExitConfirmDialog } from "@/components/editor/extras/dialogs/exit-confirm-dialog"
import { assetManager } from "@/lib/storage/assets/asset-manager"

export type ProjectLayoutType = "type1" | "type2"

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
  type: ProjectLayoutType // Layout type
  data: string // For type1: direct JSON, for type2: stringified {left, right}
  tags: string[]
  size: number
  createdAt: string
  updatedAt: string
  storageType?: "local" | "gameguild-cloud" | "google-drive"
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
  // Layout type and state management
  const [currentLayoutType, setCurrentLayoutType] = useState<ProjectLayoutType>("type1")
  
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
        setCurrentLayoutType,
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
    if (currentLayoutType === "type1" && editorState) {
      const size = estimateSize(editorState)
      setCurrentProjectSize(size)
    } else if (currentLayoutType === "type2" && (leftEditorState || rightEditorState)) {
      const combinedData = JSON.stringify({ left: leftEditorState, right: rightEditorState })
      const size = estimateSize(combinedData)
      setCurrentProjectSize(size)
    }
  }, [editorState, leftEditorState, rightEditorState, currentLayoutType])

  // Calculate assets size when project changes or editor content changes
  useEffect(() => {
    if (currentProjectId && isDbInitialized) {
      calculateProjectAssetsSize(currentProjectId)
    } else {
      setCurrentProjectAssetsSize(0)
    }
  }, [currentProjectId, isDbInitialized, editorState, leftEditorState, rightEditorState])

  const storageAdapter = {
    save: async (id: string, name: string, data: string, tags: string[] = [], storageType: "local" | "gameguild-cloud" | "google-drive" = "local", type: "type1" | "type2" = "type1") => {
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
        await dbStorage.current.save(id, name, data, tags, storageType, undefined, type)
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
    const dataToSave = currentLayoutType === "type1" 
      ? editorState 
      : JSON.stringify({ left: leftEditorState, right: rightEditorState })
    
    const refToUse = currentLayoutType === "type1" ? editorRef : leftEditorRef
    
    await saveProject({
      currentProjectId,
      currentProjectName,
      currentProjectStorageType,
      layoutType: currentLayoutType,
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
    const dataToSave = currentLayoutType === "type1" 
      ? editorState 
      : JSON.stringify({ left: leftEditorState, right: rightEditorState })
    
    const refToUse = currentLayoutType === "type1" ? editorRef : leftEditorRef
    
    await saveAsProject({
      newProjectName,
      layoutType: currentLayoutType,
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
    if (!autoSaveEnabled || !currentProjectId || !editorState || !isDbInitialized) return

    const autoSaveTimer = setTimeout(async () => {
      try {
        await storageAdapter.save(currentProjectId, currentProjectName, editorState, projectTags)
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
  }, [editorState, autoSaveEnabled, currentProjectId, currentProjectName, projectTags, isDbInitialized])



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
    await titleSave({
      editingProjectName,
      currentProjectName,
      currentProjectId,
      editorState,
      editorRef,
      projectTags,
      storageAdapter,
      setCurrentProjectName,
      setEditingProjectName,
      setIsEditingTitle,
      loadSavedProjectsList,
    })
  }

  const [exitDialogOpen, setExitDialogOpen] = useState(false)
  const [nextUrl, setNextUrl] = useState<string>("")

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

  return (
    <>
      <div className="min-h-screen bg-gray-50 dark:bg-gray-950">
        <div className="container mx-auto py-10">
          <div className="mx-auto max-w-4xl space-y-6 px-4 sm:px-6 lg:px-8">
            {/* Professional Header */}
            <div className="border border-gray-200 bg-white shadow-sm dark:border-gray-700 dark:bg-gray-900">
              <div className="flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
                <div className="flex items-center gap-3">
                  <div className="p-2 bg-blue-50 dark:bg-blue-900/30">
                    <Blocks className="h-5 w-5 text-blue-600 dark:text-blue-400" />
                  </div>
                  <div>
                    <h1 className="text-xl font-semibold text-gray-900 dark:text-gray-100">Content Studio</h1>
                    <p className="text-sm text-gray-600 dark:text-gray-400">Create your documents with the rich text editor</p>
                  </div>
                  
                  {/* Project Info Display */}
                  {currentProjectId && (
                    <ProjectStorageInfo storageType={currentProjectStorageType} />
                  )}
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
                    setLoadingRef={setLoadingRef}
                    onProjectLoad={(projectData) => {
                      setCurrentProjectId(projectData.id)
                      setCurrentProjectName(projectData.name)
                      setCurrentProjectStorageType(projectData.storageType || "local")
                      setProjectTags(projectData.tags || [])
                      setIsFirstTime(false)
                      // Update URL hash with project ID
                      window.history.pushState(null, '', `#${projectData.id}`)
                    }}
                    onProjectsListUpdate={loadSavedProjectsList}
                    onCreateNew={() => {
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
                if (editorRef.current) {
                  const emptyState =
                    '{"root":{"children":[{"children":[],"direction":null,"format":"","indent":0,"type":"paragraph","version":1}],"direction":null,"format":"","indent":0,"type":"root","version":1}}'
                  editorRef.current.setEditorState(editorRef.current.parseEditorState(emptyState))
                }
                setCurrentProjectId(projectData.id)
                setCurrentProjectName(projectData.name)
                setCurrentProjectStorageType(projectData.storageType)
                setProjectTags(projectData.tags)
                setEditorState(
                  '{"root":{"children":[{"children":[],"direction":null,"format":"","indent":0,"type":"paragraph","version":1}],"direction":null,"format":"","indent":0,"type":"root","version":1}}',
                )
                setIsFirstTime(false)
                // Update URL hash with project ID
                window.history.pushState(null, '', `#${projectData.id}`)
              }}
              onProjectsListUpdate={loadSavedProjectsList}
              onAvailableTagsUpdate={loadAvailableTags}
              generateProjectId={generateProjectId}
            />

            {/* Editor Container - Render based on layout type */}
            {currentLayoutType === "type1" ? (
              <EditorLayoutType1
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
                editorRef={editorRef}
                editorState={editorState}
                onEditorChange={setEditorState}
                onLoadingChange={(setLoading) => {
                  setLoadingRef.current = setLoading
                }}
                projectId={currentProjectId}
              />
            ) : (
              <EditorLayoutType2
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
    </>
  )
}
