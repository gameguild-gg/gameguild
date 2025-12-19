"use client"

import { useState, useEffect, useRef } from "react"
import { toast } from "sonner"
import { Eye, Home, Blocks, Menu, X } from "lucide-react"
import { Button } from "@/components/ui/button"
import { OpenProjectDialogPreview } from "@/components/editor/extras/preview/open-project-dialog-preview"
import { EnhancedStorageAdapter } from "@/lib/storage/editor/enhanced-storage-adapter"
import Link from "next/link"
import { PreviewRenderer } from "@/components/editor/extras/preview/preview-renderer"
import { PreviewTableOfContents } from "@/components/editor/extras/preview/preview-table-of-contents"
import { ProjectSidebarList } from "@/components/editor/extras/preview/project-sidebar-list-improved"
import { useRouter } from "next/navigation"
import { ExitConfirmDialog } from "@/components/editor/extras/dialogs/exit-confirm-dialog"

interface ProjectData {
  id: string
  name: string
  data: string
  tags: string[]
  size: number
  createdAt: string
  updatedAt: string
}

interface SerializedEditorState {
  root: {
    children: any[]
  }
}

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
        const checkSelectedProject = async () => {
          try {
            // First, check for project ID in URL hash
            const hash = window.location.hash.replace('#', '')
            if (hash) {
              try {
                const projectData = await dbStorage.current.load(hash)
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
        
        await checkSelectedProject()
        
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

  const getSerializedState = (): SerializedEditorState | null => {
    if (!currentProject) return null

    try {
      return JSON.parse(currentProject.data)
    } catch (error) {
      console.error("Failed to parse project data:", error)
      return null
    }
  }

  const serializedState = getSerializedState()

  return (
    <>
      <div className="min-h-screen bg-gray-50 dark:bg-gray-950">
        <div className="container mx-auto py-10">
          <div
            className={`mx-auto space-y-4 px-4 sm:px-6 lg:px-8 ${
              currentProject && serializedState ? "max-w-full" : "max-w-4xl"
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
                  {currentProject && serializedState && (
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

            {currentProject && serializedState ? (
              <div className="flex flex-col lg:flex-row lg:gap-8">
                {/* Desktop Sidebar */}
                <aside className="hidden lg:block lg:w-1/3 xl:w-1/4">
                  <ProjectSidebarList
                    storageAdapter={storageAdapter}
                    availableTags={availableTags}
                    currentProject={currentProject}
                    onProjectSelect={handleProjectLoad}
                    isDbInitialized={isDbInitialized}
                    isSticky={true}
                  />
                </aside>

                {/* Mobile Sidebar Overlay */}
                {sidebarOpen && (
                  <div className="fixed inset-0 z-50 flex lg:hidden">
                    <div className="fixed inset-0 bg-black bg-opacity-50" onClick={() => setSidebarOpen(false)} />
                    <div className="relative h-full w-80 bg-white shadow-xl dark:bg-gray-900">
                      <div className="flex items-center justify-between border-b border-gray-200 p-4 dark:border-gray-700">
                        <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Documents</h3>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => setSidebarOpen(false)}
                          className="h-8 w-8 p-0"
                        >
                          <X className="h-4 w-4" />
                        </Button>
                      </div>
                      <div className="h-full">
                        <ProjectSidebarList
                          storageAdapter={storageAdapter}
                          availableTags={availableTags}
                          currentProject={currentProject}
                          onProjectSelect={(project) => {
                            handleProjectLoad(project)
                            setSidebarOpen(false)
                          }}
                          isDbInitialized={isDbInitialized}
                        />
                      </div>
                    </div>
                  </div>
                )}

                <main className="flex-1 lg:w-3/4 xl:w-3/4">
                  <div className="grid grid-cols-1 gap-4 xl:grid-cols-7">
                    <div className="xl:col-span-5">
                      <div className="border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900">
                        <div className="p-6 sm:p-8 md:p-12">
                          <PreviewRenderer serializedState={serializedState as any} />
                        </div>
                      </div>
                    </div>

                    <aside className="xl:col-span-2">
                      <div className="sticky top-24">
                        <PreviewTableOfContents serializedState={serializedState} />
                      </div>
                    </aside>
                  </div>
                </main>
              </div>
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
