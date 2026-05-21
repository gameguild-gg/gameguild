import type { editor } from "monaco-editor"
import type { MonacoOptionsPreferences } from "@/components/block-content-editor/lib/storage/editor/editor-preferences"
import { toMonacoRenderLineHighlight } from "./line-highlight"

/**
 * Defaults applied when the consumer hasn't wired a preferences snapshot
 * yet (e.g. SSR boundary, or a brief hydration window). Mirrors the
 * shape of the editor scope in {@link MonacoOptionsPreferences}.
 */
export const FALLBACK_MONACO_OPTIONS: MonacoOptionsPreferences = {
  shikiTheme: "github",
  fontSize: 14,
  lineNumbers: true,
  wordWrap: true,
  minimap: false,
  tabSize: 2,
  renderWhitespace: "none",
  renderLineHighlight: "line",
}

export type MonacoOptions = editor.IStandaloneEditorConstructionOptions

/**
 * Convert a {@link MonacoOptionsPreferences} bag (the user-facing,
 * persisted shape) into the inline Monaco options object that
 * `@monaco-editor/react`'s `<Editor options={...} />` expects.
 *
 * Always includes the cross-cutting defaults that every Monaco surface
 * in the block-content-editor wants (scrollBeyondLastLine,
 * automaticLayout, fixedOverflowWidgets to float hovers above the modal,
 * etc.). Pass `extra` to merge in per-surface options (mermaid's font
 * family, vega-lite's JSON snippet plumbing, code-studio's path
 * completion, …) — `extra` always wins on conflict.
 */
export function buildMonacoOptions(
  prefs: MonacoOptionsPreferences | null | undefined,
  extra: MonacoOptions = {},
): MonacoOptions {
  const p = prefs ?? FALLBACK_MONACO_OPTIONS
  return {
    fontSize: p.fontSize,
    lineNumbers: p.lineNumbers ? "on" : "off",
    minimap: { enabled: p.minimap },
    wordWrap: p.wordWrap ? "on" : "off",
    tabSize: p.tabSize,
    renderWhitespace: p.renderWhitespace,
    renderLineHighlight: toMonacoRenderLineHighlight(p.renderLineHighlight),
    scrollBeyondLastLine: false,
    automaticLayout: true,
    insertSpaces: true,
    // Render hover/suggest/context widgets at <body> level so they escape
    // the modal's `overflow: hidden` clipping and the page's stacking
    // contexts (otherwise method/MDN tooltips appear behind the modal).
    fixedOverflowWidgets: true,
    ...extra,
  }
}
