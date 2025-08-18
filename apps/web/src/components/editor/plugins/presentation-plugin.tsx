"use client"

import { useEffect } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $insertNodes } from "lexical"
import { INSERT_PRESENTATION_COMMAND } from "./floating-content-insert-plugin"
import { $createPresentationNode } from "../nodes/presentation-node"

export function PresentationPlugin() {
  const [editor] = useLexicalComposerContext()

  useEffect(() => {
    if (!editor) return

    return editor.registerCommand(
      INSERT_PRESENTATION_COMMAND,
      () => {
        editor.update(() => {
          // Create an empty presentation node that will be populated by importing a file
          const presentationNode = $createPresentationNode()
          $insertNodes([presentationNode])
        })
        return true
      },
      1,
    )
  }, [editor])

  return null
}
