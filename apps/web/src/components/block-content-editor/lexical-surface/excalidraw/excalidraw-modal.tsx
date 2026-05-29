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
import { useEffect, useRef, useState } from "react"
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
 * Selector that covers internal overlays/popovers of Excalidraw
 * (help, library, mermaid-to-excalidraw, color picker, etc.).
 * Used in the Radix Dialog guards to prevent clicks inside these
 * sub-modals from closing our host Dialog.
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
  const canvasContainerRef = useRef<HTMLDivElement | null>(null)
  const { resolvedTheme } = useTheme()
  const excalidrawTheme: "light" | "dark" = resolvedTheme === "dark" ? "dark" : "light"

  // The canvas of Excalidraw is mounted during the opening animation of
  // the Radix Dialog (zoom-in-95), which causes the initial bounding rect
  // measurements to be incorrect and the pointer hit-test
  // (showing the little hand exactly over the stroke) to be offset.
  // We call `refresh()` after the animation and on any container resize
  // to force a new measurement.
  useEffect(() => {
    if (!isShown || !excalidrawAPI) return
    const t1 = window.setTimeout(() => excalidrawAPI.refresh(), 200)
    const t2 = window.setTimeout(() => excalidrawAPI.refresh(), 600)
    const container = canvasContainerRef.current
    const ro = container
      ? new ResizeObserver(() => excalidrawAPI.refresh())
      : null
    if (container && ro) ro.observe(container)
    const onWinResize = () => excalidrawAPI.refresh()
    window.addEventListener("resize", onWinResize)
    return () => {
      window.clearTimeout(t1)
      window.clearTimeout(t2)
      ro?.disconnect()
      window.removeEventListener("resize", onWinResize)
    }
  }, [isShown, excalidrawAPI])

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
        {/*
          When an internal sub-modal of Excalidraw is open (Help,
          Mermaid-to-Excalidraw, Library, etc.), the side UI of the canvas
          (toolbar, zoom, hamburger, library) remains interactive because
          it has a higher z-index than `.Modal__background`. We block
          interaction with everything inside `.excalidraw` when there is
          a `.Modal`/`.Dialog`, and re-enable it only in the internal modal itself.
        */}
        <style>{`
          .excalidraw:has(.Modal) .layer-ui__wrapper > *:not(.Modal),
          .excalidraw:has(.Modal) .excalidraw__canvas,
          .excalidraw:has(.Modal) canvas {
            pointer-events: none !important;
          }
          .excalidraw .Modal,
          .excalidraw .Modal * {
            pointer-events: auto !important;
          }
          /* Allows scrolling with the mouse wheel inside internal sub-modals
             without propagating to the global canvas zoom. */
          .excalidraw .Modal,
          .excalidraw .Modal__content,
          .excalidraw .HelpDialog,
          .excalidraw .ttd-dialog {
            overscroll-behavior: contain;
          }
        `}</style>
        <div ref={canvasContainerRef} className={cn("flex-1 min-h-0")}>
          <Excalidraw
            excalidrawAPI={setExcalidrawAPI}
            theme={excalidrawTheme}
            initialData={{
              appState: {
                ...(initialAppState || { isLoading: false }),
                theme: (initialAppState?.theme ?? excalidrawTheme) as AppState["theme"],
                // Defaults "rich" based on the example "With Additional
                // properties" from the official documentation:
                // https://docs.excalidraw.com/docs/@excalidraw/excalidraw/api/excalidraw-element-skeleton
                // Apply only to new elements created in this session;
                // does not overwrite already saved elements.
                currentItemStrokeWidth: initialAppState?.currentItemStrokeWidth ?? 2,
                currentItemStrokeStyle: initialAppState?.currentItemStrokeStyle ?? "solid",
                currentItemFillStyle: initialAppState?.currentItemFillStyle ?? "solid",
                currentItemRoughness: initialAppState?.currentItemRoughness ?? 1,
                currentItemRoundness: initialAppState?.currentItemRoundness ?? "round",
                currentItemFontSize: initialAppState?.currentItemFontSize ?? 20,
                currentItemStrokeColor: initialAppState?.currentItemStrokeColor ?? "#1971c2",
                currentItemBackgroundColor:
                  initialAppState?.currentItemBackgroundColor ?? "#a5d8ff",
                currentItemStartArrowhead:
                  initialAppState?.currentItemStartArrowhead ?? null,
                currentItemEndArrowhead:
                  initialAppState?.currentItemEndArrowhead ?? "triangle",
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
