"use client"

import { useState } from "react"
import { Eye, Home, Blocks, Menu } from "lucide-react"
import { Button } from "@/components/ui/button"
import { OpenProjectDialogPreview } from "@/components/editor/extras/preview/open-project-dialog-preview"
import Link from "next/link"
import { PreviewRendererType1 } from "@/components/editor/extras/preview/preview-renderer-type1"
import { PreviewRendererType2 } from "@/components/editor/extras/preview/preview-renderer-type2"
import { PreviewRendererSlideshowContinuous } from "@/components/editor/extras/preview/preview-renderer-slideshow-continuous"
import { PreviewRendererSlideshowSlide } from "@/components/editor/extras/preview/preview-renderer-slideshow-slide"
import { useRouter } from "next/navigation"
import { ExitConfirmDialog } from "@/components/editor/extras/dialogs/exit-confirm-dialog"
import { BlockArrayViewer } from "@/components/editor/extras/editor/block-array-viewer"
import { useViewerStorage } from "@/components/editor/hooks/useViewerStorage"


export default function PreviewPage() {
  const router = useRouter()
  const viewer = useViewerStorage()

  // ── UI-only state ──
  const [openDialogOpen, setOpenDialogOpen] = useState(false)
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const [exitDialogOpen, setExitDialogOpen] = useState(false)
  const [nextUrl, setNextUrl] = useState<string>("")

  const { layout: currentLayout, states, hasSlides, slideshowData, previewMode, isBlocksEngine, blocksArray } = viewer.layoutInfo

  // ── Navigation helpers ──
  const handleLinkNavigation = (event: React.MouseEvent<HTMLAnchorElement>, url: string) => {
    if (event.ctrlKey || event.metaKey || event.button === 1) return
    event.preventDefault()
    if (viewer.currentProject) {
      setNextUrl(url)
      setExitDialogOpen(true)
    } else {
      router.push(url)
    }
  }

  return (
    <>
      <div className="min-h-screen bg-gray-50 dark:bg-gray-950">
        <div className="container mx-auto py-10">
          <div
            className={`mx-auto space-y-4 px-4 sm:px-6 lg:px-8 ${
              viewer.currentProject && Object.keys(states.blocks).length > 0 ? "max-w-full" : "max-w-4xl"
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
                  {viewer.currentProject && (
                    <div className="ml-6 flex items-center gap-4 pl-6 border-l border-gray-300 dark:border-gray-600">
                      <div className="flex items-center gap-2 text-sm">
                        <span className="text-gray-600 dark:text-gray-400">Viewing:</span>
                        <span className="font-medium text-gray-800 dark:text-gray-200 bg-gray-100 dark:bg-gray-800 px-3 py-1">
                          {viewer.currentProject.name}
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
                  {viewer.currentProject && Object.keys(states.blocks).length > 0 && currentLayout === "single" && (
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
                    isDbInitialized={viewer.isDbInitialized}
                    storageAdapter={viewer.storageAdapter}
                    availableTags={viewer.availableTags}
                    onProjectLoad={viewer.loadProject}
                  />
                </div>

                {/* Status/Info Display */}
                {viewer.currentProject && (
                  <div className="flex items-center gap-4">
                    {viewer.currentProject.tags && viewer.currentProject.tags.length > 0 && (
                      <div className="flex items-center gap-2">
                        {viewer.currentProject.tags.slice(0, 2).map((tag) => (
                          <span
                            key={tag}
                            className="inline-flex items-center bg-blue-100 dark:bg-blue-900/50 px-2 py-1 text-xs font-medium text-blue-800 dark:text-blue-300"
                          >
                            {tag}
                          </span>
                        ))}
                        {viewer.currentProject.tags.length > 2 && (
                          <span className="text-xs text-gray-500 dark:text-gray-400">
                            +{viewer.currentProject.tags.length - 2}
                          </span>
                        )}
                      </div>
                    )}
                    <div className="text-xs text-gray-500 dark:text-gray-400 bg-gray-50 dark:bg-gray-800 px-3 py-1.5">
                      Updated {new Date(viewer.currentProject.updatedAt).toLocaleDateString()}
                    </div>
                  </div>
                )}
              </div>
            </div>

            {viewer.currentProject && isBlocksEngine ? (
              <div className="border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900 p-6">
                <BlockArrayViewer blocks={blocksArray || []} />
              </div>
            ) : viewer.currentProject && (Object.keys(states.blocks).length > 0 || hasSlides) ? (
              <>
                {hasSlides && slideshowData ? (
                  previewMode === "slide" ? (
                    <PreviewRendererSlideshowSlide
                      structure={slideshowData}
                      projectId={viewer.currentProject.id}
                      projectName={viewer.currentProject.name}
                      deps={(viewer.currentProject as any).deps || []}
                      resolvedProjects={viewer.resolvedProjects}
                      storageAdapter={viewer.storageAdapter}
                      preferences={viewer.currentProject.preferences}
                    />
                  ) : (
                    <PreviewRendererSlideshowContinuous
                      structure={slideshowData}
                      projectId={viewer.currentProject.id}
                      projectName={viewer.currentProject.name}
                      deps={(viewer.currentProject as any).deps || []}
                      resolvedProjects={viewer.resolvedProjects}
                      storageAdapter={viewer.storageAdapter}
                      preferences={viewer.currentProject.preferences}
                    />
                  )
                ) : currentLayout === "single" && Object.keys(states.blocks).length > 0 ? (
                  <PreviewRendererType1
                    serializedState={Object.values(states.blocks)[0] as any}
                    currentProject={viewer.currentProject}
                    storageAdapter={viewer.storageAdapter}
                    availableTags={viewer.availableTags}
                    isDbInitialized={viewer.isDbInitialized}
                    onProjectSelect={viewer.loadProject}
                    sidebarOpen={sidebarOpen}
                    setSidebarOpen={setSidebarOpen}
                  />
                ) : currentLayout === "multiple" && Object.keys(states.blocks).length >= 1 ? (
                  <PreviewRendererType2 blockStates={states.blocks as Record<string, any>} projectId={viewer.currentProject.id} storageAdapter={viewer.storageAdapter} preferences={viewer.currentProject.preferences} />
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
                          onClick={() => viewer.setCurrentProject(null)}
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
                      disabled={!viewer.isDbInitialized}
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
        itemName={viewer.currentProject?.name}
        itemType="project"
      />
    </>
  )
}
