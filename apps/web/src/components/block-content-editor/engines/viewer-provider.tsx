"use client"

/**
 * ViewerProvider
 *
 * Read-only counterpart to `EditorProvider`. Wraps `useViewerStorage()` and
 * exposes the loaded project + blocks through `useViewer()`. Has no write
 * path — the only state changes are dialog open/close.
 */

import { createContext, useContext, useState, type ReactNode } from "react"
import { useRouter } from "next/navigation"
import { useViewerStorage, type UseViewerStorageReturn } from "@/components/block-content-editor/hooks/useViewerStorage"
import { type FieldConfig, type ToolbarConfig, mergeFieldConfig, mergeToolbarConfig } from "./editor-config"

// ============================================================================
// Context
// ============================================================================

export interface ViewerContextValue {
  viewer: UseViewerStorageReturn
  fieldConfig: FieldConfig
  toolbarConfig: ToolbarConfig
  ui: ViewerUIState
}

export interface ViewerUIState {
  openDialogOpen: boolean
  setOpenDialogOpen: (open: boolean) => void
  sidebarOpen: boolean
  setSidebarOpen: (open: boolean) => void
  exitDialogOpen: boolean
  setExitDialogOpen: (open: boolean) => void
  nextUrl: string
  setNextUrl: (url: string) => void
  handleLinkNavigation: (event: React.MouseEvent<HTMLElement>, url: string) => void
}

const ViewerContext = createContext<ViewerContextValue | null>(null)

export function useViewer(): ViewerContextValue {
  const ctx = useContext(ViewerContext)
  if (!ctx) throw new Error("useViewer must be used within <ViewerProvider>")
  return ctx
}

// ============================================================================
// Provider
// ============================================================================

export interface ViewerProviderProps {
  fieldConfig?: Partial<FieldConfig>
  toolbarConfig?: Partial<ToolbarConfig>
  children: ReactNode
}

export function ViewerProvider({ fieldConfig: fieldPartial, toolbarConfig: toolbarPartial, children }: ViewerProviderProps) {
  const router = useRouter()
  const viewer = useViewerStorage()

  const fieldConfig = mergeFieldConfig(fieldPartial)
  const toolbarConfig = mergeToolbarConfig(toolbarPartial)

  // ── UI-only state ──
  const [openDialogOpen, setOpenDialogOpen] = useState(false)
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const [exitDialogOpen, setExitDialogOpen] = useState(false)
  const [nextUrl, setNextUrl] = useState("")

  const handleLinkNavigation = (event: React.MouseEvent<HTMLElement>, url: string) => {
    if (event.ctrlKey || event.metaKey || event.button === 1) return
    event.preventDefault()
    if (viewer.currentProject) {
      setNextUrl(url)
      setExitDialogOpen(true)
    } else {
      router.push(url)
    }
  }

  const ui: ViewerUIState = {
    openDialogOpen, setOpenDialogOpen,
    sidebarOpen, setSidebarOpen,
    exitDialogOpen, setExitDialogOpen,
    nextUrl, setNextUrl,
    handleLinkNavigation,
  }

  return (
    <ViewerContext.Provider value={{ viewer, fieldConfig, toolbarConfig, ui }}>
      {children}
    </ViewerContext.Provider>
  )
}
