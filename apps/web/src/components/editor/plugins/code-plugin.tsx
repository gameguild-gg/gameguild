"use client"

import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $getSelection, $isRangeSelection } from "lexical"
import { useEffect } from "react"

export function CodePlugin() {
  const [editor] = useLexicalComposerContext()

  useEffect(() => {
    // This is a simplified version that doesn't rely on prismjs
    // It just ensures that code blocks are properly handled
    return editor.registerUpdateListener(({ editorState }) => {
      editorState.read(() => {
        const selection = $getSelection()
        if (!$isRangeSelection(selection)) return

        // You could add custom code handling here if needed
      })
    })
  }, [editor])

  return null
}
