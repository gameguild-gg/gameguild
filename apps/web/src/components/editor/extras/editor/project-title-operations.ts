import { toast } from "sonner"
import type { LexicalEditor } from "lexical"

interface ProjectData {
  id: string
  name: string
  tags?: string[]
}

interface StorageAdapter {
  save: (id: string, name: string, data: string, tags?: string[], storageType?: "local" | "gameguild-cloud" | "google-drive", preferences?: any, type?: string) => Promise<void>
  list: () => Promise<ProjectData[]>
}

export interface TitleEditParams {
  currentProjectId: string
  currentProjectName: string
  setEditingProjectName: (name: string) => void
  setIsEditingTitle: (editing: boolean) => void
}

export interface TitleSaveParams {
  editingProjectName: string
  currentProjectName: string
  currentProjectId: string
  editorState: string // Already formatted via createProjectData
  editorRef: React.RefObject<LexicalEditor | null>
  projectTags: string[]
  storageAdapter: StorageAdapter
  setCurrentProjectName: (name: string) => void
  setEditingProjectName: (name: string) => void
  setIsEditingTitle: (editing: boolean) => void
  loadSavedProjectsList: () => Promise<void>
}

export function handleTitleEdit(params: TitleEditParams) {
  const { currentProjectId, currentProjectName, setEditingProjectName, setIsEditingTitle } = params

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

export async function handleTitleSave(params: TitleSaveParams) {
  const {
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
  } = params

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
