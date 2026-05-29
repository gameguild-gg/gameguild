/**
 * CodeHighlightPlugin — registers Prism-based syntax highlighting for
 * `CodeNode`s. Without this, code blocks render as plain monospaced
 * text. Mirrors the playground's `CodeHighlightPlugin` which simply
 * calls `registerCodeHighlighting(editor)`.
 */
"use client"

import { useEffect } from "react"
import { registerCodeHighlighting } from "@lexical/code"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"

export function CodeHighlightPlugin(): null {
  const [editor] = useLexicalComposerContext()
  useEffect(() => registerCodeHighlighting(editor), [editor])
  return null
}
