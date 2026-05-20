"use client"

import { useEffect, type ReactNode } from "react"
import { Button } from "@/components/ui/button"
import { X } from "lucide-react"
import { cn } from "@/lib/utils"
import { EditorSettingsButton } from "../settings-menu/editor-settings-button"
import { type EditorSettings } from "../settings-menu/use-editor-settings"

interface BlockEditorShellProps {
  /**
   * Settings descriptor produced by `useEditorSettings(nodeType)`. Drives
   * modal sizing (compact / widescreen / ultrawide / fullscreen) and the
   * settings-menu button placed in the header. Required so every block
   * editor honours the user's persisted preferences and stays consistent.
   */
  settings: EditorSettings
  /** Lucide icon (or any node) rendered before the title. */
  icon?: ReactNode
  /** Editor title shown in the header. */
  title: ReactNode
  /**
   * Optional slot rendered next to the title (badges, theme info, validation
   * pills, type/style indicators \u2014 anything block-specific that belongs in
   * the header).
   */
  headerMeta?: ReactNode
  /**
   * Optional extra buttons placed before the settings button in the top-right
   * corner of the header (e.g. download / export actions).
   */
  headerActions?: ReactNode
  /**
   * Optional secondary header bar rendered immediately below the main header
   * (e.g. for type/design dropdowns, settings rows, layout edit toolbars).
   */
  secondaryHeader?: ReactNode
  /**
   * Optional footer (typically Cancel/Save actions, plus an optional caption
   * input). When omitted, no footer is rendered.
   */
  footer?: ReactNode
  /** Body content of the editor. Will fill the remaining vertical space. */
  children: ReactNode
  /** Called when the user dismisses the modal (X button, backdrop, Escape). */
  onClose: () => void
  /** Extra classes for the body container (rare \u2014 normally unneeded). */
  bodyClassName?: string
  /**
   * Suppress the close-on-backdrop-click behaviour. Useful for editors that
   * may have unsaved-changes guards or open inline dialogs.
   */
  disableBackdropClose?: boolean
}

/**
 * Shared shell for block editor modals (admonition, mermaid, vega-lite,
 * html, markdown, media, code-studio, etc.).
 *
 * Why this exists: every block editor used to copy/paste the same outer
 * markup \u2014 fixed overlay, body-scroll lock, header with icon+title+close,
 * footer, escape-to-close. Inconsistent copies drifted (notably, the
 * \u201cFull Screen\u201d modal size only worked correctly in code-studio because
 * the others kept a hard-coded `p-4` outer padding that left a visible
 * frame even in fullscreen mode). Centralising the shell removes that
 * drift and concentrates accessibility & sizing concerns in one place.
 *
 * The shell intentionally stays minimal: it knows only what every editor
 * needs (overlay, header chrome, settings button, footer slot). Editor-
 * specific UI lives in `children`, `headerMeta`, `secondaryHeader` and
 * `footer` slots.
 */
export function BlockEditorShell({
  settings,
  icon,
  title,
  headerMeta,
  headerActions,
  secondaryHeader,
  footer,
  children,
  onClose,
  bodyClassName,
  disableBackdropClose = false,
}: BlockEditorShellProps) {
  // Block page scrolling and click-through while the modal is open. We also
  // clear `pointer-events` on the body so background widgets (lexical
  // toolbars, drag layers, etc.) can't intercept clicks while the modal is
  // visible \u2014 the modal itself re-enables pointer events on its containers.
  useEffect(() => {
    const prevOverflow = document.body.style.overflow
    const prevPointer = document.body.style.pointerEvents
    document.body.style.overflow = "hidden"
    document.body.style.pointerEvents = "none"
    return () => {
      document.body.style.overflow = prevOverflow
      document.body.style.pointerEvents = prevPointer
    }
  }, [])

  // While we wait for the persisted modal size to load, render nothing to
  // avoid flashing the wrong dimensions. `useEditorSettings` returns null
  // until IndexedDB resolves \u2014 same behaviour as legacy code-studio modal.
  if (!settings.modalSize) {
    return null
  }

  return (
    <div
      className={cn(
        "fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50",
        settings.containerClassName,
      )}
      style={{ pointerEvents: "auto" }}
      onClick={disableBackdropClose ? undefined : onClose}
      onMouseDown={(e) => e.stopPropagation()}
      onKeyDown={(e) => {
        if (e.key === "Escape") onClose()
        e.stopPropagation()
      }}
    >
      <div
        className={cn(
          "bg-white dark:bg-gray-900 border dark:border-gray-700 shadow-2xl flex flex-col",
          settings.modalClassName,
        )}
        style={{ pointerEvents: "auto" }}
        onClick={(e) => e.stopPropagation()}
        onKeyDown={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
          <div className="flex items-center gap-2 min-w-0">
            {icon}
            <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100 truncate">
              {title}
            </h2>
            {headerMeta && (
              <div className="ml-4 flex items-center gap-3 pl-4 border-l border-gray-300 dark:border-gray-600 min-w-0">
                {headerMeta}
              </div>
            )}
          </div>
          <div className="flex items-center gap-2 shrink-0">
            {headerActions}
            <EditorSettingsButton settings={settings} />
            <Button
              variant="ghost"
              size="sm"
              onClick={onClose}
              className="hover:bg-gray-100 dark:hover:bg-gray-800"
              title="Close"
            >
              <X className="h-4 w-4" />
            </Button>
          </div>
        </div>

        {secondaryHeader && (
          <div className="border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
            {secondaryHeader}
          </div>
        )}

        {/* Body */}
        <div className={cn("flex-1 min-h-0 flex flex-col", bodyClassName)}>
          {children}
        </div>

        {footer && (
          <div className="p-4 border-t border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
            {footer}
          </div>
        )}
      </div>
    </div>
  )
}
