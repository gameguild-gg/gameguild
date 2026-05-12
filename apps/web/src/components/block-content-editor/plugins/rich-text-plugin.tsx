"use client"

import { useEffect } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $insertNodes } from "lexical"
import { INSERT_RICH_TEXT_COMMAND } from "./floating-content-insert-plugin"
import { $createRichTextNode } from "../nodes/rich-text-node"

export function RichTextPlugin() {
  const [editor] = useLexicalComposerContext()

  useEffect(() => {
    if (!editor) return

    return editor.registerCommand(
      INSERT_RICH_TEXT_COMMAND,
      () => {
        editor.update(() => {
          const richTextNode = $createRichTextNode()
          $insertNodes([richTextNode])
        })
        return true
      },
      1,
    )
  }, [editor])

  return null
}
