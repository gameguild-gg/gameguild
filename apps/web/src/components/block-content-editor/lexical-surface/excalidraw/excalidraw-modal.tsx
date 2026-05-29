/**
 * ExcalidrawModal — full-screen modal hosting the Excalidraw canvas.
 *
 * Ported (simplified) from
 * `lexical-playground/src/ui/ExcalidrawModal.tsx`. Uses shadcn `Dialog`
 * shell + dynamic-imported Excalidraw to avoid SSR.
 */
"use client"

import "@excalidraw/excalidraw/index.css"

import * as React from "react"
import { useState } from "react"
import dynamic from "next/dynamic"
import { useTheme } from "next-themes"
import type {
  AppState,
  BinaryFiles,
  ExcalidrawImperativeAPI,
  ExcalidrawInitialDataState,
} from "@excalidraw/excalidraw/types"
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { cn } from "@/lib/utils"

/**
 * Selector que cobre overlays/popovers internos do Excalidraw
 * (help, library, mermaid-to-excalidraw, color picker, etc.).
 * Usado nos guards do Radix Dialog para que cliques dentro desses
 * sub-modais não fechem o nosso Dialog hospedeiro.
 */
const EXCALIDRAW_INTERNAL_SELECTOR =
  ".excalidraw, .excalidraw-modal-container, .excalidraw-overlay, [class*=\"excalidraw\"], .ttd-dialog, .Dialog, .Modal, .Island, .popover, .HelpDialog, .library-menu, .picker, [data-prevent-outside-click]"

function isInsideExcalidrawInternal(target: EventTarget | null): boolean {
  if (!(target instanceof Element)) return false
  return target.closest(EXCALIDRAW_INTERNAL_SELECTOR) !== null
}

const Excalidraw = dynamic(
  async () => (await import("@excalidraw/excalidraw")).Excalidraw,
  { ssr: false },
)

export type ExcalidrawInitialElements = ExcalidrawInitialDataState["elements"]

export type ExcalidrawModalProps = {
  isShown: boolean
  initialElements: ExcalidrawInitialElements
  initialAppState: AppState
  initialFiles: BinaryFiles
  onClose: () => void
  onDelete: () => void
  onSave: (
    elements: ExcalidrawInitialElements,
    appState: Partial<AppState>,
    files: BinaryFiles,
  ) => void
}

export default function ExcalidrawModal({
  isShown,
  initialElements,
  initialAppState,
  initialFiles,
  onClose,
  onDelete,
  onSave,
}: ExcalidrawModalProps): React.JSX.Element | null {
  const [excalidrawAPI, setExcalidrawAPI] = useState<ExcalidrawImperativeAPI | null>(null)
  const [elements, setElements] = useState<ExcalidrawInitialElements>(initialElements)
  const [files, setFiles] = useState<BinaryFiles>(initialFiles)
  const { resolvedTheme } = useTheme()
  const excalidrawTheme: "light" | "dark" = resolvedTheme === "dark" ? "dark" : "light"

  if (!isShown) return null

  const save = () => {
    if (elements?.some((el) => !el.isDeleted)) {
      const appState = excalidrawAPI?.getAppState()
      const partial: Partial<AppState> = {
        exportBackground: appState?.exportBackground,
        exportScale: appState?.exportScale,
        exportWithDarkMode: appState?.theme === "dark",
        isBindingEnabled: appState?.isBindingEnabled,
        isLoading: appState?.isLoading,
        name: appState?.name,
        theme: appState?.theme,
        viewBackgroundColor: appState?.viewBackgroundColor,
        viewModeEnabled: appState?.viewModeEnabled,
        zenModeEnabled: appState?.zenModeEnabled,
        zoom: appState?.zoom,
      }
      onSave(elements, partial, files)
    } else {
      onDelete()
    }
  }

  return (
    <Dialog open={isShown} onOpenChange={(open) => { if (!open) onClose() }}>
      <DialogContent
        className="max-w-[95vw] sm:max-w-[95vw] w-[95vw] h-[90vh] p-0 flex flex-col"
        onPointerDownOutside={(e) => {
          if (isInsideExcalidrawInternal(e.target)) e.preventDefault()
        }}
        onInteractOutside={(e) => {
          if (isInsideExcalidrawInternal(e.target)) e.preventDefault()
        }}
        onFocusOutside={(e) => {
          if (isInsideExcalidrawInternal(e.target)) e.preventDefault()
        }}
      >
        <DialogHeader className="px-4 pt-4 pb-2">
          <DialogTitle>Excalidraw</DialogTitle>
        </DialogHeader>
        <div className={cn("flex-1 min-h-0 px-4")}>
          <Excalidraw
            excalidrawAPI={setExcalidrawAPI}
            theme={excalidrawTheme}
            initialData={{
              appState: {
                ...(initialAppState || { isLoading: false }),
                theme: (initialAppState?.theme ?? excalidrawTheme) as AppState["theme"],
              },
              elements: initialElements,
              files: initialFiles,
            }}
            onChange={(els, _ap, fls) => {
              setElements(els)
              setFiles(fls)
            }}
          />
        </div>
        <DialogFooter className="px-4 pb-4">
          <button
            type="button"
            onClick={onClose}
            className="h-8 px-3 rounded border text-sm border-gray-300 dark:border-gray-700 hover:bg-gray-100 dark:hover:bg-gray-800"
          >
            Discard
          </button>
          <button
            type="button"
            onClick={save}
            className="h-8 px-3 rounded text-sm bg-blue-600 text-white hover:bg-blue-700"
          >
            Save
          </button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
