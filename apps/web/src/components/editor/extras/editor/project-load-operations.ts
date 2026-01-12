import { LexicalEditor } from "lexical"
import { toast } from "sonner"

// Parameter interface
export interface CheckSelectedProjectParams {
  storageAdapter: {
    load: (id: string) => Promise<{
      id: string
      name: string
      type: "type1" | "type2"
      data: string
      tags: string[]
      storageType?: "local" | "gameguild-cloud" | "google-drive"
    } | null>
  }
  editorRef: React.RefObject<LexicalEditor | null>
  leftEditorRef: React.RefObject<LexicalEditor | null>
  rightEditorRef: React.RefObject<LexicalEditor | null>
  setCurrentProjectId: (id: string) => void
  setCurrentProjectName: (name: string) => void
  setCurrentProjectStorageType: (type: "local" | "gameguild-cloud" | "google-drive") => void
  setProjectTags: (tags: string[]) => void
  setIsFirstTime: (value: boolean) => void
  setCurrentLayoutType: (type: "type1" | "type2") => void
  setEditorState: (state: string) => void
  setLeftEditorState: (state: string) => void
  setRightEditorState: (state: string) => void
}

/**
 * Check and load selected project from URL hash or localStorage
 */
export async function checkSelectedProject(params: CheckSelectedProjectParams): Promise<void> {
  const {
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
  } = params

  try {
    // First, check for project ID in URL hash
    const hash = window.location.hash.replace('#', '')
    if (hash) {
      try {
        const projectData = await storageAdapter.load(hash)
        if (projectData && projectData.data) {
          const layoutType = projectData.type || "type1"
          setCurrentLayoutType(layoutType)
          
          if (layoutType === "type1" && editorRef.current) {
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
              setEditorState(JSON.stringify(parsedData))
            } catch (validationError: any) {
              throw validationError
            }
          } else if (layoutType === "type2" && leftEditorRef.current && rightEditorRef.current) {
            try {
              // Parse type2 data (has left and right properties)
              let parsedData
              try {
                parsedData = typeof projectData.data === 'string' ? JSON.parse(projectData.data) : projectData.data
              } catch (parseError) {
                throw new Error("Project data is not valid JSON")
              }
              
              // Validate type2 structure
              if (!parsedData || typeof parsedData !== 'object' || !parsedData.left || !parsedData.right) {
                throw new Error("Project data is not in expected Type2 format")
              }
              
              // Parse and set left editor
              const leftParsed = typeof parsedData.left === 'string' ? JSON.parse(parsedData.left) : parsedData.left
              if (!leftParsed.root) {
                throw new Error("Left editor data is not valid Lexical format")
              }
              const leftEditorState = leftEditorRef.current.parseEditorState(JSON.stringify(leftParsed))
              leftEditorRef.current.setEditorState(leftEditorState)
              setLeftEditorState(JSON.stringify(leftParsed))
              
              // Parse and set right editor
              const rightParsed = typeof parsedData.right === 'string' ? JSON.parse(parsedData.right) : parsedData.right
              if (!rightParsed.root) {
                throw new Error("Right editor data is not valid Lexical format")
              }
              const rightEditorState = rightEditorRef.current.parseEditorState(JSON.stringify(rightParsed))
              rightEditorRef.current.setEditorState(rightEditorState)
              setRightEditorState(JSON.stringify(rightParsed))
            } catch (validationError: any) {
              throw validationError
            }
          }
          
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
          
          setIsFirstTime(false)
          return // Exit early, don't show first time dialog
        } else {
          toast.error("Projeto não encontrado", {
            description: `Nenhum projeto encontrado com o ID: ${hash}`,
            duration: 4000,
            icon: "❌",
          })
        }
      } catch (error) {
        console.error("Failed to load project from hash:", error)
        toast.error("Erro ao carregar projeto", {
          description: `Não foi possível carregar o projeto: ${error instanceof Error ? error.message : 'Unknown error'}`,
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
      
      // Load the project into the editor
      if (projectData.id && projectData.data) {
        const layoutType = projectData.type || "type1"
        setCurrentLayoutType(layoutType)
        
        if (layoutType === "type1" && editorRef.current) {
          try {
            const parsedData = JSON.parse(projectData.data)
            if (!parsedData || !parsedData.root) {
              throw new Error("Invalid project data format")
            }
            
            const editorState = editorRef.current.parseEditorState(projectData.data)
            editorRef.current.setEditorState(editorState)
            setEditorState(projectData.data)
          } catch (error) {
            console.error("Error loading project from localStorage:", error)
            toast.error("Erro ao carregar projeto", {
              description: "Não foi possível carregar o projeto do armazenamento local",
              duration: 4000,
              icon: "❌",
            })
            setIsFirstTime(false)
            return
          }
        } else if (layoutType === "type2" && leftEditorRef.current && rightEditorRef.current) {
          try {
            const parsedData = JSON.parse(projectData.data)
            if (!parsedData || !parsedData.left || !parsedData.right) {
              throw new Error("Invalid Type2 project data format")
            }
            
            // Set left editor
            const leftParsed = typeof parsedData.left === 'string' ? JSON.parse(parsedData.left) : parsedData.left
            const leftEditorState = leftEditorRef.current.parseEditorState(JSON.stringify(leftParsed))
            leftEditorRef.current.setEditorState(leftEditorState)
            setLeftEditorState(JSON.stringify(leftParsed))
            
            // Set right editor
            const rightParsed = typeof parsedData.right === 'string' ? JSON.parse(parsedData.right) : parsedData.right
            const rightEditorState = rightEditorRef.current.parseEditorState(JSON.stringify(rightParsed))
            rightEditorRef.current.setEditorState(rightEditorState)
            setRightEditorState(JSON.stringify(rightParsed))
          } catch (error) {
            console.error("Error loading Type2 project from localStorage:", error)
            toast.error("Erro ao carregar projeto", {
              description: "Não foi possível carregar o projeto do armazenamento local",
              duration: 4000,
              icon: "❌",
            })
            setIsFirstTime(false)
            return
          }
        }
        
        setCurrentProjectId(projectData.id)
        setCurrentProjectName(projectData.name)
        setCurrentProjectStorageType(projectData.storageType || "local")
        setProjectTags(projectData.tags || [])
        
        // Update URL hash
        window.history.pushState(null, '', `#${projectData.id}`)
        
        toast.success("Projeto carregado", {
          description: `"${projectData.name}" foi aberto para visualização`,
          duration: 2500,
          icon: "👁️",
        })
      }
    }
    
    // Check if there's a new project type selection
    const newProjectType = localStorage.getItem('newProjectType')
    if (newProjectType && (newProjectType === 'type1' || newProjectType === 'type2')) {
      console.log('Setting initial project type to:', newProjectType)
      setCurrentLayoutType(newProjectType as "type1" | "type2")
      localStorage.removeItem('newProjectType')
    }
  } catch (error) {
    console.error("Error checking selected project:", error)
    toast.error("Erro ao carregar projeto", {
      description: "Não foi possível carregar o projeto selecionado",
      duration: 4000,
      icon: "❌",
    })
  }
  
  setIsFirstTime(false)
}
