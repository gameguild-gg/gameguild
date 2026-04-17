"use client"

import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Badge } from "@/components/ui/badge"
import { Save, Eye, Blocks, Home, History, RotateCcw } from "lucide-react"
import { useState, useEffect, useRef } from "react"
import Link from "next/link"
import { useRouter } from "next/navigation"
import { OpenProjectDialog } from "@/components/editor/extras/editor/open-project-dialog"
import { CreateProjectDialog } from "@/components/editor/extras/editor/create-project-dialog"
import { SizeDetailsDialog } from "@/components/editor/extras/editor/size-details-dialog"
import { SyncStatusDialog } from "@/components/editor/extras/editor/sync-status-dialog"
import { AutoSaveToggle } from "@/components/editor/extras/editor/auto-save-toggle"
import { ProjectSizeIndicator } from "@/components/editor/extras/editor/project-size-indicator"
import { SyncStatusIndicator } from "@/components/editor/extras/editor/sync-status-indicator"
import { EditableProjectTitle } from "@/components/editor/extras/editor/editable-project-title"
import { ProjectStorageInfo } from "@/components/editor/extras/editor/project-storage-info"
import { ProjectModeIndicator } from "@/components/editor/extras/editor/project-mode-indicator"
import { PreviewModeSelector } from "@/components/editor/extras/editor/preview-mode-selector"
import { EditorLayoutType1 } from "@/components/editor/engines/lexical/editor-layout-type1"
import { EditorLayoutType2 } from "@/components/editor/engines/lexical/editor-layout-type2"
import { EditorLayoutSlideshow } from "@/components/editor/engines/lexical/editor-layout-slideshow"
import { ProjectImportDialog } from "@/components/editor/extras/editor/project-import-dialog"
import { syncConfig } from "@/lib/sync/editor/sync-config"
import { SaveAsDialog } from "@/components/editor/extras/editor/save-as-dialog"
import { ENGINE_TYPES } from "@/lib/storage/editor/project-types"
import { ExitConfirmDialog } from "@/components/editor/extras/dialogs/exit-confirm-dialog"
import { ProjectHistoryDialog } from "@/components/editor/extras/dialogs/project-history-dialog"
import { PreviewRenderer } from "@/components/editor/extras/preview/preview-renderer"
import { PreviewRendererType2 } from "@/components/editor/extras/preview/preview-renderer-type2"
import { PreviewRendererSlideshowContinuous } from "@/components/editor/extras/preview/preview-renderer-slideshow-continuous"
import { PreviewRendererSlideshowSlide } from "@/components/editor/extras/preview/preview-renderer-slideshow-slide"
import { BlockArrayEditor } from "@/components/editor/engines/blocks/block-array-editor"
import { BlockArrayViewer } from "@/components/editor/engines/blocks/block-array-viewer"
import { useProjectStorage } from "@/components/editor/hooks/useProjectStorage"
import { useProjectHistory } from "@/components/editor/hooks/useProjectHistory"
import { useProjectPreview } from "@/components/editor/hooks/useProjectPreview"

const RECOMMENDED_SIZE_KB = 5120

function formatSize(sizeInKB: number): string {
  if (sizeInKB < 1024) return `${sizeInKB.toFixed(1)}KB`
  return `${(sizeInKB / 1024).toFixed(1)}MB`
}

