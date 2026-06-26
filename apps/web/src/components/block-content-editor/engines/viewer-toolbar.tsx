"use client"

import {
  Menubar,
  MenubarContent,
  MenubarItem,
  MenubarMenu,
  MenubarSeparator,
  MenubarTrigger,
} from "@/components/ui/menubar"
import { Eye, Menu } from "lucide-react"
import { OpenProjectDialogPreview } from "@/components/block-content-editor/extras/preview/open-project-dialog-preview"
import { useViewer } from "./viewer-provider"

export function ViewerToolbar() {
  const { viewer, toolbarConfig: tc, ui } = useViewer()

  return (
    <div className="flex flex-col w-full border-b border-gray-200 bg-white dark:border-gray-800 dark:bg-gray-950">
      <div className="flex items-center justify-between px-2 py-1">
        <div className="flex items-center gap-2">
          {/* Logo */}
          <div className="p-1 bg-green-50 dark:bg-green-900/30 rounded">
            <Eye className="h-4 w-4 text-green-600 dark:text-green-400" />
          </div>

          <Menubar className="border-none shadow-none bg-transparent h-8">
            <MenubarMenu>
              <MenubarTrigger className="cursor-pointer">File</MenubarTrigger>
              <MenubarContent>
                {tc.showOpen !== false && (
                  <MenubarItem onClick={() => ui.setOpenDialogOpen(true)}>
                    Open...
                  </MenubarItem>
                )}
              </MenubarContent>
            </MenubarMenu>

            <MenubarMenu>
              <MenubarTrigger className="cursor-pointer">View</MenubarTrigger>
              <MenubarContent>
                {tc.showNavHome !== false && (
                  <MenubarItem onClick={(e) => ui.handleLinkNavigation(e, "/block-content-editor")}>
                    Home
                  </MenubarItem>
                )}
                {tc.showNavStudio !== false && (
                  <MenubarItem onClick={(e) => ui.handleLinkNavigation(e, "/block-content-editor/studio")}>
                    Studio
                  </MenubarItem>
                )}
                {viewer.currentProject && (
                  <>
                    <MenubarSeparator />
                    <MenubarItem onClick={() => ui.setSidebarOpen(true)} className="lg:hidden">
                      <Menu className="h-4 w-4 mr-2" /> Documents
                    </MenubarItem>
                  </>
                )}
              </MenubarContent>
            </MenubarMenu>
          </Menubar>

          <div className="h-4 w-px bg-gray-300 dark:bg-gray-700 mx-2"></div>

          {/* Project Title inline */}
          <div className="flex items-center gap-2 text-sm max-w-[200px] sm:max-w-xs overflow-hidden">
             {tc.showProjectTitle !== false && viewer.currentProject ? (
                <div className="min-w-0 flex-1 truncate">
                  <span className="font-medium text-gray-800 dark:text-gray-200">
                    {viewer.currentProject.name}
                  </span>
                </div>
              ) : (
                <span className="text-gray-500 dark:text-gray-400 italic text-xs">No project open</span>
              )}
          </div>
        </div>

        {/* Right side indicators */}
        <div className="flex items-center gap-3 text-xs">
          {viewer.currentProject && (
            <>
              {viewer.currentProject.tags && viewer.currentProject.tags.length > 0 && (
                <div className="flex items-center gap-1">
                  {viewer.currentProject.tags.slice(0, 2).map((tag) => (
                    <span
                      key={tag}
                      className="inline-flex items-center bg-blue-100 dark:bg-blue-900/50 px-1.5 py-0.5 rounded text-[10px] font-medium text-blue-800 dark:text-blue-300"
                    >
                      {tag}
                    </span>
                  ))}
                  {viewer.currentProject.tags.length > 2 && (
                    <span className="text-[10px] text-gray-500 dark:text-gray-400">
                      +{viewer.currentProject.tags.length - 2}
                    </span>
                  )}
                </div>
              )}
              <div className="text-[10px] text-gray-500 dark:text-gray-400">
                Updated {new Date(viewer.currentProject.metadata.updatedAt).toLocaleDateString()}
              </div>
            </>
          )}
        </div>
      </div>

      {/* Modals */}
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
  )
}
