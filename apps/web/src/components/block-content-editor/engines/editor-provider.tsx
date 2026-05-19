"use client"

/**
 * EditorProvider
 *
 * Top-level provider for every editor page. Composes the three core hooks
 * (`useProjectStorage`, `useProjectHistory`, `useProjectPreview`), merges the
 * page-supplied `FieldConfig` and `ToolbarConfig` with their defaults, and
 * exposes everything via React Context (`useEditor()`).
 *
 * Also owns:
 *   - Ctrl+S keyboard shortcut → `ui.handleSave()`.
 *   - Navigation guards / exit-confirmation dialog.
 *   - Shared UI state (dialog open/close, title editing, size indicators, …).
 *
 * See `docs/ARCHITECTURE.md` ("Provider Layer") for the high-level role
 * and `docs/DATA-FLOW.md` ("Editor Flow") for the data path.
 */

import { createContext, useContext, useEffect, useState, useRef, type ReactNode } from "react"
import { useRouter } from "next/navigation"
import { useProjectStorage, type UseProjectStorageReturn } from "@/components/block-content-editor/hooks/useProjectStorage"
import { useProjectHistory, type UseProjectHistoryReturn } from "@/components/block-content-editor/hooks/useProjectHistory"
import { useProjectPreview, type UseProjectPreviewReturn } from "@/components/block-content-editor/hooks/useProjectPreview"
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
  // Derived helpers
  handleSave: () => Promise<void>
  handleSaveAndExit: () => Promise<void>
  handleLinkNavigation: (event: React.MouseEvent<HTMLAnchorElement>, url: string) => void
  handleNavigation: (url: string) => void
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
    if (project.projectId && project.blocks.length > 0) {
      setNextUrl(url)
      setExitDialogOpen(true)
    } else {
      router.push(url)
    }
  }

  const handleNavigation = (url: string) => {
    if (project.projectId && project.blocks.length > 0) {
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
    handleSave,
    handleSaveAndExit,
    handleLinkNavigation,
    handleNavigation,
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
