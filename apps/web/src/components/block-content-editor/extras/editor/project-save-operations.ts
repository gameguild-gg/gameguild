import { toast } from "sonner"
import { findAssetUris } from "@game-guild/assets"
import { getDefaultBrowserAssetRepository } from "@game-guild/assets/browser"
import type { StorageType } from "@/components/block-content-editor/lib/storage/editor/storage-types"
import type { ProjectPreferences } from "@/components/block-content-editor/lib/storage/editor/project-preferences"

const assetRepository = getDefaultBrowserAssetRepository()

async function reconcileProjectAssets(projectId: string, data: string) {
  const document = JSON.parse(data) as unknown
  const uris = findAssetUris(document)
  await assetRepository.reconcileUsage(
    { type: "project", id: projectId },
    uris.map((uri, index) => ({ uri, consumerId: `reference-${index}` })),
  )
}

export interface SaveParams {
  currentProjectId: string
  currentProjectName: string
  currentProjectStorageType: StorageType
  data: string
  projectTags: string[]
  storageAdapter: {
    save: (id: string, name: string, data: string, tags: string[], storageType: StorageType, preferences?: ProjectPreferences) => Promise<void>
  }
  calculateProjectAssetsSize: (projectId: string) => Promise<void>
  setSaveAsDialogOpen: (open: boolean) => void
  preferences?: ProjectPreferences
}

export interface SaveAsParams {
  newProjectName: string
  data: string
  projectTags: string[]
  storageOption: StorageType
  storageAdapter: {
    save: (id: string, name: string, data: string, tags: string[], storageType: StorageType, preferences?: ProjectPreferences) => Promise<void>
    list: () => Promise<Array<{ name: string }>>
  }
  generateProjectId: () => string
  setCurrentProjectId: (id: string) => void
  setCurrentProjectName: (name: string) => void
  setCurrentProjectStorageType: (type: StorageType) => void
  setNewProjectName: (name: string) => void
  setSaveAsDialogOpen: (open: boolean) => void
  loadSavedProjectsList: () => Promise<void>
  calculateProjectAssetsSize: (projectId: string) => Promise<void>
  preferences?: ProjectPreferences
}

export async function handleSave(params: SaveParams): Promise<void> {
  const {
    currentProjectId,
    currentProjectName,
    currentProjectStorageType,
    data,
    projectTags,
    storageAdapter,
    calculateProjectAssetsSize,
    preferences,
  } = params

  if (!currentProjectId) return

  if (!data || data.trim() === "") {
    toast.error("Nothing to save", {
      description: "The editor is empty. Add content before saving.",
      duration: 3000,
      icon: "📄",
    })
    return
  }

  try {
    await storageAdapter.save(currentProjectId, currentProjectName, data, projectTags, currentProjectStorageType, preferences)
    await reconcileProjectAssets(currentProjectId, data)
    await calculateProjectAssetsSize(currentProjectId)

    toast.success("Project saved successfully", {
      description: `"${currentProjectName}" was saved to ${currentProjectStorageType}.`,
      duration: 3000,
      icon: "💾",
    })
  } catch (error) {
    console.error("Save error:", error)
    toast.error("Error saving", {
      description: "Could not save project. Please try again.",
      duration: 4000,
      icon: "❌",
    })
  }
}

export async function handleSaveAs(params: SaveAsParams): Promise<void> {
  const {
    newProjectName,
    data,
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
    preferences,
  } = params

  if (!newProjectName.trim()) {
    toast.error("Name required", {
      description: "Please enter a name for the project",
      duration: 3000,
      icon: "✏️",
    })
    return
  }

  const existingProjects = await storageAdapter.list()
  if (existingProjects.some((p) => p.name === newProjectName.trim())) {
    let suggestedName = `${newProjectName.trim()}-v2`
    let counter = 2
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

  if (!data || data.trim() === "") {
    toast.error("Nothing to save", {
      description: "The editor is empty. Add content before saving.",
      duration: 3000,
      icon: "📄",
    })
    return
  }

  try {
    const newProjectId = generateProjectId()
    await storageAdapter.save(newProjectId, newProjectName, data, projectTags, storageOption, preferences)
    await reconcileProjectAssets(newProjectId, data)

    setCurrentProjectId(newProjectId)
    setCurrentProjectName(newProjectName)
    setCurrentProjectStorageType(storageOption)
    setNewProjectName("")
    setSaveAsDialogOpen(false)
    await loadSavedProjectsList()
    await calculateProjectAssetsSize(newProjectId)

    toast.success("New project created", {
      description: `"${newProjectName}" was created and saved to ${storageOption}.`,
      duration: 3000,
      icon: "🎉",
    })
  } catch (error) {
    console.error("Save as error:", error)
    toast.error("Error creating project", {
      description: "Could not create project. Please try again.",
      duration: 4000,
      icon: "❌",
    })
  }
}
