"use client"

import { useEffect } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $insertNodes } from "lexical"
import { INSERT_HTML_COMMAND } from "./floating-content-insert-plugin"
import { $createHTMLNode } from "../nodes/html-node"

export function HTMLPlugin() {
  const [editor] = useLexicalComposerContext()

  useEffect(() => {
    if (!editor) return

    return editor.registerCommand(
      INSERT_HTML_COMMAND,
      () => {
        editor.update(() => {
          const htmlNode = $createHTMLNode()
          $insertNodes([htmlNode])
        })
        return true
      },
      1,
    )
  }, [editor])

  return null
}
