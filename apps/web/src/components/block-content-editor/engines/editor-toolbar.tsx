"use client"

import {
  Menubar,
  MenubarContent,
  MenubarItem,
  MenubarMenu,
  MenubarSeparator,
  MenubarTrigger,
  MenubarShortcut,
} from "@/components/ui/menubar"
import { Badge } from "@/components/ui/badge"
import { Blocks, History, RotateCcw } from "lucide-react"
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

export function EditorToolbar() {
  const { project, history, preview, toolbarConfig: tc, ui, fieldConfig } = useEditor()

  return (
    <div className="flex flex-col w-full border-b border-gray-200 bg-white dark:border-gray-800 dark:bg-gray-950">
      <div className="flex items-center justify-between px-2 py-1">
        <div className="flex items-center gap-2">
          {/* Logo */}
          <div className="p-1 bg-blue-50 dark:bg-blue-900/30 rounded">
            <Blocks className="h-4 w-4 text-blue-600 dark:text-blue-400" />
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
              <MenubarTrigger className="cursor-pointer">View</MenubarTrigger>
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
              <MenubarTrigger className="cursor-pointer">Settings</MenubarTrigger>
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

          <div className="h-4 w-px bg-gray-300 dark:bg-gray-700 mx-2"></div>

          {/* Project Title inline */}
          <div className="flex items-center gap-2 text-sm max-w-[200px] sm:max-w-xs overflow-hidden">
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
              <span className="text-gray-500 dark:text-gray-400 italic text-xs">Untitled</span>
            )}
          </div>
        </div>

        {/* Right side indicators */}
        <div className="flex items-center gap-3 text-xs">
          {project.projectId && tc.showTypeIndicator !== false && (
            <ProjectTypeIndicator type={project.preferences?.global?.projectType} />
          )}
          {project.projectId && tc.showStorageInfo !== false && (
            <ProjectStorageInfo storageType={project.storageType} />
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
      </div>

      {/* History Viewing Banner */}
      {history.isViewingHistory && (
        <div className="flex items-center justify-between px-3 py-1 bg-amber-50 border-t border-amber-200 dark:bg-amber-900/20 dark:border-amber-800">
          <div className="flex items-center gap-2">
            <History className="h-4 w-4 text-amber-600 dark:text-amber-400" />
            <span className="text-sm font-medium text-amber-800 dark:text-amber-200">
              Viewing historical version
            </span>
            {history.currentViewingSha && (
              <Badge variant="outline" className="font-mono text-xs">
                {history.currentViewingSha.substring(0, 7)}
              </Badge>
            )}
            <span className="text-xs text-amber-600 dark:text-amber-400">
              (Read-only)
            </span>
          </div>
          <Button
            variant="outline"
            size="sm"
            onClick={history.returnToHead}
            className="gap-2 bg-white h-7 text-xs dark:bg-gray-800 border-amber-300 dark:border-amber-700 hover:bg-amber-100 dark:hover:bg-amber-900/40"
          >
            <RotateCcw className="h-3 w-3" />
            Return to Latest
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
    </div>
  )
}
