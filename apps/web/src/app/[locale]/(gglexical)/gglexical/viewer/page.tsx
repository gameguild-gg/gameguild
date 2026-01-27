"use client"

import { useState, useEffect, useRef } from "react"
import { toast } from "sonner"
import { Eye, Home, Blocks, Menu, X } from "lucide-react"
import { Button } from "@/components/ui/button"
import { OpenProjectDialogPreview } from "@/components/editor/extras/preview/open-project-dialog-preview"
import { EnhancedStorageAdapter } from "@/lib/storage/editor/enhanced-storage-adapter"
import Link from "next/link"
import { PreviewRendererType1 } from "@/components/editor/extras/preview/preview-renderer-type1"
import { PreviewRendererType2 } from "@/components/editor/extras/preview/preview-renderer-type2"
import { PreviewRendererSlideshowContinuous } from "@/components/editor/extras/preview/preview-renderer-slideshow-continuous"
import { PreviewRendererSlideshowSlide } from "@/components/editor/extras/preview/preview-renderer-slideshow-slide"
import { useRouter } from "next/navigation"
import { ExitConfirmDialog } from "@/components/editor/extras/dialogs/exit-confirm-dialog"
import { detectProjectLayout, extractEditorStates } from "@/lib/storage/editor/layout-detector"
import { getLayoutFromType, type ProjectType, type InternalLayout } from "@/lib/storage/editor/project-types"
import { checkSelectedProject as checkProjectPreview } from "@/components/editor/extras/preview/preview-load-operations"
import type { ProjectData } from "@/components/editor/extras/preview/preview-load-operations"
import { cellsToLexical } from "@/lib/storage/editor/cell-structure"


