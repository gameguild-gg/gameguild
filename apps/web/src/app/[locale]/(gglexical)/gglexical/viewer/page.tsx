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

  const dbStorage = useRef<EnhancedStorageAdapter>(new EnhancedStorageAdapter())

  useEffect(() => {
    const initDB = async () => {
      try {
        await dbStorage.current.init()
        setIsDbInitialized(true)
        await loadAvailableTags()
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
    ): Promise<ProjectData[]> => {
      if (!isDbInitialized) {
        return []
      }

      try {
        return await dbStorage.current.searchProjects(searchTerm, tags, filterMode)
      } catch (error) {
        console.error("Failed to search projects:", error)
        return []
      }
    },
  }

  const handleProjectLoad = (projectData: ProjectData) => {
    setCurrentProject(projectData)
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
    <div className="min-h-screen bg-gray-50 dark:bg-gray-950">
      <div className="container mx-auto py-10">
        <div
          className={`mx-auto space-y-8 px-4 sm:px-6 lg:px-8 ${
            currentProject && serializedState ? "max-w-full" : "max-w-4xl"
          }`}
        >
          <div className="space-y-4 text-center">
            <div className="inline-flex items-center gap-2 rounded-full bg-green-50 px-3 py-1 text-sm font-medium text-green-700 dark:bg-green-900/50 dark:text-green-300">
              <Eye className="w-4" />
              Viewer
            </div>
            <h1 className="text-4xl font-bold tracking-tight text-gray-900 dark:text-gray-100">Content View</h1>
            <p className="mx-auto max-w-2xl text-lg text-gray-600 dark:text-gray-300">
              View your documents as they would appear to readers
            </p>
          </div>

          <div className="rounded-xl border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900">
            <div className="space-y-4 p-6">
              <div className="flex flex-wrap items-center justify-between gap-4">
                <div className="flex flex-wrap items-center gap-2">
                  {currentProject && serializedState && (
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setSidebarOpen(true)}
                      className="gap-2 border-gray-200 bg-white hover:bg-gray-50 dark:border-gray-700 dark:bg-gray-800 dark:hover:bg-gray-700 lg:hidden"
                    >
                      <Menu className="h-4 w-4" />
                      Documents
                    </Button>
                  )}

                  <Link href="/gglexical">
                    <Button
                      variant="outline"
                      size="sm"
                      className="gap-2 border-gray-200 bg-white hover:bg-gray-50 dark:border-gray-700 dark:bg-gray-800 dark:hover:bg-gray-700"
                    >
                      <Home className="h-4 w-4" />
                      Home
                    </Button>
                  </Link>

                  <Link href="/gglexical/studio">
                    <Button
                      variant="outline"
                      size="sm"
                      className="gap-2 border-gray-200 bg-white hover:bg-gray-50 dark:border-gray-700 dark:bg-gray-800 dark:hover:bg-gray-700"
                    >
                      <Blocks className="w-4" />
                      Studio
                    </Button>
                  </Link>
                </div>

                <div className="flex items-center gap-3">
                  <OpenProjectDialogPreview
                    open={openDialogOpen}
                    onOpenChange={setOpenDialogOpen}
                    isDbInitialized={isDbInitialized}
                    storageAdapter={storageAdapter}
                    availableTags={availableTags}
                    onProjectLoad={handleProjectLoad}
                  />
                </div>
              </div>

              {currentProject && (
                <div className="flex flex-wrap items-center justify-between gap-4 border-t border-gray-200 pt-4 dark:border-gray-700">
                  <div className="flex flex-wrap items-center gap-4">
                    <div className="flex items-center gap-2">
                      <span className="text-sm text-gray-500 dark:text-gray-400">Currently viewing:</span>
                      <span className="font-medium text-gray-900 dark:text-gray-100">{currentProject.name}</span>
                    </div>
                    {currentProject.tags && currentProject.tags.length > 0 && (
                      <div className="flex flex-wrap gap-1">
                        {currentProject.tags.slice(0, 3).map((tag) => (
                          <span
                            key={tag}
                            className="inline-flex items-center rounded bg-blue-100 px-2 py-1 text-xs text-blue-800 dark:bg-blue-900/50 dark:text-blue-300"
                          >
                            {tag}
                          </span>
                        ))}
                        {currentProject.tags.length > 3 && (
                          <span className="text-xs text-gray-500 dark:text-gray-400">
                            +{currentProject.tags.length - 3}
                          </span>
                        )}
                      </div>
                    )}
                  </div>
                  <div className="text-xs text-gray-500 dark:text-gray-400">
                    <span>Updated {new Date(currentProject.updatedAt).toLocaleDateString()}</span>
                  </div>
                </div>
              )}
            </div>
          </div>

          {currentProject && serializedState ? (
            <div className="flex flex-col lg:flex-row lg:gap-6">
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
                <div className="grid grid-cols-1 gap-8 xl:grid-cols-7">
                  <div className="xl:col-span-5">
                    <div className="rounded-xl border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900">
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
            <div className="rounded-xl border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900">
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

          <div className="grid grid-cols-1 gap-4 pt-4 md:grid-cols-2">
            <div className="rounded-lg border border-green-200 bg-green-50 p-4 dark:border-green-800 dark:bg-green-900/30">
              <h3 className="mb-2 font-semibold text-green-900 dark:text-green-100">Preview Mode</h3>
              <p className="text-sm text-green-700 dark:text-green-300">
                View your content exactly as readers will see it
              </p>
            </div>
            <div className="rounded-lg border border-blue-200 bg-blue-50 p-4 dark:border-blue-800 dark:bg-blue-900/30">
              <h3 className="mb-2 font-semibold text-blue-900 dark:text-blue-100">Read-Only View</h3>
              <p className="text-sm text-blue-700 dark:text-blue-300">
                Content is displayed in read-only mode for optimal viewing
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
