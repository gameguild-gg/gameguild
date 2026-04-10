import { LexicalEditor } from "lexical"
import { toast } from "sonner"
import { assetManager } from "@/lib/storage/assets/asset-manager"
import type { ProjectData } from "@/lib/storage/editor/enhanced-storage-adapter"
import type { EngineType } from "@/lib/storage/editor/project-types"

// Parameter interfaces
export interface SaveParams {
  currentProjectId: string
  currentProjectName: string
  currentProjectStorageType: "local" | "gameguild-cloud" | "google-drive"
  editorState: string // Already formatted via createProjectData
  editorRef: React.RefObject<LexicalEditor | null>
  projectTags: string[]
  storageAdapter: {
    save: (id: string, name: string, data: string, tags: string[], storageType: "local" | "gameguild-cloud" | "google-drive", preferences?: any, type?: string, deps?: ProjectData[], engine?: EngineType) => Promise<void>
  }
  calculateProjectAssetsSize: (projectId: string) => Promise<void>
  setSaveAsDialogOpen: (open: boolean) => void
  preferences?: any
  type?: string // Project type (type1, type2, type3)
  deps?: ProjectData[] // Dependent projects (for type3 slideshow)
  engine?: EngineType // Engine type (lexical, blocks)
}

export interface SaveAsParams {
  newProjectName: string
  editorState: string // Already formatted via createProjectData
  editorRef: React.RefObject<LexicalEditor | null>
  projectTags: string[]
  storageOption: "local" | "gameguild-cloud" | "google-drive"
  storageAdapter: {
    save: (id: string, name: string, data: string, tags: string[], storageType: "local" | "gameguild-cloud" | "google-drive", preferences?: any, type?: string) => Promise<void>
    list: () => Promise<Array<{ name: string }>>
  }
  generateProjectId: () => string
  setCurrentProjectId: (id: string) => void
  setCurrentProjectName: (name: string) => void
  setCurrentProjectStorageType: (type: "local" | "gameguild-cloud" | "google-drive") => void
  setNewProjectName: (name: string) => void
  setSaveAsDialogOpen: (open: boolean) => void
  loadSavedProjectsList: () => Promise<void>
  calculateProjectAssetsSize: (projectId: string) => Promise<void>
}

/**
 * Handle project save operation
 */
export async function handleSave(params: SaveParams): Promise<void> {
  const {
    currentProjectId,
    currentProjectName,
    currentProjectStorageType,
    editorState,
    editorRef,
    projectTags,
    storageAdapter,
    calculateProjectAssetsSize,
    setSaveAsDialogOpen,
    preferences,
    type,
    deps,
    engine,
  } = params

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
    await storageAdapter.save(currentProjectId, currentProjectName, stateToSave, projectTags, currentProjectStorageType, preferences, type, deps, engine)

    // Sync asset index with the saved project data
    await assetManager.syncProjectAssets(currentProjectId, stateToSave)

    // Recalculate assets size to reflect any changes
    await calculateProjectAssetsSize(currentProjectId)

    toast.success("Project saved successfully", {
      description: `"${currentProjectName}" was saved to ${currentProjectStorageType}.`,
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

/**
 * Handle save as operation (create new project)
 */
export async function handleSaveAs(params: SaveAsParams): Promise<void> {
  const {
    newProjectName,
    editorState,
    editorRef,
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
  } = params

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
    await storageAdapter.save(newProjectId, newProjectName, stateToSave, projectTags, storageOption)
    
    // Sync asset index with the saved project data
    await assetManager.syncProjectAssets(newProjectId, stateToSave)
    
    setCurrentProjectId(newProjectId)
    setCurrentProjectName(newProjectName)
    setCurrentProjectStorageType(storageOption)
    setNewProjectName("")
    setSaveAsDialogOpen(false)
    await loadSavedProjectsList()

    // Recalculate assets size for new project
    await calculateProjectAssetsSize(newProjectId)

    toast.success("New project created", {
      description: `"${newProjectName}" was created and saved to ${storageOption}.`,
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
