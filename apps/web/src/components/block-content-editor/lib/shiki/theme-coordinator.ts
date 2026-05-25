"use client"

import type { Monaco } from "@monaco-editor/react"
import * as monacoEditor from "monaco-editor"

type MonacoNamespace = Monaco | typeof monacoEditor

interface Registration {
  monaco: MonacoNamespace
  getTheme: () => string
}

/**
 * Monaco's `editor.setTheme(...)` is *global* — there is no per-editor
 * theme. That means whenever any Monaco surface (mermaid, vega-lite,
 * html, markdown, code-studio…) sets its theme, it stomps the theme of
 * every other surface currently rendered. When that surface unmounts,
 * the stomp persists and the still-mounted surfaces look wrong until
 * something else triggers them to reapply.
 *
 * This module is the single source of truth for the active Monaco theme.
 * Every Monaco wrapper registers its desired theme on mount and removes
 * itself on unmount. At any time the "dominant" theme is the one
 * declared by the *most recently registered* surface that is still
 * mounted — which mirrors the user's mental model: a modal editor opens
 * on top of the document, overrides the theme while it's visible, and
 * then yields back to the document previews when it closes.
 */
const registrations: Registration[] = []

function applyDominant(): void {
  const top = registrations[registrations.length - 1]
  if (!top) return
  try {
    const themeName = top.getTheme()
    if (themeName) {
      top.monaco.editor.setTheme(themeName)
    }
  } catch (err) {
    console.error("Monaco theme coordinator failed to apply theme:", err)
  }
}

export interface MonacoThemeHandle {
  /**
   * Re-evaluate `getTheme` and reapply if this surface is currently
   * dominant. Call when the user changes the theme preference.
   */
  refresh(): void
  /**
   * Remove this surface from the coordinator. Triggers reapplication of
   * the new dominant surface's theme (i.e. restores whatever was
   * underneath this modal).
   */
  unregister(): void
}

/**
 * Register a Monaco surface. Pass the `Monaco` namespace from
 * `@monaco-editor/react`'s `beforeMount`/`onMount`, or `monaco-editor`'s
 * own namespace for surfaces that mount Monaco directly (vega-lite).
 * The `getTheme` callback is invoked every time the coordinator needs
 * to apply this surface's theme, so it can read the current resolved
 * theme name from a ref/state without re-registering.
 */
export function registerMonacoSurface(
  monaco: MonacoNamespace,
  getTheme: () => string,
): MonacoThemeHandle {
  const reg: Registration = { monaco, getTheme }
  registrations.push(reg)
  applyDominant()
  let removed = false
  return {
    refresh() {
      if (removed) return
      applyDominant()
    },
    unregister() {
      if (removed) return
      removed = true
      const i = registrations.lastIndexOf(reg)
      if (i >= 0) registrations.splice(i, 1)
      applyDominant()
    },
  }
}