export default function Page() {
  const router = useRouter()
  const project = useProjectStorage()
  const history = useProjectHistory(project)
  const preview = useProjectPreview(project)

  // ── Sync readOnlyRef with history viewing ──
  useEffect(() => {
    project.readOnlyRef.current = history.isViewingHistory
  }, [history.isViewingHistory])

  // ── UI-only state ──
  const [saveAsDialogOpen, setSaveAsDialogOpen] = useState(false)
  const [openDialogOpen, setOpenDialogOpen] = useState(false)
  const [newProjectName, setNewProjectName] = useState("")
  const [showSizeDetails, setShowSizeDetails] = useState(false)
  const [showSyncStatus, setShowSyncStatus] = useState(false)
  const [createDialogOpen, setCreateDialogOpen] = useState(false)
  const [isEditingTitle, setIsEditingTitle] = useState(false)
  const [editingProjectName, setEditingProjectName] = useState("")
  const [historyDialogOpen, setHistoryDialogOpen] = useState(false)
  const [nextUrl, setNextUrl] = useState<string | null>(null)
  const [exitDialogOpen, setExitDialogOpen] = useState(false)
  const [importDialogOpen, setImportDialogOpen] = useState(false)
  const [importTargetSlideId, setImportTargetSlideId] = useState<string | null>(null)

  // ── Size indicator color ──
  const getSizeIndicatorColor = () => {
    if (project.projectSize > RECOMMENDED_SIZE_KB * 2) return "text-red-600"
    if (project.projectSize > RECOMMENDED_SIZE_KB) return "text-amber-600"
    return "text-green-600"
  }

  // ── Keyboard shortcut: Ctrl+S ──
  const handleSaveRef = useRef(async () => {
    if (history.isViewingHistory) return
    const result = await project.save()
    if (result.needsSaveAs) setSaveAsDialogOpen(true)
  })
  useEffect(() => {
    handleSaveRef.current = async () => {
      if (history.isViewingHistory) return
      const result = await project.save()
      if (result.needsSaveAs) setSaveAsDialogOpen(true)
    }
  })
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.ctrlKey && event.key === "s") {
        event.preventDefault()
        handleSaveRef.current()
      }
    }
    document.addEventListener("keydown", handleKeyDown)
    return () => document.removeEventListener("keydown", handleKeyDown)
  }, [])

  // ── Navigation helpers ──
  const handleLinkNavigation = (event: React.MouseEvent<HTMLAnchorElement>, url: string) => {
    if (event.ctrlKey || event.metaKey || event.button === 1) return
    event.preventDefault()
    if (project.projectId && project.editorState) {
      setNextUrl(url)
      setExitDialogOpen(true)
    } else {
      router.push(url)
    }
  }

  const handleNavigation = (url: string) => {
    if (project.projectId && project.editorState) {
      setNextUrl(url)
      setExitDialogOpen(true)
    } else {
      router.push(url)
    }
  }

  const handleSaveAndExit = async () => {
    await project.save()
    if (nextUrl) router.push(nextUrl)
    setExitDialogOpen(false)
  }

  // ── Save button handler ──
  const handleSave = async () => {
    const result = await project.save()
    if (result.needsSaveAs) setSaveAsDialogOpen(true)
  }

  // ── Import handlers (slideshow) ──
  const handleImportProject = (slideId: string) => {
    setImportTargetSlideId(slideId)
    setImportDialogOpen(true)
  }

  const handleImportConfirm = (projectId: string, loadMode: "snapshot" | "head", snapshotTag?: string) => {
    if (!importTargetSlideId) return
    project.importConfirm(importTargetSlideId, projectId, loadMode, snapshotTag)
    setImportDialogOpen(false)
    setImportTargetSlideId(null)
  }

  return (
    <>
      <div className="min-h-screen bg-gray-50 dark:bg-gray-950">
        <div className="container mx-auto py-8">
          <div className={`mx-auto space-y-6 px-4 sm:px-4 lg:px-4 ${project.layout === "single" ? "max-w-4xl" : "max-w-9xl"}`}>
            {/* Professional Header */}
            <div className="border border-gray-200 bg-white shadow-sm dark:border-gray-700 dark:bg-gray-900">
              <div className="flex items-center justify-between p-3 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
                <div className="flex items-center gap-3 flex-1 min-w-0">
                  <div className="p-1.5 bg-blue-50 dark:bg-blue-900/30 shrink-0">
                    <Blocks className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                  </div>
                  <div className="flex items-center gap-3 flex-1 min-w-0">
                    <h1 className="text-base font-semibold text-gray-900 dark:text-gray-100 whitespace-nowrap shrink-0">Content Studio</h1>
                    <div className="h-4 w-px bg-gray-300 dark:bg-gray-600 shrink-0"></div>
                    <div className="flex items-center gap-2 flex-1 min-w-0">
                      {project.projectId ? (
                        <div className="min-w-0 flex-1">
                          <EditableProjectTitle
                            projectName={project.projectName}
                            isEditing={isEditingTitle}
                            editingName={editingProjectName}
                            onEditStart={() => project.titleEdit(setEditingProjectName, setIsEditingTitle)}
                            onEditEnd={() => {
                              setIsEditingTitle(false)
                              setEditingProjectName(project.projectName)
                            }}
                            onNameChange={setEditingProjectName}
                            onSave={() => project.titleSave(editingProjectName, setEditingProjectName, setIsEditingTitle)}
                          />
                        </div>
                      ) : (
                        <span className="text-sm text-gray-500 dark:text-gray-400 italic">Untitled Project</span>
                      )}
                      {project.projectId && (
                        <div className="flex items-center gap-2 shrink-0">
                          <ProjectModeIndicator mode={project.projectMode} />
                          <ProjectStorageInfo storageType={project.storageType} />
                        </div>
                      )}
                    </div>
                  </div>
                </div>
                <div className="flex items-center gap-2">
                  <Link href="/gglexical" passHref>
                    <Button
                      onClick={(e: any) => handleLinkNavigation(e, "/gglexical")}
                      variant="ghost"
                      size="sm"
                      className="gap-2 hover:bg-gray-100 dark:hover:bg-gray-800"
                    >
                      <Home className="h-4 w-4" />
                      Home
                    </Button>
                  </Link>
                  <Link href="/gglexical/viewer" passHref>
                    <Button
                      onClick={(e: any) => handleLinkNavigation(e, "/gglexical/viewer")}
                      variant="ghost"
                      size="sm"
                      className="gap-2 hover:bg-gray-100 dark:hover:bg-gray-800"
                    >
                      <Eye className="h-4 w-4" />
                      Viewer
                    </Button>
                  </Link>
                </div>
              </div>

              {/* Action Bar */}
              <div className="flex items-center justify-between gap-4 p-4 bg-white dark:bg-gray-900">
                <div className="flex items-center gap-3">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={handleSave}
                    className="gap-2 border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800 bg-transparent"
                    disabled={!project.isDbInitialized || history.isViewingHistory}
                    title={history.isViewingHistory ? "Return to latest to save" : "Save project"}
                  >
                    <Save className="h-4 w-4" />
                    Save
                  </Button>

                  <SaveAsDialog
                    open={saveAsDialogOpen}
                    onOpenChange={setSaveAsDialogOpen}
                    projectName={newProjectName}
                    onProjectNameChange={setNewProjectName}
                    onSave={(storageOption) => project.saveAs(newProjectName, storageOption)}
                    currentProjectSize={project.projectSize}
                    getSizeIndicatorColor={getSizeIndicatorColor}
                    formatSize={formatSize}
                    isDbInitialized={project.isDbInitialized}
                  />

                  <OpenProjectDialog
                    open={openDialogOpen}
                    onOpenChange={setOpenDialogOpen}
                    isFirstTime={project.isFirstTime}
                    isDbInitialized={project.isDbInitialized}
                    storageAdapter={project.storageAdapter}
                    availableTags={project.availableTags}
                    editorRef={project.editorRef}
                    blockRefs={project.blockRefs}
                    setLoadingRef={project.setLoadingRef}
                    onProjectLoad={project.loadProject}
                    onProjectsListUpdate={project.refreshProjects}
                    onCreateNew={() => {
                      project.setProjectName("")
                      project.setTags([])
                      setCreateDialogOpen(true)
                      window.history.pushState(null, "", window.location.pathname)
                    }}
                    currentProjectName={project.projectName}
                  />
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

                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setHistoryDialogOpen(true)}
                    className="gap-2 border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800 bg-transparent"
                    disabled={!project.projectId}
                    title="View history and snapshots"
                  >
                    <History className="h-4 w-4" />
                    History
                  </Button>

                  {/* Preview Mode Selector (Slideshow Layout Only) */}
                  {project.layout === "slideshow" && project.slideshowStructure && (
                    <PreviewModeSelector
                      previewMode={project.previewMode}
                      onPreviewModeChange={project.setPreviewMode}
                    />
                  )}
                </div>

                {/* Status Indicators */}
                <div className="flex items-center gap-4">
                  <AutoSaveToggle
                    enabled={project.autoSaveEnabled}
                    onToggle={() => project.setAutoSaveEnabled(prev => !prev)}
                    disabled={!project.isDbInitialized}
                  />

                  <ProjectSizeIndicator
                    currentProjectSize={project.projectSize}
                    currentProjectAssetsSize={project.assetsSize}
                    formatSize={formatSize}
                    getSizeIndicatorColor={getSizeIndicatorColor}
                    onClick={() => setShowSizeDetails(true)}
                  />

                  {project.syncStats && (
                    <SyncStatusIndicator
                      syncStats={project.syncStats}
                      isSyncEnabled={syncConfig.isEnabled()}
                      onClick={() => setShowSyncStatus(!showSyncStatus)}
                    />
                  )}
                </div>
              </div>
            </div>

            {/* History Viewing Banner */}
            {history.isViewingHistory && (
              <div className="flex items-center justify-between p-3 bg-amber-50 border border-amber-200 dark:bg-amber-900/20 dark:border-amber-800">
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

            <CreateProjectDialog
              open={createDialogOpen}
              onOpenChange={(open) => {
                setCreateDialogOpen(open)
                if (!open) setOpenDialogOpen(true)
              }}
              isDbInitialized={project.isDbInitialized}
              storageAdapter={project.storageAdapter}
              availableTags={project.availableTags}
              onProjectCreate={project.createProject}
              onProjectsListUpdate={project.refreshProjects}
              onAvailableTagsUpdate={project.refreshTags}
              generateProjectId={project.generateProjectId}
            />

            {/* Editor Container - Render based on engine and layout type */}
            {project.engine === ENGINE_TYPES.BLOCKS ? (
              <div className="border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 p-4">
                <BlockArrayEditor
                  blocks={project.blockArrayBlocks}
                  onChange={project.setBlockArrayBlocks}
                  readOnly={history.isViewingHistory}
                />
              </div>
            ) : project.layout === "slideshow" && project.slideshowStructure ? (
              <EditorLayoutSlideshow
                structure={project.slideshowStructure}
                onStructureChange={project.setSlideshowStructure}
                deps={project.slideshowDeps}
                onDepsChange={project.setSlideshowDeps}
                currentSlideIndex={project.currentSlideIndex}
                onSlideIndexChange={project.setCurrentSlideIndex}
                slideEditorRefs={project.slideEditorRefs}
                onSlideEditorRefsChange={project.setSlideEditorRefs}
                onLoadingChange={(setLoading) => {
                  project.setLoadingRef.current = setLoading
                }}
                projectId={project.projectId}
                mode={project.projectMode}
                currentProjectType={project.projectType}
                storageAdapter={project.storageAdapter}
                preferences={project.preferences}
                onPreferencesChange={project.setPreferences}
                readOnly={history.isViewingHistory}
                resolvedProjects={project.resolvedProjects}
                onConvertToIndependent={project.convertToIndependent}
                onConvertToDependent={project.convertToDependent}
                onImportProject={handleImportProject}
              />
            ) : project.layout === "single" ? (
              <EditorLayoutType1
                editorRef={project.editorRef}
                editorState={project.editorState}
                onEditorChange={project.setEditorState}
                onLoadingChange={(setLoading) => {
                  project.setLoadingRef.current = setLoading
                }}
                projectId={project.projectId}
                mode={project.projectMode}
                currentProjectType={project.projectType}
                storageAdapter={project.storageAdapter}
                readOnly={history.isViewingHistory}
              />
            ) : (
              <EditorLayoutType2
                blockRefs={project.blockRefs}
                blockStates={project.blockStates}
                onBlockChange={(blockId, newState) => {
                  project.setBlockStates(prev => ({ ...prev, [blockId]: newState }))
                }}
                onBlockAdd={project.addBlock}
                onBlockRemove={project.removeBlock}
                onLoadingChange={(setLoading) => {
                  project.setLoadingRef.current = setLoading
                }}
                projectId={project.projectId}
                mode={project.projectMode}
                currentProjectType={project.projectType}
                storageAdapter={project.storageAdapter}
                preferences={project.preferences}
                onPreferencesChange={project.setPreferences}
                currentProjectId={project.projectId}
                readOnly={history.isViewingHistory}
              />
            )}
          </div>
        </div>
      </div>
      <SizeDetailsDialog
        open={showSizeDetails}
        onOpenChange={setShowSizeDetails}
        currentProjectSize={project.projectSize}
        currentProjectAssetsSize={project.assetsSize}
        currentProjectAssets={project.assets}
        recommendedSizeKB={RECOMMENDED_SIZE_KB}
        formatSize={formatSize}
        getSizeIndicatorColor={getSizeIndicatorColor}
      />
      <SyncStatusDialog
        open={showSyncStatus}
        onOpenChange={setShowSyncStatus}
        syncStats={project.syncStats}
        onRetryFailed={() => project.db.retryFailedSync()}
      />
      <ExitConfirmDialog
        open={exitDialogOpen}
        onOpenChange={setExitDialogOpen}
        onConfirm={() => {
          if (nextUrl) router.push(nextUrl)
        }}
        itemName={project.projectName}
        itemType="project"
        showSaveAndExit={true}
        onSaveAndExit={handleSaveAndExit}
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
        open={historyDialogOpen}
        onOpenChange={setHistoryDialogOpen}
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
        open={importDialogOpen}
        onOpenChange={setImportDialogOpen}
        storageAdapter={{
          list: () => project.storageAdapter.list(),
          listSnapshots: (id: string) => project.db.listSnapshots(id),
        }}
        onConfirm={handleImportConfirm}
        currentProjectId={project.projectId}
      />
    </>
  )
}
