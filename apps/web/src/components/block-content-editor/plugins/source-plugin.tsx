"use client"

import { useEffect } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $insertNodes } from "lexical"
import { INSERT_SOURCE_COMMAND } from "./floating-content-insert-plugin"
import { $createSourceNode } from "../nodes/source-node"

export function SourcePlugin() {
  const [editor] = useLexicalComposerContext()

  useEffect(() => {
    if (!editor) return

    return editor.registerCommand(
      INSERT_SOURCE_COMMAND,
      () => {
        editor.update(() => {
          const sourceNode = $createSourceNode()
          $insertNodes([sourceNode])
        })
        return true
      },
      1,
    )
  }, [editor])

  return null
}
