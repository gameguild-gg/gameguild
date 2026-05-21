"use client"

import { useState, useEffect, useCallback } from "react"
import {
  type ModalSize,
  type AllPreferences,
  getModalSizeClasses,
  getAllPreferences,
  setGlobalPreference,
  setNodeTypePreference,
  clearNodeTypePreference,
  subscribeToPreferences,
} from "@/components/block-content-editor/lib/storage/editor/editor-preferences"
import type { ShikiTheme } from "@/components/block-content-editor/lib/shiki/themes"

/**
 * Scope of a preference write. Currently only `modalSize` exposes a
 * nodeType-specific override (the existing System tab behaviour). Theme
 * preferences are always global to keep the mental model simple.
 */
export type PreferenceScope = "global" | "nodeType"

export interface EditorSettings {
  nodeType: string
  showSettingsMenu: boolean
  setShowSettingsMenu: (show: boolean) => void

  // Modal size (resolved with nodeType override)
  modalSize: ModalSize | null
  modalSizeIsOverride: boolean
  setModalSize: (size: ModalSize, scope?: PreferenceScope) => Promise<void>
  clearModalSizeOverride: () => Promise<void>

  // Editor in-modal font/line numbers (UI-only, not persisted)
  editorFontSize: number
  setEditorFontSize: (size: number) => void
  editorLineNumbers: boolean
  setEditorLineNumbers: (show: boolean) => void

  /** Inner panel sizing classes for the selected modal size. */
  modalClassName: string
  /** Outer overlay sizing classes for the selected modal size. */
  containerClassName: string

  /**
   * Global syntax theme for all Monaco editor surfaces. `null` while
   * preferences are still loading from IndexedDB.
   */
  shikiTheme: ShikiTheme | null
  setShikiTheme: (theme: ShikiTheme) => Promise<void>

  /**
   * Global syntax theme used for preview / read-only rendering of any
   * Monaco-using block, including the code-studio "base" display.
   */
  previewShikiTheme: ShikiTheme | null
  setPreviewShikiTheme: (theme: ShikiTheme) => Promise<void>
}

export function useEditorSettings(nodeType: string): EditorSettings {
  const [showSettingsMenu, setShowSettingsMenu] = useState(false)
  const [editorFontSize, setEditorFontSize] = useState(14)
  const [editorLineNumbers, setEditorLineNumbers] = useState(true)
  const [prefs, setPrefs] = useState<AllPreferences | null>(null)

  // Hydrate preferences and re-hydrate whenever any editor in the app
  // writes a new value (cross-editor reactivity).
  useEffect(() => {
    let cancelled = false
    const load = () => {
      getAllPreferences().then((all) => {
        if (!cancelled) setPrefs(all)
      })
    }
    load()
    const unsubscribe = subscribeToPreferences(load)
    return () => {
      cancelled = true
      unsubscribe()
    }
  }, [nodeType])

  // Close settings menu on click outside
  useEffect(() => {
    const handler = (e: MouseEvent) => {
      const target = e.target as HTMLElement
      if (showSettingsMenu && !target.closest('.settings-menu-container')) {
        setShowSettingsMenu(false)
      }
    }
    document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [showSettingsMenu])

  // Modal size resolves with nodeType override; themes are global-only.
  const modalSizeOverride = prefs?.nodeTypes[nodeType]?.modalSize
  const modalSize: ModalSize | null = prefs
    ? (modalSizeOverride ?? prefs.global.modalSize)
    : null
  const modalSizeIsOverride = modalSizeOverride !== undefined

  const shikiTheme: ShikiTheme | null = prefs ? prefs.global.shikiTheme : null
  const previewShikiTheme: ShikiTheme | null = prefs ? prefs.global.previewShikiTheme : null

  const modalClasses = modalSize ? getModalSizeClasses(modalSize) : null
  const modalClassName = modalClasses?.modal ?? 'w-full max-w-7xl h-[90vh]'
  const containerClassName = modalClasses?.container ?? 'p-4'

  const setModalSize = useCallback(
    async (size: ModalSize, scope: PreferenceScope = "nodeType") => {
      if (scope === "global") {
        await setGlobalPreference("modalSize", size)
      } else {
        await setNodeTypePreference(nodeType, "modalSize", size)
      }
    },
    [nodeType],
  )

  const clearModalSizeOverride = useCallback(
    async () => {
      await clearNodeTypePreference(nodeType, "modalSize")
    },
    [nodeType],
  )

  const setShikiTheme = useCallback(async (theme: ShikiTheme) => {
    await setGlobalPreference("shikiTheme", theme)
  }, [])

  const setPreviewShikiTheme = useCallback(async (theme: ShikiTheme) => {
    await setGlobalPreference("previewShikiTheme", theme)
  }, [])

  return {
    nodeType,
    showSettingsMenu,
    setShowSettingsMenu,
    modalSize,
    modalSizeIsOverride,
    setModalSize,
    clearModalSizeOverride,
    editorFontSize,
    setEditorFontSize,
    editorLineNumbers,
    setEditorLineNumbers,
    modalClassName,
    containerClassName,
    shikiTheme,
    setShikiTheme,
    previewShikiTheme,
    setPreviewShikiTheme,
  }
}
