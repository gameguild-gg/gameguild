import { LexicalEditor } from "lexical"
import { toast } from "sonner"

// Parameter interface
export interface CheckSelectedProjectParams {
  storageAdapter: {
    load: (id: string) => Promise<{
      id: string
      name: string
      data: string
      tags: string[]
      storageType?: "local" | "gameguild-cloud" | "google-drive"
    } | null>
  }
  editorRef: React.RefObject<LexicalEditor | null>
  setCurrentProjectId: (id: string) => void
  setCurrentProjectName: (name: string) => void
  setCurrentProjectStorageType: (type: "local" | "gameguild-cloud" | "google-drive") => void
  setProjectTags: (tags: string[]) => void
  setIsFirstTime: (value: boolean) => void
}

/**
 * Check and load selected project from URL hash or localStorage
 */
export async function checkSelectedProject(params: CheckSelectedProjectParams): Promise<void> {
  const {
    storageAdapter,
    editorRef,
    setCurrentProjectId,
    setCurrentProjectName,
    setCurrentProjectStorageType,
    setProjectTags,
    setIsFirstTime,
  } = params

  try {
    // First, check for project ID in URL hash
    const hash = window.location.hash.replace('#', '')
    if (hash) {
      try {
        const projectData = await storageAdapter.load(hash)
        if (projectData && projectData.data && editorRef.current) {
          try {
            // Validate JSON format first
            let parsedData
            try {
              parsedData = typeof projectData.data === 'string' ? JSON.parse(projectData.data) : projectData.data
            } catch (parseError) {
              throw new Error("Project data is not valid JSON")
            }
            
            // Validate Lexical editor state structure
            if (!parsedData || typeof parsedData !== 'object' || !parsedData.root) {
              throw new Error("Project data is not in expected Lexical format")
            }
            
            const editorState = editorRef.current.parseEditorState(JSON.stringify(parsedData))
            editorRef.current.setEditorState(editorState)
            
            // Set current project info
            setCurrentProjectId(projectData.id)
            setCurrentProjectName(projectData.name)
            setCurrentProjectStorageType(projectData.storageType || "local")
            setProjectTags(projectData.tags || [])
            
            // Update URL hash if not already set
            if (window.location.hash !== `#${projectData.id}`) {
              window.history.pushState(null, '', `#${projectData.id}`)
            }
            
            toast.success("Projeto carregado", {
              description: `"${projectData.name}" foi aberto com sucesso`,
              duration: 2500,
              icon: "📂",
            })
            
            return // Exit early, don't show first time dialog
          } catch (error) {
            console.error("Failed to load project from hash:", error)
            toast.error("Erro ao carregar projeto", {
              description: `Não foi possível carregar o projeto: ${error instanceof Error ? error.message : 'Unknown error'}`,
              duration: 4000,
              icon: "❌",
            })
          }
        } else {
          toast.error("Projeto não encontrado", {
            description: `Nenhum projeto encontrado com o ID: ${hash}`,
            duration: 4000,
            icon: "❌",
          })
        }
      } catch (error) {
        console.error("Error loading project from hash:", error)
      }
    }
    
    // If no hash or hash loading failed, check localStorage
    const selectedProjectData = localStorage.getItem('selectedProject')
    if (selectedProjectData) {
      const projectData = JSON.parse(selectedProjectData)
      
      // Clear the localStorage item
      localStorage.removeItem('selectedProject')
      
      // Load the project into the editor
      if (projectData.id && projectData.data && editorRef.current) {
        try {
          // Validate JSON format first
          let parsedData
          try {
            parsedData = typeof projectData.data === 'string' ? JSON.parse(projectData.data) : projectData.data
          } catch (parseError) {
            throw new Error("Project data is not valid JSON")
          }
          
          // Validate Lexical editor state structure
          if (!parsedData || typeof parsedData !== 'object' || !parsedData.root) {
            throw new Error("Project data is not in expected Lexical format")
          }
          
          const editorState = editorRef.current.parseEditorState(JSON.stringify(parsedData))
          editorRef.current.setEditorState(editorState)
          
          // Set current project info
          setCurrentProjectId(projectData.id)
          setCurrentProjectName(projectData.name)
          setProjectTags(projectData.tags || [])
          
          // Update URL hash
          window.history.pushState(null, '', `#${projectData.id}`)
          
          toast.success("Projeto carregado", {
            description: `"${projectData.name}" foi aberto com sucesso`,
            duration: 2500,
            icon: "📂",
          })
          
          return // Exit early, don't show first time dialog
        } catch (error) {
          console.error("Failed to load selected project:", error)
          toast.error("Erro ao carregar projeto", {
            description: `Não foi possível carregar o projeto selecionado: ${error instanceof Error ? error.message : 'Unknown error'}`,
            duration: 4000,
            icon: "❌",
          })
        }
      }
    }
  } catch (error) {
    console.error("Error checking selected project:", error)
  }
  
  // If no project was loaded or loading failed, show first time dialog if needed
  setIsFirstTime(false)
}
