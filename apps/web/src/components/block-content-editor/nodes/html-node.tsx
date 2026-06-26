import type { SerializedBlockNode } from "./base/serialized-block-node"

/**
 * "html" custom-node — a sandboxed multi-file HTML/CSS/JSON/XML playground.
 *
 * Files are sandboxed: JavaScript-bearing extensions (.js/.ts/.mjs/.tsx, etc.)
 * are explicitly rejected at the editor boundary, and inline `<script>` tags
 * are stripped from the preview render. The intent is for users to compose
 * markup-only "custom" widgets that are safe to embed in shared content.
 *
 * Persistence shape:
 *   - `files`: ordered list of file entries; an entry named `index.html` is
 *     the render entry point.
 *   - `openTabs`: subset of `files[].id` currently open as tabs.
 *   - `activeFileId`: file currently shown in the Monaco editor.
 */
export interface HTMLFile {
  id: string
  /** Filename including extension; must be unique within `files`. */
  name: string
  content: string
}

export interface HTMLData {
  files: HTMLFile[]
  openTabs: string[]
  activeFileId?: string
}

export type SerializedHTMLNode = SerializedBlockNode<"html", HTMLData>
