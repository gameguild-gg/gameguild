"use client"

import {
  Menubar,
  MenubarContent,
  MenubarItem,
  MenubarMenu,
  MenubarSeparator,
  MenubarTrigger,
} from "@/components/ui/menubar"
import { Eye, Menu, Sun, Moon } from "lucide-react"
import { OpenProjectDialogPreview } from "@/components/block-content-editor/extras/preview/open-project-dialog-preview"
import { useViewer } from "./viewer-provider"
import { useTheme } from "next-themes"
import { useEffect, useState } from "react"
import { Button } from "@/components/ui/button"

export function ViewerToolbar() {
  const { viewer, toolbarConfig: tc, ui } = useViewer()
  const { theme, setTheme } = useTheme()
  const [mounted, setMounted] = useState(false)

  useEffect(() => {
    setMounted(true)
  }, [])

  const toggleTheme = () => {
    setTheme(theme === "dark" ? "light" : "dark")
  }

  const isDark = theme === "dark"

  return (
    <>
      {/* Top Navigation Bar / Panels */}
      <div className="fixed top-3 z-50 flex items-center 2xl:justify-between justify-center 2xl:pointer-events-none pointer-events-auto 2xl:bg-transparent bg-white/90 2xl:dark:bg-transparent dark:bg-gray-900/90 2xl:backdrop-blur-none backdrop-blur-md 2xl:border-none border border-gray-200 dark:border-gray-800 2xl:shadow-none shadow-lg 2xl:rounded-none rounded-full 2xl:p-0 px-3 h-11 2xl:h-auto 2xl:left-4 2xl:right-4 left-1/2 -translate-x-1/2 2xl:translate-x-0 max-w-[95vw] w-max 2xl:w-auto gap-2">
        {/* Left Menu Panel */}
        <div className="flex items-center gap-2 pointer-events-auto bg-transparent 2xl:bg-white/90 2xl:dark:bg-gray-900/90 2xl:backdrop-blur-md border-none 2xl:border 2xl:border-gray-200 2xl:dark:border-gray-800 shadow-none 2xl:shadow-lg rounded-none 2xl:rounded-full p-0 2xl:px-3 h-auto 2xl:h-11">
          {/* Logo */}
          <div className="p-1 bg-green-50 dark:bg-green-900/30 rounded-full flex items-center justify-center">
            <Eye className="h-3.5 w-3.5 text-green-600 dark:text-green-400" />
          </div>

          <Menubar className="border-none shadow-none bg-transparent h-8 p-0">
            <MenubarMenu>
              <MenubarTrigger className="cursor-pointer text-xs font-semibold py-1 px-2.5 rounded-full hover:bg-gray-100 dark:hover:bg-gray-800">
                File
              </MenubarTrigger>
              <MenubarContent>
                {tc.showOpen !== false && (
                  <MenubarItem onClick={() => ui.setOpenDialogOpen(true)}>
                    Open...
                  </MenubarItem>
                )}
              </MenubarContent>
            </MenubarMenu>

            <MenubarMenu>
              <MenubarTrigger className="cursor-pointer text-xs font-semibold py-1 px-2.5 rounded-full hover:bg-gray-100 dark:hover:bg-gray-800">
                View
              </MenubarTrigger>
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

          <div className="w-px h-4 bg-gray-200 dark:bg-gray-800 mx-1"></div>

          {/* Project Title inline */}
          <div className="flex items-center gap-2 text-xs max-w-[120px] sm:max-w-[200px] overflow-hidden">
            {tc.showProjectTitle !== false && viewer.currentProject ? (
              <div className="min-w-0 flex-1 truncate">
                <span className="font-semibold text-gray-800 dark:text-gray-200">
                  {viewer.currentProject.name}
                </span>
              </div>
            ) : (
              <span className="text-gray-500 dark:text-gray-400 italic text-[11px]">No project open</span>
            )}
          </div>
        </div>

        {/* Mid Separator (Visible only on mobile in combined panel) */}
        <div className="2xl:hidden w-px h-5 bg-gray-200 dark:bg-gray-800 mx-1"></div>

        {/* Right Menu Panel */}
        <div className="flex items-center gap-3 pointer-events-auto bg-transparent 2xl:bg-white/90 2xl:dark:bg-gray-900/90 2xl:backdrop-blur-md border-none 2xl:border 2xl:border-gray-200 2xl:dark:border-gray-800 shadow-none 2xl:shadow-lg rounded-none 2xl:rounded-full p-0 2xl:px-3 h-auto 2xl:h-11">
          <div className="flex items-center gap-2.5 text-[11px]">
            {viewer.currentProject && (
              <div className="hidden sm:flex items-center gap-2.5">
                {viewer.currentProject.tags && viewer.currentProject.tags.length > 0 && (
                  <div className="flex items-center gap-1">
                    {viewer.currentProject.tags.slice(0, 2).map((tag) => (
                      <span
                        key={tag}
                        className="inline-flex items-center bg-blue-100 dark:bg-blue-900/50 px-1.5 py-0.5 rounded text-[10px] font-semibold text-blue-800 dark:text-blue-300"
                      >
                        {tag}
                      </span>
                    ))}
                    {viewer.currentProject.tags.length > 2 && (
                      <span className="text-[9px] text-gray-500 dark:text-gray-400">
                        +{viewer.currentProject.tags.length - 2}
                      </span>
                    )}
                  </div>
                )}
                <div className="text-[10px] text-gray-500 dark:text-gray-400">
                  Updated {new Date(viewer.currentProject.metadata.updatedAt).toLocaleDateString()}
                </div>
              </div>
            )}
          </div>

          <div className="w-px h-4 bg-gray-200 dark:bg-gray-800"></div>

          {mounted && (
            <Button
              variant="ghost"
              size="sm"
              onClick={toggleTheme}
              className="rounded-full w-8 h-8 p-0 flex items-center justify-center hover:bg-gray-100 dark:hover:bg-gray-800"
              aria-label="Toggle theme"
            >
              {isDark ? <Sun className="w-4 h-4 text-yellow-500" /> : <Moon className="w-4 h-4 text-gray-600" />}
            </Button>
          )}
          {!mounted && <div className="w-8 h-8 rounded-full bg-gray-100 dark:bg-gray-800 animate-pulse" />}
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
    </>
  )
}
