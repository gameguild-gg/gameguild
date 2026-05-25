import type * as monacoEditor from "monaco-editor"
import type { RenderLineHighlight } from "@/components/block-content-editor/lib/storage/editor/editor-preferences"

/** Monaco's native renderLineHighlight values (after we collapse our virtual `'rectangle'` mode). */
export type MonacoRenderLineHighlight = "none" | "gutter" | "line" | "all"

/**
 * Convert our extended `RenderLineHighlight` into the value Monaco actually
 * understands. `'rectangle'` becomes Monaco's `'line'`; the rectangle outline
 * itself is rendered by CSS once `applyLineHighlightDecoration` flips the
 * data-attribute on the editor's DOM root.
 */
export function toMonacoRenderLineHighlight(value: RenderLineHighlight): MonacoRenderLineHighlight {
  return value === "rectangle" ? "line" : value
}

/**
 * Toggle the `data-line-highlight-rectangle` attribute on the editor's
 * root DOM node so the global CSS rule that draws the active-line
 * outline applies (or stops applying). Safe to call before the editor
 * has mounted — it no-ops when the DOM node isn't available yet.
 *
 * This is intentionally per-editor instead of global so different Monaco
 * surfaces (e.g. an editor and a preview) can disagree on the rectangle
 * mode without stomping each other.
 */
export function applyLineHighlightDecoration(
  editor: monacoEditor.editor.IStandaloneCodeEditor | null | undefined,
  value: RenderLineHighlight,
): void {
  const dom = editor?.getDomNode()
  if (!dom) return
  if (value === "rectangle") {
    dom.setAttribute("data-line-highlight-rectangle", "true")
  } else {
    dom.removeAttribute("data-line-highlight-rectangle")
  }
}
