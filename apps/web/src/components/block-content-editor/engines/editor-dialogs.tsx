"use client"

import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { CreateProjectDialog } from "@/components/block-content-editor/extras/editor/create-project-dialog"
import { SizeDetailsDialog } from "@/components/block-content-editor/extras/editor/size-details-dialog"
import { SyncStatusDialog } from "@/components/block-content-editor/extras/editor/sync-status-dialog"
import { ExitConfirmDialog } from "@/components/block-content-editor/extras/dialogs/exit-confirm-dialog"
import { ProjectHistoryDialog } from "@/components/block-content-editor/extras/dialogs/project-history-dialog"
import { BlockArrayViewer } from "@/components/block-content-editor/engines/blocks/block-array-viewer"
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
        projectType={fieldConfig.projectType}
        allowedProjectTypes={fieldConfig.allowedProjectTypes}
        singleBlockMode={fieldConfig.singleBlockMode}
        allowedBlockTypes={fieldConfig.allowedBlockTypes}
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
        <DialogContent className="max-w-4xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Preview</DialogTitle>
          </DialogHeader>
          <div className="w-full max-h-[80vh] overflow-y-auto">
            <BlockArrayViewer blocks={project.blocks} />
          </div>
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
    </>
  )
}
