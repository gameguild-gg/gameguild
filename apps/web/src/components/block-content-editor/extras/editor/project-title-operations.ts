import { toast } from "sonner"
import type { StorageType } from "@/components/block-content-editor/lib/storage/editor/storage-types"
import type { ProjectPreferences } from "@/components/block-content-editor/lib/storage/editor/project-preferences"

interface ProjectListItem {
  id: string
  name: string
  tags?: string[]
}

interface StorageAdapter {
  save: (id: string, name: string, data: string, tags?: string[], storageType?: StorageType, preferences?: ProjectPreferences) => Promise<void>
  list: () => Promise<ProjectListItem[]>
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
  data: string
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
    data,
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
    if (data) {
      await storageAdapter.save(currentProjectId, editingProjectName.trim(), data, projectTags)
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
