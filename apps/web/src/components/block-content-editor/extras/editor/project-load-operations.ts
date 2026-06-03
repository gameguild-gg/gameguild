import { toast } from "sonner"
import { deserializeProject } from "@/components/block-content-editor/lib/storage/editor/block-storage"
import type { BlockArray } from "@/components/block-content-editor/lib/storage/editor/block-structure"
import type { ProjectData } from "@/components/block-content-editor/lib/storage/editor/enhanced-storage-adapter"
import type { ProjectType } from "@/components/block-content-editor/lib/storage/editor/project-types"
import { getProjectTypeLabel } from "@/components/block-content-editor/lib/storage/editor/project-types"

export interface CheckSelectedProjectParams {
  storageAdapter: {
    load: (id: string) => Promise<{
      id: string
      name: string
      data: string
      tags: string[]
      storageType?: "local" | "gameguild-cloud" | "google-drive"
      preferences?: any
    } | null>
  }
  directDbLoad?: (id: string) => Promise<ProjectData | null>
  setCurrentProjectId: (id: string) => void
  setCurrentProjectName: (name: string) => void
  setCurrentProjectStorageType: (type: "local" | "gameguild-cloud" | "google-drive") => void
  setProjectTags: (tags: string[]) => void
  setIsFirstTime: (value: boolean) => void
  setCurrentProjectMode?: (mode: any) => void
  setLastProjectLoadTime?: (time: number) => void
  setCurrentProjectPreferences?: (preferences: any) => void
  setBlocks: (blocks: BlockArray) => void
  /** Page-declared filter — refuse to load projects whose type isn't allowed here. */
  allowedProjectTypes?: ProjectType[]
}

/**
 * Check and load selected project from URL hash on mount.
 */
export async function checkSelectedProject(params: CheckSelectedProjectParams): Promise<void> {
  const {
    storageAdapter,
    directDbLoad,
    setCurrentProjectId,
    setCurrentProjectName,
    setCurrentProjectStorageType,
    setProjectTags,
    setIsFirstTime,
    setCurrentProjectMode,
    setLastProjectLoadTime,
    setCurrentProjectPreferences,
    setBlocks,
    allowedProjectTypes,
  } = params

  const loadProject = directDbLoad || storageAdapter.load

  try {
    const hash = window.location.hash.replace("#", "")
    if (!hash) {
      setIsFirstTime(false)
      return
    }

    const projectData = await loadProject(hash)
    if (!projectData || !projectData.data) {
      toast.error("Projeto não encontrado", {
        description: `Nenhum projeto encontrado com o ID: ${hash}`,
        duration: 4000,
        icon: "❌",
      })
      setIsFirstTime(false)
      return
    }

    const projectMode = projectData.preferences?.global?.mode || "free-page"
    const projectType: ProjectType = projectData.preferences?.global?.projectType ?? "general"

    if (allowedProjectTypes && allowedProjectTypes.length > 0 && !allowedProjectTypes.includes(projectType)) {
      toast.error("Projeto incompatível com esta página", {
        description: `Este editor aceita: ${allowedProjectTypes
          .map((t) => getProjectTypeLabel(t))
          .join(", ")}. O projeto é do tipo "${getProjectTypeLabel(projectType)}".`,
        duration: 5000,
        icon: "🚫",
      })
      setIsFirstTime(false)
      // Drop the hash so we don't keep retrying on refresh.
      if (typeof window !== "undefined") {
        window.history.replaceState(null, "", window.location.pathname + window.location.search)
      }
      return
    }

    setCurrentProjectId(projectData.id)
    setCurrentProjectName(projectData.name)
    setCurrentProjectStorageType(projectData.storageType || "local")
    setProjectTags(projectData.tags || [])
    setIsFirstTime(false)

    if (setCurrentProjectMode) setCurrentProjectMode(projectMode)
    if (setCurrentProjectPreferences) setCurrentProjectPreferences(projectData.preferences)
    if (setLastProjectLoadTime) setLastProjectLoadTime(Date.now())

    setBlocks(deserializeProject(projectData.data))

    if (window.location.hash !== `#${projectData.id}`) {
      window.history.pushState(null, "", `#${projectData.id}`)
    }

    toast.success("Projeto carregado", {
      description: `"${projectData.name}" foi aberto com sucesso`,
      duration: 2500,
      icon: "📂",
    })
  } catch (error) {
    console.error("Error checking selected project:", error)
    toast.error("Erro ao carregar projeto", {
      description: error instanceof Error ? error.message : "Não foi possível carregar o projeto selecionado",
      duration: 4000,
      icon: "❌",
    })
    setIsFirstTime(false)
  }
}
