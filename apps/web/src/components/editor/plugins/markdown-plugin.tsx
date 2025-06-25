"use client"

import { useEffect } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $insertNodes } from "lexical"
import { INSERT_MARKDOWN_COMMAND } from "./floating-content-insert-plugin"
import { $createMarkdownNode } from "../nodes/markdown-node"

export function MarkdownPlugin() {
  const [editor] = useLexicalComposerContext()

  useEffect(() => {
    if (!editor) return

    return editor.registerCommand(
      INSERT_MARKDOWN_COMMAND,
      () => {
        editor.update(() => {
          const markdownNode = $createMarkdownNode()
          $insertNodes([markdownNode])
        })
        return true
      },
      1,
    )
  }, [editor])

  return null
}
