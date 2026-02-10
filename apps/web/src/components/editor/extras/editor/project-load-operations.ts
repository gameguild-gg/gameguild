import { LexicalEditor } from "lexical"
import { toast } from "sonner"
import { detectProjectLayout, extractEditorStates } from "@/lib/storage/editor/layout-detector"
import { getLayoutFromType, type ProjectType, type InternalLayout } from "@/lib/storage/editor/project-types"
import type { SlideshowStructure, PreviewMode } from "@/lib/storage/editor/slideshow-structure"
import type { ProjectData } from "@/lib/storage/editor/enhanced-storage-adapter"

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
      deps?: ProjectData[]
    } | null>
  }
  // Direct DB load function that bypasses closure issues with isDbInitialized
  directDbLoad?: (id: string) => Promise<ProjectData | null>
  editorRef: React.RefObject<LexicalEditor | null>
  blockRefs: React.MutableRefObject<Record<string, LexicalEditor | null>>
  setCurrentProjectId: (id: string) => void
  setCurrentProjectName: (name: string) => void
  setCurrentProjectStorageType: (type: "local" | "gameguild-cloud" | "google-drive") => void
  setProjectTags: (tags: string[]) => void
  setIsFirstTime: (value: boolean) => void
  setCurrentLayout: (layout: InternalLayout) => void // Layout auto-detected (single, multiple, or slideshow)
  setCurrentProjectType: (type: ProjectType) => void // Project type
  setEditorState: (state: string) => void
  setBlockStates: (states: Record<string, string> | ((prev: Record<string, string>) => Record<string, string>)) => void
  setSlideshowStructure?: (structure: SlideshowStructure) => void
  setDeps?: (deps: ProjectData[]) => void
  setResolvedProjects?: (resolved: Map<string, ProjectData | null>) => void
  setCurrentSlideIndex?: (index: number) => void
  setSlideEditorRefs?: (refs: Map<string, React.RefObject<LexicalEditor>>) => void
  setPreviewMode?: (mode: PreviewMode) => void
  setCurrentProjectMode?: (mode: any) => void
  setLastProjectLoadTime?: (time: number) => void
  setCurrentProjectPreferences?: (preferences: any) => void
}

/**
 * Check and load selected project from URL hash or localStorage
 */
