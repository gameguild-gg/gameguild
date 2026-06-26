"use client"

import { lazy, type ComponentType } from "react"
import { useEffect, useRef } from "react"
import type { editor } from "monaco-editor"
import type { EditorProps, Monaco, OnMount } from "@monaco-editor/react"
import type {
  MonacoOptionsPreferences,
  RenderLineHighlight,
} from "@/components/block-content-editor/lib/storage/editor/editor-preferences"
import type { ShikiTheme } from "@/components/block-content-editor/lib/shiki/themes"
import { ensureShikiLoaded } from "@/components/block-content-editor/lib/shiki/highlighter"
import { MonacoErrorBoundary } from "@/components/block-content-editor/extras/code-studio/monaco-error-boundary"
import { applyLineHighlightDecoration } from "./line-highlight"
import {
  FALLBACK_MONACO_OPTIONS,
  buildMonacoOptions,
  type MonacoOptions,
} from "./build-monaco-options"
import { useMonacoThemeBinding } from "./use-monaco-theme-binding"
import { ClientOnlyLazy } from "../client-only-lazy"

const monacoFallback = (
  <div className="flex h-full items-center justify-center text-sm text-gray-500">
    Loading editor…
  </div>
)

// `@monaco-editor/react` is browser-only. Keep it behind a client-only
// lazy boundary without relying on Next's private loadable context.
const MonacoReactEditor = lazy(async () => ({
  default: (await import("@monaco-editor/react")).default as ComponentType<EditorProps>,
}))

export interface BaseMonacoEditorProps {
  /** Controlled value. Mutually exclusive with `defaultValue`. */
  value?: string
  /** Uncontrolled initial value (use with `keepCurrentModel` in multi-file scenarios). */
  defaultValue?: string
  /** Monaco language id (e.g. `'typescript'`, `'markdown'`, `'mermaid'`). */
  language: string
  /** Editor height. Defaults to `100%`. */
  height?: string | number
  /** Read-only mode. */
  readOnly?: boolean
  /** Whether the host page is in dark mode. Drives theme resolution. */
  isDark: boolean
  /**
   * Resolved preference snapshot. `null` is tolerated during hydration
   * and falls back to {@link FALLBACK_MONACO_OPTIONS}.
   */
  options?: MonacoOptionsPreferences | null
  /**
   * Per-surface Monaco options merged on top of the preference-derived
   * ones (always wins on conflict). Use sparingly — anything generic
   * should live in {@link buildMonacoOptions}.
   */
  extraOptions?: MonacoOptions
  /** Fallback Monaco theme used while Shiki is loading. Defaults to `"light"`. */
  fallbackLight?: string
  /** Fallback Monaco theme used while Shiki is loading in dark mode. Defaults to `"vs-dark"`. */
  fallbackDark?: string
  /** Forwarded to `<Editor path={...}>` for multi-file model isolation. */
  path?: string
  /** Forwarded to `<Editor keepCurrentModel={...}>`. */
  keepCurrentModel?: boolean
  /** Stable key to force a full editor re-instantiation (e.g. file switch). */
  editorKey?: string
  /** Called when the user edits the content. */
  onChange?: (value: string | undefined) => void
  /**
   * Called once Monaco has mounted. Use for per-surface setup: language
   * registration, snippet providers, command bindings, JSON schemas, …
   */
  onMount?: (editor: editor.IStandaloneCodeEditor, monaco: Monaco) => void
  /**
   * Called before Monaco mounts (after Shiki is requested). Use for
   * compiler-options / diagnostics defaults that must precede model
   * creation.
   */
  beforeMount?: (monaco: Monaco) => void | Promise<void>
}

/**
 * Centralized Monaco wrapper used by every block-content-editor surface
 * (code-studio, html, markdown, mermaid, vega-lite). Handles the bits
 * that were previously duplicated across five files:
 *
 *  - `MonacoErrorBoundary` and the dynamic `@monaco-editor/react` import.
 *  - Shiki bootstrapping (`ensureShikiLoaded` in `beforeMount`).
 *  - Theme resolution + registration with the global coordinator (see
 *    {@link useMonacoThemeBinding}).
 *  - Options assembly via {@link buildMonacoOptions}, including the
 *    cross-cutting `fixedOverflowWidgets` setting so hover/suggest
 *    widgets float above any host modal.
 *  - Per-editor `data-line-highlight-rectangle` CSS hook synced with
 *    the resolved `renderLineHighlight`.
 *
 * Surfaces only need to pass their language, their resolved preference
 * snapshot, and (optionally) an `onMount` callback for language-
 * specific plumbing.
 */
export function BaseMonacoEditor({
  value,
  defaultValue,
  language,
  height = "100%",
  readOnly = false,
  isDark,
  options,
  extraOptions,
  fallbackLight,
  fallbackDark,
  path,
  keepCurrentModel,
  editorKey,
  onChange,
  onMount,
  beforeMount,
}: BaseMonacoEditorProps) {
  const prefs = options ?? FALLBACK_MONACO_OPTIONS
  const { currentTheme, bindMonaco } = useMonacoThemeBinding({
    shikiTheme: prefs.shikiTheme as ShikiTheme,
    isDark,
    fallbackLight,
    fallbackDark,
  })

  const editorRef = useRef<editor.IStandaloneCodeEditor | null>(null)
  const renderLineHighlightRef = useRef<RenderLineHighlight>(prefs.renderLineHighlight)
  renderLineHighlightRef.current = prefs.renderLineHighlight

  const handleBeforeMount: EditorProps["beforeMount"] = (monaco) => {
    void ensureShikiLoaded(monaco)
    void beforeMount?.(monaco)
  }

  const handleMount: OnMount = (ed, monaco) => {
    editorRef.current = ed
    bindMonaco(monaco)
    applyLineHighlightDecoration(ed, renderLineHighlightRef.current)
    onMount?.(ed, monaco)
  }

  // Keep the rectangle-mode CSS hook in sync with the resolved value;
  // Monaco's own option update is handled through the `options` prop
  // diffing inside `@monaco-editor/react`.
  useEffect(() => {
    applyLineHighlightDecoration(editorRef.current, prefs.renderLineHighlight)
  }, [prefs.renderLineHighlight])

  const mergedOptions = buildMonacoOptions(prefs, { readOnly, ...extraOptions })
  const editorProps: EditorProps = {
    height,
    language,
    value,
    defaultValue,
    path,
    keepCurrentModel,
    onChange,
    beforeMount: handleBeforeMount,
    onMount: handleMount,
    theme: currentTheme,
    loading: "",
    options: mergedOptions,
  }

  return (
    <MonacoErrorBoundary>
      <ClientOnlyLazy
        key={editorKey}
        component={MonacoReactEditor}
        props={editorProps}
        fallback={monacoFallback}
      />
    </MonacoErrorBoundary>
  )
}
