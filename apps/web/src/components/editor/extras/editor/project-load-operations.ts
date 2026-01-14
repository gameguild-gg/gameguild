import { LexicalEditor } from "lexical"
import { toast } from "sonner"
import { detectProjectLayout, extractEditorStates, type LayoutType } from "@/lib/storage/editor/layout-detector"

// Parameter interface
export interface CheckSelectedProjectParams {
  storageAdapter: {
    load: (id: string) => Promise<{
      id: string
      name: string
      type: string // Project type (type1, type2, type3, etc.)
      data: string // Layout auto-detected from data structure
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
  setCurrentLayout: (layout: LayoutType) => void // Layout auto-detected (single, dual, or sequential)
  setCurrentProjectType: (type: string) => void // Project type
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
    setCurrentLayout,
    setCurrentProjectType,
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
          // Detect layout automaticamente from data structure
          const layoutInfo = detectProjectLayout(projectData.data)
          
          // Set layout and type
          setCurrentLayout(layoutInfo.layoutType)
          setCurrentProjectType(projectData.type)
          
          // Set project metadata immediately
          setCurrentProjectId(projectData.id)
          setCurrentProjectName(projectData.name)
          setCurrentProjectStorageType(projectData.storageType || "local")
          setProjectTags(projectData.tags || [])
          setIsFirstTime(false)
          
          // Extract editor states
          const states = extractEditorStates(projectData.data, layoutInfo.layoutType)
          
          // Wait for layout to render before loading editor data
          setTimeout(() => {
            try {
              if (layoutInfo.isSinglePanel && editorRef.current && states.single) {
                // Single panel layout
                if (!states.single.root) {
                  throw new Error("Project data is not in expected Lexical format")
                }
                
                const editorState = editorRef.current.parseEditorState(JSON.stringify(states.single))
                editorRef.current.setEditorState(editorState)
                setEditorState(JSON.stringify(states.single))
                
              } else if (layoutInfo.isDualPanel && leftEditorRef.current && rightEditorRef.current && states.left && states.right) {
                // Dual panel layout
                if (!states.left.root || !states.right.root) {
                  throw new Error("Dual panel data is not in expected Lexical format")
                }
                
                // Set left editor
                const leftEditorState = leftEditorRef.current.parseEditorState(JSON.stringify(states.left))
                leftEditorRef.current.setEditorState(leftEditorState)
                setLeftEditorState(JSON.stringify(states.left))
                
                // Set right editor
                const rightEditorState = rightEditorRef.current.parseEditorState(JSON.stringify(states.right))
                rightEditorRef.current.setEditorState(rightEditorState)
                setRightEditorState(JSON.stringify(states.right))
              }
            } catch (error) {
              console.error("Failed to load editor data:", error)
              toast.error("Erro ao carregar dados do editor", {
                description: error instanceof Error ? error.message : "Unknown error",
                duration: 4000,
                icon: "❌",
              })
            }
          }, 100) // Give React time to render the new layout
          
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
    
  } catch (error) {
    console.error("Error checking selected project:", error)
    toast.error("Erro ao carregar projeto", {
      description: "Não foi possível carregar o projeto selecionado",
      duration: 4000,
      icon: "❌",
    })
  }
  
  // If no project was loaded, mark as not first time
  setIsFirstTime(false)
}
