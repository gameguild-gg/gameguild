"use client"

import { createContext, useContext, useEffect, useState, useRef, type ReactNode } from "react"
import { useRouter } from "next/navigation"
import { useProjectStorage, type UseProjectStorageReturn } from "@/components/editor/hooks/useProjectStorage"
import { useProjectHistory, type UseProjectHistoryReturn } from "@/components/editor/hooks/useProjectHistory"
import { useProjectPreview, type UseProjectPreviewReturn } from "@/components/editor/hooks/useProjectPreview"
import { type FieldConfig, type ToolbarConfig, mergeFieldConfig, mergeToolbarConfig } from "./editor-config"

// ============================================================================
// Context
// ============================================================================

export interface EditorContextValue {
  project: UseProjectStorageReturn
  history: UseProjectHistoryReturn
  preview: UseProjectPreviewReturn
  fieldConfig: FieldConfig
  toolbarConfig: ToolbarConfig
  // UI-only state shared across toolbar/dialogs
  ui: EditorUIState
}

export interface EditorUIState {
  saveAsDialogOpen: boolean
  setSaveAsDialogOpen: (open: boolean) => void
  openDialogOpen: boolean
  setOpenDialogOpen: (open: boolean) => void
  newProjectName: string
  setNewProjectName: (name: string) => void
  showSizeDetails: boolean
  setShowSizeDetails: (open: boolean) => void
  showSyncStatus: boolean
  setShowSyncStatus: (open: boolean) => void
  createDialogOpen: boolean
  setCreateDialogOpen: (open: boolean) => void
  isEditingTitle: boolean
  setIsEditingTitle: (editing: boolean) => void
  editingProjectName: string
  setEditingProjectName: (name: string) => void
  historyDialogOpen: boolean
  setHistoryDialogOpen: (open: boolean) => void
  nextUrl: string | null
  setNextUrl: (url: string | null) => void
  exitDialogOpen: boolean
  setExitDialogOpen: (open: boolean) => void
  importDialogOpen: boolean
  setImportDialogOpen: (open: boolean) => void
  importTargetSlideId: string | null
  setImportTargetSlideId: (id: string | null) => void
  // Derived helpers
  handleSave: () => Promise<void>
  handleSaveAndExit: () => Promise<void>
  handleLinkNavigation: (event: React.MouseEvent<HTMLAnchorElement>, url: string) => void
  handleNavigation: (url: string) => void
  handleImportProject: (slideId: string) => void
  handleImportConfirm: (projectId: string, loadMode: "snapshot" | "head", snapshotTag?: string) => void
  handleExitConfirm: () => void
  getSizeIndicatorColor: () => string
  formatSize: (sizeInKB: number) => string
}

const EditorContext = createContext<EditorContextValue | null>(null)

export function useEditor(): EditorContextValue {
  const ctx = useContext(EditorContext)
  if (!ctx) throw new Error("useEditor must be used within <EditorProvider>")
  return ctx
}

// ============================================================================
// Provider
// ============================================================================

const RECOMMENDED_SIZE_KB = 5120

function formatSize(sizeInKB: number): string {
  if (sizeInKB < 1024) return `${sizeInKB.toFixed(1)}KB`
  return `${(sizeInKB / 1024).toFixed(1)}MB`
}

export interface EditorProviderProps {
  fieldConfig?: Partial<FieldConfig>
  toolbarConfig?: Partial<ToolbarConfig>
  children: ReactNode
}

export function EditorProvider({ fieldConfig: fieldPartial, toolbarConfig: toolbarPartial, children }: EditorProviderProps) {
  const router = useRouter()
  const fieldConfig = mergeFieldConfig(fieldPartial)
  const toolbarConfig = mergeToolbarConfig(toolbarPartial)

  // allowedModes drives the effective mode; defaultMode is just a UI hint for the dialog
  const effectiveMode = fieldConfig.allowedModes?.[0] ?? fieldConfig.defaultMode

  const project = useProjectStorage({
    engine: fieldConfig.defaultEngine,
    layout: fieldConfig.defaultLayout,
    mode: effectiveMode,
  })
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

  // ── Derived helpers ──
  const getSizeIndicatorColor = () => {
    if (project.projectSize > RECOMMENDED_SIZE_KB * 2) return "text-red-600"
    if (project.projectSize > RECOMMENDED_SIZE_KB) return "text-amber-600"
    return "text-green-600"
  }

  const handleSave = async () => {
    const result = await project.save()
    if (result.needsSaveAs) setSaveAsDialogOpen(true)
  }

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

  const handleExitConfirm = () => {
    if (nextUrl) router.push(nextUrl)
  }

  const ui: EditorUIState = {
    saveAsDialogOpen, setSaveAsDialogOpen,
    openDialogOpen, setOpenDialogOpen,
    newProjectName, setNewProjectName,
    showSizeDetails, setShowSizeDetails,
    showSyncStatus, setShowSyncStatus,
    createDialogOpen, setCreateDialogOpen,
    isEditingTitle, setIsEditingTitle,
    editingProjectName, setEditingProjectName,
    historyDialogOpen, setHistoryDialogOpen,
    nextUrl, setNextUrl,
    exitDialogOpen, setExitDialogOpen,
    importDialogOpen, setImportDialogOpen,
    importTargetSlideId, setImportTargetSlideId,
    handleSave,
    handleSaveAndExit,
    handleLinkNavigation,
    handleNavigation,
    handleImportProject,
    handleImportConfirm,
    handleExitConfirm,
    getSizeIndicatorColor,
    formatSize,
  }

  return (
    <EditorContext.Provider value={{ project, history, preview, fieldConfig, toolbarConfig, ui }}>
      {children}
    </EditorContext.Provider>
  )
}