export async function checkSelectedProject(params: CheckSelectedProjectParams): Promise<void> {
  const {
    storageAdapter,
    directDbLoad,
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
    setSlideshowStructure,
    setDeps,
    setResolvedProjects,
    setCurrentSlideIndex,
    setSlideEditorRefs,
    setPreviewMode,
    setCurrentProjectMode,
    setLastProjectLoadTime,
    setCurrentProjectPreferences,
  } = params
  
  // Use directDbLoad if provided (avoids closure issues), otherwise fall back to storageAdapter
  const loadProject = directDbLoad || storageAdapter.load
  console.log(`[project-load-ops] Using ${directDbLoad ? 'directDbLoad' : 'storageAdapter.load'}`)

  try {
    // First, check for project ID in URL hash
    const hash = window.location.hash.replace('#', '')
    console.log(`[project-load-ops] Hash from URL: "${hash}"`)
    if (hash) {
      try {
        console.log(`[project-load-ops] Loading project from hash: ${hash}`)
        const projectData = await loadProject(hash)
        console.log(`[project-load-ops] Loaded project:`, projectData ? `${projectData.name} (type: ${projectData.type})` : 'null')
        if (projectData && projectData.data) {
          // Detect layout automaticamente from data structure
          const layoutInfo = detectProjectLayout(projectData.data)
          
          // Extract mode from preferences or default to free-page
          const projectMode = projectData.preferences?.global?.mode || "free-page"
          
          // Layout \u00e9 derivado diretamente do tipo de projeto
          const finalLayout = getLayoutFromType(projectData.type as ProjectType)
          
          // Set layout and type
          setCurrentLayout(finalLayout)
          setCurrentProjectType(projectData.type as ProjectType)
          
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
          
          // Handle slideshow layout
          if (layoutInfo.hasSlides && layoutInfo.slideshowData) {
            if (setSlideshowStructure && setCurrentSlideIndex && setSlideEditorRefs && setPreviewMode) {
              setSlideshowStructure(layoutInfo.slideshowData)
              
              // Load deps from ProjectData
              if (setDeps) {
                setDeps(projectData.deps || [])
              }
              
              setCurrentSlideIndex(0)
              
              // Load previewMode from preferences or default to continuous
              const savedPreviewMode = projectData.preferences?.global?.previewMode || "continuous"
              setPreviewMode(savedPreviewMode as PreviewMode)
              
              // Initialize editor refs for all slides
              const newRefs = new Map<string, React.RefObject<LexicalEditor>>()
              layoutInfo.slideshowData.slides.forEach(slide => {
                newRefs.set(slide.id, { current: undefined as any })
              })
              setSlideEditorRefs(newRefs)
              
              // Load independent projects if setResolvedProjects is provided
              if (setResolvedProjects) {
                const independentSlides = layoutInfo.slideshowData.slides.filter(
                  (s: any) => s.projectRef && !s.projectRef.isDependent
                )
                
                console.log(`[project-load-ops] Found ${independentSlides.length} independent slides`)
                
                if (independentSlides.length > 0) {
                  const results = new Map<string, ProjectData | null>()
                  await Promise.all(
                    independentSlides.map(async (slide: any) => {
                      try {
                        console.log(`[project-load-ops] Loading project ${slide.projectRef.projectId} for slide ${slide.id}`)
                        // Use loadProject which prefers directDbLoad (avoids closure issues)
                        const project = await loadProject(slide.projectRef.projectId)
                        console.log(`[project-load-ops] Loaded project for slide ${slide.id}:`, project ? project.name : 'null')
                        results.set(slide.id, project as ProjectData | null)
                      } catch (error) {
                        console.error(`Failed to load independent project for slide ${slide.id}:`, error)
                        results.set(slide.id, null)
                      }
                    })
                  )
                  console.log(`[project-load-ops] Calling setResolvedProjects with ${results.size} entries`)
                  setResolvedProjects(results)
                } else {
                  setResolvedProjects(new Map())
                }
              }
              
              // Update URL hash if not already set
              if (window.location.hash !== `#${projectData.id}`) {
                window.history.pushState(null, '', `#${projectData.id}`)
              }
              
              toast.success("Projeto carregado", {
                description: `"${projectData.name}" foi aberto com sucesso`,
                duration: 2500,
                icon: "📂",
              })
              
              return // Exit early for slideshow layout
            }
          }
          
          // Extract editor states using project type
          const states = extractEditorStates(projectData.data, projectData.type as ProjectType)
          
          // Wait for layout to render before loading editor data
          setTimeout(() => {
            try {
              if (finalLayout === "single" && editorRef.current && states.blocks.b1) {
                // Single panel layout - uses b1
                // States are in cells format - store them directly
                setEditorState(JSON.stringify(states.blocks.b1))
                
                // Convert cells to Lexical for UI
                const { cellsToLexical } = require("@/lib/storage/editor/cell-structure")
                const lexicalState = cellsToLexical(states.blocks.b1)
                const editorState = editorRef.current.parseEditorState(JSON.stringify(lexicalState))
                editorRef.current.setEditorState(editorState)
                
              } else if (finalLayout === "multiple" && states.blocks) {
                // Multi panel layout - load all blocks dynamically
                // Clear existing blockRefs when loading a new project
                blockRefs.current = {}
                
                const newBlockStates: Record<string, string> = {}
                const { cellsToLexical } = require("@/lib/storage/editor/cell-structure")
                
                Object.entries(states.blocks).forEach(([blockId, blockState]: [string, any]) => {
                  if (blockState) {
                    // Initialize ref for each block
                    blockRefs.current[blockId] = null
                    // Store state in cells format
                    newBlockStates[blockId] = JSON.stringify(blockState)
                  }
                })
                
                // Set the new block states - this will trigger re-render
                setBlockStates(newBlockStates)
                
                // Wait for refs to be populated and load states into editors
                setTimeout(() => {
                  Object.entries(newBlockStates).forEach(([blockId, stateString]) => {
                    const ref = blockRefs.current[blockId]
                    if (ref) {
                      try {
                        // Convert cells to Lexical for UI
                        const cellsData = JSON.parse(stateString)
                        const lexicalState = cellsToLexical(cellsData)
                        const editorState = ref.parseEditorState(JSON.stringify(lexicalState))
                        ref.setEditorState(editorState)
                      } catch (error) {
                        console.error(`Failed to load state for block ${blockId}:`, error)
                      }
                    }
                  })
                }, 150)
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
