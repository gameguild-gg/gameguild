import { LexicalEditor } from "lexical"
import { toast } from "sonner"
import { extractEditorStates } from "@/components/block-content-editor/lib/storage/editor/layout-detector"
import { ENGINE_TYPES, type EngineType } from "@/components/block-content-editor/lib/storage/editor/project-types"
import type { ProjectData } from "@/components/block-content-editor/lib/storage/editor/enhanced-storage-adapter"
import type { CellularContent } from "@/components/block-content-editor/lib/storage/editor/cell-structure"

// Parameter interface
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
  // Direct DB load function that bypasses closure issues with isDbInitialized
  directDbLoad?: (id: string) => Promise<ProjectData | null>
  editorRef: React.RefObject<LexicalEditor | null>
  setCurrentProjectId: (id: string) => void
  setCurrentProjectName: (name: string) => void
  setCurrentProjectStorageType: (type: "local" | "gameguild-cloud" | "google-drive") => void
  setProjectTags: (tags: string[]) => void
  setIsFirstTime: (value: boolean) => void
  setEditorState: (state: string) => void
  setCurrentProjectMode?: (mode: any) => void
  setLastProjectLoadTime?: (time: number) => void
  setCurrentProjectPreferences?: (preferences: any) => void
  setCurrentEngine?: (engine: EngineType) => void
  setBlockArrayCells?: (cells: CellularContent) => void
}

/**
 * Check and load selected project from URL hash or localStorage
 */
export async function checkSelectedProject(params: CheckSelectedProjectParams): Promise<void> {
  const {
    storageAdapter,
    directDbLoad,
    editorRef,
    setCurrentProjectId,
    setCurrentProjectName,
    setCurrentProjectStorageType,
    setProjectTags,
    setIsFirstTime,
    setEditorState,
    setCurrentProjectMode,
    setLastProjectLoadTime,
    setCurrentProjectPreferences,
    setCurrentEngine,
    setBlockArrayCells,
  } = params
  
  // Use directDbLoad if provided (avoids closure issues), otherwise fall back to storageAdapter
  const loadProject = directDbLoad || storageAdapter.load

  try {
    // First, check for project ID in URL hash
    const hash = window.location.hash.replace('#', '')
    if (hash) {
      try {
        const projectData = await loadProject(hash)
        if (projectData && projectData.data) {
          // Extract mode from preferences or default to free-page
          const projectMode = projectData.preferences?.global?.mode || "free-page"
          
          // Set project metadata immediately
          setCurrentProjectId(projectData.id)
          setCurrentProjectName(projectData.name)
          setCurrentProjectStorageType(projectData.storageType || "local")
          setProjectTags(projectData.tags || [])
          setIsFirstTime(false)
          
          // Set project mode if setter provided
          if (setCurrentProjectMode) {
            setCurrentProjectMode(projectMode)
          }
          
          // Set project preferences if setter provided
          if (setCurrentProjectPreferences) {
            setCurrentProjectPreferences(projectData.preferences)
          }
          
          // Mark project load time if setter provided
          if (setLastProjectLoadTime) {
            setLastProjectLoadTime(Date.now())
          }
          
          // Restore engine type
          const projectEngine: EngineType = (projectData as any).engine || ENGINE_TYPES.LEXICAL
          if (setCurrentEngine) {
            setCurrentEngine(projectEngine)
          }
          
          // Handle blocks engine: restore cells and exit early
          if (projectEngine === ENGINE_TYPES.BLOCKS && setBlockArrayCells) {
            const states = extractEditorStates(projectData.data)
            const cellsData = states.blocks?.b1 || { order: [], blocks: {} }
            setBlockArrayCells(cellsData as any)
            
            // Update URL hash if not already set
            if (window.location.hash !== `#${projectData.id}`) {
              window.history.pushState(null, '', `#${projectData.id}`)
            }
            
            toast.success("Projeto carregado", {
              description: `"${projectData.name}" foi aberto com sucesso`,
              duration: 2500,
              icon: "📂",
            })
            
            return // Exit early for blocks engine
          }
          
          // Extract editor states (single block b1)
          const states = extractEditorStates(projectData.data)
          
          // Wait for layout to render before loading editor data
          setTimeout(() => {
            try {
              if (editorRef.current && states.blocks.b1) {
                // Single panel layout - uses b1
                setEditorState(JSON.stringify(states.blocks.b1))
                
                // Convert cells to Lexical for UI
                const { cellsToLexical } = require("@/components/block-content-editor/lib/storage/editor/cell-converters/lexical")
                const lexicalState = cellsToLexical(states.blocks.b1)
                const editorState = editorRef.current.parseEditorState(JSON.stringify(lexicalState))
                editorRef.current.setEditorState(editorState)
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
