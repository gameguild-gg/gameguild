import { toast } from "sonner"
import type { ProjectData } from "@/components/block-content-editor/lib/storage/editor/project-data"

export type { ProjectData }

export interface CheckSelectedProjectPreviewParams {
  storageAdapter: {
    load: (id: string) => Promise<ProjectData | null>
  }
  setCurrentProject: (project: ProjectData | null) => void
}

/**
 * Checks for a selected project from URL hash or localStorage and loads it for preview.
 * This operation is specifically designed for the preview/viewer page.
 */
export async function checkSelectedProject(
  params: CheckSelectedProjectPreviewParams,
): Promise<void> {
  const { storageAdapter, setCurrentProject } = params

  try {
    // First, check for project ID in URL hash
    const hash = window.location.hash.replace('#', '')
    if (hash) {
      try {
        const projectData = await storageAdapter.load(hash)
        if (projectData && projectData.data) {
          setCurrentProject(projectData)
          
          // Update URL hash if not already set
          if (window.location.hash !== `#${projectData.id}`) {
            window.history.pushState(null, '', `#${projectData.id}`)
          }
          
          toast.success("Projeto carregado", {
            description: `"${projectData.name}" foi aberto para visualização`,
            duration: 2500,
            icon: "👁️",
          })
          
          return // Exit early
        } else {
          toast.error("Projeto não encontrado", {
            description: `Nenhum projeto encontrado com o ID: ${hash}`,
            duration: 4000,
            icon: "❌",
          })
        }
      } catch (error) {
        console.error("Error loading project from hash:", error)
        toast.error("Erro ao carregar projeto", {
          description: "Não foi possível carregar o projeto da URL",
          duration: 4000,
          icon: "❌",
        })
      }
    }
    
    // If no hash or hash loading failed, check localStorage
    const selectedProjectData = localStorage.getItem('selectedProject')
    if (selectedProjectData) {
      const projectData = JSON.parse(selectedProjectData)
      
      // Clear the localStorage item
      localStorage.removeItem('selectedProject')
      
      // Set the current project for viewing
      if (projectData.id && projectData.data) {
        setCurrentProject(projectData)
        
        // Update URL hash
        window.history.pushState(null, '', `#${projectData.id}`)
        
        toast.success("Projeto carregado", {
          description: `"${projectData.name}" foi aberto para visualização`,
          duration: 2500,
          icon: "👁️",
        })
      }
    }
  } catch (error) {
    console.error("Error checking selected project:", error)
    toast.error("Erro ao carregar projeto", {
      description: "Não foi possível carregar o projeto selecionado",
      duration: 4000,
      icon: "❌",
    })
  }
}
