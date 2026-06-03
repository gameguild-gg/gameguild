"use client"

import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Save, Eye, Blocks, Home, History, RotateCcw } from "lucide-react"
import Link from "next/link"
import { SaveAsDialog } from "@/components/block-content-editor/extras/editor/save-as-dialog"
import { OpenProjectDialog } from "@/components/block-content-editor/extras/editor/open-project-dialog"
import { AutoSaveToggle } from "@/components/block-content-editor/extras/editor/auto-save-toggle"
import { ProjectSizeIndicator } from "@/components/block-content-editor/extras/editor/project-size-indicator"
import { SyncStatusIndicator } from "@/components/block-content-editor/extras/editor/sync-status-indicator"
import { EditableProjectTitle } from "@/components/block-content-editor/extras/editor/editable-project-title"
import { ProjectStorageInfo } from "@/components/block-content-editor/extras/editor/project-storage-info"
import { ProjectModeIndicator } from "@/components/block-content-editor/extras/editor/project-mode-indicator"
import { syncConfig } from "@/components/block-content-editor/lib/sync/editor/sync-config"
import { useEditor } from "./editor-provider"

export function EditorToolbar() {
  const { project, history, preview, toolbarConfig: tc, ui, fieldConfig } = useEditor()

  return (
    <div className="border border-gray-200 bg-white shadow-sm dark:border-gray-700 dark:bg-gray-900">
      {/* Title Bar */}
      <div className="flex items-center justify-between p-3 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
        <div className="flex items-center gap-3 flex-1 min-w-0">
          <div className="p-1.5 bg-blue-50 dark:bg-blue-900/30 shrink-0">
            <Blocks className="h-4 w-4 text-blue-600 dark:text-blue-400" />
          </div>
          <div className="flex items-center gap-3 flex-1 min-w-0">
            <h1 className="text-base font-semibold text-gray-900 dark:text-gray-100 whitespace-nowrap shrink-0">Content Studio</h1>
            <div className="h-4 w-px bg-gray-300 dark:bg-gray-600 shrink-0"></div>
            <div className="flex items-center gap-2 flex-1 min-w-0">
              {project.projectId && tc.showProjectTitle !== false ? (
                <div className="min-w-0 flex-1">
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
                <span className="text-sm text-gray-500 dark:text-gray-400 italic">Untitled Project</span>
              )}
              {project.projectId && (
                <div className="flex items-center gap-2 shrink-0">
                  {tc.showModeIndicator !== false && <ProjectModeIndicator mode={project.projectMode} />}
                  {tc.showStorageInfo !== false && <ProjectStorageInfo storageType={project.storageType} />}
                </div>
              )}
            </div>
          </div>
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
          {tc.showNavViewer !== false && (
            <Link href="/block-content-editor/viewer" passHref>
              <Button
                onClick={(e: any) => ui.handleLinkNavigation(e, "/block-content-editor/viewer")}
                variant="ghost"
                size="sm"
                className="gap-2 hover:bg-gray-100 dark:hover:bg-gray-800"
              >
                <Eye className="h-4 w-4" />
                Viewer
              </Button>
            </Link>
          )}
        </div>
      </div>

      {/* Action Bar */}
      <div className="flex items-center justify-between gap-4 p-4 bg-white dark:bg-gray-900">
        <div className="flex items-center gap-3">
          {tc.showSave !== false && (
            <Button
              variant="outline"
              size="sm"
              onClick={ui.handleSave}
              className="gap-2 border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800 bg-transparent"
              disabled={!project.isDbInitialized || history.isViewingHistory}
              title={history.isViewingHistory ? "Return to latest to save" : "Save project"}
            >
              <Save className="h-4 w-4" />
              Save
            </Button>
          )}

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
              onCreateNew={() => {
                project.setProjectName("")
                project.setTags([])
                ui.setCreateDialogOpen(true)
                window.history.pushState(null, "", window.location.pathname)
              }}
              currentProjectName={project.projectName}
              allowedProjectTypes={fieldConfig.allowedProjectTypes}
            />
          )}

          {tc.showPreview !== false && (
            <Button
              variant="outline"
              size="sm"
              onClick={preview.openPreview}
              className="gap-2 border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800 bg-transparent"
              disabled={!project.projectId}
              title="Preview in new tab"
            >
              <Eye className="h-4 w-4" />
              Preview
            </Button>
          )}

          {tc.showHistory !== false && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => ui.setHistoryDialogOpen(true)}
              className="gap-2 border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800 bg-transparent"
              disabled={!project.projectId}
              title="View history and snapshots"
            >
              <History className="h-4 w-4" />
              History
            </Button>
          )}

        </div>

        {/* Status Indicators */}
        <div className="flex items-center gap-4">
          {tc.showAutoSave !== false && (
            <AutoSaveToggle
              enabled={project.autoSaveEnabled}
              onToggle={() => project.setAutoSaveEnabled(prev => !prev)}
              disabled={!project.isDbInitialized}
            />
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
        <div className="flex items-center justify-between p-3 bg-amber-50 border-t border-amber-200 dark:bg-amber-900/20 dark:border-amber-800">
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
            className="gap-2 bg-white dark:bg-gray-800 border-amber-300 dark:border-amber-700 hover:bg-amber-100 dark:hover:bg-amber-900/40"
          >
            <RotateCcw className="h-4 w-4" />
            Return to Latest
          </Button>
        </div>
      )}
    </div>
  )
}
