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
import { EnhancedStorageAdapter } from "@/lib/storage/editor/enhanced-storage-adapter"
import { syncConfig } from "@/lib/sync/editor/sync-config"
import { SaveAsDialog } from "@/components/editor/extras/editor/save-as-dialog"
import { ExitConfirmDialog } from "@/components/editor/extras/dialogs/exit-confirm-dialog"

interface ProjectData {
  id: string
  name: string
  data: string
  tags: string[]
  size: number
  createdAt: string
  updatedAt: string
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
  const [editorState, setEditorState] = useState<string>("")
  const [currentProjectId, setCurrentProjectId] = useState<string>("")
  const [currentProjectName, setCurrentProjectName] = useState<string>("")
  const [saveAsDialogOpen, setSaveAsDialogOpen] = useState(false)
  const [openDialogOpen, setOpenDialogOpen] = useState(false)
  const [newProjectName, setNewProjectName] = useState("")
  const [savedProjects, setSavedProjects] = useState<ProjectData[]>([])
  const [currentProjectSize, setCurrentProjectSize] = useState<number>(0)
  const [totalStorageUsed, setTotalStorageUsed] = useState<number>(0)
  const setLoadingRef = useRef<((loading: boolean) => void) | null>(null)


  const dbStorage = useRef<EnhancedStorageAdapter>(new EnhancedStorageAdapter())
  const [isDbInitialized, setIsDbInitialized] = useState(false)

  // Add these state variables after the existing ones:
  const [syncStats, setSyncStats] = useState<any>(null)
  const [showSyncStatus, setShowSyncStatus] = useState(false)


  const editorRef = useRef<LexicalEditor | null>(null)

  // Tamanho recomendado em KB (500KB)
  const RECOMMENDED_SIZE_KB = 1000

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
    