export default function PreviewPage() {
  const [currentProject, setCurrentProject] = useState<ProjectData | null>(null)
  const [openDialogOpen, setOpenDialogOpen] = useState(false)
  const [availableTags, setAvailableTags] = useState<Array<{ name: string; usageCount: number }>>([])
  const [isDbInitialized, setIsDbInitialized] = useState(false)
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const [exitDialogOpen, setExitDialogOpen] = useState(false)
  const [nextUrl, setNextUrl] = useState<string>("")
  
  const router = useRouter()

  const dbStorage = useRef<EnhancedStorageAdapter>(new EnhancedStorageAdapter())

  const handleLinkNavigation = (event: React.MouseEvent<HTMLAnchorElement>, url: string) => {
    if (event.ctrlKey || event.metaKey || event.button === 1) {
      return
    }
    event.preventDefault()

    if (currentProject) {
      setNextUrl(url)
      setExitDialogOpen(true)
    } else {
      router.push(url)
    }
  }

  useEffect(() => {
    const initDB = async () => {
      try {
        await dbStorage.current.init()
        setIsDbInitialized(true)
        await loadAvailableTags()
        
        // Check if there's a selected project from the main page or URL hash
        await checkProjectPreview({
          storageAdapter: {
            load: (id: string) => dbStorage.current.load(id),
          },
          setCurrentProject,
        })
        
      } catch (error) {
        console.error("Failed to initialize IndexedDB:", error)
        toast.error("Storage error", {
          description: "Could not initialize database. Some features may not work.",
          duration: 5000,
          icon: "⚠️",
        })
      }
    }

    initDB()
  }, [])

  const loadAvailableTags = async () => {
    try {
      const tags = await dbStorage.current.getAllTags()
      setAvailableTags(tags)
    } catch (error) {
      console.error("Failed to load tags:", error)
    }
  }

  const storageAdapter = {
    load: async (id: string): Promise<ProjectData | null> => {
      if (!isDbInitialized) {
        throw new Error("Database not initialized")
      }

      try {
        const projectData = await dbStorage.current.load(id)
        return projectData
      } catch (error) {
        console.error("Failed to load project:", error)
        return null
      }
    },

    list: async (): Promise<ProjectData[]> => {
      if (!isDbInitialized) {
        return []
      }

      try {
        const projects = await dbStorage.current.list()
        return projects
      } catch (error) {
        console.error("Failed to list projects:", error)
        return []
      }
    },

    searchProjects: async (
      searchTerm: string,
      tags: string[],
      filterMode: "all" | "any" = "any",
      storageTypeFilter?: "local" | "gameguild-cloud" | "google-drive",
    ): Promise<ProjectData[]> => {
      if (!isDbInitialized) {
        return []
      }

      try {
        return await dbStorage.current.searchProjects(searchTerm, tags, filterMode, storageTypeFilter)
      } catch (error) {
        console.error("Failed to search projects:", error)
        return []
      }
    },
  }

  const handleNavigation = (url: string) => {
    if (currentProject) {
      setNextUrl(url)
      setExitDialogOpen(true)
    } else {
      router.push(url)
    }
  }

  const handleProjectLoad = (projectData: ProjectData) => {
    setCurrentProject(projectData)
    // Update URL hash with project ID
    window.history.pushState(null, '', `#${projectData.id}`)
  }

  const getLayoutAndStates = (): { 
    layout: InternalLayout; 
    states: { blocks: Record<string, any> };
    hasSlides: boolean;
    slideshowData?: any;
    projectType?: ProjectType;
    previewMode?: "continuous" | "slide";
  } => {
    if (!currentProject) {
      return {
        layout: "single",
        states: { blocks: {} },
        hasSlides: false,
      }
    }
    
    const layoutInfo = detectProjectLayout(currentProject.data)
    
    // Layout é derivado diretamente do tipo de projeto
    const finalLayout = getLayoutFromType(currentProject.type)
    
    const cellStates = extractEditorStates(currentProject.data, currentProject.type)
    
    // Convert cells to Lexical for preview renderers
    const states = {
      blocks: Object.entries(cellStates.blocks).reduce((acc, [blockId, cellsData]) => {
        acc[blockId] = cellsToLexical(cellsData)
        return acc
      }, {} as Record<string, any>)
    }
    
    // Get preview mode from preferences (default to continuous)
    const previewMode = currentProject.preferences?.global?.previewMode || "continuous"
    
    return {
      layout: finalLayout,
      states,
      hasSlides: layoutInfo.hasSlides,
      slideshowData: layoutInfo.slideshowData,
      projectType: currentProject.type,
      previewMode,
    }
  }

  const { layout: currentLayout, states, hasSlides, slideshowData, projectType, previewMode } = getLayoutAndStates()

  return (
    <>
      <div className="min-h-screen bg-gray-50 dark:bg-gray-950">
        <div className="container mx-auto py-10">
          <div
            className={`mx-auto space-y-4 px-4 sm:px-6 lg:px-8 ${
              currentProject && Object.keys(states.blocks).length > 0 ? "max-w-full" : "max-w-4xl"
            }`}
          >
            {/* Professional Header */}
            <div className="border border-gray-200 bg-white shadow-sm dark:border-gray-700 dark:bg-gray-900">
              <div className="flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
                <div className="flex items-center gap-3">
                  <div className="p-2 bg-green-50 dark:bg-green-900/30">
                    <Eye className="h-5 w-5 text-green-600 dark:text-green-400" />
                  </div>
                  <div>
                    <h1 className="text-xl font-semibold text-gray-900 dark:text-gray-100">Content Viewer</h1>
                    <p className="text-sm text-gray-600 dark:text-gray-400">View your documents as readers see them</p>
                  </div>
                  
                  {/* Project Info Display */}
                  {currentProject && (
                    <div className="ml-6 flex items-center gap-4 pl-6 border-l border-gray-300 dark:border-gray-600">
                      <div className="flex items-center gap-2 text-sm">
                        <span className="text-gray-600 dark:text-gray-400">Viewing:</span>
                        <span className="font-medium text-gray-800 dark:text-gray-200 bg-gray-100 dark:bg-gray-800 px-3 py-1">
                          {currentProject.name}
                        </span>
                      </div>
                    </div>
                  )}
                </div>
                <div className="flex items-center gap-2">
                  <Link href="/gglexical" passHref>
                    <Button
                      onClick={(e: any) => handleLinkNavigation(e, "/gglexical")}
                      variant="ghost"
                      size="sm"
                      className="gap-2 hover:bg-gray-100 dark:hover:bg-gray-800"
                    >
                      <Home className="h-4 w-4" />
                      Home
                    </Button>
                  </Link>
                  <Link href="/gglexical/studio" passHref>
                    <Button
                      onClick={(e: any) => handleLinkNavigation(e, "/gglexical/studio")}
                      variant="ghost"
                      size="sm"
                      className="gap-2 hover:bg-gray-100 dark:hover:bg-gray-800"
                    >
                      <Blocks className="h-4 w-4" />
                      Studio
                    </Button>
                  </Link>
                </div>
              </div>

              {/* Action Bar */}
              <div className="flex items-center justify-between gap-4 p-4 bg-white dark:bg-gray-900">
                <div className="flex items-center gap-3">
                  {currentProject && Object.keys(states.blocks).length > 0 && currentLayout === "single" && (
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setSidebarOpen(true)}
                      className="gap-2 border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800 bg-transparent lg:hidden"
                    >
                      <Menu className="h-4 w-4" />
                      Documents
                    </Button>
                  )}

                  <OpenProjectDialogPreview
                    open={openDialogOpen}
                    onOpenChange={setOpenDialogOpen}
                    isDbInitialized={isDbInitialized}
                    storageAdapter={storageAdapter}
                    availableTags={availableTags}
                    onProjectLoad={handleProjectLoad}
                  />
                </div>

                {/* Status/Info Display */}
                {currentProject && (
                  <div className="flex items-center gap-4">
                    {currentProject.tags && currentProject.tags.length > 0 && (
                      <div className="flex items-center gap-2">
                        {currentProject.tags.slice(0, 2).map((tag) => (
                          <span
                            key={tag}
                            className="inline-flex items-center bg-blue-100 dark:bg-blue-900/50 px-2 py-1 text-xs font-medium text-blue-800 dark:text-blue-300"
                          >
                            {tag}
                          </span>
                        ))}
                        {currentProject.tags.length > 2 && (
                          <span className="text-xs text-gray-500 dark:text-gray-400">
                            +{currentProject.tags.length - 2}
                          </span>
                        )}
                      </div>
                    )}
                    <div className="text-xs text-gray-500 dark:text-gray-400 bg-gray-50 dark:bg-gray-800 px-3 py-1.5">
                      Updated {new Date(currentProject.updatedAt).toLocaleDateString()}
                    </div>
                  </div>
                )}
              </div>
            </div>

            {currentProject && (Object.keys(states.blocks).length > 0 || hasSlides) ? (
              <>
                {hasSlides && slideshowData ? (
                  previewMode === "slide" ? (
                    <PreviewRendererSlideshowSlide
                      structure={slideshowData}
                      projectId={currentProject.id}
                      projectName={currentProject.name}
                      storageAdapter={storageAdapter}
                      preferences={currentProject.preferences}
                    />
                  ) : (
                    <PreviewRendererSlideshowContinuous
                      structure={slideshowData}
                      projectId={currentProject.id}
                      projectName={currentProject.name}
                      storageAdapter={storageAdapter}
                      preferences={currentProject.preferences}
                    />
                  )
                ) : currentLayout === "single" && Object.keys(states.blocks).length > 0 ? (
                  <PreviewRendererType1
                    serializedState={Object.values(states.blocks)[0] as any}
                    currentProject={currentProject}
                    storageAdapter={storageAdapter}
                    availableTags={availableTags}
                    isDbInitialized={isDbInitialized}
                    onProjectSelect={handleProjectLoad}
                    sidebarOpen={sidebarOpen}
                    setSidebarOpen={setSidebarOpen}
                  />
                ) : currentLayout === "multiple" && Object.keys(states.blocks).length >= 1 ? (
                  <PreviewRendererType2 blockStates={states.blocks as Record<string, any>} projectId={currentProject.id} storageAdapter={storageAdapter} preferences={currentProject.preferences} />
                ) : (
                  <div className="border border-red-200 bg-red-50 shadow-sm dark:border-red-700 dark:bg-red-900/20">
                    <div className="p-6 px-12 py-12">
                      <div className="py-16 text-center">
                        <Eye className="mx-auto mb-4 h-16 w-16 text-red-300 dark:text-red-600" />
                        <h3 className="mb-2 text-xl font-semibold text-red-900 dark:text-red-100">
                          Invalid Project Data
                        </h3>
                        <p className="mb-6 text-red-600 dark:text-red-400">
                          This project's data structure is incompatible with the viewer.
                          <br />
                          Layout: {currentLayout} | Blocks: {Object.keys(states.blocks).length} ({Object.keys(states.blocks).join(", ")})
                        </p>
                        <Button
                          onClick={() => setCurrentProject(null)}
                          className="bg-red-600 text-white hover:bg-red-700 dark:bg-red-600 dark:hover:bg-red-700"
                        >
                          Close Project
                        </Button>
                      </div>
                    </div>
                  </div>
                )}
              </>
            ) : (
              <div className="border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900">
                <div className="p-6 px-12 py-12">
                  <div className="py-16 text-center">
                    <Eye className="mx-auto mb-4 h-16 w-16 text-gray-300 dark:text-gray-600" />
                    <h3 className="mb-2 text-xl font-semibold text-gray-900 dark:text-gray-100">
                      No Project Selected
                    </h3>
                    <p className="mb-6 text-gray-500 dark:text-gray-400">Choose a project to view its content</p>
                    <Button
                      onClick={() => setOpenDialogOpen(true)}
                      disabled={!isDbInitialized}
                      className="bg-blue-600 text-white hover:bg-blue-700 dark:bg-blue-600 dark:hover:bg-blue-700"
                    >
                      Open Project
                    </Button>
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
      <ExitConfirmDialog
        open={exitDialogOpen}
        onOpenChange={setExitDialogOpen}
        onConfirm={() => {
          if (nextUrl) {
            router.push(nextUrl)
          }
        }}
        itemName={currentProject?.name}
        itemType="project"
      />
    </>
  )
}
