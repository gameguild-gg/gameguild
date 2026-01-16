import { LexicalEditor } from "lexical"
import { toast } from "sonner"
import { detectProjectLayout, extractEditorStates, type LayoutType } from "@/lib/storage/editor/layout-detector"
import type { SequentialPanelStructure, PreviewMode } from "@/lib/storage/editor/panel-structure"

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
      preferences?: any
    } | null>
  }
  editorRef: React.RefObject<LexicalEditor | null>
  blockRefs: React.MutableRefObject<Record<string, LexicalEditor | null>>
  setCurrentProjectId: (id: string) => void
  setCurrentProjectName: (name: string) => void
  setCurrentProjectStorageType: (type: "local" | "gameguild-cloud" | "google-drive") => void
  setProjectTags: (tags: string[]) => void
  setIsFirstTime: (value: boolean) => void
  setCurrentLayout: (layout: LayoutType) => void // Layout auto-detected (single, multiple, or sequential)
  setCurrentProjectType: (type: string) => void // Project type
  setEditorState: (state: string) => void
  setBlockStates: (states: Record<string, string> | ((prev: Record<string, string>) => Record<string, string>)) => void
  setSequentialStructure?: (structure: SequentialPanelStructure) => void
  setCurrentPanelIndex?: (index: number) => void
  setPanelEditorRefs?: (refs: Map<string, React.RefObject<LexicalEditor>>) => void
  setPreviewMode?: (mode: PreviewMode) => void
  setCurrentProjectMode?: (mode: any) => void
  setLastProjectLoadTime?: (time: number) => void
}

/**
 * Check and load selected project from URL hash or localStorage
 */
export async function checkSelectedProject(params: CheckSelectedProjectParams): Promise<void> {
  const {
    storageAdapter,
    editorRef,
    blockRefs,
    setCurrentProjectId,
    setCurrentProjectName,
    setCurrentProjectStorageType,
    setProjectTags,
    setIsFirstTime,
    setCurrentLayout,
    setCurrentProjectType,
    setEditorState,
    setBlockStates,
    setSequentialStructure,
    setCurrentPanelIndex,
    setPanelEditorRefs,
    setPreviewMode,
    setCurrentProjectMode,
    setLastProjectLoadTime,
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
          
          // Extract mode from preferences or default to free-page
          const projectMode = projectData.preferences?.global?.mode || "free-page"
          
          // Set layout and type
          setCurrentLayout(layoutInfo.layoutType)
          setCurrentProjectType(projectData.type)
          
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
          
          // Mark project load time if setter provided
          if (setLastProjectLoadTime) {
            setLastProjectLoadTime(Date.now())
          }
          
          // Handle sequential layout
          if (layoutInfo.isSequential && layoutInfo.sequentialData) {
            if (setSequentialStructure && setCurrentPanelIndex && setPanelEditorRefs && setPreviewMode) {
              setSequentialStructure(layoutInfo.sequentialData)
              setCurrentPanelIndex(0)
              
              // Load previewMode from preferences or default to continuous
              const savedPreviewMode = projectData.preferences?.global?.previewMode || "continuous"
              setPreviewMode(savedPreviewMode as PreviewMode)
              
              // Initialize editor refs for all panels
              const newRefs = new Map<string, React.RefObject<LexicalEditor>>()
              layoutInfo.sequentialData.panels.forEach(panel => {
                newRefs.set(panel.id, { current: undefined as any })
              })
              setPanelEditorRefs(newRefs)
              
              // Update URL hash if not already set
              if (window.location.hash !== `#${projectData.id}`) {
                window.history.pushState(null, '', `#${projectData.id}`)
              }
              
              toast.success("Projeto carregado", {
                description: `"${projectData.name}" foi aberto com sucesso`,
                duration: 2500,
                icon: "📂",
              })
              
              return // Exit early for sequential layout
            }
          }
          
          // Extract editor states
          const states = extractEditorStates(projectData.data, layoutInfo.layoutType)
          
          // Wait for layout to render before loading editor data
          setTimeout(() => {
            try {
              if (layoutInfo.isSinglePanel && editorRef.current && states.blocks.b1) {
                // Single panel layout - uses b1
                if (!states.blocks.b1.root) {
                  throw new Error("Project data is not in expected Lexical format")
                }
                
                const editorState = editorRef.current.parseEditorState(JSON.stringify(states.blocks.b1))
                editorRef.current.setEditorState(editorState)
                setEditorState(JSON.stringify(states.blocks.b1))
                
              } else if (layoutInfo.isMultiPanel && states.blocks) {
                // Multi panel layout - load all blocks dynamically
                const newBlockStates: Record<string, string> = {}
                
                Object.entries(states.blocks).forEach(([blockId, blockState]: [string, any]) => {
                  if (blockState && blockState.root) {
                    // Initialize ref if needed
                    if (!blockRefs.current[blockId]) {
                      blockRefs.current[blockId] = null
                    }
                    // Store state
                    newBlockStates[blockId] = JSON.stringify(blockState)
                  }
                })
                
                setBlockStates(newBlockStates)
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