    setIsFirstTime(false)
    
  }, [isDbInitialized])

  // Atualizar informações de armazenamento sempre que o editor mudar
  useEffect(() => {
    if (editorState) {
      const size = estimateSize(editorState)
      setCurrentProjectSize(size)
    }
  }, [editorState])

  const storageAdapter = {
    save: async (id: string, name: string, data: string, tags: string[] = []) => {
      if (!id || !name || !data) {
        console.warn("Invalid id, name or data")
        return
      }

      if (!isDbInitialized) {
        throw new Error("Database not initialized")
      }

      const originalSize = estimateSize(data)
      console.log(`Saving project "${name}" (${id}) - Size: ${formatSize(originalSize)}`)

      try {
        await dbStorage.current.save(id, name, data, tags)
        console.log(`Saved project "${name}" (${id}) successfully`)
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
    ): Promise<ProjectData[]> => {
      if (!isDbInitialized) {
        return []
      }

      try {
        return await dbStorage.current.searchProjects(searchTerm, tags, filterMode)
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
    if (!currentProjectId) {
      setSaveAsDialogOpen(true)
      return
    }



    // Get current editor state if editorState is empty
    let stateToSave = editorState
    if (!stateToSave && editorRef.current) {
      try {
        const currentState = editorRef.current.getEditorState()
        stateToSave = JSON.stringify(currentState.toJSON())
      } catch (error) {
        console.error("Failed to get editor state:", error)
        toast.error("Error in editor", {
          description: "Could not get publisher content",
          duration: 4000,
          icon: "⚠️",
        })
        return
      }
    }

    if (!stateToSave || stateToSave.trim() === "") {
      toast.error("Nothing to save", {
        description: "The editor is empty. Add content before saving.",
        duration: 3000,
        icon: "📄",
      })
      return
    }

    try {
      await storageAdapter.save(currentProjectId, currentProjectName, stateToSave, projectTags)

      toast.success("Project saved successfully", {
        description: `"${currentProjectName}" was saved in the database.`,
        duration: 3000,
        icon: "💾",
      })
    } catch (error: any) {
      console.error("Save error:", error)
      toast.error("Error saving", {
        description: "Could not save project. Please try again.",
        duration: 4000,
        icon: "❌",
      })
    }
  }

  const handleSaveAs = async () => {
    if (!newProjectName.trim()) {
      toast.error("Name required", {
        description: "Please enter a name for the project",
        duration: 3000,
        icon: "✏️",
      })
      return
    }



    // Check if project with same name already exists
    const existingProjects = await storageAdapter.list()
    if (existingProjects.some((p) => p.name === newProjectName.trim())) {
      // Generate suggested name with version number
      let suggestedName = `${newProjectName.trim()}-v2`
      let counter = 2

      // Keep incrementing until we find an available name
      while (existingProjects.some((p) => p.name === suggestedName)) {
        counter++
        suggestedName = `${newProjectName.trim()}-v${counter}`
      }

      toast.error("Name already exists", {
        description: `There is already a project named "${newProjectName.trim()}". Suggestion: ${suggestedName}`,
        duration: 5000,
        icon: "🚫",
      })
      return
    }

    // Get current editor state if editorState is empty
    let stateToSave = editorState
    if (!stateToSave && editorRef.current) {
      try {
        const currentState = editorRef.current.getEditorState()
        stateToSave = JSON.stringify(currentState.toJSON())
      } catch (error) {
        console.error("Failed to get editor state:", error)
        toast.error("Error in editor", {
          description: "Could not get publisher content.",
          duration: 4000,
          icon: "⚠️",
        })
        return
      }
    }

    if (!stateToSave || stateToSave.trim() === "") {
      toast.error("Nothing to save", {
        description: "The editor is empty. Add content before saving.",
        duration: 3000,
        icon: "📄",
      })
      return
    }

    try {
      const newProjectId = generateProjectId()
      await storageAdapter.save(newProjectId, newProjectName, stateToSave, projectTags)
      setCurrentProjectId(newProjectId)
      setCurrentProjectName(newProjectName)
      setNewProjectName("")
      setSaveAsDialogOpen(false)
      await loadSavedProjectsList()

      toast.success("New project created", {
        description: `"${newProjectName}" was created and saved successfully.`,
        duration: 3000,
        icon: "🎉",
      })
    } catch (error: any) {
      console.error("Save as error:", error)
      toast.error("Error creating project", {
        description: "Could not create project. Please try again.",
        duration: 4000,
        icon: "❌",
      })
    }
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

   // Add these handler functions after the existing handler functions:
  const handleTitleEdit = () => {
    if (!currentProjectId) {
      toast.error("Sem projeto ativo", {
        description: "Crie ou abra um projeto primeiro",
        duration: 3000,
        icon: "📝",
      })
      return
    }
    setEditingProjectName(currentProjectName)
    setIsEditingTitle(true)
  }

  const handleTitleSave = async () => {
    if (!editingProjectName.trim()) {
      toast.error("Nome obrigatório", {
        description: "O projeto precisa ter um nome",
        duration: 3000,
        icon: "✏️",
      })
      setEditingProjectName(currentProjectName)
      setIsEditingTitle(false)
      return
    }

    if (editingProjectName.trim() === currentProjectName) {
      setIsEditingTitle(false)
      return
    }

    // Check if project with same name already exists
    const existingProjects = await storageAdapter.list()
    if (existingProjects.some((p) => p.name === editingProjectName.trim() && p.id !== currentProjectId)) {
      toast.error("Nome já existe", {
        description: `Já existe um projeto com o nome "${editingProjectName.trim()}"`,
        duration: 4000,
        icon: "🚫",
      })
      setEditingProjectName(currentProjectName)
      setIsEditingTitle(false)
      return
    }

    try {
      // Get current editor state
      let stateToSave = editorState
      if (!stateToSave && editorRef.current) {
        const currentState = editorRef.current.getEditorState()
        stateToSave = JSON.stringify(currentState.toJSON())
      }

      if (stateToSave) {
        await storageAdapter.save(currentProjectId, editingProjectName.trim(), stateToSave, projectTags)
        setCurrentProjectName(editingProjectName.trim())
        await loadSavedProjectsList()

        toast.success("Nome alterado", {
          description: `Projeto renomeado para "${editingProjectName.trim()}"`,
          duration: 3000,
          icon: "✏️",
        })
      }
    } catch (error) {
      console.error("Failed to rename project:", error)
      toast.error("Erro ao renomear", {
        description: "Não foi possível alterar o nome do projeto",
        duration: 4000,
        icon: "❌",
      })
      setEditingProjectName(currentProjectName)
    }

    setIsEditingTitle(false)
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

  return (
    <>
      <div className="min-h-screen bg-gray-50 dark:bg-gray-950">
        <div className="container mx-auto py-10">
          <div className="mx-auto max-w-4xl space-y-8 px-4 sm:px-6 lg:px-8">
            {/* Header Section */}
            <div className="space-y-4 text-center">
              <div className="inline-flex items-center gap-2 rounded-full bg-green-50 px-3 py-1 text-sm font-medium text-green-700 dark:bg-green-900/50 dark:text-green-300">
                <Blocks className="w-4" />
                Studio
              </div>
              <h1 className="text-4xl font-bold tracking-tight text-gray-900 dark:text-gray-100">Content Studio</h1>
              <p className="mx-auto max-w-2xl text-lg text-gray-600 dark:text-gray-300">
                Create your documents with the rich text editor designed
              </p>
            </div>

            <div className="rounded-xl border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900">
              <div className="p-6">
                <div className="flex flex-wrap items-center justify-between gap-4">
                  <div className="flex flex-wrap items-center gap-2">
                    <Link href="/gglexical" passHref>
                      <Button
                        onClick={(e: any) => handleLinkNavigation(e, "/gglexical")}
                        variant="outline"
                        size="sm"
                        className="gap-2 border-gray-200 bg-white hover:bg-gray-50 dark:border-gray-700 dark:bg-gray-800 dark:hover:bg-gray-700"
                      >
                        <Home className="h-4 w-4" />
                        Home
                      </Button>
                    </Link>
                    <Link href="/gglexical/viewer" passHref>
                      <Button
                        onClick={(e: any) => handleLinkNavigation(e, "/gglexical/viewer")}
                        variant="outline"
                        size="sm"
                        className="gap-2 border-gray-200 bg-white hover:bg-gray-50 dark:border-gray-700 dark:bg-gray-800 dark:hover:bg-gray-700"
                        >
                        <Eye className="w-4" />
                        Viewer
                      </Button>
                    </Link>
                  </div>
                  <div className="flex flex-wrap items-center gap-3">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={handleSave}
                      className="gap-2 border-gray-200 bg-white hover:bg-gray-50 dark:border-gray-700 dark:bg-gray-800 dark:hover:bg-gray-700"
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
                        setProjectTags(projectData.tags || [])
                        setIsFirstTime(false)
                      }}
                      onProjectsListUpdate={loadSavedProjectsList}
                      onCreateNew={() => setCreateDialogOpen(true)}
                      currentProjectName={currentProjectName}
                    />
                  </div>
                </div>
              </div>
            </div>

            {/* Project Management Toolbar */}
            <div className="rounded-xl border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900">
              <div className="space-y-4 p-6">
                {/* Top Row - Project Title */}
                <div className="flex items-center justify-center text-center">
                  {isEditingTitle ? (
                    <input
                      type="text"
                      value={editingProjectName}
                      onChange={(e) => setEditingProjectName(e.target.value)}
                      onKeyDown={(e) => {
                        if (e.key === "Enter") handleTitleSave()
                        else if (e.key === "Escape") {
                          setIsEditingTitle(false)
                          setEditingProjectName(currentProjectName)
                        }
                      }}
                      onBlur={handleTitleSave}
                      className="w-full max-w-md bg-transparent px-2 py-1 text-center text-xl font-semibold text-gray-900 outline-none dark:text-gray-100"
                      autoFocus
                    />
                  ) : (
                    <h2
                      className="cursor-pointer rounded px-2 py-1 text-xl font-semibold text-gray-900 transition-colors hover:bg-gray-100 dark:text-gray-100 dark:hover:bg-gray-800"
                      onClick={handleTitleEdit}
                      title="Click to edit project name"
                    >
                      {currentProjectName || "Untitled Project"}
                    </h2>
                  )}
                </div>

                {/* Second Row - Info & Actions */}
                <div className="flex flex-wrap items-center justify-between gap-x-6 gap-y-4 border-y border-gray-200 py-4 dark:border-gray-700">
                  <div className="flex flex-wrap items-center gap-x-6 gap-y-2">
                    <button
                      onClick={() => setAutoSaveEnabled(!autoSaveEnabled)}
                      className="flex items-center gap-2 rounded-md px-3 py-2 transition-colors hover:bg-gray-100 dark:hover:bg-gray-800"
                      disabled={!isDbInitialized}
                    >
                      <div
                        className={`h-3 w-3 rounded-full ${
                          autoSaveEnabled && isDbInitialized ? "bg-green-500 animate-pulse" : "bg-gray-400 dark:bg-gray-600"
                        }`}
                      />
                      <span
                        className={`text-sm font-medium ${
                          autoSaveEnabled && isDbInitialized
                            ? "text-green-700 dark:text-green-400"
                            : "text-gray-500 dark:text-gray-300"
                        }`}
                      >
                        {autoSaveEnabled && isDbInitialized ? "Auto-save" : "Manual"}
                      </span>
                    </button>

                    <div className="flex items-center gap-2">
                      <HardDrive className="h-4 w-4 text-gray-500" />
                      <span className={`text-sm ${getSizeIndicatorColor()}`}>{formatSize(currentProjectSize)}</span>
                      <span className="text-sm text-gray-400 dark:text-gray-500">/</span>
                      <span className="text-sm text-gray-400 dark:text-gray-500">
                        {formatSize(RECOMMENDED_SIZE_KB)}
                      </span>
                    </div>
                  </div>

                  <div className="flex items-center gap-3">
                    {syncStats && (
                      <button
                        onClick={() => setShowSyncStatus(!showSyncStatus)}
                        className="flex items-center gap-2 rounded-lg bg-gray-50 px-3 py-2 transition-colors hover:bg-gray-100 dark:bg-gray-800 dark:hover:bg-gray-700"
                      >
                        <div
                          className={`h-2 w-2 rounded-full ${
                            syncStats.isOnline
                              ? syncStats.isSyncing
                                ? "bg-blue-500 animate-pulse"
                                : "bg-green-500"
                              : syncConfig.isEnabled()
                                ? "bg-red-500"
                                : "bg-gray-400"
                          }`}
                        />
                        <span className="text-xs text-gray-500 dark:text-gray-300">
                          {!syncConfig.isEnabled()
                            ? "Sync Off"
                            : syncStats.isOnline
                              ? syncStats.isSyncing
                                ? "Syncing..."
                                : "Online"
                              : "Offline"}
                        </span>
                        {syncStats.queue.pending > 0 && (
                          <span className="rounded-full bg-blue-100 px-1.5 py-0.5 text-xs text-blue-800 dark:bg-blue-900 dark:text-blue-200">
                            {syncStats.queue.pending}
                          </span>
                        )}
                      </button>
                    )}
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
                    setProjectTags(projectData.tags)
                    setEditorState(
                      '{"root":{"children":[{"children":[],"direction":null,"format":"","indent":0,"type":"paragraph","version":1}],"direction":null,"format":"","indent":0,"type":"root","version":1}}',
                    )
                    setIsFirstTime(false)
                  }}
                  onProjectsListUpdate={loadSavedProjectsList}
                  onAvailableTagsUpdate={loadAvailableTags}
                  generateProjectId={generateProjectId}
                />
              </div>
            </div>

            {/* Editor Container */}
            <div className="rounded-xl border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900">
              <div className="p-4 sm:p-6 md:p-8 lg:p-12">
                <Editor
                  editorRef={editorRef}
                  initialState={editorState}
                  onChange={setEditorState}
                  onLoadingChange={(setLoading) => {
                    setLoadingRef.current = setLoading
                  }}
                />
              </div>
            </div>

            {/* Help Section */}
            <div className="grid grid-cols-1 gap-4 pt-4 md:grid-cols-3">
              <div className="rounded-lg border border-blue-200 bg-blue-50 p-4 dark:border-blue-800 dark:bg-blue-900/30">
                <h3 className="mb-2 font-semibold text-blue-900 dark:text-blue-100">Rich Formatting</h3>
                <p className="text-sm text-blue-700 dark:text-blue-300">
                  Add headings, lists, links, and text formatting
                </p>
              </div>
              <div className="rounded-lg border border-green-200 bg-green-50 p-4 dark:border-green-800 dark:bg-green-900/30">
                <h3 className="mb-2 font-semibold text-green-900 dark:text-green-100">Media & Content</h3>
                <p className="text-sm text-green-700 dark:text-green-300">
                  Insert images, videos, code blocks, and more
                </p>
              </div>
              <div className="rounded-lg border border-purple-200 bg-purple-50 p-4 dark:border-purple-800 dark:bg-purple-900/30">
                <h3 className="mb-2 font-semibold text-purple-900 dark:text-purple-100">Interactive Elements</h3>
                <p className="text-sm text-purple-700 dark:text-purple-300">
                  Create quizzes, callouts, and interactive content
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
      {/* Sync Status Dialog */}
      <Dialog open={showSyncStatus} onOpenChange={setShowSyncStatus}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Synchronization Status</DialogTitle>
          </DialogHeader>
          {syncStats && (
            <div className="space-y-4">
              <div className="flex items-center justify-between rounded-lg bg-gray-50 p-3 dark:bg-gray-800">
                <span className="text-sm text-gray-600 dark:text-gray-300">Connection:</span>
                <div className="flex items-center gap-2">
                  <div className={`h-2 w-2 rounded-full ${syncStats.isOnline ? "bg-green-500" : "bg-red-500"}`} />
                  <span className="text-sm font-medium">{syncStats.isOnline ? "Online" : "Offline"}</span>
                </div>
              </div>

              <div className="space-y-2">
                <h4 className="text-sm font-medium">Sync Queue</h4>
                <div className="grid grid-cols-2 gap-2 text-xs">
                  <div className="rounded bg-blue-50 p-2 dark:bg-blue-900">
                    <div className="font-medium text-blue-800 dark:text-blue-200">Pending</div>
                    <div className="text-blue-600 dark:text-blue-300">{syncStats.queue.pending}</div>
                  </div>
                  <div className="rounded bg-yellow-50 p-2 dark:bg-yellow-900">
                    <div className="font-medium text-yellow-800 dark:text-yellow-200">Processing</div>
                    <div className="text-yellow-600 dark:text-yellow-300">{syncStats.queue.processing}</div>
                  </div>
                  <div className="rounded bg-green-50 p-2 dark:bg-green-900">
                    <div className="font-medium text-green-800 dark:text-green-200">Completed</div>
                    <div className="text-green-600 dark:text-green-300">{syncStats.queue.completed}</div>
                  </div>
                  <div className="rounded bg-red-50 p-2 dark:bg-red-900">
                    <div className="font-medium text-red-800 dark:text-red-200">Failed</div>
                    <div className="text-red-600 dark:text-red-300">{syncStats.queue.failed}</div>
                  </div>
                </div>
              </div>

              {syncStats.lastSync && (
                <div className="rounded-lg bg-gray-50 p-3 dark:bg-gray-800">
                  <span className="text-sm text-gray-600 dark:text-gray-300">Last sync:</span>
                  <div className="text-sm font-medium">{new Date(syncStats.lastSync).toLocaleString()}</div>
                </div>
              )}

              {syncStats.queue.failed > 0 && (
                <Button
                  onClick={async () => {
                    try {
                      await dbStorage.current.retryFailedSync()
                      toast.success("Trying again", {
                        description: "Failed items have been re-queued",
                        duration: 3000,
                        icon: "🔄",
                      })
                    } catch (error) {
                      toast.error("Error trying again", {
                        description: "Unable to reprocess items",
                        duration: 4000,
                        icon: "❌",
                      })
                    }
                  }}
                  className="w-full"
                  variant="outline"
                >
                  Retry Failed Items
                </Button>
              )}

              <div className="flex justify-end">
                <Button variant="outline" onClick={() => setShowSyncStatus(false)}>
                  Close
                </Button>
              </div>
            </div>
          )}
        </DialogContent>
      </Dialog>
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
      />
    </>
  )
}
