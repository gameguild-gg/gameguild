"use client"

import { Button } from "@/components/ui/button"
import { Eye, Menu } from "lucide-react"
import { PreviewRendererType1 } from "@/components/editor/extras/preview/preview-renderer-type1"
import { BlockArrayViewer } from "@/components/editor/engines/blocks/block-array-viewer"
import { useViewer } from "./viewer-provider"

export function ViewerField() {
  const { viewer, ui } = useViewer()

  const { states, isBlocksEngine, blocksArray } = viewer.layoutInfo

  if (viewer.currentProject && isBlocksEngine) {
    return (
      <div className="border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900 p-6">
        <BlockArrayViewer blocks={blocksArray || []} />
      </div>
    )
  }

  if (viewer.currentProject && Object.keys(states.blocks).length > 0) {
    return (
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
