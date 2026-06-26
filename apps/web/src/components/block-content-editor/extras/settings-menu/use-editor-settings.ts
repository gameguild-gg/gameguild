"use client"

import { useState, useEffect, useCallback } from "react"
import {
  type ModalSize,
  type AllPreferences,
  type MonacoOptionsPreferences,
  getModalSizeClasses,
  getAllPreferences,
  setGlobalPreference,
  setNodeTypePreference,
  clearNodeTypePreference,
  setMonacoOption,
  subscribeToPreferences,
} from "@/components/block-content-editor/lib/storage/editor/editor-preferences"

/**
 * Scope of a preference write. Only `modalSize` exposes a nodeType-
 * specific override (existing System tab behaviour). All Monaco-surface
 * options (`editor`, `preview`) are always global so the user gets a
 * single, formalized experience across the document.
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

  /** Inner panel sizing classes for the selected modal size. */
  modalClassName: string
  /** Outer overlay sizing classes for the selected modal size. */
  containerClassName: string

  /**
   * Global Monaco options applied to every editable surface (code-studio
   * secondary displays, html, markdown, mermaid, vega-lite, …). `null`
   * while preferences are still hydrating from IndexedDB.
   */
  editor: MonacoOptionsPreferences | null
  /**
   * Global Monaco options applied to read-only previews and to the
   * code-studio "base" display (what students see).
   */
  preview: MonacoOptionsPreferences | null

  /** Update a single key inside the global `editor` options group. */
  setEditorOption: <K extends keyof MonacoOptionsPreferences>(
    key: K,
    value: MonacoOptionsPreferences[K],
  ) => Promise<void>
  /** Update a single key inside the global `preview` options group. */
  setPreviewOption: <K extends keyof MonacoOptionsPreferences>(
    key: K,
    value: MonacoOptionsPreferences[K],
  ) => Promise<void>
}

export function useEditorSettings(nodeType: string): EditorSettings {
  const [showSettingsMenu, setShowSettingsMenu] = useState(false)
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

  // Modal size resolves with nodeType override; Monaco options are global-only.
  const modalSizeOverride = prefs?.nodeTypes[nodeType]?.modalSize
  const modalSize: ModalSize | null = prefs
    ? (modalSizeOverride ?? prefs.global.modalSize)
    : null
  const modalSizeIsOverride = modalSizeOverride !== undefined

  const editor: MonacoOptionsPreferences | null = prefs ? prefs.global.editor : null
  const preview: MonacoOptionsPreferences | null = prefs ? prefs.global.preview : null

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

  const setEditorOption = useCallback(
    async <K extends keyof MonacoOptionsPreferences>(key: K, value: MonacoOptionsPreferences[K]) => {
      await setMonacoOption('editor', key, value)
    },
    [],
  )

  const setPreviewOption = useCallback(
    async <K extends keyof MonacoOptionsPreferences>(key: K, value: MonacoOptionsPreferences[K]) => {
      await setMonacoOption('preview', key, value)
    },
    [],
  )

  return {
    nodeType,
    showSettingsMenu,
    setShowSettingsMenu,
    modalSize,
    modalSizeIsOverride,
    setModalSize,
    clearModalSizeOverride,
    modalClassName,
    containerClassName,
    editor,
    preview,
    setEditorOption,
    setPreviewOption,
  }
}
