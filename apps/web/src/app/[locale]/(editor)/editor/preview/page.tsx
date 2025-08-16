"use client"

import { useState, useEffect, useRef } from "react"
import { toast } from "sonner"
import { Eye, Home } from "lucide-react"
import { Button } from "@/components/ui/button"
import { OpenProjectDialogPreview } from "@/components/editor/ui/preview/open-project-dialog-preview"
import { EnhancedStorageAdapter } from "@/lib/storage/editor/enhanced-storage-adapter"
import Link from "next/link"
import { PreviewRenderer } from "@/components/editor/ui/preview-renderer"
import { PreviewTableOfContents } from "@/components/editor/ui/preview-table-of-contents"
import { useTheme } from "@/lib/context/theme-context"

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

  const { theme } = useTheme()

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

  const formatStorageSize = (sizeInKB: number): string => {
    const sizeInMB = sizeInKB / 1024
    const sizeInGB = sizeInMB / 1024

    if (sizeInGB >= 1) {
      return `${sizeInGB.toFixed(1)}GB`
    } else if (sizeInMB >= 1) {
      return `${sizeInMB.toFixed(1)}MB`
    } else {
      return `${sizeInKB.toFixed(1)}KB`
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
        <div className={`mx-auto space-y-8 ${currentProject && serializedState ? "max-w-7xl" : "max-w-4xl"}`}>
          <div className="text-center space-y-4">
            <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-green-50 dark:bg-green-900/50 text-green-700 dark:text-green-300 text-sm font-medium">
              <Eye className="w-4 h-4" />
              Preview Mode
            </div>
            <h1 className="text-4xl font-bold tracking-tight text-gray-900 dark:text-gray-100">Content Preview</h1>
            <p className="text-lg text-gray-600 dark:text-gray-300 max-w-2xl mx-auto">
              View your projects as they would appear to readers
            </p>
          </div>

          <div className="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 shadow-sm">
            <div className="p-6 space-y-4">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-4">
                  <Link href="/editor">
                    <Button
                      variant="outline"
                      size="sm"
                      className="gap-2 bg-white dark:bg-gray-800 border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-700"
                    >
                      <Home className="w-4 h-4" />
                      Home
                    </Button>
                  </Link>

                  <Link href="/editor/lexical">
                    <Button
                      variant="outline"
                      size="sm"
                      className="gap-2 bg-white dark:bg-gray-800 border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-700"
                    >
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                          d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
                        />
                      </svg>
                      Editor
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
                    formatStorageSize={formatStorageSize}
                  />
                </div>
              </div>

              {currentProject && (
                <div className="flex items-center justify-between pt-4 border-t border-gray-200 dark:border-gray-700">
                  <div className="flex items-center gap-4">
                    <div className="flex items-center gap-2">
                      <span className="text-sm text-gray-500 dark:text-gray-400">Currently viewing:</span>
                      <span className="text-sm font-medium text-gray-900 dark:text-gray-100">
                        {currentProject.name}
                      </span>
                    </div>
                    {currentProject.tags && currentProject.tags.length > 0 && (
                      <div className="flex gap-1">
                        {currentProject.tags.slice(0, 3).map((tag) => (
                          <span
                            key={tag}
                            className="inline-flex items-center px-2 py-1 rounded text-xs bg-blue-100 dark:bg-blue-900/50 text-blue-800 dark:text-blue-300"
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
                  <div className="flex items-center gap-2 text-xs text-gray-500 dark:text-gray-400">
                    <span>Updated {new Date(currentProject.updatedAt).toLocaleDateString()}</span>
                  </div>
                </div>
              )}
            </div>
          </div>

          {currentProject && serializedState ? (
            <div className="grid grid-cols-1 lg:grid-cols-4 gap-8">
              <div className="lg:col-span-3">
                <div className="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 shadow-sm">
                  <div className="p-6 px-12 py-12">
                    <PreviewRenderer
                      serializedState={serializedState}
                      showHeader={true}
                      projectName={currentProject.name}
                    />
                  </div>
                </div>
              </div>

              <div className="lg:col-span-1">
                <div className="sticky top-24">
                  <PreviewTableOfContents serializedState={serializedState} />
                </div>
              </div>
            </div>
          ) : (
            <div className="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 shadow-sm">
              <div className="p-6 px-12 py-12">
                <div className="text-center py-16">
                  <Eye className="w-16 h-16 text-gray-300 dark:text-gray-600 mx-auto mb-4" />
                  <h3 className="text-xl font-semibold text-gray-900 dark:text-gray-100 mb-2">No Project Selected</h3>
                  <p className="text-gray-500 dark:text-gray-400 mb-6">Choose a project to preview its content</p>
                  <Button
                    onClick={() => setOpenDialogOpen(true)}
                    disabled={!isDbInitialized}
                    className="bg-blue-600 hover:bg-blue-700 dark:bg-blue-600 dark:hover:bg-blue-700 text-white"
                  >
                    Open Project
                  </Button>
                </div>
              </div>
            </div>
          )}

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 pt-4">
            <div className="p-4 bg-green-50 dark:bg-green-900/30 rounded-lg border border-green-200 dark:border-green-800">
              <h3 className="font-semibold text-green-900 dark:text-green-100 mb-2">Preview Mode</h3>
              <p className="text-sm text-green-700 dark:text-green-300">
                View your content exactly as readers will see it
              </p>
            </div>
            <div className="p-4 bg-blue-50 dark:bg-blue-900/30 rounded-lg border border-blue-200 dark:border-blue-800">
              <h3 className="font-semibold text-blue-900 dark:text-blue-100 mb-2">Read-Only View</h3>
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
