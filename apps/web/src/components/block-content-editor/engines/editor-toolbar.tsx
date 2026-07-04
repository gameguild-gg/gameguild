"use client"

import {
  Menubar,
  MenubarContent,
  MenubarItem,
  MenubarMenu,
  MenubarSeparator,
  MenubarTrigger,
} from "@/components/ui/menubar"
import { Badge } from "@/components/ui/badge"
import { Blocks, History, RotateCcw, Sun, Moon } from "lucide-react"
import { Button } from "@/components/ui/button"
import { SaveAsDialog } from "@/components/block-content-editor/extras/editor/save-as-dialog"
import { OpenProjectDialog } from "@/components/block-content-editor/extras/editor/open-project-dialog"
import { ProjectSizeIndicator } from "@/components/block-content-editor/extras/editor/project-size-indicator"
import { SyncStatusIndicator } from "@/components/block-content-editor/extras/editor/sync-status-indicator"
import { EditableProjectTitle } from "@/components/block-content-editor/extras/editor/editable-project-title"
import { ProjectStorageInfo } from "@/components/block-content-editor/extras/editor/project-storage-info"
import { ProjectTypeIndicator } from "@/components/block-content-editor/extras/editor/project-type-indicator"
import { syncConfig } from "@/components/block-content-editor/lib/sync/editor/sync-config"
import { useEditor } from "./editor-provider"
import { useTheme } from "next-themes"
import { useEffect, useState } from "react"

