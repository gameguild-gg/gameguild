"use client"

import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { CreateProjectDialog } from "@/components/editor/extras/editor/create-project-dialog"
import { SizeDetailsDialog } from "@/components/editor/extras/editor/size-details-dialog"
import { SyncStatusDialog } from "@/components/editor/extras/editor/sync-status-dialog"
import { ProjectImportDialog } from "@/components/editor/extras/editor/project-import-dialog"
import { ExitConfirmDialog } from "@/components/editor/extras/dialogs/exit-confirm-dialog"
import { ProjectHistoryDialog } from "@/components/editor/extras/dialogs/project-history-dialog"
import { PreviewRenderer } from "@/components/editor/extras/preview/preview-renderer"
import { PreviewRendererType2 } from "@/components/editor/extras/preview/preview-renderer-type2"
import { PreviewRendererSlideshowContinuous } from "@/components/editor/extras/preview/preview-renderer-slideshow-continuous"
import { PreviewRendererSlideshowSlide } from "@/components/editor/extras/preview/preview-renderer-slideshow-slide"
import { BlockArrayViewer } from "@/components/editor/engines/blocks/block-array-viewer"
import { ENGINE_TYPES } from "@/lib/storage/editor/project-types"
import { useEditor } from "./editor-provider"

const RECOMMENDED_SIZE_KB = 5120

export function EditorDialogs() {
  const { project, history, preview, ui, fieldConfig } = useEditor()

  return (
    <>
      <CreateProjectDialog
        open={ui.createDialogOpen}
        onOpenChange={(open) => {
          ui.setCreateDialogOpen(open)
          if (!open) ui.setOpenDialogOpen(true)
        }}
        isDbInitialized={project.isDbInitialized}
        storageAdapter={project.storageAdapter}
        availableTags={project.availableTags}
        onProjectCreate={project.createProject}
        onProjectsListUpdate={project.refreshProjects}
        onAvailableTagsUpdate={project.refreshTags}
        generateProjectId={project.generateProjectId}
        allowedEngines={fieldConfig.engines}
        allowedLayouts={fieldConfig.layouts}
        allowedModes={fieldConfig.allowedModes}
        defaultMode={fieldConfig.allowedModes?.[0] ?? fieldConfig.defaultMode}
      />

      <SizeDetailsDialog
        open={ui.showSizeDetails}
        onOpenChange={ui.setShowSizeDetails}
        currentProjectSize={project.projectSize}
        currentProjectAssetsSize={project.assetsSize}
        currentProjectAssets={project.assets}
        recommendedSizeKB={RECOMMENDED_SIZE_KB}
        formatSize={ui.formatSize}
        getSizeIndicatorColor={ui.getSizeIndicatorColor}
      />

      <SyncStatusDialog
        open={ui.showSyncStatus}
        onOpenChange={ui.setShowSyncStatus}
        syncStats={project.syncStats}
        onRetryFailed={() => project.db.retryFailedSync()}
      />

      <ExitConfirmDialog
        open={ui.exitDialogOpen}
        onOpenChange={ui.setExitDialogOpen}
        onConfirm={ui.handleExitConfirm}
        itemName={project.projectName}
        itemType="project"
        showSaveAndExit={true}
        onSaveAndExit={ui.handleSaveAndExit}
      />

      {/* Preview Dialog */}
      <Dialog open={preview.previewOpen} onOpenChange={preview.setPreviewOpen}>
        <DialogContent
          className={(preview.previewLayout === "multiple" || preview.previewLayout === "slideshow") ? "max-w-none! p-6" : "max-w-4xl max-h-[90vh] overflow-y-auto"}
          style={(preview.previewLayout === "multiple" || preview.previewLayout === "slideshow") ? { width: '95vw', maxWidth: '95vw' } : undefined}
        >
          <DialogHeader>
            <DialogTitle>Preview</DialogTitle>
          </DialogHeader>
          {project.engine === ENGINE_TYPES.BLOCKS && (
            <div className="w-full max-h-[80vh] overflow-y-auto">
              <BlockArrayViewer blocks={project.blockArrayBlocks} />
            </div>
          )}
          {project.engine !== ENGINE_TYPES.BLOCKS && preview.previewLayout === "single" && preview.previewState && (
            <PreviewRenderer serializedState={preview.previewState} />
          )}
          {preview.previewLayout === "multiple" && Object.keys(preview.previewBlockStates).length >= 1 && (
            <div className="w-full max-h-[80vh] overflow-y-auto">
              <PreviewRendererType2
                blockStates={preview.previewBlockStates}
                preferences={project.preferences}
                onLayoutChange={(panels, direction) => {
                  if (project.preferences) {
                    project.setPreferences({
                      ...project.preferences,
                      global: {
                        ...project.preferences.global,
                        advancedMultiBlockPanels: panels,
                        multiBlockDirection: direction,
                      }
                    })
                  }
                }}
              />
            </div>
          )}
          {preview.previewLayout === "slideshow" && preview.previewSlideshowStructure && (
            <div className="w-full max-h-[80vh] overflow-y-auto">
              {preview.previewSlideshowMode === "slide" ? (
                <PreviewRendererSlideshowSlide
                  structure={preview.previewSlideshowStructure}
                  projectId={project.projectId}
                  projectName={project.projectName}
                  deps={project.slideshowDeps}
                  resolvedProjects={project.resolvedProjects}
                  storageAdapter={project.storageAdapter}
                  preferences={project.preferences}
                />
              ) : (
                <PreviewRendererSlideshowContinuous
                  structure={preview.previewSlideshowStructure}
                  projectId={project.projectId}
                  projectName={project.projectName}
                  deps={project.slideshowDeps}
                  resolvedProjects={project.resolvedProjects}
                  storageAdapter={project.storageAdapter}
                  preferences={project.preferences}
                />
              )}
            </div>
          )}
        </DialogContent>
      </Dialog>

      {/* History Dialog */}
      <ProjectHistoryDialog
        open={ui.historyDialogOpen}
        onOpenChange={ui.setHistoryDialogOpen}
        projectId={project.projectId}
        projectName={project.projectName}
        isViewingHistory={history.isViewingHistory}
        currentViewingSha={history.currentViewingSha}
        onLoadCommit={history.loadCommit}
        onLoadSnapshot={history.loadSnapshot}
        onReturnToHead={history.returnToHead}
        onCreateSnapshot={project.createSnapshot}
        listHistory={(id) => project.db.listHistory(id)}
        listSnapshots={(id) => project.db.listSnapshots(id)}
      />

      {/* Project Import Dialog for slideshow slides */}
      <ProjectImportDialog
        open={ui.importDialogOpen}
        onOpenChange={ui.setImportDialogOpen}
        storageAdapter={{
          list: () => project.storageAdapter.list(),
          listSnapshots: (id: string) => project.db.listSnapshots(id),
        }}
        onConfirm={ui.handleImportConfirm}
        currentProjectId={project.projectId}
      />
    </>
  )
}
