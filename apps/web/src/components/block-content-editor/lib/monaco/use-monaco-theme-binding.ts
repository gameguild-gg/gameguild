"use client"

import { useCallback, useEffect, useRef } from "react"
import type { Monaco } from "@monaco-editor/react"
import { isShikiActive, useShikiReady } from "@/components/block-content-editor/lib/shiki/highlighter"
import { getShikiThemeName, type ShikiTheme } from "@/components/block-content-editor/lib/shiki/themes"
import {
  registerMonacoSurface,
  type MonacoThemeHandle,
} from "@/components/block-content-editor/lib/shiki/theme-coordinator"

interface UseMonacoThemeBindingArgs {
  /** User-selected Shiki theme. Resolved to a light/dark variant. */
  shikiTheme: ShikiTheme
  /** Whether the host page is in dark mode. */
  isDark: boolean
  /**
   * Built-in Monaco theme used while Shiki hasn't finished initializing
   * (or when Shiki is unavailable). Defaults to the standard `"light"`
   * / `"vs-dark"` pair; mermaid passes `"mermaid-light"` / `"mermaid-dark"`.
   */
  fallbackLight?: string
  fallbackDark?: string
}

interface MonacoThemeBinding {
  /** Resolved Monaco theme name for the current frame. */
  currentTheme: string
  /** Whether Shiki has finished loading (for conditional rendering). */
  shikiReady: boolean
  /**
   * Register the surface with the global Monaco theme coordinator. Call
   * once from the editor's `onMount`. The binding tracks the latest
   * resolver via refs, refreshes on theme/dark-mode changes, and
   * unregisters on unmount so closing this surface restores the theme
   * of any underlying Monaco editor.
   */
  bindMonaco: (monaco: Monaco) => void
}

/**
 * Centralizes the Shiki/Monaco theme handshake that every Monaco wrapper
 * in the block-content-editor used to copy/paste:
 *
 * 1. Pick the Shiki theme name when Shiki is loaded; otherwise fall back
 *    to a built-in Monaco theme (light / vs-dark, or a per-surface
 *    custom theme such as mermaid-light).
 * 2. Register with the global theme coordinator so a LIFO stack of
 *    Monaco surfaces play nicely (e.g. opening a mermaid editor doesn't
 *    permanently steal the theme from an underlying code-studio preview).
 * 3. Refresh the coordinator whenever the resolver inputs change.
 * 4. Unregister on unmount.
 */
export function useMonacoThemeBinding({
  shikiTheme,
  isDark,
  fallbackLight = "light",
  fallbackDark = "vs-dark",
}: UseMonacoThemeBindingArgs): MonacoThemeBinding {
  const shikiReady = useShikiReady()

  const resolveTheme = useCallback((): string => {
    if (shikiReady && isShikiActive()) {
      return getShikiThemeName(shikiTheme, isDark)
    }
    return isDark ? fallbackDark : fallbackLight
  }, [shikiReady, shikiTheme, isDark, fallbackLight, fallbackDark])

  // Keep a ref to the resolver so the coordinator's closure always reads
  // the latest inputs without re-registering on every theme change.
  const resolveRef = useRef(resolveTheme)
  resolveRef.current = resolveTheme

  const handleRef = useRef<MonacoThemeHandle | null>(null)

  const bindMonaco = useCallback((monaco: Monaco) => {
    if (handleRef.current) return
    handleRef.current = registerMonacoSurface(monaco, () => resolveRef.current())
  }, [])

  // Refresh the coordinator whenever any input that affects the theme
  // changes. No-op when the handle hasn't been bound yet.
  useEffect(() => {
    handleRef.current?.refresh()
  }, [resolveTheme])

  // Unregister on unmount so the dominant theme falls back to whichever
  // surface was registered before this one.
  useEffect(
    () => () => {
      handleRef.current?.unregister()
      handleRef.current = null
    },
    [],
  )

  return { currentTheme: resolveTheme(), shikiReady, bindMonaco }
}