export function EditorToolbar() {
  const { project, history, preview, toolbarConfig: tc, ui, fieldConfig } = useEditor()
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
      {/* Top Navigation Bar */}
      <div className="fixed top-3 left-1/2 -translate-x-1/2 z-50 flex items-center gap-2 rounded-full border border-gray-200 dark:border-gray-800 bg-white/90 dark:bg-gray-900/90 backdrop-blur-md shadow-lg px-3 h-11 max-w-[95vw] w-max">
        <div className="flex min-w-0 items-center gap-2 pointer-events-auto">
          {/* Logo */}
          <div className="p-1 bg-blue-50 dark:bg-blue-900/30 rounded-full flex items-center justify-center">
            <Blocks className="h-3.5 w-3.5 text-blue-600 dark:text-blue-400" />
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
                {tc.showOpen !== false && (
                  <MenubarItem onClick={() => ui.setCreateDialogOpen(true)}>
                    New Project
                  </MenubarItem>
                )}
                <MenubarSeparator />
                {tc.showSave !== false && (
                  <MenubarItem
                    onClick={ui.handleSave}
                    disabled={!project.isDbInitialized || history.isViewingHistory}
                  >
                    Save
                  </MenubarItem>
                )}
                {tc.showSaveAs !== false && (
                  <MenubarItem onClick={() => ui.setSaveAsDialogOpen(true)}>
                    Save As...
                  </MenubarItem>
                )}
                <MenubarSeparator />
                {tc.showHistory !== false && (
                  <MenubarItem onClick={() => ui.setHistoryDialogOpen(true)} disabled={!project.projectId}>
                    History
                  </MenubarItem>
                )}
              </MenubarContent>
            </MenubarMenu>

            <MenubarMenu>
              <MenubarTrigger className="cursor-pointer text-xs font-semibold py-1 px-2.5 rounded-full hover:bg-gray-100 dark:hover:bg-gray-800">
                View
              </MenubarTrigger>
              <MenubarContent>
                {tc.showPreview !== false && (
                  <MenubarItem onClick={preview.openPreview} disabled={!project.projectId}>
                    Preview
                  </MenubarItem>
                )}
                <MenubarSeparator />
                {tc.showNavHome !== false && (
                  <MenubarItem onClick={(e) => ui.handleLinkNavigation(e, "/block-content-editor")}>
                    Home
                  </MenubarItem>
                )}
                {tc.showNavViewer !== false && (
                  <MenubarItem onClick={(e) => ui.handleLinkNavigation(e, "/block-content-editor/viewer")}>
                    Viewer
                  </MenubarItem>
                )}
              </MenubarContent>
            </MenubarMenu>

            <MenubarMenu>
              <MenubarTrigger className="cursor-pointer text-xs font-semibold py-1 px-2.5 rounded-full hover:bg-gray-100 dark:hover:bg-gray-800">
                Settings
              </MenubarTrigger>
              <MenubarContent>
                {tc.showAutoSave !== false && (
                  <MenubarItem onClick={(e) => {
                    e.preventDefault();
                    if (project.isDbInitialized) project.setAutoSaveEnabled(prev => !prev);
                  }}>
                    Auto Save {project.autoSaveEnabled ? "✓" : ""}
                  </MenubarItem>
                )}
              </MenubarContent>
            </MenubarMenu>
          </Menubar>

          <div className="w-px h-4 bg-gray-200 dark:bg-gray-800 mx-1"></div>

          {/* Project Title inline */}
          <div className="flex items-center gap-2 text-xs max-w-[120px] sm:max-w-[200px] overflow-hidden">
            {project.projectId && tc.showProjectTitle !== false ? (
              <div className="min-w-0 flex-1 truncate">
                <EditableProjectTitle
                  projectName={project.projectName}
                  isEditing={ui.isEditingTitle}
                  editingName={ui.editingProjectName}
                  onEditStart={() => project.titleEdit(ui.setEditingProjectName, ui.setIsEditingTitle)}
                  onEditEnd={() => {
                    ui.setIsEditingTitle(false)
                    ui.setEditingProjectName(project.projectName)
                  }}
                  onNameChange={ui.setEditingProjectName}
                  onSave={() => project.titleSave(ui.editingProjectName, ui.setEditingProjectName, ui.setIsEditingTitle)}
                />
              </div>
            ) : (
              <span className="text-gray-500 dark:text-gray-400 italic text-[11px]">Untitled</span>
            )}
          </div>
        </div>

        <div className="w-px h-5 bg-gray-200 dark:bg-gray-800 mx-1"></div>

        <div className="flex items-center gap-3 pointer-events-auto">
          <div className="flex items-center gap-2.5 text-[11px]">
            {project.projectId && tc.showTypeIndicator !== false && (
              <div className="hidden sm:inline-flex">
                <ProjectTypeIndicator type={project.preferences?.global?.projectType} />
              </div>
            )}
            {project.projectId && tc.showStorageInfo !== false && (
              <div className="hidden sm:inline-flex">
                <ProjectStorageInfo storageType={project.storageType} />
              </div>
            )}
            {tc.showSizeIndicator !== false && (
              <ProjectSizeIndicator
                currentProjectSize={project.projectSize}
                currentProjectAssetsSize={project.assetsSize}
                formatSize={ui.formatSize}
                getSizeIndicatorColor={ui.getSizeIndicatorColor}
                onClick={() => ui.setShowSizeDetails(true)}
              />
            )}
            {tc.showSyncStatus !== false && project.syncStats && (
              <SyncStatusIndicator
                syncStats={project.syncStats}
                isSyncEnabled={syncConfig.isEnabled()}
                onClick={() => ui.setShowSyncStatus(!ui.showSyncStatus)}
              />
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

      {/* Floating History Viewing Banner */}
      {history.isViewingHistory && (
        <div className="fixed top-[64px] left-1/2 -translate-x-1/2 z-50 flex items-center gap-3 px-4 py-1.5 bg-amber-50 dark:bg-amber-950 border border-amber-200 dark:border-amber-800 shadow-lg rounded-full text-xs animate-bounce-subtle">
          <History className="h-3.5 w-3.5 text-amber-600 dark:text-amber-400" />
          <span className="font-medium text-amber-800 dark:text-amber-200">
            Viewing history version
          </span>
          {history.currentViewingSha && (
            <Badge variant="outline" className="font-mono text-[10px] px-1 py-0 h-4 border-amber-300 dark:border-amber-700 text-amber-700 dark:text-amber-300">
              {history.currentViewingSha.substring(0, 7)}
            </Badge>
          )}
          <span className="text-[10px] text-amber-600 dark:text-amber-400">
            (Read-only)
          </span>
          <Button
            variant="ghost"
            size="sm"
            onClick={history.returnToHead}
            className="h-6 px-2 text-[10px] text-amber-900 dark:text-amber-100 hover:bg-amber-100 dark:hover:bg-amber-900/40 rounded-full font-bold flex items-center gap-1"
          >
            <RotateCcw className="h-2.5 w-2.5" />
            Return
          </Button>
        </div>
      )}

      {/* Modals */}
      {tc.showSaveAs !== false && (
        <SaveAsDialog
          open={ui.saveAsDialogOpen}
          onOpenChange={ui.setSaveAsDialogOpen}
          projectName={ui.newProjectName}
          onProjectNameChange={ui.setNewProjectName}
          onSave={(storageOption, tags) => project.saveAs(ui.newProjectName, storageOption, tags)}
          currentProjectSize={project.projectSize}
          getSizeIndicatorColor={ui.getSizeIndicatorColor}
          formatSize={ui.formatSize}
          isDbInitialized={project.isDbInitialized}
          availableTags={project.availableTags}
          initialTags={project.tags}
        />
      )}

      {tc.showOpen !== false && (
        <OpenProjectDialog
          open={ui.openDialogOpen}
          onOpenChange={ui.setOpenDialogOpen}
          isFirstTime={project.isFirstTime}
          isDbInitialized={project.isDbInitialized}
          storageAdapter={project.storageAdapter}
          availableTags={project.availableTags}
          onProjectLoad={project.loadProject}
          onProjectsListUpdate={project.refreshProjects}
          onCreateNew={() => ui.setCreateDialogOpen(true)}
          currentProjectName={project.projectName}
          allowedProjectTypes={fieldConfig.allowedProjectTypes}
        />
      )}
    </>
  )
}
