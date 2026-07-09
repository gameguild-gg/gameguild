"use client"

import { Button } from "@/components/ui/button"
import { Eye, Home, Blocks, Menu } from "lucide-react"
import Link from "next/link"
import { OpenProjectDialogPreview } from "@/components/block-content-editor/extras/preview/open-project-dialog-preview"
import { useViewer } from "./viewer-provider"

export function ViewerToolbar() {
  const { viewer, toolbarConfig: tc, ui } = useViewer()

  return (
    <div className="border border-gray-200 bg-white shadow-sm dark:border-gray-700 dark:bg-gray-900">
      {/* Title Bar */}
      <div className="flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
        <div className="flex items-center gap-3">
          <div className="p-2 bg-green-50 dark:bg-green-900/30">
            <Eye className="h-5 w-5 text-green-600 dark:text-green-400" />
          </div>
          <div>
            <h1 className="text-xl font-semibold text-gray-900 dark:text-gray-100">Content Viewer</h1>
            <p className="text-sm text-gray-600 dark:text-gray-400">View your documents as readers see them</p>
          </div>

          {tc.showProjectTitle !== false && viewer.currentProject && (
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
          {tc.showNavHome !== false && (
            <Link href="/block-content-editor" passHref>
              <Button
                onClick={(e: any) => ui.handleLinkNavigation(e, "/block-content-editor")}
                variant="ghost"
                size="sm"
                className="gap-2 hover:bg-gray-100 dark:hover:bg-gray-800"
              >
                <Home className="h-4 w-4" />
                Home
              </Button>
            </Link>
          )}
          {tc.showNavStudio !== false && (
            <Link href="/block-content-editor/studio" passHref>
              <Button
                onClick={(e: any) => ui.handleLinkNavigation(e, "/block-content-editor/studio")}
                variant="ghost"
                size="sm"
                className="gap-2 hover:bg-gray-100 dark:hover:bg-gray-800"
              >
                <Blocks className="h-4 w-4" />
                Studio
              </Button>
            </Link>
          )}
        </div>
      </div>

      {/* Action Bar */}
      <div className="flex items-center justify-between gap-4 p-4 bg-white dark:bg-gray-900">
        <div className="flex items-center gap-3">
          {viewer.currentProject && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => ui.setSidebarOpen(true)}
              className="gap-2 border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800 bg-transparent lg:hidden"
            >
              <Menu className="h-4 w-4" />
              Documents
            </Button>
          )}

          {tc.showOpen !== false && (
            <OpenProjectDialogPreview
              open={ui.openDialogOpen}
              onOpenChange={ui.setOpenDialogOpen}
              isDbInitialized={viewer.isDbInitialized}
              storageAdapter={viewer.storageAdapter}
              availableTags={viewer.availableTags}
              onProjectLoad={viewer.loadProject}
            />
          )}
        </div>

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
              Updated {new Date(viewer.currentProject.metadata.updatedAt).toLocaleDateString()}
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
