import { toast } from "sonner"
import type { ProjectPreferences } from "@/lib/storage/editor/project-preferences"
import type { ProjectType } from "@/lib/storage/editor/project-types"
import { detectProjectLayout } from "@/lib/storage/editor/layout-detector"

export interface ProjectData {
  id: string
  name: string
  type: ProjectType // Project type (not layout)
  data: string
  tags: string[]
  size: number
  createdAt: string
  updatedAt: string
  storageType?: "local" | "gameguild-cloud" | "google-drive"
  isLocallyAvailable?: boolean
  preferences?: ProjectPreferences
  deps?: any[]
}

export interface CheckSelectedProjectPreviewParams {
  storageAdapter: {
    load: (id: string) => Promise<ProjectData | null>
  }
  setCurrentProject: (project: ProjectData | null) => void
  setResolvedProjects?: (projects: Map<string, any>) => void
}

/**
 * Checks for a selected project from URL hash or localStorage and loads it for preview.
 * This operation is specifically designed for the preview/viewer page.
 */
export async function checkSelectedProject(
  params: CheckSelectedProjectPreviewParams,
): Promise<void> {
  const { storageAdapter, setCurrentProject, setResolvedProjects } = params

  // Helper to load independent projects for slideshows
  const loadIndependentProjects = async (projectData: ProjectData) => {
    if (!setResolvedProjects) return
    
    const layoutInfo = detectProjectLayout(projectData.data)
    if (!layoutInfo.hasSlides || !layoutInfo.slideshowData) return
    
    const independentSlides = layoutInfo.slideshowData.slides.filter(
      (slide: any) => slide.projectRef && !slide.projectRef.isDependent
    )
    
    if (independentSlides.length === 0) return
    
    const results = new Map<string, ProjectData | null>()
    await Promise.all(
      independentSlides.map(async (slide: any) => {
        const projectId = slide.projectRef!.projectId
        try {
          const project = await storageAdapter.load(projectId)
          results.set(slide.id, project)
        } catch (error) {
          console.error(`Failed to load independent project ${projectId}:`, error)
          results.set(slide.id, null)
        }
      })
    )
    setResolvedProjects(results)
  }

  try {
    // First, check for project ID in URL hash
    const hash = window.location.hash.replace('#', '')
    if (hash) {
      try {
        const projectData = await storageAdapter.load(hash)
        if (projectData && projectData.data) {
          setCurrentProject(projectData)
          
          // Load independent projects for slideshows inline to avoid race conditions
          await loadIndependentProjects(projectData)
          
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
        
        // Load independent projects for slideshows inline to avoid race conditions
        await loadIndependentProjects(projectData)
        
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
