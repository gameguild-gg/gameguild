"use client"

import { Button } from "@/components/ui/button"
import { Eye, Menu } from "lucide-react"
import { PreviewRendererType1 } from "@/components/editor/extras/preview/preview-renderer-type1"
import { PreviewRendererType2 } from "@/components/editor/extras/preview/preview-renderer-type2"
import { PreviewRendererSlideshowContinuous } from "@/components/editor/extras/preview/preview-renderer-slideshow-continuous"
import { PreviewRendererSlideshowSlide } from "@/components/editor/extras/preview/preview-renderer-slideshow-slide"
import { BlockArrayViewer } from "@/components/editor/engines/blocks/block-array-viewer"
import { useViewer } from "./viewer-provider"

export function ViewerField() {
  const { viewer, ui } = useViewer()

  const { layout: currentLayout, states, hasSlides, slideshowData, previewMode, isBlocksEngine, blocksArray } = viewer.layoutInfo

  if (viewer.currentProject && isBlocksEngine) {
    return (
      <div className="border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900 p-6">
        <BlockArrayViewer blocks={blocksArray || []} />
      </div>
    )
  }

  if (viewer.currentProject && (Object.keys(states.blocks).length > 0 || hasSlides)) {
    return (
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
            sidebarOpen={ui.sidebarOpen}
            setSidebarOpen={ui.setSidebarOpen}
          />
        ) : currentLayout === "multiple" && Object.keys(states.blocks).length >= 1 ? (
          <PreviewRendererType2
            blockStates={states.blocks as Record<string, any>}
            projectId={viewer.currentProject.id}
            storageAdapter={viewer.storageAdapter}
            preferences={viewer.currentProject.preferences}
          />
        ) : (
          <div className="border border-red-200 bg-red-50 shadow-sm dark:border-red-700 dark:bg-red-900/20">
            <div className="p-6 px-12 py-12">
              <div className="py-16 text-center">
                <Eye className="mx-auto mb-4 h-16 w-16 text-red-300 dark:text-red-600" />
                <h3 className="mb-2 text-xl font-semibold text-red-900 dark:text-red-100">
                  Invalid Project Data
                </h3>
                <p className="mb-6 text-red-600 dark:text-red-400">
                  This project&apos;s data structure is incompatible with the viewer.
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
    )
  }

  return (
    <div className="border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900">
      <div className="p-6 px-12 py-12">
        <div className="py-16 text-center">
          <Eye className="mx-auto mb-4 h-16 w-16 text-gray-300 dark:text-gray-600" />
          <h3 className="mb-2 text-xl font-semibold text-gray-900 dark:text-gray-100">
            No Project Selected
          </h3>
          <p className="mb-6 text-gray-500 dark:text-gray-400">Choose a project to view its content</p>
          <Button
            onClick={() => ui.setOpenDialogOpen(true)}
            disabled={!viewer.isDbInitialized}
            className="bg-blue-600 text-white hover:bg-blue-700 dark:bg-blue-600 dark:hover:bg-blue-700"
          >
            Open Project
          </Button>
        </div>
      </div>
    </div>
  )
}
